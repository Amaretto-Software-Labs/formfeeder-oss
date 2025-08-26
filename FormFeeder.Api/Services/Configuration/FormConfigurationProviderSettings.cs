namespace FormFeeder.Api.Services.Configuration;

public sealed record FormConfigurationProviderSettings
{
    public const string SectionName = "FormConfigurationProvider";

    /// <summary>
    /// Gets the provider type to use for form configuration storage.
    /// Valid values: "AppSettings", "Database".
    /// </summary>
    public string Type { get; init; } = "AppSettings";

    /// <summary>
    /// Gets a value indicating whether whether to seed database with configurations from appsettings on startup.
    /// Only applies when Type is "Database".
    /// </summary>
    public bool SeedFromAppSettings { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether whether to overwrite existing database configurations when seeding.
    /// Only applies when SeedFromAppSettings is true.
    /// </summary>
    public bool OverwriteExistingOnSeed { get; init; } = false;
}

public static class FormConfigurationProviderType
{
    public const string AppSettings = "AppSettings";
    public const string Database = "Database";
}
