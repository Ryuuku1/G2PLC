namespace G2PLC.Domain.Models;

/// <summary>
/// Represents a complete LSF frameset containing multiple components.
/// </summary>
public class LsfFrameset
{
    /// <summary>
    /// Gets or sets the unit of measurement (e.g., "MILLIMETRE").
    /// </summary>
    public string Unit { get; set; } = "MILLIMETRE";

    /// <summary>
    /// Gets or sets the profile dimensions (e.g., "41.30X100.00").
    /// </summary>
    public string Profile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the profile description (e.g., "Standard Profile").
    /// </summary>
    public string ProfileDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the frameset ID.
    /// </summary>
    public int FramesetId { get; set; }

    /// <summary>
    /// Gets or sets the frameset name (e.g., "Alfa").
    /// </summary>
    public string FramesetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the frameset location/project (e.g., "Chicago").
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of components in this frameset.
    /// </summary>
    public List<LsfComponent> Components { get; set; } = new();

    public override string ToString()
    {
        return $"Frameset {FramesetId}: {FramesetName} @ {Location} ({Components.Count} components, Profile: {Profile})";
    }
}
