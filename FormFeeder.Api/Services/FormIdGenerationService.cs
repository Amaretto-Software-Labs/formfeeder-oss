using System.Security.Cryptography;
using System.Text;

namespace FormFeeder.Api.Services;

/// <summary>
/// Service for generating cryptographically secure form IDs for private forms.
/// </summary>
public sealed class FormIdGenerationService : IFormIdGenerationService
{
    private const string Prefix = "prv-";
    private const int HashLength = 7; // Results in 10+ total characters with prefix
    private const string CharSet = "abcdefghijklmnopqrstuvwxyz0123456789";

    public string GeneratePrivateFormId()
    {
        var randomBytes = new byte[HashLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var result = new StringBuilder(Prefix, Prefix.Length + HashLength);
        
        foreach (var b in randomBytes)
        {
            result.Append(CharSet[b % CharSet.Length]);
        }

        return result.ToString();
    }
}