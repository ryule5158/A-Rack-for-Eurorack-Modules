using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SolidWorks.Interop.sldworks;

internal static class BuildRack4Modules
{
    [STAThread]
    private static int Main(string[] arguments)
    {
        try
        {
            string root = arguments.Length == 0
                ? Directory.GetCurrentDirectory()
                : Path.GetFullPath(arguments[0]);

            RackCadSession session = new RackCadSession(root);
            new RackConceptGenerator(session).Build();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("BUILD_FAILED=" + exception);
            return 1;
        }
    }
}

internal sealed class RackConceptGenerator
{
    private const string SheetMaterial = "5052-H32";
    private const string FrameMaterial = "6061 Alloy";
    private static readonly double[] Graphite = { 0.12, 0.15, 0.18 };
    private static readonly double[] DarkGrey = { 0.20, 0.23, 0.26 };
    private static readonly double[] NaturalAluminium = { 0.67, 0.70, 0.73 };
    private static readonly double[] AccentBlue = { 0.14, 0.41, 0.62 };
    private static readonly double[] WarningAmber = { 0.93, 0.57, 0.11 };

    private readonly RackCadSession cad;
    private readonly Dictionary<string, string> parts;
    private readonly List<Placement> placements;
    private readonly double bodyWidth;
    private readonly double bodyHeight;
    private readonly double bodyDepth;
    private readonly double sideThickness;
    private readonly double shellThickness;
    private readonly double railLength;
    private readonly double rowPitch;
    private readonly double railSpacing;

    internal RackConceptGenerator(RackCadSession session)
    {
        cad = session;
        parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        placements = new List<Placement>();
        bodyWidth = cad.N("enclosure", "outer_width");
        bodyHeight = cad.N("enclosure", "outer_height");
        bodyDepth = cad.N("enclosure", "body_depth");
        sideThickness = cad.N("enclosure", "side_frame_thickness");
        shellThickness = cad.N("enclosure", "body_thickness");
        railLength = cad.N("rail", "length");
        rowPitch = cad.N("eurorack", "row_pitch");
        railSpacing = cad.N("eurorack", "mounting_hole_vertical_spacing");
    }

    internal void Build()
    {
        cad.Log("STAGE=01 Native aluminium enclosure and interface windows");
        BuildBackPanel();
        BuildSideFrame();
        BuildEdgePanel();

        cad.Log("STAGE=02 Six 104HP rails and continuous M3 tapped strips");
        BuildRail();
        BuildThreadStrip();
        BuildRailEndBlock();

        cad.Log("STAGE=03 Rear VESA load path and replaceable interface cassettes");
        BuildVesaPlate();
        BuildRearCrossBeam();
        BuildVesaStile();
        BuildVesaBridge();
        BuildAudioCassette();
        BuildDigitalCassette();
        BuildPowerBlankCassette();
        BuildVentCassette();

        cad.Log("STAGE=04 Travel lid, handle, latches, folding-leg envelope and feet");
        BuildDeepLid();
        BuildHandle();
        BuildLatch();
        BuildLeg();
        BuildFeet();

        cad.Log("STAGE=05 Module gauges and reserved power envelopes");
        BuildModuleGauge();
        BuildDistributedPowerEnvelope();
        BuildCentralPowerEnvelope();

        ConfigurePlacements();

        cad.Log("STAGE=06 Native SolidWorks assemblies and STEP exports");
        BuildAssembly("Rack4Modules_OpenCase", false, false);
        BuildAssembly("Rack4Modules_TransportClosed", true, false);
        BuildAssembly("Rack4Modules_ClearanceCheck", false, true);
        WriteBuildReport();

        int activateError = 0;
        ModelDoc2 visibleAssembly = (ModelDoc2)cad.Application.ActivateDoc3(
            "Rack4Modules_OpenCase.SLDASM", true, 0, ref activateError);
        if (visibleAssembly != null)
        {
            cad.Show(visibleAssembly);
        }

        cad.Log("BUILD_COMPLETE=true");
        cad.Log("PART_COUNT=" + parts.Count);
        cad.Log("OPEN_ASSEMBLY_COMPONENT_COUNT=" + placements.Count);
        cad.Log("PROJECT_ROOT=" + cad.Root);
    }

