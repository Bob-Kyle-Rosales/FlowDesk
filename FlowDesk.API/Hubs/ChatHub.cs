using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FlowDesk.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageRepository _repo;

    public ChatHub(IMessageRepository repo) => _repo = repo;

    public async Task JoinProject(string projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project-{projectId}");

        var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(projectId, out var projectGuid) &&
            Guid.TryParse(userIdStr, out var userId))
        {
            await _repo.MarkReadAsync(projectGuid, userId);
        }
    }

    public async Task LeaveProject(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project-{projectId}");
    }
}
