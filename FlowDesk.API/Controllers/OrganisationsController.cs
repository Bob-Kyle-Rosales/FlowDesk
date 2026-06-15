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
    private readonly IFileStorageService _fileStorage;

    public OrganisationsController(IOrganisationService service, IFileStorageService fileStorage)
    {
        _service = service;
        _fileStorage = fileStorage;
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

    [Authorize(Policy = "AgencyOwnerOnly")]
    [HttpPost("me/upload-logo")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<OrganisationResponse>> UploadLogo(IFormFile file)
    {
        if (file.Length == 0) return BadRequest("File is empty.");
        using var stream = file.OpenReadStream();
        var fileUrl = await _fileStorage.UploadAsync("logos", file.FileName, file.ContentType, stream);
        return Ok(await _service.UpdateLogoAsync(new UpdateLogoRequest(fileUrl)));
    }

    [AllowAnonymous]
    [HttpGet("public/{slug}")]
    public async Task<ActionResult<PublicOrganisationResponse>> GetPublic(string slug)
        => Ok(await _service.GetPublicAsync(slug));
}
