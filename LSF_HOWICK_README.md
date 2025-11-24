# Howick LSF (Light Steel Framing) Support

## Overview

This document describes the LSF support added to G2PLC for the **Howick V1 roll-forming machine**. The system can now parse Howick CSV format files and map LSF manufacturing operations to PLC Modbus registers.

## LSF vs G-code

The LSF system is fundamentally different from traditional CNC G-code:

| Aspect | G-code (CNC) | LSF (Howick) |
|--------|--------------|--------------|
| **Format** | Plain text commands (G01, M03, etc.) | CSV with component definitions |
| **Operations** | Tool movements (X/Y/Z positions) | Forming operations (SWAGE, LIP_CUT, NOTCH, etc.) |
| **Coordinate System** | 3D Cartesian (X, Y, Z, + rotational) | 1D linear positions along component length |
| **Machine Type** | CNC mills, lathes, routers | Roll-forming machines for steel framing |
| **Purpose** | Subtractive manufacturing | Forming/bending steel profiles |

## Supported LSF Operations

The Howick V1 machine supports these operations:

1. **SWAGE** - Creates an indentation in the profile for strength/connection
2. **LIP_CUT** - Cuts the lip/flange of the profile
3. **NOTCH** - Creates a notch in the profile
4. **DIMPLE** - Creates a dimple for screw/fastening points
5. **END_TRUSS** - Cuts the end for truss connections

## File Format (Howick CSV)

### Example: PR-2.csv

```csv
UNIT,MILLIMETRE
PROFILE,41.30X100.00,Standard Profile
FRAMESET,1,Alfa,Chicago,,
COMPONENT,H1,LABEL_NRM,1,997.6,SWAGE,19.5,SWAGE,978.1,LIP_CUT,99.1,DIMPLE,19.5
COMPONENT,D1,LABEL_INV,1,617.3,SWAGE,20.6,END_TRUSS,0.0,DIMPLE,15.0
```

### Record Types

#### UNIT
- **Format**: `UNIT,<unit_name>`
- **Example**: `UNIT,MILLIMETRE`
- Defines the measurement unit for all dimensions

#### PROFILE
- **Format**: `PROFILE,<dimensions>,<description>`
- **Example**: `PROFILE,41.30X100.00,Standard Profile`
- Defines the steel profile being used

#### FRAMESET
- **Format**: `FRAMESET,<id>,<name>,<location>,,`
- **Example**: `FRAMESET,1,Alfa,Chicago,,`
- Defines the panel/frameset metadata

#### COMPONENT
- **Format**: `COMPONENT,<id>,<orientation>,<qty>,<length>,<op_type>,<position>,...`
- **Example**: `COMPONENT,H1,LABEL_NRM,1,997.6,SWAGE,19.5,DIMPLE,89.5`
- **Fields**:
  - `id`: Component identifier (H1, V2, D1, etc.)
  - `orientation`: LABEL_NRM (normal) or LABEL_INV (inverted)
  - `qty`: Quantity to manufacture
  - `length`: Total component length in mm
  - Operation pairs: `<operation_type>,<position_mm>`

## Usage

### 1. Parsing Howick CSV Files

```csharp
using G2PLC.Application.Parsers;
using Microsoft.Extensions.Logging;

var logger = loggerFactory.CreateLogger<HowickCsvParser>();
var parser = new HowickCsvParser(logger);

// Parse the CSV file
var frameset = await parser.ParseFileAsync("PR-2.csv");

// Access frameset data
Console.WriteLine($"Frameset: {frameset.FramesetName} @ {frameset.Location}");
Console.WriteLine($"Profile: {frameset.Profile}");
Console.WriteLine($"Components: {frameset.Components.Count}");

// Access individual components
foreach (var component in frameset.Components)
{
    Console.WriteLine($"{component.ComponentId}: {component.Length}mm, {component.Operations.Count} operations");
}
```

### 2. Mapping to PLC Registers

```csharp
using G2PLC.Application.Mappers;

var configuration = new MappingConfiguration
{
    RegisterMappings = new RegisterMappings(),
    ValidationRules = new ValidationRules(),
    ProcessingOptions = new ProcessingOptions()
};

var mapper = new LsfDataMapper(mapperLogger, configuration);

// Map entire frameset
var allMappings = mapper.MapFramesetToRegisters(frameset);

// Or map individual component
var h1 = frameset.Components.First(c => c.ComponentId == "H1");
var h1Mappings = mapper.MapComponentToRegisters(h1);

// Write to PLC
foreach (var mapping in allMappings)
{
    await modbusClient.WriteHoldingRegisterAsync(mapping.Address, mapping.Value);
}
```

## PLC Register Mapping

### Frameset Header (Registers 100-101)
- **100**: Frameset ID
- **101**: Component Count

### Component Metadata (Registers 200-204)
For each component:
- **200**: Component ID (ASCII code of first character)
- **201**: Component Length (in microns, scaled ×1000)
- **202**: Quantity
- **203**: Orientation (0=Normal, 1=Inverted)
- **204**: Operation Count

