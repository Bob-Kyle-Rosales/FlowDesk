using FlowDesk.Core.Entities;

namespace FlowDesk.Core.Interfaces;

public interface IInvoiceRepository
{
    Task<IEnumerable<Invoice>> GetAllAsync(Guid? clientIdFilter);
    Task<Invoice?> GetByIdAsync(Guid id);
    Task<Invoice?> GetByPaymentIntentIdAsync(string paymentIntentId);
    Task<Invoice> CreateAsync(Invoice invoice);
    Task UpdateAsync(Invoice invoice);
    Task DeleteAsync(Invoice invoice);
}
