using FlowDesk.Core.DTOs.Projects;
using FlowDesk.Core.Entities;
using FlowDesk.Core.Enums;
using FlowDesk.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlowDesk.Core.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IProjectRepository repo, ICurrentUserService currentUser, ILogger<ProjectService> logger)
    {
        _repo = repo;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IEnumerable<ProjectResponse>> GetAllAsync()
    {
        var clientFilter = _currentUser.Role == "Client" ? _currentUser.UserId : null;
        var projects = await _repo.GetAllAsync(clientFilter);
        return projects.Select(ToResponse);
    }

    public async Task<ProjectResponse> GetByIdAsync(Guid id)
        => ToResponse(await GetAccessibleProjectAsync(id));

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            ClientId = request.ClientId,
            OrganisationId = _currentUser.OrganisationId!.Value,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(project);

        // Reload to populate Client navigation property
        var created = await _repo.GetByIdAsync(project.Id)
            ?? throw new InvalidOperationException("Failed to load created project.");
        _logger.LogInformation("Project {ProjectId} created by org {OrgId}", created.Id, created.OrganisationId);
        return ToResponse(created);
    }

    public async Task<ProjectResponse> UpdateAsync(Guid id, UpdateProjectRequest request)
    {
        var project = await GetAccessibleProjectAsync(id);

        if (!Enum.TryParse<ProjectStatus>(request.Status, out var status))
            throw new InvalidOperationException($"Invalid status: {request.Status}.");

        project.Name = request.Name;
        project.Description = request.Description;
        project.Status = status;

        await _repo.UpdateAsync(project);
        _logger.LogInformation("Project {ProjectId} updated", id);
        return ToResponse(project);
    }

    public async Task DeleteAsync(Guid id)
    {
        var project = await GetAccessibleProjectAsync(id);
        await _repo.DeleteAsync(project);
        _logger.LogInformation("Project {ProjectId} deleted", id);
    }

    public async Task<ProjectStatsResponse> GetStatsAsync(Guid id)
    {
        await GetAccessibleProjectAsync(id);

        var (milestoneCount, completedMilestones, deliverableCount, approvedDeliverables) =
            await _repo.GetStatsAsync(id);

        var percent = milestoneCount == 0 ? 0
            : (int)Math.Round(completedMilestones / (double)milestoneCount * 100);

        return new ProjectStatsResponse(
            Math.Clamp(percent, 0, 100),
            milestoneCount, completedMilestones,
            deliverableCount, approvedDeliverables);
    }

    // Returns project if it exists and is accessible to the caller.
    // Clients see only projects where they are the assigned client.
    // Same 404 message for "not found" and "wrong client" — avoids leaking existence.
    private async Task<Project> GetAccessibleProjectAsync(Guid id)
    {
        var project = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Project not found.");

        if (_currentUser.Role == "Client" && project.ClientId != _currentUser.UserId)
            throw new KeyNotFoundException("Project not found.");

        return project;
    }

    private static ProjectResponse ToResponse(Project p)
        => new(p.Id, p.Name, p.Description, p.Status.ToString(),
               p.ClientId, p.Client.Name, p.OrganisationId, p.CreatedAt);
}
