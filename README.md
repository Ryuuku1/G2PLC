# G2PLC - CNC to PLC Communication System

A production-ready .NET 9.0 system that bridges CNC programming and industrial control, supporting both **G-code** and **LSF (Light Steel Framing)** formats with **Modbus TCP** and **OPC UA** communication protocols.

## 🎯 Applications

- **Console Application**: Command-line tool for automated processing
- **WPF HMI Application**: Modern graphical interface for manual operation and monitoring

Both applications share the same core libraries and support G-code and LSF file formats.

## Architecture

This project follows **Clean Architecture** principles with clear separation of concerns:

```
G2PLC/
├── src/
│   ├── G2PLC.Domain/          # Domain layer - entities, interfaces, enums
│   ├── G2PLC.Application/     # Application layer - business logic, parsers, mappers
│   ├── G2PLC.Infrastructure/  # Infrastructure layer - Modbus, OPC UA, config
│   ├── G2PLC.Console/         # Console application
│   └── G2PLC.UI.Wpf/          # WPF HMI application (MVVM)
└── tests/                     # Unit and integration tests
```

### Layer Responsibilities

**Domain Layer** (`G2PLC.Domain`)
- Domain models: `GCodeCommand`, `RegisterMapping`
- Enums: `RegisterType`
- Interfaces: `IGCodeParser`, `IDataMapper`, `IModbusClient`, `ILogger`
- No external dependencies

**Application Layer** (`G2PLC.Application`)
- `GCodeParser`: Parses G-code text using regex patterns
- `PLCDataMapper`: Maps G-code commands to PLC register addresses with scaling
- References: Domain layer only

**Infrastructure Layer** (`G2PLC.Infrastructure`)
- `ModbusClientWrapper`: Modbus TCP communication using NModbus
- `FileLogger`: File-based logging implementation
- `AppConfigurationManager`: Configuration management
- `ValidationHelper`: Data validation utilities
- Custom exceptions: `ModbusConnectionException`, `ModbusWriteException`, etc.
- References: Domain and Application layers

**Console Application** (`G2PLC.Console`)
- Command-line interface for automated processing
- Batch file processing
- Progress display and error handling

**WPF HMI Application** (`G2PLC.UI.Wpf`)
- Modern WPF interface with MVVM pattern
- Real-time monitoring and control
- Visual progress tracking and logging
- File browsing and PLC configuration
- **Multi-language support** (English, Portuguese - Portugal)
- **Dark/Light theme switching**
- See [HMI_README.md](HMI_README.md) for details

## Features

### Core Capabilities
- **G-code Parsing**: Supports G, M, and T commands with unlimited axes
- **LSF (Howick) Parsing**: Light Steel Framing component data for Howick V1 machines
- **Modbus TCP Communication**: Industry-standard protocol (NModbus)
- **OPC UA Communication**: Modern industrial protocol (OPC Foundation SDK)
- **Configurable Mapping**: Register addresses and scaling factors
- **Comprehensive Logging**: File-based and console logging
- **Error Handling**: Custom exceptions with detailed context
- **Validation**: Input validation for all parameters
- **Graphical HMI**: User-friendly WPF application with real-time monitoring
- **Multi-language Support**: English and Portuguese (Portugal) - see [LOCALIZATION.md](LOCALIZATION.md)
- **Theme Support**: Dark and Light themes with runtime switching - see [THEME_SYSTEM.md](THEME_SYSTEM.md)

## Supported G-codes

**G-codes (Motion)**
- G00: Rapid positioning
- G01: Linear interpolation
- G02: Circular interpolation CW
- G03: Circular interpolation CCW
- G17/G18/G19: Plane selection
- G20/G21: Unit selection (inches/mm)
- G28/G30: Return to reference point
- G90/G91: Absolute/incremental positioning

**M-codes (Machine Functions)**
- M00: Program stop
- M01: Optional stop
- M03: Spindle on CW
- M04: Spindle on CCW
- M05: Spindle off
- M30: Program end

**T-codes (Tool Selection)**
- T01-T99: Tool selection

## Requirements

- .NET 9.0 SDK or higher
- PLC with Modbus TCP support

## Building the Application

```bash
# Restore NuGet packages
dotnet restore

# Build in Release mode
dotnet build --configuration Release

# Build and run
dotnet run --project src/G2PLC.Console
```

## Configuration

Edit `App.config` in the console application to configure:

### PLC Connection Settings
```xml
<add key="PLC_IpAddress" value="192.168.1.100" />
<add key="PLC_Port" value="502" />
<add key="PLC_SlaveId" value="1" />
<add key="PLC_ConnectionTimeout" value="5000" />
```

