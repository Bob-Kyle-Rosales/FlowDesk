using FlowDesk.Core.DTOs.Deliverables;
using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowDesk.API.Controllers;

[ApiController]
[Authorize]
public class DeliverablesController : ControllerBase
{
    private readonly IDeliverableService _service;

    public DeliverablesController(IDeliverableService service)
    {
        _service = service;
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

    [HttpPost("api/deliverables/{id:guid}/upload-url")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<UploadUrlResponse>> GetUploadUrl(
        Guid id, [FromBody] GetUploadUrlRequest request)
        => Ok(await _service.GetUploadUrlAsync(id, request.FileName, request.ContentType));

    [HttpPatch("api/deliverables/{id:guid}")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<DeliverableResponse>> ConfirmUpload(
        Guid id, [FromBody] ConfirmUploadRequest request)
        => Ok(await _service.ConfirmUploadAsync(id, request.FileUrl));

    [HttpPatch("api/deliverables/{id:guid}/approve")]
    [Authorize(Policy = "ClientOnly")]
    public async Task<ActionResult<DeliverableResponse>> Approve(Guid id)
        => Ok(await _service.ApproveAsync(id));

    [HttpPatch("api/deliverables/{id:guid}/revision")]
    [Authorize(Policy = "ClientOnly")]
    public async Task<ActionResult<DeliverableResponse>> RequestRevision(
        Guid id, [FromBody] RevisionRequest request)
        => Ok(await _service.RequestRevisionAsync(id, request));
}
