# G2PLC Mapping Configuration Examples

This directory contains example mapping configuration files for different scenarios and protocols.

## Available Examples

### 1. Simple 3-Axis Example (`mappings_simple_3axis.json`)
**Best for**: Basic CNC machines, 3D printers, simple routers

A minimal configuration for standard 3-axis (X, Y, Z) machines.

**Features:**
- X, Y, Z linear axes
- Feed rate, Spindle speed, Tool number
- Modbus register addresses 0-2 for axes
- ScaleFactor of 1000 (mm to microns conversion)

**Use this when:** You have a basic 3-axis machine and want to get started quickly.

---

### 2. Modbus TCP Example (`mappings_modbus_example.json`)
**Best for**: Advanced CNC machines with Modbus TCP protocol

Complete 6-axis configuration for complex machines using Modbus TCP.

**Features:**
- All 6 axes: X, Y, Z (linear) + A, B, C (rotary)
- Separate register addresses for each parameter
- Feed rate, Spindle speed, Tool number, G/M commands
- Detailed address mapping (registers 100-121)
- Conversion factors optimized for industrial precision

**Use this when:**
- You have a 5-axis or 6-axis CNC machine
- You're using Modbus TCP protocol
- You need precise control over register addressing

---

### 3. OPC UA Example (`mappings_opcua_example.json`)
**Best for**: Modern machines with OPC UA protocol

Complete 6-axis configuration for OPC UA communication.

**Features:**
- All 6 axes with OPC UA node paths
- Industry-standard node naming: `ns=2;s=CNC.Axes.{Axis}.Position`
- OPC UA endpoint configuration
- Security settings (None, Basic256Sha256, etc.)
- Node paths for all parameters

**Key Differences from Modbus:**
- Uses `OpcUaNodePath` instead of register addresses
- Address field is ignored (set to 0)
- Requires endpoint URL configuration
- Supports secure communication

**Use this when:**
- Your machine supports OPC UA
- You need secure industrial communication
- You want industry-standard protocol

---

## Understanding Mapping Files

### Common Fields

All mapping files share these key fields:

```json
{
  "RegisterMappings": {
    "Axes": {
      "X": {
        "Address": 100,           // Modbus register or 0 for OPC UA
        "ScaleFactor": 1000,      // Multiplier for values
        "Description": "X-axis position",
        "AxisType": "Linear",     // Linear or Rotary
        "Unit": "mm",             // Measurement unit
        "OpcUaNodePath": "..."    // Only for OPC UA
      }
    }
  }
}
```

### Field Explanations

| Field | Purpose | Example Values |
|-------|---------|----------------|
| **Address** | Modbus register number (0-based) | 0, 100, 101... |
| **ScaleFactor** | Value multiplier before writing | 1000 (mm→µm), 10, 1 |
| **Description** | Human-readable description | "X-axis position" |
| **AxisType** | Type of axis movement | Linear, Rotary |
| **Unit** | Measurement unit | mm, degrees, rpm |
| **OpcUaNodePath** | OPC UA node identifier | ns=2;s=CNC.Axes.X.Position |

---

## Scale Factor Examples

Scale factors allow you to convert between different units:

| Input | ScaleFactor | Output | Use Case |
|-------|-------------|--------|----------|
| 10.5 mm | 1000 | 10500 | Millimeters to microns |
| 100 mm/min | 10 | 1000 | Feed rate scaling |
| 1500 RPM | 1 | 1500 | No conversion needed |
| 45.5° | 1000 | 45500 | Degrees with precision |

**Formula:** `PLC_Value = Original_Value × ScaleFactor`

---

## Modbus Addressing

### 0-Based vs 1-Based

G2PLC uses **0-based addressing** internally:

| Modbus Register | Address in JSON |
|-----------------|-----------------|
| 40001 | 0 |
| 40002 | 1 |
| 40003 | 2 |
| 40101 | 100 |

**Important:** When configuring addresses in JSON, use the 0-based value (subtract 1 from the Modbus register number if your documentation uses 1-based addressing).

---

## OPC UA Node Paths

### Standard Naming Convention

OPC UA uses hierarchical node paths:

```
ns={namespace};s={identifier}
```

**Examples:**
```
ns=2;s=CNC.Axes.X.Position      ← X-axis position
ns=2;s=CNC.Spindle.Speed        ← Spindle speed
ns=2;s=CNC.Command.GCode        ← G-code command
```

