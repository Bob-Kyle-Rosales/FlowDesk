namespace FlowDesk.Core.DTOs.Invoices;

public record CreateInvoiceRequest(
    string Title,
    Guid ClientId,
    Guid? ProjectId,
    DateTime? DueDate,
    List<InvoiceItemRequest> Items);
