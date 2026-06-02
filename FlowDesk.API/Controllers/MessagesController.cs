using FlowDesk.API.Hubs;
using FlowDesk.Core.DTOs.Messages;
using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FlowDesk.API.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/messages")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _service;
    private readonly IHubContext<ChatHub> _hub;

    public MessagesController(IMessageService service, IHubContext<ChatHub> hub)
    {
        _service = service;
        _hub = hub;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MessageResponse>>> GetAll(Guid projectId)
        => Ok(await _service.GetAllAsync(projectId));

    [HttpPost]
    public async Task<ActionResult<MessageResponse>> Send(
        Guid projectId, [FromBody] CreateMessageRequest request)
    {
        var message = await _service.CreateAsync(projectId, request);
        await _hub.Clients.Group($"project-{projectId}").SendAsync("ReceiveMessage", message);
        return CreatedAtAction(nameof(GetAll), new { projectId }, message);
    }

    [HttpPatch("read")]
    public async Task<IActionResult> MarkRead(Guid projectId)
    {
        await _service.MarkReadAsync(projectId);
        return NoContent();
    }
}
