using G2PLC.Application.Mappers;
using G2PLC.Application.Parsers;
using G2PLC.Domain.Configuration;
using G2PLC.Domain.Interfaces;
using G2PLC.Domain.Models;
using G2PLC.E2ETests.Configuration;
using G2PLC.E2ETests.Mocks;
using G2PLC.Infrastructure.Communication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace G2PLC.E2ETests;

/// <summary>
/// Base class for E2E tests providing common setup and utilities.
/// </summary>
public abstract class E2ETestBase : IDisposable
{
    protected readonly E2ETestConfiguration Configuration;
    protected readonly ILogger Logger;

    // Mock servers (only used when UseMockPlc = true)
    protected MockModbusServer? MockModbusServer;
    protected MockOpcUaServer? MockOpcUaServer;

    // Parsers
    protected readonly GCodeParser GCodeParser;
    protected readonly HowickCsvParser HowickParser;

    // Mappers
    protected readonly LsfDataMapper LsfMapper;

    protected E2ETestBase()
    {
        // Load configuration - can be overridden in derived classes
        Configuration = E2ETestConfiguration.CreateMockConfiguration();

        Logger = NullLogger.Instance;

        // Initialize parsers
        var gCodeLogger = NullLogger<GCodeParser>.Instance;
        var howickLogger = NullLogger<HowickCsvParser>.Instance;
        GCodeParser = new GCodeParser(gCodeLogger);
        HowickParser = new HowickCsvParser(howickLogger);

        // Initialize mapper
        var mapperLogger = NullLogger<LsfDataMapper>.Instance;
        var mappingConfig = new MappingConfiguration
        {
            RegisterMappings = new RegisterMappings(),
            ValidationRules = new ValidationRules(),
            ProcessingOptions = new ProcessingOptions()
        };
        LsfMapper = new LsfDataMapper(mapperLogger, mappingConfig);

        // Setup mock servers if needed
        if (Configuration.UseMockPlc)
        {
            SetupMockServers();
        }
    }

    protected virtual void SetupMockServers()
    {
        MockModbusServer = new MockModbusServer(Configuration.Modbus.Port);
        MockModbusServer.Start();

        MockOpcUaServer = new MockOpcUaServer(Configuration.OpcUa.EndpointUrl);
        MockOpcUaServer.Start();
    }

    /// <summary>
    /// Parses G-Code text by creating a temporary file.
    /// </summary>
    protected async Task<List<GCodeCommand>> ParseGCodeTextAsync(string gcodeText)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, gcodeText);
            return await GCodeParser.ParseFileAsync(tempFile);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    /// <summary>
    /// Creates a Modbus client (mock or real based on configuration).
    /// </summary>
    protected IModbusClient CreateModbusClient()
    {
        if (Configuration.UseMockPlc && MockModbusServer != null)
        {
            var logger = NullLogger<MockModbusClient>.Instance;
            return new MockModbusClient(logger, MockModbusServer);
        }
        else
        {
            var logger = NullLogger<ModbusClientWrapper>.Instance;
            return new ModbusClientWrapper(
                logger,
                Configuration.Modbus.IpAddress,
                Configuration.Modbus.Port);
        }
    }

    /// <summary>
    /// Creates an OPC UA client (mock or real based on configuration).
    /// </summary>
    protected IOpcUaClient CreateOpcUaClient()
    {
        if (Configuration.UseMockPlc && MockOpcUaServer != null)
        {
            var logger = NullLogger<MockOpcUaClient>.Instance;
            return new MockOpcUaClient(logger, MockOpcUaServer);
        }
        else
        {
            var logger = NullLogger<OpcUaClientWrapper>.Instance;
            return new OpcUaClientWrapper(
                logger,
                Configuration.OpcUa.EndpointUrl,
                Configuration.OpcUa.Timeout,
                Configuration.OpcUa.UseSecurity);
        }
    }

    /// <summary>
    /// Gets register values from the PLC (mock or real). Only returns non-zero registers.
    /// </summary>
    protected Dictionary<ushort, ushort> GetModbusRegisters()
    {
        if (Configuration.UseMockPlc && MockModbusServer != null)
        {
            return MockModbusServer.GetNonZeroRegisters();
        }

        throw new InvalidOperationException("Cannot get registers from real PLC directly. Use read operations instead.");
    }

    /// <summary>
    /// Gets all register values from the PLC (mock or real), including zero values.
    /// </summary>
    protected Dictionary<ushort, ushort> GetAllModbusRegisters()
    {
        if (Configuration.UseMockPlc && MockModbusServer != null)
        {
            return MockModbusServer.GetAllRegisters();
        }

        throw new InvalidOperationException("Cannot get registers from real PLC directly. Use read operations instead.");
    }

    /// <summary>
    /// Gets OPC UA node values (mock or real).
    /// </summary>
    protected Dictionary<string, object> GetOpcUaNodes()
    {
        if (Configuration.UseMockPlc && MockOpcUaServer != null)
        {
            return MockOpcUaServer.GetNonDefaultNodes();
        }

        throw new InvalidOperationException("Cannot get all nodes from real OPC UA server directly. Use read operations instead.");
    }

    /// <summary>
    /// Resets PLC state (only works with mock).
    /// </summary>
    protected void ResetPlcState()
    {
        if (Configuration.UseMockPlc)
        {
            MockModbusServer?.ResetRegisters();
            MockOpcUaServer?.ResetNodes();
        }
        else
        {
            throw new InvalidOperationException("Cannot reset real PLC state from tests.");
        }
    }

    public virtual void Dispose()
    {
        MockModbusServer?.Dispose();
        MockOpcUaServer?.Dispose();
    }
}
