using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal static class BuildRackLayoutV03BackgroundAssemblies
{
    [STAThread]
    private static int Main(string[] arguments)
    {
        try
        {
            string root = arguments.Length == 0
                ? Directory.GetCurrentDirectory()
                : Path.GetFullPath(arguments[0]);
            string openAssembly = Path.Combine(root, "cad", "assemblies", "Rack4Modules_OpenCase_V03.SLDASM");
            if (!File.Exists(openAssembly))
            {
                throw new FileNotFoundException("The completed V0.3 open-case assembly must exist first.", openAssembly);
            }

            RackCadSession session = new RackCadSession(root);
            V03BackgroundAssemblyBuilder builder = new V03BackgroundAssemblyBuilder(session, openAssembly);
            try
            {
                builder.Build();
            }
            finally
            {
                builder.RestoreOpenAssembly();
            }

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V03_BACKGROUND_ASSEMBLIES_FAILED=" + exception);
            return 1;
        }
    }
}

internal sealed class V03BackgroundAssemblyBuilder
{
    private const string OpenAssemblyStem = "Rack4Modules_OpenCase_V03";
    private const string TransportAssemblyStem = "Rack4Modules_TransportClosed_V03";
    private const string ClearanceAssemblyStem = "Rack4Modules_ClearanceCheck_V03";
    private const string ModuleEnvelopeStem = "ModuleDepthEnvelope_85mm_V03";
    private const string CorrectedHandleStem = "RearCarryHandle_V03_ClearanceFit";
    private const string OriginalHandleStem = "RearCarryHandle_V03_Reference";
    private const double FoldedLegThickness = 6.0;
    private const double LidCatchAxialLength = 4.0;

    private readonly RackCadSession cad;
    private readonly string openAssemblyPath;
    private readonly Dictionary<string, string> parts;
    private readonly List<Placement> placements;
    private readonly double bodyWidth;
    private readonly double bodyHeight;
    private readonly double sideThickness;
    private readonly double shellThickness;
    private readonly double railLength;
    private readonly double rowPitch;
    private readonly double railSpacing;
    private string handleStem;

    internal V03BackgroundAssemblyBuilder(RackCadSession session, string openAssembly)
    {
        if (session == null)
        {
            throw new ArgumentNullException("session");
        }

        if (string.IsNullOrWhiteSpace(openAssembly))
        {
            throw new ArgumentException("The visible V0.3 assembly path is required.", "openAssembly");
        }

        cad = session;
        openAssemblyPath = Path.GetFullPath(openAssembly);
        parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        placements = new List<Placement>();
        bodyWidth = cad.N("enclosure", "outer_width");
        bodyHeight = cad.N("enclosure", "outer_height");
        sideThickness = cad.N("enclosure", "side_frame_thickness");
        shellThickness = cad.N("enclosure", "body_thickness");
        railLength = cad.N("rail", "length");
        rowPitch = cad.N("eurorack", "row_pitch");
        railSpacing = cad.N("eurorack", "mounting_hole_vertical_spacing");
    }

    internal void Build()
    {
        RegisterExistingParts();
        ConfigurePlacements();
        EnsureModuleDepthEnvelope();

        cad.Log("V03_BACKGROUND_REFERENCE=Intellijel Gen-2 primary; Befaco 7U secondary");
        cad.Log("V03_BACKGROUND_FOLDED_LEGS=x+/-271,y-56; side catches=x+/-272");
        cad.Log("V03_BACKGROUND_MODULE_DEPTH=normal 85 mm; PSU zone 60 mm; busboard zone 73 mm");

        BuildAssembly(TransportAssemblyStem, true, false);
        BuildAssembly(ClearanceAssemblyStem, false, true);

        cad.Log("V03_BACKGROUND_ASSEMBLIES_COMPLETE=true");
    }

