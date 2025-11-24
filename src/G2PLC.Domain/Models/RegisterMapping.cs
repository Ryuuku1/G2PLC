using G2PLC.Domain.Enums;

namespace G2PLC.Domain.Models;

/// <summary>
/// Represents a mapping between a G-code parameter value and a PLC register address.
/// </summary>
public class RegisterMapping
{
    /// <summary>
    /// Gets or sets the Modbus register address (0-based).
    /// </summary>
    public ushort Address { get; set; }

    /// <summary>
    /// Gets or sets the scaled value to write to the PLC register.
    /// </summary>
    public ushort Value { get; set; }

    /// <summary>
    /// Gets or sets the human-readable parameter name (e.g., "X_Position", "FeedRate").
    /// </summary>
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scaling factor applied to the original value.
    /// </summary>
    public decimal ScaleFactor { get; set; }

    /// <summary>
    /// Gets or sets the original G-code value before scaling was applied.
    /// </summary>
    public decimal OriginalValue { get; set; }

    /// <summary>
    /// Gets or sets the type of register (HoldingRegister or Coil).
    /// </summary>
    public RegisterType RegisterType { get; set; }

    /// <summary>
    /// Returns a string representation of the register mapping for debugging.
    /// </summary>
    public override string ToString()
    {
        return $"{ParameterName}: {OriginalValue} -> Address={Address}, Value={Value} (Scale={ScaleFactor}, Type={RegisterType})";
    }
}
