using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FormFeeder.Api.Models;
using FormFeeder.Api.Services;
using Polly;

namespace FormFeeder.Api.Connectors;

internal readonly record struct SlackConfiguration(
    string WebhookUrl,
    string? Channel = null,
    string Username = "FormFeeder",
    string IconEmoji = ":envelope:",
    bool IncludeRawJson = false);

internal readonly record struct SlackField(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("short")] bool Short);

internal readonly record struct SlackAttachment(
    [property: JsonPropertyName("color")] string Color,
    [property: JsonPropertyName("fallback")] string? Fallback = null,
    [property: JsonPropertyName("pretext")] string? Pretext = null,
    [property: JsonPropertyName("title")] string? Title = null,
    [property: JsonPropertyName("fields")] SlackField[]? Fields = null,
    [property: JsonPropertyName("footer")] string? Footer = null,
    [property: JsonPropertyName("ts")] long? Timestamp = null,
    [property: JsonPropertyName("text")] string? Text = null,
    [property: JsonPropertyName("mrkdwn_in")] string[]? MarkdownIn = null);

internal readonly record struct SlackMessage(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("icon_emoji")] string IconEmoji,
    [property: JsonPropertyName("attachments")] SlackAttachment[] Attachments,
    [property: JsonPropertyName("channel")] string? Channel = null);

