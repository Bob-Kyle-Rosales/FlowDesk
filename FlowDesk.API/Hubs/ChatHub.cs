using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FlowDesk.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageRepository _repo;
    private readonly IProjectRepository _projectRepo;

    public ChatHub(IMessageRepository repo, IProjectRepository projectRepo)
    {
        _repo = repo;
        _projectRepo = projectRepo;
    }

    public async Task JoinProject(string projectId)
    {
        if (!Guid.TryParse(projectId, out var projectGuid) ||
            !Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new HubException("Invalid request.");

        // IProjectRepository.GetByIdAsync filters by org (EF global query filter) and client ownership
        var project = await _projectRepo.GetByIdAsync(projectGuid);
        if (project is null)
            throw new HubException("Access denied.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"project-{projectId}");
        await _repo.MarkReadAsync(projectGuid, userId);
    }

    public async Task LeaveProject(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project-{projectId}");
    }
}
