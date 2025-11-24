namespace G2PLC.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when a Modbus operation times out.
/// </summary>
public class ModbusTimeoutException : Exception
{
    /// <summary>
    /// Initializes a new instance of the ModbusTimeoutException class.
    /// </summary>
    public ModbusTimeoutException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the ModbusTimeoutException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ModbusTimeoutException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ModbusTimeoutException class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ModbusTimeoutException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
