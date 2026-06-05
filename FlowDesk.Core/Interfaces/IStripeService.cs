namespace FlowDesk.Core.Interfaces;

public interface IStripeService
{
    string BuildConnectUrl(Guid organisationId, string callbackUri);
    Task<string> ExchangeCodeForAccountIdAsync(string code);
}
