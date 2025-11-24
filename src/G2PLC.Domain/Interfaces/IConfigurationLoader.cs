namespace G2PLC.Domain.Interfaces;

/// <summary>
/// Loads configuration from external sources.
/// </summary>
public interface IConfigurationLoader
{
    /// <summary>
    /// Loads mapping configuration from the specified path.
    /// </summary>
    Task<Configuration.MappingConfiguration> LoadMappingConfigurationAsync(string configurationPath);
}
