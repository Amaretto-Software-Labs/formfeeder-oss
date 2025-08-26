using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using FormFeeder.Api.Services;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FormFeeder.Api.Endpoints;

/// <summary>
/// Endpoints for managing private form auto-generation.
/// </summary>
public static class PrivateFormEndpoints
{
    public static void MapPrivateFormEndpoints(this IEndpointRouteBuilder app)
    {
        var formsApi = app.MapGroup("/v1/forms")
            .WithTags("Private Form Generation");

        formsApi.MapPost("/from-email", CreatePrivateFormFromEmail)
            .DisableAntiforgery()
            .Produces<CreatePrivateFormResponse>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithName("CreatePrivateFormFromEmail")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Auto-generate a private form configuration";
                operation.Description = "Creates a new private form configuration with MailJet connector for the provided email address. Supports both JSON and form-data content types.";
                return operation;
            });
    }

    private static async Task<Results<Ok<CreatePrivateFormResponse>, ValidationProblem, StatusCodeHttpResult>> CreatePrivateFormFromEmail(
        HttpRequest request,
        IPrivateFormGenerationService formGenerationService,
        IFormConfigurationManagementService formConfigManagementService,
        ILogger<Program> logger)
    {
        try
        {
            var emailResult = await ExtractEmailFromRequestAsync(request);
            if (!emailResult.IsValid)
            {
                return TypedResults.ValidationProblem(emailResult.ValidationErrors!);
            }

            var email = emailResult.Email!;
            logger.LogInformation("Creating private form for email: {Email}", email);

            // Generate form configuration
            var formConfig = await formGenerationService.GeneratePrivateFormAsync(email);

            // Save the configuration
            var savedConfig = await formConfigManagementService.CreateFormConfigurationAsync(formConfig);

            logger.LogInformation(
                "Successfully created private form {FormId} for email: {Email}",
                savedConfig.FormId, email);

            return TypedResults.Ok(new CreatePrivateFormResponse(savedConfig.FormId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating private form from email");
            return TypedResults.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<EmailExtractionResult> ExtractEmailFromRequestAsync(HttpRequest request)
    {
        try
        {
            string? email = null;

            // Handle JSON content type
            if (request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) == true)
            {
                var body = await new StreamReader(request.Body).ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    return EmailExtractionResult.Invalid("Request body is empty");
                }

                var jsonDoc = JsonDocument.Parse(body);
                if (jsonDoc.RootElement.TryGetProperty("email", out var emailElement))
                {
                    email = emailElement.GetString();
                }
            }

            // Handle form data content type
            else if (request.ContentType?.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (request.Form.TryGetValue("email", out var emailValues))
                {
                    email = emailValues.FirstOrDefault();
                }
            }
            else
            {
                return EmailExtractionResult.Invalid("Content-Type must be application/json or application/x-www-form-urlencoded");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return EmailExtractionResult.Invalid("Email address is required");
            }

            // Basic email validation
            if (!IsValidEmailFormat(email))
            {
                return EmailExtractionResult.Invalid("Invalid email format");
            }

            return EmailExtractionResult.Valid(email);
        }
        catch (JsonException)
        {
            return EmailExtractionResult.Invalid("Invalid JSON format");
        }
        catch (Exception ex)
        {
            return EmailExtractionResult.Invalid($"Error processing request: {ex.Message}");
        }
    }

    private static bool IsValidEmailFormat(string email)
    {
        var emailAttribute = new EmailAddressAttribute();
        return emailAttribute.IsValid(email);
    }

    private record EmailExtractionResult(bool IsValid, string? Email, Dictionary<string, string[]>? ValidationErrors)
    {
        public static EmailExtractionResult Valid(string email) =>
            new(true, email, null);

        public static EmailExtractionResult Invalid(string error) =>
            new(false, null, new Dictionary<string, string[]> { ["email"] = [error] });
    }
}

/// <summary>
/// Response for private form creation endpoint.
/// </summary>
public sealed record CreatePrivateFormResponse(string FormId);
