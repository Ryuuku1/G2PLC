using G2PLC.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace G2PLC.E2ETests.Mocks;

/// <summary>
/// Mock Modbus client that connects to MockModbusServer for testing.
/// </summary>
public class MockModbusClient : IModbusClient
{
    private readonly ILogger<MockModbusClient> _logger;
    private readonly MockModbusServer _mockServer;
    private bool _disposed;

    public bool IsConnected { get; private set; }

    public MockModbusClient(ILogger<MockModbusClient> logger, MockModbusServer mockServer)
    {
        _logger = logger;
        _mockServer = mockServer;
    }

    public void Connect()
    {
        ConnectAsync().GetAwaiter().GetResult();
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (!_mockServer.IsRunning)
        {
            throw new InvalidOperationException("Mock Modbus server is not running");
        }

        IsConnected = true;
        _logger.LogInformation("Connected to mock Modbus server on port {Port}", _mockServer.Port);
        return Task.CompletedTask;
    }

    public void Disconnect()
    {
        IsConnected = false;
        _logger.LogInformation("Disconnected from mock Modbus server");
    }

    public void WriteHoldingRegister(ushort address, ushort value)
    {
        WriteHoldingRegisterAsync(address, value).GetAwaiter().GetResult();
    }

    public Task WriteHoldingRegisterAsync(ushort address, ushort value, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected to Modbus server");
        }

        _mockServer.SetHoldingRegister(address, value);
        _logger.LogDebug("Wrote value {Value} to register {Address}", value, address);
        return Task.CompletedTask;
    }

    public void WriteMultipleRegisters(ushort startAddress, ushort[] values)
    {
        WriteMultipleRegistersAsync(startAddress, values).GetAwaiter().GetResult();
    }

    public async Task WriteMultipleRegistersAsync(ushort startAddress, ushort[] values, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected to Modbus server");
        }

        for (int i = 0; i < values.Length; i++)
        {
            await WriteHoldingRegisterAsync((ushort)(startAddress + i), values[i], cancellationToken);
        }

        _logger.LogDebug("Wrote {Count} values starting at register {Address}", values.Length, startAddress);
    }

    public ushort[] ReadHoldingRegisters(ushort startAddress, ushort count)
    {
        return ReadHoldingRegistersAsync(startAddress, count).GetAwaiter().GetResult();
    }

    public Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected to Modbus server");
        }

        var values = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = _mockServer.GetHoldingRegister((ushort)(startAddress + i));
        }

        _logger.LogDebug("Read {Count} values from register {Address}", count, startAddress);
        return Task.FromResult(values);
    }

    public void WriteCoil(ushort address, bool value)
    {
        WriteCoilAsync(address, value).GetAwaiter().GetResult();
    }

    public Task WriteCoilAsync(ushort address, bool value, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("WriteCoil not implemented in mock - address: {Address}, value: {Value}", address, value);
        return Task.CompletedTask;
    }

    public bool[] ReadCoils(ushort startAddress, ushort count)
    {
        return ReadCoilsAsync(startAddress, count).GetAwaiter().GetResult();
    }

    public Task<bool[]> ReadCoilsAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ReadCoils not implemented in mock - returning default values");
        return Task.FromResult(new bool[count]);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Disconnect();
            _disposed = true;
        }
    }
}