### Register Address Mapping
```xml
<!-- 0-based addressing: Register 40001 = Address 0 -->
<add key="Register_XPosition" value="0" />
<add key="Register_YPosition" value="1" />
<add key="Register_ZPosition" value="2" />
<add key="Register_FeedRate" value="3" />
<add key="Register_SpindleSpeed" value="4" />
<add key="Register_ToolNumber" value="5" />
<add key="Register_GCommand" value="6" />
<add key="Register_MCommand" value="7" />
```

### Scaling Factors
```xml
<!-- Convert mm to microns -->
<add key="Scale_Position" value="1000" />
<!-- Scale feed rate -->
<add key="Scale_FeedRate" value="10" />
```

### Processing Settings
```xml
<!-- Delay between commands (ms) -->
<add key="Processing_DelayBetweenCommands_Ms" value="100" />
<!-- Verify writes by reading back -->
<add key="Processing_VerifyWrites" value="false" />
```

### Logging Settings
```xml
<add key="Log_FilePath" value="logs/gcode_to_plc.log" />
<add key="Log_Level" value="Info" />
```

## Usage

### Basic Usage
```bash
# Run with default settings from App.config
dotnet run --project src/G2PLC.Console

# Or run the compiled executable
.\src\G2PLC.Console\bin\Release\net9.0\G2PLC.Console.exe
```

### Command-line Arguments
```bash
# Override PLC IP address
dotnet run --project src/G2PLC.Console -- --ip 192.168.1.50

# Override port
dotnet run --project src/G2PLC.Console -- --port 5020

# Specify G-code file
dotnet run --project src/G2PLC.Console -- --file myprogram.gcode

# Enable verbose output
dotnet run --project src/G2PLC.Console -- --verbose

# Enable write verification
dotnet run --project src/G2PLC.Console -- --verify

# Combine multiple arguments
dotnet run --project src/G2PLC.Console -- --ip 192.168.1.50 --file myprogram.gcode --verbose --verify

# Display help
dotnet run --project src/G2PLC.Console -- --help
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Configuration error |
| 2 | File not found |
| 3 | PLC connection failed |
| 4 | Parse error |
| 5 | Write error |
| 99 | Unhandled exception |

## Sample G-code File

A sample G-code file (`sample_program.gcode`) is included demonstrating:
- Unit and positioning mode setup
- Spindle control
- Rapid and linear moves
- Circular interpolation
- Tool changes
- Multiple drilling operations

## Modbus Address Mapping

The application uses **0-based addressing** internally:
- Modbus register 40001 → Address 0
- Modbus register 40002 → Address 1
- Coil 00001 → Address 0

When configuring register addresses in `App.config`, use the 0-based address.

## Scaling Implementation

Scaling is applied during the mapping phase:
1. Original G-code value is preserved in `RegisterMapping.OriginalValue`
2. Scaling factor is applied: `scaledValue = originalValue × scaleFactor`
3. Result is rounded and clamped to ushort range (0-65535)
4. Negative values are clamped to 0 with a warning

**Note**: For machines requiring negative positioning values, consider implementing:
- Two's complement encoding (values > 32767 represent negative)
- Separate sign bit register
- Offset-based approach

## Error Handling

The application includes comprehensive error handling:
- Custom exceptions with context (address, value, operation)
- Automatic logging of all errors with stack traces
- Connection state management
- Graceful degradation on non-critical errors

## Logging

Logs are written to the configured log file with format:
```
[YYYY-MM-DD HH:mm:ss.fff] [THREAD-ID] [LEVEL] Message
```

Log levels: Debug, Info, Warning, Error

Critical errors are also displayed in the console with color coding.

## Performance

- Typical throughput: 100+ commands/second (depending on PLC response time)
- Configurable delay between commands for PLC processing
- Entire G-code file loaded into memory (suitable for typical CNC programs)

## Testing Without a PLC

To test the application without a real PLC:
1. Use a Modbus TCP simulator (e.g., ModRSsim2, pyModSlave)
2. Configure the simulator to listen on port 502
3. Update `App.config` with the simulator's IP address
4. Run the application with `--verbose` to see all operations

## Extending the Application

### Adding New G-codes
1. Update `GCodeParser.IsValidGCommand()` to include the new G-code number
2. Add mapping logic in `PLCDataMapper.MapToRegisters()` if special handling is needed

### Adding New Registers
1. Add configuration key in `App.config`
2. Add property in `PLCDataMapper`
3. Update `ConfigureRegisterAddresses()` method
4. Add mapping logic in `MapToRegisters()`

### Custom Logging
Implement `ILogger` interface with your preferred logging framework (e.g., Serilog, NLog).

## License

This project is provided as-is for industrial automation purposes.

## Support

For issues and questions, please refer to the inline code documentation (XML comments).

---

**Built with .NET 9.0 | Clean Architecture | Production-Ready**
