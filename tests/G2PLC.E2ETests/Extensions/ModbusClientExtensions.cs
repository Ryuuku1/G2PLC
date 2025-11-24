using G2PLC.Domain.Interfaces;

namespace G2PLC.E2ETests.Extensions;

/// <summary>
/// Extension methods for IModbusClient to provide convenience methods for testing.
/// </summary>
public static class ModbusClientExtensions
{
    /// <summary>
    /// Reads a single holding register asynchronously.
    /// </summary>
    public static async Task<ushort> ReadHoldingRegisterAsync(this IModbusClient client, ushort address)
    {
        var values = await client.ReadHoldingRegistersAsync(address, 1);
        return values[0];
    }

    /// <summary>
    /// Writes multiple holding registers asynchronously (wrapper for interface method).
    /// </summary>
    public static Task WriteMultipleHoldingRegistersAsync(this IModbusClient client, ushort startAddress, ushort[] values)
    {
        return client.WriteMultipleRegistersAsync(startAddress, values);
    }
}