    private void BuildBackPanel()
    {
        ModelDoc2 document = cad.NewPart("BackPanel_5052_2mm");
        Body2 body = cad.Box(0, 0, bodyDepth - shellThickness, bodyWidth, bodyHeight, shellThickness);

        body = CutWindow(body, -178, 100, 136, 58, "audio cassette window");
        body = CutWindow(body, 178, 70, 136, 83, "digital cassette window");
        body = CutWindow(body, 178, -120, 68, 35, "future power blank window");
        body = CutWindow(body, -178, -120, 136, 22, "lower-left ventilation window");
        body = CutWindow(body, 0, 174, 136, 22, "upper passive ventilation window");

        foreach (double signX in Signs())
        {
            foreach (double signY in Signs())
            {
                body = HoleThroughZ(body, signX * 50, signY * 50,
                    cad.N("vesa", "hole_diameter"), bodyDepth - shellThickness, shellThickness,
                    "VESA M4 clearance");
                body = HoleThroughZ(body, -178 + signX * 72, 100 + signY * 34,
                    3.4, bodyDepth - shellThickness, shellThickness, "audio cassette M3");
                body = HoleThroughZ(body, 178 + signX * 72, 70 + signY * 46,
                    3.4, bodyDepth - shellThickness, shellThickness, "digital cassette M3");
                body = HoleThroughZ(body, 178 + signX * 38, -120 + signY * 22,
                    3.4, bodyDepth - shellThickness, shellThickness, "power blank cassette M3");
                body = HoleThroughZ(body, -178 + signX * 73, -120 + signY * 9,
                    3.4, bodyDepth - shellThickness, shellThickness, "lower ventilation cassette M3");
                body = HoleThroughZ(body, signX * 73, 174 + signY * 9,
                    3.4, bodyDepth - shellThickness, shellThickness, "upper ventilation cassette M3");
            }
        }

        cad.AddBody(document, body, "Back panel with VESA and five replaceable cassette apertures");
        FinishPart(document, "BackPanel_5052_2mm", SheetMaterial, Graphite, true);
    }

    private void BuildSideFrame()
    {
        ModelDoc2 document = cad.NewPart("SideFrame_6061_3mm");
        Body2 body = cad.Box(0, 0, 0, sideThickness, bodyHeight, bodyDepth - shellThickness);

        foreach (double railY in RailPositions())
        {
            body = cad.Cut(body,
                cad.Cylinder(-sideThickness / 2.0 - 0.4, railY, 6.0, 1, 0, 0, 3.4, sideThickness + 0.8),
                "M3 rail end fastener");
        }

        foreach (double latchY in new double[] { -145, 145 })
        {
            body = cad.Cut(body,
                cad.Cylinder(-sideThickness / 2.0 - 0.4, latchY, 9, 1, 0, 0, 3.4, sideThickness + 0.8),
                "flush travel-latch mounting hole");
        }

        body = cad.Cut(body,
            cad.Cylinder(-sideThickness / 2.0 - 0.4, -125, 92, 1, 0, 0, 6.2, sideThickness + 0.8),
            "folding-leg pivot");

        cad.AddBody(document, body, "Machined left or right 6061 side frame");
        FinishPart(document, "SideFrame_6061_3mm", FrameMaterial, DarkGrey, true);
    }

    private void BuildEdgePanel()
    {
        ModelDoc2 document = cad.NewPart("TopBottomPanel_5052_2mm");
        Body2 body = cad.Box(0, 0, 0, bodyWidth - 2 * sideThickness, shellThickness, bodyDepth);
        cad.AddBody(document, body, "Common top or bottom 5052 folded-panel blank");
        FinishPart(document, "TopBottomPanel_5052_2mm", SheetMaterial, Graphite, true);
    }

    private void BuildRail()
    {
        ModelDoc2 document = cad.NewPart("Rail_104HP_104xM3");
        double height = cad.N("rail", "profile_height");
        double depth = cad.N("rail", "profile_depth");
        double pitch = cad.N("rail", "thread_hole_pitch");
        int holeCount = (int)cad.N("rail", "thread_hole_count");
        Body2 body = cad.Box(0, 0, 0, railLength, height, depth);

        body = cad.Cut(body,
            cad.Box(0, 0, depth - 2.1, railLength + 1.0, cad.N("rail", "thread_strip_width") + 0.4, 2.5),
            "continuous M3 threaded-strip channel");

        for (int index = 0; index < holeCount; index++)
        {
            double x = -railLength / 2.0 + pitch / 2.0 + index * pitch;
            body = HoleThroughZ(body, x, 0, 3.2, 0, depth, "104HP module fastener clearance");
            if ((index + 1) % 26 == 0)
            {
                cad.Log("RAIL_HOLES=" + (index + 1) + "/" + holeCount);
            }
        }

        cad.AddBody(document, body, "104HP rail profile with 104 module screw positions");
        cad.Property(document, "HP", "104");
        cad.Property(document, "Mounting screw", "M3");
        FinishPart(document, "Rail_104HP_104xM3", FrameMaterial, NaturalAluminium, true);
    }

