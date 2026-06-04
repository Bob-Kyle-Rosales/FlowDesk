namespace FlowDesk.Core.DTOs.Invoices;

public record UpdateInvoiceRequest(
    string Title,
    Guid ClientId,
    Guid? ProjectId,
    DateTime? DueDate,
    List<InvoiceItemRequest> Items);
