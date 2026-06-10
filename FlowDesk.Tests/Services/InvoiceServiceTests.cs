using FlowDesk.Core.DTOs.Invoices;
using FlowDesk.Core.Entities;
using FlowDesk.Core.Enums;
using FlowDesk.Core.Interfaces;
using FlowDesk.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FlowDesk.Tests.Services;

public class InvoiceServiceTests
{
    private readonly Mock<IInvoiceRepository> _invoiceRepo = new();
    private readonly Mock<IOrganisationRepository> _orgRepo = new();
    private readonly Mock<IStripeService> _stripe = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly InvoiceService _sut;

    public InvoiceServiceTests()
    {
        _currentUser.Setup(u => u.Role).Returns("AgencyOwner");
        _currentUser.Setup(u => u.OrganisationId).Returns(Guid.NewGuid());

        _sut = new InvoiceService(
            _invoiceRepo.Object,
            _orgRepo.Object,
            _stripe.Object,
            _email.Object,
            _currentUser.Object,
            NullLogger<InvoiceService>.Instance);
    }

    private static Invoice BuildInvoice(Guid id, InvoiceStatus status, Guid orgId, Guid clientId) =>
        new()
        {
            Id = id,
            Title = "Invoice #1",
            Status = status,
            OrganisationId = orgId,
            ClientId = clientId,
            Client = new User { Id = clientId, Name = "Client", Email = "client@example.com" },
            Items = new List<InvoiceItem> { new() { Description = "Work", Quantity = 1, UnitPrice = 500m } }
        };

    [Fact]
    public async Task UpdateAsync_NonDraftInvoice_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _invoiceRepo.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new Invoice { Id = id, Status = InvoiceStatus.Sent });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateAsync(id, new UpdateInvoiceRequest("Title", Guid.NewGuid(), null, null, new())));
    }

    [Fact]
    public async Task DeleteAsync_NonDraftInvoice_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _invoiceRepo.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new Invoice { Id = id, Status = InvoiceStatus.Paid });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeleteAsync(id));
    }

    [Fact]
    public async Task SendAsync_NonDraftInvoice_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _invoiceRepo.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new Invoice { Id = id, Status = InvoiceStatus.Sent });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SendAsync(id));
    }

    [Fact]
    public async Task SendAsync_DraftInvoice_ChangesStatusToSentAndSendsEmail()
    {
        var id = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var invoice = BuildInvoice(id, InvoiceStatus.Draft, orgId, clientId);

        _invoiceRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(invoice);
        _invoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns(Task.CompletedTask);
        _orgRepo.Setup(r => r.GetByIdAsync(orgId))
            .ReturnsAsync(new Organisation { Id = orgId, Name = "Test Agency" });
        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _sut.SendAsync(id);

        _invoiceRepo.Verify(r => r.UpdateAsync(It.Is<Invoice>(inv => inv.Status == InvoiceStatus.Sent)), Times.Once);
        _email.Verify(e => e.SendAsync(
            invoice.Client.Email, invoice.Client.Name,
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task PayAsync_NonSentInvoice_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _invoiceRepo.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new Invoice { Id = id, Status = InvoiceStatus.Draft });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.PayAsync(id));
    }

    [Fact]
    public async Task PayAsync_NoStripeAccount_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        _invoiceRepo.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new Invoice { Id = id, Status = InvoiceStatus.Sent, OrganisationId = orgId });
        _orgRepo.Setup(r => r.GetByIdAsync(orgId))
            .ReturnsAsync(new Organisation { Id = orgId, StripeAccountId = null });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.PayAsync(id));
    }

    [Fact]
    public async Task PayAsync_SentInvoiceWithStripeAccount_ReturnsClientSecret()
    {
        var id = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = id, Status = InvoiceStatus.Sent, OrganisationId = orgId,
            Items = new List<InvoiceItem> { new() { Quantity = 2, UnitPrice = 250m } }
        };

        _invoiceRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(invoice);
        _invoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns(Task.CompletedTask);
        _orgRepo.Setup(r => r.GetByIdAsync(orgId))
            .ReturnsAsync(new Organisation { Id = orgId, StripeAccountId = "acct_test123" });
        _stripe.Setup(s => s.CreatePaymentIntentAsync(500m, "acct_test123"))
            .ReturnsAsync(("pi_secret_xyz", "pi_intent_123"));

        var result = await _sut.PayAsync(id);

        Assert.Equal("pi_secret_xyz", result.ClientSecret);
    }

    [Fact]
    public async Task HandlePaymentSucceededAsync_UnknownIntent_DoesNotThrow()
    {
        _invoiceRepo.Setup(r => r.GetByPaymentIntentIdAsync("pi_unknown")).ReturnsAsync((Invoice?)null);

        var ex = await Record.ExceptionAsync(() => _sut.HandlePaymentSucceededAsync("pi_unknown"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task HandlePaymentSucceededAsync_AlreadyPaidInvoice_SkipsUpdate()
    {
        _invoiceRepo.Setup(r => r.GetByPaymentIntentIdAsync("pi_abc"))
            .ReturnsAsync(new Invoice { Status = InvoiceStatus.Paid });

        await _sut.HandlePaymentSucceededAsync("pi_abc");

        _invoiceRepo.Verify(r => r.UpdateAsync(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task HandlePaymentSucceededAsync_SentInvoice_MarksAsPaid()
    {
        var invoice = new Invoice { Status = InvoiceStatus.Sent };
        _invoiceRepo.Setup(r => r.GetByPaymentIntentIdAsync("pi_def")).ReturnsAsync(invoice);
        _invoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns(Task.CompletedTask);

        await _sut.HandlePaymentSucceededAsync("pi_def");

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.NotNull(invoice.PaidAt);
        _invoiceRepo.Verify(r => r.UpdateAsync(invoice), Times.Once);
    }
}