    private void BuildThreadStrip()
    {
        ModelDoc2 document = cad.NewPart("ThreadStrip_104HP_M3Pilot");
        double width = cad.N("rail", "thread_strip_width");
        double thickness = cad.N("rail", "thread_strip_thickness");
        double pitch = cad.N("rail", "thread_hole_pitch");
        double diameter = cad.N("rail", "thread_hole_minor_diameter");
        int holeCount = (int)cad.N("rail", "thread_hole_count");
        Body2 body = cad.Box(0, 0, 0, railLength, width, thickness);

        for (int index = 0; index < holeCount; index++)
        {
            double x = -railLength / 2.0 + pitch / 2.0 + index * pitch;
            body = HoleThroughZ(body, x, 0, diameter, 0, thickness, "M3 tap drill");
            if ((index + 1) % 26 == 0)
            {
                cad.Log("THREAD_STRIP_HOLES=" + (index + 1) + "/" + holeCount);
            }
        }

        cad.AddBody(document, body, "Continuous 104 position M3 tap-drill strip");
        cad.Property(document, "Thread finish", "Tap 104 positions M3 x 0.5 after manufacture");
        FinishPart(document, "ThreadStrip_104HP_M3Pilot", FrameMaterial, NaturalAluminium, true);
    }

    private void BuildRailEndBlock()
    {
        double blockWidth = (bodyWidth - 2 * sideThickness - railLength) / 2.0;
        ModelDoc2 document = cad.NewPart("RailEndBlock_M3");
        Body2 body = cad.Box(0, 0, 0, blockWidth, cad.N("rail", "profile_height"), cad.N("rail", "profile_depth"));
        body = cad.Cut(body,
            cad.Cylinder(-blockWidth / 2.0 - 0.4, 0, 6, 1, 0, 0, 2.5, blockWidth + 0.8),
            "M3 end-block tap drill");
        cad.AddBody(document, body, "Rail-to-side-frame aluminium spacer");
        FinishPart(document, "RailEndBlock_M3", FrameMaterial, NaturalAluminium, false);
    }

    private void BuildVesaPlate()
    {
        ModelDoc2 document = cad.NewPart("VesaReinforcement_100x100_M4");
        double width = cad.N("vesa", "reinforcement_width");
        double height = cad.N("vesa", "reinforcement_height");
        double thickness = cad.N("vesa", "reinforcement_thickness");
        Body2 body = cad.Box(0, 0, 0, width, height, thickness);

        foreach (double signX in Signs())
        {
            foreach (double signY in Signs())
            {
                body = HoleThroughZ(body, signX * 50, signY * 50,
                    cad.N("vesa", "hole_diameter"), 0, thickness, "VESA 100 M4");
            }
        }

        cad.AddBody(document, body, "160 x 160 x 3 mm VESA 100 load-spreading plate");
        cad.Property(document, "Mounting", "VESA MIS-D 100 x 100, four M4 fasteners");
        FinishPart(document, "VesaReinforcement_100x100_M4", FrameMaterial, NaturalAluminium, true);
    }

    private void BuildRearCrossBeam()
    {
        ModelDoc2 document = cad.NewPart("RearCrossBeam_6061");
        cad.AddBody(document,
            cad.Box(0, 0, 0, bodyWidth - 2 * sideThickness, 12, 6),
            "VESA load path from side frame to side frame");
        FinishPart(document, "RearCrossBeam_6061", FrameMaterial, DarkGrey, false);
    }

    private void BuildVesaStile()
    {
        ModelDoc2 document = cad.NewPart("VesaStile_6061");
        cad.AddBody(document,
            cad.Box(0, 0, 0, 10, 322, 6),
            "Vertical VESA load-path stile outside central PSU envelope");
        FinishPart(document, "VesaStile_6061", FrameMaterial, DarkGrey, false);
    }

