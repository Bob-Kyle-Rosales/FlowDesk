using FlowDesk.Core.DTOs.Projects;

namespace FlowDesk.Core.Interfaces;

public interface IProjectService
{
    Task<IEnumerable<ProjectResponse>> GetAllAsync();
    Task<ProjectResponse> GetByIdAsync(Guid id);
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request);
    Task<ProjectResponse> UpdateAsync(Guid id, UpdateProjectRequest request);
    Task DeleteAsync(Guid id);
    Task<ProjectStatsResponse> GetStatsAsync(Guid id);
}
