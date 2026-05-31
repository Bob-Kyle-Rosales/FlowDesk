using FlowDesk.Core.Entities;
using FlowDesk.Core.Interfaces;
using FlowDesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Repositories;

public class DeliverableRepository : IDeliverableRepository
{
    private readonly AppDbContext _context;

    public DeliverableRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Deliverable>> GetAllByProjectAsync(Guid projectId)
        => await _context.Deliverables
            .Where(d => d.ProjectId == projectId)
            .Where(d => _context.Projects.Any(p => p.Id == d.ProjectId))
            .OrderBy(d => d.CreatedAt)
            .ToListAsync();

    // Filters through org-scoped Projects to prevent cross-tenant access.
    public async Task<Deliverable?> GetByIdAsync(Guid id)
        => await _context.Deliverables
            .Where(d => d.Id == id)
            .Where(d => _context.Projects.Any(p => p.Id == d.ProjectId))
            .FirstOrDefaultAsync();

    public async Task<Deliverable> CreateAsync(Deliverable deliverable)
    {
        await _context.Deliverables.AddAsync(deliverable);
        await _context.SaveChangesAsync();
        return deliverable;
    }

    public async Task UpdateAsync(Deliverable deliverable)
    {
        _context.Deliverables.Update(deliverable);
        await _context.SaveChangesAsync();
    }
}
