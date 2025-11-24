# G2PLC WPF HMI Application

## Overview

The G2PLC HMI (Human-Machine Interface) is a modern WPF application that provides a user-friendly graphical interface for controlling CNC to PLC communication. It supports both **G-Code** and **LSF (Howick)** file formats and can communicate with PLCs via **Modbus TCP** or **OPC UA** protocols.

## Features

### ✅ User-Friendly Interface
- Clean, modern Material Design-inspired UI
- Intuitive workflow from file loading to execution
- Real-time status indicators and progress tracking
- Comprehensive logging console

### ✅ File Support
- **G-Code Files** (.gcode, .nc)
  - Parse standard G-Code commands
  - Extract axis positions (X, Y, Z, A, B, C, etc.)
  - Support for unlimited axes

- **LSF (Howick) Files** (.csv)
  - Parse Howick V1 machine CSV format
  - Extract components and operations
  - Map to PLC registers automatically

### ✅ PLC Communication
- **Modbus TCP**
  - Configurable IP address and port
  - Standard Modbus holding registers
  - Simple numeric addressing

- **OPC UA**
  - Modern industrial protocol
  - Hierarchical node addressing
  - Secure communication support
  - Rich data types

### ✅ Execution Control
- **Start** button with automatic continuous execution
- **Stop** button to cancel execution at any time
- Real-time progress tracking (line/component count)
- Progress percentage with visual progress bar
- Detailed logging of all operations

## Architecture

The HMI follows the **MVVM (Model-View-ViewModel)** pattern with clean separation of concerns:

```
G2PLC.UI.Wpf/
├── App.xaml / App.xaml.cs          # Application entry point with DI
├── MainWindow.xaml / .cs           # Main UI view
├── ViewModels/
│   └── MainViewModel.cs            # Main ViewModel with business logic
└── Converters/
    ├── InverseBoolConverter.cs     # Bool inversion for UI bindings
    ├── ConnectionStatusColorConverter.cs
    ├── ConnectionStatusTextConverter.cs
    └── PlcTypeVisibilityConverter.cs
```

### Key Technologies
- **.NET 9.0** with Windows target
- **WPF** for rich desktop UI
- **CommunityToolkit.Mvvm** for MVVM helpers
- **Microsoft.Extensions.DependencyInjection** for IoC
- **Microsoft.Extensions.Logging** for logging

## User Guide

### 1. Load a File

1. Select the **File Type** from the dropdown:
   - `G-Code` for CNC programs
   - `LSF (Howick)` for light steel framing data

2. Click **Browse...** to open a file dialog

3. Select your file - it will be automatically parsed

4. Check the log console for parsing results

### 2. Configure PLC Connection

1. Select the **PLC Type**:
   - `Modbus` for Modbus TCP
   - `OPC UA` for OPC UA communication

2. Enter connection details:
   - **Modbus**: IP Address (e.g., `192.168.1.100`) and Port (default: `502`)
   - **OPC UA**: Endpoint URL (e.g., `opc.tcp://192.168.1.100:4840`)

3. Click **Connect** to establish the connection

4. Wait for the green "PLC Connected" status indicator

### 3. Execute the Program

1. Ensure the PLC is connected (green status indicator)

2. Ensure a file is loaded (check the file path)

3. Click **▶ Start** to begin execution

4. Monitor progress:
   - Current line/component counter
   - Progress percentage
   - Progress bar
   - Detailed log messages

5. Click **⏹ Stop** to cancel execution at any time

### 4. Understanding the UI

#### File Selection Section
- Choose file type and browse for files
- Displays the selected file path

#### PLC Configuration Section
- Select PLC protocol
- Configure connection parameters
- Connect/Disconnect buttons

#### Execution Control Section
- Start and Stop buttons
- Line counter (current/total)
- Progress percentage

#### Status and Log Section
- Connection status indicator:
  - 🟢 **Green**: PLC Connected
  - ⚫ **Gray**: PLC Disconnected
- Progress bar showing overall progress
- Scrollable log console with timestamped messages

#### Status Bar
- Bottom bar showing current operation status
- Updates in real-time

## Configuration

### Modbus Register Mapping

The HMI uses a simplified register mapping. For production use, you should configure proper mappings in `mappings.json`:

**Default axis mappings** (in code):
- X axis → Register 100
- Y axis → Register 101
- Z axis → Register 102
- A axis → Register 103
- B axis → Register 104
- C axis → Register 105

**Value scaling**: All position values are multiplied by 1000 (converted to microns) before writing.

### OPC UA Node Mapping

