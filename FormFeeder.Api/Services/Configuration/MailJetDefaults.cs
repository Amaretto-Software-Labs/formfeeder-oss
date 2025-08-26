namespace FormFeeder.Api.Services.Configuration;

/// <summary>
/// Configuration class for MailJet default settings used in auto-generated private forms.
/// </summary>
public sealed class MailJetDefaults
{
    public const string ConfigurationSectionName = "MailJetDefaults";
    
    public required string ApiKey { get; set; }
    public required string ApiSecret { get; set; }
    public required string FromEmail { get; set; }
    public required string FromName { get; set; }
    public required string Subject { get; set; }
    public required string TemplateId { get; set; }
}