using G2PLC.Domain.Models;
using System.Net;

namespace G2PLC.Infrastructure.Validation;

/// <summary>
/// Provides validation methods for data before sending to PLC.
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validates if a string is a valid IP address format.
    /// </summary>
    /// <param name="ip">The IP address string to validate.</param>
    /// <returns>True if valid, otherwise false.</returns>
    public static bool IsValidIpAddress(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return false;
        }

        return IPAddress.TryParse(ip, out _);
    }

    /// <summary>
    /// Validates if a port number is in the valid range (1-65535).
    /// </summary>
    /// <param name="port">The port number to validate.</param>
    /// <returns>True if valid, otherwise false.</returns>
    public static bool IsValidPort(int port)
    {
        return port is >= 1 and <= 65535;
    }

    /// <summary>
    /// Validates if a register address is in a valid range.
    /// </summary>
    /// <param name="address">The register address to validate.</param>
    /// <returns>True if valid, otherwise false.</returns>
    public static bool IsValidRegisterAddress(ushort address)
    {
        // Modbus holding registers can range from 0 to 65535
        return true; // ushort is always in valid range
    }

    /// <summary>
    /// Validates if a register value is valid.
    /// </summary>
    /// <param name="value">The register value to validate.</param>
    /// <returns>True if valid, otherwise false.</returns>
    public static bool IsValidRegisterValue(ushort value)
    {
        // ushort is always in valid range
        return true;
    }

    /// <summary>
    /// Validates a G-code command and returns a list of validation warnings/errors.
    /// </summary>
    /// <param name="command">The G-code command to validate.</param>
    /// <returns>A list of validation messages (empty if valid).</returns>
    public static List<string> ValidateGCodeCommand(GCodeCommand command)
    {
        var messages = new List<string>();

        if (command == null)
        {
            messages.Add("Command is null");
            return messages;
        }

        // Use the built-in validation method
        var errors = command.Validate();
        if (errors.Any())
        {
            messages.AddRange(errors);
        }

        // Additional specific validations
        if (command.Parameters.TryGetValue('X', out var xValue))
        {
            if (Math.Abs(xValue) > 10000)
            {
                messages.Add($"X position {xValue} may be out of typical range");
            }
        }

        if (command.Parameters.TryGetValue('Y', out var yValue))
        {
            if (Math.Abs(yValue) > 10000)
            {
                messages.Add($"Y position {yValue} may be out of typical range");
            }
        }

        if (command.Parameters.TryGetValue('Z', out var zValue))
        {
            if (Math.Abs(zValue) > 10000)
            {
                messages.Add($"Z position {zValue} may be out of typical range");
            }
        }

        if (command.Parameters.TryGetValue('F', out var feedRate))
        {
            if (feedRate <= 0)
            {
                messages.Add($"Feed rate {feedRate} must be positive");
            }
            else if (feedRate > 10000)
            {
                messages.Add($"Feed rate {feedRate} may be out of typical range");
            }
        }

        if (command.Parameters.TryGetValue('S', out var spindleSpeed))
        {
            if (spindleSpeed < 0)
            {
                messages.Add($"Spindle speed {spindleSpeed} cannot be negative");
            }
            else if (spindleSpeed > 65535)
            {
                messages.Add($"Spindle speed {spindleSpeed} exceeds maximum value");
            }
        }

        return messages;
    }
}
