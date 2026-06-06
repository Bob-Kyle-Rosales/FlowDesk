using FlowDesk.Core.DTOs.Projects;
using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowDesk.API.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _service;
    private readonly IAIReportService _reportService;

    public ProjectsController(IProjectService service, IAIReportService reportService)
    {
        _service = service;
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectResponse>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpPost]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<ProjectResponse>> Create([FromBody] CreateProjectRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> GetById(Guid id)
        => Ok(await _service.GetByIdAsync(id));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<ProjectResponse>> Update(
        Guid id, [FromBody] UpdateProjectRequest request)
        => Ok(await _service.UpdateAsync(id, request));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id:guid}/stats")]
    public async Task<ActionResult<ProjectStatsResponse>> GetStats(Guid id)
        => Ok(await _service.GetStatsAsync(id));

    [HttpPost("{id:guid}/report")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task StreamReport(Guid id, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        await foreach (var token in _reportService.StreamReportAsync(id, ct))
        {
            if (ct.IsCancellationRequested) break;
            await Response.WriteAsync($"data: {token}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}
