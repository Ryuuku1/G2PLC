using G2PLC.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace G2PLC.E2ETests.Mocks;

/// <summary>
/// Mock OPC UA client that connects to MockOpcUaServer for testing.
/// </summary>
public class MockOpcUaClient : IOpcUaClient
{
    private readonly ILogger<MockOpcUaClient> _logger;
    private readonly MockOpcUaServer _mockServer;
    private bool _disposed;

    public bool IsConnected { get; private set; }

    public MockOpcUaClient(ILogger<MockOpcUaClient> logger, MockOpcUaServer mockServer)
    {
        _logger = logger;
        _mockServer = mockServer;
    }

    public Task ConnectAsync()
    {
        if (!_mockServer.IsRunning)
        {
            throw new InvalidOperationException("Mock OPC UA server is not running");
        }

        IsConnected = true;
        _logger.LogInformation("Connected to mock OPC UA server");
        return Task.CompletedTask;
    }

    public void Disconnect()
    {
        IsConnected = false;
        _logger.LogInformation("Disconnected from mock OPC UA server");
    }

    public Task WriteNodeValueAsync(string nodeId, object value)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected to OPC UA server");
        }

        _mockServer.WriteNode(nodeId, value);
        _logger.LogDebug("Wrote value {Value} to node {NodeId}", value, nodeId);
        return Task.CompletedTask;
    }

    public Task<object?> ReadNodeValueAsync(string nodeId)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected to OPC UA server");
        }

        var value = _mockServer.ReadNode(nodeId);
        _logger.LogDebug("Read value {Value} from node {NodeId}", value, nodeId);
        return Task.FromResult(value);
    }

    public async Task WriteMultipleNodeValuesAsync(Dictionary<string, object> nodeValues)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected to OPC UA server");
        }

        foreach (var kvp in nodeValues)
        {
            await WriteNodeValueAsync(kvp.Key, kvp.Value);
        }

        _logger.LogDebug("Wrote {Count} values to OPC UA nodes", nodeValues.Count);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Disconnect();
            _disposed = true;
        }
    }
}
