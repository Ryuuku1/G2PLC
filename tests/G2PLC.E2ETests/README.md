# G2PLC End-to-End Tests

## Overview

The E2E (End-to-End) test suite provides comprehensive testing of the complete G2PLC pipeline from file parsing through PLC communication. These tests verify that G-Code and LSF files are correctly parsed, mapped, and written to PLC registers or OPC UA nodes.

## Features

### ✅ Dual Mode Testing
- **Mock Mode** (default): Uses in-memory mock PLC servers for fast, reliable testing
- **Real PLC Mode**: Test against actual PLCs to verify hardware integration

### ✅ Complete Coverage
- **G-Code to Modbus**: Parse G-Code and write to Modbus holding registers
- **G-Code to OPC UA**: Parse G-Code and write to OPC UA nodes
- **LSF to Modbus**: Parse LSF (Howick) and write component data
- **LSF to OPC UA**: Parse LSF and write to structured nodes

### ✅ Verification
- Write-read-back verification
- Register/node value assertions
- Mapping correctness validation
- Custom mapping scenarios

## Test Structure

```
G2PLC.E2ETests/
├── Configuration/
│   └── E2ETestConfiguration.cs    # Test configuration
├── Mocks/
│   ├── MockModbusServer.cs        # In-memory Modbus TCP server
│   ├── MockOpcUaServer.cs         # In-memory OPC UA server
│   └── MockOpcUaClient.cs         # Mock OPC UA client
├── E2ETestBase.cs                 # Base class with common utilities
├── GCodeModbusE2ETests.cs         # G-Code → Modbus tests
├── GCodeOpcUaE2ETests.cs          # G-Code → OPC UA tests
└── LsfModbusE2ETests.cs           # LSF → Modbus tests
```

## Running the Tests

### Using Mock PLC (Default)

No configuration needed. Tests run automatically with mock servers:

```bash
# Run all E2E tests
dotnet test tests/G2PLC.E2ETests

# Run specific test class
dotnet test tests/G2PLC.E2ETests --filter "FullyQualifiedName~GCodeModbusE2ETests"

# Run specific test
dotnet test tests/G2PLC.E2ETests --filter "Name=GCodeToModbus_SimpleLinearMove_WritesCorrectRegisters"
```

**Advantages of Mock Mode:**
- ✅ Fast execution (no network latency)
- ✅ No hardware required
- ✅ Consistent, repeatable results
- ✅ Can verify exact register/node values
- ✅ Safe to run in CI/CD pipelines

### Using Real PLC

To test against a real PLC, update the configuration:

**Option 1: Modify E2ETestBase.cs**

```csharp
protected E2ETestBase()
{
    // Change this line:
    Configuration = E2ETestConfiguration.CreateRealPlcConfiguration();

    // Update connection details:
    Configuration.Modbus.IpAddress = "192.168.1.100"; // Your PLC IP
    Configuration.Modbus.Port = 502;

    Configuration.OpcUa.EndpointUrl = "opc.tcp://192.168.1.100:4840";
}
```

**Option 2: Override in Test Class**

```csharp
public class MyRealPlcTests : E2ETestBase
{
    public MyRealPlcTests()
    {
        Configuration.UseMockPlc = false;
        Configuration.Modbus.IpAddress = "10.0.0.50";
    }

    // Your tests...
}
```

**Requirements for Real PLC Testing:**
- ⚠️ PLC must be accessible on network
- ⚠️ Correct IP address and port
- ⚠️ PLC must support Modbus TCP or OPC UA
- ⚠️ Ensure you have permission to write to PLC
- ⚠️ Tests may be slower due to network communication

## Test Scenarios

### G-Code to Modbus Tests

**GCodeModbusE2ETests.cs**

| Test | Description |
|------|-------------|
| `GCodeToModbus_SimpleLinearMove_WritesCorrectRegisters` | Single G01 command with XYZ axes |
| `GCodeToModbus_MultipleCommands_WritesAllPositions` | Sequential commands, final values verified |
| `GCodeToModbus_ComplexProgram_MapsAllAxes` | 6-axis (XYZABC) program with arc moves |
| `GCodeToModbus_VerifyWriteReadback_ConfirmsCorrectValue` | Write and read-back verification |
| `GCodeToModbus_CustomRegisterMapping_WritesToCorrectAddresses` | Custom address mapping |

