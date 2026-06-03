using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlowDesk.API.Hubs;

[Authorize]
public class ProjectHub : Hub
{
    public async Task JoinProject(string projectId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"proj-{projectId}");

    public async Task LeaveProject(string projectId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"proj-{projectId}");
}
