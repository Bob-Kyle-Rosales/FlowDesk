using FlowDesk.Core.DTOs.Deliverables;
using FlowDesk.Core.Entities;
using FlowDesk.Core.Enums;
using FlowDesk.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlowDesk.Core.Services;

public class DeliverableService : IDeliverableService
{
    private readonly IDeliverableRepository _repo;
    private readonly IProjectRepository _projectRepo;
    private readonly IFileStorageService _fileStorage;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DeliverableService> _logger;

    public DeliverableService(
        IDeliverableRepository repo,
        IProjectRepository projectRepo,
        IFileStorageService fileStorage,
        ICurrentUserService currentUser,
        ILogger<DeliverableService> logger)
    {
        _repo = repo;
        _projectRepo = projectRepo;
        _fileStorage = fileStorage;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IEnumerable<DeliverableResponse>> GetAllByProjectAsync(Guid projectId)
    {
        await RequireProjectAccessAsync(projectId);
        return (await _repo.GetAllByProjectAsync(projectId)).Select(ToResponse);
    }

    public async Task<DeliverableResponse> CreateAsync(Guid projectId, CreateDeliverableRequest request)
    {
        await RequireProjectAccessAsync(projectId);

        var deliverable = new Deliverable
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            MilestoneId = request.MilestoneId,
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(deliverable);
        _logger.LogInformation("Deliverable {DeliverableId} created in project {ProjectId}", deliverable.Id, projectId);
        return ToResponse(deliverable);
    }

    public async Task<DeliverableResponse> UpdateAsync(Guid id, UpdateDeliverableRequest request)
    {
        var deliverable = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Deliverable not found.");

        deliverable.Name = request.Name;
        deliverable.Description = request.Description;
        deliverable.MilestoneId = request.MilestoneId;

        await _repo.UpdateAsync(deliverable);
        _logger.LogInformation("Deliverable {DeliverableId} updated", id);
        return ToResponse(deliverable);
    }

    public async Task<UploadUrlResponse> GetUploadUrlAsync(Guid id, string fileName, string contentType)
    {
        var deliverable = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Deliverable not found.");

        var (uploadUrl, fileUrl) = await _fileStorage.GenerateUploadUrlAsync(
            deliverable.Id, fileName, contentType);

        return new UploadUrlResponse(uploadUrl, fileUrl);
    }

    public async Task<DeliverableResponse> ConfirmUploadAsync(Guid id, string fileUrl)
    {
        var deliverable = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Deliverable not found.");

        deliverable.FileUrl = fileUrl;

        // First upload moves status from Pending → UnderReview so clients can act on it.
        // Subsequent re-uploads (after a revision) keep the existing status.
        if (deliverable.Status == DeliverableStatus.Pending)
            deliverable.Status = DeliverableStatus.UnderReview;

        await _repo.UpdateAsync(deliverable);
        _logger.LogInformation("Deliverable {DeliverableId} file confirmed, status {Status}", id, deliverable.Status);
        return ToResponse(deliverable);
    }

    public async Task<DeliverableResponse> ApproveAsync(Guid id)
    {
        var deliverable = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Deliverable not found.");

        if (deliverable.Status != DeliverableStatus.UnderReview)
            throw new InvalidOperationException("Only deliverables under review can be approved.");

        deliverable.Status = DeliverableStatus.Approved;
        deliverable.ApprovedById = _currentUser.UserId;
        deliverable.ApprovedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(deliverable);
        _logger.LogInformation("Deliverable {DeliverableId} approved by {UserId}", id, _currentUser.UserId);
        return ToResponse(deliverable);
    }

    public async Task<DeliverableResponse> RequestRevisionAsync(Guid id, RevisionRequest request)
    {
        var deliverable = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Deliverable not found.");

        if (deliverable.Status != DeliverableStatus.UnderReview)
            throw new InvalidOperationException("Only deliverables under review can have a revision requested.");

        deliverable.Status = DeliverableStatus.Revision;
        deliverable.RevisionNotes = request.Notes;
        deliverable.Version++;

        await _repo.UpdateAsync(deliverable);
        _logger.LogInformation("Deliverable {DeliverableId} revision requested", id);
        return ToResponse(deliverable);
    }

    private async Task RequireProjectAccessAsync(Guid projectId)
    {
        var project = await _projectRepo.GetByIdAsync(projectId)
            ?? throw new KeyNotFoundException("Project not found.");

        if (_currentUser.Role == UserRole.Client.ToString() && project.ClientId != _currentUser.UserId)
            throw new KeyNotFoundException("Project not found.");
    }

    private static DeliverableResponse ToResponse(Deliverable d)
        => new(d.Id, d.ProjectId, d.MilestoneId, d.Name, d.Description,
               d.Status.ToString(), d.FileUrl, d.Version, d.RevisionNotes,
               d.ApprovedAt, d.CreatedAt);
}
