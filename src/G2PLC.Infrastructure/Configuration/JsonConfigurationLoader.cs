using System.Text.Json;
using System.Text.Json.Serialization;
using G2PLC.Domain.Interfaces;
using G2PLC.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace G2PLC.Infrastructure.Configuration;

public class JsonConfigurationLoader : IConfigurationLoader
{
    private readonly ILogger<JsonConfigurationLoader> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonConfigurationLoader(ILogger<JsonConfigurationLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Domain.Configuration.MappingConfiguration> LoadMappingConfigurationAsync(string configurationPath)
    {
        if (string.IsNullOrWhiteSpace(configurationPath))
        {
            _logger.LogError("Configuration path is null or empty");
            throw new ArgumentException("Configuration path cannot be null or empty", nameof(configurationPath));
        }

        if (!File.Exists(configurationPath))
        {
            _logger.LogError("Configuration file not found at path: {Path}", configurationPath);
            throw new FileNotFoundException($"Configuration file not found: {configurationPath}");
        }

        try
        {
            _logger.LogInformation("Loading configuration from: {Path}", configurationPath);

            await using var fileStream = File.OpenRead(configurationPath);
            var configuration = await JsonSerializer.DeserializeAsync<Domain.Configuration.MappingConfiguration>(
                fileStream,
                JsonOptions);

            if (configuration == null)
            {
                _logger.LogError("Failed to deserialize configuration - result was null");
                throw new ConfigurationException("Configuration deserialization resulted in null");
            }

            ValidateConfiguration(configuration);

            _logger.LogInformation("Configuration loaded successfully with {PositionCount} position mappings, {CommandCount} command mappings, {ParameterCount} parameter mappings",
                configuration.RegisterMappings.Axes.Count,
                configuration.RegisterMappings.Commands.Count,
                configuration.RegisterMappings.Parameters.Count);

            return configuration;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON configuration file");
            throw new ConfigurationException($"Invalid JSON in configuration file: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error reading configuration file");
            throw new ConfigurationException($"Error reading configuration file: {ex.Message}", ex);
        }
    }

    private void ValidateConfiguration(Domain.Configuration.MappingConfiguration configuration)
    {
        if (configuration.RegisterMappings == null)
        {
            throw new ConfigurationException("RegisterMappings section is required");
        }

        if (configuration.ValidationRules == null)
        {
            throw new ConfigurationException("ValidationRules section is required");
        }

        if (configuration.ProcessingOptions == null)
        {
            throw new ConfigurationException("ProcessingOptions section is required");
        }

        if (configuration.RegisterMappings.Axes.Count == 0)
        {
            _logger.LogWarning("No axis mappings defined in configuration");
        }

        if (configuration.RegisterMappings.Commands.Count == 0)
        {
            _logger.LogWarning("No command mappings defined in configuration");
        }
    }
}
