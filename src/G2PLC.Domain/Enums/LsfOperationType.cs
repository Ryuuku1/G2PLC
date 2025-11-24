namespace G2PLC.Domain.Enums;

/// <summary>
/// Types of operations performed on LSF (Light Steel Framing) components.
/// </summary>
public enum LsfOperationType
{
    /// <summary>
    /// Swage operation - creates an indentation in the profile.
    /// </summary>
    Swage,

    /// <summary>
    /// Lip cut operation - cuts the lip/flange of the profile.
    /// </summary>
    LipCut,

    /// <summary>
    /// Notch operation - creates a notch in the profile.
    /// </summary>
    Notch,

    /// <summary>
    /// Dimple operation - creates a dimple for fastening.
    /// </summary>
    Dimple,

    /// <summary>
    /// End truss operation - cuts the end for truss connections.
    /// </summary>
    EndTruss
}
