using FlowDesk.Core.DTOs.Users;
using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowDesk.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "AgencyOnly")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserSummaryResponse>>> GetUsers([FromQuery] string? role)
    {
        if (!string.Equals(role, "Client", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only role=Client is supported.");

        var users = await _userRepository.GetClientUsersAsync();
        return Ok(users.Select(u => new UserSummaryResponse(u.Id, u.Name, u.Email)));
    }
}
