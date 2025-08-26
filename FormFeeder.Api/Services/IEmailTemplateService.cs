using System.Text.Json;

using FormFeeder.Api.Models;

namespace FormFeeder.Api.Services;

public sealed record EmailContent(string Html, string Text);

public interface IEmailTemplateService
{
    EmailContent GenerateEmailContent(FormSubmission submission);
}

public sealed class EmailTemplateService : IEmailTemplateService
{
    public EmailContent GenerateEmailContent(FormSubmission submission)
    {
        var html = GenerateHtmlContent(submission);
        var text = GenerateTextContent(submission);

        return new EmailContent(html, text);
    }

    private static string GenerateHtmlContent(FormSubmission submission)
    {
        var styles = GenerateEmailStyles();
        var header = GenerateHtmlHeader(submission.FormId);
        var metadata = GenerateHtmlMetadata(submission);
        var formData = GenerateHtmlFormDataSection(submission.FormData);

        return $@"<!DOCTYPE html>
<html>
<head>
    {styles}
</head>
<body>
    <div class='container'>
        {header}
        {metadata}
        {formData}
    </div>
</body>
</html>";
    }

    private static string GenerateEmailStyles()
    {
        return @"<style>
        body { font-family: Arial, sans-serif; line-height: 1.6; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #f4f4f4; padding: 10px; margin-bottom: 20px; }
        .metadata { background: #f9f9f9; padding: 10px; margin-bottom: 20px; }
        .form-data { background: #fff; padding: 10px; border: 1px solid #ddd; }
        .field { margin-bottom: 10px; }
        .label { font-weight: bold; color: #333; }
        .value { color: #666; margin-left: 10px; }
    </style>";
    }

    private static string GenerateHtmlHeader(string formId)
    {
        return $@"<div class='header'>
            <h2>New Form Submission: {formId}</h2>
        </div>";
    }

    private static string GenerateHtmlMetadata(FormSubmission submission)
    {
        return $@"<div class='metadata'>
            <h3>Submission Details</h3>
            <div class='field'>
                <span class='label'>Submitted At:</span>
                <span class='value'>{submission.SubmittedAt:yyyy-MM-dd HH:mm:ss} UTC</span>
            </div>
            <div class='field'>
                <span class='label'>IP Address:</span>
                <span class='value'>{submission.ClientIp ?? "Unknown"}</span>
            </div>
            <div class='field'>
                <span class='label'>User Agent:</span>
                <span class='value'>{submission.UserAgent ?? "Unknown"}</span>
            </div>
            <div class='field'>
                <span class='label'>Referer:</span>
                <span class='value'>{submission.Referer ?? "Unknown"}</span>
            </div>
        </div>";
    }

    private static string GenerateHtmlFormDataSection(JsonDocument? formData)
    {
        return $@"<div class='form-data'>
            <h3>Form Data</h3>
            {GenerateFormDataHtml(formData)}
        </div>";
    }

    private static string GenerateFormDataHtml(JsonDocument? formData)
    {
        if (formData is null)
        {
            return "<p>No form data submitted</p>";
        }

        var html = string.Empty;
        foreach (var property in formData.RootElement.EnumerateObject())
        {
            html += $@"
                <div class='field'>
                    <span class='label'>{property.Name}:</span>
                    <span class='value'>{property.Value}</span>
                </div>";
        }

        return string.IsNullOrEmpty(html) ? "<p>No form data submitted</p>" : html;
    }

    private static string GenerateTextContent(FormSubmission submission)
    {
        var text = $@"
New Form Submission: {submission.FormId}
========================================

Submission Details:
- Submitted At: {submission.SubmittedAt:yyyy-MM-dd HH:mm:ss} UTC
- IP Address: {submission.ClientIp ?? "Unknown"}
- User Agent: {submission.UserAgent ?? "Unknown"}
- Referer: {submission.Referer ?? "Unknown"}

Form Data:
{GenerateFormDataText(submission.FormData)}";

        return text;
    }

    private static string GenerateFormDataText(JsonDocument? formData)
    {
        if (formData is null)
        {
            return "No form data submitted";
        }

        var text = string.Empty;
        foreach (var property in formData.RootElement.EnumerateObject())
        {
            text += $"- {property.Name}: {property.Value}\n";
        }

        return string.IsNullOrEmpty(text) ? "No form data submitted" : text;
    }
}
