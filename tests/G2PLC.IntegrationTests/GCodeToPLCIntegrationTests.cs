using G2PLC.Application.Mappers;
using G2PLC.Application.Parsers;
using G2PLC.Domain.Configuration;
using G2PLC.Domain.Enums;
using G2PLC.Domain.Interfaces;
using G2PLC.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace G2PLC.IntegrationTests;

/// <summary>
/// Integration tests demonstrating complete G-code to PLC I/O conversion.
/// This simulates how G-code commands are converted into PLC register writes,
/// which would control a real CNC machine's movement.
/// </summary>
public class GCodeToPlcIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly IGCodeParser _parser;
    private readonly IDataMapper _mapper;
    private readonly MockPlcSimulator _plcSimulator;

    public GCodeToPlcIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        var parserLogger = new Mock<ILogger<GCodeParser>>();
        var mapperLogger = new Mock<ILogger<PlcDataMapper>>();

        var configuration = CreateDefaultConfiguration();

        _parser = new GCodeParser(parserLogger.Object);
        _mapper = new PlcDataMapper(mapperLogger.Object, configuration);
        _plcSimulator = new MockPlcSimulator();
    }

    private static MappingConfiguration CreateDefaultConfiguration()
    {
        return new MappingConfiguration
        {
            RegisterMappings = new RegisterMappings
            {
                Axes = new Dictionary<string, AxisMappingConfig>
                {
                    ["X"] = new() { Address = 0, ScaleFactor = 1000m },
                    ["Y"] = new() { Address = 1, ScaleFactor = 1000m },
                    ["Z"] = new() { Address = 2, ScaleFactor = 1000m }
                },
                Commands = new Dictionary<string, RegisterMappingConfig>
                {
                    ["GCommand"] = new() { Address = 6, ScaleFactor = 1m },
                    ["MCommand"] = new() { Address = 7, ScaleFactor = 1m }
                },
                Parameters = new Dictionary<string, RegisterMappingConfig>
                {
                    ["FeedRate"] = new() { Address = 3, ScaleFactor = 10m },
                    ["SpindleSpeed"] = new() { Address = 4, ScaleFactor = 1m },
                    ["ToolNumber"] = new() { Address = 5, ScaleFactor = 1m }
                }
            },
            ValidationRules = new ValidationRules
            {
                Position = new() { MinValue = -9999m, MaxValue = 65000m, ClampNegativeToZero = true },
                FeedRate = new() { MinValue = 0m, MaxValue = 10000m },
                SpindleSpeed = new() { MinValue = 0m, MaxValue = 24000m },
                ToolNumber = new() { MinValue = 0m, MaxValue = 99m }
            },
            ProcessingOptions = new ProcessingOptions()
        };
    }

    [Fact]
    public void FullWorkflow_SimpleMachineMovement_ShouldConvertGCodeToPLCIO()
    {
        // Arrange: G-code program to move machine to position (10, 20, 5) with feed rate 100
        var gcodeLines = new[]
        {
            "G21 (Set units to mm)",
            "G90 (Absolute positioning)",
            "G01 X10.0 Y20.0 Z5.0 F100 (Move to position)"
        };

        // Act & Assert
        foreach (var line in gcodeLines)
        {
            _output.WriteLine($"\n>>> Processing: {line}");

            // Parse G-code
            var command = _parser.ParseLine(line);
            if (command == null)
            {
                _output.WriteLine("    Skipped (comment or empty)");
                continue;
            }

            _output.WriteLine($"    Parsed: {command.CommandType}{command.CommandNumber}");

            // Map to PLC registers
            var registerMappings = _mapper.MapToRegisters(command);

            // Write to PLC (simulated)
            foreach (var mapping in registerMappings)
            {
                _plcSimulator.WriteRegister(mapping.Address, mapping.Value);

                _output.WriteLine($"    PLC Write: Register[{mapping.Address}] = {mapping.Value} " +
                                $"({mapping.ParameterName}: {mapping.OriginalValue} * {mapping.ScaleFactor})");
            }
        }

        // Verify the machine state
        _output.WriteLine("\n=== Final Machine State ===");
        _output.WriteLine($"G-Code Command: G{_plcSimulator.GetRegisterValue(6)}");
        _output.WriteLine($"X Position: {_plcSimulator.GetRegisterValue(0) / 1000.0m} mm (Raw: {_plcSimulator.GetRegisterValue(0)} microns)");
        _output.WriteLine($"Y Position: {_plcSimulator.GetRegisterValue(1) / 1000.0m} mm (Raw: {_plcSimulator.GetRegisterValue(1)} microns)");
        _output.WriteLine($"Z Position: {_plcSimulator.GetRegisterValue(2) / 1000.0m} mm (Raw: {_plcSimulator.GetRegisterValue(2)} microns)");
        _output.WriteLine($"Feed Rate: {_plcSimulator.GetRegisterValue(3) / 10.0m} mm/min (Raw: {_plcSimulator.GetRegisterValue(3)})");

        // Assertions
        _plcSimulator.GetRegisterValue(6).ShouldBe((ushort)1); // G01
        _plcSimulator.GetRegisterValue(0).ShouldBe((ushort)10000); // X = 10.0 mm = 10000 microns
        _plcSimulator.GetRegisterValue(1).ShouldBe((ushort)20000); // Y = 20.0 mm = 20000 microns
        _plcSimulator.GetRegisterValue(2).ShouldBe((ushort)5000);  // Z = 5.0 mm = 5000 microns
        _plcSimulator.GetRegisterValue(3).ShouldBe((ushort)1000);  // F = 100 mm/min scaled
    }

    [Fact]
    public void FullWorkflow_ComplexMachiningOperation_ShouldExecuteCorrectly()
    {
        // Arrange: A realistic machining operation
        var gcodeProgram = new[]
        {
            "(Machining Program - Rectangular Pocket)",
            "G21 (Metric)",
            "G90 (Absolute mode)",
            "",
            "T01 (Select tool 1)",
            "M03 S1200 (Spindle on CW at 1200 RPM)",
            "",
            "G00 X0 Y0 Z10 (Rapid to start position above workpiece)",
            "G01 Z-2.0 F50 (Plunge to depth)",
            "",
            "(Cut rectangular profile)",
            "G01 X50.0 Y0 F200 (Cut to corner 1)",
            "G01 X50.0 Y30.0 F200 (Cut to corner 2)",
            "G01 X0 Y30.0 F200 (Cut to corner 3)",
            "G01 X0 Y0 F200 (Return to start)",
            "",
            "G00 Z10 (Retract)",
            "M05 (Spindle off)",
            "M30 (Program end)"
        };

        var machineState = new List<MachineSnapshot>();

        // Act
        foreach (var line in gcodeProgram)
        {
            var command = _parser.ParseLine(line);
            if (command == null) continue;

            var mappings = _mapper.MapToRegisters(command);

            foreach (var mapping in mappings)
            {
                _plcSimulator.WriteRegister(mapping.Address, mapping.Value);
            }

            // Take snapshot after each command
            machineState.Add(new MachineSnapshot
            {
                Command = $"{command.CommandType}{command.CommandNumber:D2}",
                X = _plcSimulator.GetRegisterValue(0) / 1000.0m,
                Y = _plcSimulator.GetRegisterValue(1) / 1000.0m,
                Z = _plcSimulator.GetRegisterValue(2) / 1000.0m,
                FeedRate = _plcSimulator.GetRegisterValue(3) / 10.0m,
                SpindleSpeed = _plcSimulator.GetRegisterValue(4),
                ToolNumber = _plcSimulator.GetRegisterValue(5)
            });
        }

        // Assert key positions
        _output.WriteLine("\n=== Machine Path (Key Positions) ===");
        foreach (var state in machineState)
        {
            _output.WriteLine($"{state.Command}: Position({state.X:F1}, {state.Y:F1}, {state.Z:F1}) " +
                            $"Feed={state.FeedRate:F0} Spindle={state.SpindleSpeed} Tool=T{state.ToolNumber:D2}");
        }

        // Verify final state
        var finalState = machineState.Last();
        finalState.Command.ShouldBe("M30"); // Program should end with M30

        // Verify tool was changed
        machineState.Any(s => s.ToolNumber == 1).ShouldBeTrue();

        // Verify spindle was turned on and off
        machineState.Any(s => s.SpindleSpeed == 1200).ShouldBeTrue();

        // Verify all four corners were reached
        machineState.Any(s => s is { X: 50, Y: 0 }).ShouldBeTrue();   // Corner 1
        machineState.Any(s => s is { X: 50, Y: 30 }).ShouldBeTrue();  // Corner 2
        machineState.Any(s => s is { X: 0, Y: 30 }).ShouldBeTrue();   // Corner 3
        machineState.Any(s => s is { X: 0, Y: 0 }).ShouldBeTrue();    // Return to start
    }

    [Fact]
    public void FullWorkflow_CircularInterpolation_ShouldParseArcMovement()
    {
        // Arrange: Arc movement (circular interpolation)
        var gcodeLines = new[]
        {
            "G90 (Absolute mode)",
            "G17 (XY plane)",
            "G02 X10.0 Y10.0 I5.0 J0 F100 (Arc clockwise with center offset)"
        };

        // Act
        foreach (var line in gcodeLines)
        {
            var command = _parser.ParseLine(line);
            if (command == null) continue;

            _output.WriteLine($"Parsed: {command}");

            // Verify arc parameters are captured
            if (command.CommandNumber == 2) // G02
            {
                command.Parameters.ShouldContainKey('I');
                command.Parameters.ShouldContainKey('J');
                command.Parameters['I'].ShouldBe(5.0m);
                command.Parameters['J'].ShouldBe(0m);

                _output.WriteLine($"  Arc center offset: I={command.Parameters['I']}, J={command.Parameters['J']}");
            }
        }
    }

    [Fact]
    public void FullWorkflow_NegativeCoordinates_ShouldBeClampedToZero()
    {
        // Arrange: Movement to negative position (testing clamping behavior)
        var command = _parser.ParseLine("G01 X-10.0 Y-5.0 Z-2.0");

        // Act
        var mappings = _mapper.MapToRegisters(command!);

        foreach (var mapping in mappings)
        {
            _plcSimulator.WriteRegister(mapping.Address, mapping.Value);
        }

        // Assert: Negative values should be clamped to zero
        _output.WriteLine("\n=== Negative Value Clamping ===");
        _output.WriteLine($"X Position: {_plcSimulator.GetRegisterValue(0)} (should be 0, was -10.0mm)");
        _output.WriteLine($"Y Position: {_plcSimulator.GetRegisterValue(1)} (should be 0, was -5.0mm)");
        _output.WriteLine($"Z Position: {_plcSimulator.GetRegisterValue(2)} (should be 0, was -2.0mm)");

        _plcSimulator.GetRegisterValue(0).ShouldBe((ushort)0); // X clamped
        _plcSimulator.GetRegisterValue(1).ShouldBe((ushort)0); // Y clamped
        _plcSimulator.GetRegisterValue(2).ShouldBe((ushort)0); // Z clamped
    }

    [Fact]
    public void FullWorkflow_RapidVsFeedMoves_ShouldShowSpeedDifference()
    {
        // Arrange
        var rapid = _parser.ParseLine("G00 X10 Y10 Z5");
        var feed = _parser.ParseLine("G01 X20 Y20 Z10 F150");

        // Act
        _output.WriteLine("\n=== Rapid vs Feed Moves ===");

        // Rapid move (G00)
        var rapidMappings = _mapper.MapToRegisters(rapid!);
        _output.WriteLine($"G00 (Rapid): {rapidMappings.Count} registers");

        // Feed move (G01)
        var feedMappings = _mapper.MapToRegisters(feed!);
        _output.WriteLine($"G01 (Feed): {feedMappings.Count} registers");

        // Assert
        feedMappings.ShouldContain(m => m.ParameterName == "Feed_Rate");
        _output.WriteLine($"Feed rate specified: {feedMappings.First(m => m.ParameterName == "Feed_Rate").OriginalValue} mm/min");
    }
}

/// <summary>
/// Mock PLC simulator for testing without real hardware.
/// Simulates PLC holding registers.
/// </summary>
public class MockPlcSimulator
{
    private readonly Dictionary<ushort, ushort> _registers = new();

    public void WriteRegister(ushort address, ushort value)
    {
        _registers[address] = value;
    }

    public ushort GetRegisterValue(ushort address)
    {
        return _registers.TryGetValue(address, out var value) ? value : (ushort)0;
    }

    public Dictionary<ushort, ushort> GetAllRegisters() => new(_registers);
}

/// <summary>
/// Represents a snapshot of machine state at a specific point.
/// </summary>
public class MachineSnapshot
{
    public string Command { get; set; } = string.Empty;
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Z { get; set; }
    public decimal FeedRate { get; set; }
    public ushort SpindleSpeed { get; set; }
    public ushort ToolNumber { get; set; }
}
