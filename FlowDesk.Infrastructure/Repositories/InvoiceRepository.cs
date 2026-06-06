using FlowDesk.Core.Entities;
using FlowDesk.Core.Interfaces;
using FlowDesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _context;

    public InvoiceRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Invoice>> GetAllAsync(Guid? clientIdFilter)
    {
        var query = _context.Invoices
            .Include(i => i.Client)
            .Include(i => i.Project)
            .Include(i => i.Items)
            .AsQueryable();

        if (clientIdFilter.HasValue)
            query = query.Where(i => i.ClientId == clientIdFilter.Value);

        return await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
    }

    public async Task<Invoice?> GetByIdAsync(Guid id)
        => await _context.Invoices
            .Include(i => i.Client)
            .Include(i => i.Project)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<Invoice?> GetByPaymentIntentIdAsync(string paymentIntentId)
        => await _context.Invoices
            .IgnoreQueryFilters()
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.StripePaymentIntentId == paymentIntentId);

    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        await _context.Invoices.AddAsync(invoice);
        await _context.SaveChangesAsync();
        return invoice;
    }

    public async Task UpdateAsync(Invoice invoice)
    {
        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Invoice invoice)
    {
        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();
    }
}
