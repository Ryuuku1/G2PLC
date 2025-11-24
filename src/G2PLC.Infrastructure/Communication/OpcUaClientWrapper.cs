using G2PLC.Domain.Interfaces;
using G2PLC.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace G2PLC.Infrastructure.Communication;

/// <summary>
/// Wrapper for OPC UA client communication using the OPC Foundation SDK.
/// </summary>
public class OpcUaClientWrapper : IOpcUaClient
{
    private readonly ILogger<OpcUaClientWrapper> _logger;
    private readonly string _endpointUrl;
    private readonly int _timeout;
    private readonly bool _useSecurity;
    private ISession? _session;
    private ApplicationConfiguration? _configuration;
    private bool _disposed;

    public bool IsConnected => _session != null && _session.Connected;

    public OpcUaClientWrapper(
        ILogger<OpcUaClientWrapper> logger,
        string endpointUrl,
        int timeout = 5000,
        bool useSecurity = false)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _endpointUrl = endpointUrl ?? throw new ArgumentNullException(nameof(endpointUrl));
        _timeout = timeout;
        _useSecurity = useSecurity;
    }

    public async Task ConnectAsync()
    {
        if (IsConnected)
        {
            _logger.LogWarning("Already connected to OPC UA server: {EndpointUrl}", _endpointUrl);
            return;
        }

        try
        {
            _logger.LogInformation("Connecting to OPC UA server: {EndpointUrl}", _endpointUrl);

            // Create application configuration
            _configuration = await CreateApplicationConfigurationAsync();

            // Discover endpoints using configuration
            var endpointDescription = CoreClientUtils.SelectEndpoint(_configuration, _endpointUrl, _useSecurity);

            _logger.LogDebug("Selected endpoint: {EndpointUrl}, Security: {SecurityPolicyUri}",
                endpointDescription.EndpointUrl, endpointDescription.SecurityPolicyUri);

            // Create endpoint configuration
            var endpointConfiguration = EndpointConfiguration.Create(_configuration);
            endpointConfiguration.OperationTimeout = _timeout;

            // Create configured endpoint
            var endpoint = new ConfiguredEndpoint(
                collection: null,
                description: endpointDescription,
                configuration: endpointConfiguration);

            // Create session using the new API
            _session = await Session.Create(
                configuration: _configuration,
                endpoint: endpoint,
                updateBeforeConnect: false,
                checkDomain: false,
                sessionName: "G2PLC Session",
                sessionTimeout: (uint)_timeout,
                identity: new UserIdentity(new AnonymousIdentityToken()),
                preferredLocales: null,
                ct: CancellationToken.None);

            _logger.LogInformation("Successfully connected to OPC UA server: {EndpointUrl}", _endpointUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to OPC UA server: {EndpointUrl}", _endpointUrl);
            throw new ModbusConnectionException($"Failed to connect to OPC UA server: {_endpointUrl}", ex);
        }
    }

    public void Disconnect()
    {
        if (_session != null)
        {
            try
            {
                _logger.LogInformation("Disconnecting from OPC UA server");
                _session.CloseAsync().GetAwaiter().GetResult();
                _session.Dispose();
                _session = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from OPC UA server");
            }
        }
    }

    public async Task WriteNodeValueAsync(string nodeId, object value)
    {
        EnsureConnected();

        try
        {
            var nodeIdObj = new NodeId(nodeId);

            var writeValue = new WriteValue
            {
                NodeId = nodeIdObj,
                AttributeId = Attributes.Value,
                Value = new DataValue(new Variant(value))
            };

            var writeRequest = new WriteRequest
            {
                NodesToWrite = new WriteValueCollection { writeValue }
            };

            var response = await _session!.WriteAsync(
                requestHeader: null,
                nodesToWrite: writeRequest.NodesToWrite,
                ct: CancellationToken.None);

            if (StatusCode.IsBad(response.Results[0]))
            {
                throw new ModbusWriteException(
                    $"Failed to write to OPC UA node {nodeId}: {response.Results[0]}");
            }

            _logger.LogDebug("Successfully wrote value to node {NodeId}: {Value}", nodeId, value);
        }
        catch (Exception ex) when (ex is not ModbusWriteException)
        {
            _logger.LogError(ex, "Error writing to OPC UA node {NodeId}", nodeId);
            throw new ModbusWriteException($"Error writing to OPC UA node {nodeId}", ex);
        }
    }

    public async Task<object?> ReadNodeValueAsync(string nodeId)
    {
        EnsureConnected();

        try
        {
            var nodeIdObj = new NodeId(nodeId);

            var response = await _session!.ReadAsync(
                requestHeader: null,
                maxAge: 0,
                timestampsToReturn: TimestampsToReturn.Neither,
                nodesToRead: new ReadValueIdCollection
                {
                    new ReadValueId
                    {
                        NodeId = nodeIdObj,
                        AttributeId = Attributes.Value
                    }
                },
                ct: CancellationToken.None);

            if (StatusCode.IsBad(response.Results[0].StatusCode))
            {
                throw new Exception($"Failed to read OPC UA node {nodeId}: {response.Results[0].StatusCode}");
            }

            var value = response.Results[0].Value;
            _logger.LogDebug("Successfully read value from node {NodeId}: {Value}", nodeId, value);

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from OPC UA node {NodeId}", nodeId);
            throw;
        }
    }

    public async Task WriteMultipleNodeValuesAsync(Dictionary<string, object> nodeValues)
    {
        EnsureConnected();

        if (nodeValues == null || nodeValues.Count == 0)
        {
            return;
        }

        try
        {
            var writeValues = new WriteValueCollection();

            foreach (var kvp in nodeValues)
            {
                writeValues.Add(new WriteValue
                {
                    NodeId = new NodeId(kvp.Key),
                    AttributeId = Attributes.Value,
                    Value = new DataValue(new Variant(kvp.Value))
                });
            }

            var response = await _session!.WriteAsync(
                requestHeader: null,
                nodesToWrite: writeValues,
                ct: CancellationToken.None);

            // Check for errors
            for (int i = 0; i < response.Results.Count; i++)
            {
                if (StatusCode.IsBad(response.Results[i]))
                {
                    var nodeId = nodeValues.Keys.ElementAt(i);
                    _logger.LogError("Failed to write to OPC UA node {NodeId}: {Status}",
                        nodeId, response.Results[i]);
                }
            }

            _logger.LogDebug("Successfully wrote {Count} values to OPC UA nodes", nodeValues.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing multiple values to OPC UA nodes");
            throw new ModbusWriteException("Error writing multiple values to OPC UA nodes", ex);
        }
    }

    private void EnsureConnected()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException(
                "Not connected to OPC UA server. Call ConnectAsync first.");
        }
    }

    private async Task<ApplicationConfiguration> CreateApplicationConfigurationAsync()
    {
        var application = new ApplicationInstance
        {
            ApplicationName = "G2PLC OPC UA Client",
            ApplicationType = ApplicationType.Client,
            ConfigSectionName = "G2PLC.OpcUaClient"
        };

        // Build the application configuration
        var config = await application.Build(
            applicationUri: "urn:G2PLC:OpcUaClient",
            productUri: "urn:G2PLC")
            .AsClient()
            .AddSecurityConfiguration(new CertificateIdentifierCollection())
            .CreateAsync();

        // Note: Certificate checking is handled automatically by the SDK
        // For production, configure proper certificates in the security configuration

        return config;
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