**Example Test:**
```csharp
[Fact]
public async Task GCodeToModbus_SimpleLinearMove_WritesCorrectRegisters()
{
    // Arrange
    var gcode = "G01 X100.5 Y200.25 Z50.0 F1500";
    var commands = await GCodeParser.ParseTextAsync(gcode);

    using var modbusClient = CreateModbusClient();
    await modbusClient.ConnectAsync();

    ResetPlcState();

    // Act - Write positions to PLC
    var command = commands.First();
    foreach (var param in command.Parameters)
    {
        ushort address = GetRegisterAddress(param.Key);
        ushort value = (ushort)(param.Value * 1000); // Scale to microns
        await modbusClient.WriteHoldingRegisterAsync(address, value);
    }

    // Assert - Verify registers
    var registers = GetModbusRegisters();

    registers[(ushort)100].ShouldBe((ushort)100500); // X axis
    registers[(ushort)101].ShouldBe((ushort)65535);  // Y axis (clamped)
    registers[(ushort)102].ShouldBe((ushort)50000);  // Z axis
}
```

### G-Code to OPC UA Tests

**GCodeOpcUaE2ETests.cs**

| Test | Description |
|------|-------------|
| `GCodeToOpcUa_SimpleLinearMove_WritesCorrectNodes` | Write XYZ positions to nodes |
| `GCodeToOpcUa_MultipleCommands_WritesAllPositions` | Sequential updates to nodes |
| `GCodeToOpcUa_BatchWrite_WritesMultipleNodesEfficiently` | Batch write performance |
| `GCodeToOpcUa_VerifyWriteReadback_ConfirmsCorrectValue` | Node read-back verification |
| `GCodeToOpcUa_CustomNodeMapping_WritesToCorrectPaths` | Custom node structure |

**Example Test:**
```csharp
[Fact]
public async Task GCodeToOpcUa_BatchWrite_WritesMultipleNodesEfficiently()
{
    // Arrange
    var gcode = "G01 X100 Y200 Z300 A45 B90 C180";
    var commands = await GCodeParser.ParseTextAsync(gcode);

    using var opcClient = CreateOpcUaClient();
    await opcClient.ConnectAsync();

    // Act - Batch write
    var command = commands.First();
    var nodeValues = new Dictionary<string, object>();

    foreach (var param in command.Parameters)
    {
        string nodeId = $"ns=2;s=CNC.Axes.{param.Key}.Position";
        nodeValues[nodeId] = param.Value * 1000;
    }

    await opcClient.WriteMultipleNodeValuesAsync(nodeValues);

    // Assert - All axes written
    var nodes = GetOpcUaNodes();
    nodes.Count.ShouldBe(6); // All 6 axes
}
```

### LSF to Modbus Tests

**LsfModbusE2ETests.cs**

| Test | Description |
|------|-------------|
| `LsfToModbus_ParseAndMapFrameset_WritesAllComponents` | Complete frameset processing |
| `LsfToModbus_SingleComponent_MapsCorrectly` | Single component metadata |
| `LsfToModbus_VerifyOperations_CorrectTypesAndPositions` | Operation type and position mapping |
| `LsfToModbus_FramesetHeader_WritesMetadata` | Frameset-level metadata |
| `LsfToModbus_ComponentSequence_ProcessesInOrder` | Sequential component processing |

**Example Test:**
```csharp
[Fact]
public async Task LsfToModbus_SingleComponent_MapsCorrectly()
{
    // Arrange
    var frameset = await HowickParser.ParseFileAsync("PR-2.csv");
    var h1Component = frameset.Components.First(c => c.ComponentId == "H1");

    using var modbusClient = CreateModbusClient();
    await modbusClient.ConnectAsync();

    // Act - Map component to registers
    var mappings = LsfMapper.MapComponentToRegisters(h1Component);

    foreach (var mapping in mappings)
    {
        await modbusClient.WriteHoldingRegisterAsync(mapping.Address, mapping.Value);
    }

    // Assert - Verify component data
    var registers = GetModbusRegisters();

    // Component ID (first character)
    registers[addressForId].ShouldBe((ushort)'H');

    // Operation count
    registers[addressForOpCount].ShouldBe((ushort)9); // H1 has 9 operations
}
```

