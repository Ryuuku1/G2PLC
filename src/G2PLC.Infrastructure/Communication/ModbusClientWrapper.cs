using System.Net.Sockets;
using G2PLC.Domain.Interfaces;
using G2PLC.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using NModbus;

namespace G2PLC.Infrastructure.Communication;

/// <summary>
/// Wraps NModbus library with clean interface and comprehensive error handling.
/// </summary>
public class ModbusClientWrapper : IModbusClient
{
    private readonly ILogger<ModbusClientWrapper> _logger;
    private readonly string _ipAddress;
    private readonly int _port;
    private readonly byte _slaveId;
    private readonly int _connectTimeout;
    private readonly int _readTimeout;
    private readonly int _writeTimeout;

    private TcpClient? _tcpClient;
    private IModbusMaster? _modbusClient;
    private bool _isConnected = false;
    private bool _disposed = false;

    /// <summary>
    /// Gets a value indicating whether the client is currently connected to the PLC.
    /// </summary>
    public bool IsConnected => _isConnected && _tcpClient is { Connected: true };

    /// <summary>
    /// Initializes a new instance of the ModbusClientWrapper class.
    /// </summary>
    /// <param name="logger">The logger for logging operations.</param>
    /// <param name="ipAddress">The PLC IP address.</param>
    /// <param name="port">The Modbus TCP port (default 502).</param>
    /// <param name="slaveId">The Modbus slave ID (default 1).</param>
    /// <param name="connectTimeout">Connection timeout in milliseconds (default 5000).</param>
    /// <param name="readTimeout">Read timeout in milliseconds (default 1000).</param>
    /// <param name="writeTimeout">Write timeout in milliseconds (default 1000).</param>
    public ModbusClientWrapper(
        ILogger<ModbusClientWrapper> logger,
        string ipAddress,
        int port = 502,
        byte slaveId = 1,
        int connectTimeout = 5000,
        int readTimeout = 1000,
        int writeTimeout = 1000)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ipAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        _port = port;
        _slaveId = slaveId;
        _connectTimeout = connectTimeout;
        _readTimeout = readTimeout;
        _writeTimeout = writeTimeout;
    }

    /// <summary>
    /// Establishes a connection to the PLC.
    /// </summary>
    public void Connect()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ModbusClientWrapper));
        }

        try
        {
            _logger.LogInformation("Connecting to PLC at {IpAddress}:{Port}...", _ipAddress, _port);

            // Create TCP client with timeout
            _tcpClient = new TcpClient();
            var connectTask = _tcpClient.ConnectAsync(_ipAddress, _port);

            if (!connectTask.Wait(_connectTimeout))
            {
                _tcpClient?.Close();
                throw new ModbusTimeoutException($"Connection to {_ipAddress}:{_port} timed out after {_connectTimeout}ms");
            }

            // Set timeouts
            _tcpClient.ReceiveTimeout = _readTimeout;
            _tcpClient.SendTimeout = _writeTimeout;

            // Create Modbus master
            var factory = new ModbusFactory();
            _modbusClient = factory.CreateMaster(_tcpClient);

            _isConnected = true;

            _logger.LogInformation("Successfully connected to PLC at {IpAddress}:{Port}", _ipAddress, _port);
        }
        catch (ModbusTimeoutException)
        {
            throw;
        }
        catch (SocketException ex)
        {
            var message = $"Socket error connecting to PLC at {_ipAddress}:{_port}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusConnectionException(message, ex);
        }
        catch (Exception ex)
        {
            var message = $"Failed to connect to PLC at {_ipAddress}:{_port}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusConnectionException(message, ex);
        }
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ModbusClientWrapper));
        }

        try
        {
            _logger.LogInformation("Connecting to PLC at {IpAddress}:{Port} asynchronously...", _ipAddress, _port);

            _tcpClient = new TcpClient();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_connectTimeout);

            await _tcpClient.ConnectAsync(_ipAddress, _port, cts.Token);

            _tcpClient.ReceiveTimeout = _readTimeout;
            _tcpClient.SendTimeout = _writeTimeout;

            var factory = new ModbusFactory();
            _modbusClient = factory.CreateMaster(_tcpClient);

            _isConnected = true;

            _logger.LogInformation("Successfully connected to PLC at {IpAddress}:{Port}", _ipAddress, _port);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _tcpClient?.Close();
            throw new ModbusTimeoutException($"Connection to {_ipAddress}:{_port} timed out after {_connectTimeout}ms");
        }
        catch (SocketException ex)
        {
            var message = $"Socket error connecting to PLC at {_ipAddress}:{_port}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusConnectionException(message, ex);
        }
        catch (Exception ex)
        {
            var message = $"Failed to connect to PLC at {_ipAddress}:{_port}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusConnectionException(message, ex);
        }
    }

    /// <summary>
    /// Closes the connection to the PLC.
    /// </summary>
    public void Disconnect()
    {
        if (!_isConnected)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Disconnecting from PLC...");

            _modbusClient?.Dispose();
            _modbusClient = null;

            _tcpClient?.Close();
            _tcpClient = null;

            _isConnected = false;

            _logger.LogInformation("Successfully disconnected from PLC");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnect");
            // Don't throw on disconnect errors
        }
    }

    /// <summary>
    /// Writes a single holding register value to the PLC.
    /// </summary>
    /// <param name="address">The register address (0-based).</param>
    /// <param name="value">The value to write.</param>
    public void WriteHoldingRegister(ushort address, ushort value)
    {
        EnsureConnected();

        try
        {
            _modbusClient!.WriteSingleRegister(_slaveId, address, value);
            _logger.LogDebug("Wrote register {Address} = {Value}", address, value);
        }
        catch (InvalidModbusRequestException ex)
        {
            var message = $"Invalid Modbus request writing to register {address}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusWriteException(message, address, value, ex);
        }
        catch (SlaveException ex)
        {
            var message = $"PLC slave error writing to register {address}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusWriteException(message, address, value, ex);
        }
        catch (IOException ex)
        {
            var message = $"I/O error writing to register {address}: {ex.Message}";
            _logger.LogError(ex, message);
            _isConnected = false; // Mark as disconnected on I/O error
            throw new ModbusWriteException(message, address, value, ex);
        }
        catch (TimeoutException ex)
        {
            var message = $"Timeout writing to register {address}";
            _logger.LogError(ex, message);
            throw new ModbusTimeoutException(message, ex);
        }
        catch (Exception ex)
        {
            var message = $"Unexpected error writing to register {address}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusWriteException(message, address, value, ex);
        }
    }

    public async Task WriteHoldingRegisterAsync(ushort address, ushort value, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            await _modbusClient!.WriteSingleRegisterAsync(_slaveId, address, value);
            _logger.LogDebug("Wrote register {Address} = {Value} asynchronously", address, value);
        }
        catch (InvalidModbusRequestException ex)
        {
            var message = $"Invalid Modbus request writing to register {address}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusWriteException(message, address, value, ex);
        }
        catch (SlaveException ex)
        {
            var message = $"PLC slave error writing to register {address}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusWriteException(message, address, value, ex);
        }
        catch (IOException ex)
        {
            var message = $"I/O error writing to register {address}: {ex.Message}";
            _logger.LogError(ex, message);
            _isConnected = false;
            throw new ModbusWriteException(message, address, value, ex);
        }
        catch (TimeoutException ex)
        {
            var message = $"Timeout writing to register {address}";
            _logger.LogError(ex, message);
            throw new ModbusTimeoutException(message, ex);
        }
        catch (Exception ex)
        {
            var message = $"Unexpected error writing to register {address}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusWriteException(message, address, value, ex);
        }
    }

    /// <summary>
    /// Writes multiple holding register values to the PLC.
    /// </summary>
    /// <param name="startAddress">The starting register address (0-based).</param>
    /// <param name="values">The values to write.</param>
    public void WriteMultipleRegisters(ushort startAddress, ushort[] values)
    {
        EnsureConnected();

        if (values == null || values.Length == 0)
        {
            throw new ArgumentException("Values array cannot be null or empty", nameof(values));
        }

        try
        {
            _modbusClient!.WriteMultipleRegisters(_slaveId, startAddress, values);
            _logger.LogDebug("Wrote {Count} registers starting at {Address}", values.Length, startAddress);
        }
        catch (InvalidModbusRequestException ex)
        {
            var message = $"Invalid Modbus request writing to registers starting at {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusWriteException(message, ex);
        }
        catch (SlaveException ex)
        {
            var message = $"PLC slave error writing to registers starting at {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusWriteException(message, ex);
        }
        catch (IOException ex)
        {
            var message = $"I/O error writing to registers starting at {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            _isConnected = false;
            throw new ModbusWriteException(message, ex);
        }
        catch (TimeoutException ex)
        {
            var message = $"Timeout writing to registers starting at {startAddress}";
            _logger.LogError(ex, message);
            throw new ModbusTimeoutException(message, ex);
        }
        catch (Exception ex)
        {
            var message = $"Unexpected error writing to registers starting at {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusWriteException(message, ex);
        }
    }

    public async Task WriteMultipleRegistersAsync(ushort startAddress, ushort[] values, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        if (values == null || values.Length == 0)
        {
            throw new ArgumentException("Values array cannot be null or empty", nameof(values));
        }

        try
        {
            await _modbusClient!.WriteMultipleRegistersAsync(_slaveId, startAddress, values);
            _logger.LogDebug("Wrote {Count} registers starting at {Address} asynchronously", values.Length, startAddress);
        }
        catch (InvalidModbusRequestException ex)
        {
            var message = $"Invalid Modbus request writing to registers starting at {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusWriteException(message, ex);
        }
        catch (SlaveException ex)
        {
            var message = $"PLC slave error writing to registers starting at {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusWriteException(message, ex);
        }
        catch (IOException ex)
        {
            var message = $"I/O error writing to registers starting at {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            _isConnected = false;
            throw new ModbusWriteException(message, ex);
        }
        catch (TimeoutException ex)
        {
            var message = $"Timeout writing to registers starting at {startAddress}";
            _logger.LogError(ex, message);
            throw new ModbusTimeoutException(message, ex);
        }
        catch (Exception ex)
        {
            var message = $"Unexpected error writing to registers starting at {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusWriteException(message, ex);
        }
    }

    /// <summary>
    /// Reads holding register values from the PLC.
    /// </summary>
    /// <param name="startAddress">The starting register address (0-based).</param>
    /// <param name="count">The number of registers to read.</param>
    /// <returns>An array of register values.</returns>
    public ushort[] ReadHoldingRegisters(ushort startAddress, ushort count)
    {
        EnsureConnected();

        if (count == 0)
        {
            throw new ArgumentException("Count must be greater than 0", nameof(count));
        }

        try
        {
            var values = _modbusClient!.ReadHoldingRegisters(_slaveId, startAddress, count);
            _logger.LogDebug("Read {Count} registers starting at {Address}", count, startAddress);
            return values;
        }
        catch (InvalidModbusRequestException ex)
        {
            var message = $"Invalid Modbus request reading from register {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusReadException(message, startAddress, count, ex);
        }
        catch (SlaveException ex)
        {
            var message = $"PLC slave error reading from register {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusReadException(message, startAddress, count, ex);
        }
        catch (IOException ex)
        {
            var message = $"I/O error reading from register {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            _isConnected = false;
            throw new ModbusReadException(message, startAddress, count, ex);
        }
        catch (TimeoutException ex)
        {
            var message = $"Timeout reading from register {startAddress}";
            _logger.LogError(ex, message);
            throw new ModbusTimeoutException(message, ex);
        }
        catch (Exception ex)
        {
            var message = $"Unexpected error reading from register {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusReadException(message, startAddress, count, ex);
        }
    }

    public async Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        if (count == 0)
        {
            throw new ArgumentException("Count must be greater than 0", nameof(count));
        }

        try
        {
            var values = await _modbusClient!.ReadHoldingRegistersAsync(_slaveId, startAddress, count);
            _logger.LogDebug("Read {Count} registers starting at {Address} asynchronously", count, startAddress);
            return values;
        }
        catch (InvalidModbusRequestException ex)
        {
            var message = $"Invalid Modbus request reading from register {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusReadException(message, startAddress, count, ex);
        }
        catch (SlaveException ex)
        {
            var message = $"PLC slave error reading from register {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusReadException(message, startAddress, count, ex);
        }
        catch (IOException ex)
        {
            var message = $"I/O error reading from register {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            _isConnected = false;
            throw new ModbusReadException(message, startAddress, count, ex);
        }
        catch (TimeoutException ex)
        {
            var message = $"Timeout reading from register {startAddress}";
            _logger.LogError(ex, message);
            throw new ModbusTimeoutException(message, ex);
        }
        catch (Exception ex)
        {
            var message = $"Unexpected error reading from register {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusReadException(message, startAddress, count, ex);
        }
    }

    /// <summary>
    /// Writes a single coil value to the PLC.
    /// </summary>
    /// <param name="address">The coil address (0-based).</param>
    /// <param name="value">The value to write (true for ON, false for OFF).</param>
    public void WriteCoil(ushort address, bool value)
    {
        EnsureConnected();

        try
        {
            _modbusClient!.WriteSingleCoil(_slaveId, address, value);
            _logger.LogDebug("Wrote coil {Address} = {Value}", address, value);
        }
        catch (Exception ex)
        {
            var message = $"Error writing to coil {address}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusWriteException(message, ex);
        }
    }

    public async Task WriteCoilAsync(ushort address, bool value, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            await _modbusClient!.WriteSingleCoilAsync(_slaveId, address, value);
            _logger.LogDebug("Wrote coil {Address} = {Value} asynchronously", address, value);
        }
        catch (Exception ex)
        {
            var message = $"Error writing to coil {address}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusWriteException(message, ex);
        }
    }

    /// <summary>
    /// Reads coil values from the PLC.
    /// </summary>
    /// <param name="startAddress">The starting coil address (0-based).</param>
    /// <param name="count">The number of coils to read.</param>
    /// <returns>An array of coil values.</returns>
    public bool[] ReadCoils(ushort startAddress, ushort count)
    {
        EnsureConnected();

        if (count == 0)
        {
            throw new ArgumentException("Count must be greater than 0", nameof(count));
        }

        try
        {
            var values = _modbusClient!.ReadCoils(_slaveId, startAddress, count);
            _logger.LogDebug("Read {Count} coils starting at {Address}", count, startAddress);
            return values;
        }
        catch (Exception ex)
        {
            var message = $"Error reading from coil {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusReadException(message, startAddress, count, ex);
        }
    }

    public async Task<bool[]> ReadCoilsAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        if (count == 0)
        {
            throw new ArgumentException("Count must be greater than 0", nameof(count));
        }

        try
        {
            var values = await _modbusClient!.ReadCoilsAsync(_slaveId, startAddress, count);
            _logger.LogDebug("Read {Count} coils starting at {Address} asynchronously", count, startAddress);
            return values;
        }
        catch (Exception ex)
        {
            var message = $"Error reading from coil {startAddress}: {ex.Message}";
            _logger.LogError(ex, message);
            throw new ModbusReadException(message, startAddress, count, ex);
        }
    }

    /// <summary>
    /// Ensures the client is connected before performing operations.
    /// </summary>
    private void EnsureConnected()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ModbusClientWrapper));
        }

        if (!_isConnected || _tcpClient == null || !_tcpClient.Connected)
        {
            throw new ModbusConnectionException("Not connected to PLC. Call Connect() first.");
        }
    }

    /// <summary>
    /// Disposes the Modbus client and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Disconnect();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
