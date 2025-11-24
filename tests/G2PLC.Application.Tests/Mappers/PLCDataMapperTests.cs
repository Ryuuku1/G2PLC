using G2PLC.Application.Mappers;
using G2PLC.Domain.Configuration;
using G2PLC.Domain.Enums;
using G2PLC.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace G2PLC.Application.Tests.Mappers;

public class PlcDataMapperTests
{
    private readonly Mock<ILogger<PlcDataMapper>> _loggerMock;
    private readonly PlcDataMapper _mapper;

    public PlcDataMapperTests()
    {
        _loggerMock = new Mock<ILogger<PlcDataMapper>>();
        var configuration = CreateDefaultConfiguration();
        _mapper = new PlcDataMapper(_loggerMock.Object, configuration);
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
    public void MapToRegisters_WithGCommand_ShouldMapToGCommandRegister()
    {
        var command = new GCodeCommand
        {
            CommandType = "G",
            CommandNumber = 1
        };

        var results = _mapper.MapToRegisters(command);

        results.ShouldContain(r => r.ParameterName == "G_Command");
        var gCommandMapping = results.First(r => r.ParameterName == "G_Command");
        gCommandMapping.Address.ShouldBe((ushort)6);
        gCommandMapping.Value.ShouldBe((ushort)1);
        gCommandMapping.RegisterType.ShouldBe(RegisterType.HoldingRegister);
    }

    [Fact]
    public void MapToRegisters_WithMCommand_ShouldMapToMCommandRegister()
    {
        var command = new GCodeCommand
        {
            CommandType = "M",
            CommandNumber = 3
        };

        var results = _mapper.MapToRegisters(command);

        results.ShouldContain(r => r.ParameterName == "M_Command");
        var mCommandMapping = results.First(r => r.ParameterName == "M_Command");
        mCommandMapping.Address.ShouldBe((ushort)7);
        mCommandMapping.Value.ShouldBe((ushort)3);
        mCommandMapping.RegisterType.ShouldBe(RegisterType.HoldingRegister);
    }

    [Fact]
    public void MapToRegisters_WithTCommand_ShouldMapToToolNumberRegister()
    {
        var command = new GCodeCommand
        {
            CommandType = "T",
            CommandNumber = 5
        };

        var results = _mapper.MapToRegisters(command);

        results.ShouldContain(r => r.ParameterName == "Tool_Number");
        var toolMapping = results.First(r => r.ParameterName == "Tool_Number");
        toolMapping.Address.ShouldBe((ushort)5);
        toolMapping.Value.ShouldBe((ushort)5);
    }

    [Fact]
    public void MapToRegisters_WithXYZParameters_ShouldScaleAndMap()
    {
        var command = new GCodeCommand
        {
            CommandType = "G",
            CommandNumber = 1,
            Parameters = new Dictionary<char, decimal>
            {
                {'X', 10.5m},
                {'Y', 20.3m},
                {'Z', 5.0m}
            }
        };

        var results = _mapper.MapToRegisters(command);

        var xMapping = results.First(r => r.ParameterName == "X_Position");
        xMapping.Address.ShouldBe((ushort)0);
        xMapping.Value.ShouldBe((ushort)10500); // 10.5 * 1000
        xMapping.OriginalValue.ShouldBe(10.5m);
        xMapping.ScaleFactor.ShouldBe(1000m);

        var yMapping = results.First(r => r.ParameterName == "Y_Position");
        yMapping.Address.ShouldBe((ushort)1);
        yMapping.Value.ShouldBe((ushort)20300); // 20.3 * 1000

        var zMapping = results.First(r => r.ParameterName == "Z_Position");
        zMapping.Address.ShouldBe((ushort)2);
        zMapping.Value.ShouldBe((ushort)5000); // 5.0 * 1000
    }

    [Fact]
    public void MapToRegisters_WithFeedRate_ShouldScaleCorrectly()
    {
        var command = new GCodeCommand
        {
            CommandType = "G",
            CommandNumber = 1,
            Parameters = new Dictionary<char, decimal>
            {
                {'F', 100m}
            }
        };

        var results = _mapper.MapToRegisters(command);

        var feedMapping = results.First(r => r.ParameterName == "Feed_Rate");
        feedMapping.Address.ShouldBe((ushort)3);
        feedMapping.Value.ShouldBe((ushort)1000); // 100 * 10
        feedMapping.OriginalValue.ShouldBe(100m);
        feedMapping.ScaleFactor.ShouldBe(10m);
    }

    [Fact]
    public void MapToRegisters_WithSpindleSpeed_ShouldNotScale()
    {
        var command = new GCodeCommand
        {
            CommandType = "M",
            CommandNumber = 3,
            Parameters = new Dictionary<char, decimal>
            {
                {'S', 1200m}
            }
        };

        var results = _mapper.MapToRegisters(command);

        var spindleMapping = results.First(r => r.ParameterName == "Spindle_Speed");
        spindleMapping.Address.ShouldBe((ushort)4);
        spindleMapping.Value.ShouldBe((ushort)1200);
        spindleMapping.ScaleFactor.ShouldBe(1m);
    }

    [Fact]
    public void MapToRegisters_WithNegativeValue_ShouldClampToZero()
    {
        var command = new GCodeCommand
        {
            CommandType = "G",
            CommandNumber = 1,
            Parameters = new Dictionary<char, decimal>
            {
                {'X', -10m}
            }
        };

        var results = _mapper.MapToRegisters(command);

        var xMapping = results.First(r => r.ParameterName == "X_Position");
        xMapping.Value.ShouldBe((ushort)0);
    }

    [Fact]
    public void MapToRegisters_WithValueExceedingMax_ShouldClampToMax()
    {
        var command = new GCodeCommand
        {
            CommandType = "G",
            CommandNumber = 1,
            Parameters = new Dictionary<char, decimal>
            {
                {'X', 100m} // Will be scaled to 100000, exceeding ushort.MaxValue
            }
        };

        var results = _mapper.MapToRegisters(command);

        var xMapping = results.First(r => r.ParameterName == "X_Position");
        xMapping.Value.ShouldBe(ushort.MaxValue);
    }

    [Fact]
    public void MapToRegisters_ShouldReturnMappingsInAddressOrder()
    {
        var command = new GCodeCommand
        {
            CommandType = "G",
            CommandNumber = 1,
            Parameters = new Dictionary<char, decimal>
            {
                {'F', 100m},  // Address 3
                {'X', 10m},   // Address 0
                {'Z', 5m},    // Address 2
                {'Y', 20m}    // Address 1
            }
        };

        var results = _mapper.MapToRegisters(command);

        for (var i = 0; i < results.Count - 1; i++)
        {
            results[i].Address.ShouldBeLessThanOrEqualTo(results[i + 1].Address);
        }
    }

    [Fact]
    public void GetRegisterAddresses_ShouldReturnAllAddresses()
    {
        var addresses = _mapper.GetRegisterAddresses();

        addresses.ShouldContainKey("X_Position");
        addresses.ShouldContainKey("Y_Position");
        addresses.ShouldContainKey("Z_Position");
        addresses.ShouldContainKey("FeedRate");
        addresses.ShouldContainKey("SpindleSpeed");
        addresses.ShouldContainKey("ToolNumber");
        addresses.ShouldContainKey("GCommand");
        addresses.ShouldContainKey("MCommand");
    }

    [Fact]
    public void MapToRegisters_WithNullCommand_ShouldThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            _mapper.MapToRegisters(null!));
    }

    [Fact]
    public void MapToRegisters_WithCompleteCommand_ShouldMapAllParameters()
    {
        var command = new GCodeCommand
        {
            CommandType = "G",
            CommandNumber = 1,
            Parameters = new Dictionary<char, decimal>
            {
                {'X', 10m},
                {'Y', 20m},
                {'Z', 5m},
                {'F', 100m}
            }
        };

        var results = _mapper.MapToRegisters(command);

        results.Count.ShouldBe(5); // G_Command + X + Y + Z + F
        results.ShouldContain(r => r.ParameterName == "G_Command");
        results.ShouldContain(r => r.ParameterName == "X_Position");
        results.ShouldContain(r => r.ParameterName == "Y_Position");
        results.ShouldContain(r => r.ParameterName == "Z_Position");
        results.ShouldContain(r => r.ParameterName == "Feed_Rate");
    }
}
