# Rack4Modules V0.3 native SOLIDWORKS validation

Generated: 2026-08-25 20:17:55
Project root: `C:\Users\LENOVO\Desktop\Rack4Modules`


## Frozen design inputs

- PASS: Body external width -- expected 548 mm, actual 548 mm
- PASS: Rear-facing narrow edge clear width -- expected 542 mm, actual 542 mm
- PASS: Eurorack format -- 3 independent 104HP rows; no 1U row
- PASS: Rail count in source parameters -- 6 full-width rails
- Expected open-case component count is derived from ConfigurePlacements: 5 shell + 4 x 6 rail items + 7 VESA items + 4 edge-zone items + 2 legs + 4 catches + 1 four-foot part = 47.
- Transport adds one lid for 48 components. Clearance adds 3 fit gauges, 3 nominal 85 mm module envelopes and 2 power-reservation envelopes for 55 components.

## Filesystem inventory; SOLIDWORKS lock files excluded

- PASS: Native part directory exists -- C:\Users\LENOVO\Desktop\Rack4Modules\cad\parts
- PASS: Native assembly directory exists -- C:\Users\LENOVO\Desktop\Rack4Modules\cad\assemblies
- PASS: STEP export directory exists -- C:\Users\LENOVO\Desktop\Rack4Modules\exports
- Native `.SLDPRT` files excluding `~$` lock files: 35
- Native `.SLDASM` files excluding `~$` lock files: 6
- STEP files excluding `~$` lock files: 25

## SOLIDWORKS session

- PASS: Connected to an existing SOLIDWORKS session -- 33.1.2

## Native parts, solid bodies and assigned physical materials

- PASS: BackPanel_V03_VESAOnly solid-body count -- expected 1, actual 1
- PASS: BackPanel_V03_VESAOnly physical material -- expected `5052-H32`, actual `5052-H32`
- PASS: SideFrame_V03_RecessedLeg solid-body count -- expected 1, actual 1
- PASS: SideFrame_V03_RecessedLeg physical material -- expected `6061-T6 (SS)`, actual `6061-T6 (SS)`
- PASS: RearEdge_V03_IO_Handle_Power solid-body count -- expected 1, actual 1
- PASS: RearEdge_V03_IO_Handle_Power physical material -- expected `5052-H32`, actual `5052-H32`
- PASS: LowerEdge_V03_HiddenVent solid-body count -- expected 1, actual 1
- PASS: LowerEdge_V03_HiddenVent physical material -- expected `5052-H32`, actual `5052-H32`
- PASS: Rail_104HP_104xM3 solid-body count -- expected 1, actual 1
- PASS: Rail_104HP_104xM3 physical material -- expected `6061-T6 (SS)`, actual `6061-T6 (SS)`
- PASS: ThreadStrip_104HP_M3Pilot solid-body count -- expected 1, actual 1
- PASS: ThreadStrip_104HP_M3Pilot physical material -- expected `6061-T6 (SS)`, actual `6061-T6 (SS)`
- PASS: RailEndBlock_M3 solid-body count -- expected 1, actual 1
- PASS: RailEndBlock_M3 physical material -- expected `6061-T6 (SS)`, actual `6061-T6 (SS)`
- PASS: VesaReinforcement_100x100_M4 solid-body count -- expected 1, actual 1
- PASS: VesaReinforcement_100x100_M4 physical material -- expected `6061-T6 (SS)`, actual `6061-T6 (SS)`
- PASS: RearCrossBeam_6061 solid-body count -- expected 1, actual 1
- PASS: RearCrossBeam_6061 physical material -- expected `6061-T6 (SS)`, actual `6061-T6 (SS)`
- PASS: VesaStile_6061 solid-body count -- expected 1, actual 1
- PASS: VesaStile_6061 physical material -- expected `6061-T6 (SS)`, actual `6061-T6 (SS)`
- PASS: VesaBridge_6061 solid-body count -- expected 1, actual 1
- PASS: VesaBridge_6061 physical material -- expected `6061-T6 (SS)`, actual `6061-T6 (SS)`
- PASS: RearEdgeAudio_V03_8xTRS635 solid-body count -- expected 1, actual 1
- PASS: RearEdgeAudio_V03_8xTRS635 physical material -- expected `5052-H32`, actual `5052-H32`
- PASS: RearEdgeMidiUsb_V03_3xDIN_USB_C solid-body count -- expected 1, actual 1
- PASS: RearEdgeMidiUsb_V03_3xDIN_USB_C physical material -- expected `5052-H32`, actual `5052-H32`
- PASS: RearEdgePowerBlank_V03 solid-body count -- expected 1, actual 1
- PASS: RearEdgePowerBlank_V03 physical material -- expected `5052-H32`, actual `5052-H32`
- PASS: RearCarryHandle_V03_ClearanceFit solid-body count -- expected 3, actual 3
- PASS: RearCarryHandle_V03_ClearanceFit physical material -- expected `6061-T6 (SS)`, actual `6061-T6 (SS)`
- PASS: SideRecessedLeg_V03_TwoPosition solid-body count -- expected 2, actual 2
- PASS: SideRecessedLeg_V03_TwoPosition physical material -- expected `6061-T6 (SS)`, actual `6061-T6 (SS)`
- PASS: InternalLidCatch_V03 solid-body count -- expected 1, actual 1
- PASS: InternalLidCatch_V03 physical material -- expected `6061-T6 (SS)`, actual `6061-T6 (SS)`
- PASS: FourBackFeet_V03 solid-body count -- expected 4, actual 4
- PASS: FourBackFeet_V03 physical material -- expected `6061-T6 (SS)`, actual `6061-T6 (SS)`
- PASS: DeepTravelLid_70mmClearance solid-body count -- expected 5, actual 5
- PASS: DeepTravelLid_70mmClearance physical material -- expected `5052-H32`, actual `5052-H32`
- PASS: DeepTravelLid_70mmClearance solid envelope -- expected 552 x 424 x 83.5 mm; actual 552 x 424 x 83.5 mm
- Travel lid mass is the sum of a separate face and four return bodies; it is a folded-sheet concept, not a measured or bend-qualified lid.
- PASS: FitGauge_104HP_3U solid-body count -- expected 1, actual 1
- PASS: FitGauge_104HP_3U physical material -- expected `6061-T6 (SS)`, actual `6061-T6 (SS)`

