using G2PLC.Domain.Models;
using Shouldly;
using Xunit;

namespace G2PLC.Domain.Tests.Models;

public class GCodeCommandTests
{
    [Fact]
    public void Constructor_WithRawLine_ShouldInitializeRawLine()
    {
        var rawLine = "G01 X10.5 Y20.3";
        var command = new GCodeCommand(rawLine);

        command.RawLine.ShouldBe(rawLine);
        command.IsValid.ShouldBeFalse();
        command.Parameters.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithoutRawLine_ShouldInitializeWithEmptyString()
    {
        var command = new GCodeCommand();

        command.RawLine.ShouldBe(string.Empty);
        command.IsValid.ShouldBeFalse();
        command.Parameters.ShouldNotBeNull();
    }

    [Fact]
    public void Validate_WithValidGCommand_ShouldReturnEmptyList()
    {
        var command = new GCodeCommand
        {
            CommandType = "G",
            CommandNumber = 1
        };

        var errors = command.Validate();

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_WithMissingCommandType_ShouldReturnError()
    {
        var command = new GCodeCommand
        {
            CommandType = "",
            CommandNumber = 1
        };

        var errors = command.Validate();

        errors.ShouldNotBeEmpty();
        errors.ShouldContain(e => e.Contains("Command type is required"));
    }

    [Fact]
    public void Validate_WithInvalidCommandType_ShouldReturnError()
    {
        var command = new GCodeCommand
        {
            CommandType = "X",
            CommandNumber = 1
        };

        var errors = command.Validate();

        errors.ShouldContain(e => e.Contains("Invalid command type"));
    }

    [Fact]
    public void Validate_WithMissingCommandNumber_ShouldReturnError()
    {
        var command = new GCodeCommand
        {
            CommandType = "G",
            CommandNumber = null
        };

        var errors = command.Validate();

        errors.ShouldContain(e => e.Contains("Command number is required"));
    }

    [Fact]
    public void Validate_WithOutOfRangeCommandNumber_ShouldReturnError()
    {
        var command = new GCodeCommand
        {
            CommandType = "G",
            CommandNumber = 1000
        };

        var errors = command.Validate();

        errors.ShouldContain(e => e.Contains("Command number out of range"));
    }

    [Fact]
    public void ToString_WithCompleteCommand_ShouldFormatCorrectly()
    {
        var command = new GCodeCommand
        {
            CommandType = "G",
            CommandNumber = 1,
            Parameters = new Dictionary<char, decimal>
            {
                {'X', 10.5m},
                {'Y', 20.3m}
            }
        };

        var result = command.ToString();

        result.ShouldContain("G01");
        result.ShouldContain("X10.5");
        result.ShouldContain("Y20.3");
    }

    [Fact]
    public void ToString_WithLineNumber_ShouldIncludeLineNumber()
    {
        var command = new GCodeCommand
        {
            LineNumber = 100,
            CommandType = "G",
            CommandNumber = 0
        };

        var result = command.ToString();

        result.ShouldStartWith("N100");
    }

    [Fact]
    public void Parameters_ShouldAllowMultipleAdditions()
    {
        var command = new GCodeCommand();

        command.Parameters['X'] = 10.5m;
        command.Parameters['Y'] = 20.3m;
        command.Parameters['Z'] = -5.2m;

        command.Parameters.Count.ShouldBe(3);
        command.Parameters['X'].ShouldBe(10.5m);
        command.Parameters['Y'].ShouldBe(20.3m);
        command.Parameters['Z'].ShouldBe(-5.2m);
    }
}
