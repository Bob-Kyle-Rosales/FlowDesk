using FlowDesk.Core.Enums;

namespace FlowDesk.Core.DTOs.Invoices;

public record InvoiceResponse(
    Guid Id,
    string Title,
    InvoiceStatus Status,
    decimal Total,
    DateTime? DueDate,
    DateTime? PaidAt,
    DateTime CreatedAt,
    Guid ClientId,
    string ClientName,
    Guid? ProjectId,
    string? ProjectName,
    List<InvoiceItemResponse> Items);
