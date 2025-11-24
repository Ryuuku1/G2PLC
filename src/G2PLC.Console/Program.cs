using G2PLC.Application.Mappers;
using G2PLC.Application.Parsers;
using G2PLC.Domain.Configuration;
using G2PLC.Domain.Interfaces;
using G2PLC.Infrastructure.Communication;
using G2PLC.Infrastructure.Configuration;
using G2PLC.Infrastructure.Exceptions;
using G2PLC.Infrastructure.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace G2PLC.Console;

internal class Program
{
    private const int ExitSuccess = 0;
    private const int ExitConfigError = 1;
    private const int ExitFileNotFound = 2;
    private const int ExitPlcConnectionFailed = 3;
    private const int ExitParseError = 4;
    private const int ExitWriteError = 5;
    private const int ExitUnhandledException = 99;

    private static async Task<int> Main(string[] args)
    {
        var host = CreateHostBuilder(args);

        try
        {
            var app = host.Services.GetRequiredService<Application>();
            return await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"\nFATAL ERROR: {ex.Message}");
            System.Console.ResetColor();
            return ExitUnhandledException;
        }
    }

    private static IHost CreateHostBuilder(string[] args)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "mappings.json");

        MappingConfiguration? mappingConfiguration;

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                SetupLogging(services);

                var serviceProvider = services.BuildServiceProvider();
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                var configLoader = new JsonConfigurationLoader(serviceProvider.GetRequiredService<ILogger<JsonConfigurationLoader>>());

                try
                {
                    mappingConfiguration = configLoader.LoadMappingConfigurationAsync(configPath).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to load configuration from {Path}. Using default configuration.", configPath);
                    mappingConfiguration = CreateDefaultConfiguration();
                }

                services.AddSingleton(mappingConfiguration);
                services.AddSingleton<IConfigurationLoader, JsonConfigurationLoader>();
                services.AddTransient<IGCodeParser, GCodeParser>();
                services.AddTransient<IDataMapper, PlcDataMapper>();
                services.AddTransient(CreateModbusClient);
                services.AddTransient<Application>();
            })
            .Build();

        return host;
    }

    private static void SetupLogging(IServiceCollection services)
    {
        var logFilePath = AppConfigurationManager.GetString("Log_FilePath", "logs/gcode_to_plc.log");
        var logDirectory = Path.GetDirectoryName(logFilePath);

        if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
            builder.AddFile(logFilePath);
            builder.SetMinimumLevel(LogLevel.Debug);
        });
    }

    private static MappingConfiguration CreateDefaultConfiguration()
    {
        return new MappingConfiguration
        {
            RegisterMappings = new RegisterMappings
            {
                Axes = new Dictionary<string, AxisMappingConfig>
                {
                    ["X"] = new() { Address = 0, ScaleFactor = 1000m, Description = "X-axis linear position", AxisType = AxisType.Linear, Unit = "mm" },
                    ["Y"] = new() { Address = 1, ScaleFactor = 1000m, Description = "Y-axis linear position", AxisType = AxisType.Linear, Unit = "mm" },
                    ["Z"] = new() { Address = 2, ScaleFactor = 1000m, Description = "Z-axis linear position", AxisType = AxisType.Linear, Unit = "mm" },
                    ["U"] = new() { Address = 10, ScaleFactor = 1000m, Description = "U-axis (parallel to X)", AxisType = AxisType.Linear, Unit = "mm" },
                    ["V"] = new() { Address = 11, ScaleFactor = 1000m, Description = "V-axis (parallel to Y)", AxisType = AxisType.Linear, Unit = "mm" },
                    ["W"] = new() { Address = 12, ScaleFactor = 1000m, Description = "W-axis (parallel to Z)", AxisType = AxisType.Linear, Unit = "mm" },
                    ["A"] = new() { Address = 13, ScaleFactor = 1000m, Description = "A-axis rotation around X", AxisType = AxisType.Rotational, Unit = "degrees" },
                    ["B"] = new() { Address = 14, ScaleFactor = 1000m, Description = "B-axis rotation around Y", AxisType = AxisType.Rotational, Unit = "degrees" },
                    ["C"] = new() { Address = 15, ScaleFactor = 1000m, Description = "C-axis rotation around Z", AxisType = AxisType.Rotational, Unit = "degrees" }
                },
                Commands = new Dictionary<string, RegisterMappingConfig>
                {
                    ["GCommand"] = new() { Address = 6, ScaleFactor = 1m, Description = "G command" },
                    ["MCommand"] = new() { Address = 7, ScaleFactor = 1m, Description = "M command" }
                },
                Parameters = new Dictionary<string, RegisterMappingConfig>
                {
                    ["FeedRate"] = new() { Address = 3, ScaleFactor = 10m, Description = "Feed rate" },
                    ["SpindleSpeed"] = new() { Address = 4, ScaleFactor = 1m, Description = "Spindle speed" },
                    ["ToolNumber"] = new() { Address = 5, ScaleFactor = 1m, Description = "Tool number" }
                },
                DigitalOutputs = new Dictionary<string, DigitalIoMappingConfig>
                {
                    ["CoolantFlood"] = new() { Address = 100, TriggerMCode = 8, TriggerValue = true, Description = "Flood coolant" },
                    ["CoolantMist"] = new() { Address = 101, TriggerMCode = 7, TriggerValue = true, Description = "Mist coolant" },
                    ["SpindleEnable"] = new() { Address = 102, TriggerMCode = 3, TriggerValue = true, Description = "Spindle enable" },
                    ["SpindleDirection"] = new() { Address = 103, Description = "Spindle direction (CW/CCW)" }
                },
                CustomRegisters = new Dictionary<string, RegisterMappingConfig>()
            },
            ValidationRules = new ValidationRules
            {
                Position = new() { MinValue = -9999m, MaxValue = 9999m, ClampNegativeToZero = true },
                FeedRate = new() { MinValue = 0m, MaxValue = 10000m },
                SpindleSpeed = new() { MinValue = 0m, MaxValue = 24000m },
                ToolNumber = new() { MinValue = 0m, MaxValue = 99m }
            },
            ProcessingOptions = new ProcessingOptions
            {
                DelayBetweenCommandsMs = 100,
                VerifyWritesEnabled = false,
                ContinueOnError = true,
                MaxRetries = 3
            }
        };
    }

    private static IModbusClient CreateModbusClient(IServiceProvider sp)
    {
        var logger = sp.GetRequiredService<ILogger<ModbusClientWrapper>>();
        var plcIpAddress = AppConfigurationManager.GetString("PLC_IpAddress", "192.168.1.100");
        var plcPort = AppConfigurationManager.GetInt("PLC_Port", 502);
        var plcSlaveId = AppConfigurationManager.GetByte("PLC_SlaveId", 1);
        var plcConnectionTimeout = AppConfigurationManager.GetInt("PLC_ConnectionTimeout", 5000);

        return new ModbusClientWrapper(logger, plcIpAddress, plcPort, plcSlaveId, plcConnectionTimeout);
    }

    private class Application
    {
        private readonly ILogger<Application> _logger;
        private readonly IGCodeParser _gcodeParser;
        private readonly IDataMapper _dataMapper;
        private readonly IModbusClient _modbusClient;
        private readonly MappingConfiguration _configuration;

        public Application(
            ILogger<Application> logger,
            IGCodeParser gcodeParser,
            IDataMapper dataMapper,
            IModbusClient modbusClient,
            MappingConfiguration configuration)
        {
            _logger = logger;
            _gcodeParser = gcodeParser;
            _dataMapper = dataMapper;
            _modbusClient = modbusClient;
            _configuration = configuration;
        }

        public async Task<int> RunAsync(string[] args)
        {
            try
            {
                LogApplicationStart();

                var config = ParseCommandLineArguments(args);

                if (config.ContainsKey("help"))
                {
                    DisplayHelp();
                    return ExitSuccess;
                }

                var gcodeFilePath = GetGCodeFilePath(config);
                var verifyWrites = ShouldVerifyWrites(config);
                var verbose = config.ContainsKey("verbose");

                LogConfiguration(gcodeFilePath, verifyWrites);

                await ConnectToPlcAsync();

                if (!File.Exists(gcodeFilePath))
                {
                    _logger.LogError("G-code file not found: {FilePath}", gcodeFilePath);
                    return ExitFileNotFound;
                }

                var result = await ProcessGCodeFileAsync(gcodeFilePath, verifyWrites, verbose);

                return result;
            }
            catch (ModbusConnectionException ex)
            {
                _logger.LogError(ex, "Failed to connect to PLC");
                return ExitPlcConnectionFailed;
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "File not found");
                return ExitFileNotFound;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                return ExitUnhandledException;
            }
            finally
            {
                await CleanupAsync();
            }
        }

        private void LogApplicationStart()
        {
            _logger.LogInformation("========================================");
            _logger.LogInformation("G-code to PLC Application Starting");
            _logger.LogInformation("========================================");
        }

        private static string GetGCodeFilePath(Dictionary<string, string> config)
        {
            return config.TryGetValue("file", out var value)
                ? value
                : AppConfigurationManager.GetString("GCode_FilePath", "sample_program.gcode");
        }

        private bool ShouldVerifyWrites(Dictionary<string, string> config)
        {
            return config.ContainsKey("verify") ||
                   _configuration.ProcessingOptions.VerifyWritesEnabled;
        }

        private void LogConfiguration(string gcodeFilePath, bool verifyWrites)
        {
            _logger.LogInformation("G-code File: {FilePath}", gcodeFilePath);
            _logger.LogInformation("Verify Writes: {VerifyWrites}", verifyWrites);
        }

        private async Task ConnectToPlcAsync()
        {
            await _modbusClient.ConnectAsync();
        }

        private async Task<int> ProcessGCodeFileAsync(string gcodeFilePath, bool verifyWrites, bool verbose)
        {
            var commands = await _gcodeParser.ParseFileAsync(gcodeFilePath);
            _logger.LogInformation("Parsed {Count} commands from file", commands.Count);

            var stopwatch = Stopwatch.StartNew();
            var processedCount = 0;
            var errorCount = 0;

            System.Console.WriteLine($"\nProcessing {commands.Count} commands...\n");

            for (var i = 0; i < commands.Count; i++)
            {
                if ((i + 1) % 10 == 0 || i == commands.Count - 1)
                {
                    System.Console.Write($"\rProgress: {i + 1}/{commands.Count} commands");
                }

                var command = commands[i];
                var result = await ProcessSingleCommandAsync(command, verifyWrites, verbose);
                processedCount++;
                errorCount += result;

                if (_configuration.ProcessingOptions.DelayBetweenCommandsMs > 0)
                {
                    await Task.Delay(_configuration.ProcessingOptions.DelayBetweenCommandsMs);
                }
            }

            stopwatch.Stop();

            DisplaySummary(commands.Count, processedCount, errorCount, stopwatch);

            return errorCount > 0 ? ExitWriteError : ExitSuccess;
        }

        private async Task<int> ProcessSingleCommandAsync(
            Domain.Models.GCodeCommand command,
            bool verifyWrites,
            bool verbose)
        {
            var validationMessages = ValidationHelper.ValidateGCodeCommand(command);
            LogValidationWarnings(validationMessages);

            var registerMappings = _dataMapper.MapToRegisters(command);

            if (verbose)
            {
                System.Console.WriteLine($"\n{command}");
            }

            var errorCount = 0;
            foreach (var mapping in registerMappings)
            {
                errorCount += await WriteMappingAsync(mapping, verifyWrites, verbose);
            }

            return errorCount;
        }

        private void LogValidationWarnings(List<string> validationMessages)
        {
            foreach (var msg in validationMessages)
            {
                _logger.LogWarning("Validation: {Message}", msg);
            }
        }

        private async Task<int> WriteMappingAsync(
            Domain.Models.RegisterMapping mapping,
            bool verifyWrites,
            bool verbose)
        {
            try
            {
                await _modbusClient.WriteHoldingRegisterAsync(mapping.Address, mapping.Value);

                if (verbose)
                {
                    System.Console.WriteLine($"  -> {mapping}");
                }

                if (verifyWrites)
                {
                    return await VerifyWriteAsync(mapping);
                }

                return 0;
            }
            catch (ModbusWriteException ex)
            {
                _logger.LogError(ex, "Failed to write register {Address}", mapping.Address);
                return 1;
            }
            catch (ModbusTimeoutException ex)
            {
                _logger.LogError(ex, "Timeout writing register {Address}", mapping.Address);
                return 1;
            }
        }

        private async Task<int> VerifyWriteAsync(Domain.Models.RegisterMapping mapping)
        {
            var readValues = await _modbusClient.ReadHoldingRegistersAsync(mapping.Address, 1);
            if (readValues[0] == mapping.Value) return 0;
            _logger.LogError(
                "Verification failed for register {Address}: expected {Expected}, got {Actual}",
                mapping.Address,
                mapping.Value,
                readValues[0]);
            return 1;
        }

        private void DisplaySummary(int totalLines, int processedCount, int errorCount, Stopwatch stopwatch)
        {
            System.Console.WriteLine("\n");
            System.Console.WriteLine("========================================");
            System.Console.WriteLine("Processing Complete");
            System.Console.WriteLine("========================================");
            System.Console.WriteLine($"Total Commands: {totalLines}");
            System.Console.WriteLine($"Processed Commands: {processedCount}");
            System.Console.WriteLine($"Errors: {errorCount}");
            System.Console.WriteLine($"Elapsed Time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            System.Console.WriteLine($"Throughput: {processedCount / stopwatch.Elapsed.TotalSeconds:F2} commands/second");
            System.Console.WriteLine("========================================");

            _logger.LogInformation(
                "Processing complete: {ProcessedCount} commands processed, {ErrorCount} errors",
                processedCount,
                errorCount);
            _logger.LogInformation("Elapsed time: {ElapsedSeconds:F2} seconds", stopwatch.Elapsed.TotalSeconds);
        }

        private async Task CleanupAsync()
        {
            try
            {
                _modbusClient.Disconnect();
                _modbusClient.Dispose();
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }

            _logger.LogInformation("Application shutdown");
        }

        private static Dictionary<string, string> ParseCommandLineArguments(string[] args)
        {
            var config = new Dictionary<string, string>();

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i].ToLowerInvariant();

                switch (arg)
                {
                    case "--ip":
                        if (i + 1 < args.Length)
                        {
                            config["ip"] = args[++i];
                        }
                        break;

                    case "--port":
                        if (i + 1 < args.Length)
                        {
                            config["port"] = args[++i];
                        }
                        break;

                    case "--file":
                        if (i + 1 < args.Length)
                        {
                            config["file"] = args[++i];
                        }
                        break;

                    case "--verbose":
                        config["verbose"] = "true";
                        break;

                    case "--verify":
                        config["verify"] = "true";
                        break;

                    case "--help":
                    case "-h":
                        config["help"] = "true";
                        break;
                }
            }

            return config;
        }

        private static void DisplayHelp()
        {
            System.Console.WriteLine("G-code to PLC Modbus Application");
            System.Console.WriteLine("=================================");
            System.Console.WriteLine();
            System.Console.WriteLine("Usage: G2PLC.Console [options]");
            System.Console.WriteLine();
            System.Console.WriteLine("Options:");
            System.Console.WriteLine("  --ip <address>      Override PLC IP address");
            System.Console.WriteLine("  --port <number>     Override PLC port");
            System.Console.WriteLine("  --file <path>       Specify G-code file path");
            System.Console.WriteLine("  --verbose           Enable verbose output");
            System.Console.WriteLine("  --verify            Enable register verification after writes");
            System.Console.WriteLine("  --help, -h          Display this help message");
            System.Console.WriteLine();
            System.Console.WriteLine("Exit Codes:");
            System.Console.WriteLine("  0  - Success");
            System.Console.WriteLine("  1  - Configuration error");
            System.Console.WriteLine("  2  - File not found");
            System.Console.WriteLine("  3  - PLC connection failed");
            System.Console.WriteLine("  4  - Parse error");
            System.Console.WriteLine("  5  - Write error");
            System.Console.WriteLine("  99 - Unhandled exception");
        }
    }
}
