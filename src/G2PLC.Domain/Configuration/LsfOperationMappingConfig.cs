namespace G2PLC.Domain.Configuration;

/// <summary>
/// Configuration for mapping LSF operations to PLC registers.
/// </summary>
public class LsfOperationMappingConfig
{
    /// <summary>
    /// Gets or sets the PLC register address for the operation type.
    /// </summary>
    public ushort OperationTypeAddress { get; set; }

    /// <summary>
    /// Gets or sets the PLC register address for the operation position.
    /// </summary>
    public ushort PositionAddress { get; set; }

    /// <summary>
    /// Gets or sets the scale factor for position (e.g., 1000 to convert mm to microns).
    /// </summary>
    public decimal PositionScaleFactor { get; set; } = 1000m;

    /// <summary>
    /// Gets or sets the description of this operation mapping.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the numeric code to send to PLC for this operation type.
    /// </summary>
    public ushort OperationCode { get; set; }
}
