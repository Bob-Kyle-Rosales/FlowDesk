namespace FlowDesk.Core.DTOs.Invoices;

public record InvoiceItemResponse(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);
