// FlowDesk.Core/Interfaces/IStripeService.cs
namespace FlowDesk.Core.Interfaces;

public interface IStripeService
{
    string BuildConnectUrl(Guid organisationId, string callbackUri);
    Task<string> ExchangeCodeForAccountIdAsync(string code);
    Task<(string ClientSecret, string PaymentIntentId)> CreatePaymentIntentAsync(
        decimal total, string destinationAccountId);
    bool TryConstructWebhookEvent(
        string payload, string signature, out object stripeEvent);
}
