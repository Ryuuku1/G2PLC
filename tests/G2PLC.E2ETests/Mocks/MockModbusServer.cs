using System.Collections.Concurrent;

namespace G2PLC.E2ETests.Mocks;

/// <summary>
/// Simplified mock Modbus TCP server for E2E testing.
/// Simulates a PLC by maintaining holding registers in memory.
/// For real Modbus testing, use an actual PLC or dedicated Modbus simulator.
/// </summary>
public class MockModbusServer : IDisposable
{
    private readonly ConcurrentDictionary<ushort, ushort> _holdingRegisters;
    private bool _disposed;

    public int Port { get; }
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets a snapshot of all holding registers.
    /// </summary>
    public IReadOnlyDictionary<ushort, ushort> HoldingRegisters =>
        new Dictionary<ushort, ushort>(_holdingRegisters);

    public MockModbusServer(int port = 5502)
    {
        Port = port;
        _holdingRegisters = new ConcurrentDictionary<ushort, ushort>();

        // Initialize registers with default values
        for (ushort i = 0; i < 1000; i++)
        {
            _holdingRegisters[i] = 0;
        }
    }

    /// <summary>
    /// Starts the mock Modbus server (simulated - no actual network server).
    /// </summary>
    public void Start()
    {
        if (IsRunning)
            return;

        IsRunning = true;
    }

    /// <summary>
    /// Stops the mock Modbus server.
    /// </summary>
    public void Stop()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
    }

    /// <summary>
    /// Gets the value of a holding register.
    /// </summary>
    public ushort GetHoldingRegister(ushort address)
    {
        return _holdingRegisters.TryGetValue(address, out var value) ? value : (ushort)0;
    }

    /// <summary>
    /// Sets the value of a holding register (simulates a write from client).
    /// </summary>
    public void SetHoldingRegister(ushort address, ushort value)
    {
        _holdingRegisters[address] = value;
    }

    /// <summary>
    /// Resets all registers to zero.
    /// </summary>
    public void ResetRegisters()
    {
        foreach (var key in _holdingRegisters.Keys.ToList())
        {
            _holdingRegisters[key] = 0;
        }
    }

    /// <summary>
    /// Gets all non-zero registers as a dictionary.
    /// </summary>
    public Dictionary<ushort, ushort> GetNonZeroRegisters()
    {
        return _holdingRegisters
            .Where(kvp => kvp.Value != 0)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Gets all registers (including zeros) as a dictionary.
    /// </summary>
    public Dictionary<ushort, ushort> GetAllRegisters()
    {
        return new Dictionary<ushort, ushort>(_holdingRegisters);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }
}
