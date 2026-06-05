using FlowDesk.Core.DTOs.Stripe;
using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowDesk.API.Controllers;

[ApiController]
[Route("api/stripe")]
public class StripeController : ControllerBase
{
    private readonly IStripeService _stripe;
    private readonly IOrganisationRepository _orgRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _config;
    private readonly ILogger<StripeController> _logger;

    public StripeController(
        IStripeService stripe,
        IOrganisationRepository orgRepo,
        ICurrentUserService currentUser,
        IConfiguration config,
        ILogger<StripeController> logger)
    {
        _stripe = stripe;
        _orgRepo = orgRepo;
        _currentUser = currentUser;
        _config = config;
        _logger = logger;
    }

    [HttpGet("connect-url")]
    [Authorize(Policy = "AgencyOwnerOnly")]
    public ActionResult<ConnectUrlResponse> GetConnectUrl()
    {
        var callbackUri = $"{Request.Scheme}://{Request.Host}/api/stripe/connect/callback";
        var url = _stripe.BuildConnectUrl(_currentUser.OrganisationId!.Value, callbackUri);
        return Ok(new ConnectUrlResponse(url));
    }

    [HttpGet("connect/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error = null)
    {
        var frontendUrl = _config["FRONTEND_URL"] ?? "http://localhost:3000";
        var settingsUrl = $"{frontendUrl}/dashboard/settings";

        if (error != null)
        {
            _logger.LogWarning("Stripe Connect denied by user: {Error}", error);
            return Redirect($"{settingsUrl}?stripe_error=denied");
        }

        if (string.IsNullOrEmpty(code) || !Guid.TryParse(state, out var orgId))
        {
            _logger.LogWarning("Stripe Connect callback received invalid parameters");
            return Redirect($"{settingsUrl}?stripe_error=invalid");
        }

        try
        {
            var stripeAccountId = await _stripe.ExchangeCodeForAccountIdAsync(code);

            var org = await _orgRepo.GetByIdAsync(orgId)
                ?? throw new KeyNotFoundException($"Organisation {orgId} not found.");

            org.StripeAccountId = stripeAccountId;
            await _orgRepo.UpdateAsync(org);

            _logger.LogInformation("Stripe Connect linked account {AccountId} to org {OrgId}",
                stripeAccountId, orgId);

            return Redirect($"{settingsUrl}?stripe=connected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe Connect exchange failed for org {OrgId}", orgId);
            return Redirect($"{settingsUrl}?stripe_error=failed");
        }
    }
}