### Operations (Starting at Register 210)
Each operation uses 2 registers:
- **Even addresses (210, 212, 214, ...)**: Operation Type Code
  - 1 = SWAGE
  - 2 = LIP_CUT
  - 3 = NOTCH
  - 4 = DIMPLE
  - 5 = END_TRUSS
- **Odd addresses (211, 213, 215, ...)**: Position (in microns, scaled ×1000)

### Trigger Register (209)
- **209**: Component Ready Trigger (set to 1 when component data is loaded)

## Domain Models

### LsfFrameset
```csharp
public class LsfFrameset
{
    public string Unit { get; set; }  // e.g., "MILLIMETRE"
    public string Profile { get; set; }  // e.g., "41.30X100.00"
    public int FramesetId { get; set; }
    public string FramesetName { get; set; }
    public string Location { get; set; }
    public List<LsfComponent> Components { get; set; }
}
```

### LsfComponent
```csharp
public class LsfComponent
{
    public string ComponentId { get; set; }  // e.g., "H1", "V2", "D1"
    public LabelOrientation LabelOrientation { get; set; }  // Normal or Inverted
    public int Quantity { get; set; }
    public decimal Length { get; set; }  // in mm
    public List<LsfOperation> Operations { get; set; }
}
```

### LsfOperation
```csharp
public class LsfOperation
{
    public LsfOperationType OperationType { get; set; }
    public decimal Position { get; set; }  // in mm
}
```

## Test Results

All **60 tests pass** including 9 comprehensive integration tests for LSF:

✅ `ParsePR2File_ShouldLoadFramesetCorrectly` - Validates frameset parsing
✅ `ParsePR2File_ShouldParseH1ComponentCorrectly` - Validates component parsing
✅ `ParsePR2File_ShouldParseH1OperationsCorrectly` - Validates operation parsing
✅ `ParsePR2File_ShouldParseD1DiagonalWithEndTrussCorrectly` - Validates diagonal components
✅ `MapH1Component_ShouldGenerateCorrectRegisterMappings` - Validates metadata mapping
✅ `MapH1Component_ShouldMapOperationsCorrectly` - Validates operation mapping
✅ `MapEntireFrameset_ShouldGenerateCompleteRegisterSet` - Validates full frameset mapping
✅ `MapFrameset_ShouldOrderRegistersByAddress` - Validates register ordering
✅ `ParseAndMap_EndToEnd_ShouldProduceValidPLCData` - End-to-end validation

## Example: PR-2 Panel

The **PR-2.csv** file defines a rectangular steel frame with:
- **Dimensions**: 1000mm × 1000mm
- **Profile**: 41.30mm × 100.00mm standard steel
- **Components**:
  - **H1, H2, H3**: Horizontal members (997.6mm)
  - **V1, V2, V3**: Vertical members (1000.0mm)
  - **D1, D2**: Diagonal braces (617.3mm)

### Component H1 (Horizontal Top Rail)
- Length: 997.6mm
- Orientation: Normal
- Operations:
  - SWAGE @ 19.5mm
  - SWAGE @ 978.1mm
  - LIP_CUT @ 99.1mm, 109.1mm, 498.8mm
  - DIMPLE @ 19.5mm, 89.5mm, 498.8mm, 978.1mm

### Component D1 (Diagonal Brace)
- Length: 617.3mm
- Orientation: Inverted
- Operations:
  - SWAGE @ 20.6mm, 34.3mm, 583.0mm, 596.7mm
  - END_TRUSS @ 0.0mm, 617.3mm (both ends)
  - DIMPLE @ 15.0mm, 602.3mm

## Key Features

1. **Fully Generic** - No hardcoded component names or operation limits
2. **Type-Safe** - Strong typing with enums for operations and orientations
3. **Scalable** - Handles any number of components and operations
4. **Well-Tested** - Comprehensive test coverage with real-world data
5. **Logged** - Detailed logging at all stages
6. **Clean Architecture** - Separation of concerns (Domain/Application/Infrastructure)

## Files Created

### Domain Layer
- `LsfOperationType.cs` - Enum for operation types
- `LabelOrientation.cs` - Enum for component orientation
- `LsfOperation.cs` - Operation model
- `LsfComponent.cs` - Component model
- `LsfFrameset.cs` - Frameset model
- `LsfOperationMappingConfig.cs` - Configuration model

### Application Layer
- `HowickCsvParser.cs` - CSV parser
- `LsfDataMapper.cs` - PLC register mapper

### Tests
- `HowickLsfIntegrationTests.cs` - 9 comprehensive integration tests

### Data Files
- `PR-2.csv` - Example LSF panel definition

## Future Enhancements

Potential improvements:
1. Support for multi-register values (32-bit for longer components)
2. Batch processing of multiple framesets
3. Real-time PLC communication with operation sequencing
4. Validation against machine capabilities
5. Operation optimization/reordering
6. Support for additional Howick machine models
