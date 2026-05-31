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
}
