using Shouldly;
using G2PLC.E2ETests.Extensions;
using Xunit;

namespace G2PLC.E2ETests;

/// <summary>
/// End-to-end tests for G-Code to OPC UA communication.
/// These tests verify the complete pipeline from G-Code parsing to OPC UA node writes.
/// </summary>
public class GCodeOpcUaE2ETests : E2ETestBase
{
    [Fact]
    public async Task GCodeToOpcUa_SimpleLinearMove_WritesCorrectNodes()
    {
        // Arrange
        var gcode = "G01 X100.5 Y200.25 Z50.0 F1500";
        var commands = await ParseGCodeTextAsync(gcode);

        using var opcClient = CreateOpcUaClient();
        await opcClient.ConnectAsync();

        ResetPlcState();

        // Act - Write axis positions to nodes
        var command = commands.First();
        foreach (var param in command.Parameters)
        {
            string nodeId = $"ns=2;s=CNC.Axes.{param.Key}.Position";
            await opcClient.WriteNodeValueAsync(nodeId, param.Value * 1000); // Scale to microns
        }

        // Assert - Verify nodes were written correctly
        var nodes = GetOpcUaNodes();

        nodes.ShouldContainKey("ns=2;s=CNC.Axes.X.Position");
        Convert.ToDouble(nodes["ns=2;s=CNC.Axes.X.Position"]).ShouldBe(100500.0);

        nodes.ShouldContainKey("ns=2;s=CNC.Axes.Y.Position");
        Convert.ToDouble(nodes["ns=2;s=CNC.Axes.Y.Position"]).ShouldBe(200250.0);

        nodes.ShouldContainKey("ns=2;s=CNC.Axes.Z.Position");
        Convert.ToDouble(nodes["ns=2;s=CNC.Axes.Z.Position"]).ShouldBe(50000.0);
    }

    [Fact]
    public async Task GCodeToOpcUa_MultipleCommands_WritesAllPositions()
    {
        // Arrange
        var gcode = @"
G00 X10 Y10 Z0
G01 X20 Y20 Z5 F500
G01 X30 Y30 Z10 F500
";
        var commands = await ParseGCodeTextAsync(gcode);

        using var opcClient = CreateOpcUaClient();
        await opcClient.ConnectAsync();

        ResetPlcState();

        // Act - Process all commands
        foreach (var command in commands)
        {
            foreach (var param in command.Parameters)
            {
                string nodeId = $"ns=2;s=CNC.Axes.{param.Key}.Position";
                await opcClient.WriteNodeValueAsync(nodeId, param.Value * 1000);
            }
        }

        // Assert - Verify final positions (last command wins)
        var nodes = GetOpcUaNodes();

        Convert.ToDouble(nodes["ns=2;s=CNC.Axes.X.Position"]).ShouldBe(30000.0); // X = 30mm
        Convert.ToDouble(nodes["ns=2;s=CNC.Axes.Y.Position"]).ShouldBe(30000.0); // Y = 30mm
        Convert.ToDouble(nodes["ns=2;s=CNC.Axes.Z.Position"]).ShouldBe(10000.0); // Z = 10mm
    }

    [Fact]
    public async Task GCodeToOpcUa_BatchWrite_WritesMultipleNodesEfficiently()
    {
        // Arrange
        var gcode = "G01 X100 Y200 Z300 A45 B90 C180";
        var commands = await ParseGCodeTextAsync(gcode);

        using var opcClient = CreateOpcUaClient();
        await opcClient.ConnectAsync();

        ResetPlcState();

        // Act - Use batch write for efficiency
        var command = commands.First();
        var nodeValues = new Dictionary<string, object>();

        foreach (var param in command.Parameters)
        {
            string nodeId = $"ns=2;s=CNC.Axes.{param.Key}.Position";
            nodeValues[nodeId] = param.Value * 1000;
        }

        await opcClient.WriteMultipleNodeValuesAsync(nodeValues);

        // Assert - Verify all nodes
        var nodes = GetOpcUaNodes();

        nodes.ShouldContainKey("ns=2;s=CNC.Axes.X.Position");
        nodes.ShouldContainKey("ns=2;s=CNC.Axes.Y.Position");
        nodes.ShouldContainKey("ns=2;s=CNC.Axes.Z.Position");
        nodes.ShouldContainKey("ns=2;s=CNC.Axes.A.Position");
        nodes.ShouldContainKey("ns=2;s=CNC.Axes.B.Position");
        nodes.ShouldContainKey("ns=2;s=CNC.Axes.C.Position");

        Convert.ToDouble(nodes["ns=2;s=CNC.Axes.X.Position"]).ShouldBe(100000.0);
        Convert.ToDouble(nodes["ns=2;s=CNC.Axes.A.Position"]).ShouldBe(45000.0);
    }

