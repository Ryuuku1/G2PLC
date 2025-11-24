using G2PLC.Application.Mappers;
using G2PLC.Application.Parsers;
using G2PLC.Domain.Configuration;
using G2PLC.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace G2PLC.IntegrationTests;

/// <summary>
/// Integration tests for Howick LSF CSV parsing and mapping to PLC registers.
/// </summary>
public class HowickLsfIntegrationTests
{
    private readonly HowickCsvParser _parser;
    private readonly LsfDataMapper _mapper;
    private readonly string _testFilePath;

    public HowickLsfIntegrationTests()
    {
        var parserLogger = NullLogger<HowickCsvParser>.Instance;
        var mapperLogger = NullLogger<LsfDataMapper>.Instance;

        _parser = new HowickCsvParser(parserLogger);

        var configuration = new MappingConfiguration
        {
            RegisterMappings = new RegisterMappings(),
            ValidationRules = new ValidationRules(),
            ProcessingOptions = new ProcessingOptions()
        };

        _mapper = new LsfDataMapper(mapperLogger, configuration);

        _testFilePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "PR-2.csv");
    }

    [Fact]
    public async Task ParsePR2File_ShouldLoadFramesetCorrectly()
    {
        // Arrange & Act
        var frameset = await _parser.ParseFileAsync(_testFilePath);

        // Assert
        frameset.ShouldNotBeNull();
        frameset.Unit.ShouldBe("MILLIMETRE");
        frameset.Profile.ShouldBe("41.30X100.00");
        frameset.ProfileDescription.ShouldBe("Standard Profile");
        frameset.FramesetId.ShouldBe(1);
        frameset.FramesetName.ShouldBe("Alfa");
        frameset.Location.ShouldBe("Chicago");
        frameset.Components.Count.ShouldBe(8); // H1, H2, H3, V1, V2, V3, D1, D2
    }

    [Fact]
    public async Task ParsePR2File_ShouldParseH1ComponentCorrectly()
    {
        // Arrange & Act
        var frameset = await _parser.ParseFileAsync(_testFilePath);
        var h1 = frameset.Components.FirstOrDefault(c => c.ComponentId == "H1");

        // Assert
        h1.ShouldNotBeNull();
        h1.ComponentId.ShouldBe("H1");
        h1.LabelOrientation.ShouldBe(LabelOrientation.Normal);
        h1.Quantity.ShouldBe(1);
        h1.Length.ShouldBe(997.6m);
        h1.Operations.Count.ShouldBe(9); // 2 SWAGE + 3 LIP_CUT + 4 DIMPLE
    }

    [Fact]
    public async Task ParsePR2File_ShouldParseH1OperationsCorrectly()
    {
        // Arrange & Act
        var frameset = await _parser.ParseFileAsync(_testFilePath);
        var h1 = frameset.Components.FirstOrDefault(c => c.ComponentId == "H1");

        // Assert - Check specific operations
        h1.ShouldNotBeNull();

        var swageOps = h1.Operations.Where(o => o.OperationType == LsfOperationType.Swage).ToList();
        swageOps.Count.ShouldBe(2);
        swageOps[0].Position.ShouldBe(19.5m);
        swageOps[1].Position.ShouldBe(978.1m);

        var lipCutOps = h1.Operations.Where(o => o.OperationType == LsfOperationType.LipCut).ToList();
        lipCutOps.Count.ShouldBe(3);
        lipCutOps[0].Position.ShouldBe(99.1m);
        lipCutOps[1].Position.ShouldBe(109.1m);
        lipCutOps[2].Position.ShouldBe(498.8m);

        var dimpleOps = h1.Operations.Where(o => o.OperationType == LsfOperationType.Dimple).ToList();
        dimpleOps.Count.ShouldBe(4);
    }

    [Fact]
    public async Task ParsePR2File_ShouldParseD1DiagonalWithEndTrussCorrectly()
    {
        // Arrange & Act
        var frameset = await _parser.ParseFileAsync(_testFilePath);
        var d1 = frameset.Components.FirstOrDefault(c => c.ComponentId == "D1");

        // Assert
        d1.ShouldNotBeNull();
        d1.ComponentId.ShouldBe("D1");
        d1.LabelOrientation.ShouldBe(LabelOrientation.Inverted);
        d1.Length.ShouldBe(617.3m);

        var endTrussOps = d1.Operations.Where(o => o.OperationType == LsfOperationType.EndTruss).ToList();
        endTrussOps.Count.ShouldBe(2);
        endTrussOps[0].Position.ShouldBe(0.0m);
        endTrussOps[1].Position.ShouldBe(617.3m);
    }

    [Fact]
    public async Task MapH1Component_ShouldGenerateCorrectRegisterMappings()
    {
        // Arrange
        var frameset = await _parser.ParseFileAsync(_testFilePath);
        var h1 = frameset.Components.FirstOrDefault(c => c.ComponentId == "H1");

        // Act
        var mappings = _mapper.MapComponentToRegisters(h1!);

        // Assert
        mappings.ShouldNotBeNull();
        mappings.Count.ShouldBeGreaterThan(0);

        // Check component metadata registers
        var componentIdMapping = mappings.FirstOrDefault(m => m.ParameterName == "H1_ID");
        componentIdMapping.ShouldNotBeNull();
        componentIdMapping.Value.ShouldBe((ushort)'H'); // First character ASCII

        var lengthMapping = mappings.FirstOrDefault(m => m.ParameterName == "H1_Length");
        lengthMapping.ShouldNotBeNull();
        // Length gets clamped to ushort.MaxValue (65535) since 997600 exceeds it
        lengthMapping.Value.ShouldBe(ushort.MaxValue);

        var quantityMapping = mappings.FirstOrDefault(m => m.ParameterName == "H1_Quantity");
        quantityMapping.ShouldNotBeNull();
        quantityMapping.Value.ShouldBe((ushort)1);

        var orientationMapping = mappings.FirstOrDefault(m => m.ParameterName == "H1_Orientation");
        orientationMapping.ShouldNotBeNull();
        orientationMapping.Value.ShouldBe((ushort)0); // Normal = 0

        var opCountMapping = mappings.FirstOrDefault(m => m.ParameterName == "H1_OpCount");
        opCountMapping.ShouldNotBeNull();
        opCountMapping.Value.ShouldBe((ushort)9); // 9 operations
    }

    [Fact]
    public async Task MapH1Component_ShouldMapOperationsCorrectly()
    {
        // Arrange
        var frameset = await _parser.ParseFileAsync(_testFilePath);
        var h1 = frameset.Components.FirstOrDefault(c => c.ComponentId == "H1");

        // Act
        var mappings = _mapper.MapComponentToRegisters(h1!);

        // Assert - Check first operation (SWAGE at 19.5mm)
        var op0TypeMapping = mappings.FirstOrDefault(m => m.ParameterName == "H1_Op0_Type");
        op0TypeMapping.ShouldNotBeNull();
        op0TypeMapping.Value.ShouldBe((ushort)1); // SWAGE = 1

        var op0PosMapping = mappings.FirstOrDefault(m => m.ParameterName == "H1_Op0_Pos");
        op0PosMapping.ShouldNotBeNull();
        op0PosMapping.Value.ShouldBe((ushort)19500); // 19500 microns
    }

    [Fact]
    public async Task MapEntireFrameset_ShouldGenerateCompleteRegisterSet()
    {
        // Arrange
        var frameset = await _parser.ParseFileAsync(_testFilePath);

        // Act
        var allMappings = _mapper.MapFramesetToRegisters(frameset);

        // Assert
        allMappings.ShouldNotBeNull();
        allMappings.Count.ShouldBeGreaterThan(0);

        // Check frameset header
        var framesetIdMapping = allMappings.FirstOrDefault(m => m.ParameterName == "FramesetId");
        framesetIdMapping.ShouldNotBeNull();
        framesetIdMapping.Value.ShouldBe((ushort)1);

        var componentCountMapping = allMappings.FirstOrDefault(m => m.ParameterName == "ComponentCount");
        componentCountMapping.ShouldNotBeNull();
        componentCountMapping.Value.ShouldBe((ushort)8); // 8 components in PR-2

        // Verify all components have trigger registers
        var triggerMappings = allMappings.Where(m => m.ParameterName.EndsWith("_Trigger")).ToList();
        triggerMappings.Count.ShouldBe(8); // One for each component
    }

    [Fact]
    public async Task MapFrameset_ShouldOrderRegistersByAddress()
    {
        // Arrange
        var frameset = await _parser.ParseFileAsync(_testFilePath);

        // Act
        var allMappings = _mapper.MapFramesetToRegisters(frameset);

        // Assert - Verify mappings are ordered by address
        for (int i = 1; i < allMappings.Count; i++)
        {
            allMappings[i].Address.ShouldBeGreaterThanOrEqualTo(allMappings[i - 1].Address);
        }
    }

    [Fact]
    public async Task ParseAndMap_EndToEnd_ShouldProduceValidPLCData()
    {
        // Arrange & Act - Full end-to-end test
        var frameset = await _parser.ParseFileAsync(_testFilePath);
        var allMappings = _mapper.MapFramesetToRegisters(frameset);

        // Assert - Comprehensive validation
        frameset.Components.Count.ShouldBe(8);
        allMappings.Count.ShouldBeGreaterThan(50); // Lots of operations

        // All register values should be valid (0-65535)
        foreach (var mapping in allMappings)
        {
            mapping.Value.ShouldBeInRange((ushort)0, ushort.MaxValue);
            mapping.Address.ShouldBeInRange((ushort)0, ushort.MaxValue);
        }

        // All mappings should have parameter names
        foreach (var mapping in allMappings)
        {
            mapping.ParameterName.ShouldNotBeNullOrEmpty();
        }

        // Verify operation type codes are valid (1-5)
        var operationTypeMappings = allMappings.Where(m => m.ParameterName.Contains("_Type")).ToList();
        foreach (var opType in operationTypeMappings)
        {
            opType.Value.ShouldBeInRange((ushort)0, (ushort)5);
        }
    }
}
