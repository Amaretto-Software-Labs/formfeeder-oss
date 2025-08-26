using FormFeeder.Api.Models;

namespace FormFeeder.Api.Services;

/// <summary>
/// Service responsible for generating private form configurations from email addresses.
/// </summary>
public interface IPrivateFormGenerationService
{
    /// <summary>
    /// Generates a new private form configuration for the specified recipient email.
    /// </summary>
    /// <param name="recipientEmail">The email address to send form submissions to</param>
    /// <returns>A form configuration with privacy mode enabled and MailJet connector configured</returns>
    Task<FormConfiguration> GeneratePrivateFormAsync(string recipientEmail);
}