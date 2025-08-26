using FormFeeder.Api.Models;
using FormFeeder.Api.Models.DTOs;
using FormFeeder.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FormFeeder.Api.Endpoints;

public static class FormEndpoints
{
    public static void MapFormEndpoints(this IEndpointRouteBuilder app)
    {
        var formApi = app.MapGroup("/v1/form")
            .WithTags("Form Submissions");

        formApi.MapPost("/{formId}", SubmitForm)
            .DisableAntiforgery()
            .RequireRateLimiting(RateLimitingService.PerFormPolicy)
            .Produces<FormSubmissionResponse>(StatusCodes.Status200OK)
            .Produces<FormSubmissionResponse>(StatusCodes.Status400BadRequest)
            .Produces<FormSubmissionResponse>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status429TooManyRequests)
            .WithName("SubmitForm")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Submit a form";
                operation.Description = "Accepts form submissions with automatic client info extraction. Validates form ID and origin domain. Rate limited per form and IP.";
                return operation;
            });
    }

    private static async Task<Results<Ok<FormSubmissionResponse>, BadRequest<FormSubmissionResponse>, NotFound<FormSubmissionResponse>, UnauthorizedHttpResult>> SubmitForm(
        string formId,
        HttpRequest request,
        FormSubmissionService submissionService,
        IFormConfigurationService formConfigService,
        IConnectorService connectorService,
        IFormValidationService validationService,
        IFormDataExtractionService dataExtractionService,
        IBackgroundTaskQueue backgroundTaskQueue,
        ILogger<Program> logger)
    {
        try
        {
            var validationResult = await ValidateFormSubmission(formId, request, validationService);
            if (validationResult is not null)
                return validationResult;

            var submissionResult = await ProcessFormSubmission(
                formId, request, submissionService, dataExtractionService);
            if (!submissionResult.IsSuccess)
            {
                return CreateErrorResponse(formId, submissionResult.Error ?? "Unknown error");
            }

            var response = submissionResult.Value!;
            await QueueConnectorExecution(formId, response, formConfigService, connectorService, backgroundTaskQueue, logger);
            
            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing form submission for form {FormId}", formId);
            return TypedResults.BadRequest(new FormSubmissionResponse(
                Guid.Empty, formId, DateTime.UtcNow, false, 
                "An error occurred while processing your submission"));
        }
    }

    private static async Task<Results<Ok<FormSubmissionResponse>, BadRequest<FormSubmissionResponse>, NotFound<FormSubmissionResponse>, UnauthorizedHttpResult>?> ValidateFormSubmission(
        string formId,
        HttpRequest request,
        IFormValidationService validationService)
    {
        var validationResult = await validationService.ValidateFormAndOriginAsync(formId, request);
        if (validationResult.IsSuccess)
            return null;

        var errorMessage = validationResult.Error ?? "Form validation failed";
        return errorMessage switch
        {
            var error when error.Contains("does not exist") => TypedResults.NotFound(new FormSubmissionResponse(
                Guid.Empty, formId, DateTime.UtcNow, false, errorMessage)),
            var error when error.Contains("disabled") => TypedResults.BadRequest(new FormSubmissionResponse(
                Guid.Empty, formId, DateTime.UtcNow, false, errorMessage)),
            _ => TypedResults.Unauthorized()
        };
    }

    private static async Task<Result<FormSubmissionResponse>> ProcessFormSubmission(
        string formId,
        HttpRequest request,
        FormSubmissionService submissionService,
        IFormDataExtractionService dataExtractionService)
    {
        var formData = await dataExtractionService.ExtractFormDataAsync(request);
        var clientInfo = dataExtractionService.ExtractClientInfo(request);

        return await submissionService.SaveSubmissionAsync(
            formId,
            formData,
            clientInfo.IpAddress,
            clientInfo.UserAgent,
            clientInfo.Referer,
            clientInfo.ContentType);
    }

    private static BadRequest<FormSubmissionResponse> CreateErrorResponse(string formId, string error)
    {
        return TypedResults.BadRequest(new FormSubmissionResponse(
            Guid.Empty, formId, DateTime.UtcNow, false, error));
    }

    private static async Task QueueConnectorExecution(
        string formId,
        FormSubmissionResponse response,
        IFormConfigurationService formConfigService,
        IConnectorService connectorService,
        IBackgroundTaskQueue backgroundTaskQueue,
        ILogger<Program> logger)
    {
        if (response.Submission is null)
            return;

        var formConfig = await formConfigService.GetFormConfigurationAsync(formId).ConfigureAwait(false);
        if (formConfig?.Connectors is null || formConfig.Connectors.Count == 0)
            return;

        await backgroundTaskQueue.QueueBackgroundWorkItemAsync(async token =>
        {
            try
            {
                if (formConfig.PrivacyMode)
                {
                    logger.LogInformation("Executing connectors for privacy mode form {FormId} - submission not persisted", formId);
                }
                
                await connectorService.ExecuteConnectorsAsync(response.Submission, formConfig.Connectors).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing connectors for form {FormId}", formId);
            }
        }).ConfigureAwait(false);
    }
}