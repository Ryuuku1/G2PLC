namespace G2PLC.Domain.Interfaces;

/// <summary>
/// Interface for OPC UA client communication.
/// </summary>
public interface IOpcUaClient : IDisposable
{
    /// <summary>
    /// Connects to the OPC UA server asynchronously.
    /// </summary>
    Task ConnectAsync();

    /// <summary>
    /// Disconnects from the OPC UA server.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Gets a value indicating whether the client is connected to the server.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Writes a value to an OPC UA node asynchronously.
    /// </summary>
    /// <param name="nodeId">The node identifier (e.g., "ns=2;s=MyVariable").</param>
    /// <param name="value">The value to write.</param>
    Task WriteNodeValueAsync(string nodeId, object value);

    /// <summary>
    /// Reads a value from an OPC UA node asynchronously.
    /// </summary>
    /// <param name="nodeId">The node identifier (e.g., "ns=2;s=MyVariable").</param>
    /// <returns>The value read from the node.</returns>
    Task<object?> ReadNodeValueAsync(string nodeId);

    /// <summary>
    /// Writes multiple values to OPC UA nodes asynchronously.
    /// </summary>
    /// <param name="nodeValues">Dictionary of node IDs and their corresponding values.</param>
    Task WriteMultipleNodeValuesAsync(Dictionary<string, object> nodeValues);
}
