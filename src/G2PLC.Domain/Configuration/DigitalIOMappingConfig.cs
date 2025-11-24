namespace G2PLC.Domain.Configuration;

/// <summary>
/// Configuration for digital I/O (coils) mapping.
/// </summary>
public class DigitalIoMappingConfig
{
    /// <summary>
    /// The coil address in the PLC.
    /// </summary>
    public ushort Address { get; set; }

    /// <summary>
    /// Optional description of what this I/O controls.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// M-code that triggers this output (e.g., M8 for coolant on).
    /// </summary>
    public int? TriggerMCode { get; set; }

    /// <summary>
    /// Value to set when triggered (true = ON, false = OFF).
    /// </summary>
    public bool TriggerValue { get; set; } = true;
}