## Broad back: central VESA 100 only, with separate corner-foot bodies

- PASS: BackPanel_V03_VESAOnly solid envelope -- expected 548 x 420 x 2 mm; actual 548 x 420 x 2 mm
- PASS: Broad-back through-hole count -- exactly four central VESA holes; actual 4
- PASS: Broad back has no additional signal, power, ventilation or rectangular apertures -- four inner loops, all belonging to VESA
- PASS: VESA M4 clearance hole at (-50, -50) mm -- 4.5 mm hole on a 100 x 100 mm pattern
- PASS: VESA M4 clearance hole at (-50, 50) mm -- 4.5 mm hole on a 100 x 100 mm pattern
- PASS: VESA M4 clearance hole at (50, -50) mm -- 4.5 mm hole on a 100 x 100 mm pattern
- PASS: VESA M4 clearance hole at (50, 50) mm -- 4.5 mm hole on a 100 x 100 mm pattern
- PASS: VesaReinforcement_100x100_M4 solid envelope -- expected 160 x 160 x 3 mm; actual 160 x 160 x 3 mm
- PASS: Reinforcement aligns with VESA position (-50, -50) -- 100 x 100 mm load-spreader hole alignment
- PASS: Reinforcement aligns with VESA position (-50, 50) -- 100 x 100 mm load-spreader hole alignment
- PASS: Reinforcement aligns with VESA position (50, -50) -- 100 x 100 mm load-spreader hole alignment
- PASS: Reinforcement aligns with VESA position (50, 50) -- 100 x 100 mm load-spreader hole alignment
- PASS: Four feet occupy broad-back corners outside the VESA clear zone -- centres x = +/-245 mm, y = +/-185 mm; matched 4

## Rear-facing narrow edge: audio, central handle, MIDI/USB-C, power blank

