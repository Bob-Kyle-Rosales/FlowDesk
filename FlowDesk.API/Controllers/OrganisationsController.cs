using FlowDesk.Core.DTOs.Deliverables;
using FlowDesk.Core.DTOs.Organisations;
using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowDesk.API.Controllers;

[ApiController]
[Route("api/organisations")]
[Authorize]
public class OrganisationsController : ControllerBase
{
    private readonly IOrganisationService _service;

    public OrganisationsController(IOrganisationService service)
    {
        _service = service;
    }

    [Authorize(Policy = "AgencyOnly")]
    [HttpGet("me")]
    public async Task<ActionResult<OrganisationResponse>> GetMine()
        => Ok(await _service.GetMineAsync());

    [Authorize(Policy = "AgencyOwnerOnly")]
    [HttpPut("me")]
    public async Task<ActionResult<OrganisationResponse>> UpdateMine(
        [FromBody] UpdateOrganisationRequest request)
        => Ok(await _service.UpdateMineAsync(request));

    [Authorize(Policy = "AgencyOnly")]
    [HttpPost("me/logo-upload-url")]
    public async Task<ActionResult<UploadUrlResponse>> GetLogoUploadUrl(
        [FromBody] GetLogoUploadUrlRequest request)
    {
        var (uploadUrl, fileUrl) = await _service.GetLogoUploadUrlAsync(
            request.FileName, request.ContentType);
        return Ok(new UploadUrlResponse(uploadUrl, fileUrl));
    }

    [Authorize(Policy = "AgencyOwnerOnly")]
    [HttpPatch("me/logo")]
    public async Task<ActionResult<OrganisationResponse>> UpdateLogo(
        [FromBody] UpdateLogoRequest request)
        => Ok(await _service.UpdateLogoAsync(request));

    [AllowAnonymous]
    [HttpGet("public/{slug}")]
    public async Task<ActionResult<PublicOrganisationResponse>> GetPublic(string slug)
        => Ok(await _service.GetPublicAsync(slug));
}
