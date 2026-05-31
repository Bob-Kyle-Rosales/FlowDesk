using FlowDesk.Core.Entities;

namespace FlowDesk.Core.Interfaces;

public interface IMilestoneRepository
{
    Task<IEnumerable<Milestone>> GetAllByProjectAsync(Guid projectId);
    Task<Milestone?> GetByIdAsync(Guid id);
    Task<Milestone> CreateAsync(Milestone milestone);
    Task UpdateAsync(Milestone milestone);
    Task DeleteAsync(Milestone milestone);
}
