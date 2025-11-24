using System.Configuration;

namespace G2PLC.Infrastructure.Configuration;

/// <summary>
/// Manages application configuration from App.config file.
/// </summary>
public class AppConfigurationManager
{
    /// <summary>
    /// Gets a configuration value as a string.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value if key is not found.</param>
    /// <returns>The configuration value or default value.</returns>
    public static string GetString(string key, string defaultValue = "")
    {
        var value = ConfigurationManager.AppSettings[key];
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    /// <summary>
    /// Gets a configuration value as an integer.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value if key is not found or invalid.</param>
    /// <returns>The configuration value or default value.</returns>
    public static int GetInt(string key, int defaultValue = 0)
    {
        var value = ConfigurationManager.AppSettings[key];
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets a configuration value as a ushort.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value if key is not found or invalid.</param>
    /// <returns>The configuration value or default value.</returns>
    public static ushort GetUShort(string key, ushort defaultValue = 0)
    {
        var value = ConfigurationManager.AppSettings[key];
        return ushort.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets a configuration value as a byte.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value if key is not found or invalid.</param>
    /// <returns>The configuration value or default value.</returns>
    public static byte GetByte(string key, byte defaultValue = 0)
    {
        var value = ConfigurationManager.AppSettings[key];
        return byte.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets a configuration value as a decimal.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value if key is not found or invalid.</param>
    /// <returns>The configuration value or default value.</returns>
    public static decimal GetDecimal(string key, decimal defaultValue = 0)
    {
        var value = ConfigurationManager.AppSettings[key];
        return decimal.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets a configuration value as a boolean.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value if key is not found or invalid.</param>
    /// <returns>The configuration value or default value.</returns>
    public static bool GetBool(string key, bool defaultValue = false)
    {
        var value = ConfigurationManager.AppSettings[key];
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Validates that a string is a valid IP address format.
    /// </summary>
    /// <param name="ipAddress">The IP address to validate.</param>
    /// <returns>True if valid, otherwise false.</returns>
    public static bool ValidateIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return false;
        }

        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }

    /// <summary>
    /// Validates that a port number is in the valid range.
    /// </summary>
    /// <param name="port">The port number to validate.</param>
    /// <returns>True if valid, otherwise false.</returns>
    public static bool ValidatePort(int port)
    {
        return port is >= 1 and <= 65535;
    }
}