    internal void RestoreOpenAssembly()
    {
        ModelDoc2 document = cad.Application.GetOpenDocumentByName(openAssemblyPath) as ModelDoc2;
        if (document == null)
        {
            int errors = 0;
            int warnings = 0;
            document = cad.Application.OpenDoc6(
                openAssemblyPath,
                (int)swDocumentTypes_e.swDocASSEMBLY,
                (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                string.Empty,
                ref errors,
                ref warnings) as ModelDoc2;

            if (document == null || errors != 0)
            {
                throw new InvalidOperationException(
                    "The visible V0.3 open case could not be restored; errors=" +
                    errors.ToString(CultureInfo.InvariantCulture) +
                    ", warnings=" + warnings.ToString(CultureInfo.InvariantCulture));
            }
        }

        cad.Application.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY);
        cad.Application.Visible = true;
        cad.Application.UserControl = true;
        cad.Show(document);
        cad.Log("V03_RESTORED_VISIBLE_ASSEMBLY=" + OpenAssemblyStem);
    }

    private void RegisterExistingParts()
    {
        foreach (string stem in new string[]
        {
            "BackPanel_V03_VESAOnly",
            "SideFrame_V03_RecessedLeg",
            "RearEdge_V03_IO_Handle_Power",
            "LowerEdge_V03_HiddenVent",
            "Rail_104HP_104xM3",
            "ThreadStrip_104HP_M3Pilot",
            "RailEndBlock_M3",
            "VesaReinforcement_100x100_M4",
            "RearCrossBeam_6061",
            "VesaStile_6061",
            "VesaBridge_6061",
            "RearEdgeAudio_V03_8xTRS635",
            "RearEdgeMidiUsb_V03_3xDIN_USB_C",
            "RearEdgePowerBlank_V03",
            "SideRecessedLeg_V03_TwoPosition",
            "InternalLidCatch_V03",
            "FourBackFeet_V03",
            "DeepTravelLid_70mmClearance",
            "FitGauge_104HP_3U",
            "ReservedPowerBus_500x85x20",
            "ReservedPowerSupply_210x90x45"
        })
        {
            RegisterExistingPart(stem);
        }

        string correctedHandlePath = PartPath(CorrectedHandleStem);
        if (File.Exists(correctedHandlePath))
        {
            handleStem = CorrectedHandleStem;
            parts.Add(handleStem, correctedHandlePath);
        }
        else
        {
            handleStem = OriginalHandleStem;
            RegisterExistingPart(handleStem);
            cad.Log("WARNING: Corrected 126 mm handle is unavailable; the original 132 mm grip overlaps each neighbouring I/O cassette by approximately 2 mm.");
        }
    }

    private void RegisterExistingPart(string stem)
    {
        string path = PartPath(stem);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("A required existing V0.3 native part is missing.", path);
        }

