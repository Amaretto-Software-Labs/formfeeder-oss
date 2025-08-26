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
    private readonly IFormIdGenerationService formIdGenerator;
    private readonly MailJetDefaults mailJetDefaults;
    private readonly ILogger<PrivateFormGenerationService> logger;

    public PrivateFormGenerationService(
        IFormIdGenerationService formIdGenerator,
        IOptions<MailJetDefaults> mailJetDefaults,
        ILogger<PrivateFormGenerationService> logger)
    {
        this.formIdGenerator = formIdGenerator;
        this.mailJetDefaults = mailJetDefaults.Value;
        this.logger = logger;
    }

    public Task<FormConfiguration> GeneratePrivateFormAsync(string recipientEmail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);

        var formId = formIdGenerator.GeneratePrivateFormId();

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
                WindowMinutes = 1,
            },
            Connectors =
            [
                new ConnectorConfiguration(
                    Type: "MailJet",
                    Name: "PrivateFormNotification",
                    Enabled: true,
                    Settings: new Dictionary<string, object>
                    {
                        ["ApiKey"] = mailJetDefaults.ApiKey,
                        ["ApiSecret"] = mailJetDefaults.ApiSecret,
                        ["FromEmail"] = mailJetDefaults.FromEmail,
                        ["FromName"] = mailJetDefaults.FromName,
                        ["ToEmail"] = recipientEmail,
                        ["ToName"] = "Recipient",
                        ["Subject"] = mailJetDefaults.Subject,
                        ["TemplateId"] = mailJetDefaults.TemplateId
                    })
            ],
        };

        logger.LogInformation(
            "Generated private form configuration {FormId} for recipient {RecipientEmail}",
            formId, recipientEmail);

        return Task.FromResult(formConfiguration);
    }
}