## Register Mapping

### Default G-Code Register Addresses

| Axis | Register Address | Description |
|------|-----------------|-------------|
| X | 100 | X-axis position (microns) |
| Y | 101 | Y-axis position (microns) |
| Z | 102 | Z-axis position (microns) |
| A | 103 | A-axis rotation (millidegrees) |
| B | 104 | B-axis rotation (millidegrees) |
| C | 105 | C-axis rotation (millidegrees) |
| F | 103 | Feedrate (mm/min * 1000) |
| S | 104 | Spindle speed (RPM * 1000) |

**Scaling:**
- Linear axes: Value in mm × 1000 = microns
- Rotary axes: Value in degrees × 1000 = millidegrees
- Values clamped to `ushort` range (0-65535)

### OPC UA Node Structure

**G-Code Nodes:**
```
ns=2;s=CNC.Axes.X.Position
ns=2;s=CNC.Axes.Y.Position
ns=2;s=CNC.Axes.Z.Position
ns=2;s=CNC.Axes.A.Position
ns=2;s=CNC.Axes.B.Position
ns=2;s=CNC.Axes.C.Position
```

**LSF Nodes:**
```
ns=2;s=LSF.FramesetId
ns=2;s=LSF.ComponentCount
ns=2;s=LSF.{ComponentId}_ID
ns=2;s=LSF.{ComponentId}_Length
ns=2;s=LSF.{ComponentId}_OpCount
ns=2;s=LSF.{ComponentId}_Op{N}_Type
ns=2;s=LSF.{ComponentId}_Op{N}_Pos
```

## Verification Utilities

### GetModbusRegisters()

Returns all non-zero registers from mock or reads from real PLC:

```csharp
var registers = GetModbusRegisters();

// Assert specific values
registers[(ushort)100].ShouldBe(expectedValue);

// Assert register exists
registers.ShouldContainKey((ushort)101);

// Check count
registers.Count.ShouldBeGreaterThan(0);
```

### GetOpcUaNodes()

Returns all non-default nodes from mock or reads from real server:

```csharp
var nodes = GetOpcUaNodes();

// Assert specific values
Convert.ToDouble(nodes["ns=2;s=CNC.Axes.X.Position"]).ShouldBe(100000.0);

// Assert node exists
nodes.ShouldContainKey("ns=2;s=CNC.Axes.Y.Position");
```

### ResetPlcState()

Clears all registers/nodes (mock only):

```csharp
ResetPlcState(); // Reset before each test
```

## Writing Custom Tests

### Example: Custom Mapping Test

```csharp
[Fact]
public async Task CustomMapping_MySpecificScenario()
{
    // Arrange - Define your mapping
    var customMapping = new Dictionary<char, ushort>
    {
        ['X'] = 2000, // Custom X register
        ['Y'] = 2001, // Custom Y register
        ['Z'] = 2002  // Custom Z register
    };

    var gcode = "G01 X10 Y20 Z30";
    var commands = await GCodeParser.ParseTextAsync(gcode);

    using var modbusClient = CreateModbusClient();
    await modbusClient.ConnectAsync();

    ResetPlcState();

    // Act - Write using custom mapping
    var command = commands.First();
    foreach (var param in command.Parameters)
    {
        if (customMapping.ContainsKey(param.Key))
        {
            ushort address = customMapping[param.Key];
            ushort value = (ushort)(param.Value * 1000);
            await modbusClient.WriteHoldingRegisterAsync(address, value);
        }
    }

    // Assert - Verify custom addresses
    var registers = GetModbusRegisters();

    registers[(ushort)2000].ShouldBe((ushort)10000); // X
    registers[(ushort)2001].ShouldBe((ushort)20000); // Y
    registers[(ushort)2002].ShouldBe((ushort)30000); // Z
}
```

