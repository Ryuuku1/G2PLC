using G2PLC.Domain.Enums;

namespace G2PLC.Domain.Models;

/// <summary>
/// Represents a single operation on an LSF component.
/// </summary>
public class LsfOperation
{
    /// <summary>
    /// Gets or sets the type of operation (SWAGE, LIP_CUT, etc.).
    /// </summary>
    public LsfOperationType OperationType { get; set; }

    /// <summary>
    /// Gets or sets the position along the component length (in mm).
    /// </summary>
    public decimal Position { get; set; }

    public override string ToString()
    {
        return $"{OperationType} @ {Position:F1}mm";
    }
}