- PASS: RearEdgeAudio_V03_8xTRS635 solid envelope -- expected 200 x 2 x 80 mm; actual 200 x 2 x 80 mm
- PASS: Single-row 6.35 mm TRS audio connector count -- eight 11.2 mm mechanical openings
- PASS: Audio opening global x = -241 mm -- local x = -77; global z = 55 mm; 22 mm pitch
- PASS: Audio opening global x = -219 mm -- local x = -55; global z = 55 mm; 22 mm pitch
- PASS: Audio opening global x = -197 mm -- local x = -33; global z = 55 mm; 22 mm pitch
- PASS: Audio opening global x = -175 mm -- local x = -11; global z = 55 mm; 22 mm pitch
- PASS: Audio opening global x = -153 mm -- local x = 11; global z = 55 mm; 22 mm pitch
- PASS: Audio opening global x = -131 mm -- local x = 33; global z = 55 mm; 22 mm pitch
- PASS: Audio opening global x = -109 mm -- local x = 55; global z = 55 mm; 22 mm pitch
- PASS: Audio opening global x = -87 mm -- local x = 77; global z = 55 mm; 22 mm pitch
- PASS: Audio cassette has four M3 clearance mounting holes -- 4 x diameter 3.4 mm
- PASS: Audio cassette contains only eight connector holes and four mounting holes -- 12 through apertures
- PASS: RearEdgeMidiUsb_V03_3xDIN_USB_C solid envelope -- expected 150 x 2 x 80 mm; actual 150 x 2 x 80 mm
- PASS: DIN-5 MIDI opening count -- three diameter-15 mm DIN body openings
- PASS: DIN-5 body opening global x = 87 mm -- local x = -52; global z = 55 mm
- PASS: DIN-5 body opening global x = 121 mm -- local x = -18; global z = 55 mm
- PASS: DIN-5 body opening global x = 155 mm -- local x = 16; global z = 55 mm
- PASS: DIN-5 mounting-ear holes -- three pairs, diameter 3.2 mm, 22.2 mm pair spacing
- PASS: Provisional USB-C carrier mounting holes -- 2 x diameter 2.4 mm; final vendor drawing not selected
- PASS: Digital cassette M3 mounting-hole count -- 4 x diameter 3.4 mm
- PASS: One provisional USB-C mechanical opening -- 12 x 6 mm, global centre x = 191 mm, z = 55 mm; supplier-specific fit remains unverified
- PASS: Digital cassette aperture total -- 3 DIN + 6 DIN ears + 1 USB-C rectangle + 2 USB ears + 4 panel fasteners
- PASS: RearEdgePowerBlank_V03 solid envelope -- expected 50 x 2 x 80 mm; actual 50 x 2 x 80 mm
- PASS: Independent power plate remains electrically undrilled -- four M3 cassette fasteners only; zero inlet, switch or connector holes
- PASS: Power plate has no concealed rectangular or additional circular opening -- 4 mounting-only through loops
- PASS: RearEdge_V03_IO_Handle_Power solid envelope -- expected 542 x 2 x 108 mm; actual 542 x 2 x 108 mm
- PASS: Audio support-window alignment -- x = -164 mm; 180 x 60 mm
- PASS: MIDI/USB support-window alignment -- x = +139 mm; 132 x 60 mm
- PASS: Power-blank support-window alignment -- x = +239 mm; 34 x 60 mm
- PASS: Two narrow-edge joiner slots -- x = +/-265 mm, outside interface windows
- PASS: Central carry-handle support fasteners -- four diameter-5.2 mm holes at x = +/-55 mm
- PASS: Three replaceable narrow-edge cassette mounting groups -- 12 total M3 clearance holes
- PASS: LowerEdge_V03_HiddenVent solid envelope -- expected 542 x 2 x 108 mm; actual 542 x 2 x 108 mm
- PASS: Passive ventilation remains on the lower narrow edge -- two separated groups of eight 22 x 4 mm slots; actual 16
- PASS: Lower narrow edge retains two joiner slots -- actual 2
- PASS: Corrected central carry-handle outer/grip width -- expected 126 mm, with 110 mm mounting pitch, actual 126 mm
- PASS: Carry-handle mounting centres -- x = -55 and +55 mm; 110 mm pitch

## 3 x 104HP rail and continuous threaded-strip geometry

- PASS: Rail_104HP_104xM3 solid envelope -- expected 528.32 x 10 x 12 mm; actual 528.32 x 10 x 12 mm
- PASS: Rail contains 104 module fastener positions -- 104 diameter-3.2 mm openings on 5.08 mm pitch
- PASS: ThreadStrip_104HP_M3Pilot solid envelope -- expected 528.32 x 6 x 2 mm; actual 528.32 x 6 x 2 mm
- PASS: Continuous strip contains 104 M3 tap-pilot positions -- 104 diameter-2.5 mm pilot holes
- PASS: FitGauge_104HP_3U solid envelope -- expected 528 x 128.5 x 2 mm; actual 528 x 128.5 x 2 mm

## Power reservation volumes; no power product or circuit selected

- PASS: ReservedPowerBus_500x85x20 solid envelope -- expected 500 x 85 x 20 mm; actual 500 x 85 x 20 mm
- Distributed power-bus box is a keepout marker, not a selected busboard, regulator or physically manufactured aluminium part.
- PASS: ReservedPowerSupply_210x90x45 solid envelope -- expected 210 x 90 x 45 mm; actual 210 x 90 x 45 mm
- Central PSU box is a keepout marker, not a selected power module, inlet, mains circuit or validated isolation boundary.
- PASS: ModuleDepthEnvelope_85mm_V03 solid envelope -- expected 528 x 112 x 73 mm; actual 528 x 112 x 73 mm
- PASS: 85 mm nominal module-depth reference body -- nonphysical envelope begins behind 12 mm rails and ends 85 mm behind the module face
- Module envelopes deliberately expose the 25 mm central PSU and 12 mm distributed-bus depth conflicts; they do not represent manufactured modules.

## OPEN native assembly and STEP export

