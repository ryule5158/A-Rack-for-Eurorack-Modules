using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal static class ReviseRackLayoutV03
{
    [STAThread]
    private static int Main(string[] arguments)
    {
        try
        {
            string root = arguments.Length == 0 ? Directory.GetCurrentDirectory() : Path.GetFullPath(arguments[0]);
            RackCadSession session = new RackCadSession(root);
            new EdgeReferencedLayoutBuilder(session).Build();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V03_BUILD_FAILED=" + exception);
            return 1;
        }
    }
}

internal sealed class EdgeReferencedLayoutBuilder
{
    private const string SheetMaterial = "5052-H32";
    private const string FrameMaterial = "6061-T6 (SS)";
    private static readonly double[] Silver = { 0.67, 0.70, 0.73 };
    private static readonly double[] Graphite = { 0.12, 0.15, 0.18 };
    private static readonly double[] DarkHardware = { 0.06, 0.07, 0.08 };
    private static readonly double[] ConnectorBlue = { 0.12, 0.35, 0.55 };

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

    internal EdgeReferencedLayoutBuilder(RackCadSession session)
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
        VerifyExistingSessionIsProjectOnly();
        cad.Application.DocumentVisible(true, (int)swDocumentTypes_e.swDocPART);
        cad.Application.Visible = true;
        cad.Application.UserControl = true;

        cad.Log("V03_REFERENCE=Intellijel Gen-2 primary; Befaco 7U secondary");
        cad.Log("V03_LAYOUT=Rear-facing narrow edge I/O; broad back reserved for VESA");

        BuildBroadBackV03();
        BuildSideFrameV03();
        BuildRearIoEdge();
        BuildLowerVentEdge();
        BuildAudioEdgeCassette();
        BuildMidiUsbEdgeCassette();
        BuildPowerBlankEdgeCassette();
        BuildRearCarryHandle();
        BuildSideLeg();
        BuildInternalLidCatch();
        BuildFourBackFeet();

        RegisterExistingParts();
        ConfigurePlacements();

        if (!cad.Application.CloseAllDocuments(true))
        {
            throw new InvalidOperationException("SOLIDWORKS would not close the already-saved project documents.");
        }

