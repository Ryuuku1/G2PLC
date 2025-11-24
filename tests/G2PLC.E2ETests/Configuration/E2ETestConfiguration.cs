namespace G2PLC.E2ETests.Configuration;

/// <summary>
/// Configuration for E2E tests.
/// Set UseMockPlc to false to test against a real PLC.
/// </summary>
public class E2ETestConfiguration
{
    /// <summary>
    /// Use mock PLC for testing (true) or real PLC (false).
    /// </summary>
    public bool UseMockPlc { get; set; } = true;

    /// <summary>
    /// Modbus TCP configuration for real PLC testing.
    /// </summary>
    public ModbusConfig Modbus { get; set; } = new();

    /// <summary>
    /// OPC UA configuration for real PLC testing.
    /// </summary>
    public OpcUaConfig OpcUa { get; set; } = new();

    public class ModbusConfig
    {
        public string IpAddress { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 502;
        public int Timeout { get; set; } = 5000;
    }

    public class OpcUaConfig
    {
        public string EndpointUrl { get; set; } = "opc.tcp://localhost:4840";
        public int Timeout { get; set; } = 5000;
        public bool UseSecurity { get; set; } = false;
    }

    /// <summary>
    /// Creates default configuration for mock testing.
    /// </summary>
    public static E2ETestConfiguration CreateMockConfiguration()
    {
        return new E2ETestConfiguration
        {
            UseMockPlc = true,
            Modbus = new ModbusConfig
            {
                IpAddress = "127.0.0.1",
                Port = 5502 // Mock server port
            }
        };
    }

    /// <summary>
    /// Creates configuration for real PLC testing.
    /// Update these values to match your actual PLC.
    /// </summary>
    public static E2ETestConfiguration CreateRealPlcConfiguration()
    {
        return new E2ETestConfiguration
        {
            UseMockPlc = false,
            Modbus = new ModbusConfig
            {
                IpAddress = "192.168.1.100", // Update this!
                Port = 502,
                Timeout = 10000
            },
            OpcUa = new OpcUaConfig
            {
                EndpointUrl = "opc.tcp://192.168.1.100:4840", // Update this!
                Timeout = 10000,
                UseSecurity = false
            }
        };
    }
}