- PASS: open native SLDASM exists -- C:\Users\LENOVO\Desktop\Rack4Modules\cad\assemblies\Rack4Modules_OpenCase_V03.SLDASM
- PASS: open STEP export exists -- C:\Users\LENOVO\Desktop\Rack4Modules\exports\Rack4Modules_OpenCase_V03.STEP
- PASS: open native assembly opens in SOLIDWORKS -- C:\Users\LENOVO\Desktop\Rack4Modules\cad\assemblies\Rack4Modules_OpenCase_V03.SLDASM
- PASS: open component count matches V0.3 source placement formula -- expected 47, SOLIDWORKS 47, enumerated 47
- PASS: open component `BackPanel_V03_VESAOnly` -- expected 1, actual 1
- PASS: open component `SideFrame_V03_RecessedLeg` -- expected 2, actual 2
- PASS: open component `RearEdge_V03_IO_Handle_Power` -- expected 1, actual 1
- PASS: open component `LowerEdge_V03_HiddenVent` -- expected 1, actual 1
- PASS: open component `Rail_104HP_104xM3` -- expected 6, actual 6
- PASS: open component `ThreadStrip_104HP_M3Pilot` -- expected 6, actual 6
- PASS: open component `RailEndBlock_M3` -- expected 12, actual 12
- PASS: open component `VesaReinforcement_100x100_M4` -- expected 1, actual 1
- PASS: open component `RearCrossBeam_6061` -- expected 2, actual 2
- PASS: open component `VesaStile_6061` -- expected 2, actual 2
- PASS: open component `VesaBridge_6061` -- expected 2, actual 2
- PASS: open component `RearEdgeAudio_V03_8xTRS635` -- expected 1, actual 1
- PASS: open component `RearEdgeMidiUsb_V03_3xDIN_USB_C` -- expected 1, actual 1
- PASS: open component `RearEdgePowerBlank_V03` -- expected 1, actual 1
- PASS: open component `RearCarryHandle_V03_ClearanceFit` -- expected 1, actual 1
- PASS: open component `SideRecessedLeg_V03_TwoPosition` -- expected 2, actual 2
- PASS: open component `InternalLidCatch_V03` -- expected 4, actual 4
- PASS: open component `FourBackFeet_V03` -- expected 1, actual 1
- PASS: open position `BackPanel_V03_VESAOnly` -- expected (0, 0, 0) mm; actual (0, 0, 0) mm
- PASS: open position `RearEdge_V03_IO_Handle_Power` -- expected (0, 209, 0) mm; actual (0, 209, 0) mm
- PASS: open position `LowerEdge_V03_HiddenVent` -- expected (0, -209, 0) mm; actual (0, -209, 0) mm
- PASS: open position `RearEdgeAudio_V03_8xTRS635` -- expected (-164, 211, 15) mm; actual (-164, 211, 15) mm
- PASS: open position `RearEdgeMidiUsb_V03_3xDIN_USB_C` -- expected (139, 211, 15) mm; actual (139, 211, 15) mm
- PASS: open position `RearEdgePowerBlank_V03` -- expected (239, 211, 15) mm; actual (239, 211, 15) mm
- PASS: open position `RearCarryHandle_V03_ClearanceFit` -- expected (0, 215, 45) mm; actual (0, 215, 45) mm
- PASS: open position `VesaReinforcement_100x100_M4` -- expected (0, 0, 105) mm; actual (0, 0, 105) mm
- PASS: open position `FourBackFeet_V03` -- expected (0, 0, 110) mm; actual (0, 0, 110) mm
- PASS: open 3U row-to-rail vertical layout -- three 133.35 mm-pitch rows, each with a pair of rails 122.5 mm apart
- PASS: open flush internal cover catch 1 -- expected x = +/-272, y = +/-150, z = 55 mm; actual (-272, 150, 55) mm
- PASS: open cover catch remains inside case width 1 -- component bounding box remains within x = +/-274 mm
- PASS: open corrected recessed-leg centre 1 -- expected x = +/-271, y = -56, z = 46 mm; actual (-271, -56, 46) mm
- PASS: open recessed-leg outer surface 1 -- component bounding box remains within external case x = +/-274 mm
- PASS: open recessed-leg pivot clears side-pocket boundaries 1 -- pocket y = -137..27, z = 42..64 mm; actual y = -131..27, z = 44..60
- PASS: open corrected recessed-leg centre 2 -- expected x = +/-271, y = -56, z = 46 mm; actual (271, -56, 46) mm
- PASS: open recessed-leg outer surface 2 -- component bounding box remains within external case x = +/-274 mm
- PASS: open recessed-leg pivot clears side-pocket boundaries 2 -- pocket y = -137..27, z = 42..64 mm; actual y = -131..27, z = 44..60
- PASS: open flush internal cover catch 2 -- expected x = +/-272, y = +/-150, z = 55 mm; actual (-272, -150, 55) mm
- PASS: open cover catch remains inside case width 2 -- component bounding box remains within x = +/-274 mm
- PASS: open flush internal cover catch 3 -- expected x = +/-272, y = +/-150, z = 55 mm; actual (272, -150, 55) mm
- PASS: open cover catch remains inside case width 3 -- component bounding box remains within x = +/-274 mm
- PASS: open flush internal cover catch 4 -- expected x = +/-272, y = +/-150, z = 55 mm; actual (272, 150, 55) mm
- PASS: open cover catch remains inside case width 4 -- component bounding box remains within x = +/-274 mm
- PASS: open short-side recessed folding-leg count -- 2 legs; actual 2
- PASS: open internal cover-lock count -- 4 flush catches; actual 4
- PASS: open corrected handle clears both neighbouring removable panels -- audio-to-handle 1 mm; handle-to-MIDI/USB 1 mm
- open actual SOLIDWORKS bounding-box minimum = (-274, -210, 0) mm; maximum = (274, 220, 116) mm.
- open assembly envelope including conceptual hardware: 548 x 430 x 116 mm.
- PASS: open IMassProperty material-derived CAD mass is finite -- 4.175 kg; calculated CAD mass, never a physical scale measurement
- open CAD mass includes conceptual handle/legs/catches and four feet currently modelled as 6061 aluminium rather than selected rubber; repeated component instances and overlapping multibody reference solids are counted.
- PASS: open interference results were classified -- API total=0; physical=0; real keepout violations=0; intentional module/power conflicts=0; conceptual=0; contact/tolerance=0
- PASS: open no detected physical collision or unclassified power-reserve violation -- 0 intentional packaging conflicts, 0 conceptual overlaps and 0 contact/tolerance candidates are separately reported
- PASS: open STEP export can be imported by SOLIDWORKS -- unique temporary copy of the exact export; import errors=0; source=C:\Users\LENOVO\Desktop\Rack4Modules\exports\Rack4Modules_OpenCase_V03.STEP
- PASS: open STEP import contains assembly geometry -- 47 components