### Namespace Index

The `ns=` part indicates the namespace:
- `ns=0` = OPC UA base namespace (reserved)
- `ns=1` = Server namespace
- `ns=2` = Custom application namespace (most common)

**Note:** Adjust the namespace index based on your OPC UA server configuration.

---

## Creating Your Own Mapping

### Step 1: Start with an Example
Copy the example that best matches your machine:
- 3-axis machine → `mappings_simple_3axis.json`
- Modbus TCP → `mappings_modbus_example.json`
- OPC UA → `mappings_opcua_example.json`

### Step 2: Customize Addresses

**For Modbus:**
Update the `Address` fields to match your PLC register layout:
```json
"X": {
  "Address": 100,  ← Change this to your actual register
  "ScaleFactor": 1000,
  "Description": "X-axis position"
}
```

**For OPC UA:**
Update the `OpcUaNodePath` fields to match your server:
```json
"X": {
  "Address": 0,  ← Leave as 0 for OPC UA
  "OpcUaNodePath": "ns=2;s=CNC.Axes.X.Position",  ← Update this
  "ScaleFactor": 1000
}
```

### Step 3: Adjust Scale Factors

Determine what conversion you need:

1. **No conversion needed:** Set ScaleFactor = 1
2. **mm to microns:** Set ScaleFactor = 1000
3. **inches to mils:** Set ScaleFactor = 1000
4. **Custom scaling:** Calculate required multiplier

### Step 4: Test Your Configuration

1. Load your mapping file in G2PLC (**Configuration → Load Mappings File**)
2. Edit if needed (**Configuration → Edit Mappings**)
3. Connect to your PLC
4. Run a simple test G-code file
5. Verify values are correct on your PLC

---

## Common Issues and Solutions

### Issue: Values Too Large
**Problem:** PLC receives values > 65535 (ushort max)
**Solution:** Reduce ScaleFactor or use separate registers for larger values

### Issue: Wrong Modbus Address
**Problem:** PLC not receiving data
**Solution:**
- Verify you're using 0-based addressing
- Check PLC documentation for correct register numbers
- Use PLC monitoring tools to verify register writes

### Issue: OPC UA Connection Failed
**Problem:** Cannot connect to OPC UA server
**Solution:**
- Verify endpoint URL is correct
- Check security mode matches server configuration
- Ensure server is running and accessible

### Issue: Negative Values
**Problem:** Negative coordinates are clamped to 0
**Solution:** Consider these approaches:
- Use offset-based positioning (add large offset)
- Implement two's complement encoding
- Use separate sign bit register

---

## Advanced Configuration

### Multiple Machine Profiles

Create separate mapping files for different machines:
```
mappings_machine_a.json    ← Your mill
mappings_machine_b.json    ← Your lathe
mappings_machine_c.json    ← Your router
```

Switch between them using **Configuration → Load Mappings File**

### Custom Parameters

You can add additional parameters beyond the default axes:

```json
"CustomParameter1": {
  "Address": 200,
  "ScaleFactor": 1,
  "Description": "Coolant flow rate"
}
```

### OPC UA Security

For secure OPC UA connections, configure in the OpcUaSettings section:

```json
"OpcUaSettings": {
  "EndpointUrl": "opc.tcp://192.168.1.100:4840",
  "SecurityMode": "SignAndEncrypt",
  "SecurityPolicy": "Basic256Sha256",
  "ApplicationName": "G2PLC Client",
  "SessionTimeout": 60000
}
```

---

## Accessing Examples in G2PLC

### From the Application

1. Launch G2PLC WPF application
2. Navigate to **Documents** menu
3. Select:
   - **Open Examples Folder** - Opens file explorer to examples directory
   - **Simple 3-Axis Example** - Opens the 3-axis example file
   - **Modbus Example** - Opens the Modbus TCP example
   - **OPC UA Example** - Opens the OPC UA example

### From File Explorer

Examples are located at:
```
<G2PLC Installation>\docs\examples\
```

Or in the source repository:
```
G2PLC\docs\examples\
```

---

## Additional Resources

- **LOCALIZATION.md** - Language support and translation guide
- **THEME_SYSTEM.md** - UI theming documentation
- **MAPPINGS_GUIDE.md** - Detailed mapping configuration guide
- **README.md** - Main project documentation

---

**Need help?** Check the inline comments in each example file for more details!
