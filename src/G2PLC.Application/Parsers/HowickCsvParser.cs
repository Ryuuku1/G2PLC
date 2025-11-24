using G2PLC.Domain.Enums;
using G2PLC.Domain.Models;
using Microsoft.Extensions.Logging;

namespace G2PLC.Application.Parsers;

/// <summary>
/// Parses Howick V1 machine CSV format for LSF component manufacturing.
/// </summary>
public class HowickCsvParser
{
    private readonly ILogger<HowickCsvParser> _logger;

    public HowickCsvParser(ILogger<HowickCsvParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Parses a Howick CSV file and returns an LSF frameset.
    /// </summary>
    public async Task<LsfFrameset> ParseFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        _logger.LogInformation("Parsing Howick CSV file: {FilePath}", filePath);

        var frameset = new LsfFrameset();
        var lines = await File.ReadAllLinesAsync(filePath);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length == 0)
            {
                continue;
            }

            var recordType = parts[0].Trim();

            switch (recordType)
            {
                case "UNIT":
                    frameset.Unit = parts.Length > 1 ? parts[1].Trim() : "MILLIMETRE";
                    break;

                case "PROFILE":
                    if (parts.Length > 1)
                    {
                        frameset.Profile = parts[1].Trim();
                    }
                    if (parts.Length > 2)
                    {
                        frameset.ProfileDescription = parts[2].Trim();
                    }
                    break;

                case "FRAMESET":
                    if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out var framesetId))
                    {
                        frameset.FramesetId = framesetId;
                    }
                    if (parts.Length > 2)
                    {
                        frameset.FramesetName = parts[2].Trim();
                    }
                    if (parts.Length > 3)
                    {
                        frameset.Location = parts[3].Trim();
                    }
                    break;

                case "COMPONENT":
                    var component = ParseComponent(parts);
                    if (component != null)
                    {
                        frameset.Components.Add(component);
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown record type: {RecordType}", recordType);
                    break;
            }
        }

        _logger.LogInformation("Parsed frameset: {Frameset}", frameset);
        _logger.LogInformation("Total components: {Count}", frameset.Components.Count);

        return frameset;
    }

    private LsfComponent? ParseComponent(string[] parts)
    {
        if (parts.Length < 5)
        {
            _logger.LogWarning("Invalid COMPONENT record - not enough fields");
            return null;
        }

        var component = new LsfComponent
        {
            ComponentId = parts[1].Trim()
        };

        // Parse label orientation (LABEL_NRM or LABEL_INV)
        var labelStr = parts[2].Trim();
        component.LabelOrientation = labelStr == "LABEL_INV"
            ? LabelOrientation.Inverted
            : LabelOrientation.Normal;

        // Parse quantity
        if (int.TryParse(parts[3].Trim(), out var quantity))
        {
            component.Quantity = quantity;
        }

        // Parse length
        if (decimal.TryParse(parts[4].Trim(), out var length))
        {
            component.Length = length;
        }

        // Parse operations (pairs of operation_type, position)
        for (int i = 5; i < parts.Length; i += 2)
        {
            if (i + 1 >= parts.Length)
            {
                break; // No position for this operation
            }

            var operationTypeStr = parts[i].Trim();
            var positionStr = parts[i + 1].Trim();

            if (decimal.TryParse(positionStr, out var position))
            {
                var operationType = ParseOperationType(operationTypeStr);
                if (operationType.HasValue)
                {
                    component.Operations.Add(new LsfOperation
                    {
                        OperationType = operationType.Value,
                        Position = position
                    });
                }
            }
        }

        _logger.LogDebug("Parsed component: {Component}", component);

        return component;
    }

    private LsfOperationType? ParseOperationType(string operationStr)
    {
        return operationStr.ToUpperInvariant() switch
        {
            "SWAGE" => LsfOperationType.Swage,
            "LIP_CUT" => LsfOperationType.LipCut,
            "NOTCH" => LsfOperationType.Notch,
            "DIMPLE" => LsfOperationType.Dimple,
            "END_TRUSS" => LsfOperationType.EndTruss,
            _ => null
        };
    }
}
