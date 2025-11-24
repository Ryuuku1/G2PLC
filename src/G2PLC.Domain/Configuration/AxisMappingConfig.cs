namespace G2PLC.Domain.Configuration;

/// <summary>
/// Configuration for an axis mapping (extends RegisterMappingConfig with axis-specific properties).
/// </summary>
public class AxisMappingConfig : RegisterMappingConfig
{
    /// <summary>
    /// Gets or sets the type of axis (Linear, Rotational, or Custom).
    /// </summary>
    public AxisType AxisType { get; set; } = AxisType.Linear;

    /// <summary>
    /// Gets or sets the unit of measurement for this axis.
    /// Examples: "mm", "degrees", "inches", "microns"
    /// </summary>
    public string Unit { get; set; } = "mm";
}

/// <summary>
/// Defines the type of axis.
/// </summary>
public enum AxisType
{
    /// <summary>
    /// Linear axis (X, Y, Z, U, V, W, etc.) - measured in distance units.
    /// </summary>
    Linear,

    /// <summary>
    /// Rotational axis (A, B, C, etc.) - measured in angular units.
    /// </summary>
    Rotational,

    /// <summary>
    /// Custom axis type for machine-specific axes.
    /// </summary>
    Custom
}
