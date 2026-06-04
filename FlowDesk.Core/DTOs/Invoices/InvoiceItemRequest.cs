namespace FlowDesk.Core.DTOs.Invoices;

public record InvoiceItemRequest(string Description, decimal Quantity, decimal UnitPrice);
