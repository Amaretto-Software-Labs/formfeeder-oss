using FormFeeder.Api.Models;

namespace FormFeeder.Api.Connectors;

public interface IConnector
{
    string Type { get; }
    string Name { get; }
    bool Enabled { get; set; }
    Task<ConnectorResult> ExecuteAsync(FormSubmission submission, Dictionary<string, object>? configuration = null);
}

public sealed record ConnectorResult(
    bool Success,
    string? Message = null,
    Dictionary<string, object>? Metadata = null,
    Exception? Error = null)
{
    public static ConnectorResult Successful(string message = "Connector executed successfully", Dictionary<string, object>? metadata = null)
        => new(true, message, metadata);
    
    public static ConnectorResult Failed(string message, Exception? error = null)
        => new(false, message, Error: error);
}