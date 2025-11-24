namespace G2PLC.Domain.Configuration;

/// <summary>
/// Configuration for mapping a value to an OPC UA node.
/// </summary>
public class OpcUaNodeMappingConfig
{
    /// <summary>
    /// Gets or sets the OPC UA node identifier (e.g., "ns=2;s=MyVariable" or just "MyVariable" if base path is configured).
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scale factor to apply to the value before writing.
    /// </summary>
    public decimal ScaleFactor { get; set; } = 1m;

    /// <summary>
    /// Gets or sets the description of this node mapping.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type for the OPC UA node (Int16, UInt16, Int32, Float, Double, String, etc.).
    /// </summary>
    public string DataType { get; set; } = "Int32";
}
