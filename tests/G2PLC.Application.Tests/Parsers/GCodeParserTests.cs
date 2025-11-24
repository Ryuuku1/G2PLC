using G2PLC.Application.Parsers;
using G2PLC.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace G2PLC.Application.Tests.Parsers;

public class GCodeParserTests
{
    private readonly Mock<ILogger<GCodeParser>> _loggerMock;
    private readonly GCodeParser _parser;

    public GCodeParserTests()
    {
        _loggerMock = new Mock<ILogger<GCodeParser>>();
        _parser = new GCodeParser(_loggerMock.Object);
    }

    [Fact]
    public void ParseLine_WithNullOrEmpty_ShouldReturnNull()
    {
        _parser.ParseLine(null!).ShouldBeNull();
        _parser.ParseLine("").ShouldBeNull();
        _parser.ParseLine("   ").ShouldBeNull();
    }

    [Fact]
    public void ParseLine_WithCommentOnly_ShouldReturnNull()
    {
        _parser.ParseLine("(This is a comment)").ShouldBeNull();
        _parser.ParseLine("; This is also a comment").ShouldBeNull();
    }

    [Fact]
    public void ParseLine_WithSimpleGCommand_ShouldParseCorrectly()
    {
        var result = _parser.ParseLine("G01");

        result.ShouldNotBeNull();
        result.CommandType.ShouldBe("G");
        result.CommandNumber.ShouldBe(1);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ParseLine_WithGCommandAndParameters_ShouldParseAll()
    {
        var result = _parser.ParseLine("G01 X10.5 Y20.3 Z-5.2 F100");

        result.ShouldNotBeNull();
        result.CommandType.ShouldBe("G");
        result.CommandNumber.ShouldBe(1);
        result.Parameters['X'].ShouldBe(10.5m);
        result.Parameters['Y'].ShouldBe(20.3m);
        result.Parameters['Z'].ShouldBe(-5.2m);
        result.Parameters['F'].ShouldBe(100m);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ParseLine_WithMCommand_ShouldParseCorrectly()
    {
        var result = _parser.ParseLine("M03 S1200");

        result.ShouldNotBeNull();
        result.CommandType.ShouldBe("M");
        result.CommandNumber.ShouldBe(3);
        result.Parameters['S'].ShouldBe(1200m);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ParseLine_WithTCommand_ShouldParseCorrectly()
    {
        var result = _parser.ParseLine("T01");

        result.ShouldNotBeNull();
        result.CommandType.ShouldBe("T");
        result.CommandNumber.ShouldBe(1);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ParseLine_WithLineNumber_ShouldExtractLineNumber()
    {
        var result = _parser.ParseLine("N100 G01 X10 Y20");

        result.ShouldNotBeNull();
        result.LineNumber.ShouldBe(100);
        result.CommandType.ShouldBe("G");
        result.CommandNumber.ShouldBe(1);
    }

    [Fact]
    public void ParseLine_WithCommentAfterCommand_ShouldIgnoreComment()
    {
        var result = _parser.ParseLine("G01 X10 Y20 (Move to position)");

        result.ShouldNotBeNull();
        result.CommandType.ShouldBe("G");
        result.Parameters['X'].ShouldBe(10m);
        result.Parameters['Y'].ShouldBe(20m);
    }

    [Fact]
    public void ParseLine_WithLowercaseCommand_ShouldConvertToUppercase()
    {
        var result = _parser.ParseLine("g01 x10.5 y20.3");

        result.ShouldNotBeNull();
        result.CommandType.ShouldBe("G");
        result.CommandNumber.ShouldBe(1);
        result.Parameters['X'].ShouldBe(10.5m);
        result.Parameters['Y'].ShouldBe(20.3m);
    }

    [Fact]
    public void ParseLine_WithArcParameters_ShouldParseIJK()
    {
        var result = _parser.ParseLine("G02 X10 Y10 I5 J0 F100");

        result.ShouldNotBeNull();
        result.CommandType.ShouldBe("G");
        result.CommandNumber.ShouldBe(2);
        result.Parameters['I'].ShouldBe(5m);
        result.Parameters['J'].ShouldBe(0m);
    }

    [Fact]
    public void ParseLine_WithNegativeValues_ShouldParseCorrectly()
    {
        var result = _parser.ParseLine("G01 X-10.5 Y-20.3 Z-5");

        result.ShouldNotBeNull();
        result.Parameters['X'].ShouldBe(-10.5m);
        result.Parameters['Y'].ShouldBe(-20.3m);
        result.Parameters['Z'].ShouldBe(-5m);
    }

    [Fact]
    public void ParseLine_WithDuplicateParameter_ShouldUseLastValue()
    {
        var result = _parser.ParseLine("G01 X10 X20");

        result.ShouldNotBeNull();
        result.Parameters['X'].ShouldBe(20m);
    }

    [Fact]
    public void IsValidCommand_WithUnsupportedGCode_ShouldReturnFalse()
    {
        var command = new GCodeCommand
        {
            CommandType = "G",
            CommandNumber = 99
        };

        var result = _parser.IsValidCommand(command);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsValidCommand_WithSupportedGCode_ShouldReturnTrue()
    {
        var supportedGCodes = new[] { 0, 1, 2, 3, 17, 18, 19, 20, 21, 28, 30, 90, 91 };

        foreach (var code in supportedGCodes)
        {
            var command = new GCodeCommand
            {
                CommandType = "G",
                CommandNumber = code
            };

            _parser.IsValidCommand(command).ShouldBeTrue($"G{code} should be valid");
        }
    }

    [Fact]
    public void IsValidCommand_WithSupportedMCode_ShouldReturnTrue()
    {
        var supportedMCodes = new[] { 0, 1, 3, 4, 5, 30 };

        foreach (var code in supportedMCodes)
        {
            var command = new GCodeCommand
            {
                CommandType = "M",
                CommandNumber = code
            };

            _parser.IsValidCommand(command).ShouldBeTrue($"M{code} should be valid");
        }
    }

    [Fact]
    public void IsValidCommand_WithValidToolNumber_ShouldReturnTrue()
    {
        for (var i = 0; i <= 99; i++)
        {
            var command = new GCodeCommand
            {
                CommandType = "T",
                CommandNumber = i
            };

            _parser.IsValidCommand(command).ShouldBeTrue($"T{i} should be valid");
        }
    }

    [Fact]
    public void IsValidCommand_WithInvalidToolNumber_ShouldReturnFalse()
    {
        var command = new GCodeCommand
        {
            CommandType = "T",
            CommandNumber = 100
        };

        _parser.IsValidCommand(command).ShouldBeFalse();
    }

    [Fact]
    public void ParseFile_WithValidFile_ShouldParseAllCommands()
    {
        var testFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(testFile, new[]
            {
                "(Test G-code file)",
                "G21",
                "G90",
                "G01 X10 Y20 F100",
                "M03 S1200",
                "",
                "; Comment line",
                "G00 Z5",
                "M30"
            });

            var results = _parser.ParseFile(testFile);

            results.Count.ShouldBe(6); // 6 valid commands
            results[0].CommandNumber.ShouldBe(21); // G21
            results[1].CommandNumber.ShouldBe(90); // G90
            results[2].CommandNumber.ShouldBe(1);  // G01
            results[3].CommandNumber.ShouldBe(3);  // M03
            results[4].CommandNumber.ShouldBe(0);  // G00
            results[5].CommandNumber.ShouldBe(30); // M30
        }
        finally
        {
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public void ParseFile_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        Should.Throw<FileNotFoundException>(() =>
            _parser.ParseFile("nonexistent_file.gcode"));
    }

    [Fact]
    public void ParseFile_WithNullPath_ShouldThrowArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            _parser.ParseFile(null!));
    }
}
