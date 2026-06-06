using FlowDesk.Core.DTOs.Invoices;
using FlowDesk.Core.Entities;
using FlowDesk.Core.Enums;
using FlowDesk.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlowDesk.Core.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _repo;
    private readonly IOrganisationRepository _orgRepo;
    private readonly IStripeService _stripe;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IInvoiceRepository repo,
        IOrganisationRepository orgRepo,
        IStripeService stripe,
        ICurrentUserService currentUser,
        ILogger<InvoiceService> logger)
    {
        _repo = repo;
        _orgRepo = orgRepo;
        _stripe = stripe;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IEnumerable<InvoiceResponse>> GetAllAsync()
    {
        var clientFilter = _currentUser.Role == UserRole.Client.ToString() ? _currentUser.UserId : null;
        return (await _repo.GetAllAsync(clientFilter)).Select(ToResponse);
    }

    public async Task<InvoiceResponse> GetByIdAsync(Guid id)
    {
        var invoice = await GetOrThrowAsync(id);
        if (_currentUser.Role == UserRole.Client.ToString() && invoice.ClientId != _currentUser.UserId)
            throw new UnauthorizedAccessException("Access denied.");
        return ToResponse(invoice);
    }

    public async Task<InvoiceResponse> CreateAsync(CreateInvoiceRequest request)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            ClientId = request.ClientId,
            ProjectId = request.ProjectId,
            DueDate = request.DueDate,
            OrganisationId = _currentUser.OrganisationId!.Value,
            Items = request.Items.Select(i => new InvoiceItem
            {
                Id = Guid.NewGuid(),
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        await _repo.CreateAsync(invoice);
        var created = await _repo.GetByIdAsync(invoice.Id)
            ?? throw new InvalidOperationException("Failed to load created invoice.");
        _logger.LogInformation("Invoice {InvoiceId} created", created.Id);
        return ToResponse(created);
    }

    public async Task<InvoiceResponse> UpdateAsync(Guid id, UpdateInvoiceRequest request)
    {
        var invoice = await GetOrThrowAsync(id);
        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only Draft invoices can be edited.");

        invoice.Title = request.Title;
        invoice.ClientId = request.ClientId;
        invoice.ProjectId = request.ProjectId;
        invoice.DueDate = request.DueDate;
        invoice.Items = request.Items.Select(i => new InvoiceItem
        {
            Id = Guid.NewGuid(),
            Description = i.Description,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            InvoiceId = invoice.Id
        }).ToList();

        await _repo.UpdateAsync(invoice);
        var updated = await _repo.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Failed to load updated invoice.");
        _logger.LogInformation("Invoice {InvoiceId} updated", id);
        return ToResponse(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        var invoice = await GetOrThrowAsync(id);
        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only Draft invoices can be deleted.");
        await _repo.DeleteAsync(invoice);
        _logger.LogInformation("Invoice {InvoiceId} deleted", id);
    }

    public async Task<InvoiceResponse> SendAsync(Guid id)
    {
        var invoice = await GetOrThrowAsync(id);
        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only Draft invoices can be sent.");

        invoice.Status = InvoiceStatus.Sent;
        await _repo.UpdateAsync(invoice);
        _logger.LogInformation("Invoice {InvoiceId} sent", id);

        var updated = await _repo.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Failed to load sent invoice.");
        return ToResponse(updated);
    }

    public async Task<PayInvoiceResponse> PayAsync(Guid id)
    {
        var invoice = await GetOrThrowAsync(id);

        if (_currentUser.Role == UserRole.Client.ToString() && invoice.ClientId != _currentUser.UserId)
            throw new UnauthorizedAccessException("Access denied.");

        if (invoice.Status != InvoiceStatus.Sent)
            throw new InvalidOperationException("Only Sent invoices can be paid.");

        var org = await _orgRepo.GetByIdAsync(invoice.OrganisationId)
            ?? throw new KeyNotFoundException("Organisation not found.");

        if (string.IsNullOrEmpty(org.StripeAccountId))
            throw new InvalidOperationException("The agency has not connected a Stripe account yet.");

        var total = invoice.Items.Sum(i => i.Quantity * i.UnitPrice);

        // Known MVP limitation: if Stripe succeeds but the DB save below fails, a PaymentIntent
        // exists in Stripe with no local record. The webhook will log a warning and skip it.
        // Manual cleanup via Stripe Dashboard may be needed in that rare case.
        var (clientSecret, paymentIntentId) = await _stripe.CreatePaymentIntentAsync(total, org.StripeAccountId);

        invoice.StripePaymentIntentId = paymentIntentId;
        await _repo.UpdateAsync(invoice);

        _logger.LogInformation("PaymentIntent {PaymentIntentId} created for invoice {InvoiceId}",
            paymentIntentId, id);

        return new PayInvoiceResponse(clientSecret);
    }

    public async Task HandlePaymentSucceededAsync(string paymentIntentId)
    {
        var invoice = await _repo.GetByPaymentIntentIdAsync(paymentIntentId);
        if (invoice == null)
        {
            _logger.LogWarning("payment_intent.succeeded for unknown PaymentIntentId {Id}", paymentIntentId);
            return;
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            _logger.LogInformation("Webhook for already-paid invoice {InvoiceId} — skipping", invoice.Id);
            return;
        }

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;
        await _repo.UpdateAsync(invoice);
        _logger.LogInformation("Invoice {InvoiceId} marked Paid via webhook", invoice.Id);
    }

    private async Task<Invoice> GetOrThrowAsync(Guid id)
        => await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Invoice {id} not found.");

    private static InvoiceResponse ToResponse(Invoice inv) => new(
        inv.Id,
        inv.Title,
        inv.Status,
        inv.Items.Sum(i => i.Quantity * i.UnitPrice),
        inv.DueDate,
        inv.PaidAt,
        inv.CreatedAt,
        inv.ClientId,
        inv.Client?.Name ?? string.Empty,
        inv.ProjectId,
        inv.Project?.Name,
        inv.Items.Select(i => new InvoiceItemResponse(
            i.Id,
            i.Description,
            i.Quantity,
            i.UnitPrice,
            i.Quantity * i.UnitPrice)).ToList());
}
