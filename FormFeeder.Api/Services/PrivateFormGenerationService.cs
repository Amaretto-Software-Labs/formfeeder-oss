using FormFeeder.Api.Connectors;
using FormFeeder.Api.Models;
using FormFeeder.Api.Services.Configuration;
using Microsoft.Extensions.Options;

namespace FormFeeder.Api.Services;

/// <summary>
/// Service for generating private form configurations with MailJet connector.
/// </summary>
public sealed class PrivateFormGenerationService : IPrivateFormGenerationService
{
    private readonly IFormIdGenerationService _formIdGenerator;
    private readonly MailJetDefaults _mailJetDefaults;
    private readonly ILogger<PrivateFormGenerationService> _logger;

    public PrivateFormGenerationService(
        IFormIdGenerationService formIdGenerator,
        IOptions<MailJetDefaults> mailJetDefaults,
        ILogger<PrivateFormGenerationService> logger)
    {
        _formIdGenerator = formIdGenerator;
        _mailJetDefaults = mailJetDefaults.Value;
        _logger = logger;
    }

    public Task<FormConfiguration> GeneratePrivateFormAsync(string recipientEmail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);

        var formId = _formIdGenerator.GeneratePrivateFormId();
        
        var formConfiguration = new FormConfiguration
        {
            FormId = formId,
            AllowedDomains = ["*"],
            Description = $"Auto-generated private form for {recipientEmail}",
            Enabled = true,
            PrivacyMode = true,
            RateLimit = new RateLimitSettings
            {
                RequestsPerWindow = 100,
                WindowMinutes = 1
            },
            Connectors = 
            [
                new ConnectorConfiguration(
                    Type: "MailJet",
                    Name: "PrivateFormNotification",
                    Enabled: true,
                    Settings: new Dictionary<string, object>
                    {
                        ["ApiKey"] = _mailJetDefaults.ApiKey,
                        ["ApiSecret"] = _mailJetDefaults.ApiSecret,
                        ["FromEmail"] = _mailJetDefaults.FromEmail,
                        ["FromName"] = _mailJetDefaults.FromName,
                        ["ToEmail"] = recipientEmail,
                        ["ToName"] = "Recipient",
                        ["Subject"] = _mailJetDefaults.Subject,
                        ["TemplateId"] = _mailJetDefaults.TemplateId
                    }
                )
            ]
        };

        _logger.LogInformation("Generated private form configuration {FormId} for recipient {RecipientEmail}", 
            formId, recipientEmail);

        return Task.FromResult(formConfiguration);
    }
}