        parts.Add(stem, path);
    }

    private string PartPath(string stem)
    {
        return Path.Combine(cad.PartsDirectory, stem + ".SLDPRT");
    }

    private void EnsureModuleDepthEnvelope()
    {
        string existing = PartPath(ModuleEnvelopeStem);
        if (File.Exists(existing))
        {
            parts.Add(ModuleEnvelopeStem, existing);
            cad.Log("V03_EXISTING_MODULE_ENVELOPE=" + existing);
            return;
        }

        double railDepth = cad.N("rail", "profile_depth");
        double nominalModuleDepth = cad.N("enclosure", "normal_module_depth");
        double moduleBodyDepth = nominalModuleDepth - railDepth;
        double moduleBodyHeight = railSpacing - cad.N("rail", "profile_height") - 0.5;
        double moduleBodyWidth = railLength - 0.32;
        if (moduleBodyDepth <= 0.0 || moduleBodyHeight <= 0.0 || moduleBodyWidth <= 0.0)
        {
            throw new InvalidDataException("The normal module depth envelope must fit behind the rail profile.");
        }

        ModelDoc2 document = cad.NewPart(ModuleEnvelopeStem);
        cad.AddBody(
            document,
            cad.Box(0, 0, railDepth, moduleBodyWidth, moduleBodyHeight, moduleBodyDepth),
            "Nominal 85 mm module-depth keepout behind one 104HP Eurorack row");
        document.MaterialPropertyValues = new double[]
        {
            0.18, 0.68, 0.88, 0.25, 0.75, 0.25, 0.35, 0.72, 0.0
        };
        cad.Property(document, "Project", "Rack4Modules V0.3 mechanical clearance validation");
        cad.Property(document, "Purpose", "Non-production 104HP module-body envelope; excluded from assembly BOM");
        cad.Property(document, "Depth reference", "85 mm from the module mounting face; solid starts behind the 12 mm rail");
        cad.Property(document, "Power overlap boundary", "Central PSU zone allows 60 mm; distributed busboard zone allows 73 mm");
        parts.Add(ModuleEnvelopeStem, cad.SavePart(document, ModuleEnvelopeStem, false));
        cad.Application.CloseDoc(document.GetTitle());
        cad.Log("V03_MODULE_ENVELOPE_CREATED=" + ModuleEnvelopeStem);
    }

    private void ConfigurePlacements()
    {
        double sideCentre = bodyWidth / 2.0 - sideThickness / 2.0;
        double rearEdgeCentre = bodyHeight / 2.0 - shellThickness / 2.0;
        double foldedLegCentre = bodyWidth / 2.0 - FoldedLegThickness / 2.0;
        double lidCatchCentre = bodyWidth / 2.0 - LidCatchAxialLength / 2.0;
        if (foldedLegCentre + FoldedLegThickness / 2.0 > bodyWidth / 2.0 + 0.000001 ||
            lidCatchCentre + LidCatchAxialLength / 2.0 > bodyWidth / 2.0 + 0.000001)
        {
            throw new InvalidOperationException("The folded legs and internal lid catches must remain inside the 548 mm case width.");
        }

        placements.Add(new Placement("BackPanel_V03_VESAOnly", "Broad back - VESA only", 0, 0, 0));
        placements.Add(new Placement("SideFrame_V03_RecessedLeg", "Left side frame with hidden leg pocket", -sideCentre, 0, 0));
        placements.Add(new Placement("SideFrame_V03_RecessedLeg", "Right side frame with hidden leg pocket", sideCentre, 0, 0));
        placements.Add(new Placement("RearEdge_V03_IO_Handle_Power", "Rear edge integrated I O and handle strip", 0, rearEdgeCentre, 0));
        placements.Add(new Placement("LowerEdge_V03_HiddenVent", "Lower edge passive ventilation strip", 0, -rearEdgeCentre, 0));

        int railNumber = 1;
        double blockWidth = (bodyWidth - 2.0 * sideThickness - railLength) / 2.0;
        double blockX = railLength / 2.0 + blockWidth / 2.0;
        foreach (double rowCentre in new double[] { -rowPitch, 0, rowPitch })
        {
            foreach (double railY in new double[]
            {
                rowCentre - railSpacing / 2.0,
                rowCentre + railSpacing / 2.0
            })
            {
                placements.Add(new Placement("Rail_104HP_104xM3", "104HP rail " + railNumber, 0, railY, 0));
                placements.Add(new Placement(
                    "ThreadStrip_104HP_M3Pilot",
                    "Continuous M3 strip " + railNumber,
                    0,
                    railY,
                    cad.N("rail", "profile_depth") - 2.1));
                placements.Add(new Placement("RailEndBlock_M3", "Left rail end block " + railNumber, -blockX, railY, 0));
                placements.Add(new Placement("RailEndBlock_M3", "Right rail end block " + railNumber, blockX, railY, 0));
                railNumber++;
            }
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
        placements.Add(new Placement(handleStem, "Central rear-edge carry handle", 0, 215, 45));

        placements.Add(new Placement("SideRecessedLeg_V03_TwoPosition", "Left folded two-position leg", -foldedLegCentre, -56, 46));
        placements.Add(new Placement("SideRecessedLeg_V03_TwoPosition", "Right folded two-position leg", foldedLegCentre, -56, 46));
        foreach (double catchX in new double[] { -lidCatchCentre, lidCatchCentre })
        {
            foreach (double catchY in new double[] { -150, 150 })
            {
                placements.Add(new Placement("InternalLidCatch_V03", "Flush internal cover catch", catchX, catchY, 55));
            }
        }

        placements.Add(new Placement("FourBackFeet_V03", "Four broad-back anti-slip feet", 0, 0, 110));

        if (placements.Count != 47)
        {
            throw new InvalidOperationException(
                "The V0.3 open-case layout must contain exactly 47 component instances; actual=" +
                placements.Count.ToString(CultureInfo.InvariantCulture));
        }
    }

    private void BuildAssembly(string stem, bool addTravelLid, bool addClearanceGauges)
    {
        ModelDoc2 document = cad.NewAssembly(stem);
        int componentCount = 0;
        foreach (Placement placement in placements)
        {
            cad.AddComponent(document, parts[placement.Part], placement.Label, placement.X, placement.Y, placement.Z);
            componentCount++;
            if (componentCount % 12 == 0 || componentCount == placements.Count)
            {
                cad.Log(stem + "_BASE_COMPONENTS=" + componentCount.ToString(CultureInfo.InvariantCulture));
            }
        }

        if (addTravelLid)
        {
            cad.AddComponent(
                document,
                parts["DeepTravelLid_70mmClearance"],
                "Removable 70 mm patched-performance travel lid",
                0,
                0,
                0);
            componentCount++;
        }

        if (addClearanceGauges)
        {
            foreach (double rowCentre in new double[] { -rowPitch, 0, rowPitch })
            {
                AddReferenceComponent(document, "FitGauge_104HP_3U", "104HP standard 3U panel fit gauge", 0, rowCentre, 0);
                AddReferenceComponent(document, ModuleEnvelopeStem, "85 mm nominal module-depth keepout", 0, rowCentre, 0);
                componentCount += 2;
            }

            AddReferenceComponent(
                document,
                "ReservedPowerBus_500x85x20",
                "Distributed busboard keepout; local module depth 73 mm",
                0,
                -105,
                73);
            AddReferenceComponent(
                document,
                "ReservedPowerSupply_210x90x45",
                "Undecided central PSU keepout; local module depth 60 mm",
                0,
                0,
                60);
            componentCount += 2;
        }

        cad.Property(document, "Reference layout", "Intellijel Gen-2 primary; Befaco 7U secondary");
        cad.Property(document, "Rear edge order", "8xTRS | carry handle | 3xDIN MIDI + USB-C | blank power plate");
        cad.Property(document, "Broad back", "VESA 100 only; no signal or power connector openings");
        cad.Property(document, "Format", "3 x 3U x 104HP; no 1U row");
        cad.Property(document, "Folded side hardware", "Legs x +/-271, y -56; cover catches x +/-272; body width 548 mm");
        cad.Property(document, "Electrical boundary", "All connectors and power envelopes are mechanical provisions only");
        if (addTravelLid)
        {
            cad.Property(document, "Travel lid", "Existing deep lid; 70 mm patched-cable clearance and 12 mm overlap");
        }

        if (addClearanceGauges)
        {
            cad.Property(document, "Normal module depth", "85 mm from the module mounting face");
            cad.Property(document, "Central PSU local module depth", "60 mm; a nominal 85 mm module overlaps the power keepout by 25 mm");
            cad.Property(document, "Distributed bus local module depth", "73 mm; a nominal 85 mm module overlaps the bus keepout by 12 mm");
            cad.Property(document, "Clearance warning", "Module/power envelope clashes are intentional evidence of unresolved electrical packaging");
        }

        cad.SaveAssembly(document, stem, true);
        cad.Log(stem + "_COMPONENT_COUNT=" + componentCount.ToString(CultureInfo.InvariantCulture));
        cad.Application.CloseDoc(document.GetTitle());
    }

    private void AddReferenceComponent(
        ModelDoc2 assembly,
        string stem,
        string description,
        double x,
        double y,
        double z)
    {
        Component2 component = cad.AddComponent(assembly, parts[stem], description, x, y, z);
        component.ExcludeFromBOM = true;
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