    private void BuildVesaBridge()
    {
        ModelDoc2 document = cad.NewPart("VesaBridge_6061");
        cad.AddBody(document,
            cad.Box(0, 0, 0, 240, 10, 6),
            "Local VESA reinforcement bridge bypassing PSU cavity");
        FinishPart(document, "VesaBridge_6061", FrameMaterial, DarkGrey, false);
    }

    private void BuildAudioCassette()
    {
        ModelDoc2 document = cad.NewPart("RearAudio_8xTRS635");
        double width = cad.N("rear_io", "audio_plate_width");
        double height = cad.N("rear_io", "audio_plate_height");
        double diameter = cad.N("rear_io", "audio_trs_635_hole_diameter");
        Body2 body = cad.Box(0, 0, 0, width, height, shellThickness);

        foreach (double y in new double[] { -13, 13 })
        {
            foreach (double x in new double[] { -42, -14, 14, 42 })
            {
                body = HoleThroughZ(body, x, y, diameter, 0, shellThickness, "6.35 mm TRS jack");
            }
        }

        body = AddCassetteMounts(body, 72, 34, width, height);
        cad.AddBody(document, body, "Eight isolated 6.35 mm TRS connector apertures");
        cad.Property(document, "Electrical function", "Mechanical reserve only; no audio circuitry included");
        FinishPart(document, "RearAudio_8xTRS635", SheetMaterial, AccentBlue, true);
    }

    private void BuildDigitalCassette()
    {
        ModelDoc2 document = cad.NewPart("RearDigital_3xDIN_2xUSBD_2xTRS35");
        double width = cad.N("rear_io", "digital_plate_width");
        double height = cad.N("rear_io", "digital_plate_height");
        Body2 body = cad.Box(0, 0, 0, width, height, shellThickness);

        foreach (double x in new double[] { -45, 0, 45 })
        {
            body = HoleThroughZ(body, x, 27,
                cad.N("rear_io", "midi_din5_hole_diameter"), 0, shellThickness, "DIN-5 MIDI connector");
            body = HoleThroughZ(body, x - 11.1, 27, 3.2, 0, shellThickness, "DIN-5 mounting ear");
            body = HoleThroughZ(body, x + 11.1, 27, 3.2, 0, shellThickness, "DIN-5 mounting ear");
        }

        foreach (double x in new double[] { -32, 32 })
        {
            body = HoleThroughZ(body, x, -15,
                cad.N("rear_io", "usb_neutrik_d_hole_diameter"), 0, shellThickness, "USB Neutrik D-format");
            body = HoleThroughZ(body, x - 9.5, -27, 3.2, 0, shellThickness, "USB D-format mounting hole");
            body = HoleThroughZ(body, x + 9.5, -3, 3.2, 0, shellThickness, "USB D-format mounting hole");
        }

        body = HoleThroughZ(body, -55, -39,
            cad.N("rear_io", "aux_trs_35_hole_diameter"), 0, shellThickness, "3.5 mm auxiliary TRS");
        body = HoleThroughZ(body, 55, -39,
            cad.N("rear_io", "aux_trs_35_hole_diameter"), 0, shellThickness, "3.5 mm auxiliary TRS");
        body = AddCassetteMounts(body, 72, 46, width, height);

        cad.AddBody(document, body, "Three MIDI DIN-5, two USB D-format and two 3.5 mm apertures");
        cad.Property(document, "Electrical function", "Mechanical reserve only; no MIDI or USB electronics included");
        FinishPart(document, "RearDigital_3xDIN_2xUSBD_2xTRS35", SheetMaterial, AccentBlue, true);
    }

    private void BuildPowerBlankCassette()
    {
        ModelDoc2 document = cad.NewPart("RearPowerBlank_NoConnectorLocked");
        double width = cad.N("rear_io", "power_blank_plate_width");
        double height = cad.N("rear_io", "power_blank_plate_height");
        Body2 body = cad.Box(0, 0, 0, width, height, shellThickness);
        body = AddCassetteMounts(body, 38, 22, width, height);
        cad.AddBody(document, body, "Blank removable power cassette: intentionally no power connector hole");
        cad.Property(document, "Power topology", "Undecided; do not drill a mains or DC inlet until specified");
        FinishPart(document, "RearPowerBlank_NoConnectorLocked", SheetMaterial, Graphite, true);
    }

