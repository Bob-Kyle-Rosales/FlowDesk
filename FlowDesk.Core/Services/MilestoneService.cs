using FlowDesk.Core.DTOs.Milestones;
using FlowDesk.Core.Entities;
using FlowDesk.Core.Enums;
using FlowDesk.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlowDesk.Core.Services;

public class MilestoneService : IMilestoneService
{
    private readonly IMilestoneRepository _repo;
    private readonly IProjectRepository _projectRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<MilestoneService> _logger;

    public MilestoneService(
        IMilestoneRepository repo,
        IProjectRepository projectRepo,
        ICurrentUserService currentUser,
        ILogger<MilestoneService> logger)
    {
        _repo = repo;
        _projectRepo = projectRepo;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IEnumerable<MilestoneResponse>> GetAllByProjectAsync(Guid projectId)
    {
        await RequireProjectAccessAsync(projectId);
        return (await _repo.GetAllByProjectAsync(projectId)).Select(ToResponse);
    }

    public async Task<MilestoneResponse> CreateAsync(Guid projectId, CreateMilestoneRequest request)
    {
        await RequireProjectAccessAsync(projectId);

        var milestone = new Milestone
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = request.Title,
            Description = request.Description,
            Order = request.Order,
            DueDate = request.DueDate
        };

        await _repo.CreateAsync(milestone);
        _logger.LogInformation("Milestone {MilestoneId} created in project {ProjectId}", milestone.Id, projectId);
        return ToResponse(milestone);
    }

    public async Task<MilestoneResponse> UpdateAsync(Guid id, UpdateMilestoneRequest request)
    {
        var milestone = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Milestone not found.");

        milestone.Title = request.Title;
        milestone.Description = request.Description;
        milestone.Order = request.Order;
        milestone.DueDate = request.DueDate;

        await _repo.UpdateAsync(milestone);
        _logger.LogInformation("Milestone {MilestoneId} updated", id);
        return ToResponse(milestone);
    }

    public async Task DeleteAsync(Guid id)
    {
        var milestone = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Milestone not found.");
        await _repo.DeleteAsync(milestone);
        _logger.LogInformation("Milestone {MilestoneId} deleted", id);
    }

    public async Task<MilestoneResponse> UpdateStatusAsync(Guid id, UpdateMilestoneStatusRequest request)
    {
        var milestone = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Milestone not found.");

        if (!Enum.TryParse<MilestoneStatus>(request.Status, out var status))
            throw new InvalidOperationException($"Invalid status: {request.Status}.");

        milestone.Status = status;
        await _repo.UpdateAsync(milestone);
        _logger.LogInformation("Milestone {MilestoneId} status updated to {Status}", id, status);
        return ToResponse(milestone);
    }

    // Verifies the project exists, belongs to the caller's org, and (for clients) is their project.
    private async Task RequireProjectAccessAsync(Guid projectId)
    {
        var project = await _projectRepo.GetByIdAsync(projectId)
            ?? throw new KeyNotFoundException("Project not found.");

        if (_currentUser.Role == "Client" && project.ClientId != _currentUser.UserId)
            throw new KeyNotFoundException("Project not found.");
    }

    private static MilestoneResponse ToResponse(Milestone m)
        => new(m.Id, m.ProjectId, m.Title, m.Description, m.Status.ToString(), m.Order, m.DueDate);
}
