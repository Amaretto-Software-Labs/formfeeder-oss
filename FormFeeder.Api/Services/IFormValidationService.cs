using FormFeeder.Api.Models;

namespace FormFeeder.Api.Services;

public interface IFormValidationService
{
    Task<Result<string>> ValidateFormAndOriginAsync(string formId, HttpRequest request);
}

public sealed class FormValidationService(
    IFormConfigurationService formConfigService,
    ILogger<FormValidationService> logger) : IFormValidationService
{
    public async Task<Result<string>> ValidateFormAndOriginAsync(string formId, HttpRequest request)
    {
        var formValidationResult = await ValidateFormExistsAndEnabled(formId);
        if (!formValidationResult.IsSuccess)
        {
            return Result.Failure<string>(formValidationResult.Error!);
        }

        var formConfig = formValidationResult.Value!;
        var originValidationResult = await ValidateOriginDomain(formId, request, formConfig);

        return originValidationResult.IsSuccess
            ? Result.Success(formId)
            : originValidationResult;
    }

    private async Task<Result<FormConfiguration>> ValidateFormExistsAndEnabled(string formId)
    {
        var formConfig = await formConfigService.GetFormConfigurationAsync(formId).ConfigureAwait(false);
        if (formConfig is null)
        {
            logger.LogWarning("Attempted submission to non-existent form: {FormId}", formId);
            return Result.Failure<FormConfiguration>($"Form '{formId}' does not exist.");
        }

        if (!formConfig.Enabled)
        {
            logger.LogWarning("Attempted submission to disabled form: {FormId}", formId);
            return Result.Failure<FormConfiguration>($"Form '{formId}' is currently disabled.");
        }

        // Validate privacy mode configuration
        if (!formConfig.IsPrivacyModeValid())
        {
            logger.LogWarning(
                "Form '{FormId}' has invalid privacy mode configuration: " +
                "privacy mode is enabled but no connectors are enabled", formId);
            return Result.Failure<FormConfiguration>(
                "Privacy mode requires at least one enabled connector");
        }

        return Result.Success(formConfig);
    }

    private async Task<Result<string>> ValidateOriginDomain(string formId, HttpRequest request, FormConfiguration formConfig)
    {
        var origin = ExtractOriginFromRequest(request);

        if (IsNullOrLocalOrigin(origin))
        {
            return ValidateLocalOrigin(formId, formConfig);
        }

        return await ValidateRemoteOrigin(formId, origin!, formConfig);
    }

    private static string? ExtractOriginFromRequest(HttpRequest request)
    {
        return request.Headers.Origin.FirstOrDefault() ?? request.Headers.Referer.FirstOrDefault();
    }

    private static bool IsNullOrLocalOrigin(string? origin)
    {
        return origin == "null" || string.IsNullOrEmpty(origin);
    }

    private Result<string> ValidateLocalOrigin(string formId, FormConfiguration formConfig)
    {
        if (!formConfig.AllowedDomains.Contains("*"))
        {
            logger.LogWarning("Submission from local file (Origin: null) not allowed for form: {FormId}", formId);
            return Result.Failure<string>("Origin not allowed for this form");
        }

        return Result.Success(formId);
    }

    private async Task<Result<string>> ValidateRemoteOrigin(string formId, string origin, FormConfiguration formConfig)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            logger.LogWarning("Invalid origin/referer URI format: {Origin} for form: {FormId}", origin, formId);
            return ValidateInvalidOriginFormat(formConfig);
        }

        var domain = $"{uri.Scheme}://{uri.Host}";
        var isAllowed = await formConfigService.IsDomainAllowedForFormAsync(formId, domain).ConfigureAwait(false);

        if (!isAllowed)
        {
            logger.LogWarning("Attempted submission from unauthorized domain: {Domain} to form: {FormId}", domain, formId);
            return Result.Failure<string>("Domain not allowed for this form");
        }

        return Result.Success(formId);
    }

    private static Result<string> ValidateInvalidOriginFormat(FormConfiguration formConfig)
    {
        return formConfig.AllowedDomains.Contains("*")
            ? Result.Success(string.Empty)
            : Result.Failure<string>("Invalid origin format and wildcards not allowed");
    }
}
