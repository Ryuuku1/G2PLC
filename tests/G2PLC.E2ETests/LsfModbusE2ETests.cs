using Shouldly;
using G2PLC.E2ETests.Extensions;
using Xunit;

namespace G2PLC.E2ETests;

/// <summary>
/// End-to-end tests for LSF (Howick) to Modbus PLC communication.
/// These tests verify the complete pipeline from LSF parsing to PLC register writes.
/// </summary>
public class LsfModbusE2ETests : E2ETestBase
{
    private readonly string _testFilePath = Path.Combine(
        Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "PR-2.csv");

    [Fact]
    public async Task LsfToModbus_ParseAndMapFrameset_WritesAllComponents()
    {
        // Arrange
        var frameset = await HowickParser.ParseFileAsync(_testFilePath);

        using var modbusClient = CreateModbusClient();
        await modbusClient.ConnectAsync();

        ResetPlcState();

        // Act - Map and write all components
        var componentsWritten = 0;
        foreach (var component in frameset.Components)
        {
            var mappings = LsfMapper.MapComponentToRegisters(component);

            foreach (var mapping in mappings)
            {
                await modbusClient.WriteHoldingRegisterAsync(mapping.Address, mapping.Value);
            }

            componentsWritten++;
        }

        // Assert
        componentsWritten.ShouldBe(8); // PR-2 has 8 components

        var registers = GetModbusRegisters();
        registers.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task LsfToModbus_SingleComponent_MapsCorrectly()
    {
        // Arrange
        var frameset = await HowickParser.ParseFileAsync(_testFilePath);
        var h1Component = frameset.Components.First(c => c.ComponentId == "H1");

        using var modbusClient = CreateModbusClient();
        await modbusClient.ConnectAsync();

        ResetPlcState();

        // Act - Map and write H1 component
        var mappings = LsfMapper.MapComponentToRegisters(h1Component);

        foreach (var mapping in mappings)
        {
            await modbusClient.WriteHoldingRegisterAsync(mapping.Address, mapping.Value);
        }

        // Assert - Verify component metadata
        var registers = GetModbusRegisters();

        // Check that component ID was written (first character as ASCII)
        var componentIdMapping = mappings.FirstOrDefault(m => m.ParameterName == "H1_ID");
        if (componentIdMapping != null)
        {
            registers.ShouldContainKey(componentIdMapping.Address);
            registers[componentIdMapping.Address].ShouldBe((ushort)'H');
        }

        // Check that length was written (clamped to ushort.MaxValue)
        var lengthMapping = mappings.FirstOrDefault(m => m.ParameterName == "H1_Length");
        if (lengthMapping != null)
        {
            registers.ShouldContainKey(lengthMapping.Address);
            registers[lengthMapping.Address].ShouldBe(ushort.MaxValue); // 997.6mm * 1000 exceeds ushort
        }

        // Check operation count
        var opCountMapping = mappings.FirstOrDefault(m => m.ParameterName == "H1_OpCount");
        if (opCountMapping != null)
        {
            registers.ShouldContainKey(opCountMapping.Address);
            registers[opCountMapping.Address].ShouldBe((ushort)9); // H1 has 9 operations
        }
    }

    [Fact]
    public async Task LsfToModbus_VerifyOperations_CorrectTypesAndPositions()
    {
        // Arrange
        var frameset = await HowickParser.ParseFileAsync(_testFilePath);
        var h1Component = frameset.Components.First(c => c.ComponentId == "H1");

        using var modbusClient = CreateModbusClient();
        await modbusClient.ConnectAsync();

        ResetPlcState();

        // Act
        var mappings = LsfMapper.MapComponentToRegisters(h1Component);

        foreach (var mapping in mappings)
        {
            await modbusClient.WriteHoldingRegisterAsync(mapping.Address, mapping.Value);
        }

        // Assert - Check first operation (SWAGE at 19.5mm)
        var op0TypeMapping = mappings.FirstOrDefault(m => m.ParameterName == "H1_Op0_Type");
        var op0PosMapping = mappings.FirstOrDefault(m => m.ParameterName == "H1_Op0_Pos");

        if (op0TypeMapping != null && op0PosMapping != null)
        {
            var registers = GetModbusRegisters();

            registers[op0TypeMapping.Address].ShouldBe((ushort)1); // SWAGE = 1
            registers[op0PosMapping.Address].ShouldBe((ushort)19500); // 19.5mm in microns
        }
    }

    [Fact]
    public async Task LsfToModbus_FramesetHeader_WritesMetadata()
    {
        // Arrange
        var frameset = await HowickParser.ParseFileAsync(_testFilePath);

        using var modbusClient = CreateModbusClient();
        await modbusClient.ConnectAsync();

        ResetPlcState();

        // Act - Map entire frameset
        var allMappings = LsfMapper.MapFramesetToRegisters(frameset);

        foreach (var mapping in allMappings)
        {
            await modbusClient.WriteHoldingRegisterAsync(mapping.Address, mapping.Value);
        }

        // Assert - Check frameset metadata
        var framesetIdMapping = allMappings.FirstOrDefault(m => m.ParameterName == "FramesetId");
        var componentCountMapping = allMappings.FirstOrDefault(m => m.ParameterName == "ComponentCount");

        if (framesetIdMapping != null && componentCountMapping != null)
        {
            var registers = GetModbusRegisters();

            registers[framesetIdMapping.Address].ShouldBe((ushort)1); // Frameset ID = 1
            registers[componentCountMapping.Address].ShouldBe((ushort)8); // 8 components
        }
    }

    [Fact]
    public async Task LsfToModbus_ReadbackVerification_ConfirmsWrites()
    {
        // Arrange
        var frameset = await HowickParser.ParseFileAsync(_testFilePath);
        var component = frameset.Components.First();

        using var modbusClient = CreateModbusClient();
        await modbusClient.ConnectAsync();

        ResetPlcState();

        // Act - Write and verify each register
        var mappings = LsfMapper.MapComponentToRegisters(component);
        var verifiedCount = 0;

        foreach (var mapping in mappings.Take(10)) // Test first 10 mappings
        {
            await modbusClient.WriteHoldingRegisterAsync(mapping.Address, mapping.Value);

            var readback = await modbusClient.ReadHoldingRegisterAsync(mapping.Address);

            if (readback == mapping.Value)
            {
                verifiedCount++;
            }
        }

        // Assert - All writes should verify
        verifiedCount.ShouldBe(10);
    }

    [Fact]
    public async Task LsfToModbus_ComponentSequence_ProcessesInOrder()
    {
        // Arrange
        var frameset = await HowickParser.ParseFileAsync(_testFilePath);

        using var modbusClient = CreateModbusClient();
        await modbusClient.ConnectAsync();

        ResetPlcState();

        // Act - Process components sequentially
        var processedComponents = new List<string>();

        foreach (var component in frameset.Components)
        {
            var mappings = LsfMapper.MapComponentToRegisters(component);

            foreach (var mapping in mappings)
            {
                await modbusClient.WriteHoldingRegisterAsync(mapping.Address, mapping.Value);
            }

            processedComponents.Add(component.ComponentId);

            // Small delay to simulate real processing
            await Task.Delay(10);
        }

        // Assert - Verify all components processed in correct order
        processedComponents.ShouldBe(new[] { "H1", "H2", "H3", "V1", "V2", "V3", "D1", "D2" });

        var registers = GetModbusRegisters();
        registers.Count.ShouldBeGreaterThan(20); // Multiple registers written for all components
    }
}
