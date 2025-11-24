namespace G2PLC.Domain.Enums;

public enum RegisterType
{
    /// <summary>
    /// Holding Register (Function Code 03 for read, 16 for write multiple).
    /// Used for storing 16-bit integer values.
    /// </summary>
    HoldingRegister,

    /// <summary>
    /// Coil (Function Code 01 for read, 05 for write single).
    /// Used for storing boolean (on/off) values.
    /// </summary>
    Coil
}
