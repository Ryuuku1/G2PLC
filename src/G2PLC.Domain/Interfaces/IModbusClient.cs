namespace G2PLC.Domain.Interfaces;

/// <summary>
/// Provides Modbus TCP communication with PLC devices.
/// </summary>
public interface IModbusClient : IDisposable
{
    /// <summary>
    /// Gets whether the client is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to the PLC.
    /// </summary>
    void Connect();

    /// <summary>
    /// Connects to the PLC asynchronously.
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the PLC.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Writes a single holding register.
    /// </summary>
    void WriteHoldingRegister(ushort address, ushort value);

    /// <summary>
    /// Writes a single holding register asynchronously.
    /// </summary>
    Task WriteHoldingRegisterAsync(ushort address, ushort value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes multiple consecutive holding registers.
    /// </summary>
    void WriteMultipleRegisters(ushort startAddress, ushort[] values);

    /// <summary>
    /// Writes multiple consecutive holding registers asynchronously.
    /// </summary>
    Task WriteMultipleRegistersAsync(ushort startAddress, ushort[] values, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads multiple consecutive holding registers.
    /// </summary>
    ushort[] ReadHoldingRegisters(ushort startAddress, ushort count);

    /// <summary>
    /// Reads multiple consecutive holding registers asynchronously.
    /// </summary>
    Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a single coil (digital output).
    /// </summary>
    void WriteCoil(ushort address, bool value);

    /// <summary>
    /// Writes a single coil asynchronously.
    /// </summary>
    Task WriteCoilAsync(ushort address, bool value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads multiple consecutive coils.
    /// </summary>
    bool[] ReadCoils(ushort startAddress, ushort count);

    /// <summary>
    /// Reads multiple consecutive coils asynchronously.
    /// </summary>
    Task<bool[]> ReadCoilsAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default);
}
