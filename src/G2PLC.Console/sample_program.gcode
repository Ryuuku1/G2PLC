(G-code Sample Program for CNC Machining)
(Program: SAMPLE-001)
(Date: 2025-11-03)
(Description: Demonstrates various G-code commands for PLC testing)

(Initialize: Set units and positioning mode)
G21 (Set units to millimeters)
G90 (Absolute positioning mode)
G17 (Select XY plane)

(Home the machine)
G28 X0 Y0 Z0 (Return to reference point)

(Tool change: Select Tool 1)
T01
M03 S1200 (Start spindle clockwise at 1200 RPM)

(Wait for spindle to reach speed - typically handled by PLC)

(Rapid move to starting position)
G00 X10.0 Y10.0 Z5.0

(Linear move with feed rate)
G01 Z-2.0 F100 (Plunge to depth at 100 mm/min)

(Simple rectangular profile)
G01 X50.0 Y10.0 F200 (Move to corner 1)
G01 X50.0 Y30.0 F200 (Move to corner 2)
G01 X10.0 Y30.0 F200 (Move to corner 3)
G01 X10.0 Y10.0 F200 (Move to corner 4 - complete rectangle)

(Retract)
G01 Z5.0 F150

(Demonstrate arc movements)
G00 X40.0 Y25.0 (Position for circle)
G01 Z-1.5 F100 (Plunge to depth)

(Circular interpolation - full circle)
G02 X40.0 Y25.0 I5.0 J0 F180 (Arc clockwise, radius 5mm)

(Retract)
G01 Z5.0 F150

(Tool change: Select Tool 2)
G00 Z20.0 (Retract for tool change)
M05 (Stop spindle)
T02
M03 S2000 (Start spindle at 2000 RPM for new tool)

(Move to new position for drilling)
G00 X15.0 Y15.0
G01 Z-3.0 F80 (Drill hole 1)
G00 Z5.0 (Retract)

G00 X25.0 Y15.0
G01 Z-3.0 F80 (Drill hole 2)
G00 Z5.0

G00 X35.0 Y15.0
G01 Z-3.0 F80 (Drill hole 3)
G00 Z5.0

(Return to safe position)
G00 Z20.0
G00 X0 Y0

(Stop spindle)
M05

(Program end)
M30 (End of program and reset)
