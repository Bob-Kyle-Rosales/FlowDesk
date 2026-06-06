using FlowDesk.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace FlowDesk.Infrastructure.Services;

public class SendGridEmailService : IEmailService
{
    private readonly string? _apiKey;
    private readonly string? _fromEmail;
    private readonly string _fromName;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IConfiguration config, ILogger<SendGridEmailService> logger)
    {
        _apiKey = config["SENDGRID_API_KEY"];
        _fromEmail = config["SENDGRID_FROM_EMAIL"];
        _fromName = config["SENDGRID_FROM_NAME"] ?? "FlowDesk";
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_fromEmail))
        {
            _logger.LogWarning(
                "Email to {Email} ({Subject}) skipped — SENDGRID_API_KEY or SENDGRID_FROM_EMAIL not configured",
                toEmail, subject);
            return;
        }

        var client = new SendGridClient(_apiKey);
        var from = new EmailAddress(_fromEmail, _fromName);
        var to = new EmailAddress(toEmail, toName);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlBody);

        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError(
                "SendGrid returned {Status} sending to {Email}: {Body}",
                (int)response.StatusCode, toEmail, body);
        }
        else
        {
            _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
    }
}
