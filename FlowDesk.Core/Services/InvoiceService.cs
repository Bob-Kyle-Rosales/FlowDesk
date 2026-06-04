using FlowDesk.Core.DTOs.Invoices;
using FlowDesk.Core.Entities;
using FlowDesk.Core.Enums;
using FlowDesk.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlowDesk.Core.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IInvoiceRepository repo,
        ICurrentUserService currentUser,
        ILogger<InvoiceService> logger)
    {
        _repo = repo;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IEnumerable<InvoiceResponse>> GetAllAsync()
        => (await _repo.GetAllAsync()).Select(ToResponse);

    public async Task<InvoiceResponse> GetByIdAsync(Guid id)
        => ToResponse(await GetOrThrowAsync(id));

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
