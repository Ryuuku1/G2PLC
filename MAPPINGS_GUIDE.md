# G2PLC Variable Mappings Guide

## Overview

This guide explains how to configure variable mappings between G-Code/LSF parameters and PLC registers (Modbus) or nodes (OPC UA).

## Where to Configure Mappings

### Option 1: Using the WPF UI (Recommended)

1. Launch the **G2PLC WPF Application** ([G2PLC.UI.Wpf.exe](src/G2PLC.UI.Wpf/bin/Debug/net9.0-windows/G2PLC.UI.Wpf.exe))
2. Go to **Configuration → Edit Mappings** menu
3. Modify the axis mappings directly in the visual editor
4. Click **Save** to apply changes

### Option 2: Using mappings.json File

Create or edit a `mappings.json` file in the application directory. The WPF application can load/save this file via:
- **Configuration → Load Mappings File**
- **Configuration → Save Mappings File**

## Mapping Configuration Structure

### Axis Mappings

Maps G-Code axis parameters (X, Y, Z, A, B, C) to PLC registers or OPC UA nodes:

```json
"Axes": {
  "X": {
    "Address": 100,           // Modbus register number
    "ScaleFactor": 1000.0,    // Converts mm to microns (multiply by 1000)
    "Description": "X-axis linear position",
    "AxisType": "Linear",     // Linear or Rotational
    "Unit": "mm"              // Display unit
  }
}
```

**For Modbus:**
- `Address` = Holding register number where the value will be written
- Value written = (G-Code value × ScaleFactor)

**For OPC UA:**
- Node path format: `ns=2;s=CNC.Axes.{AxisName}.Position`
- Example: `ns=2;s=CNC.Axes.X.Position`

### Current Default Mapping

| Axis | Register Address | Scale Factor | Description |
|------|-----------------|--------------|-------------|
| X    | 100             | 1000         | X-axis (linear) |
| Y    | 101             | 1000         | Y-axis (linear) |
| Z    | 102             | 1000         | Z-axis (linear) |
| A    | 103             | 1000         | A-axis (rotational) |
| B    | 104             | 1000         | B-axis (rotational) |
| C    | 105             | 1000         | C-axis (rotational) |
| F    | 110             | 10           | Feed rate |
| S    | 111             | 1            | Spindle speed |

### Example G-Code Mapping

**G-Code Input:**
```gcode
G01 X100.5 Y200.25 Z50.0 F1500
```

**Modbus Writes:**
- Register 100 = 100500 (X: 100.5 × 1000)
- Register 101 = 200250 (Y: 200.25 × 1000)
- Register 102 = 50000 (Z: 50.0 × 1000)
- Register 110 = 15000 (F: 1500 × 10)

**OPC UA Writes:**
- `ns=2;s=CNC.Axes.X.Position` = 100500
- `ns=2;s=CNC.Axes.Y.Position` = 200250
- `ns=2;s=CNC.Axes.Z.Position` = 50000

## Additional Configuration

### Commands Mapping

Maps G/M commands to registers:

```json
"Commands": {
  "GCommand": {
    "Address": 6,
    "ScaleFactor": 1.0,
    "Description": "G command number"
  }
}
```

### Parameters Mapping

Maps special parameters (F, S, T):

```json
"Parameters": {
  "FeedRate": {
    "Address": 110,
    "ScaleFactor": 10.0,
    "Description": "Feed rate"
  }
}
```

### Digital Outputs

Maps M-codes to digital I/O:

```json
"DigitalOutputs": {
  "CoolantFlood": {
    "Address": 100,
    "TriggerMCode": 8,      // M08 triggers this output
    "TriggerValue": true,   // Sets coil to ON
    "Description": "Flood coolant"
  }
}
```

### Validation Rules

Configure min/max value limits:

```json
"ValidationRules": {
  "Position": {
    "MinValue": -9999.0,
    "MaxValue": 9999.0,
    "ClampNegativeToZero": false
  }
}
```

### Processing Options

Configure execution behavior:

```json
"ProcessingOptions": {
  "DelayBetweenCommandsMs": 100,  // Delay between writes
  "VerifyWritesEnabled": false,   // Read-back verification
  "ContinueOnError": true,        // Continue if write fails
  "MaxRetries": 3                 // Retry attempts
}
```

## Quick Start Examples

### Example 1: Change X-axis Register

To write X-axis to register 200 instead of 100:

**Via UI:**
1. Configuration → Edit Mappings
2. Find X axis row
3. Change Register Address to 200
4. Click Save

**Via JSON:**
```json
"X": {
  "Address": 200,
  "ScaleFactor": 1000.0,
  ...
}
```

### Example 2: Change Scale Factor

To send values in millimeters without scaling (instead of microns):

**Via UI:**
1. Configuration → Edit Mappings
2. Change Scale Factor from 1000 to 1
3. Click Save

**Via JSON:**
```json
"ScaleFactor": 1.0
```

### Example 3: Custom OPC UA Node Path

The node path format is hardcoded as: `ns=2;s=CNC.Axes.{Axis}.Position`

To use a different structure, you would need to modify `MainViewModel.cs` line 308:

```csharp
// Current:
string nodeId = $"ns=2;s=CNC.Axes.{param.Key}.Position";

// Custom example:
string nodeId = $"ns=3;s=Machine.{param.Key}.CurrentPos";
```

## Testing Your Mappings

1. **Load a G-Code file** with known axis values
2. **Connect to PLC** (Modbus or OPC UA)
3. **Run the program**
4. **Verify values** in your PLC:
   - For Modbus: Check holding registers
   - For OPC UA: Read the node values

## Troubleshooting

### Value seems wrong
- Check the **ScaleFactor** - common mistake is wrong multiplier
- Verify the **register address** matches your PLC configuration
- For ushort overflow (values > 65535), wrapping will occur

### Cannot edit mappings in UI
- Make sure you have write permissions to the application directory
- Check if another process has locked the mappings.json file

### PLC not receiving data
- Verify **register addresses** match your PLC configuration
- Check **PLC connection** is established (green status)
- Enable verbose logging to see write operations

## Advanced: Creating Custom Mappings

For specialized machines with custom axes or parameters:

1. Save current mappings: **Configuration → Save Mappings File**
2. Edit the JSON file in a text editor
3. Add new axes following the structure above
4. Load the modified file: **Configuration → Load Mappings File**
5. Test with sample G-Code

## Support

For questions or issues with mappings, check:
- E2E tests in `tests/G2PLC.E2ETests/` for working examples
- Console application in `src/G2PLC.Console/Program.cs` for default configuration
- Domain models in `src/G2PLC.Domain/Configuration/` for configuration schema
