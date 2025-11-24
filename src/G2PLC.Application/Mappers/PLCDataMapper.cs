using G2PLC.Domain.Configuration;
using G2PLC.Domain.Enums;
using G2PLC.Domain.Interfaces;
using G2PLC.Domain.Models;
using Microsoft.Extensions.Logging;

namespace G2PLC.Application.Mappers;

public class PlcDataMapper : IDataMapper
{
    private readonly ILogger<PlcDataMapper> _logger;
    private readonly MappingConfiguration _configuration;

    public PlcDataMapper(ILogger<PlcDataMapper> logger, MappingConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        ValidateConfiguration();
        LogConfigurationSummary();
    }

    public List<RegisterMapping> MapToRegisters(GCodeCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var mappings = new List<RegisterMapping>();

        try
        {
            AddCommandMapping(command, mappings);
            AddPositionMappings(command, mappings);
            AddParameterMappings(command, mappings);
            AddDigitalOutputMappings(command, mappings);

            mappings = mappings.OrderBy(m => m.Address).ToList();

            _logger.LogDebug("Mapped {Command} to {Count} register(s)", command, mappings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping command to registers: {Command}", command);
            throw;
        }

        return mappings;
    }

    public Dictionary<string, ushort> GetRegisterAddresses()
    {
        var addresses = new Dictionary<string, ushort>();

        // Add all dynamically configured axes
        foreach (var kvp in _configuration.RegisterMappings.Axes)
        {
            addresses[$"{kvp.Key}_Position"] = kvp.Value.Address;
        }

        // Backward compatibility: add legacy Positions and RotationalAxes if Axes is empty
#pragma warning disable CS0618
        if (_configuration.RegisterMappings.Axes.Count == 0)
        {
            if (_configuration.RegisterMappings.Positions != null)
            {
                foreach (var kvp in _configuration.RegisterMappings.Positions)
                {
                    addresses[$"{kvp.Key}_Position"] = kvp.Value.Address;
                }
            }

            if (_configuration.RegisterMappings.RotationalAxes != null)
            {
                foreach (var kvp in _configuration.RegisterMappings.RotationalAxes)
                {
                    addresses[$"{kvp.Key}_Position"] = kvp.Value.Address;
                }
            }
        }
#pragma warning restore CS0618

        foreach (var kvp in _configuration.RegisterMappings.Commands)
        {
            addresses[kvp.Key] = kvp.Value.Address;
        }

        foreach (var kvp in _configuration.RegisterMappings.Parameters)
        {
            addresses[kvp.Key] = kvp.Value.Address;
        }

        foreach (var kvp in _configuration.RegisterMappings.DigitalOutputs)
        {
            addresses[$"Digital_{kvp.Key}"] = kvp.Value.Address;
        }

        foreach (var kvp in _configuration.RegisterMappings.CustomRegisters)
        {
            addresses[$"Custom_{kvp.Key}"] = kvp.Value.Address;
        }

        return addresses;
    }

    private void AddCommandMapping(GCodeCommand command, List<RegisterMapping> mappings)
    {
        if (!command.CommandNumber.HasValue)
        {
            return;
        }

        switch (command.CommandType)
        {
            case "G":
                AddGCommandMapping(command.CommandNumber.Value, mappings);
                break;
            case "M":
                AddMCommandMapping(command.CommandNumber.Value, mappings);
                break;
            case "T":
                AddToolNumberMapping(command.CommandNumber.Value, mappings);
                break;
        }
    }

    private void AddGCommandMapping(int commandNumber, List<RegisterMapping> mappings)
    {
        if (!_configuration.RegisterMappings.Commands.TryGetValue("GCommand", out var config))
        {
            _logger.LogWarning("GCommand mapping not found in configuration");
            return;
        }

        mappings.Add(CreateMapping(
            config.Address,
            (ushort)commandNumber,
            "G_Command",
            1m,
            commandNumber));
    }

    private void AddMCommandMapping(int commandNumber, List<RegisterMapping> mappings)
    {
        if (!_configuration.RegisterMappings.Commands.TryGetValue("MCommand", out var config))
        {
            _logger.LogWarning("MCommand mapping not found in configuration");
            return;
        }

        mappings.Add(CreateMapping(
            config.Address,
            (ushort)commandNumber,
            "M_Command",
            1m,
            commandNumber));
    }

    private void AddToolNumberMapping(int toolNumber, List<RegisterMapping> mappings)
    {
        if (!IsValidToolNumber(toolNumber))
        {
            _logger.LogWarning("Tool number {ToolNumber} out of valid range", toolNumber);
            return;
        }

        if (!_configuration.RegisterMappings.Parameters.TryGetValue("ToolNumber", out var config))
        {
            _logger.LogWarning("ToolNumber mapping not found in configuration");
            return;
        }

        mappings.Add(CreateMapping(
            config.Address,
            (ushort)toolNumber,
            "Tool_Number",
            config.ScaleFactor,
            toolNumber));
    }

    private void AddPositionMappings(GCodeCommand command, List<RegisterMapping> mappings)
    {
        // Dynamically process all configured axes
        foreach (var axisEntry in _configuration.RegisterMappings.Axes)
        {
            var axisName = axisEntry.Key;
            var axisConfig = axisEntry.Value;

            // Axis name should be a single character for G-code compatibility
            if (string.IsNullOrEmpty(axisName) || axisName.Length != 1)
            {
                _logger.LogWarning("Axis name '{AxisName}' is invalid (must be single character) - skipping", axisName);
                continue;
            }

            var axisChar = axisName[0];

            // Check if this axis has a value in the G-code command
            if (!command.Parameters.TryGetValue(axisChar, out var value))
            {
                continue; // No value for this axis in the current command
            }

            // Validate and map the axis value
            var validatedValue = ValidateAndClampPosition(value);
            var scaledValue = ScaleAndClamp(validatedValue, axisConfig.ScaleFactor);

            var mapping = CreateMapping(
                axisConfig.Address,
                scaledValue,
                $"{axisName}_Position",
                axisConfig.ScaleFactor,
                value);

            if (!string.IsNullOrEmpty(axisConfig.Description))
            {
                var axisTypeStr = axisConfig.AxisType == AxisType.Rotational ? "rotational" : "linear";
                _logger.LogDebug("Mapping {AxisType} axis {Axis}: {Description}",
                    axisTypeStr, axisName, axisConfig.Description);
            }

            mappings.Add(mapping);
        }

        // Backward compatibility: support legacy Positions and RotationalAxes properties
        ProcessLegacyAxesIfPresent(command, mappings);
    }

    private void ProcessLegacyAxesIfPresent(GCodeCommand command, List<RegisterMapping> mappings)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        // Process legacy Positions configuration if present and Axes is empty
        if (_configuration.RegisterMappings.Axes.Count == 0 &&
            _configuration.RegisterMappings.Positions != null)
        {
            foreach (var posEntry in _configuration.RegisterMappings.Positions)
            {
                ProcessLegacyAxisMapping(command, posEntry.Key, posEntry.Value, mappings);
            }
        }

        // Process legacy RotationalAxes configuration if present and Axes is empty
        if (_configuration.RegisterMappings.Axes.Count == 0 &&
            _configuration.RegisterMappings.RotationalAxes != null)
        {
            foreach (var rotEntry in _configuration.RegisterMappings.RotationalAxes)
            {
                ProcessLegacyAxisMapping(command, rotEntry.Key, rotEntry.Value, mappings);
            }
        }
#pragma warning restore CS0618
    }

