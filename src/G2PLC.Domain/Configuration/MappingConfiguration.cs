namespace G2PLC.Domain.Configuration;

/// <summary>
/// Complete configuration for G-code to PLC register mappings.
/// </summary>
public class MappingConfiguration
{
    public RegisterMappings RegisterMappings { get; set; } = new();
    public ValidationRules ValidationRules { get; set; } = new();
    public ProcessingOptions ProcessingOptions { get; set; } = new();
    public OpcUaConfiguration? OpcUaConfiguration { get; set; }
}

/// <summary>
/// Defines all register address mappings.
/// </summary>
public class RegisterMappings
{
    /// <summary>
    /// Dynamic axis mappings - supports any axis letter (X, Y, Z, A, B, C, U, V, W, or custom).
    /// The key is the axis letter/name (single character recommended for G-code compatibility).
    /// </summary>
    public Dictionary<string, AxisMappingConfig> Axes { get; set; } = new();

    /// <summary>
    /// G and M command registers.
    /// </summary>
    public Dictionary<string, RegisterMappingConfig> Commands { get; set; } = new();

    /// <summary>
    /// Motion parameters (feed rate, spindle speed, tool number, etc.).
    /// </summary>
    public Dictionary<string, RegisterMappingConfig> Parameters { get; set; } = new();

    /// <summary>
    /// Digital I/O mappings (coolant, auxiliary outputs, etc.).
    /// </summary>
    public Dictionary<string, DigitalIoMappingConfig> DigitalOutputs { get; set; } = new();

    /// <summary>
    /// Custom register mappings for machine-specific parameters.
    /// </summary>
    public Dictionary<string, RegisterMappingConfig> CustomRegisters { get; set; } = new();

    /// <summary>
    /// LSF operation mappings for Howick machine (SWAGE, LIP_CUT, NOTCH, DIMPLE, END_TRUSS).
    /// </summary>
    public Dictionary<string, LsfOperationMappingConfig> LsfOperations { get; set; } = new();

    /// <summary>
    /// Legacy property for backward compatibility. Maps to Axes with AxisType = Linear.
    /// </summary>
    [Obsolete("Use Axes instead. This property is maintained for backward compatibility.")]
    public Dictionary<string, RegisterMappingConfig>? Positions { get; set; }

    /// <summary>
    /// Legacy property for backward compatibility. Maps to Axes with AxisType = Rotational.
    /// </summary>
    [Obsolete("Use Axes instead. This property is maintained for backward compatibility.")]
    public Dictionary<string, RegisterMappingConfig>? RotationalAxes { get; set; }
}

/// <summary>
/// Validation rules for G-code values.
/// </summary>
public class ValidationRules
{
    public ValueRange Position { get; set; } = new();
    public ValueRange FeedRate { get; set; } = new();
    public ValueRange SpindleSpeed { get; set; } = new();
    public ValueRange ToolNumber { get; set; } = new();
}

/// <summary>
/// Defines acceptable range for a value.
/// </summary>
public class ValueRange
{
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public bool ClampNegativeToZero { get; set; }
}

/// <summary>
/// Processing options for command execution.
/// </summary>
public class ProcessingOptions
{
    public int DelayBetweenCommandsMs { get; set; } = 100;
    public bool VerifyWritesEnabled { get; set; }
    public bool ContinueOnError { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
}
