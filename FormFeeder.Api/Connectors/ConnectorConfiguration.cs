namespace FormFeeder.Api.Connectors;

public record ConnectorConfiguration(
    string Type,
    string Name,
    bool Enabled = true,
    Dictionary<string, object>? Settings = null)
{
    public Dictionary<string, object> Settings { get; init; } = Settings ?? [];
}