### Example: Real PLC Integration Test

```csharp
public class RealPlcIntegrationTests : E2ETestBase
{
    public RealPlcIntegrationTests()
    {
        // Configure for your actual PLC
        Configuration.UseMockPlc = false;
        Configuration.Modbus.IpAddress = "10.0.0.100";
        Configuration.Modbus.Port = 502;
    }

    [Fact]
    public async Task RealPlc_WriteAndVerify()
    {
        // Arrange
        var gcode = "G01 X50 Y50 Z10";
        var commands = await GCodeParser.ParseTextAsync(gcode);

        using var modbusClient = CreateModbusClient();
        await modbusClient.ConnectAsync();

        // Act & Assert
        var command = commands.First();
        foreach (var param in command.Parameters)
        {
            ushort address = GetRegisterAddress(param.Key);
            ushort value = (ushort)(param.Value * 1000);

            // Write
            await modbusClient.WriteHoldingRegisterAsync(address, value);

            // Verify immediately
            var readback = await modbusClient.ReadHoldingRegisterAsync(address);
            readback.ShouldBe(value);
        }
    }
}
```

## Troubleshooting

### Mock Tests Failing

**Issue**: Mock server not starting
```
Solution: Check port 5502 is not in use. Change port in configuration if needed.
```

**Issue**: Registers not found
```
Solution: Call ResetPlcState() at start of test to clear previous values.
```

### Real PLC Tests Failing

**Issue**: Connection timeout
```
Solution:
- Verify PLC IP address is correct
- Check network connectivity (ping the PLC)
- Ensure firewall allows Modbus port 502
- Increase timeout in configuration
```

**Issue**: Write failures
```
Solution:
- Verify PLC is not write-protected
- Check register address range is valid for your PLC
- Ensure PLC is in correct operating mode
```

**Issue**: OPC UA connection errors
```
Solution:
- Verify endpoint URL format: opc.tcp://host:port
- Check OPC UA server is running
- Try UseSecurity = false first
- Check certificate trust issues
```

## Best Practices

### 1. Always Reset State
```csharp
[Fact]
public async Task MyTest()
{
    ResetPlcState(); // Start clean
    // ... rest of test
}
```

### 2. Use Descriptive Assertions
```csharp
// ✅ Good
registers[(ushort)100].ShouldBe((ushort)50000, "X axis position should be 50mm in microns");

// ❌ Less clear
Assert.Equal(50000, registers[(ushort)100]);
```

### 3. Test One Thing at a Time
```csharp
// ✅ Good - Tests single axis
[Fact]
public async Task WriteXAxis_VerifiesCorrectly() { }

// ✅ Good - Tests multi-axis
[Fact]
public async Task WriteMultipleAxes_AllVerify() { }

// ❌ Too broad
[Fact]
public async Task TestEverything() { }
```

### 4. Use Realistic G-Code
```csharp
// ✅ Good - Real-world command
var gcode = "G01 X100.5 Y200.25 Z50.0 F1500";

// ❌ Less realistic
var gcode = "G01 X1 Y1 Z1";
```

## CI/CD Integration

E2E tests run automatically in CI/CD pipelines using mock mode:

```yaml
# .github/workflows/test.yml
- name: Run E2E Tests
  run: dotnet test tests/G2PLC.E2ETests --logger "console;verbosity=detailed"
```

For real PLC testing in CI/CD:
- Use dedicated test PLC
- Store configuration in secrets
- Run on self-hosted runner with PLC access
- Schedule during off-hours

## Summary

The E2E test suite provides:
- ✅ Fast, reliable mock-based testing
- ✅ Real PLC verification capability
- ✅ Complete pipeline coverage (parse → map → write)
- ✅ Easy customization for specific scenarios
- ✅ Clear verification of correct register/node values
- ✅ Support for both Modbus and OPC UA
- ✅ G-Code and LSF file format support

Run `dotnet test tests/G2PLC.E2ETests` to verify your setup!
