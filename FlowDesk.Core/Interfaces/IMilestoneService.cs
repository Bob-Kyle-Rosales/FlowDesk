using FlowDesk.Core.DTOs.Milestones;

namespace FlowDesk.Core.Interfaces;

public interface IMilestoneService
{
    Task<IEnumerable<MilestoneResponse>> GetAllByProjectAsync(Guid projectId);
    Task<MilestoneResponse> CreateAsync(Guid projectId, CreateMilestoneRequest request);
    Task<MilestoneResponse> UpdateAsync(Guid id, UpdateMilestoneRequest request);
    Task DeleteAsync(Guid id);
    Task<MilestoneResponse> UpdateStatusAsync(Guid id, UpdateMilestoneStatusRequest request);
}
