namespace G2PLC.Domain.Models;

public class GCodeCommand
{
    /// <summary>
    /// Gets or sets the command type (G, M, or T).
    /// </summary>
    public string CommandType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the numeric command code (e.g., 01 for G01, 03 for M03).
    /// </summary>
    public int? CommandNumber { get; set; }

    /// <summary>
    /// Gets or sets the parameters dictionary containing axis positions (X, Y, Z),
    /// feed rate (F), spindle speed (S), tool number (T), and optional parameters (I, J, K, R, etc.).
    /// </summary>
    public Dictionary<char, decimal> Parameters { get; set; } = new Dictionary<char, decimal>();

    /// <summary>
    /// Gets or sets the original unparsed line.
    /// </summary>
    public string RawLine { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional line number (N code).
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Gets or sets whether the command passed validation.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Initializes a new instance of the GCodeCommand class.
    /// </summary>
    /// <param name="rawLine">Optional raw G-code line.</param>
    public GCodeCommand(string? rawLine = null)
    {
        RawLine = rawLine ?? string.Empty;
        IsValid = false;
    }

    /// <summary>
    /// Validates the G-code command structure.
    /// </summary>
    /// <returns>A list of validation error messages (empty if valid).</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(CommandType))
        {
            errors.Add("Command type is required");
        }
        else if (CommandType != "G" && CommandType != "M" && CommandType != "T")
        {
            errors.Add($"Invalid command type: {CommandType}. Must be G, M, or T");
        }

        if (!CommandNumber.HasValue)
        {
            errors.Add("Command number is required");
        }
        else if (CommandNumber.Value < 0 || CommandNumber.Value > 999)
        {
            errors.Add($"Command number out of range: {CommandNumber.Value}");
        }

        return errors;
    }

    /// <summary>
    /// Returns a string representation of the G-code command for debugging.
    /// </summary>
    public override string ToString()
    {
        var commandStr = CommandNumber.HasValue ? $"{CommandType}{CommandNumber.Value:D2}" : CommandType;
        var lineNumStr = LineNumber.HasValue ? $"N{LineNumber.Value} " : "";
        var paramsStr = string.Join(" ", Parameters.Select(p => $"{p.Key}{p.Value}"));

        return $"{lineNumStr}{commandStr} {paramsStr}".Trim();
    }
}