    private void BuildVentCassette()
    {
        ModelDoc2 document = cad.NewPart("VentCassette_8Slots_Passive");
        double width = cad.N("thermal", "replaceable_vent_panel_width");
        double height = cad.N("thermal", "replaceable_vent_panel_height");
        double slotLength = cad.N("thermal", "slot_length");
        double slotWidth = cad.N("thermal", "slot_width");
        Body2 body = cad.Box(0, 0, 0, width, height, shellThickness);

        for (int index = 0; index < (int)cad.N("thermal", "slot_count"); index++)
        {
            double x = -59.5 + 17.0 * index;
            body = cad.Cut(body,
                cad.Box(x, 0, -0.3, slotWidth, slotLength, shellThickness + 0.6),
                "replaceable passive ventilation slot");
        }

        body = AddCassetteMounts(body, 73, 9, width, height);
        cad.AddBody(document, body, "Eight-slot fanless replaceable ventilation cassette");
        FinishPart(document, "VentCassette_8Slots_Passive", SheetMaterial, DarkGrey, true);
    }

    private void BuildDeepLid()
    {
        ModelDoc2 document = cad.NewPart("DeepTravelLid_70mmClearance");
        double thickness = cad.N("enclosure", "lid_thickness");
        double cavityWidth = bodyWidth + 1.0;
        double cavityHeight = bodyHeight + 1.0;
        double externalWidth = cavityWidth + 2 * thickness;
        double externalHeight = cavityHeight + 2 * thickness;
        double frontZ = -cad.N("enclosure", "lid_inner_clearance");
        double skirtDepth = cad.N("enclosure", "lid_inner_clearance") + cad.N("enclosure", "lid_overlap");

        cad.AddBody(document,
            cad.Box(0, 0, frontZ - thickness, externalWidth, externalHeight, thickness),
            "Deep travel-lid face preserving 70 mm patched-cable clearance");

        foreach (double sign in Signs())
        {
            cad.AddBody(document,
                cad.Box(sign * (cavityWidth / 2.0 + thickness / 2.0), 0,
                    frontZ, thickness, cavityHeight, skirtDepth),
                sign < 0 ? "Left lid return" : "Right lid return");
            cad.AddBody(document,
                cad.Box(0, sign * (cavityHeight / 2.0 + thickness / 2.0),
                    frontZ, externalWidth, thickness, skirtDepth),
                sign < 0 ? "Lower lid return" : "Upper lid return");
        }

        cad.Property(document, "Front patch clearance", "70 mm");
        cad.Property(document, "Construction", "1.5 mm 5052 folded-panel concept; final bends require DFM");
        FinishPart(document, "DeepTravelLid_70mmClearance", SheetMaterial, Graphite, true);
    }

    private void BuildHandle()
    {
        ModelDoc2 document = cad.NewPart("TopFoldFlatHandle_Concept");
        cad.AddBody(document, cad.Box(-90, 0, 0, 18, 10, 12), "Left fold-flat handle mounting block");
        cad.AddBody(document, cad.Box(90, 0, 0, 18, 10, 12), "Right fold-flat handle mounting block");
        cad.AddBody(document, cad.Box(0, 0, 5, 162, 8, 8), "150 mm useful handle grip envelope");
        cad.Property(document, "Hardware status", "Envelope only; select actual folding-handle supplier before production");
        FinishPart(document, "TopFoldFlatHandle_Concept", FrameMaterial, DarkGrey, false);
    }

    private void BuildLatch()
    {
        ModelDoc2 document = cad.NewPart("LidLatch_Concept");
        Body2 body = cad.Box(0, 0, -4, 3, 28, 18);
        cad.AddBody(document, body, "Low-profile removable-lid latch envelope");
        cad.Property(document, "Hardware status", "Four positions reserved; vendor-specific latch not selected");
        FinishPart(document, "LidLatch_Concept", FrameMaterial, DarkGrey, false);
    }

    private void BuildLeg()
    {
        ModelDoc2 document = cad.NewPart("FoldOutLeg_Concept");
        cad.AddBody(document, cad.Box(0, 0, 0, 12, 172, 6), "Fold-flat rear support-leg envelope");
        cad.Property(document, "Angle options", "15 degrees or 30 degrees; hinge hardware pending");
        FinishPart(document, "FoldOutLeg_Concept", FrameMaterial, DarkGrey, false);
    }

