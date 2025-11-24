; G-code example for 5-axis CNC machine with complex I/O control
; This demonstrates the enhanced multi-axis support
;
; Machine configuration:
; - Linear axes: X, Y, Z (primary cartesian)
; - Rotational axes: A (around X), C (table rotation)
; - Digital I/O: Coolant, spindle control
;
; Operation: 3D surface machining with table rotation

(Program: 5-Axis Complex Part)
(Tool: 6mm Ball End Mill)
(Material: Aluminum 7075)

; Initialization
G21 (Metric mode)
G90 (Absolute positioning)
G54 (Work coordinate system 1)

; Tool change
M06 T01 (Change to tool 1)
G43 H01 (Tool length compensation)

; Spindle and coolant start
M03 S12000 (Spindle CW at 12000 RPM)
M08 (Flood coolant ON)

; Move to start position with all axes
G00 X50.0 Y100.0 Z150.0 (Rapid to safe Z)
G00 A0.0 C0.0 (Position rotational axes)

; First surface - standard 3-axis
G01 Z10.0 F500 (Approach workpiece)
G01 X100.0 Y150.0 F1200 (Linear move)
G01 X150.0 Y200.0 Z5.0 (Simultaneous 3-axis move)

; Rotate table for second surface
G00 Z150.0 (Retract)
G00 C90.0 (Rotate table 90 degrees)
G01 Z10.0 F500

; 4-axis contouring (X, Y, Z, C simultaneous)
G01 X120.0 Y180.0 Z8.0 C95.0 F800

; 5-axis simultaneous move
G01 X130.0 Y190.0 Z7.0 A15.0 C100.0 F600 (All 5 axes moving)
G01 X140.0 Y200.0 Z6.0 A20.0 C105.0

; Complex toolpath with tilted head
G01 A25.0 F300 (Tilt cutting head)
G01 X160.0 Y210.0 Z8.0 C110.0 F800 (4-axis simultaneous)

; Switch to mist coolant for finishing
M09 (Turn off flood coolant)
M07 (Mist coolant ON)

; Finishing pass
G01 X170.0 Y220.0 Z10.0 A30.0 C120.0 F1500

; Spindle direction change for tapping
M05 (Spindle stop)
M04 S1000 (Spindle CCW at 1000 RPM for tap)

; Return to neutral
G00 Z150.0
G00 A0.0 C0.0

; Program end
M09 (Coolant OFF)
M05 (Spindle OFF)
G00 X0 Y0 Z200.0 (Move to home)
M30 (End program)
