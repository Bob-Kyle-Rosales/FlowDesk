using FlowDesk.Core.Entities;

namespace FlowDesk.Core.Interfaces;

public interface IDeliverableRepository
{
    Task<IEnumerable<Deliverable>> GetAllByProjectAsync(Guid projectId);
    Task<Deliverable?> GetByIdAsync(Guid id);
    Task<Deliverable> CreateAsync(Deliverable deliverable);
    Task UpdateAsync(Deliverable deliverable);
}
