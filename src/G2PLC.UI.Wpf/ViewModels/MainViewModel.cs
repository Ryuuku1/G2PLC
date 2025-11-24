using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using G2PLC.Application.Parsers;
using G2PLC.Application.Mappers;
using G2PLC.Domain.Interfaces;
using G2PLC.Domain.Models;
using G2PLC.Domain.Configuration;
using G2PLC.Infrastructure.Communication;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace G2PLC.UI.Wpf.ViewModels;

/// <summary>
/// Main ViewModel for the G2PLC HMI application.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly GCodeParser _gCodeParser;
    private readonly HowickCsvParser _howickParser;
    private readonly LsfDataMapper _lsfMapper;
    private IModbusClient? _modbusClient;
    private IOpcUaClient? _opcUaClient;
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _fileType = "G-Code";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isConnected = false;

    [ObservableProperty]
    private bool _isRunning = false;

    [ObservableProperty]
    private int _progressPercentage = 0;

    [ObservableProperty]
    private int _currentLine = 0;

    [ObservableProperty]
    private int _totalLines = 0;

    [ObservableProperty]
    private string _plcType = "Modbus";

    [ObservableProperty]
    private string _plcAddress = "127.0.0.1";

    [ObservableProperty]
    private int _plcPort = 502;

    [ObservableProperty]
    private string _opcUaEndpoint = "opc.tcp://localhost:4840";

    [ObservableProperty]
    private ObservableCollection<string> _logMessages = new();

    private List<GCodeCommand>? _gCodeCommands;
    private LsfFrameset? _lsfFrameset;
    private MappingConfiguration _mappingConfiguration;

    public ObservableCollection<string> FileTypes { get; } = new() { "G-Code", "LSF (Howick)" };
    public ObservableCollection<string> PlcTypes { get; } = new() { "Modbus", "OPC UA" };

    public MainViewModel(
        ILogger<MainViewModel> logger,
        ILogger<GCodeParser> gCodeLogger,
        ILogger<HowickCsvParser> howickLogger,
        ILogger<LsfDataMapper> mapperLogger)
    {
        _logger = logger;
        _gCodeParser = new GCodeParser(gCodeLogger);
        _howickParser = new HowickCsvParser(howickLogger);

        _mappingConfiguration = CreateDefaultMappingConfiguration();
        _lsfMapper = new LsfDataMapper(mapperLogger, _mappingConfiguration);

        AddLog("Application initialized");
    }

    private MappingConfiguration CreateDefaultMappingConfiguration()
    {
        return new MappingConfiguration
        {
            RegisterMappings = new RegisterMappings
            {
                Axes = new Dictionary<string, AxisMappingConfig>
                {
                    ["X"] = new() { Address = 100, ScaleFactor = 1000m, Description = "X-axis", AxisType = AxisType.Linear, Unit = "mm" },
                    ["Y"] = new() { Address = 101, ScaleFactor = 1000m, Description = "Y-axis", AxisType = AxisType.Linear, Unit = "mm" },
                    ["Z"] = new() { Address = 102, ScaleFactor = 1000m, Description = "Z-axis", AxisType = AxisType.Linear, Unit = "mm" },
                    ["A"] = new() { Address = 103, ScaleFactor = 1000m, Description = "A-axis", AxisType = AxisType.Rotational, Unit = "degrees" },
                    ["B"] = new() { Address = 104, ScaleFactor = 1000m, Description = "B-axis", AxisType = AxisType.Rotational, Unit = "degrees" },
                    ["C"] = new() { Address = 105, ScaleFactor = 1000m, Description = "C-axis", AxisType = AxisType.Rotational, Unit = "degrees" }
                }
            },
            ValidationRules = new ValidationRules(),
            ProcessingOptions = new ProcessingOptions()
        };
    }

    [RelayCommand]
    private void LoadFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = FileType == "G-Code"
                ? "G-Code Files (*.gcode;*.nc)|*.gcode;*.nc|All Files (*.*)|*.*"
                : "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Title = $"Select {FileType} File"
        };

        if (dialog.ShowDialog() == true)
        {
            FilePath = dialog.FileName;
            AddLog($"File selected: {Path.GetFileName(FilePath)}");
            _ = ParseFileAsync();
        }
    }

    [RelayCommand]
    private async Task ConnectPlcAsync()
    {
        try
        {
            AddLog($"Connecting to {PlcType} PLC...");
            StatusMessage = $"Connecting to {PlcType}...";

            if (PlcType == "Modbus")
            {
                var modbusLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ModbusClientWrapper>.Instance;
                _modbusClient = new ModbusClientWrapper(modbusLogger, PlcAddress, PlcPort);
                await _modbusClient.ConnectAsync();
                IsConnected = _modbusClient.IsConnected;
            }
            else // OPC UA
            {
                var opcLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<OpcUaClientWrapper>.Instance;
                _opcUaClient = new OpcUaClientWrapper(opcLogger, OpcUaEndpoint);
                await _opcUaClient.ConnectAsync();
                IsConnected = _opcUaClient.IsConnected;
            }

            if (IsConnected)
            {
                AddLog($"Connected to {PlcType} successfully");
                StatusMessage = "Connected";
            }
        }
        catch (Exception ex)
        {
            AddLog($"Connection failed: {ex.Message}");
            StatusMessage = "Connection failed";
            MessageBox.Show($"Failed to connect to PLC: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void DisconnectPlc()
    {
        try
        {
            if (_modbusClient != null)
            {
                _modbusClient.Disconnect();
                _modbusClient.Dispose();
                _modbusClient = null;
            }

            if (_opcUaClient != null)
            {
                _opcUaClient.Disconnect();
                _opcUaClient.Dispose();
                _opcUaClient = null;
            }

            IsConnected = false;
            AddLog("Disconnected from PLC");
            StatusMessage = "Disconnected";
        }
        catch (Exception ex)
        {
            AddLog($"Disconnect error: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartExecution))]
    private async Task StartExecutionAsync()
    {
        if (!IsConnected)
        {
            MessageBox.Show("Please connect to PLC first", "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(FilePath) || (_gCodeCommands == null && _lsfFrameset == null))
        {
            MessageBox.Show("Please load a file first", "No File Loaded", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            AddLog("Starting execution...");
            StatusMessage = "Running";

            if (FileType == "G-Code")
            {
                await ExecuteGCodeAsync(_cancellationTokenSource.Token);
            }
            else
            {
                await ExecuteLsfAsync(_cancellationTokenSource.Token);
            }

            if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                AddLog("Execution completed successfully");
                StatusMessage = "Completed";
                MessageBox.Show("Execution completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (OperationCanceledException)
        {
            AddLog("Execution stopped by user");
            StatusMessage = "Stopped";
        }
        catch (Exception ex)
        {
            AddLog($"Execution error: {ex.Message}");
            StatusMessage = "Error";
            MessageBox.Show($"Execution error: {ex.Message}", "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsRunning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStopExecution))]
    private void StopExecution()
    {
        _cancellationTokenSource?.Cancel();
        AddLog("Stopping execution...");
        StatusMessage = "Stopping...";
    }

    private bool CanStartExecution() => !IsRunning && IsConnected;
    private bool CanStopExecution() => IsRunning;

    private async Task ParseFileAsync()
    {
        try
        {
            AddLog($"Parsing {FileType} file...");
            StatusMessage = "Parsing file...";

            if (FileType == "G-Code")
            {
                _gCodeCommands = await _gCodeParser.ParseFileAsync(FilePath);
                TotalLines = _gCodeCommands.Count;
                AddLog($"Parsed {TotalLines} G-Code commands");
            }
            else
            {
                _lsfFrameset = await _howickParser.ParseFileAsync(FilePath);
                TotalLines = _lsfFrameset.Components.Count;
                AddLog($"Parsed LSF frameset with {TotalLines} components");
            }

            StatusMessage = "File parsed successfully";
        }
        catch (Exception ex)
        {
            AddLog($"Parse error: {ex.Message}");
            StatusMessage = "Parse error";
            MessageBox.Show($"Failed to parse file: {ex.Message}", "Parse Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExecuteGCodeAsync(CancellationToken cancellationToken)
    {
        if (_gCodeCommands == null) return;

        CurrentLine = 0;

        foreach (var command in _gCodeCommands)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CurrentLine++;
            ProgressPercentage = (CurrentLine * 100) / TotalLines;

            AddLog($"Executing: {command.CommandType} {string.Join(" ", command.Parameters.Select(p => $"{p.Key}{p.Value}"))}");

            // Write axis positions to PLC
            foreach (var param in command.Parameters)
            {
                if (PlcType == "Modbus" && _modbusClient != null)
                {
                    // For Modbus: Write to register addresses
                    // Simplified example - you'll need proper mapping configuration
                    ushort address = GetRegisterAddress(param.Key);
                    ushort value = (ushort)(param.Value * 1000); // Scale to microns
                    await _modbusClient.WriteHoldingRegisterAsync(address, value);
                }
                else if (PlcType == "OPC UA" && _opcUaClient != null)
                {
                    // For OPC UA: Write to node IDs
                    string nodeId = $"ns=2;s=CNC.Axes.{param.Key}.Position";
                    await _opcUaClient.WriteNodeValueAsync(nodeId, param.Value * 1000);
                }
            }

            // Small delay for demonstration
            await Task.Delay(100, cancellationToken);
        }
    }

    private async Task ExecuteLsfAsync(CancellationToken cancellationToken)
    {
        if (_lsfFrameset == null) return;

        CurrentLine = 0;

        foreach (var component in _lsfFrameset.Components)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CurrentLine++;
            ProgressPercentage = (CurrentLine * 100) / TotalLines;

            AddLog($"Processing component: {component.ComponentId} ({component.Operations.Count} operations)");

            // Map component to registers
            var mappings = _lsfMapper.MapComponentToRegisters(component);

            // Write to PLC
            if (PlcType == "Modbus" && _modbusClient != null)
            {
                foreach (var mapping in mappings)
                {
                    await _modbusClient.WriteHoldingRegisterAsync(mapping.Address, mapping.Value);
                }
            }
            else if (PlcType == "OPC UA" && _opcUaClient != null)
            {
                var nodeValues = new Dictionary<string, object>();
                foreach (var mapping in mappings)
                {
                    nodeValues[$"ns=2;s=LSF.{mapping.ParameterName}"] = mapping.Value;
                }
                await _opcUaClient.WriteMultipleNodeValuesAsync(nodeValues);
            }

            AddLog($"Component {component.ComponentId} written to PLC");

            // Small delay for demonstration
            await Task.Delay(200, cancellationToken);
        }
    }

    private ushort GetRegisterAddress(char axis)
    {
        // Simplified mapping - in production, use proper configuration
        return axis switch
        {
            'X' => 100,
            'Y' => 101,
            'Z' => 102,
            'A' => 103,
            'B' => 104,
            'C' => 105,
            _ => 100
        };
    }

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            LogMessages.Add($"[{timestamp}] {message}");

            // Keep only last 100 messages
            while (LogMessages.Count > 100)
            {
                LogMessages.RemoveAt(0);
            }
        });

        _logger.LogInformation(message);
    }

    partial void OnIsRunningChanged(bool value)
    {
        StartExecutionCommand.NotifyCanExecuteChanged();
        StopExecutionCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsConnectedChanged(bool value)
    {
        StartExecutionCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void EditMappings()
    {
        var mappingWindow = new MappingsEditorWindow(_mappingConfiguration);
        if (mappingWindow.ShowDialog() == true)
        {
            _mappingConfiguration = mappingWindow.MappingConfiguration;
            AddLog("Mappings updated");
        }
    }

    [RelayCommand]
    private async Task LoadMappingsAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            Title = "Load Mappings File"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = await File.ReadAllTextAsync(dialog.FileName);
                var config = System.Text.Json.JsonSerializer.Deserialize<MappingConfiguration>(json);
                if (config != null)
                {
                    _mappingConfiguration = config;
                    AddLog($"Mappings loaded from {Path.GetFileName(dialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                AddLog($"Failed to load mappings: {ex.Message}");
                MessageBox.Show($"Failed to load mappings: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task SaveMappingsAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            Title = "Save Mappings File",
            FileName = "mappings.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_mappingConfiguration, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(dialog.FileName, json);
                AddLog($"Mappings saved to {Path.GetFileName(dialog.FileName)}");
            }
            catch (Exception ex)
            {
                AddLog($"Failed to save mappings: {ex.Message}");
                MessageBox.Show($"Failed to save mappings: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
