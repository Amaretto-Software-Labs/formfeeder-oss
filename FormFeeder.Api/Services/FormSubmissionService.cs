using FormFeeder.Api.Data;
using FormFeeder.Api.Models;
using FormFeeder.Api.Models.DTOs;
using System.Text.Json;

namespace FormFeeder.Api.Services;

public sealed class FormSubmissionService(
    AppDbContext context, 
    IFormConfigurationService formConfigService,
    ILogger<FormSubmissionService> logger)
{
    public async Task<Result<FormSubmissionResponse>> SaveSubmissionAsync(
        string formId, 
        Dictionary<string, object> formData,
        string? clientIp,
        string? userAgent,
        string? referer,
        string? contentType)
    {
        try
        {
            // Validate form configuration first
            var formConfig = await formConfigService.GetFormConfigurationAsync(formId).ConfigureAwait(false);
            
            // Form must exist
            if (formConfig is null)
            {
                logger.LogWarning("Form submission rejected - form does not exist: {FormId}", formId);
                return Result.Failure<FormSubmissionResponse>("Form configuration not found");
            }
            
            // Form must be enabled
            if (!formConfig.Enabled)
            {
                logger.LogWarning("Form submission rejected - form is disabled: {FormId}", formId);
                return Result.Failure<FormSubmissionResponse>("Form is currently disabled");
            }
            
            // Form configuration must be valid (privacy mode requires connectors)
            if (!formConfig.IsPrivacyModeValid())
            {
                logger.LogWarning("Form submission rejected - invalid privacy mode configuration: {FormId}", formId);
                return Result.Failure<FormSubmissionResponse>("Invalid form configuration");
            }

            // Convert Dictionary to JsonDocument
            var jsonString = JsonSerializer.Serialize(formData);
            var jsonDocument = JsonDocument.Parse(jsonString);

            var submission = new FormSubmission
            {
                Id = Guid.NewGuid(),
                FormId = formId,
                FormData = jsonDocument,
                ClientIp = clientIp,
                UserAgent = userAgent,
                Referer = referer,
                ContentType = contentType,
                SubmittedAt = DateTime.UtcNow
            };

            // Privacy mode means no database persistence
            if (formConfig.PrivacyMode)
            {
                logger.LogInformation("Privacy mode active - form submission not persisted: {FormId} - {SubmissionId}", 
                    formId, submission.Id);
            }
            else
            {
                context.FormSubmissions.Add(submission);
                await context.SaveChangesAsync().ConfigureAwait(false);
                logger.LogInformation("Form submission saved: {FormId} - {SubmissionId}", formId, submission.Id);
            }

            var response = new FormSubmissionResponse(
                submission.Id,
                submission.FormId,
                submission.SubmittedAt,
                true,
                "Form submitted successfully",
                submission);
                
            return Result.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing form submission for form {FormId}", formId);
            return Result.Failure<FormSubmissionResponse>("An error occurred while processing your submission", ex);
        }
    }
}