        cad.Application.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY);
        BuildVisibleOpenAssembly();
        WriteReport();
    }

    private void BuildBroadBackV03()
    {
        ModelDoc2 document = cad.NewPart("BackPanel_V03_VESAOnly");
        Body2 body = cad.Box(0, 0, bodyDepth - shellThickness, bodyWidth, bodyHeight, shellThickness);
        foreach (double signX in Signs())
        {
            foreach (double signY in Signs())
            {
                body = HoleZ(body, signX * 50, signY * 50, 4.5,
                    bodyDepth - shellThickness, shellThickness, "VESA 100 M4 clearance");
            }
        }

        cad.AddBody(document, body, "Unperforated broad back with only central VESA 100 holes");
        cad.Property(document, "Reference rule", "No audio, MIDI, USB, power or ventilation apertures on broad back");
        Finish(document, "BackPanel_V03_VESAOnly", SheetMaterial, Graphite, true);
    }

    private void BuildSideFrameV03()
    {
        ModelDoc2 document = cad.NewPart("SideFrame_V03_RecessedLeg");
        Body2 body = cad.Box(0, 0, 0, sideThickness, bodyHeight, bodyDepth - shellThickness);

        foreach (double railY in RailPositions())
        {
            body = HoleX(body, railY, 6.0, 3.4, "M3 rail end screw");
        }

        body = cad.Cut(body,
            cad.Box(0, -55, 42, sideThickness + 0.8, 164, 22),
            "Intellijel-style recessed folding-leg pocket in short side panel");

        foreach (double catchY in new double[] { -150, 150 })
        {
            body = HoleX(body, catchY, 55, 12.2, "internal cover latch catch");
        }

        cad.AddBody(document, body, "Common left or right side frame with hidden leg and cover catches");
        cad.Property(document, "Reference rule", "Two-position leg recessed in each short side panel");
        Finish(document, "SideFrame_V03_RecessedLeg", FrameMaterial, Silver, true);
    }

    private void BuildRearIoEdge()
    {
        ModelDoc2 document = cad.NewPart("RearEdge_V03_IO_Handle_Power");
        double width = bodyWidth - 2 * sideThickness;
        Body2 body = cad.Box(0, 0, 0, width, shellThickness, bodyDepth - shellThickness);

        body = EdgeWindow(body, -164, 55, 180, 60, "eight-TRS audio cassette opening");
        body = EdgeWindow(body, 139, 55, 132, 60, "MIDI and USB-C cassette opening");
        body = EdgeWindow(body, 239, 55, 34, 60, "future power blank cassette opening");

        foreach (double sign in Signs())
        {
            body = cad.Cut(body, cad.Box(sign * 265, 0, 38, 5, shellThickness + 0.8, 34),
                "case joiner slot copied from reference edge layout");
        }

        body = AddEdgeCassetteMounts(body, -164, 94, 20, 90, "audio cassette M3");
        body = AddEdgeCassetteMounts(body, 139, 69, 20, 90, "MIDI USB cassette M3");
        body = AddEdgeCassetteMounts(body, 239, 20, 20, 90, "power blank cassette M3");

        foreach (double x in new double[] { -55, 55 })
        {
            body = HoleY(body, x, 50, 5.2, "carry handle M5 lower fastener");
            body = HoleY(body, x, 61, 5.2, "carry handle M5 upper fastener");
        }

        cad.AddBody(document, body,
            "Rear-facing narrow edge: audio left, carry handle centre, MIDI USB and power right");
        cad.Property(document, "Reference layout", "Intellijel Gen-2: AUDIO I/O | HANDLE | MIDI USB POWER");
        Finish(document, "RearEdge_V03_IO_Handle_Power", SheetMaterial, Silver, true);
    }

    private void BuildLowerVentEdge()
    {
        ModelDoc2 document = cad.NewPart("LowerEdge_V03_HiddenVent");
        double width = bodyWidth - 2 * sideThickness;
        Body2 body = cad.Box(0, 0, 0, width, shellThickness, bodyDepth - shellThickness);

        foreach (double groupX in new double[] { -180, 180 })
        {
            foreach (double xOffset in new double[] { -45, -15, 15, 45 })
            {
                foreach (double z in new double[] { 47, 63 })
                {
                    body = cad.Cut(body, cad.Box(groupX + xOffset, 0, z - 2,
                        22, shellThickness + 0.8, 4), "hidden lower-edge passive ventilation slot");
                }
            }
        }

        foreach (double sign in Signs())
        {
            body = cad.Cut(body, cad.Box(sign * 265, 0, 38, 5, shellThickness + 0.8, 34),
                "lower case-joiner slot");
        }

        cad.AddBody(document, body, "Blank lower edge with two hidden passive-vent groups");
        cad.Property(document, "Reference variance", "Vent slots added on hidden lower edge; reference cases keep broad back clear");
        Finish(document, "LowerEdge_V03_HiddenVent", SheetMaterial, Silver, true);
    }

    private void BuildAudioEdgeCassette()
    {
        ModelDoc2 document = cad.NewPart("RearEdgeAudio_V03_8xTRS635");
        Body2 body = cad.Box(0, 0, 0, 200, shellThickness, 80);
        foreach (double x in new double[] { -77, -55, -33, -11, 11, 33, 55, 77 })
        {
            body = HoleY(body, x, 40, 11.2, "single-row 6.35 mm TRS connector");
        }

        body = EdgePlateMounts(body, 94, 5, 75);
        cad.AddBody(document, body, "Eight 6.35 mm TRS apertures in one horizontal row");
        cad.Property(document, "Electrical boundary", "Mechanical reserve only; audio function requires a compatible I/O board");
        Finish(document, "RearEdgeAudio_V03_8xTRS635", SheetMaterial, ConnectorBlue, true);
    }

    private void BuildMidiUsbEdgeCassette()
    {
        ModelDoc2 document = cad.NewPart("RearEdgeMidiUsb_V03_3xDIN_USB_C");
        Body2 body = cad.Box(0, 0, 0, 150, shellThickness, 80);

        foreach (double x in new double[] { -52, -18, 16 })
        {
            body = HoleY(body, x, 40, 15, "DIN-5 MIDI IN OUT or THRU");
            body = HoleY(body, x - 11.1, 40, 3.2, "DIN-5 mounting ear");
            body = HoleY(body, x + 11.1, 40, 3.2, "DIN-5 mounting ear");
        }

        body = cad.Cut(body, cad.Box(52, 0, 37, 12, shellThickness + 0.8, 6),
            "USB-C mechanical opening");
        body = HoleY(body, 42, 40, 2.4, "USB-C carrier fastener");
        body = HoleY(body, 62, 40, 2.4, "USB-C carrier fastener");
        body = EdgePlateMounts(body, 69, 5, 75);

        cad.AddBody(document, body, "Three full-size DIN-5 MIDI and one USB-C edge connector aperture");
        cad.Property(document, "Electrical boundary", "Mechanical reserve only; no MIDI transceiver or USB device circuitry included");
        Finish(document, "RearEdgeMidiUsb_V03_3xDIN_USB_C", SheetMaterial, ConnectorBlue, true);
    }

    private void BuildPowerBlankEdgeCassette()
    {
        ModelDoc2 document = cad.NewPart("RearEdgePowerBlank_V03");
        Body2 body = cad.Box(0, 0, 0, 50, shellThickness, 80);
        body = EdgePlateMounts(body, 20, 5, 75);
        cad.AddBody(document, body, "Undrilled narrow-edge future power cassette");
        cad.Property(document, "Power boundary", "No power topology, switch, inlet or fixing pattern frozen");
        Finish(document, "RearEdgePowerBlank_V03", SheetMaterial, Graphite, true);
    }

    private void BuildRearCarryHandle()
    {
        ModelDoc2 document = cad.NewPart("RearCarryHandle_V03_Reference");
        cad.AddBody(document, cad.Box(-55, 0, 0, 16, 10, 14), "Left handle mount");
        cad.AddBody(document, cad.Box(55, 0, 0, 16, 10, 14), "Right handle mount");
        cad.AddBody(document, cad.Box(0, 0, 6, 126, 8, 9), "Central 126 mm carry grip with 1 mm clearance to each I/O cassette");
        cad.Property(document, "Reference layout", "Centred between audio group and MIDI USB power group");
        cad.Property(document, "Hardware boundary", "Envelope only; final purchased handle requires supplier drawing");
        Finish(document, "RearCarryHandle_V03_Reference", FrameMaterial, DarkHardware, false);
    }

    private void BuildSideLeg()
    {
        ModelDoc2 document = cad.NewPart("SideRecessedLeg_V03_TwoPosition");
        cad.AddBody(document, cad.Box(0, 0, 0, 6, 150, 12), "Folded leg arm inside side-panel pocket");
        cad.AddBody(document, cad.Cylinder(-3, 75, 6, 1, 0, 0, 16, 6), "Leg pivot boss");
        cad.Property(document, "Reference layout", "One spring-locked leg hidden in each short side panel");
        cad.Property(document, "Angle positions", "15 degrees and 30 degrees; detent geometry pending hardware selection");
        Finish(document, "SideRecessedLeg_V03_TwoPosition", FrameMaterial, DarkHardware, false);
    }

    private void BuildInternalLidCatch()
    {
        ModelDoc2 document = cad.NewPart("InternalLidCatch_V03");
        cad.AddBody(document, cad.Cylinder(-2, 0, 0, 1, 0, 0, 12, 4),
            "Flush side-panel cover catch envelope");
        cad.Property(document, "Reference layout", "Four internal side catches; no protruding front latch blocks");
        Finish(document, "InternalLidCatch_V03", FrameMaterial, DarkHardware, false);
    }

    private void BuildFourBackFeet()
    {
        ModelDoc2 document = cad.NewPart("FourBackFeet_V03");
        foreach (double x in new double[] { -245, 245 })
        {
            foreach (double y in new double[] { -185, 185 })
            {
                cad.AddBody(document, cad.Cylinder(x, y, 0, 0, 0, 1, 12, 6),
                    "Back-panel rubber foot envelope");
            }
        }

        cad.Property(document, "Reference layout", "Four feet at broad-back corners; central VESA zone remains clear");
        Finish(document, "FourBackFeet_V03", FrameMaterial, DarkHardware, false);
    }

    private void RegisterExistingParts()
    {
        foreach (string stem in new string[]
        {
            "Rail_104HP_104xM3",
            "ThreadStrip_104HP_M3Pilot",
            "RailEndBlock_M3",
            "VesaReinforcement_100x100_M4",
            "RearCrossBeam_6061",
            "VesaStile_6061",
            "VesaBridge_6061",
            "DeepTravelLid_70mmClearance",
            "FitGauge_104HP_3U",
            "ReservedPowerBus_500x85x20",
            "ReservedPowerSupply_210x90x45"
        })
        {
            string path = Path.Combine(cad.PartsDirectory, stem + ".SLDPRT");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Required prior-stage native part missing", path);
            }

            parts.Add(stem, path);
        }
    }

    private void ConfigurePlacements()
    {
        placements.Add(new Placement("BackPanel_V03_VESAOnly", "Broad back - VESA only", 0, 0, 0));
        placements.Add(new Placement("SideFrame_V03_RecessedLeg", "Left side frame with hidden leg pocket", -272.5, 0, 0));
        placements.Add(new Placement("SideFrame_V03_RecessedLeg", "Right side frame with hidden leg pocket", 272.5, 0, 0));
        placements.Add(new Placement("RearEdge_V03_IO_Handle_Power", "Rear edge integrated I O and handle strip", 0, 209, 0));
        placements.Add(new Placement("LowerEdge_V03_HiddenVent", "Lower edge passive ventilation strip", 0, -209, 0));

        int railNumber = 1;
        double blockWidth = (bodyWidth - 2 * sideThickness - railLength) / 2.0;
        double blockX = railLength / 2.0 + blockWidth / 2.0;
        foreach (double y in RailPositions())
        {
            placements.Add(new Placement("Rail_104HP_104xM3", "104HP rail " + railNumber, 0, y, 0));
            placements.Add(new Placement("ThreadStrip_104HP_M3Pilot", "Continuous M3 strip " + railNumber,
                0, y, cad.N("rail", "profile_depth") - 2.1));
            placements.Add(new Placement("RailEndBlock_M3", "Left rail end block " + railNumber, -blockX, y, 0));
            placements.Add(new Placement("RailEndBlock_M3", "Right rail end block " + railNumber, blockX, y, 0));
            railNumber++;
        }

        placements.Add(new Placement("VesaReinforcement_100x100_M4", "VESA load spreader", 0, 0, 105));
        placements.Add(new Placement("RearCrossBeam_6061", "Upper VESA crossbeam", 0, 155, 99));
        placements.Add(new Placement("RearCrossBeam_6061", "Lower VESA crossbeam", 0, -155, 99));
        placements.Add(new Placement("VesaStile_6061", "Left VESA load stile", -115, 0, 93));
        placements.Add(new Placement("VesaStile_6061", "Right VESA load stile", 115, 0, 93));
        placements.Add(new Placement("VesaBridge_6061", "Upper local VESA bridge", 0, 70, 99));
        placements.Add(new Placement("VesaBridge_6061", "Lower local VESA bridge", 0, -70, 99));

        placements.Add(new Placement("RearEdgeAudio_V03_8xTRS635", "Rear edge eight TRS audio cassette", -164, 211, 15));
        placements.Add(new Placement("RearEdgeMidiUsb_V03_3xDIN_USB_C", "Rear edge MIDI and USB-C cassette", 139, 211, 15));
        placements.Add(new Placement("RearEdgePowerBlank_V03", "Rear edge undrilled power cassette", 239, 211, 15));
        placements.Add(new Placement("RearCarryHandle_V03_Reference", "Central rear-edge carry handle", 0, 215, 45));

        placements.Add(new Placement("SideRecessedLeg_V03_TwoPosition", "Left folded two-position leg", -271, -56, 46));
        placements.Add(new Placement("SideRecessedLeg_V03_TwoPosition", "Right folded two-position leg", 271, -56, 46));
        foreach (double x in new double[] { -272, 272 })
        {
            foreach (double y in new double[] { -150, 150 })
            {
                placements.Add(new Placement("InternalLidCatch_V03", "Flush internal cover catch", x, y, 55));
            }
        }

        placements.Add(new Placement("FourBackFeet_V03", "Four broad-back anti-slip feet", 0, 0, 110));
    }

    private void BuildVisibleOpenAssembly()
    {
        ModelDoc2 assembly = cad.NewAssembly("Rack4Modules_OpenCase_V03");
        cad.Application.Visible = true;
        cad.Application.UserControl = true;
        cad.Application.FrameState = (int)swWindowState_e.swWindowMaximized;
        cad.Show(assembly);
        cad.Log("VISIBLE_ASSEMBLY=Rack4Modules_OpenCase_V03");

        int index = 0;
        foreach (Placement placement in placements)
        {
            cad.AddComponent(assembly, parts[placement.Part], placement.Label,
                placement.X, placement.Y, placement.Z);
            index++;
            cad.Log("V03_COMPONENT=" + index + "/" + placements.Count + ":" + placement.Label);
            if (index <= 5 || index % 6 == 0 || index == placements.Count)
            {
                cad.Show(assembly);
            }

            Thread.Sleep(90);
        }

        cad.Property(assembly, "Reference layout", "Intellijel Gen-2 primary; Befaco 7U secondary");
        cad.Property(assembly, "Rear edge order", "8xTRS | carry handle | 3xDIN MIDI + USB-C | blank power plate");
        cad.Property(assembly, "Broad back", "VESA 100 only; no signal or power connector openings");
        cad.Property(assembly, "Format", "3 x 3U x 104HP; no 1U row");
        cad.SaveAssembly(assembly, "Rack4Modules_OpenCase_V03", true);
        cad.Show(assembly);
        cad.Application.FrameState = (int)swWindowState_e.swWindowMaximized;
        cad.Log("V03_ASSEMBLY_COMPLETE=true");
        cad.Log("V03_COMPONENT_COUNT=" + placements.Count);
    }

    private void WriteReport()
    {
        StringBuilder text = new StringBuilder();
        text.AppendLine("# Rack4Modules V0.3 edge-layout build");
        text.AppendLine();
        text.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        text.AppendLine();
        text.AppendLine("- Primary layout reference: Intellijel 7U Performance Case Gen-2.");
        text.AppendLine("- Secondary layout reference: Befaco 7U Case.");
        text.AppendLine("- The term rear panel means the rear-facing narrow perimeter edge, not the broad VESA back plane.");
        text.AppendLine("- Rear edge order: eight 6.35 mm TRS jacks, centred carry handle, three DIN-5 MIDI ports and USB-C, separate blank power cassette.");
        text.AppendLine("- Folding legs and four internal lid catches are in the two short side panels.");
        text.AppendLine("- Broad back retains only central VESA 100 and four corner feet.");
        text.AppendLine("- Passive ventilation is moved to the normally hidden lower perimeter edge.");
        text.AppendLine("- All external connectors remain mechanical provisions; electrical functions are not implemented.");
        File.WriteAllText(Path.Combine(cad.ReportsDirectory, "layout-v03-build.md"), text.ToString(), new UTF8Encoding(false));
    }

    private void VerifyExistingSessionIsProjectOnly()
    {
        ModelDoc2 current = cad.Application.GetFirstDocument() as ModelDoc2;
        while (current != null)
        {
            string path = current.GetPathName();
            if (current.GetSaveFlag())
            {
                throw new InvalidOperationException("Refusing to close an unsaved SOLIDWORKS document: " + current.GetTitle());
            }

            if (!string.IsNullOrWhiteSpace(path) && !path.StartsWith(cad.Root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Refusing to alter a SOLIDWORKS session containing non-project data: " + path);
            }

            current = current.GetNext() as ModelDoc2;
        }
    }

    private void Finish(ModelDoc2 document, string stem, string material, double[] appearance, bool exportStep)
    {
        cad.ApplyMaterial(document, material, appearance);
        cad.Property(document, "Project", "Rack4Modules V0.3 reference-edge layout");
        parts.Add(stem, cad.SavePart(document, stem, exportStep));
        string title = document.GetTitle();
        cad.Application.CloseDoc(title);
        cad.Log("V03_PART_READY=" + stem);
    }

    private Body2 EdgeWindow(Body2 body, double x, double z, double width, double height, string description)
    {
        return cad.Cut(body, cad.Box(x, 0, z - height / 2.0,
            width, shellThickness + 0.8, height), description);
    }

    private Body2 EdgePlateMounts(Body2 body, double x, double lowerZ, double upperZ)
    {
        foreach (double sign in Signs())
        {
            body = HoleY(body, sign * x, lowerZ, 3.4, "edge cassette M3");
            body = HoleY(body, sign * x, upperZ, 3.4, "edge cassette M3");
        }

        return body;
    }

    private Body2 AddEdgeCassetteMounts(Body2 body, double centreX,
        double horizontal, double lowerZ, double upperZ, string description)
    {
        foreach (double sign in Signs())
        {
            body = HoleY(body, centreX + sign * horizontal, lowerZ, 3.4, description);
            body = HoleY(body, centreX + sign * horizontal, upperZ, 3.4, description);
        }

        return body;
    }

    private Body2 HoleZ(Body2 body, double x, double y, double diameter,
        double startZ, double thickness, string description)
    {
        return cad.Cut(body, cad.Cylinder(x, y, startZ - 0.3, 0, 0, 1,
            diameter, thickness + 0.6), description);
    }

    private Body2 HoleY(Body2 body, double x, double z, double diameter, string description)
    {
        return cad.Cut(body, cad.Cylinder(x, -shellThickness / 2.0 - 0.3, z,
            0, 1, 0, diameter, shellThickness + 0.6), description);
    }

    private Body2 HoleX(Body2 body, double y, double z, double diameter, string description)
    {
        return cad.Cut(body, cad.Cylinder(-sideThickness / 2.0 - 0.3, y, z,
            1, 0, 0, diameter, sideThickness + 0.6), description);
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
