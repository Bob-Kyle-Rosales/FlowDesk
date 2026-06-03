using FlowDesk.API.Hubs;
using FlowDesk.Core.DTOs.Milestones;
using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FlowDesk.API.Controllers;

[ApiController]
[Authorize]
public class MilestonesController : ControllerBase
{
    private readonly IMilestoneService _service;
    private readonly IHubContext<ProjectHub> _hub;
    private readonly ILogger<MilestonesController> _logger;

    public MilestonesController(
        IMilestoneService service,
        IHubContext<ProjectHub> hub,
        ILogger<MilestonesController> logger)
    {
        _service = service;
        _hub = hub;
        _logger = logger;
    }

    [HttpGet("api/projects/{projectId:guid}/milestones")]
    public async Task<ActionResult<IEnumerable<MilestoneResponse>>> GetAll(Guid projectId)
        => Ok(await _service.GetAllByProjectAsync(projectId));

    [HttpPost("api/projects/{projectId:guid}/milestones")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<MilestoneResponse>> Create(
        Guid projectId, [FromBody] CreateMilestoneRequest request)
    {
        var result = await _service.CreateAsync(projectId, request);
        return StatusCode(201, result);
    }

    [HttpPut("api/milestones/{id:guid}")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<MilestoneResponse>> Update(
        Guid id, [FromBody] UpdateMilestoneRequest request)
        => Ok(await _service.UpdateAsync(id, request));

    [HttpDelete("api/milestones/{id:guid}")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPatch("api/milestones/{id:guid}/status")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<MilestoneResponse>> UpdateStatus(
        Guid id, [FromBody] UpdateMilestoneStatusRequest request)
    {
        var result = await _service.UpdateStatusAsync(id, request);
        try
        {
            await _hub.Clients.Group($"proj-{result.ProjectId}")
                .SendAsync("OnMilestoneUpdated", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProjectHub broadcast failed for project {ProjectId}", result.ProjectId);
        }
        return Ok(result);
    }
}
