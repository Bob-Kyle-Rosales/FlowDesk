using System.Net.Http.Json;
using FlowDesk.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace FlowDesk.Infrastructure.Services;

public class StripeService : IStripeService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public StripeService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public string BuildConnectUrl(Guid organisationId, string callbackUri)
    {
        var clientId = _config["STRIPE_CLIENT_ID"]
            ?? throw new InvalidOperationException("STRIPE_CLIENT_ID is not set.");

        return "https://connect.stripe.com/oauth/authorize" +
               $"?response_type=code" +
               $"&client_id={Uri.EscapeDataString(clientId)}" +
               $"&scope=read_write" +
               $"&redirect_uri={Uri.EscapeDataString(callbackUri)}" +
               $"&state={organisationId}";
    }

    public async Task<string> ExchangeCodeForAccountIdAsync(string code)
    {
        var secretKey = _config["STRIPE_SECRET_KEY"]
            ?? throw new InvalidOperationException("STRIPE_SECRET_KEY is not set.");

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", secretKey);

        var body = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
        });

        var response = await client.PostAsync(
            "https://connect.stripe.com/oauth/token", body);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Stripe OAuth token exchange failed ({(int)response.StatusCode}): {errorBody}");
        }

        var token = await response.Content.ReadFromJsonAsync<OAuthToken>()
            ?? throw new InvalidOperationException("Failed to deserialize Stripe OAuth token response.");

        return token.StripeUserId
            ?? throw new InvalidOperationException("Stripe did not return a StripeUserId.");
    }
}
