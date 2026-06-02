using FlowDesk.Core.DTOs.Messages;
using FlowDesk.Core.Entities;
using FlowDesk.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlowDesk.Core.Services;

public class MessageService : IMessageService
{
    private readonly IMessageRepository _repo;
    private readonly IProjectRepository _projectRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<MessageService> _logger;

    public MessageService(
        IMessageRepository repo,
        IProjectRepository projectRepo,
        ICurrentUserService currentUser,
        ILogger<MessageService> logger)
    {
        _repo = repo;
        _projectRepo = projectRepo;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IEnumerable<MessageResponse>> GetAllAsync(Guid projectId)
    {
        _ = await _projectRepo.GetByIdAsync(projectId)
            ?? throw new KeyNotFoundException($"Project {projectId} not found.");
        var messages = await _repo.GetByProjectAsync(projectId);
        return messages.Select(ToResponse);
    }

    public async Task<MessageResponse> CreateAsync(Guid projectId, CreateMessageRequest request)
    {
        _ = await _projectRepo.GetByIdAsync(projectId)
            ?? throw new KeyNotFoundException($"Project {projectId} not found.");

        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = request.Content,
            ProjectId = projectId,
            SenderId = _currentUser.UserId!.Value,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(message);
        _logger.LogInformation("Message {MessageId} sent in project {ProjectId}", created.Id, projectId);
        return ToResponse(created);
    }

    public async Task MarkReadAsync(Guid projectId)
        => await _repo.MarkReadAsync(projectId, _currentUser.UserId!.Value);

    private static MessageResponse ToResponse(Message m) => new(
        m.Id,
        m.Content,
        m.SenderId,
        m.Sender?.Name ?? string.Empty,
        m.IsRead,
        m.CreatedAt);
}
