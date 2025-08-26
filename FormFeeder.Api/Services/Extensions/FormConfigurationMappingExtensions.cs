using FormFeeder.Api.Connectors;
using FormFeeder.Api.Models;
using FormFeeder.Api.Models.Entities;
using System.Text.Json;

namespace FormFeeder.Api.Services.Extensions;

internal static class FormConfigurationMappingExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static FormConfiguration ToFormConfiguration(this FormConfigurationEntity entity)
    {
        var config = new FormConfiguration
        {
            FormId = entity.FormId,
            Description = entity.Description,
            Enabled = entity.Enabled,
            PrivacyMode = entity.PrivacyMode,
            AllowedDomains = entity.AllowedDomains.Select(d => d.Domain).ToList()
        };

        // Map rate limit settings
        if (entity.RateLimit is not null)
        {
            config.RateLimit = new RateLimitSettings
            {
                RequestsPerWindow = entity.RateLimit.RequestsPerWindow,
                WindowMinutes = entity.RateLimit.WindowMinutes
            };
        }

        // Map connector configurations
        if (entity.Connectors.Count > 0)
        {
            config.Connectors = entity.Connectors
                .Select(c => c.ToConnectorConfiguration())
                .ToList();
        }

        return config;
    }

    public static FormConfigurationEntity ToFormConfigurationEntity(this FormConfiguration config)
    {
        var entity = new FormConfigurationEntity
        {
            FormId = config.FormId,
            Description = config.Description,
            Enabled = config.Enabled,
            PrivacyMode = config.PrivacyMode,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            AllowedDomains = config.AllowedDomains
                .Select(domain => new AllowedDomainEntity { Domain = domain })
                .ToList()
        };

        // Map rate limit settings
        if (config.RateLimit is not null)
        {
            entity.RateLimit = new RateLimitSettingsEntity
            {
                RequestsPerWindow = config.RateLimit.RequestsPerWindow,
                WindowMinutes = config.RateLimit.WindowMinutes
            };
        }

        // Map connector configurations
        if (config.Connectors is not null && config.Connectors.Count > 0)
        {
            entity.Connectors = config.Connectors
                .Select(c => c.ToConnectorConfigurationEntity())
                .ToList();
        }

        return entity;
    }

    public static ConnectorConfiguration ToConnectorConfiguration(this ConnectorConfigurationEntity entity)
    {
        var settings = new Dictionary<string, object>();
        
        if (!string.IsNullOrEmpty(entity.SettingsJson))
        {
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(entity.SettingsJson, JsonOptions);
                settings = JsonElementToDictionary(jsonElement);
            }
            catch (JsonException)
            {
                // Log warning and use empty settings if JSON is invalid
                settings = new Dictionary<string, object>();
            }
        }

        return new ConnectorConfiguration(
            entity.Type,
            entity.Name,
            entity.Enabled,
            settings);
    }

    public static ConnectorConfigurationEntity ToConnectorConfigurationEntity(this ConnectorConfiguration config)
    {
        var settingsJson = config.Settings.Count > 0
            ? JsonSerializer.Serialize(config.Settings, JsonOptions)
            : null;

        return new ConnectorConfigurationEntity
        {
            Type = config.Type,
            Name = config.Name,
            Enabled = config.Enabled,
            SettingsJson = settingsJson
        };
    }

    private static Dictionary<string, object> JsonElementToDictionary(JsonElement element)
    {
        var dictionary = new Dictionary<string, object>();

        foreach (var property in element.EnumerateObject())
        {
            dictionary[property.Name] = JsonElementToObject(property.Value);
        }

        return dictionary;
    }

    private static object JsonElementToObject(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString()!,
        JsonValueKind.Number when element.TryGetInt64(out var intValue) => intValue,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null!,
        JsonValueKind.Object => JsonElementToDictionary(element),
        JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToArray(),
        _ => element.GetRawText()
    };
}