using FormFeeder.Api.Connectors;
using FormFeeder.Api.Models;

namespace FormFeeder.Api.Services;

public interface IConnectorService
{
    Task ExecuteConnectorsAsync(FormSubmission submission, List<ConnectorConfiguration>? connectorConfigs);
}

public sealed class ConnectorService(
    ILogger<ConnectorService> logger,
    IConnectorFactory connectorFactory) : IConnectorService
{
    public async Task ExecuteConnectorsAsync(FormSubmission submission, List<ConnectorConfiguration>? connectorConfigs)
    {
        if (connectorConfigs == null || !connectorConfigs.Any())
        {
            logger.LogDebug("No connectors configured for form {FormId}", submission.FormId);
            return;
        }

        var enabledConnectors = connectorConfigs.Where(c => c.Enabled).ToList();

        if (!enabledConnectors.Any())
        {
            logger.LogDebug("No connectors configured for form {FormId}", submission.FormId);
            return;
        }

        List<Task> tasks = [];

        foreach (var config in enabledConnectors)
        {
            tasks.Add(ExecuteConnectorAsync(submission, config));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ExecuteConnectorAsync(FormSubmission submission, ConnectorConfiguration config)
    {
        try
        {
            logger.LogInformation(
                "Executing {ConnectorType} connector '{ConnectorName}' for form {FormId}",
                config.Type, config.Name, submission.FormId);

            if (!connectorFactory.IsConnectorTypeSupported(config.Type))
            {
                logger.LogWarning("Unknown connector type: {ConnectorType}", config.Type);
                return;
            }

            var connector = connectorFactory.CreateConnector(config.Type, config.Name);
            if (connector is null)
            {
                logger.LogError("Failed to create connector of type {ConnectorType}", config.Type);
                return;
            }

            connector.Enabled = config.Enabled;

            var result = await connector.ExecuteAsync(submission, config.Settings).ConfigureAwait(false);

            if (result.Success)
            {
                logger.LogInformation(
                    "Connector {ConnectorName} executed successfully: {Message}",
                    config.Name, result.Message);
            }
            else
            {
                logger.LogWarning(
                    "Connector {ConnectorName} failed: {Message}",
                    config.Name, result.Message);

                if (result.Error is not null)
                {
                    logger.LogError(result.Error, "Connector {ConnectorName} error details", config.Name);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error executing connector {ConnectorName} for form {FormId}",
                config.Name, submission.FormId);
        }
    }
}
