namespace G2PLC.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when a Modbus write operation fails.
/// </summary>
public class ModbusWriteException : Exception
{
    /// <summary>
    /// Gets the register address where the write failed.
    /// </summary>
    public ushort Address { get; }

    /// <summary>
    /// Gets the value that was attempted to be written.
    /// </summary>
    public ushort Value { get; }

    /// <summary>
    /// Initializes a new instance of the ModbusWriteException class.
    /// </summary>
    public ModbusWriteException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the ModbusWriteException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ModbusWriteException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ModbusWriteException class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ModbusWriteException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ModbusWriteException class with address and value context.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="address">The register address where the write failed.</param>
    /// <param name="value">The value that was attempted to be written.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ModbusWriteException(string message, ushort address, ushort value, Exception innerException)
        : base(message, innerException)
    {
        Address = address;
        Value = value;
    }
}