## TRANSPORT native assembly and STEP export

- PASS: transport native SLDASM exists -- C:\Users\LENOVO\Desktop\Rack4Modules\cad\assemblies\Rack4Modules_TransportClosed_V03.SLDASM
- PASS: transport STEP export exists -- C:\Users\LENOVO\Desktop\Rack4Modules\exports\Rack4Modules_TransportClosed_V03.STEP
- WARNING: SOLIDWORKS open warning for Rack4Modules_TransportClosed_V03.SLDASM -- warning bitmask 32
- PASS: transport native assembly opens in SOLIDWORKS -- C:\Users\LENOVO\Desktop\Rack4Modules\cad\assemblies\Rack4Modules_TransportClosed_V03.SLDASM
- PASS: transport component count matches V0.3 source placement formula -- expected 48, SOLIDWORKS 48, enumerated 48
- PASS: transport component `BackPanel_V03_VESAOnly` -- expected 1, actual 1
- PASS: transport component `SideFrame_V03_RecessedLeg` -- expected 2, actual 2
- PASS: transport component `RearEdge_V03_IO_Handle_Power` -- expected 1, actual 1
- PASS: transport component `LowerEdge_V03_HiddenVent` -- expected 1, actual 1
- PASS: transport component `Rail_104HP_104xM3` -- expected 6, actual 6
- PASS: transport component `ThreadStrip_104HP_M3Pilot` -- expected 6, actual 6
- PASS: transport component `RailEndBlock_M3` -- expected 12, actual 12
- PASS: transport component `VesaReinforcement_100x100_M4` -- expected 1, actual 1
- PASS: transport component `RearCrossBeam_6061` -- expected 2, actual 2
- PASS: transport component `VesaStile_6061` -- expected 2, actual 2
- PASS: transport component `VesaBridge_6061` -- expected 2, actual 2
- PASS: transport component `RearEdgeAudio_V03_8xTRS635` -- expected 1, actual 1
- PASS: transport component `RearEdgeMidiUsb_V03_3xDIN_USB_C` -- expected 1, actual 1
- PASS: transport component `RearEdgePowerBlank_V03` -- expected 1, actual 1
- PASS: transport component `RearCarryHandle_V03_ClearanceFit` -- expected 1, actual 1
- PASS: transport component `SideRecessedLeg_V03_TwoPosition` -- expected 2, actual 2
- PASS: transport component `InternalLidCatch_V03` -- expected 4, actual 4
- PASS: transport component `FourBackFeet_V03` -- expected 1, actual 1
- PASS: transport component `DeepTravelLid_70mmClearance` -- expected 1, actual 1
- PASS: transport position `BackPanel_V03_VESAOnly` -- expected (0, 0, 0) mm; actual (0, 0, 0) mm
- PASS: transport position `RearEdge_V03_IO_Handle_Power` -- expected (0, 209, 0) mm; actual (0, 209, 0) mm
- PASS: transport position `LowerEdge_V03_HiddenVent` -- expected (0, -209, 0) mm; actual (0, -209, 0) mm
- PASS: transport position `RearEdgeAudio_V03_8xTRS635` -- expected (-164, 211, 15) mm; actual (-164, 211, 15) mm
- PASS: transport position `RearEdgeMidiUsb_V03_3xDIN_USB_C` -- expected (139, 211, 15) mm; actual (139, 211, 15) mm
- PASS: transport position `RearEdgePowerBlank_V03` -- expected (239, 211, 15) mm; actual (239, 211, 15) mm
- PASS: transport position `RearCarryHandle_V03_ClearanceFit` -- expected (0, 215, 45) mm; actual (0, 215, 45) mm
- PASS: transport position `VesaReinforcement_100x100_M4` -- expected (0, 0, 105) mm; actual (0, 0, 105) mm
- PASS: transport position `FourBackFeet_V03` -- expected (0, 0, 110) mm; actual (0, 0, 110) mm
- PASS: transport 3U row-to-rail vertical layout -- three 133.35 mm-pitch rows, each with a pair of rails 122.5 mm apart
- PASS: transport flush internal cover catch 1 -- expected x = +/-272, y = +/-150, z = 55 mm; actual (-272, 150, 55) mm
- PASS: transport cover catch remains inside case width 1 -- component bounding box remains within x = +/-274 mm
- PASS: transport flush internal cover catch 2 -- expected x = +/-272, y = +/-150, z = 55 mm; actual (272, -150, 55) mm
- PASS: transport cover catch remains inside case width 2 -- component bounding box remains within x = +/-274 mm
- PASS: transport corrected recessed-leg centre 1 -- expected x = +/-271, y = -56, z = 46 mm; actual (271, -56, 46) mm
- PASS: transport recessed-leg outer surface 1 -- component bounding box remains within external case x = +/-274 mm
- PASS: transport recessed-leg pivot clears side-pocket boundaries 1 -- pocket y = -137..27, z = 42..64 mm; actual y = -131..27, z = 44..60
- PASS: transport flush internal cover catch 3 -- expected x = +/-272, y = +/-150, z = 55 mm; actual (-272, -150, 55) mm
- PASS: transport cover catch remains inside case width 3 -- component bounding box remains within x = +/-274 mm
- PASS: transport corrected recessed-leg centre 2 -- expected x = +/-271, y = -56, z = 46 mm; actual (-271, -56, 46) mm
- PASS: transport recessed-leg outer surface 2 -- component bounding box remains within external case x = +/-274 mm
- PASS: transport recessed-leg pivot clears side-pocket boundaries 2 -- pocket y = -137..27, z = 42..64 mm; actual y = -131..27, z = 44..60
- PASS: transport flush internal cover catch 4 -- expected x = +/-272, y = +/-150, z = 55 mm; actual (272, 150, 55) mm
- PASS: transport cover catch remains inside case width 4 -- component bounding box remains within x = +/-274 mm
- PASS: transport short-side recessed folding-leg count -- 2 legs; actual 2
- PASS: transport internal cover-lock count -- 4 flush catches; actual 4
- PASS: transport corrected handle clears both neighbouring removable panels -- audio-to-handle 1 mm; handle-to-MIDI/USB 1 mm
- transport actual SOLIDWORKS bounding-box minimum = (-276, -212, -71.5) mm; maximum = (276, 220, 116) mm.
- transport assembly envelope including conceptual hardware: 552 x 432 x 187.5 mm.
- PASS: transport IMassProperty material-derived CAD mass is finite -- 5.757 kg; calculated CAD mass, never a physical scale measurement
- transport CAD mass includes conceptual handle/legs/catches and four feet currently modelled as 6061 aluminium rather than selected rubber; repeated component instances and overlapping multibody reference solids are counted.
- PASS: transport interference results were classified -- API total=0; physical=0; real keepout violations=0; intentional module/power conflicts=0; conceptual=0; contact/tolerance=0
- PASS: transport no detected physical collision or unclassified power-reserve violation -- 0 intentional packaging conflicts, 0 conceptual overlaps and 0 contact/tolerance candidates are separately reported
- PASS: transport STEP export can be imported by SOLIDWORKS -- unique temporary copy of the exact export; import errors=0; source=C:\Users\LENOVO\Desktop\Rack4Modules\exports\Rack4Modules_TransportClosed_V03.STEP
- PASS: transport STEP import contains assembly geometry -- 48 components