    private void ProcessLegacyAxisMapping(
        GCodeCommand command,
        string axisName,
        RegisterMappingConfig config,
        List<RegisterMapping> mappings)
    {
        if (string.IsNullOrEmpty(axisName) || axisName.Length != 1)
        {
            return;
        }

        var axisChar = axisName[0];
        if (!command.Parameters.TryGetValue(axisChar, out var value))
        {
            return;
        }

        var validatedValue = ValidateAndClampPosition(value);
        var scaledValue = ScaleAndClamp(validatedValue, config.ScaleFactor);

        var mapping = CreateMapping(
            config.Address,
            scaledValue,
            $"{axisName}_Position",
            config.ScaleFactor,
            value);

        if (!string.IsNullOrEmpty(config.Description))
        {
            _logger.LogDebug("Mapping axis {Axis} (legacy): {Description}", axisName, config.Description);
        }

        mappings.Add(mapping);
    }

    private void AddParameterMappings(GCodeCommand command, List<RegisterMapping> mappings)
    {
        AddFeedRateMapping(command, mappings);
        AddSpindleSpeedMapping(command, mappings);
    }

    private void AddFeedRateMapping(GCodeCommand command, List<RegisterMapping> mappings)
    {
        if (!command.Parameters.TryGetValue('F', out var feedRate))
        {
            return;
        }

        if (!_configuration.RegisterMappings.Parameters.TryGetValue("FeedRate", out var config))
        {
            _logger.LogWarning("FeedRate mapping not found in configuration");
            return;
        }

        var validatedValue = ValidateFeedRate(feedRate);
        var scaledValue = ScaleAndClamp(validatedValue, config.ScaleFactor);

        mappings.Add(CreateMapping(
            config.Address,
            scaledValue,
            "Feed_Rate",
            config.ScaleFactor,
            feedRate));
    }