    private void BuildFeet()
    {
        ModelDoc2 document = cad.NewPart("EightRubberFeet_Envelope");
        foreach (double x in new double[] { -245, -95, 95, 245 })
        {
            foreach (double y in Signs())
            {
                cad.AddBody(document, cad.Cylinder(x, y * 190, 0, 0, 0, 1, 12, 6),
                    "Rear anti-slip foot at " + x.ToString(CultureInfo.InvariantCulture));
            }
        }

        cad.Property(document, "Material note", "Rubber vendor hardware envelope, not an aluminium fabrication part");
        FinishPart(document, "EightRubberFeet_Envelope", string.Empty, DarkGrey, false);
    }

    private void BuildModuleGauge()
    {
        ModelDoc2 document = cad.NewPart("FitGauge_104HP_3U");
        double panelHeight = cad.N("eurorack", "panel_height");
        cad.AddBody(document,
            cad.Box(0, 0, -cad.N("eurorack", "panel_thickness"),
                railLength - 0.32, panelHeight, cad.N("eurorack", "panel_thickness")),
            "Reference 104HP x 3U module faceplate");
        cad.Property(document, "Purpose", "Mechanical fit gauge only; not a deliverable synthesizer module");
        FinishPart(document, "FitGauge_104HP_3U", FrameMaterial, NaturalAluminium, false);
    }

    private void BuildDistributedPowerEnvelope()
    {
        ModelDoc2 document = cad.NewPart("ReservedPowerBus_500x85x20");
        cad.AddBody(document,
            cad.Box(0, 0, 0, 500, 85, 20),
            "Reserved 500 x 85 x 20 mm distributed Eurorack busboard envelope");
        cad.Property(document, "Purpose", "Keepout envelope only; no PSU, connector or PCB fixing selected");
        FinishPart(document, "ReservedPowerBus_500x85x20", string.Empty, WarningAmber, false);
    }

    private void BuildCentralPowerEnvelope()
    {
        ModelDoc2 document = cad.NewPart("ReservedPowerSupply_210x90x45");
        cad.AddBody(document,
            cad.Box(0, 0, 0, 210, 90, 45),
            "Reserved 210 x 90 x 45 mm central power supply envelope");
        cad.Property(document, "Purpose", "Keepout envelope only; topology and inlet remain undecided");
        FinishPart(document, "ReservedPowerSupply_210x90x45", string.Empty, WarningAmber, false);
    }

    private void ConfigurePlacements()
    {
        placements.Add(new Placement("BackPanel_5052_2mm", "Rear structural shell", 0, 0, 0));
        placements.Add(new Placement("SideFrame_6061_3mm", "Left side frame", -(bodyWidth / 2.0 - sideThickness / 2.0), 0, 0));
        placements.Add(new Placement("SideFrame_6061_3mm", "Right side frame", bodyWidth / 2.0 - sideThickness / 2.0, 0, 0));
        placements.Add(new Placement("TopBottomPanel_5052_2mm", "Top shell edge", 0, bodyHeight / 2.0 - shellThickness / 2.0, 0));
        placements.Add(new Placement("TopBottomPanel_5052_2mm", "Bottom shell edge", 0, -(bodyHeight / 2.0 - shellThickness / 2.0), 0));

        int railIndex = 1;
        double blockWidth = (bodyWidth - 2 * sideThickness - railLength) / 2.0;
        double blockX = railLength / 2.0 + blockWidth / 2.0;
        foreach (double railY in RailPositions())
        {
            placements.Add(new Placement("Rail_104HP_104xM3", "3U mounting rail " + railIndex, 0, railY, 0));
            placements.Add(new Placement("ThreadStrip_104HP_M3Pilot", "M3 threaded strip " + railIndex,
                0, railY, cad.N("rail", "profile_depth") - 2.1));
            placements.Add(new Placement("RailEndBlock_M3", "Left rail spacer " + railIndex, -blockX, railY, 0));
            placements.Add(new Placement("RailEndBlock_M3", "Right rail spacer " + railIndex, blockX, railY, 0));
            railIndex++;
        }

        placements.Add(new Placement("VesaReinforcement_100x100_M4", "VESA 100 reinforcement", 0, 0, 105));
        placements.Add(new Placement("RearCrossBeam_6061", "Upper VESA crossbeam", 0, 155, 99));
        placements.Add(new Placement("RearCrossBeam_6061", "Lower VESA crossbeam", 0, -155, 99));
        placements.Add(new Placement("VesaStile_6061", "Left VESA load stile", -115, 0, 93));
        placements.Add(new Placement("VesaStile_6061", "Right VESA load stile", 115, 0, 93));
        placements.Add(new Placement("VesaBridge_6061", "Upper local VESA bridge", 0, 70, 99));
        placements.Add(new Placement("VesaBridge_6061", "Lower local VESA bridge", 0, -70, 99));

        placements.Add(new Placement("RearAudio_8xTRS635", "Eight 6.35 mm rear audio connectors", -178, 100, bodyDepth));
        placements.Add(new Placement("RearDigital_3xDIN_2xUSBD_2xTRS35", "Rear MIDI, USB and 3.5 mm interfaces", 178, 70, bodyDepth));
        placements.Add(new Placement("RearPowerBlank_NoConnectorLocked", "Undrilled future power cassette", 178, -120, bodyDepth));
        placements.Add(new Placement("VentCassette_8Slots_Passive", "Lower-left removable ventilation panel", -178, -120, bodyDepth));
        placements.Add(new Placement("VentCassette_8Slots_Passive", "Upper removable ventilation panel", 0, 174, bodyDepth));

        placements.Add(new Placement("TopFoldFlatHandle_Concept", "Fold-flat carry handle", 0, 215, 52));
        placements.Add(new Placement("FoldOutLeg_Concept", "Left fold-flat support leg", -258, -65, bodyDepth + shellThickness));
        placements.Add(new Placement("FoldOutLeg_Concept", "Right fold-flat support leg", 258, -65, bodyDepth + shellThickness));
        placements.Add(new Placement("EightRubberFeet_Envelope", "Eight rear rubber feet", 0, 0, bodyDepth + shellThickness));

        foreach (double signX in Signs())
        {
            foreach (double signY in Signs())
            {
                placements.Add(new Placement("LidLatch_Concept",
                    "Travel lid latch " + (signX < 0 ? "L" : "R") + (signY < 0 ? "B" : "T"),
                    signX * (bodyWidth / 2.0 + 1.5), signY * 145, 0));
            }
        }
    }

