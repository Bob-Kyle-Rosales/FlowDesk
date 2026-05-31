using FlowDesk.Core.Entities;

namespace FlowDesk.Core.Interfaces;

public interface IProjectRepository
{
    Task<IEnumerable<Project>> GetAllAsync(Guid? clientIdFilter);
    Task<Project?> GetByIdAsync(Guid id);
    Task<Project> CreateAsync(Project project);
    Task UpdateAsync(Project project);
    Task DeleteAsync(Project project);
    Task<(int milestoneCount, int completedMilestones, int deliverableCount, int approvedDeliverables)>
        GetStatsAsync(Guid projectId);
}
