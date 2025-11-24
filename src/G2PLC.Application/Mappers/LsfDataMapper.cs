using G2PLC.Domain.Configuration;
using G2PLC.Domain.Enums;
using G2PLC.Domain.Models;
using Microsoft.Extensions.Logging;

namespace G2PLC.Application.Mappers;

/// <summary>
/// Maps LSF (Light Steel Framing) components to PLC register mappings.
/// </summary>
public class LsfDataMapper
{
    private readonly ILogger<LsfDataMapper> _logger;
    private readonly MappingConfiguration _configuration;

    // Base register addresses for LSF operations
    private const ushort BaseComponentIdAddress = 200;
    private const ushort BaseComponentLengthAddress = 201;
    private const ushort BaseComponentQuantityAddress = 202;
    private const ushort BaseComponentOrientationAddress = 203;
    private const ushort BaseOperationCountAddress = 204;
    private const ushort BaseOperationStartAddress = 210;

    public LsfDataMapper(ILogger<LsfDataMapper> logger, MappingConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Maps an LSF component to PLC register writes.
    /// </summary>
    public List<RegisterMapping> MapComponentToRegisters(LsfComponent component)
    {
        if (component == null)
        {
            throw new ArgumentNullException(nameof(component));
        }

        var mappings = new List<RegisterMapping>();

        try
        {
            _logger.LogInformation("Mapping LSF component: {Component}", component);

            // Component metadata
            AddComponentMetadata(component, mappings);

            // Operation mappings
            AddOperationMappings(component, mappings);

            mappings = mappings.OrderBy(m => m.Address).ToList();

            _logger.LogDebug("Mapped {ComponentId} to {Count} register(s)", component.ComponentId, mappings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping component to registers: {Component}", component);
            throw;
        }

        return mappings;
    }

    /// <summary>
    /// Maps an entire frameset to PLC register writes.
    /// </summary>
    public List<RegisterMapping> MapFramesetToRegisters(LsfFrameset frameset)
    {
        if (frameset == null)
        {
            throw new ArgumentNullException(nameof(frameset));
        }

        var allMappings = new List<RegisterMapping>();

        _logger.LogInformation("Mapping LSF frameset: {Frameset}", frameset);

        // Add frameset header information
        AddFramesetHeader(frameset, allMappings);

        // Map each component
        foreach (var component in frameset.Components)
        {
            var componentMappings = MapComponentToRegisters(component);
            allMappings.AddRange(componentMappings);

            // Add a trigger register to signal "component ready" to PLC
            allMappings.Add(new RegisterMapping
            {
                Address = 209, // Trigger address
                Value = 1,
                ParameterName = $"{component.ComponentId}_Trigger",
                ScaleFactor = 1m,
                OriginalValue = 1m,
                RegisterType = RegisterType.HoldingRegister
            });
        }

        _logger.LogInformation("Mapped frameset with {ComponentCount} components to {RegisterCount} total registers",
            frameset.Components.Count, allMappings.Count);

        return allMappings.OrderBy(m => m.Address).ToList();
    }

    private void AddFramesetHeader(LsfFrameset frameset, List<RegisterMapping> mappings)
    {
        // Frameset ID
        mappings.Add(new RegisterMapping
        {
            Address = 100,
            Value = (ushort)frameset.FramesetId,
            ParameterName = "FramesetId",
            ScaleFactor = 1m,
            OriginalValue = frameset.FramesetId,
            RegisterType = RegisterType.HoldingRegister
        });

        // Component count
        mappings.Add(new RegisterMapping
        {
            Address = 101,
            Value = (ushort)frameset.Components.Count,
            ParameterName = "ComponentCount",
            ScaleFactor = 1m,
            OriginalValue = frameset.Components.Count,
            RegisterType = RegisterType.HoldingRegister
        });

        _logger.LogDebug("Added frameset header: ID={FramesetId}, Components={Count}",
            frameset.FramesetId, frameset.Components.Count);
    }

    private void AddComponentMetadata(LsfComponent component, List<RegisterMapping> mappings)
    {
        // Component ID (convert first character to ASCII code)
        var componentIdValue = component.ComponentId.Length > 0
            ? (ushort)component.ComponentId[0]
            : (ushort)0;

        mappings.Add(new RegisterMapping
        {
            Address = BaseComponentIdAddress,
            Value = componentIdValue,
            ParameterName = $"{component.ComponentId}_ID",
            ScaleFactor = 1m,
            OriginalValue = componentIdValue,
            RegisterType = RegisterType.HoldingRegister
        });

        // Component length (scaled to microns)
        var scaledLength = ScaleAndClamp(component.Length, 1000m);
        mappings.Add(new RegisterMapping
        {
            Address = BaseComponentLengthAddress,
            Value = scaledLength,
            ParameterName = $"{component.ComponentId}_Length",
            ScaleFactor = 1000m,
            OriginalValue = component.Length,
            RegisterType = RegisterType.HoldingRegister
        });

        // Quantity
        mappings.Add(new RegisterMapping
        {
            Address = BaseComponentQuantityAddress,
            Value = (ushort)component.Quantity,
            ParameterName = $"{component.ComponentId}_Quantity",
            ScaleFactor = 1m,
            OriginalValue = component.Quantity,
            RegisterType = RegisterType.HoldingRegister
        });

        // Orientation (0 = Normal, 1 = Inverted)
        var orientationValue = component.LabelOrientation == LabelOrientation.Inverted ? (ushort)1 : (ushort)0;
        mappings.Add(new RegisterMapping
        {
            Address = BaseComponentOrientationAddress,
            Value = orientationValue,
            ParameterName = $"{component.ComponentId}_Orientation",
            ScaleFactor = 1m,
            OriginalValue = orientationValue,
            RegisterType = RegisterType.HoldingRegister
        });

        // Operation count
        mappings.Add(new RegisterMapping
        {
            Address = BaseOperationCountAddress,
            Value = (ushort)component.Operations.Count,
            ParameterName = $"{component.ComponentId}_OpCount",
            ScaleFactor = 1m,
            OriginalValue = component.Operations.Count,
            RegisterType = RegisterType.HoldingRegister
        });

        _logger.LogDebug("Added component metadata for {ComponentId}: Length={Length}mm, Qty={Qty}, Ops={OpCount}",
            component.ComponentId, component.Length, component.Quantity, component.Operations.Count);
    }

    private void AddOperationMappings(LsfComponent component, List<RegisterMapping> mappings)
    {
        ushort operationIndex = 0;

        foreach (var operation in component.Operations)
        {
            // Each operation uses 2 registers: [type, position]
            var baseAddress = (ushort)(BaseOperationStartAddress + (operationIndex * 2));

            // Operation type code
            var operationTypeCode = GetOperationTypeCode(operation.OperationType);
            mappings.Add(new RegisterMapping
            {
                Address = baseAddress,
                Value = operationTypeCode,
                ParameterName = $"{component.ComponentId}_Op{operationIndex}_Type",
                ScaleFactor = 1m,
                OriginalValue = operationTypeCode,
                RegisterType = RegisterType.HoldingRegister
            });

            // Operation position (scaled to microns)
            var scaledPosition = ScaleAndClamp(operation.Position, 1000m);
            mappings.Add(new RegisterMapping
            {
                Address = (ushort)(baseAddress + 1),
                Value = scaledPosition,
                ParameterName = $"{component.ComponentId}_Op{operationIndex}_Pos",
                ScaleFactor = 1000m,
                OriginalValue = operation.Position,
                RegisterType = RegisterType.HoldingRegister
            });

            _logger.LogDebug("Mapped operation {Index}: {Operation}", operationIndex, operation);

            operationIndex++;
        }
    }

    private ushort GetOperationTypeCode(LsfOperationType operationType)
    {
        return operationType switch
        {
            LsfOperationType.Swage => 1,
            LsfOperationType.LipCut => 2,
            LsfOperationType.Notch => 3,
            LsfOperationType.Dimple => 4,
            LsfOperationType.EndTruss => 5,
            _ => 0
        };
    }

    private ushort ScaleAndClamp(decimal value, decimal scaleFactor)
    {
        var scaledValue = value * scaleFactor;

        if (scaledValue < 0)
        {
            _logger.LogWarning("Scaled value {Value} is negative, clamping to 0", scaledValue);
            return 0;
        }

        if (scaledValue > ushort.MaxValue)
        {
            _logger.LogWarning("Scaled value {Value} exceeds ushort max, clamping to {Max}", scaledValue, ushort.MaxValue);
            return ushort.MaxValue;
        }

        return (ushort)Math.Round(scaledValue);
    }
}
