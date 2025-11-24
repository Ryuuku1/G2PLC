namespace G2PLC.Domain.Configuration;

/// <summary>
/// Configuration for a single register mapping.
/// </summary>
public class RegisterMappingConfig
{
    /// <summary>
    /// Gets or sets the PLC register address (0-based).
    /// </summary>
    public ushort Address { get; set; }

    /// <summary>
    /// Gets or sets the scaling factor to apply to values.
    /// </summary>
    public decimal ScaleFactor { get; set; } = 1.0m;

    /// <summary>
    /// Gets or sets the human-readable description of this mapping.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
