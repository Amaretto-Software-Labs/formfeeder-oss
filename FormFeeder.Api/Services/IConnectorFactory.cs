using FormFeeder.Api.Connectors;

namespace FormFeeder.Api.Services;

public interface IConnectorFactory
{
    IConnector? CreateConnector(string connectorType, string name);

    bool IsConnectorTypeSupported(string connectorType);
}

public sealed class ConnectorFactory(IServiceScopeFactory serviceScopeFactory, ILogger<ConnectorFactory> logger) : IConnectorFactory
{
    private static readonly Dictionary<string, Type> ConnectorTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "MailJet", typeof(MailJetConnector) },
        { "Slack", typeof(SlackConnector) },
    };

    public IConnector? CreateConnector(string connectorType, string name)
    {
        if (string.IsNullOrWhiteSpace(connectorType) || !ConnectorTypes.TryGetValue(connectorType, out var type))
        {
            logger.LogWarning("Unknown connector type: {ConnectorType}", connectorType);
            return null;
        }

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var loggerType = typeof(ILogger<>).MakeGenericType(type);
            var connectorLogger = serviceProvider.GetService(loggerType);

            // Handle different connector types with different constructor signatures
            if (type == typeof(SlackConnector))
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var retryPolicyFactory = serviceProvider.GetRequiredService<IRetryPolicyFactory>();
                var constructor = type.GetConstructor([loggerType, typeof(IHttpClientFactory), typeof(IRetryPolicyFactory), typeof(string)]);
                if (constructor is null)
                {
                    logger.LogError("No suitable constructor found for Slack connector");
                    return null;
                }

                return constructor.Invoke([connectorLogger, httpClientFactory, retryPolicyFactory, name]) as IConnector;
            }
            else if (type == typeof(MailJetConnector))
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var emailTemplateService = serviceProvider.GetRequiredService<IEmailTemplateService>();
                var retryPolicyFactory = serviceProvider.GetRequiredService<IRetryPolicyFactory>();
                var constructor = type.GetConstructor([loggerType, typeof(IConfiguration), typeof(IEmailTemplateService), typeof(IRetryPolicyFactory), typeof(string)]);
                if (constructor is null)
                {
                    logger.LogError("No suitable constructor found for MailJet connector");
                    return null;
                }

                return constructor.Invoke([connectorLogger, configuration, emailTemplateService, retryPolicyFactory, name]) as IConnector;
            }
            else
            {
                logger.LogError("Unknown connector type in factory: {ConnectorType}", type.Name);
                return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating connector of type {ConnectorType}", type.Name);
            return null;
        }
    }

    public bool IsConnectorTypeSupported(string connectorType) =>
        !string.IsNullOrWhiteSpace(connectorType) && ConnectorTypes.ContainsKey(connectorType);
}
