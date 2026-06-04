using FlowDesk.Core.DTOs.Invoices;

namespace FlowDesk.Core.Interfaces;

public interface IInvoiceService
{
    Task<IEnumerable<InvoiceResponse>> GetAllAsync();
    Task<InvoiceResponse> GetByIdAsync(Guid id);
    Task<InvoiceResponse> CreateAsync(CreateInvoiceRequest request);
    Task<InvoiceResponse> UpdateAsync(Guid id, UpdateInvoiceRequest request);
    Task DeleteAsync(Guid id);
}
