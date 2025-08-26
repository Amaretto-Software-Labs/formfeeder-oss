using FormFeeder.Api.Models;
using FormFeeder.Api.Models.DTOs;
using FormFeeder.Api.Connectors;
using System.Text.Json;

namespace FormFeeder.Api.Tests.Infrastructure;

public static class TestDataBuilder
{
    public static FormSubmission CreateFormSubmission(
        string? formId = null,
        Dictionary<string, object>? formData = null,
        DateTime? submittedAt = null,
        string? clientIp = "192.168.1.1",
        string? userAgent = "Mozilla/5.0", 
        string? referer = "https://example.com")
    {
        var data = formData ?? new Dictionary<string, object>
        {
            ["name"] = "John Doe",
            ["email"] = "john@example.com",
            ["message"] = "Test message"
        };

        var jsonString = JsonSerializer.Serialize(data);
        var jsonDocument = JsonDocument.Parse(jsonString);

        return new FormSubmission
        {
            Id = Guid.NewGuid(),
            FormId = formId ?? "test-form",
            FormData = jsonDocument,
            SubmittedAt = submittedAt ?? DateTime.UtcNow,
            ClientIp = clientIp,
            UserAgent = userAgent,
            Referer = referer
        };
    }

    public static FormConfiguration CreateFormConfiguration(
        string? formId = null,
        List<string>? allowedDomains = null,
        bool enabled = true,
        RateLimitSettings? rateLimit = null,
        List<ConnectorConfiguration>? connectors = null)
    {
        return new FormConfiguration
        {
            FormId = formId ?? "test-form",
            AllowedDomains = allowedDomains ?? ["example.com"],
            Enabled = enabled,
            Description = "Test form configuration",
            RateLimit = rateLimit ?? new RateLimitSettings { RequestsPerWindow = 10, WindowMinutes = 1 },
            Connectors = connectors
        };
    }

    public static ConnectorConfiguration CreateConnectorConfiguration(
        string? type = null,
        string? name = null,
        bool enabled = true,
        Dictionary<string, object>? settings = null)
    {
        return new ConnectorConfiguration(
            type ?? "MailJet",
            name ?? "test-connector",
            enabled,
            settings ?? new Dictionary<string, object>
            {
                ["ApiKey"] = "test-api-key",
                ["ApiSecret"] = "test-api-secret",
                ["FromEmail"] = "test@example.com",
                ["ToEmail"] = "recipient@example.com"
            }
        );
    }

    public static FormSubmissionResponse CreateFormSubmissionResponse(
        Guid? id = null,
        string? formId = null,
        DateTime? submittedAt = null,
        bool success = true,
        string? message = null,
        FormSubmission? submission = null)
    {
        return new FormSubmissionResponse(
            id ?? Guid.NewGuid(),
            formId ?? "test-form",
            submittedAt ?? DateTime.UtcNow,
            success,
            message ?? "Success",
            submission ?? CreateFormSubmission()
        );
    }
}