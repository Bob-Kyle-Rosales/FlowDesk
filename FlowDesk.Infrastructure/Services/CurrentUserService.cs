using System.Security.Claims;
using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FlowDesk.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            return claim is not null ? Guid.Parse(claim.Value) : null;
        }
    }

    public Guid? OrganisationId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst("org");
            return claim is not null ? Guid.Parse(claim.Value) : null;
        }
    }

    public string? Role =>
        _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
}
