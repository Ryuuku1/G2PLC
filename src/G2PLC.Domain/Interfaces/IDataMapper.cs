using G2PLC.Domain.Models;

namespace G2PLC.Domain.Interfaces;

/// <summary>
/// Maps G-code commands to PLC register addresses.
/// </summary>
public interface IDataMapper
{
    /// <summary>
    /// Converts a G-code command to a list of register mappings.
    /// </summary>
    List<RegisterMapping> MapToRegisters(GCodeCommand command);

    /// <summary>
    /// Returns the current register address mapping configuration.
    /// </summary>
    Dictionary<string, ushort> GetRegisterAddresses();
}
