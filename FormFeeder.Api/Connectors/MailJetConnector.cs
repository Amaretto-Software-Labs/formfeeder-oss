using FormFeeder.Api.Models;
using FormFeeder.Api.Services;

using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using Mailjet.Client.TransactionalEmails.Response;

namespace FormFeeder.Api.Connectors;

#pragma warning disable CS9113
public sealed class MailJetConnector(
    ILogger<MailJetConnector> logger,
    IConfiguration configuration,
    IEmailTemplateService emailTemplateService,
    IRetryPolicyFactory retryPolicyFactory,
    string name = "MailJet") : IConnector
#pragma warning restore CS9113
{
    public string Type => "MailJet";

    public string Name { get; private set; } = name ?? "MailJet";

    public bool Enabled { get; set; } = true;

    public async Task<ConnectorResult> ExecuteAsync(FormSubmission submission, Dictionary<string, object>? configuration = null)
    {
        try
        {
            var validationResult = ValidateConfiguration(configuration);
            if (!validationResult.IsSuccess)
            {
                return ConnectorResult.Failed(validationResult.Error!);
            }

            var emailSettings = ExtractEmailSettings(configuration!, submission);

            // Debug logging to see what values we're using
            logger.LogDebug(
                "MailJet Settings - FromEmail: {FromEmail}, ToEmail: {ToEmail}, TemplateId: {TemplateId}, ApiKey: {ApiKey}",
                emailSettings.FromEmail, emailSettings.ToEmail, emailSettings.TemplateId, emailSettings.ApiKey?.Substring(0, Math.Min(5, emailSettings.ApiKey.Length)) + "...");

            var client = new MailjetClient(emailSettings.ApiKey, emailSettings.ApiSecret);
            var retryPolicy = retryPolicyFactory.CreateMailJetRetryPolicy();

            TransactionalEmailResponse result;
            if (!string.IsNullOrEmpty(emailSettings.TemplateId))
            {
                var email = BuildTemplateEmail(emailSettings, submission);
                result = await retryPolicy.ExecuteAsync(async _ =>
                    await client.SendTransactionalEmailAsync(email).ConfigureAwait(false));
            }
            else
            {
                var emailContent = emailTemplateService.GenerateEmailContent(submission);
                var email = BuildContentEmail(emailSettings, emailContent);
                result = await retryPolicy.ExecuteAsync(async _ =>
                    await client.SendTransactionalEmailAsync(email).ConfigureAwait(false));
            }

            return ProcessTransactionalResponse(result, submission.FormId ?? string.Empty, emailSettings.ToEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing MailJet connector for form {FormId}", submission.FormId);
            return ConnectorResult.Failed($"MailJet connector error: {ex.Message}", ex);
        }
    }

    private static Result<bool> ValidateConfiguration(Dictionary<string, object>? configuration)
    {
        if (configuration == null)
        {
            return Result.Failure<bool>("MailJet configuration is missing");
        }

        var apiKey = configuration.GetValueOrDefault("ApiKey")?.ToString();
        var apiSecret = configuration.GetValueOrDefault("ApiSecret")?.ToString();
        var fromEmail = configuration.GetValueOrDefault("FromEmail")?.ToString();
        var toEmail = configuration.GetValueOrDefault("ToEmail")?.ToString();

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            return Result.Failure<bool>("MailJet API credentials are missing");
        }

        if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(toEmail))
        {
            return Result.Failure<bool>("From or To email addresses are missing");
        }

        return Result.Success(true);
    }

    private static EmailSettings ExtractEmailSettings(Dictionary<string, object> configuration, FormSubmission submission)
    {
        return new EmailSettings
        {
            ApiKey = configuration.GetValueOrDefault("ApiKey")?.ToString() ?? string.Empty,
            ApiSecret = configuration.GetValueOrDefault("ApiSecret")?.ToString() ?? string.Empty,
            FromEmail = configuration.GetValueOrDefault("FromEmail")?.ToString() ?? string.Empty,
            FromName = configuration.GetValueOrDefault("FromName")?.ToString() ?? "FormFeeder",
            ToEmail = configuration.GetValueOrDefault("ToEmail")?.ToString() ?? string.Empty,
            ToName = configuration.GetValueOrDefault("ToName")?.ToString() ?? "Admin",
            Subject = configuration.GetValueOrDefault("Subject")?.ToString() ?? $"New form submission: {submission.FormId}",
            TemplateId = configuration.GetValueOrDefault("TemplateId")?.ToString(),
        };
    }

    private static TransactionalEmail BuildTemplateEmail(EmailSettings settings, FormSubmission submission)
    {
        var templateId = int.Parse(settings.TemplateId!);

        // Parse form data into individual fields for template iteration
        var formFields = new Dictionary<string, object>();
        if (submission.FormData?.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            foreach (var property in submission.FormData.RootElement.EnumerateObject())
            {
                formFields[property.Name] = property.Value.ToString();
            }
        }

        var variables = new Dictionary<string, object>
        {
            ["formId"] = submission.FormId ?? string.Empty,
            ["submittedAt"] = submission.SubmittedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            ["ipAddress"] = submission.ClientIp ?? "Unknown",
            ["userAgent"] = submission.UserAgent ?? "Unknown",
            ["referer"] = submission.Referer ?? "Unknown",
            ["formData"] = submission.FormData?.RootElement.ToString() ?? "{}",
            ["formFields"] = formFields, // Dictionary for template iteration
        };

        return new TransactionalEmailBuilder()
            .WithFrom(new SendContact(settings.FromEmail, settings.FromName))
            .WithTo(new SendContact(settings.ToEmail, settings.ToName))
            .WithTemplateId(templateId)
            .WithTemplateLanguage(true)
            .WithVariables(variables)
            .Build();
    }

    private static TransactionalEmail BuildContentEmail(EmailSettings settings, EmailContent emailContent)
    {
        return new TransactionalEmailBuilder()
            .WithFrom(new SendContact(settings.FromEmail, settings.FromName))
            .WithTo(new SendContact(settings.ToEmail, settings.ToName))
            .WithSubject(settings.Subject)
            .WithHtmlPart(emailContent.Html)
            .WithTextPart(emailContent.Text)
            .Build();
    }

    private ConnectorResult ProcessTransactionalResponse(TransactionalEmailResponse response, string formId, string toEmail)
    {
        if (response?.Messages == null || response.Messages.Length == 0)
        {
            logger.LogWarning("MailJet response is empty for form {FormId}", formId);
            return ConnectorResult.Failed("Failed to send email via MailJet. No response received.");
        }

        var message = response.Messages[0];

        if (message.Status != "success")
        {
            var errors = message.Errors?.Any() == true
                ? string.Join(", ", message.Errors.Select(e => $"{e.ErrorCode}: {e.ErrorMessage}"))
                : "Unknown error";

            logger.LogWarning(
                "MailJet response indicates failure for form {FormId}. Status: {Status}. Errors: {Errors}",
                formId, message.Status, errors);
            return ConnectorResult.Failed($"Failed to send email via MailJet. Status: {message.Status}. Errors: {errors}");
        }

        var metadata = new Dictionary<string, object>
        {
            ["MessageId"] = message.To?.FirstOrDefault()?.MessageID.ToString() ?? string.Empty,
            ["MessageUUID"] = message.To?.FirstOrDefault()?.MessageUUID?.ToString() ?? string.Empty,
            ["Status"] = message.Status,
        };

        logger.LogInformation(
            "Email sent successfully for form {FormId} to {ToEmail}. MessageId: {MessageId}",
            formId, toEmail, metadata["MessageId"]);
        return ConnectorResult.Successful($"Email sent to {toEmail}", metadata);
    }

    private record EmailSettings
    {
        public required string ApiKey { get; init; }

        public required string ApiSecret { get; init; }

        public required string FromEmail { get; init; }

        public required string FromName { get; init; }

        public required string ToEmail { get; init; }

        public required string ToName { get; init; }

        public required string Subject { get; init; }

        public string? TemplateId { get; init; }
    }
}