    private void BuildAssembly(string stem, bool addLid, bool addGauges)
    {
        ModelDoc2 assembly = cad.NewAssembly(stem);
        int index = 0;
        foreach (Placement placement in placements)
        {
            cad.AddComponent(assembly, parts[placement.Part], placement.Label,
                placement.X, placement.Y, placement.Z);
            index++;
            if (index % 10 == 0)
            {
                cad.Log(stem + "_COMPONENTS=" + index);
            }
        }

        if (addLid)
        {
            cad.AddComponent(assembly, parts["DeepTravelLid_70mmClearance"],
                "Removable patched-performance travel lid", 0, 0, 0);
        }

        if (addGauges)
        {
            foreach (double rowY in new double[] { -rowPitch, 0, rowPitch })
            {
                Component2 gauge = cad.AddComponent(assembly, parts["FitGauge_104HP_3U"],
                    "104HP standard 3U panel gauge", 0, rowY, 0);
                gauge.ExcludeFromBOM = true;
            }

            Component2 distributed = cad.AddComponent(assembly, parts["ReservedPowerBus_500x85x20"],
                "Distributed busboard reserved volume", 0, -105, 73);
            distributed.ExcludeFromBOM = true;

            Component2 central = cad.AddComponent(assembly, parts["ReservedPowerSupply_210x90x45"],
                "Central 45 mm power-module reserved volume", 0, 0, 60);
            central.ExcludeFromBOM = true;
        }

        cad.Property(assembly, "Capacity", "Three independent 104HP Eurorack 3U rows; no 1U row");
        cad.Property(assembly, "Design boundary", "Mechanical concept only; PSU and electronic interfaces not designed");
        cad.SaveAssembly(assembly, stem, true);
        cad.Show(assembly);
    }

