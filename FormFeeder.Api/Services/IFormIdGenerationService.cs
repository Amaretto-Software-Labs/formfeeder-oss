namespace FormFeeder.Api.Services;

/// <summary>
/// Service responsible for generating unique form IDs with cryptographic randomness.
/// </summary>
public interface IFormIdGenerationService
{
    /// <summary>
    /// Generates a unique form ID with the "prv-" prefix for private forms.
    /// </summary>
    /// <returns>A cryptographically random form ID (minimum 10 characters total).</returns>
    string GeneratePrivateFormId();
}