    private void AddSpindleSpeedMapping(GCodeCommand command, List<RegisterMapping> mappings)
    {
        if (!command.Parameters.TryGetValue('S', out var spindleSpeed))
        {
            return;
        }

        if (!_configuration.RegisterMappings.Parameters.TryGetValue("SpindleSpeed", out var config))
        {
            _logger.LogWarning("SpindleSpeed mapping not found in configuration");
            return;
        }

        var validatedValue = ValidateSpindleSpeed(spindleSpeed);
        var scaledValue = ScaleAndClamp(validatedValue, config.ScaleFactor);

        mappings.Add(CreateMapping(
            config.Address,
            scaledValue,
            "Spindle_Speed",
            config.ScaleFactor,
            spindleSpeed));
    }

    private static RegisterMapping CreateMapping(
        ushort address,
        ushort value,
        string parameterName,
        decimal scaleFactor,
        decimal originalValue)
    {
        return new RegisterMapping
        {
            Address = address,
            Value = value,
            ParameterName = parameterName,
            ScaleFactor = scaleFactor,
            OriginalValue = originalValue,
            RegisterType = RegisterType.HoldingRegister
        };
    }

    private ushort ScaleAndClamp(decimal value, decimal scaleFactor)
    {
        var scaledValue = Math.Round(value * scaleFactor);

        if (scaledValue < 0)
        {
            _logger.LogWarning("Negative value {Value} clamped to 0 (scaled: {ScaledValue})", value, scaledValue);
            return 0;
        }

        if (scaledValue > ushort.MaxValue)
        {
            _logger.LogWarning("Value {Value} exceeds maximum (scaled: {ScaledValue}), clamped to {MaxValue}",
                value, scaledValue, ushort.MaxValue);
            return ushort.MaxValue;
        }

        return (ushort)scaledValue;
    }

    private decimal ValidateAndClampPosition(decimal value)
    {
        var rules = _configuration.ValidationRules.Position;

        if (rules.ClampNegativeToZero && value < 0)
        {
            _logger.LogWarning("Negative position value {Value} clamped to 0", value);
            return 0;
        }

        if (value < rules.MinValue)
        {
            _logger.LogWarning("Position value {Value} below minimum {Min}, clamped", value, rules.MinValue);
            return rules.MinValue;
        }

        if (value > rules.MaxValue)
        {
            _logger.LogWarning("Position value {Value} exceeds maximum {Max}, clamped", value, rules.MaxValue);
            return rules.MaxValue;
        }

        return value;
    }

