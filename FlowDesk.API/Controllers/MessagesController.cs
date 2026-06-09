using FlowDesk.API.Hubs;
using FlowDesk.Core.DTOs.Deliverables;
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
    private readonly IFileStorageService _fileStorage;
    private readonly IProjectService _projectService;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(
        IMessageService service,
        IHubContext<ChatHub> hub,
        IFileStorageService fileStorage,
        IProjectService projectService,
        ILogger<MessagesController> logger)
    {
        _service = service;
        _hub = hub;
        _fileStorage = fileStorage;
        _projectService = projectService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MessageResponse>>> GetAll(Guid projectId)
        => Ok(await _service.GetAllAsync(projectId));

    [HttpPost]
    public async Task<ActionResult<MessageResponse>> Send(
        Guid projectId, [FromBody] CreateMessageRequest request)
    {
        var message = await _service.CreateAsync(projectId, request);
        try
        {
            await _hub.Clients.Group($"project-{projectId}").SendAsync("ReceiveMessage", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR broadcast failed for project {ProjectId}", projectId);
        }
        return CreatedAtAction(nameof(GetAll), new { projectId }, message);
    }

    [HttpPost("upload-url")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<UploadUrlResponse>> GetUploadUrl(
        Guid projectId, [FromBody] GetMessageUploadUrlRequest request)
    {
        var project = await _projectService.GetByIdAsync(projectId);
        if (project is null) return NotFound();

        var (uploadUrl, fileUrl) = await _fileStorage.GenerateUploadUrlAsync(
            $"messages/{projectId}/{Guid.NewGuid()}", request.FileName, request.ContentType);
        return Ok(new UploadUrlResponse(uploadUrl, fileUrl));
    }

    [HttpPatch("read")]
    public async Task<IActionResult> MarkRead(Guid projectId)
    {
        await _service.MarkReadAsync(projectId);
        return NoContent();
    }
}
