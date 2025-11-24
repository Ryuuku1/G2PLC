using G2PLC.Domain.Enums;

namespace G2PLC.Domain.Models;

/// <summary>
/// Represents an LSF (Light Steel Framing) component with operations.
/// </summary>
public class LsfComponent
{
    /// <summary>
    /// Gets or sets the component identifier (e.g., "H1", "V2", "D1").
    /// </summary>
    public string ComponentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the label orientation (Normal or Inverted).
    /// </summary>
    public LabelOrientation LabelOrientation { get; set; }

    /// <summary>
    /// Gets or sets the quantity of this component to manufacture.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the total length of the component (in mm).
    /// </summary>
    public decimal Length { get; set; }

    /// <summary>
    /// Gets or sets the list of operations to perform on this component.
    /// </summary>
    public List<LsfOperation> Operations { get; set; } = new();

    public override string ToString()
    {
        return $"{ComponentId} ({LabelOrientation}, {Length:F1}mm, {Operations.Count} ops, Qty: {Quantity})";
    }
}