    private decimal ValidateFeedRate(decimal value)
    {
        var rules = _configuration.ValidationRules.FeedRate;

        if (value < rules.MinValue)
        {
            _logger.LogWarning("Feed rate {Value} below minimum {Min}, clamped", value, rules.MinValue);
            return rules.MinValue;
        }

        if (value > rules.MaxValue)
        {
            _logger.LogWarning("Feed rate {Value} exceeds maximum {Max}, clamped", value, rules.MaxValue);
            return rules.MaxValue;
        }

        return value;
    }

    private decimal ValidateSpindleSpeed(decimal value)
    {
        var rules = _configuration.ValidationRules.SpindleSpeed;

        if (value < rules.MinValue)
        {
            _logger.LogWarning("Spindle speed {Value} below minimum {Min}, clamped", value, rules.MinValue);
            return rules.MinValue;
        }

        if (value > rules.MaxValue)
        {
            _logger.LogWarning("Spindle speed {Value} exceeds maximum {Max}, clamped", value, rules.MaxValue);
            return rules.MaxValue;
        }

        return value;
    }

    private bool IsValidToolNumber(int toolNumber)
    {
        var rules = _configuration.ValidationRules.ToolNumber;
        return toolNumber >= rules.MinValue && toolNumber <= rules.MaxValue;
    }

    private void ValidateConfiguration()
    {
        if (_configuration.RegisterMappings == null)
        {
            throw new ArgumentException("RegisterMappings cannot be null", nameof(_configuration));
        }

        if (_configuration.ValidationRules == null)
        {
            throw new ArgumentException("ValidationRules cannot be null", nameof(_configuration));
        }
    }

    private void AddDigitalOutputMappings(GCodeCommand command, List<RegisterMapping> mappings)
    {
        // Only process M-commands for digital outputs
        if (command.CommandType != "M" || !command.CommandNumber.HasValue)
        {
            return;
        }

        var mCode = command.CommandNumber.Value;

        foreach (var kvp in _configuration.RegisterMappings.DigitalOutputs)
        {
            var outputConfig = kvp.Value;

            // Check if this M-code triggers this output
            if (outputConfig.TriggerMCode.HasValue && outputConfig.TriggerMCode.Value == mCode)
            {
                var mapping = new RegisterMapping
                {
                    Address = outputConfig.Address,
                    Value = outputConfig.TriggerValue ? (ushort)1 : (ushort)0,
                    ParameterName = $"Digital_{kvp.Key}",
                    ScaleFactor = 1m,
                    OriginalValue = outputConfig.TriggerValue ? 1m : 0m,
                    RegisterType = Domain.Enums.RegisterType.Coil
                };

                if (!string.IsNullOrEmpty(outputConfig.Description))
                {
                    _logger.LogInformation("Digital I/O: {Name} = {Value} ({Description})",
                        kvp.Key, outputConfig.TriggerValue, outputConfig.Description);
                }

                mappings.Add(mapping);
            }

            // Handle M-code OFF commands (e.g., M9 turns off coolant)
            if (mCode == 9 && outputConfig.TriggerMCode == 8) // M9 turns off M8 (flood coolant)
            {
                mappings.Add(new RegisterMapping
                {
                    Address = outputConfig.Address,
                    Value = 0,
                    ParameterName = $"Digital_{kvp.Key}",
                    ScaleFactor = 1m,
                    OriginalValue = 0m,
                    RegisterType = Domain.Enums.RegisterType.Coil
                });

                _logger.LogInformation("Digital I/O: {Name} = OFF", kvp.Key);
            }

            if (mCode == 9 && outputConfig.TriggerMCode == 7) // M9 turns off M7 (mist coolant)
            {
                mappings.Add(new RegisterMapping
                {
                    Address = outputConfig.Address,
                    Value = 0,
                    ParameterName = $"Digital_{kvp.Key}",
                    ScaleFactor = 1m,
                    OriginalValue = 0m,
                    RegisterType = Domain.Enums.RegisterType.Coil
                });

                _logger.LogInformation("Digital I/O: {Name} = OFF", kvp.Key);
            }

            // M5 turns off spindle
            if (mCode == 5 && outputConfig.TriggerMCode == 3)
            {
                mappings.Add(new RegisterMapping
                {
                    Address = outputConfig.Address,
                    Value = 0,
                    ParameterName = $"Digital_{kvp.Key}",
                    ScaleFactor = 1m,
                    OriginalValue = 0m,
                    RegisterType = Domain.Enums.RegisterType.Coil
                });

                _logger.LogInformation("Digital I/O: {Name} = OFF", kvp.Key);
            }
        }

        // Handle spindle direction for M3/M4
        if ((mCode == 3 || mCode == 4) &&
            _configuration.RegisterMappings.DigitalOutputs.TryGetValue("SpindleDirection", out var dirConfig))
        {
            mappings.Add(new RegisterMapping
            {
                Address = dirConfig.Address,
                Value = mCode == 3 ? (ushort)1 : (ushort)0, // M3 = CW (1), M4 = CCW (0)
                ParameterName = "Digital_SpindleDirection",
                ScaleFactor = 1m,
                OriginalValue = mCode == 3 ? 1m : 0m,
                RegisterType = Domain.Enums.RegisterType.Coil
            });

            _logger.LogInformation("Spindle direction: {Direction}", mCode == 3 ? "CW (Clockwise)" : "CCW (Counter-Clockwise)");
        }
    }

