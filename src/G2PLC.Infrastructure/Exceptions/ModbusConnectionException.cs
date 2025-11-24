namespace G2PLC.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when a Modbus connection error occurs.
/// </summary>
public class ModbusConnectionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the ModbusConnectionException class.
    /// </summary>
    public ModbusConnectionException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the ModbusConnectionException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ModbusConnectionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ModbusConnectionException class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ModbusConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
