using G2PLC.Domain.Models;

namespace G2PLC.Domain.Interfaces;

/// <summary>
/// Parses G-code text into structured commands.
/// </summary>
public interface IGCodeParser
{
    /// <summary>
    /// Parses a single G-code line into a structured command.
    /// </summary>
    GCodeCommand? ParseLine(string line);

    /// <summary>
    /// Parses all commands from a G-code file.
    /// </summary>
    List<GCodeCommand> ParseFile(string filePath);

    /// <summary>
    /// Parses all commands from a G-code file asynchronously.
    /// </summary>
    Task<List<GCodeCommand>> ParseFileAsync(string filePath);

    /// <summary>
    /// Validates whether a command is supported.
    /// </summary>
    bool IsValidCommand(GCodeCommand command);
}
