using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FlowDesk.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace FlowDesk.Infrastructure.Services;

public class StripeService : IStripeService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StripeService> _logger;

    public StripeService(
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ILogger<StripeService> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
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

        var token = await response.Content.ReadFromJsonAsync<StripeOAuthTokenResponse>()
            ?? throw new InvalidOperationException("Failed to deserialize Stripe OAuth token response.");

        return token.StripeUserId
            ?? throw new InvalidOperationException("Stripe did not return a StripeUserId.");
    }

    public async Task<(string ClientSecret, string PaymentIntentId)> CreatePaymentIntentAsync(
        decimal total, string destinationAccountId)
    {
        var apiKey = _config["STRIPE_SECRET_KEY"]
            ?? throw new InvalidOperationException("STRIPE_SECRET_KEY is not set.");

        var amountInCents = (long)Math.Round(total * 100, MidpointRounding.AwayFromZero);

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInCents,
            Currency = "usd",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
            TransferData = new PaymentIntentTransferDataOptions
            {
                Destination = destinationAccountId,
            },
        };

        var requestOptions = new RequestOptions { ApiKey = apiKey };
        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options, requestOptions);

        return (intent.ClientSecret!, intent.Id);
    }

    public bool TryConstructWebhookEvent(
        string payload, string signature, out object? stripeEvent)
    {
        var secret = _config["STRIPE_WEBHOOK_SECRET"]
            ?? throw new InvalidOperationException("STRIPE_WEBHOOK_SECRET is not set.");

        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, secret);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed");
            stripeEvent = null;
            return false;
        }
    }

    private record StripeOAuthTokenResponse(
        [property: JsonPropertyName("stripe_user_id")] string? StripeUserId);
}
