namespace G2PLC.Domain.Configuration;

/// <summary>
/// Configuration for OPC UA server connection.
/// </summary>
public class OpcUaConfiguration
{
    /// <summary>
    /// Gets or sets the OPC UA server endpoint URL (e.g., "opc.tcp://localhost:4840").
    /// </summary>
    public string EndpointUrl { get; set; } = "opc.tcp://localhost:4840";

    /// <summary>
    /// Gets or sets the connection timeout in milliseconds.
    /// </summary>
    public int Timeout { get; set; } = 5000;

    /// <summary>
    /// Gets or sets whether to use security (certificates).
    /// </summary>
    public bool UseSecurity { get; set; } = false;

    /// <summary>
    /// Gets or sets the namespace index for custom nodes (default namespace is usually 2).
    /// </summary>
    public int NamespaceIndex { get; set; } = 2;

    /// <summary>
    /// Gets or sets the base node path for variables (e.g., "MyPLC" for nodes like "ns=2;s=MyPLC.Variable1").
    /// </summary>
    public string BaseNodePath { get; set; } = string.Empty;
}