public sealed class SlackConnector(
    ILogger<SlackConnector> logger,
    IHttpClientFactory httpClientFactory,
    IRetryPolicyFactory retryPolicyFactory,
    string name = "Slack") : IConnector
{
    public string Type => "Slack";
    public string Name { get; } = name ?? "Slack";
    public bool Enabled { get; set; } = true;

    public async Task<ConnectorResult> ExecuteAsync(FormSubmission submission, Dictionary<string, object>? configuration = null)
    {
        try
        {
            var configResult = ParseConfiguration(configuration);
            if (!configResult.IsSuccess)
            {
                return ConnectorResult.Failed(configResult.Error!);
            }

            var config = configResult.Value!;
            var message = BuildSlackMessage(submission, config);
            
            logger.LogDebug("Sending Slack notification for form {FormId} to webhook", submission.FormId);
            
            var result = await SendSlackMessageAsync(config.WebhookUrl, message);
            return result.Match(
                success => CreateSuccessResult(success),
                error => ConnectorResult.Failed(error)
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing Slack connector for form {FormId}", submission.FormId);
            return ConnectorResult.Failed($"Slack connector error: {ex.Message}", ex);
        }
    }

    private static Result<SlackConfiguration> ParseConfiguration(Dictionary<string, object>? configuration)
    {
        if (configuration is null)
        {
            return Result.Failure<SlackConfiguration>("Slack configuration is missing");
        }

        var webhookUrl = configuration.GetValueOrDefault("WebhookUrl")?.ToString();
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return Result.Failure<SlackConfiguration>("Slack webhook URL is missing");
        }

        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri) || 
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Result.Failure<SlackConfiguration>("Invalid Slack webhook URL");
        }

        var channel = configuration.GetValueOrDefault("Channel")?.ToString();
        var username = configuration.GetValueOrDefault("Username")?.ToString() ?? "FormFeeder";
        var iconEmoji = configuration.GetValueOrDefault("IconEmoji")?.ToString() ?? ":envelope:";
        var includeRawJson = configuration.GetValueOrDefault("IncludeRawJson")?.ToString()?.
            Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        return Result.Success(new SlackConfiguration(webhookUrl, channel, username, iconEmoji, includeRawJson));
    }

    private async Task<Result<string>> SendSlackMessageAsync(string webhookUrl, SlackMessage message)
    {
        using var httpClient = httpClientFactory.CreateClient();
        var retryPolicy = retryPolicyFactory.CreateSlackRetryPolicy();
        var json = JsonSerializer.Serialize(message);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        try
        {
            var response = await retryPolicy.ExecuteAsync(async _ => 
                await httpClient.PostAsync(webhookUrl, content));
                
            var responseBody = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Slack notification sent successfully. Response: {Response}", responseBody);
                return Result.Success(responseBody);
            }
            
            logger.LogWarning("Failed to send Slack notification. Status: {StatusCode}, Response: {Response}", 
                response.StatusCode, responseBody);
            
            return Result.Failure<string>($"Failed to send Slack notification. Status: {response.StatusCode}, Error: {responseBody}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while sending Slack notification after retries");
            return Result.Failure<string>($"Failed to send Slack notification: {ex.Message}");
        }
    }

    private static ConnectorResult CreateSuccessResult(string responseBody)
    {
        var metadata = new Dictionary<string, object>
        {
            ["Response"] = responseBody
        };
        
        return ConnectorResult.Successful("Slack notification sent", metadata);
    }

    private static SlackMessage BuildSlackMessage(FormSubmission submission, in SlackConfiguration config)
    {
        var formFields = ExtractFormFields(submission.FormData);
        var metadataFields = CreateMetadataFields(submission);
        
        var attachments = new List<SlackAttachment>
        {
            new(
                Color: "good",
                Fallback: $"New form submission: {submission.FormId}",
                Pretext: "New form submission received",
                Title: $"Form: {submission.FormId}",
                Fields: formFields,
                Footer: "FormFeeder",
                Timestamp: new DateTimeOffset(submission.SubmittedAt).ToUnixTimeSeconds()
            ),
            new(
                Color: "#e0e0e0",
                Fields: metadataFields
            )
        };

        if (config.IncludeRawJson)
        {
            attachments.Add(new SlackAttachment(
                Color: "#f0f0f0",
                Title: "Raw JSON Data",
                Text: $"```{submission.FormData.RootElement}```",
                MarkdownIn: ["text"]
            ));
        }

        return new SlackMessage(
            Username: config.Username,
            IconEmoji: config.IconEmoji,
            Attachments: [.. attachments],
            Channel: config.Channel
        );
    }

    private static SlackField[] ExtractFormFields(JsonDocument formData)
    {
        if (formData.RootElement.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var fields = new List<SlackField>();
        
        foreach (var property in formData.RootElement.EnumerateObject())
        {
            var value = property.Value.ValueKind == JsonValueKind.String 
                ? property.Value.GetString() 
                : property.Value.ToString();
                
            var truncatedValue = TruncateValue(value);
            
            fields.Add(new SlackField(
                Title: FormatFieldName(property.Name),
                Value: truncatedValue ?? "N/A",
                Short: (truncatedValue?.Length ?? 0) < 40
            ));
        }

        return [.. fields];
    }

    private static SlackField[] CreateMetadataFields(FormSubmission submission) =>
    [
        new(
            Title: "Submitted At",
            Value: submission.SubmittedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            Short: true
        ),
        new(
            Title: "IP Address",
            Value: submission.ClientIp ?? "Unknown",
            Short: true
        ),
        new(
            Title: "User Agent",
            Value: TruncateUserAgent(submission.UserAgent),
            Short: false
        )
    ];

    private static string? TruncateValue(string? value)
    {
        if (value is null) return null;
        return value.Length > 100 ? $"{value[..97]}..." : value;
    }

    private static string FormatFieldName(string fieldName) =>
        string.Join(" ", SplitCamelCase(fieldName.Replace("_", " ").Replace("-", " "))
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .Select(word => char.ToUpper(word[0]) + word[1..].ToLower()));

    private static IEnumerable<string> SplitCamelCase(string input)
    {
        var currentWord = new StringBuilder();
        
        foreach (var ch in input)
        {
            if (char.IsUpper(ch) && currentWord.Length > 0)
            {
                yield return currentWord.ToString();
                currentWord.Clear();
            }
            currentWord.Append(ch);
        }
        
        if (currentWord.Length > 0)
        {
            yield return currentWord.ToString();
        }
    }

    private static string TruncateUserAgent(string? userAgent) =>
        userAgent switch
        {
            null or "" => "Unknown",
            { Length: <= 100 } => userAgent,
            _ => $"{userAgent[..97]}..."
        };
}


