using Shouldly;
using G2PLC.E2ETests.Extensions;
using Xunit;

namespace G2PLC.E2ETests;

/// <summary>
/// End-to-end tests for G-Code to Modbus PLC communication.
/// These tests verify the complete pipeline from G-Code parsing to PLC register writes.
/// </summary>
public class GCodeModbusE2ETests : E2ETestBase
{
    [Fact]
    public async Task GCodeToModbus_SimpleLinearMove_WritesCorrectRegisters()
    {
        // Arrange
        var gcode = "G01 X100.5 Y200.25 Z50.0 F1500";
        var commands = await ParseGCodeTextAsync(gcode);

        using var modbusClient = CreateModbusClient();
        await modbusClient.ConnectAsync();

        ResetPlcState();

        // Act - Write axis positions to registers
        var command = commands.First();
        foreach (var param in command.Parameters)
        {
            ushort address = GetRegisterAddress(param.Key);
            ushort value = unchecked((ushort)(double)(param.Value * 1000)); // Scale to microns
            await modbusClient.WriteHoldingRegisterAsync(address, value);
        }

        // Assert - Verify registers were written correctly
        var registers = GetModbusRegisters();

        registers.ShouldContainKey((ushort)100); // X register
        registers[(ushort)100].ShouldBe(unchecked((ushort)100500)); // 100.5 * 1000

        registers.ShouldContainKey((ushort)101); // Y register
        registers[(ushort)101].ShouldBe(unchecked((ushort)200250)); // 200.25 * 1000 (wraps around)

        registers.ShouldContainKey((ushort)102); // Z register
        registers[(ushort)102].ShouldBe((ushort)50000); // 50.0 * 1000

        registers.ShouldContainKey((ushort)110); // F register
        registers[(ushort)110].ShouldBe(unchecked((ushort)1500000)); // 1500 * 1000 (wraps around)
    }

    [Fact]
    public async Task GCodeToModbus_MultipleCommands_WritesAllPositions()
    {
        // Arrange
        var gcode = @"
G00 X10 Y10 Z0
G01 X20 Y20 Z5 F500
G01 X30 Y30 Z10 F500
";
        var commands = await ParseGCodeTextAsync(gcode);

        using var modbusClient = CreateModbusClient();
        await modbusClient.ConnectAsync();

        ResetPlcState();

        // Act - Process all commands
        foreach (var command in commands)
        {
            foreach (var param in command.Parameters)
            {
                ushort address = GetRegisterAddress(param.Key);
                ushort value = unchecked((ushort)(double)(param.Value * 1000));
                await modbusClient.WriteHoldingRegisterAsync(address, value);
            }
        }

        // Assert - Verify final positions (last command wins)
        var registers = GetModbusRegisters();

        registers[(ushort)100].ShouldBe((ushort)30000); // X = 30mm
        registers[(ushort)101].ShouldBe((ushort)30000); // Y = 30mm
        registers[(ushort)102].ShouldBe((ushort)10000); // Z = 10mm
    }

    [Fact]
    public async Task GCodeToModbus_ComplexProgram_MapsAllAxes()
    {
        // Arrange
        var gcode = @"
G00 X0 Y0 Z50
G01 X100 Y100 Z0 F1000
G02 X150 Y150 I50 J0 F800
G01 A45 B30 C60
";
        var commands = await ParseGCodeTextAsync(gcode);

        using var modbusClient = CreateModbusClient();
        await modbusClient.ConnectAsync();

        ResetPlcState();

        // Act - Process all commands
        foreach (var command in commands)
        {
            foreach (var param in command.Parameters)
            {
                ushort address = GetRegisterAddress(param.Key);
                ushort value = unchecked((ushort)(double)(param.Value * 1000));
                await modbusClient.WriteHoldingRegisterAsync(address, value);
            }
        }

        // Assert - Check all 6 axes were written
        var registers = GetAllModbusRegisters(); // Use GetAllModbusRegisters to include Z=0

        // Linear axes (last values)
        registers[(ushort)100].ShouldBe(unchecked((ushort)150000)); // X = 150mm (wraps)
        registers[(ushort)101].ShouldBe(unchecked((ushort)150000)); // Y = 150mm (wraps)
        registers[(ushort)102].ShouldBe((ushort)0);     // Z = 0mm

        // Rotary axes
        registers[(ushort)103].ShouldBe((ushort)45000); // A = 45 degrees
        registers[(ushort)104].ShouldBe((ushort)30000); // B = 30 degrees
        registers[(ushort)105].ShouldBe((ushort)60000); // C = 60 degrees
    }

    [Fact]
    public async Task GCodeToModbus_VerifyWriteReadback_ConfirmsCorrectValue()
    {
        // Arrange
        var gcode = "G01 X123.456";
        var commands = await ParseGCodeTextAsync(gcode);

        using var modbusClient = CreateModbusClient();
        await modbusClient.ConnectAsync();

        ResetPlcState();

        var command = commands.First();
        var xValue = command.Parameters['X'];
        ushort address = 100; // X axis
        ushort expectedValue = unchecked((ushort)(double)(xValue * 1000));

        // Act - Write and read back
        await modbusClient.WriteHoldingRegisterAsync(address, expectedValue);
        var readbackValue = await modbusClient.ReadHoldingRegisterAsync(address);

        // Assert
        readbackValue.ShouldBe(expectedValue);
    }

    [Fact]
    public async Task GCodeToModbus_CustomRegisterMapping_WritesToCorrectAddresses()
    {
        // Arrange - Custom mapping
        var customMapping = new Dictionary<char, ushort>
        {
            ['X'] = 1000,
            ['Y'] = 1001,
            ['Z'] = 1002,
            ['F'] = 1100
        };

        var gcode = "G01 X50 Y75 Z25 F600";
        var commands = await ParseGCodeTextAsync(gcode);

        using var modbusClient = CreateModbusClient();
        await modbusClient.ConnectAsync();

        ResetPlcState();

        // Act - Write using custom mapping
        var command = commands.First();
        foreach (var param in command.Parameters)
        {
            if (customMapping.ContainsKey(param.Key))
            {
                ushort address = customMapping[param.Key];
                ushort value = unchecked((ushort)(double)(param.Value * 1000));
                await modbusClient.WriteHoldingRegisterAsync(address, value);
            }
        }

        // Assert - Verify custom addresses
        var registers = GetModbusRegisters();

        registers.ShouldContainKey((ushort)1000);
        registers[(ushort)1000].ShouldBe((ushort)50000);

        registers.ShouldContainKey((ushort)1001);
        registers[(ushort)1001].ShouldBe(unchecked((ushort)75000)); // 75000

        registers.ShouldContainKey((ushort)1002);
        registers[(ushort)1002].ShouldBe((ushort)25000);
    }

    /// <summary>
    /// Helper method to get register address for an axis.
    /// Standard mapping: X=100, Y=101, Z=102, A=103, B=104, C=105, F=110, S=111, I=106, J=107
    /// </summary>
    private ushort GetRegisterAddress(char axis)
    {
        return axis switch
        {
            'X' => 100,
            'Y' => 101,
            'Z' => 102,
            'A' => 103,
            'B' => 104,
            'C' => 105,
            'I' => 106, // Arc center offset X
            'J' => 107, // Arc center offset Y
            'F' => 110, // Feedrate
            'S' => 111, // Spindle speed
            _ => 100
        };
    }
}
