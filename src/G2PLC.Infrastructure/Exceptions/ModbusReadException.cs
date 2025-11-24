namespace G2PLC.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when a Modbus read operation fails.
/// </summary>
public class ModbusReadException : Exception
{
    /// <summary>
    /// Gets the register address where the read failed.
    /// </summary>
    public ushort Address { get; }

    /// <summary>
    /// Gets the number of registers attempted to read.
    /// </summary>
    public ushort Count { get; }

    /// <summary>
    /// Initializes a new instance of the ModbusReadException class.
    /// </summary>
    public ModbusReadException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the ModbusReadException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ModbusReadException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ModbusReadException class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ModbusReadException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ModbusReadException class with address and count context.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="address">The register address where the read failed.</param>
    /// <param name="count">The number of registers attempted to read.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ModbusReadException(string message, ushort address, ushort count, Exception innerException)
        : base(message, innerException)
    {
        Address = address;
        Count = count;
    }
}