**For G-Code**:
- Node format: `ns=2;s=CNC.Axes.{AxisLetter}.Position`
- Example: `ns=2;s=CNC.Axes.X.Position`

**For LSF**:
- Node format: `ns=2;s=LSF.{ParameterName}`
- Example: `ns=2;s=LSF.H1_Length`

## Running the Application

### From Visual Studio
1. Set `G2PLC.UI.Wpf` as the startup project
2. Press F5 or click Start

### From Command Line
```bash
cd src/G2PLC.UI.Wpf
dotnet run
```

### Building an Executable
```bash
dotnet publish src/G2PLC.UI.Wpf/G2PLC.UI.Wpf.csproj -c Release -r win-x64 --self-contained
```

The executable will be in:
```
src/G2PLC.UI.Wpf/bin/Release/net9.0-windows/win-x64/publish/G2PLC.UI.Wpf.exe
```

## Example Workflow

### G-Code Example

1. **Load File**:
   - File Type: `G-Code`
   - Select: `example_complex_5axis_machine.gcode`
   - Parsed: 215 commands

2. **Configure PLC**:
   - PLC Type: `Modbus`
   - IP: `127.0.0.1`
   - Port: `502`

3. **Execute**:
   - Click ▶ Start
   - Watch progress: 215 lines processed
   - Each G01 command writes axis positions to PLC

### LSF (Howick) Example

1. **Load File**:
   - File Type: `LSF (Howick)`
   - Select: `PR-2.csv`
   - Parsed: 8 components

2. **Configure PLC**:
   - PLC Type: `OPC UA`
   - Endpoint: `opc.tcp://192.168.1.200:4840`

3. **Execute**:
   - Click ▶ Start
   - Watch progress: 8 components processed
   - Each component's operations mapped to PLC nodes

## Troubleshooting

### Cannot Connect to PLC

**Modbus**:
- ✓ Check IP address is correct
- ✓ Verify PLC is powered on and network accessible
- ✓ Ensure port 502 is not blocked by firewall
- ✓ Verify no other application is using the PLC

**OPC UA**:
- ✓ Check endpoint URL format: `opc.tcp://host:port`
- ✓ Verify OPC UA server is running
- ✓ Check if security is required (use `UseSecurity` setting)
- ✓ Ensure port 4840 (or custom port) is not blocked

### File Parsing Errors

- ✓ Verify file format matches selected type
- ✓ Check file encoding (should be UTF-8)
- ✓ Ensure file is not corrupted
- ✓ Review log messages for specific error details

### Application Crashes on Start

- ✓ Ensure all dependencies are installed
- ✓ Check .NET 9.0 Desktop Runtime is installed
- ✓ Review console output for error messages
- ✓ Try running from command line for detailed errors

## Code Structure

### MainViewModel

The core business logic resides in `MainViewModel.cs`:

```csharp
public partial class MainViewModel : ObservableObject
{
    // Observable properties for UI binding
    [ObservableProperty] private string _filePath;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private int _progressPercentage;

    // Relay commands for user actions
    [RelayCommand] private void LoadFile();
    [RelayCommand] private async Task ConnectPlcAsync();
    [RelayCommand] private async Task StartExecutionAsync();
    [RelayCommand] private void StopExecution();
}
```

### Value Converters

Custom converters for UI data binding:
- **InverseBoolConverter**: Inverts boolean values (for enabling/disabling buttons)
- **ConnectionStatusColorConverter**: Maps connection status to colors (green/gray)
- **ConnectionStatusTextConverter**: Maps connection status to text
- **ModbusVisibilityConverter**: Shows/hides Modbus settings
- **OpcUaVisibilityConverter**: Shows/hides OPC UA settings

### Dependency Injection

`App.xaml.cs` configures services:

```csharp
private void ConfigureServices(IServiceCollection services)
{
    // Logging
    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    });

    // ViewModels
    services.AddSingleton<MainViewModel>();

    // Views
    services.AddTransient<MainWindow>();
}
```

## Future Enhancements

Potential improvements:
1. **Configuration file** for register/node mappings
2. **Edit mode** to modify mappings in UI
3. **Simulation mode** for testing without PLC
4. **Data visualization** (charts for axis positions)
5. **Export logs** to file
6. **Multiple file execution** (batch processing)
7. **Custom register mapping** editor
8. **3D visualization** of toolpath
9. **Recipe management** (save/load configurations)
10. **Multi-language support**

## License

Part of the G2PLC project. See main README for license information.

## Support

For issues, questions, or contributions, please refer to the main G2PLC repository.
