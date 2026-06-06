using FlowDesk.Core.DTOs.Auth;
using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowDesk.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IAuthService authService, ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (tokens, user) = await _authService.RegisterAsync(request);
        SetTokenCookies(tokens);
        return StatusCode(201, user);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (tokens, user) = await _authService.LoginAsync(request);
        SetTokenCookies(tokens);
        return Ok(user);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        var (tokens, user) = await _authService.RefreshAsync(refreshToken);
        SetTokenCookies(tokens);
        return Ok(user);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refresh_token"];
        if (!string.IsNullOrEmpty(refreshToken))
            await _authService.LogoutAsync(refreshToken);

        DeleteTokenCookies();
        return NoContent();
    }

    [Authorize(Policy = "AgencyOwnerOnly")]
    [HttpPost("invite")]
    public async Task<IActionResult> Invite([FromBody] InviteRequest request)
    {
        var orgId = _currentUserService.OrganisationId!.Value;
        var inviteLink = await _authService.InviteAsync(request, orgId);
        return Ok(new { inviteLink });
    }

    [HttpPost("accept-invite")]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request)
    {
        var (tokens, user) = await _authService.AcceptInviteAsync(request);
        SetTokenCookies(tokens);
        return StatusCode(201, user);
    }

    private void SetTokenCookies(TokenPair tokens)
    {
        var isProd = !HttpContext.RequestServices
            .GetRequiredService<IHostEnvironment>()
            .IsDevelopment();

        Response.Cookies.Append("access_token", tokens.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProd,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(15)
        });

        Response.Cookies.Append("refresh_token", tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProd,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    private void DeleteTokenCookies()
    {
        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("refresh_token");
    }
}
