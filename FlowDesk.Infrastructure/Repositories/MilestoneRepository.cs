using FlowDesk.Core.Entities;
using FlowDesk.Core.Interfaces;
using FlowDesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Repositories;

public class MilestoneRepository : IMilestoneRepository
{
    private readonly AppDbContext _context;

    public MilestoneRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Milestone>> GetAllByProjectAsync(Guid projectId)
        => await _context.Milestones
            .Where(m => m.ProjectId == projectId)
            .Where(m => _context.Projects.Any(p => p.Id == m.ProjectId))
            .OrderBy(m => m.Order)
            .ToListAsync();

    // Filters through the org-scoped Projects table to prevent cross-tenant access.
    // A milestone whose project belongs to another org will not match the Any() subquery.
    public async Task<Milestone?> GetByIdAsync(Guid id)
        => await _context.Milestones
            .Where(m => m.Id == id)
            .Where(m => _context.Projects.Any(p => p.Id == m.ProjectId))
            .FirstOrDefaultAsync();

    public async Task<Milestone> CreateAsync(Milestone milestone)
    {
        await _context.Milestones.AddAsync(milestone);
        await _context.SaveChangesAsync();
        return milestone;
    }

    public async Task UpdateAsync(Milestone milestone)
    {
        _context.Milestones.Update(milestone);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Milestone milestone)
    {
        _context.Milestones.Remove(milestone);
        await _context.SaveChangesAsync();
    }
}