    [Fact]
    public async Task GCodeToOpcUa_VerifyWriteReadback_ConfirmsCorrectValue()
    {
        // Arrange
        var gcode = "G01 X123.456";
        var commands = await ParseGCodeTextAsync(gcode);

        using var opcClient = CreateOpcUaClient();
        await opcClient.ConnectAsync();

        ResetPlcState();

        var command = commands.First();
        var xValue = command.Parameters['X'];
        string nodeId = "ns=2;s=CNC.Axes.X.Position";
        double expectedValue = (double)(xValue * 1000);

        // Act - Write and read back
        await opcClient.WriteNodeValueAsync(nodeId, expectedValue);
        var readbackValue = await opcClient.ReadNodeValueAsync(nodeId);

        // Assert
        Convert.ToDouble(readbackValue).ShouldBe(expectedValue);
    }

    [Fact]
    public async Task GCodeToOpcUa_ComplexProgram_MapsAllAxes()
    {
        // Arrange
        var gcode = @"
G00 X0 Y0 Z50
G01 X100 Y100 Z0 F1000
G01 A45 B30 C60
";
        var commands = await ParseGCodeTextAsync(gcode);

        using var opcClient = CreateOpcUaClient();
        await opcClient.ConnectAsync();

        ResetPlcState();

        // Act - Process all commands
        foreach (var command in commands)
        {
            var nodeValues = new Dictionary<string, object>();

            foreach (var param in command.Parameters)
            {
                string nodeId = $"ns=2;s=CNC.Axes.{param.Key}.Position";
                nodeValues[nodeId] = param.Value * 1000;
            }

            if (nodeValues.Count > 0)
            {
                await opcClient.WriteMultipleNodeValuesAsync(nodeValues);
            }
        }

        // Assert - Check all axes
        var nodes = GetOpcUaNodes();

        // Linear axes (final values)
        Convert.ToDouble(nodes["ns=2;s=CNC.Axes.X.Position"]).ShouldBe(100000.0);
        Convert.ToDouble(nodes["ns=2;s=CNC.Axes.Y.Position"]).ShouldBe(100000.0);
        Convert.ToDouble(nodes["ns=2;s=CNC.Axes.Z.Position"]).ShouldBe(0.0);

        // Rotary axes
        Convert.ToDouble(nodes["ns=2;s=CNC.Axes.A.Position"]).ShouldBe(45000.0);
        Convert.ToDouble(nodes["ns=2;s=CNC.Axes.B.Position"]).ShouldBe(30000.0);
        Convert.ToDouble(nodes["ns=2;s=CNC.Axes.C.Position"]).ShouldBe(60000.0);
    }

    [Fact]
    public async Task GCodeToOpcUa_CustomNodeMapping_WritesToCorrectPaths()
    {
        // Arrange - Custom node structure
        var nodePrefix = "ns=2;s=Machine.CNC";

        var gcode = "G01 X50 Y75 Z25";
        var commands = await ParseGCodeTextAsync(gcode);

        using var opcClient = CreateOpcUaClient();
        await opcClient.ConnectAsync();

        ResetPlcState();

        // Act - Write using custom node structure
        var command = commands.First();
        var nodeValues = new Dictionary<string, object>();

        foreach (var param in command.Parameters)
        {
            string nodeId = $"{nodePrefix}.{param.Key}";
            nodeValues[nodeId] = param.Value * 1000;
        }

        await opcClient.WriteMultipleNodeValuesAsync(nodeValues);

        // Assert - Read back custom nodes
        var xValue = await opcClient.ReadNodeValueAsync($"{nodePrefix}.X");
        var yValue = await opcClient.ReadNodeValueAsync($"{nodePrefix}.Y");
        var zValue = await opcClient.ReadNodeValueAsync($"{nodePrefix}.Z");

        Convert.ToDouble(xValue).ShouldBe(50000.0);
        Convert.ToDouble(yValue).ShouldBe(75000.0);
        Convert.ToDouble(zValue).ShouldBe(25000.0);
    }
}
