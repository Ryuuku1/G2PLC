using G2PLC.Domain.Enums;
using G2PLC.Domain.Models;
using Shouldly;
using Xunit;

namespace G2PLC.Domain.Tests.Models;

public class RegisterMappingTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var mapping = new RegisterMapping();

        mapping.Address.ShouldBe((ushort)0);
        mapping.Value.ShouldBe((ushort)0);
        mapping.ParameterName.ShouldBe(string.Empty);
        mapping.ScaleFactor.ShouldBe(0m);
        mapping.OriginalValue.ShouldBe(0m);
        mapping.RegisterType.ShouldBe(RegisterType.HoldingRegister);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var mapping = new RegisterMapping
        {
            Address = 10,
            Value = 1000,
            ParameterName = "X_Position",
            ScaleFactor = 100m,
            OriginalValue = 10m,
            RegisterType = RegisterType.HoldingRegister
        };

        mapping.Address.ShouldBe((ushort)10);
        mapping.Value.ShouldBe((ushort)1000);
        mapping.ParameterName.ShouldBe("X_Position");
        mapping.ScaleFactor.ShouldBe(100m);
        mapping.OriginalValue.ShouldBe(10m);
        mapping.RegisterType.ShouldBe(RegisterType.HoldingRegister);
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        var mapping = new RegisterMapping
        {
            Address = 5,
            Value = 5000,
            ParameterName = "Feed_Rate",
            ScaleFactor = 10m,
            OriginalValue = 500m,
            RegisterType = RegisterType.HoldingRegister
        };

        var result = mapping.ToString();

        result.ShouldContain("Feed_Rate");
        result.ShouldContain("500");
        result.ShouldContain("Address=5");
        result.ShouldContain("Value=5000");
        result.ShouldContain("Scale=10");
        result.ShouldContain("HoldingRegister");
    }

    [Fact]
    public void RegisterType_Coil_ShouldBeSupported()
    {
        var mapping = new RegisterMapping
        {
            RegisterType = RegisterType.Coil
        };

        mapping.RegisterType.ShouldBe(RegisterType.Coil);
    }
}