    private void WriteBuildReport()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("# Rack4Modules native SolidWorks build report");
        report.AppendLine();
        report.AppendLine("Generated at: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        report.AppendLine("SolidWorks revision: " + cad.Application.RevisionNumber());
        report.AppendLine("Native unique part files: " + parts.Count.ToString(CultureInfo.InvariantCulture));
        report.AppendLine("Open-case assembly instances: " + placements.Count.ToString(CultureInfo.InvariantCulture));
        report.AppendLine();
        report.AppendLine("## Frozen mechanical dimensions");
        report.AppendLine();
        report.AppendLine("- Front format: 3 x 3U, 104HP per row, 312HP total; no 1U.");
        report.AppendLine("- Nominal rail width: 528.32 mm; 104 positions at 5.08 mm pitch.");
        report.AppendLine("- Module mounting-hole row spacing: 122.5 mm.");
        report.AppendLine("- Body: 548 x 420 x 110 mm, excluding external hardware.");
        report.AppendLine("- Lid cavity: 70 mm front clearance; 12 mm body overlap.");
        report.AppendLine("- Rear support: VESA 100 x 100 mm, four 4.5 mm M4 clearances.");
        report.AppendLine("- Rear interfaces: 8 x 6.35 mm TRS, 3 x DIN-5, 2 x USB D, 2 x 3.5 mm TRS.");
        report.AppendLine("- Power connector plate: intentionally blank.");
        report.AppendLine("- Reserved busboard: 500 x 85 x 20 mm; reserved central PSU: 210 x 90 x 45 mm.");
        report.AppendLine();
        report.AppendLine("## Native assemblies");
        report.AppendLine();
        report.AppendLine("- Rack4Modules_OpenCase.SLDASM: empty front with accessible module rails.");
        report.AppendLine("- Rack4Modules_TransportClosed.SLDASM: same case with the 70 mm deep travel lid.");
        report.AppendLine("- Rack4Modules_ClearanceCheck.SLDASM: three 104HP panel gauges and both power keepouts.");
        report.AppendLine();
        report.AppendLine("## Verification boundaries");
        report.AppendLine();
        report.AppendLine("Connector apertures require supplier drawings for the final chosen parts.");
        report.AppendLine("The rear handle, latches, legs and feet are mechanical envelopes, not selected purchased hardware.");
        report.AppendLine("The VESA design load is a target; physical fastener, fatigue and static-load tests remain required.");
        report.AppendLine("Power, MIDI, USB and audio functions are not designed, electrically connected or physically tested.");
        report.AppendLine("Folded-sheet radii, bend allowances, captive nuts and production fasteners require manufacturer DFM.");

        string outputPath = Path.Combine(cad.ReportsDirectory, "solidworks-build-report.md");
        File.WriteAllText(outputPath, report.ToString(), new UTF8Encoding(false));
        cad.Log("BUILD_REPORT=" + outputPath);
    }

    private void FinishPart(ModelDoc2 document, string stem, string material, double[] color, bool exportStep)
    {
        cad.ApplyMaterial(document, material, color);
        cad.Property(document, "Project", "Rack4Modules 9U x 104HP Eurorack performance case");
        cad.Property(document, "Units", "mm");
        parts.Add(stem, cad.SavePart(document, stem, exportStep));
        cad.Show(document);
        cad.Log("PART_SAVED=" + stem);
    }

    private Body2 AddCassetteMounts(Body2 body, double horizontal, double vertical, double width, double height)
    {
        if (horizontal >= width / 2.0 || vertical >= height / 2.0)
        {
            throw new InvalidOperationException("Cassette mounting coordinates fall outside the panel blank.");
        }

        foreach (double signX in Signs())
        {
            foreach (double signY in Signs())
            {
                body = HoleThroughZ(body, signX * horizontal, signY * vertical,
                    3.4, 0, shellThickness, "cassette M3 clearance");
            }
        }

        return body;
    }

    private Body2 CutWindow(Body2 body, double x, double y, double width, double height, string description)
    {
        return cad.Cut(body,
            cad.Box(x, y, bodyDepth - shellThickness - 0.3, width, height, shellThickness + 0.6),
            description);
    }

    private Body2 HoleThroughZ(Body2 body, double x, double y, double diameter,
        double startZ, double thickness, string description)
    {
        return cad.Cut(body,
            cad.Cylinder(x, y, startZ - 0.3, 0, 0, 1, diameter, thickness + 0.6),
            description);
    }

    private IEnumerable<double> RailPositions()
    {
        foreach (double rowCenter in new double[] { -rowPitch, 0, rowPitch })
        {
            yield return rowCenter - railSpacing / 2.0;
            yield return rowCenter + railSpacing / 2.0;
        }
    }

    private static IEnumerable<double> Signs()
    {
        yield return -1.0;
        yield return 1.0;
    }

    private sealed class Placement
    {
        internal readonly string Part;
        internal readonly string Label;
        internal readonly double X;
        internal readonly double Y;
        internal readonly double Z;

        internal Placement(string part, string label, double x, double y, double z)
        {
            Part = part;
            Label = label;
            X = x;
            Y = y;
            Z = z;
        }
    }
}
