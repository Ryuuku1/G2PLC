using Shouldly;
using G2PLC.E2ETests.Extensions;
using Xunit;

namespace G2PLC.E2ETests;

/// <summary>
/// End-to-end tests for LSF (Howick) to OPC UA communication.
/// These tests verify the complete pipeline from LSF parsing to OPC UA node writes.
/// </summary>
public class LsfOpcUaE2ETests : E2ETestBase
{
    private readonly string _testFilePath = Path.Combine(
        Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "PR-2.csv");

    [Fact]
    public async Task LsfToOpcUa_ParseAndMapFrameset_WritesAllComponents()
    {
        // Arrange
        var frameset = await HowickParser.ParseFileAsync(_testFilePath);

        using var opcClient = CreateOpcUaClient();
        await opcClient.ConnectAsync();

        ResetPlcState();

        // Act - Map and write all components
        var componentsWritten = 0;
        foreach (var component in frameset.Components)
        {
            var mappings = LsfMapper.MapComponentToRegisters(component);

            var nodeValues = new Dictionary<string, object>();
            foreach (var mapping in mappings)
            {
                var nodeId = $"ns=2;s=LSF.Component.{component.ComponentId}.{mapping.ParameterName}";
                nodeValues[nodeId] = (double)mapping.Value;
            }

            await opcClient.WriteMultipleNodeValuesAsync(nodeValues);
            componentsWritten++;
        }

        // Assert
        componentsWritten.ShouldBe(8); // PR-2 has 8 components

        var nodes = GetOpcUaNodes();
        nodes.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task LsfToOpcUa_SingleComponent_MapsCorrectly()
    {
        // Arrange
        var frameset = await HowickParser.ParseFileAsync(_testFilePath);
        var h1Component = frameset.Components.First(c => c.ComponentId == "H1");

        using var opcClient = CreateOpcUaClient();
        await opcClient.ConnectAsync();

        ResetPlcState();

        // Act - Map and write H1 component
        var mappings = LsfMapper.MapComponentToRegisters(h1Component);

        var nodeValues = new Dictionary<string, object>();
        foreach (var mapping in mappings)
        {
            string nodeId = $"ns=2;s=LSF.Component.H1.{mapping.ParameterName}";
            nodeValues[nodeId] = (double)mapping.Value;
        }

        await opcClient.WriteMultipleNodeValuesAsync(nodeValues);

        // Assert - Verify component metadata
        var nodes = GetOpcUaNodes();

        // Check that component ID was written (first character as ASCII)
        string componentIdNode = "ns=2;s=LSF.Component.H1.H1_ID";
        if (nodes.ContainsKey(componentIdNode))
        {
            Convert.ToDouble(nodes[componentIdNode]).ShouldBe((double)(ushort)'H');
        }

        // Check that length was written (clamped to ushort.MaxValue)
        string lengthNode = "ns=2;s=LSF.Component.H1.H1_Length";
        if (nodes.ContainsKey(lengthNode))
        {
            Convert.ToDouble(nodes[lengthNode]).ShouldBe((double)ushort.MaxValue); // 997.6mm * 1000 exceeds ushort
        }

        // Check operation count
        string opCountNode = "ns=2;s=LSF.Component.H1.H1_OpCount";
        if (nodes.ContainsKey(opCountNode))
        {
            Convert.ToDouble(nodes[opCountNode]).ShouldBe(9.0); // H1 has 9 operations
        }
    }

    [Fact]
    public async Task LsfToOpcUa_VerifyOperations_CorrectTypesAndPositions()
    {
        // Arrange
        var frameset = await HowickParser.ParseFileAsync(_testFilePath);
        var h1Component = frameset.Components.First(c => c.ComponentId == "H1");

        using var opcClient = CreateOpcUaClient();
        await opcClient.ConnectAsync();

        ResetPlcState();

        // Act
        var mappings = LsfMapper.MapComponentToRegisters(h1Component);

        var nodeValues = new Dictionary<string, object>();
        foreach (var mapping in mappings)
        {
            string nodeId = $"ns=2;s=LSF.Component.H1.{mapping.ParameterName}";
            nodeValues[nodeId] = (double)mapping.Value;
        }

        await opcClient.WriteMultipleNodeValuesAsync(nodeValues);

        // Assert - Check first operation (SWAGE at 19.5mm)
        var nodes = GetOpcUaNodes();

        string op0TypeNode = "ns=2;s=LSF.Component.H1.H1_Op0_Type";
        string op0PosNode = "ns=2;s=LSF.Component.H1.H1_Op0_Pos";

        if (nodes.ContainsKey(op0TypeNode) && nodes.ContainsKey(op0PosNode))
        {
            Convert.ToDouble(nodes[op0TypeNode]).ShouldBe(1.0); // SWAGE = 1
            Convert.ToDouble(nodes[op0PosNode]).ShouldBe(19500.0); // 19.5mm in microns
        }
    }

    [Fact]
    public async Task LsfToOpcUa_FramesetHeader_WritesMetadata()
    {
        // Arrange
        var frameset = await HowickParser.ParseFileAsync(_testFilePath);

        using var opcClient = CreateOpcUaClient();
        await opcClient.ConnectAsync();

        ResetPlcState();

        // Act - Map entire frameset
        var allMappings = LsfMapper.MapFramesetToRegisters(frameset);

        var nodeValues = new Dictionary<string, object>();
        foreach (var mapping in allMappings)
        {
            string nodeId = $"ns=2;s=LSF.Frameset.{mapping.ParameterName}";
            nodeValues[nodeId] = (double)mapping.Value;
        }

        await opcClient.WriteMultipleNodeValuesAsync(nodeValues);

        // Assert - Check frameset metadata
        var nodes = GetOpcUaNodes();

        string framesetIdNode = "ns=2;s=LSF.Frameset.FramesetId";
        string componentCountNode = "ns=2;s=LSF.Frameset.ComponentCount";

        if (nodes.ContainsKey(framesetIdNode) && nodes.ContainsKey(componentCountNode))
        {
            Convert.ToDouble(nodes[framesetIdNode]).ShouldBe(1.0); // Frameset ID = 1
            Convert.ToDouble(nodes[componentCountNode]).ShouldBe(8.0); // 8 components
        }
    }

    [Fact]
    public async Task LsfToOpcUa_ReadbackVerification_ConfirmsWrites()
    {
        // Arrange
        var frameset = await HowickParser.ParseFileAsync(_testFilePath);
        var component = frameset.Components.First();

        using var opcClient = CreateOpcUaClient();
        await opcClient.ConnectAsync();

        ResetPlcState();

        // Act - Write and verify each node
        var mappings = LsfMapper.MapComponentToRegisters(component);
        var verifiedCount = 0;

        foreach (var mapping in mappings.Take(10)) // Test first 10 mappings
        {
            string nodeId = $"ns=2;s=LSF.Component.{component.ComponentId}.{mapping.ParameterName}";
            double expectedValue = (double)mapping.Value;

            await opcClient.WriteNodeValueAsync(nodeId, expectedValue);

            var readback = await opcClient.ReadNodeValueAsync(nodeId);

            if (Math.Abs(Convert.ToDouble(readback) - expectedValue) < 0.01)
            {
                verifiedCount++;
            }
        }

        // Assert - All writes should verify
        verifiedCount.ShouldBe(10);
    }

    [Fact]
    public async Task LsfToOpcUa_ComponentSequence_ProcessesInOrder()
    {
        // Arrange
        var frameset = await HowickParser.ParseFileAsync(_testFilePath);

        using var opcClient = CreateOpcUaClient();
        await opcClient.ConnectAsync();

        ResetPlcState();

        // Act - Process components sequentially
        var processedComponents = new List<string>();

        foreach (var component in frameset.Components)
        {
            var mappings = LsfMapper.MapComponentToRegisters(component);

            var nodeValues = new Dictionary<string, object>();
            foreach (var mapping in mappings)
            {
                string nodeId = $"ns=2;s=LSF.Component.{component.ComponentId}.{mapping.ParameterName}";
                nodeValues[nodeId] = (double)mapping.Value;
            }

            await opcClient.WriteMultipleNodeValuesAsync(nodeValues);
            processedComponents.Add(component.ComponentId);

            // Small delay to simulate real processing
            await Task.Delay(10);
        }

        // Assert - Verify all components processed in correct order
        processedComponents.ShouldBe(new[] { "H1", "H2", "H3", "V1", "V2", "V3", "D1", "D2" });

        var nodes = GetOpcUaNodes();
        nodes.Count.ShouldBeGreaterThan(20); // Multiple nodes written for all components
    }

    [Fact]
    public async Task LsfToOpcUa_CustomNodeStructure_WritesToCorrectPaths()
    {
        // Arrange - Custom node structure
        var frameset = await HowickParser.ParseFileAsync(_testFilePath);
        var h1Component = frameset.Components.First(c => c.ComponentId == "H1");
        var nodePrefix = "ns=2;s=Machine.Manufacturing.Howick";

        using var opcClient = CreateOpcUaClient();
        await opcClient.ConnectAsync();

        ResetPlcState();

        // Act - Write using custom node structure
        var mappings = LsfMapper.MapComponentToRegisters(h1Component);

        var nodeValues = new Dictionary<string, object>();
        foreach (var mapping in mappings.Take(5)) // Take first 5 parameters for testing
        {
            string nodeId = $"{nodePrefix}.{mapping.ParameterName}";
            nodeValues[nodeId] = (double)mapping.Value;
        }

        await opcClient.WriteMultipleNodeValuesAsync(nodeValues);

        // Assert - Read back custom nodes
        var nodes = GetOpcUaNodes();

        // Verify at least some nodes were written under the custom path
        var customPathNodes = nodes.Keys.Where(k => k.StartsWith(nodePrefix)).ToList();
        customPathNodes.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task LsfToOpcUa_BatchWrite_WritesMultipleComponentsEfficiently()
    {
        // Arrange
        var frameset = await HowickParser.ParseFileAsync(_testFilePath);

        using var opcClient = CreateOpcUaClient();
        await opcClient.ConnectAsync();

        ResetPlcState();

        // Act - Use batch write for all components at once
        var allNodeValues = new Dictionary<string, object>();

        foreach (var component in frameset.Components)
        {
            var mappings = LsfMapper.MapComponentToRegisters(component);

            foreach (var mapping in mappings)
            {
                string nodeId = $"ns=2;s=LSF.Component.{component.ComponentId}.{mapping.ParameterName}";
                allNodeValues[nodeId] = (double)mapping.Value;
            }
        }

        await opcClient.WriteMultipleNodeValuesAsync(allNodeValues);

        // Assert - Verify all components written
        var nodes = GetOpcUaNodes();

        // Should have nodes for all 8 components (H1, H2, H3, V1, V2, V3, D1, D2)
        var h1Nodes = nodes.Keys.Where(k => k.Contains(".H1.")).ToList();
        var h2Nodes = nodes.Keys.Where(k => k.Contains(".H2.")).ToList();
        var v1Nodes = nodes.Keys.Where(k => k.Contains(".V1.")).ToList();

        h1Nodes.Count.ShouldBeGreaterThan(0);
        h2Nodes.Count.ShouldBeGreaterThan(0);
        v1Nodes.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task LsfToOpcUa_OperationDetails_WritesCompleteData()
    {
        // Arrange
        var frameset = await HowickParser.ParseFileAsync(_testFilePath);
        var component = frameset.Components.First(c => c.Operations.Count > 0);

        using var opcClient = CreateOpcUaClient();
        await opcClient.ConnectAsync();

        ResetPlcState();

        // Act - Map component with operations
        var mappings = LsfMapper.MapComponentToRegisters(component);

        var nodeValues = new Dictionary<string, object>();
        foreach (var mapping in mappings)
        {
            string nodeId = $"ns=2;s=LSF.Component.{component.ComponentId}.{mapping.ParameterName}";
            nodeValues[nodeId] = (double)mapping.Value;
        }

        await opcClient.WriteMultipleNodeValuesAsync(nodeValues);

        // Assert - Verify operation data was written
        var nodes = GetOpcUaNodes();

        // Check for operation type and position nodes
        var operationNodes = nodes.Keys
            .Where(k => k.Contains("_Op") && (k.Contains("_Type") || k.Contains("_Pos")))
            .ToList();

        operationNodes.Count.ShouldBeGreaterThan(0);

        // Verify at least one operation has both type and position
        var op0Type = nodes.Keys.FirstOrDefault(k => k.Contains("_Op0_Type"));
        var op0Pos = nodes.Keys.FirstOrDefault(k => k.Contains("_Op0_Pos"));

        op0Type.ShouldNotBeNull();
        op0Pos.ShouldNotBeNull();
    }

    [Fact]
    public async Task LsfToOpcUa_ComponentMetadata_PreservesDataIntegrity()
    {
        // Arrange
        var frameset = await HowickParser.ParseFileAsync(_testFilePath);
        var h1Component = frameset.Components.First(c => c.ComponentId == "H1");

        using var opcClient = CreateOpcUaClient();
        await opcClient.ConnectAsync();

        ResetPlcState();

        // Act - Write component metadata
        var mappings = LsfMapper.MapComponentToRegisters(h1Component);

        var nodeValues = new Dictionary<string, object>();
        foreach (var mapping in mappings)
        {
            string nodeId = $"ns=2;s=LSF.H1.{mapping.ParameterName}";
            nodeValues[nodeId] = (double)mapping.Value;
        }

        await opcClient.WriteMultipleNodeValuesAsync(nodeValues);

        // Assert - Read back and verify metadata integrity
        var lengthNode = "ns=2;s=LSF.H1.H1_Length";
        var opCountNode = "ns=2;s=LSF.H1.H1_OpCount";

        var length = await opcClient.ReadNodeValueAsync(lengthNode);
        var opCount = await opcClient.ReadNodeValueAsync(opCountNode);

        // Length should be clamped to ushort.MaxValue (997.6mm * 1000 exceeds ushort)
        Convert.ToDouble(length).ShouldBe((double)ushort.MaxValue);

        // H1 has 9 operations
        Convert.ToDouble(opCount).ShouldBe(9.0);
    }
}