## CLEARANCE native assembly and STEP export

- PASS: clearance native SLDASM exists -- C:\Users\LENOVO\Desktop\Rack4Modules\cad\assemblies\Rack4Modules_ClearanceCheck_V03.SLDASM
- PASS: clearance STEP export exists -- C:\Users\LENOVO\Desktop\Rack4Modules\exports\Rack4Modules_ClearanceCheck_V03.STEP
- WARNING: SOLIDWORKS open warning for Rack4Modules_ClearanceCheck_V03.SLDASM -- warning bitmask 32
- PASS: clearance native assembly opens in SOLIDWORKS -- C:\Users\LENOVO\Desktop\Rack4Modules\cad\assemblies\Rack4Modules_ClearanceCheck_V03.SLDASM
- PASS: clearance component count matches V0.3 source placement formula -- expected 55, SOLIDWORKS 55, enumerated 55
- PASS: clearance component `BackPanel_V03_VESAOnly` -- expected 1, actual 1
- PASS: clearance component `SideFrame_V03_RecessedLeg` -- expected 2, actual 2
- PASS: clearance component `RearEdge_V03_IO_Handle_Power` -- expected 1, actual 1
- PASS: clearance component `LowerEdge_V03_HiddenVent` -- expected 1, actual 1
- PASS: clearance component `Rail_104HP_104xM3` -- expected 6, actual 6
- PASS: clearance component `ThreadStrip_104HP_M3Pilot` -- expected 6, actual 6
- PASS: clearance component `RailEndBlock_M3` -- expected 12, actual 12
- PASS: clearance component `VesaReinforcement_100x100_M4` -- expected 1, actual 1
- PASS: clearance component `RearCrossBeam_6061` -- expected 2, actual 2
- PASS: clearance component `VesaStile_6061` -- expected 2, actual 2
- PASS: clearance component `VesaBridge_6061` -- expected 2, actual 2
- PASS: clearance component `RearEdgeAudio_V03_8xTRS635` -- expected 1, actual 1
- PASS: clearance component `RearEdgeMidiUsb_V03_3xDIN_USB_C` -- expected 1, actual 1
- PASS: clearance component `RearEdgePowerBlank_V03` -- expected 1, actual 1
- PASS: clearance component `RearCarryHandle_V03_ClearanceFit` -- expected 1, actual 1
- PASS: clearance component `SideRecessedLeg_V03_TwoPosition` -- expected 2, actual 2
- PASS: clearance component `InternalLidCatch_V03` -- expected 4, actual 4
- PASS: clearance component `FourBackFeet_V03` -- expected 1, actual 1
- PASS: clearance component `FitGauge_104HP_3U` -- expected 3, actual 3
- PASS: clearance component `ModuleDepthEnvelope_85mm_V03` -- expected 3, actual 3
- PASS: clearance component `ReservedPowerBus_500x85x20` -- expected 1, actual 1
- PASS: clearance component `ReservedPowerSupply_210x90x45` -- expected 1, actual 1
- PASS: clearance position `BackPanel_V03_VESAOnly` -- expected (0, 0, 0) mm; actual (0, 0, 0) mm
- PASS: clearance position `RearEdge_V03_IO_Handle_Power` -- expected (0, 209, 0) mm; actual (0, 209, 0) mm
- PASS: clearance position `LowerEdge_V03_HiddenVent` -- expected (0, -209, 0) mm; actual (0, -209, 0) mm
- PASS: clearance position `RearEdgeAudio_V03_8xTRS635` -- expected (-164, 211, 15) mm; actual (-164, 211, 15) mm
- PASS: clearance position `RearEdgeMidiUsb_V03_3xDIN_USB_C` -- expected (139, 211, 15) mm; actual (139, 211, 15) mm
- PASS: clearance position `RearEdgePowerBlank_V03` -- expected (239, 211, 15) mm; actual (239, 211, 15) mm
- PASS: clearance position `RearCarryHandle_V03_ClearanceFit` -- expected (0, 215, 45) mm; actual (0, 215, 45) mm
- PASS: clearance position `VesaReinforcement_100x100_M4` -- expected (0, 0, 105) mm; actual (0, 0, 105) mm
- PASS: clearance position `FourBackFeet_V03` -- expected (0, 0, 110) mm; actual (0, 0, 110) mm
- PASS: clearance 3U row-to-rail vertical layout -- three 133.35 mm-pitch rows, each with a pair of rails 122.5 mm apart
- PASS: clearance flush internal cover catch 1 -- expected x = +/-272, y = +/-150, z = 55 mm; actual (272, -150, 55) mm
- PASS: clearance cover catch remains inside case width 1 -- component bounding box remains within x = +/-274 mm
- PASS: clearance corrected recessed-leg centre 1 -- expected x = +/-271, y = -56, z = 46 mm; actual (271, -56, 46) mm
- PASS: clearance recessed-leg outer surface 1 -- component bounding box remains within external case x = +/-274 mm
- PASS: clearance recessed-leg pivot clears side-pocket boundaries 1 -- pocket y = -137..27, z = 42..64 mm; actual y = -131..27, z = 44..60
- PASS: clearance flush internal cover catch 2 -- expected x = +/-272, y = +/-150, z = 55 mm; actual (-272, -150, 55) mm
- PASS: clearance cover catch remains inside case width 2 -- component bounding box remains within x = +/-274 mm
- PASS: clearance flush internal cover catch 3 -- expected x = +/-272, y = +/-150, z = 55 mm; actual (-272, 150, 55) mm
- PASS: clearance cover catch remains inside case width 3 -- component bounding box remains within x = +/-274 mm
- PASS: clearance flush internal cover catch 4 -- expected x = +/-272, y = +/-150, z = 55 mm; actual (272, 150, 55) mm
- PASS: clearance cover catch remains inside case width 4 -- component bounding box remains within x = +/-274 mm
- PASS: clearance corrected recessed-leg centre 2 -- expected x = +/-271, y = -56, z = 46 mm; actual (-271, -56, 46) mm
- PASS: clearance recessed-leg outer surface 2 -- component bounding box remains within external case x = +/-274 mm
- PASS: clearance recessed-leg pivot clears side-pocket boundaries 2 -- pocket y = -137..27, z = 42..64 mm; actual y = -131..27, z = 44..60
- PASS: clearance short-side recessed folding-leg count -- 2 legs; actual 2
- PASS: clearance internal cover-lock count -- 4 flush catches; actual 4
- PASS: clearance corrected handle clears both neighbouring removable panels -- audio-to-handle 1 mm; handle-to-MIDI/USB 1 mm
- PASS: clearance position `ReservedPowerBus_500x85x20` -- expected (0, -105, 73) mm; actual (0, -105, 73) mm
- PASS: clearance position `ReservedPowerSupply_210x90x45` -- expected (0, 0, 60) mm; actual (0, 0, 60) mm
- PASS: clearance nonphysical marker excluded from bill of materials: FitGauge_104HP_3U -- gauge/keepout must not be represented as a purchased physical assembly part
- PASS: clearance nonphysical marker excluded from bill of materials: ModuleDepthEnvelope_85mm_V03 -- gauge/keepout must not be represented as a purchased physical assembly part
- PASS: clearance nonphysical marker excluded from bill of materials: FitGauge_104HP_3U -- gauge/keepout must not be represented as a purchased physical assembly part
- PASS: clearance nonphysical marker excluded from bill of materials: ReservedPowerBus_500x85x20 -- gauge/keepout must not be represented as a purchased physical assembly part
- PASS: clearance nonphysical marker excluded from bill of materials: ModuleDepthEnvelope_85mm_V03 -- gauge/keepout must not be represented as a purchased physical assembly part
- PASS: clearance nonphysical marker excluded from bill of materials: ModuleDepthEnvelope_85mm_V03 -- gauge/keepout must not be represented as a purchased physical assembly part
- PASS: clearance nonphysical marker excluded from bill of materials: FitGauge_104HP_3U -- gauge/keepout must not be represented as a purchased physical assembly part
- PASS: clearance nonphysical marker excluded from bill of materials: ReservedPowerSupply_210x90x45 -- gauge/keepout must not be represented as a purchased physical assembly part
- PASS: clearance 3 x 104HP fit-gauge row positions -- y = -133.35, 0 and +133.35 mm
- PASS: clearance 3 nominal 85 mm module-envelope row positions -- y = -133.35, 0 and +133.35 mm; each marker is excluded from the physical BOM
- PASS: Central PSU footprint local module-depth limit -- only 60 mm before the reserved 45 mm PSU volume
- PASS: Distributed bus footprint local module-depth limit -- only 73 mm before the reserved 20 mm bus volume
- PASS: Documented central PSU versus nominal module-depth conflict -- 85 mm nominal module intersects the PSU reservation by 25 mm; maximum module depth in that footprint is 60 mm
- WARNING: Unresolved central PSU packaging constraint -- A full-depth 85 mm module and the central 45 mm PSU reservation cannot coexist; 25 mm overlap remains.
- PASS: Documented distributed-bus versus nominal module-depth conflict -- 85 mm nominal module intersects the bus reservation by 12 mm; maximum module depth in that footprint is 73 mm
- WARNING: Unresolved distributed-bus packaging constraint -- A full-depth 85 mm module and the distributed power-bus reservation cannot coexist; 12 mm overlap remains.
- clearance actual SOLIDWORKS bounding-box minimum = (-274, -210, -2) mm; maximum = (274, 220, 116) mm.
- clearance assembly envelope including conceptual hardware: 548 x 430 x 118 mm.
- PASS: clearance IMassProperty material-derived CAD mass is finite -- 19.925 kg; calculated CAD mass, never a physical scale measurement
- clearance CAD mass includes conceptual handle/legs/catches and four feet currently modelled as 6061 aluminium rather than selected rubber; repeated component instances and overlapping multibody reference solids are counted.
- Clearance CAD mass additionally includes nonphysical fit gauges, nominal module envelopes and power keepouts; envelopes may have no assigned physical material or default density, so this is not the empty-case mass.
- WARNING: clearance intentional 85 mm module versus power-reservation overlap -- ModuleDepthEnvelope_85mm_V03 <-> ReservedPowerSupply_210x90x45; overlapping volume 472500 mm^3; known packaging limit: central PSU permits 60 mm and bus region permits 73 mm
- WARNING: clearance intentional 85 mm module versus power-reservation overlap -- ModuleDepthEnvelope_85mm_V03 <-> ReservedPowerBus_500x85x20; overlapping volume 420900 mm^3; known packaging limit: central PSU permits 60 mm and bus region permits 73 mm
- PASS: clearance interference results were classified -- API total=2; physical=0; real keepout violations=0; intentional module/power conflicts=2; conceptual=0; contact/tolerance=0
- PASS: clearance no detected physical collision or unclassified power-reserve violation -- 2 intentional packaging conflicts, 0 conceptual overlaps and 0 contact/tolerance candidates are separately reported
- PASS: clearance STEP export can be imported by SOLIDWORKS -- unique temporary copy of the exact export; import errors=0; source=C:\Users\LENOVO\Desktop\Rack4Modules\exports\Rack4Modules_ClearanceCheck_V03.STEP
- PASS: clearance STEP import contains assembly geometry -- 55 components

## Engineering and electrical validation boundary

- The 8 audio apertures, 3 DIN-5 apertures and USB-C opening are mechanical provisions only.
- No audio signal direction, MIDI transceiver, USB protocol, connector vendor, PCB or electrical operation is validated.
- The removable power plate has no inlet, switch, connector or power-topology commitment.
- Central PSU keepout leaves 60 mm local module depth; distributed bus keepout leaves 73 mm. An unrestricted 85 mm depth is not available across those zones.
- Material assignment and geometry are not load certification, thermal qualification, supplier fit confirmation or physical test evidence.
- Interference checks separate zero-volume contact, conceptual gauge/hardware overlap, reserved-volume violations and physical solid interference.

## Result

- Status: **PASS**
- Passed checks: 291
- Warnings requiring engineering review: 6
- Failed checks: 0
- Native CAD documents were inspected without saving or replacing them.
