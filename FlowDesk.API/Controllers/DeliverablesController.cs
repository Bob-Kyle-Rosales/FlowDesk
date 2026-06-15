using FlowDesk.API.Hubs;
using FlowDesk.Core.DTOs.Deliverables;
using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FlowDesk.API.Controllers;

[ApiController]
[Authorize]
public class DeliverablesController : ControllerBase
{
    private readonly IDeliverableService _service;
    private readonly IHubContext<ProjectHub> _hub;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<DeliverablesController> _logger;

    public DeliverablesController(
        IDeliverableService service,
        IHubContext<ProjectHub> hub,
        IFileStorageService fileStorage,
        ILogger<DeliverablesController> logger)
    {
        _service = service;
        _hub = hub;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    [HttpGet("api/projects/{projectId:guid}/deliverables")]
    public async Task<ActionResult<IEnumerable<DeliverableResponse>>> GetAll(Guid projectId)
        => Ok(await _service.GetAllByProjectAsync(projectId));

    [HttpPost("api/projects/{projectId:guid}/deliverables")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<DeliverableResponse>> Create(
        Guid projectId, [FromBody] CreateDeliverableRequest request)
    {
        var result = await _service.CreateAsync(projectId, request);
        return StatusCode(201, result);
    }

    [HttpPut("api/deliverables/{id:guid}")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<DeliverableResponse>> Update(
        Guid id, [FromBody] UpdateDeliverableRequest request)
        => Ok(await _service.UpdateAsync(id, request));

    [HttpPost("api/deliverables/{id:guid}/upload")]
    [Authorize(Policy = "AgencyOnly")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<ActionResult<DeliverableResponse>> Upload(Guid id, IFormFile file)
    {
        if (file.Length == 0) return BadRequest("File is empty.");
        using var stream = file.OpenReadStream();
        var fileUrl = await _fileStorage.UploadAsync(
            $"deliverables/{id}", file.FileName, file.ContentType, stream);
        var result = await _service.ConfirmUploadAsync(id, fileUrl);
        await BroadcastDeliverableUpdated(result);
        return Ok(result);
    }

    [HttpPatch("api/deliverables/{id:guid}")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<DeliverableResponse>> ConfirmUpload(
        Guid id, [FromBody] ConfirmUploadRequest request)
    {
        var result = await _service.ConfirmUploadAsync(id, request.FileUrl);
        await BroadcastDeliverableUpdated(result);
        return Ok(result);
    }

    [HttpPatch("api/deliverables/{id:guid}/approve")]
    [Authorize(Policy = "ClientOnly")]
    public async Task<ActionResult<DeliverableResponse>> Approve(Guid id)
    {
        var result = await _service.ApproveAsync(id);
        await BroadcastDeliverableUpdated(result);
        return Ok(result);
    }

    [HttpPatch("api/deliverables/{id:guid}/revision")]
    [Authorize(Policy = "ClientOnly")]
    public async Task<ActionResult<DeliverableResponse>> RequestRevision(
        Guid id, [FromBody] RevisionRequest request)
    {
        var result = await _service.RequestRevisionAsync(id, request);
        await BroadcastDeliverableUpdated(result);
        return Ok(result);
    }

    private async Task BroadcastDeliverableUpdated(DeliverableResponse result)
    {
        try
        {
            await _hub.Clients.Group($"proj-{result.ProjectId}")
                .SendAsync("OnDeliverableUpdated", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProjectHub broadcast failed for project {ProjectId}", result.ProjectId);
        }
    }
}
