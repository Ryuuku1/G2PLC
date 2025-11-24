using System.Collections.Concurrent;

namespace G2PLC.E2ETests.Mocks;

/// <summary>
/// Mock OPC UA server for E2E testing.
/// Simulates an OPC UA server by maintaining nodes in memory.
/// </summary>
public class MockOpcUaServer : IDisposable
{
    private readonly ConcurrentDictionary<string, object> _nodes;
    private bool _disposed;

    public bool IsRunning { get; private set; }
    public string EndpointUrl { get; }

    /// <summary>
    /// Gets a snapshot of all nodes.
    /// </summary>
    public IReadOnlyDictionary<string, object> Nodes =>
        new Dictionary<string, object>(_nodes);

    public MockOpcUaServer(string endpointUrl = "opc.tcp://localhost:4840")
    {
        EndpointUrl = endpointUrl;
        _nodes = new ConcurrentDictionary<string, object>();
        IsRunning = false;
    }

    /// <summary>
    /// Starts the mock OPC UA server.
    /// </summary>
    public void Start()
    {
        if (IsRunning)
            return;

        IsRunning = true;

        // Initialize some default nodes
        InitializeDefaultNodes();
    }

    /// <summary>
    /// Stops the mock OPC UA server.
    /// </summary>
    public void Stop()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
    }

    /// <summary>
    /// Writes a value to a node.
    /// </summary>
    public void WriteNode(string nodeId, object value)
    {
        _nodes[nodeId] = value;
    }

    /// <summary>
    /// Reads a value from a node.
    /// </summary>
    public object? ReadNode(string nodeId)
    {
        return _nodes.TryGetValue(nodeId, out var value) ? value : null;
    }

    /// <summary>
    /// Resets all nodes to their default values.
    /// </summary>
    public void ResetNodes()
    {
        _nodes.Clear();
        InitializeDefaultNodes();
    }

    /// <summary>
    /// Gets all non-zero/non-empty nodes.
    /// </summary>
    public Dictionary<string, object> GetNonDefaultNodes()
    {
        return _nodes
            .Where(kvp => !IsDefaultValue(kvp.Value))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private void InitializeDefaultNodes()
    {
        // Initialize CNC axis nodes
        var axes = new[] { "X", "Y", "Z", "A", "B", "C" };
        foreach (var axis in axes)
        {
            _nodes[$"ns=2;s=CNC.Axes.{axis}.Position"] = 0.0;
            _nodes[$"ns=2;s=CNC.Axes.{axis}.Speed"] = 0.0;
        }

        // Initialize LSF nodes
        _nodes["ns=2;s=LSF.FramesetId"] = 0;
        _nodes["ns=2;s=LSF.ComponentCount"] = 0;
    }

    private bool IsDefaultValue(object value)
    {
        return value switch
        {
            int i => i == 0,
            double d => d == 0.0,
            float f => f == 0.0f,
            string s => string.IsNullOrEmpty(s),
            _ => false
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }
}
