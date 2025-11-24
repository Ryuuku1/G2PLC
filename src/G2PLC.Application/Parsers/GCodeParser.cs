using System.Text.RegularExpressions;
using G2PLC.Domain.Interfaces;
using G2PLC.Domain.Models;
using Microsoft.Extensions.Logging;

namespace G2PLC.Application.Parsers;

public class GCodeParser : IGCodeParser
{
    private readonly ILogger<GCodeParser> _logger;

    // Regular expression patterns
    private static readonly Regex CommandPattern = new(@"([GMT])([0-9]+)", RegexOptions.Compiled);
    private static readonly Regex ParameterPattern = new(@"([XYZABCFSTIJKR])([+-]?[0-9]*\.?[0-9]+)", RegexOptions.Compiled);
    private static readonly Regex CommentPattern = new(@"\(.*?\)|;.*", RegexOptions.Compiled);
    private static readonly Regex LineNumberPattern = new(@"^N([0-9]+)", RegexOptions.Compiled);

    public GCodeParser(ILogger<GCodeParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public GCodeCommand? ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        try
        {
            // Trim and convert to uppercase
            var processedLine = line.Trim().ToUpperInvariant();

            // Remove comments
            processedLine = CommentPattern.Replace(processedLine, "").Trim();

            // Skip empty lines after comment removal
            if (string.IsNullOrWhiteSpace(processedLine))
            {
                return null;
            }

            var command = new GCodeCommand(line);

            // Extract line number if present
            var lineNumberMatch = LineNumberPattern.Match(processedLine);
            if (lineNumberMatch.Success)
            {
                command.LineNumber = int.Parse(lineNumberMatch.Groups[1].Value);
                processedLine = processedLine.Substring(lineNumberMatch.Length).Trim();
            }

            // Extract main command (G/M/T)
            var commandMatch = CommandPattern.Match(processedLine);
            if (!commandMatch.Success)
            {
                _logger.LogWarning("No valid command found in line: {Line}", line);
                return null;
            }

            command.CommandType = commandMatch.Groups[1].Value;
            command.CommandNumber = int.Parse(commandMatch.Groups[2].Value);

            // Extract all parameters
            var parameterMatches = ParameterPattern.Matches(processedLine);
            foreach (Match match in parameterMatches)
            {
                var paramChar = match.Groups[1].Value[0];
                var paramValue = decimal.Parse(match.Groups[2].Value);

                // If same parameter appears twice, use the last value
                command.Parameters[paramChar] = paramValue;
            }

            // Validate the command
            command.IsValid = IsValidCommand(command);

            if (command.IsValid)
            {
                _logger.LogDebug("Successfully parsed: {Command}", command);
            }
            else
            {
                _logger.LogWarning("Invalid command structure: {Line}", line);
            }

            return command;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing G-code line '{Line}'", line);
            return null;
        }
    }

    public List<GCodeCommand> ParseFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"G-code file not found: {filePath}");
        }

        _logger.LogInformation("Parsing G-code file: {FilePath}", filePath);

        var commands = new List<GCodeCommand>();
        var lines = File.ReadAllLines(filePath);

        for (var i = 0; i < lines.Length; i++)
        {
            var command = ParseLine(lines[i]);
            if (command != null)
            {
                commands.Add(command);
            }
        }

        _logger.LogInformation("Parsed {Count} valid commands from {TotalLines} lines", commands.Count, lines.Length);

        return commands;
    }

    public async Task<List<GCodeCommand>> ParseFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"G-code file not found: {filePath}");
        }

        _logger.LogInformation("Parsing G-code file asynchronously: {FilePath}", filePath);

        var commands = new List<GCodeCommand>();
        var lines = await File.ReadAllLinesAsync(filePath);

        for (var i = 0; i < lines.Length; i++)
        {
            var command = ParseLine(lines[i]);
            if (command != null)
            {
                commands.Add(command);
            }
        }

        _logger.LogInformation("Parsed {Count} valid commands from {TotalLines} lines", commands.Count, lines.Length);

        return commands;
    }

    public bool IsValidCommand(GCodeCommand command)
    {
        if (command == null)
        {
            return false;
        }

        var errors = command.Validate();
        if (errors.Any())
        {
            foreach (var error in errors)
            {
                _logger.LogDebug("Validation error: {Error}", error);
            }
            return false;
        }

        // Additional validation for specific commands
        if (command.CommandType == "G")
        {
            return IsValidGCommand(command.CommandNumber ?? 0);
        }
        else if (command.CommandType == "M")
        {
            return IsValidMCommand(command.CommandNumber ?? 0);
        }
        else if (command.CommandType == "T")
        {
            return IsValidTCommand(command.CommandNumber ?? 0);
        }

        return true;
    }

    private static bool IsValidGCommand(int commandNumber)
    {
        // Supported G-codes
        var validGCodes = new[] { 0, 1, 2, 3, 17, 18, 19, 20, 21, 28, 30, 90, 91 };
        return validGCodes.Contains(commandNumber);
    }

    private static bool IsValidMCommand(int commandNumber)
    {
        // Supported M-codes
        var validMCodes = new[] { 0, 1, 3, 4, 5, 30 };
        return validMCodes.Contains(commandNumber);
    }

    private static bool IsValidTCommand(int commandNumber)
    {
        // Tool numbers typically range from 0-99
        return commandNumber is >= 0 and <= 99;
    }
}
