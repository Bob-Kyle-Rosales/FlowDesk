using FlowDesk.Core.Entities;
using FlowDesk.Core.Enums;
using FlowDesk.Core.Interfaces;
using FlowDesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Project>> GetAllAsync(Guid? clientIdFilter)
    {
        var query = _context.Projects.Include(p => p.Client).AsQueryable();

        if (clientIdFilter.HasValue)
            query = query.Where(p => p.ClientId == clientIdFilter.Value);

        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<Project?> GetByIdAsync(Guid id)
        => await _context.Projects
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Project> CreateAsync(Project project)
    {
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async Task UpdateAsync(Project project)
    {
        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Project project)
    {
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
    }

    public async Task<(int milestoneCount, int completedMilestones, int deliverableCount, int approvedDeliverables)>
        GetStatsAsync(Guid projectId)
    {
        var milestoneStatuses = await _context.Milestones
            .Where(m => m.ProjectId == projectId)
            .Select(m => m.Status)
            .ToListAsync();

        var deliverableStatuses = await _context.Deliverables
            .Where(d => d.ProjectId == projectId)
            .Select(d => d.Status)
            .ToListAsync();

        return (
            milestoneStatuses.Count,
            milestoneStatuses.Count(s => s == MilestoneStatus.Completed),
            deliverableStatuses.Count,
            deliverableStatuses.Count(s => s == DeliverableStatus.Approved)
        );
    }
}