    private void LogConfigurationSummary()
    {
        var axisCount = _configuration.RegisterMappings.Axes.Count;
        var linearCount = _configuration.RegisterMappings.Axes.Count(a => a.Value.AxisType == AxisType.Linear);
        var rotationalCount = _configuration.RegisterMappings.Axes.Count(a => a.Value.AxisType == AxisType.Rotational);
        var customAxisCount = _configuration.RegisterMappings.Axes.Count(a => a.Value.AxisType == AxisType.Custom);

#pragma warning disable CS0618
        // Backward compatibility logging
        if (axisCount == 0)
        {
            var legacyPosCount = _configuration.RegisterMappings.Positions?.Count ?? 0;
            var legacyRotCount = _configuration.RegisterMappings.RotationalAxes?.Count ?? 0;
            axisCount = legacyPosCount + legacyRotCount;
            linearCount = legacyPosCount;
            rotationalCount = legacyRotCount;

            if (axisCount > 0)
            {
                _logger.LogWarning("Using legacy Positions/RotationalAxes configuration. Consider migrating to Axes configuration.");
            }
        }
#pragma warning restore CS0618

        if (customAxisCount > 0)
        {
            _logger.LogInformation(
                "PLCDataMapper initialized with {AxisCount} axes ({LinearCount} linear + {RotationalCount} rotational + {CustomCount} custom), " +
                "{CommandCount} commands, {ParameterCount} parameters, {DigitalIOCount} digital I/Os",
                axisCount,
                linearCount,
                rotationalCount,
                customAxisCount,
                _configuration.RegisterMappings.Commands.Count,
                _configuration.RegisterMappings.Parameters.Count,
                _configuration.RegisterMappings.DigitalOutputs.Count);
        }
        else if (axisCount > 0)
        {
            _logger.LogInformation(
                "PLCDataMapper initialized with {AxisCount} axes ({LinearCount} linear + {RotationalCount} rotational), " +
                "{CommandCount} commands, {ParameterCount} parameters, {DigitalIOCount} digital I/Os",
                axisCount,
                linearCount,
                rotationalCount,
                _configuration.RegisterMappings.Commands.Count,
                _configuration.RegisterMappings.Parameters.Count,
                _configuration.RegisterMappings.DigitalOutputs.Count);
        }
        else
        {
            _logger.LogWarning("No axes configured!");
        }

        if (_configuration.RegisterMappings.CustomRegisters.Count > 0)
        {
            _logger.LogInformation("Custom registers configured: {Count}",
                _configuration.RegisterMappings.CustomRegisters.Count);
        }

        // Log configured axis names for visibility
        if (axisCount > 0)
        {
            var axisNames = string.Join(", ", _configuration.RegisterMappings.Axes.Keys);
            _logger.LogDebug("Configured axes: {AxisNames}", axisNames);
        }
    }
}
