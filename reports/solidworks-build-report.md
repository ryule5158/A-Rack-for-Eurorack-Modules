# Rack4Modules native SolidWorks build report

Generated at: 2026-08-25 19:07:49
SolidWorks revision: 33.1.2
Native unique part files: 22
Open-case assembly instances: 49

## Frozen mechanical dimensions

- Front format: 3 x 3U, 104HP per row, 312HP total; no 1U.
- Nominal rail width: 528.32 mm; 104 positions at 5.08 mm pitch.
- Module mounting-hole row spacing: 122.5 mm.
- Body: 548 x 420 x 110 mm, excluding external hardware.
- Lid cavity: 70 mm front clearance; 12 mm body overlap.
- Rear support: VESA 100 x 100 mm, four 4.5 mm M4 clearances.
- Rear interfaces: 8 x 6.35 mm TRS, 3 x DIN-5, 2 x USB D, 2 x 3.5 mm TRS.
- Power connector plate: intentionally blank.
- Reserved busboard: 500 x 85 x 20 mm; reserved central PSU: 210 x 90 x 45 mm.

## Native assemblies

- Rack4Modules_OpenCase.SLDASM: empty front with accessible module rails.
- Rack4Modules_TransportClosed.SLDASM: same case with the 70 mm deep travel lid.
- Rack4Modules_ClearanceCheck.SLDASM: three 104HP panel gauges and both power keepouts.

## Verification boundaries

Connector apertures require supplier drawings for the final chosen parts.
The rear handle, latches, legs and feet are mechanical envelopes, not selected purchased hardware.
The VESA design load is a target; physical fastener, fatigue and static-load tests remain required.
Power, MIDI, USB and audio functions are not designed, electrically connected or physically tested.
Folded-sheet radii, bend allowances, captive nuts and production fasteners require manufacturer DFM.
