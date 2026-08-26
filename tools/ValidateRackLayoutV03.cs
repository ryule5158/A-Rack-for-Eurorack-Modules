using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Read-only SOLIDWORKS inspection. The sole intentional project write is the
// validation report; native models and imported STEP documents are never saved.
internal static class ValidateRackLayoutV03
{
    [STAThread]
    private static int Main(string[] arguments)
    {
        RackLayoutV03Validator validator = null;

        try
        {
            if (arguments.Length != 1)
            {
                throw new ArgumentException("Pass exactly one Rack4Modules project root.");
            }

            validator = new RackLayoutV03Validator(Path.GetFullPath(arguments[0]));
            validator.Run();
            return validator.FailureCount == 0 ? 0 : 2;
        }
        catch (Exception exception)
        {
            if (validator != null)
            {
                validator.RecordFatal(exception);
                validator.WriteReport();
            }

            Console.Error.WriteLine("V03_VALIDATION_FAILED=" + exception);
            return 1;
        }
        finally
        {
            if (validator != null)
            {
                validator.CloseOwnedDocuments();
            }
        }
    }
}

internal sealed class RackLayoutV03Validator
{
    private const double MetresToMillimetres = 1000.0;
    private const double DimensionTolerance = 0.08;
    private const double NegligibleInterferenceVolume = 0.001;
    private const string SheetMaterial = "5052-H32";
    private const string FrameMaterial = "6061-T6 (SS)";
    private const string CorrectedHandle = "RearCarryHandle_V03_ClearanceFit";
    private const string ModuleDepthEnvelope = "ModuleDepthEnvelope_85mm_V03";

    private static readonly string[] RequiredPhysicalParts =
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
        CorrectedHandle,
        "SideRecessedLeg_V03_TwoPosition",
        "InternalLidCatch_V03",
        "FourBackFeet_V03",
        "DeepTravelLid_70mmClearance",
        "FitGauge_104HP_3U"
    };

    private readonly string root;
    private readonly string reportPath;
    private readonly StringBuilder report = new StringBuilder();
    private readonly Dictionary<string, PartSnapshot> partCache =
        new Dictionary<string, PartSnapshot>(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> ownedDocumentTitles = new List<string>();

    private Dictionary<string, object> baseParameters;
    private Dictionary<string, object> revisionParameters;
    private SldWorks application;
    private string originalActiveTitle;
    private int passes;
    private int warnings;
    private int failures;

    internal RackLayoutV03Validator(string projectRoot)
    {
        if (!Directory.Exists(projectRoot))
        {
            throw new DirectoryNotFoundException("Rack4Modules root does not exist: " + projectRoot);
        }

        root = projectRoot;
        reportPath = Path.Combine(root, "reports", "layout-v03-validation.md");
        report.AppendLine("# Rack4Modules V0.3 native SOLIDWORKS validation");
        report.AppendLine();
        report.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        report.AppendLine("Project root: `" + root + "`");
        report.AppendLine();
    }

    internal int FailureCount
    {
        get { return failures; }
    }

    internal void Run()
    {
        LoadDesignParameters();
        ValidateInventory();
        AttachSolidWorks();
        ValidatePhysicalPartInventory();
        ValidateBroadBackAndVesa();
        ValidateRearEdgePanels();
        ValidateRailGeometry();
        ValidatePowerEnvelopeParts();

        ValidateAssemblyStage("open", "Rack4Modules_OpenCase_V03", "Rack4Modules_OpenCase", false, false);
        ValidateAssemblyStage("transport", "Rack4Modules_TransportClosed_V03", "Rack4Modules_TransportClosed", true, false);
        ValidateAssemblyStage("clearance", "Rack4Modules_ClearanceCheck_V03", "Rack4Modules_ClearanceCheck", false, true);

        Section("Engineering and electrical validation boundary");
        Note("The 8 audio apertures, 3 DIN-5 apertures and USB-C opening are mechanical provisions only.");
        Note("No audio signal direction, MIDI transceiver, USB protocol, connector vendor, PCB or electrical operation is validated.");
        Note("The removable power plate has no inlet, switch, connector or power-topology commitment.");
        Note("Central PSU keepout leaves 60 mm local module depth; distributed bus keepout leaves 73 mm. An unrestricted 85 mm depth is not available across those zones.");
        Note("Material assignment and geometry are not load certification, thermal qualification, supplier fit confirmation or physical test evidence.");
        Note("Interference checks separate zero-volume contact, conceptual gauge/hardware overlap, reserved-volume violations and physical solid interference.");

        WriteReport();
        Console.WriteLine("V03_VALIDATION_REPORT=" + reportPath);
        Console.WriteLine("V03_VALIDATION_PASSES=" + passes.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("V03_VALIDATION_WARNINGS=" + warnings.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("V03_VALIDATION_FAILURES=" + failures.ToString(CultureInfo.InvariantCulture));
    }

    internal void RecordFatal(Exception exception)
    {
        Section("Unhandled validation error");
        Fail("Validator completed its requested inspections", exception.GetType().Name + ": " + exception.Message);
    }

    internal void WriteReport()
    {
        StringBuilder output = new StringBuilder(report.ToString());
        output.AppendLine();
        output.AppendLine("## Result");
        output.AppendLine();
        output.AppendLine("- Status: **" + (failures == 0 ? "PASS" : "FAIL") + "**");
        output.AppendLine("- Passed checks: " + passes.ToString(CultureInfo.InvariantCulture));
        output.AppendLine("- Warnings requiring engineering review: " + warnings.ToString(CultureInfo.InvariantCulture));
        output.AppendLine("- Failed checks: " + failures.ToString(CultureInfo.InvariantCulture));
        output.AppendLine("- Native CAD documents were inspected without saving or replacing them.");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
        File.WriteAllText(reportPath, output.ToString(), new UTF8Encoding(false));
    }

    internal void CloseOwnedDocuments()
    {
        if (application == null)
        {
            return;
        }

        for (int index = ownedDocumentTitles.Count - 1; index >= 0; index--)
        {
            try
            {
                application.CloseDoc(ownedDocumentTitles[index]);
            }
            catch (COMException)
            {
                // A temporary import may already have closed with its parent.
            }
        }

        if (!string.IsNullOrEmpty(originalActiveTitle))
        {
            try
            {
                int activationError = 0;
                application.ActivateDoc3(originalActiveTitle, false, 0, ref activationError);
            }
            catch (COMException)
            {
                // Restoring the previous view must not rewrite a native model.
            }
        }
    }

    private void LoadDesignParameters()
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        baseParameters = serializer.Deserialize<Dictionary<string, object>>(
            File.ReadAllText(Path.Combine(root, "design", "parameters.json")));
        revisionParameters = serializer.Deserialize<Dictionary<string, object>>(
            File.ReadAllText(Path.Combine(root, "design", "parameters-v0.3.json")));

        Section("Frozen design inputs");
        Check(Almost(Number(baseParameters, "enclosure", "outer_width"), 548.0),
            "Body external width", Describe(Number(baseParameters, "enclosure", "outer_width"), "548 mm"));
        Check(Almost(Number(revisionParameters, "body", "rear_edge_inner_width"), 542.0),
            "Rear-facing narrow edge clear width", Describe(Number(revisionParameters, "body", "rear_edge_inner_width"), "542 mm"));
        Check(Almost(Number(baseParameters, "eurorack", "rows"), 3.0) &&
              Almost(Number(baseParameters, "eurorack", "hp_per_row"), 104.0),
            "Eurorack format", "3 independent 104HP rows; no 1U row");
        Check(Almost(Number(baseParameters, "rail", "count"), 6.0),
            "Rail count in source parameters", "6 full-width rails");
        Note("Expected open-case component count is derived from ConfigurePlacements: 5 shell + 4 x 6 rail items + 7 VESA items + 4 edge-zone items + 2 legs + 4 catches + 1 four-foot part = 47.");
        Note("Transport adds one lid for 48 components. Clearance adds 3 fit gauges, 3 nominal 85 mm module envelopes and 2 power-reservation envelopes for 55 components.");
    }

    private void ValidateInventory()
    {
        Section("Filesystem inventory; SOLIDWORKS lock files excluded");
        string partsDirectory = Path.Combine(root, "cad", "parts");
        string assembliesDirectory = Path.Combine(root, "cad", "assemblies");
        string exportsDirectory = Path.Combine(root, "exports");

        Check(Directory.Exists(partsDirectory), "Native part directory exists", partsDirectory);
        Check(Directory.Exists(assembliesDirectory), "Native assembly directory exists", assembliesDirectory);
        Check(Directory.Exists(exportsDirectory), "STEP export directory exists", exportsDirectory);

        if (Directory.Exists(partsDirectory))
        {
            Note("Native `.SLDPRT` files excluding `~$` lock files: " +
                CountUnlockedFiles(partsDirectory, ".SLDPRT").ToString(CultureInfo.InvariantCulture));
        }

        if (Directory.Exists(assembliesDirectory))
        {
            Note("Native `.SLDASM` files excluding `~$` lock files: " +
                CountUnlockedFiles(assembliesDirectory, ".SLDASM").ToString(CultureInfo.InvariantCulture));
        }

        if (Directory.Exists(exportsDirectory))
        {
            Note("STEP files excluding `~$` lock files: " +
                CountUnlockedFiles(exportsDirectory, ".STEP").ToString(CultureInfo.InvariantCulture));
        }
    }

    private void AttachSolidWorks()
    {
        string[] programIds = { "SldWorks.Application.33", "SldWorks.Application" };

        foreach (string programId in programIds)
        {
            try
            {
                application = Marshal.GetActiveObject(programId) as SldWorks;
                if (application != null)
                {
                    break;
                }
            }
            catch (COMException)
            {
                // Try the unversioned registered SOLIDWORKS instance next.
            }
        }

        if (application == null)
        {
            throw new InvalidOperationException(
                "Start SOLIDWORKS before validation; this inspector does not start or replace a concurrent CAD session.");
        }

        ModelDoc2 active = application.ActiveDoc as ModelDoc2;
        originalActiveTitle = active == null ? null : active.GetTitle();

        Section("SOLIDWORKS session");
        Pass("Connected to an existing SOLIDWORKS session", application.RevisionNumber());
    }

    private void ValidatePhysicalPartInventory()
    {
        Section("Native parts, solid bodies and assigned physical materials");

        foreach (string stem in RequiredPhysicalParts)
        {
            PartSnapshot part = GetPart(stem);
            if (part == null)
            {
                continue;
            }

            int expectedBodies = stem == "FourBackFeet_V03" ? 4 :
                stem == "DeepTravelLid_70mmClearance" ? 5 :
                stem == "SideRecessedLeg_V03_TwoPosition" ? 2 :
                stem == CorrectedHandle ? 3 : 1;
            Check(part.Bodies.Count == expectedBodies,
                stem + " solid-body count",
                "expected " + expectedBodies + ", actual " + part.Bodies.Count);

            string expectedMaterial = IsSheetPart(stem) ? SheetMaterial : FrameMaterial;
            Check(string.Equals(part.Material, expectedMaterial, StringComparison.OrdinalIgnoreCase),
                stem + " physical material",
                "expected `" + expectedMaterial + "`, actual `" +
                (string.IsNullOrEmpty(part.Material) ? "<unassigned>" : part.Material) + "`");

            if (stem == "DeepTravelLid_70mmClearance")
            {
                AssertSize(part, 552.0, 424.0, 83.5);
                Note("Travel lid mass is the sum of a separate face and four return bodies; it is a folded-sheet concept, not a measured or bend-qualified lid.");
            }
        }
    }

    private void ValidateBroadBackAndVesa()
    {
        Section("Broad back: central VESA 100 only, with separate corner-foot bodies");
        PartSnapshot back = GetPart("BackPanel_V03_VESAOnly");
        PartSnapshot reinforcement = GetPart("VesaReinforcement_100x100_M4");
        PartSnapshot feet = GetPart("FourBackFeet_V03");

        if (back != null)
        {
            AssertSize(back, 548.0, 420.0, 2.0);
            List<CylindricalFace> holes = Cylinders(back, 2);
            Check(holes.Count == 4, "Broad-back through-hole count",
                "exactly four central VESA holes; actual " + holes.Count);
            Check(CountPlanarInnerLoops(back, 2) == 4,
                "Broad back has no additional signal, power, ventilation or rectangular apertures",
                "four inner loops, all belonging to VESA");

            foreach (double x in new double[] { -50.0, 50.0 })
            {
                foreach (double y in new double[] { -50.0, 50.0 })
                {
                    Check(ContainsCylinder(holes, x, y, 4.5, 2),
                        "VESA M4 clearance hole at (" + Format(x) + ", " + Format(y) + ") mm",
                        "4.5 mm hole on a 100 x 100 mm pattern");
                }
            }
        }

        if (reinforcement != null)
        {
            AssertSize(reinforcement, 160.0, 160.0, 3.0);
            List<CylindricalFace> holes = Cylinders(reinforcement, 2);
            foreach (double x in new double[] { -50.0, 50.0 })
            {
                foreach (double y in new double[] { -50.0, 50.0 })
                {
                    Check(ContainsCylinder(holes, x, y, 4.5, 2),
                        "Reinforcement aligns with VESA position (" + Format(x) + ", " + Format(y) + ")",
                        "100 x 100 mm load-spreader hole alignment");
                }
            }
        }

        if (feet != null)
        {
            int matched = 0;
            foreach (Body2 body in feet.Bodies)
            {
                double[] box = body.GetBodyBox() as double[];
                if (box == null || box.Length < 6)
                {
                    continue;
                }

                double x = (box[0] + box[3]) * MetresToMillimetres / 2.0;
                double y = (box[1] + box[4]) * MetresToMillimetres / 2.0;
                if (Almost(Math.Abs(x), 245.0) && Almost(Math.Abs(y), 185.0))
                {
                    matched++;
                }
            }

            Check(matched == 4, "Four feet occupy broad-back corners outside the VESA clear zone",
                "centres x = +/-245 mm, y = +/-185 mm; matched " + matched);
        }
    }

    private void ValidateRearEdgePanels()
    {
        Section("Rear-facing narrow edge: audio, central handle, MIDI/USB-C, power blank");
        ValidateAudioPlate();
        ValidateDigitalPlate();
        ValidatePowerBlank();
        ValidateRearEdgeWindows();
        ValidateLowerVentEdge();

        PartSnapshot handle = GetPart(CorrectedHandle);
        if (handle != null)
        {
            double width = AxisLength(handle.Box, 0);
            Check(Almost(width, 126.0), "Corrected central carry-handle outer/grip width",
                Describe(width, "126 mm, with 110 mm mounting pitch"));

            List<double> mountingCenters = new List<double>();
            foreach (Body2 body in handle.Bodies)
            {
                double[] box = body.GetBodyBox() as double[];
                if (box != null && box.Length >= 6 && AxisLength(box, 0) < 25.0)
                {
                    mountingCenters.Add((box[0] + box[3]) * MetresToMillimetres / 2.0);
                }
            }

            mountingCenters.Sort();
            Check(mountingCenters.Count == 2 && Almost(mountingCenters[0], -55.0) &&
                  Almost(mountingCenters[1], 55.0),
                "Carry-handle mounting centres", "x = -55 and +55 mm; 110 mm pitch");
        }
    }

    private void ValidateAudioPlate()
    {
        PartSnapshot audio = GetPart("RearEdgeAudio_V03_8xTRS635");
        if (audio == null)
        {
            return;
        }

        AssertSize(audio, 200.0, 2.0, 80.0);
        List<CylindricalFace> cylinders = Cylinders(audio, 1);
        List<CylindricalFace> connectors = FilterDiameter(cylinders, 11.2);
        Check(connectors.Count == 8, "Single-row 6.35 mm TRS audio connector count",
            "eight 11.2 mm mechanical openings");

        double[] localX = { -77, -55, -33, -11, 11, 33, 55, 77 };
        foreach (double x in localX)
        {
            Check(ContainsCylinder(connectors, x, 40.0, 11.2, 1),
                "Audio opening global x = " + Format(x - 164.0) + " mm",
                "local x = " + Format(x) + "; global z = 55 mm; 22 mm pitch");
        }

        Check(FilterDiameter(cylinders, 3.4).Count == 4,
            "Audio cassette has four M3 clearance mounting holes", "4 x diameter 3.4 mm");
        Check(CountPlanarInnerLoops(audio, 1) == 12,
            "Audio cassette contains only eight connector holes and four mounting holes", "12 through apertures");
    }

    private void ValidateDigitalPlate()
    {
        PartSnapshot digital = GetPart("RearEdgeMidiUsb_V03_3xDIN_USB_C");
        if (digital == null)
        {
            return;
        }

        AssertSize(digital, 150.0, 2.0, 80.0);
        List<CylindricalFace> cylinders = Cylinders(digital, 1);
        List<CylindricalFace> midi = FilterDiameter(cylinders, 15.0);
        Check(midi.Count == 3, "DIN-5 MIDI opening count", "three diameter-15 mm DIN body openings");

        foreach (double x in new double[] { -52, -18, 16 })
        {
            Check(ContainsCylinder(midi, x, 40.0, 15.0, 1),
                "DIN-5 body opening global x = " + Format(x + 139.0) + " mm",
                "local x = " + Format(x) + "; global z = 55 mm");
        }

        Check(FilterDiameter(cylinders, 3.2).Count == 6,
            "DIN-5 mounting-ear holes", "three pairs, diameter 3.2 mm, 22.2 mm pair spacing");
        Check(FilterDiameter(cylinders, 2.4).Count == 2,
            "Provisional USB-C carrier mounting holes", "2 x diameter 2.4 mm; final vendor drawing not selected");
        Check(FilterDiameter(cylinders, 3.4).Count == 4,
            "Digital cassette M3 mounting-hole count", "4 x diameter 3.4 mm");

        List<RectangularOpening> rectangles = RectangularOpenings(digital);
        Check(rectangles.Count == 1 && Almost(rectangles[0].CenterX, 52.0) &&
              Almost(rectangles[0].CenterZ, 40.0) && Almost(rectangles[0].Width, 12.0) &&
              Almost(rectangles[0].Depth, 6.0),
            "One provisional USB-C mechanical opening",
            "12 x 6 mm, global centre x = 191 mm, z = 55 mm; supplier-specific fit remains unverified");
        Check(CountPlanarInnerLoops(digital, 1) == 16,
            "Digital cassette aperture total", "3 DIN + 6 DIN ears + 1 USB-C rectangle + 2 USB ears + 4 panel fasteners");
    }

    private void ValidatePowerBlank()
    {
        PartSnapshot blank = GetPart("RearEdgePowerBlank_V03");
        if (blank == null)
        {
            return;
        }

        AssertSize(blank, 50.0, 2.0, 80.0);
        List<CylindricalFace> cylinders = Cylinders(blank, 1);
        Check(cylinders.Count == 4 && FilterDiameter(cylinders, 3.4).Count == 4,
            "Independent power plate remains electrically undrilled",
            "four M3 cassette fasteners only; zero inlet, switch or connector holes");
        Check(CountPlanarInnerLoops(blank, 1) == 4 && RectangularOpenings(blank).Count == 0,
            "Power plate has no concealed rectangular or additional circular opening", "4 mounting-only through loops");
    }

    private void ValidateRearEdgeWindows()
    {
        PartSnapshot edge = GetPart("RearEdge_V03_IO_Handle_Power");
        if (edge == null)
        {
            return;
        }

        AssertSize(edge, 542.0, 2.0, 108.0);
        List<RectangularOpening> openings = RectangularOpenings(edge);
        Check(ContainsRectangle(openings, -164.0, 55.0, 180.0, 60.0),
            "Audio support-window alignment", "x = -164 mm; 180 x 60 mm");
        Check(ContainsRectangle(openings, 139.0, 55.0, 132.0, 60.0),
            "MIDI/USB support-window alignment", "x = +139 mm; 132 x 60 mm");
        Check(ContainsRectangle(openings, 239.0, 55.0, 34.0, 60.0),
            "Power-blank support-window alignment", "x = +239 mm; 34 x 60 mm");
        Check(ContainsRectangle(openings, -265.0, 55.0, 5.0, 34.0) &&
              ContainsRectangle(openings, 265.0, 55.0, 5.0, 34.0),
            "Two narrow-edge joiner slots", "x = +/-265 mm, outside interface windows");

        List<CylindricalFace> holes = Cylinders(edge, 1);
        Check(FilterDiameter(holes, 5.2).Count == 4,
            "Central carry-handle support fasteners", "four diameter-5.2 mm holes at x = +/-55 mm");
        Check(FilterDiameter(holes, 3.4).Count == 12,
            "Three replaceable narrow-edge cassette mounting groups", "12 total M3 clearance holes");
    }

    private void ValidateLowerVentEdge()
    {
        PartSnapshot edge = GetPart("LowerEdge_V03_HiddenVent");
        if (edge == null)
        {
            return;
        }

        AssertSize(edge, 542.0, 2.0, 108.0);
        List<RectangularOpening> openings = RectangularOpenings(edge);
        int ventSlots = 0;
        int joiners = 0;

        foreach (RectangularOpening opening in openings)
        {
            if (Almost(opening.Width, 22.0) && Almost(opening.Depth, 4.0) &&
                (Almost(opening.CenterZ, 47.0) || Almost(opening.CenterZ, 63.0)))
            {
                ventSlots++;
            }

            if (Almost(opening.Width, 5.0) && Almost(opening.Depth, 34.0))
            {
                joiners++;
            }
        }

        Check(ventSlots == 16, "Passive ventilation remains on the lower narrow edge",
            "two separated groups of eight 22 x 4 mm slots; actual " + ventSlots);
        Check(joiners == 2, "Lower narrow edge retains two joiner slots", "actual " + joiners);
    }

    private void ValidateRailGeometry()
    {
        Section("3 x 104HP rail and continuous threaded-strip geometry");
        PartSnapshot rail = GetPart("Rail_104HP_104xM3");
        PartSnapshot strip = GetPart("ThreadStrip_104HP_M3Pilot");
        PartSnapshot gauge = GetPart("FitGauge_104HP_3U");

        if (rail != null)
        {
            AssertSize(rail, 528.32, 10.0, 12.0);
            Check(FilterDiameter(Cylinders(rail, 2), 3.2).Count == 104,
                "Rail contains 104 module fastener positions", "104 diameter-3.2 mm openings on 5.08 mm pitch");
        }

        if (strip != null)
        {
            AssertSize(strip, 528.32, 6.0, 2.0);
            Check(FilterDiameter(Cylinders(strip, 2), 2.5).Count == 104,
                "Continuous strip contains 104 M3 tap-pilot positions", "104 diameter-2.5 mm pilot holes");
        }

        if (gauge != null)
        {
            AssertSize(gauge, 528.0, 128.5, 2.0);
        }
    }

    private void ValidatePowerEnvelopeParts()
    {
        Section("Power reservation volumes; no power product or circuit selected");
        PartSnapshot distributed = GetPart("ReservedPowerBus_500x85x20");
        PartSnapshot central = GetPart("ReservedPowerSupply_210x90x45");
        PartSnapshot module = GetPart(ModuleDepthEnvelope);

        if (distributed != null)
        {
            AssertSize(distributed, 500.0, 85.0, 20.0);
            Note("Distributed power-bus box is a keepout marker, not a selected busboard, regulator or physically manufactured aluminium part.");
        }

        if (central != null)
        {
            AssertSize(central, 210.0, 90.0, 45.0);
            Note("Central PSU box is a keepout marker, not a selected power module, inlet, mains circuit or validated isolation boundary.");
        }

        if (module != null)
        {
            AssertSize(module, 528.0, 112.0, 73.0);
            Check(Almost(module.Box[2] * MetresToMillimetres, 12.0) &&
                  Almost(module.Box[5] * MetresToMillimetres, 85.0),
                "85 mm nominal module-depth reference body",
                "nonphysical envelope begins behind 12 mm rails and ends 85 mm behind the module face");
            Note("Module envelopes deliberately expose the 25 mm central PSU and 12 mm distributed-bus depth conflicts; they do not represent manufactured modules.");
        }
    }

    private void ValidateAssemblyStage(string stage, string preferredStem, string fallbackStem,
        bool includesLid, bool includesClearanceObjects)
    {
        Section(stage.ToUpperInvariant() + " native assembly and STEP export");
        string stem = ResolveAssemblyStem(preferredStem, fallbackStem);
        if (stem == null)
        {
            Fail(stage + " V0.3 assembly exists", "Neither `" + preferredStem + "` nor `" + fallbackStem + "` exists.");
            return;
        }

        if (!string.Equals(stem, preferredStem, StringComparison.OrdinalIgnoreCase))
        {
            Warn(stage + " uses a legacy filename", "Actual V0.3 components must still replace the legacy-layout contents.");
        }

        string nativePath = Path.Combine(root, "cad", "assemblies", stem + ".SLDASM");
        string stepPath = Path.Combine(root, "exports", stem + ".STEP");
        Check(File.Exists(nativePath), stage + " native SLDASM exists", nativePath);
        Check(File.Exists(stepPath), stage + " STEP export exists", stepPath);

        ModelDoc2 model = null;
        try
        {
            model = OpenNative(nativePath, swDocumentTypes_e.swDocASSEMBLY);
            Check(model != null && string.Equals(Path.GetFullPath(model.GetPathName()), nativePath,
                StringComparison.OrdinalIgnoreCase), stage + " native assembly opens in SOLIDWORKS", nativePath);
        }
        catch (Exception exception)
        {
            Fail(stage + " native assembly opens in SOLIDWORKS", exception.Message);
        }

        if (model != null)
        {
            ValidateAssemblyContents(stage, model, includesLid, includesClearanceObjects);
            DetectInterference(stage, model);
        }

        if (File.Exists(stepPath))
        {
            ValidateStepImport(stage, stepPath);
        }
    }

    private void ValidateAssemblyContents(string stage, ModelDoc2 model, bool includesLid,
        bool includesClearanceObjects)
    {
        AssemblyDoc assembly = model as AssemblyDoc;
        if (assembly == null)
        {
            Fail(stage + " document type", "Opened native file is not an assembly.");
            return;
        }

        Array rawComponents = assembly.GetComponents(false) as Array;
        List<ComponentSnapshot> components = new List<ComponentSnapshot>();
        Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (rawComponents != null)
        {
            foreach (object item in rawComponents)
            {
                Component2 component = item as Component2;
                if (component == null)
                {
                    continue;
                }

                ComponentSnapshot snapshot = Snapshot(component);
                components.Add(snapshot);
                int oldCount;
                counts.TryGetValue(snapshot.Stem, out oldCount);
                counts[snapshot.Stem] = oldCount + 1;
            }
        }

        Dictionary<string, int> expected = ExpectedComponentCounts(includesLid, includesClearanceObjects);
        int expectedTotal = 0;
        foreach (KeyValuePair<string, int> pair in expected)
        {
            expectedTotal += pair.Value;
        }

        int reportedCount = assembly.GetComponentCount(false);
        Check(reportedCount == expectedTotal && components.Count == expectedTotal,
            stage + " component count matches V0.3 source placement formula",
            "expected " + expectedTotal + ", SOLIDWORKS " + reportedCount + ", enumerated " + components.Count);

        foreach (KeyValuePair<string, int> pair in expected)
        {
            int actual;
            counts.TryGetValue(pair.Key, out actual);
            Check(actual == pair.Value, stage + " component `" + pair.Key + "`",
                "expected " + pair.Value + ", actual " + actual);
        }

        foreach (KeyValuePair<string, int> pair in counts)
        {
            if (!expected.ContainsKey(pair.Key))
            {
                Fail(stage + " unexpected or legacy-layout component", pair.Key + " x " + pair.Value);
            }
        }

        VerifyUniquePosition(stage, components, "BackPanel_V03_VESAOnly", 0.0, 0.0, 0.0);
        VerifyUniquePosition(stage, components, "RearEdge_V03_IO_Handle_Power", 0.0, 209.0, 0.0);
        VerifyUniquePosition(stage, components, "LowerEdge_V03_HiddenVent", 0.0, -209.0, 0.0);
        VerifyUniquePosition(stage, components, "RearEdgeAudio_V03_8xTRS635", -164.0, 211.0, 15.0);
        VerifyUniquePosition(stage, components, "RearEdgeMidiUsb_V03_3xDIN_USB_C", 139.0, 211.0, 15.0);
        VerifyUniquePosition(stage, components, "RearEdgePowerBlank_V03", 239.0, 211.0, 15.0);
        VerifyUniquePosition(stage, components, CorrectedHandle, 0.0, 215.0, 45.0);
        VerifyUniquePosition(stage, components, "VesaReinforcement_100x100_M4", 0.0, 0.0, 105.0);
        VerifyUniquePosition(stage, components, "FourBackFeet_V03", 0.0, 0.0, 110.0);

        ValidateRailPlacements(stage, components);
        ValidateFlushSideHardware(stage, components);
        ValidateCentralHandleClearance(stage, components);

        if (includesClearanceObjects)
        {
            ValidateClearancePlacements(stage, components);
        }

        double[] bounds = assembly.GetBox(0) as double[];
        if (bounds != null && bounds.Length >= 6)
        {
            Note(stage + " actual SOLIDWORKS bounding-box minimum = (" +
                Format(bounds[0] * MetresToMillimetres) + ", " +
                Format(bounds[1] * MetresToMillimetres) + ", " +
                Format(bounds[2] * MetresToMillimetres) + ") mm; maximum = (" +
                Format(bounds[3] * MetresToMillimetres) + ", " +
                Format(bounds[4] * MetresToMillimetres) + ", " +
                Format(bounds[5] * MetresToMillimetres) + ") mm.");
            Note(stage + " assembly envelope including conceptual hardware: " +
                Format(AxisLength(bounds, 0)) + " x " + Format(AxisLength(bounds, 1)) +
                " x " + Format(AxisLength(bounds, 2)) + " mm.");
        }

        MassProperty mass = model.Extension.CreateMassProperty();
        if (mass != null)
        {
            Check(mass.Mass > 0.0 && !double.IsNaN(mass.Mass) && !double.IsInfinity(mass.Mass),
                stage + " IMassProperty material-derived CAD mass is finite",
                Format(mass.Mass) + " kg; calculated CAD mass, never a physical scale measurement");
            Note(stage + " CAD mass includes conceptual handle/legs/catches and four feet currently modelled as 6061 aluminium rather than selected rubber; repeated component instances and overlapping multibody reference solids are counted.");
            if (includesClearanceObjects)
            {
                Note("Clearance CAD mass additionally includes nonphysical fit gauges, nominal module envelopes and power keepouts; envelopes may have no assigned physical material or default density, so this is not the empty-case mass.");
            }
        }
        else
        {
            Warn(stage + " material-derived CAD mass", "SOLIDWORKS mass-property API returned no object.");
        }
    }

    private Dictionary<string, int> ExpectedComponentCounts(bool includesLid, bool includesClearanceObjects)
    {
        int railCount = Convert.ToInt32(Number(baseParameters, "rail", "count"), CultureInfo.InvariantCulture);
        int catches = Convert.ToInt32(Number(revisionParameters, "side_features", "internal_lid_catch_count"),
            CultureInfo.InvariantCulture);
        Dictionary<string, int> expected = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        expected.Add("BackPanel_V03_VESAOnly", 1);
        expected.Add("SideFrame_V03_RecessedLeg", 2);
        expected.Add("RearEdge_V03_IO_Handle_Power", 1);
        expected.Add("LowerEdge_V03_HiddenVent", 1);
        expected.Add("Rail_104HP_104xM3", railCount);
        expected.Add("ThreadStrip_104HP_M3Pilot", railCount);
        expected.Add("RailEndBlock_M3", railCount * 2);
        expected.Add("VesaReinforcement_100x100_M4", 1);
        expected.Add("RearCrossBeam_6061", 2);
        expected.Add("VesaStile_6061", 2);
        expected.Add("VesaBridge_6061", 2);
        expected.Add("RearEdgeAudio_V03_8xTRS635", 1);
        expected.Add("RearEdgeMidiUsb_V03_3xDIN_USB_C", 1);
        expected.Add("RearEdgePowerBlank_V03", 1);
        expected.Add(CorrectedHandle, 1);
        expected.Add("SideRecessedLeg_V03_TwoPosition", 2);
        expected.Add("InternalLidCatch_V03", catches);
        expected.Add("FourBackFeet_V03", 1);

        if (includesLid)
        {
            expected.Add("DeepTravelLid_70mmClearance", 1);
        }

        if (includesClearanceObjects)
        {
            expected.Add("FitGauge_104HP_3U", 3);
            expected.Add(ModuleDepthEnvelope, 3);
            expected.Add("ReservedPowerBus_500x85x20", 1);
            expected.Add("ReservedPowerSupply_210x90x45", 1);
        }

        return expected;
    }

    private void ValidateRailPlacements(string stage, List<ComponentSnapshot> components)
    {
        double rowPitch = Number(baseParameters, "eurorack", "row_pitch");
        double spacing = Number(baseParameters, "eurorack", "mounting_hole_vertical_spacing");
        List<double> expected = new List<double>();
        List<double> actual = new List<double>();

        foreach (double center in new double[] { -rowPitch, 0.0, rowPitch })
        {
            expected.Add(center - spacing / 2.0);
            expected.Add(center + spacing / 2.0);
        }

        foreach (ComponentSnapshot component in components)
        {
            if (SameStem(component, "Rail_104HP_104xM3"))
            {
                actual.Add(component.Y);
            }
        }

        expected.Sort();
        actual.Sort();
        bool positionsMatch = actual.Count == expected.Count;
        for (int index = 0; positionsMatch && index < expected.Count; index++)
        {
            positionsMatch = Almost(actual[index], expected[index]);
        }

        Check(positionsMatch, stage + " 3U row-to-rail vertical layout",
            "three 133.35 mm-pitch rows, each with a pair of rails 122.5 mm apart");
    }

    private void ValidateFlushSideHardware(string stage, List<ComponentSnapshot> components)
    {
        int legs = 0;
        int catches = 0;

        foreach (ComponentSnapshot component in components)
        {
            if (SameStem(component, "SideRecessedLeg_V03_TwoPosition"))
            {
                legs++;
                Check(Almost(Math.Abs(component.X), 271.0) && Almost(component.Y, -56.0) &&
                      Almost(component.Z, 46.0),
                    stage + " corrected recessed-leg centre " + legs,
                    "expected x = +/-271, y = -56, z = 46 mm; actual " + Coordinates(component));
                Check(WithinCaseWidth(component.Component),
                    stage + " recessed-leg outer surface " + legs,
                    "component bounding box remains within external case x = +/-274 mm");
                ValidateLegPocketBounds(stage, component, legs);
            }

            if (SameStem(component, "InternalLidCatch_V03"))
            {
                catches++;
                Check(Almost(Math.Abs(component.X), 272.0) &&
                      (Almost(component.Y, -150.0) || Almost(component.Y, 150.0)) &&
                      Almost(component.Z, 55.0),
                    stage + " flush internal cover catch " + catches,
                    "expected x = +/-272, y = +/-150, z = 55 mm; actual " + Coordinates(component));
                Check(WithinCaseWidth(component.Component),
                    stage + " cover catch remains inside case width " + catches,
                    "component bounding box remains within x = +/-274 mm");
            }
        }

        Check(legs == 2, stage + " short-side recessed folding-leg count", "2 legs; actual " + legs);
        Check(catches == 4, stage + " internal cover-lock count", "4 flush catches; actual " + catches);
    }

    private void ValidateLegPocketBounds(string stage, ComponentSnapshot component, int index)
    {
        double[] box = component.Component.GetBox(false, false) as double[];
        if (box == null || box.Length < 6)
        {
            Warn(stage + " recessed-leg pocket bounds " + index,
                "SOLIDWORKS did not expose a transformed component bounding box.");
            return;
        }

        double minimumY = box[1] * MetresToMillimetres;
        double maximumY = box[4] * MetresToMillimetres;
        double minimumZ = box[2] * MetresToMillimetres;
        double maximumZ = box[5] * MetresToMillimetres;
        Check(minimumY >= -137.0 - DimensionTolerance && maximumY <= 27.0 + DimensionTolerance &&
              minimumZ >= 42.0 - DimensionTolerance && maximumZ <= 64.0 + DimensionTolerance,
            stage + " recessed-leg pivot clears side-pocket boundaries " + index,
            "pocket y = -137..27, z = 42..64 mm; actual y = " + Format(minimumY) + ".." +
            Format(maximumY) + ", z = " + Format(minimumZ) + ".." + Format(maximumZ));
    }

    private void ValidateCentralHandleClearance(string stage, List<ComponentSnapshot> components)
    {
        ComponentSnapshot audio = FindUnique(components, "RearEdgeAudio_V03_8xTRS635");
        ComponentSnapshot digital = FindUnique(components, "RearEdgeMidiUsb_V03_3xDIN_USB_C");
        ComponentSnapshot handle = FindUnique(components, CorrectedHandle);
        if (audio == null || digital == null || handle == null)
        {
            return;
        }

        double[] audioBox = audio.Component.GetBox(false, false) as double[];
        double[] digitalBox = digital.Component.GetBox(false, false) as double[];
        double[] handleBox = handle.Component.GetBox(false, false) as double[];
        if (audioBox == null || digitalBox == null || handleBox == null ||
            audioBox.Length < 6 || digitalBox.Length < 6 || handleBox.Length < 6)
        {
            Warn(stage + " central handle to cassette clearance", "A transformed component bounding box is unavailable.");
            return;
        }

        double leftGap = (handleBox[0] - audioBox[3]) * MetresToMillimetres;
        double rightGap = (digitalBox[0] - handleBox[3]) * MetresToMillimetres;
        Check(Almost(leftGap, 1.0) && Almost(rightGap, 1.0),
            stage + " corrected handle clears both neighbouring removable panels",
            "audio-to-handle " + Format(leftGap) + " mm; handle-to-MIDI/USB " + Format(rightGap) + " mm");
    }

    private void ValidateClearancePlacements(string stage, List<ComponentSnapshot> components)
    {
        VerifyUniquePosition(stage, components, "ReservedPowerBus_500x85x20", 0.0, -105.0, 73.0);
        VerifyUniquePosition(stage, components, "ReservedPowerSupply_210x90x45", 0.0, 0.0, 60.0);
        List<double> gaugeRows = new List<double>();
        List<double> nominalModuleRows = new List<double>();

        foreach (ComponentSnapshot component in components)
        {
            bool marker = SameStem(component, "FitGauge_104HP_3U") ||
                          SameStem(component, ModuleDepthEnvelope) ||
                          SameStem(component, "ReservedPowerBus_500x85x20") ||
                          SameStem(component, "ReservedPowerSupply_210x90x45");
            if (!marker)
            {
                continue;
            }

            Check(component.Component.ExcludeFromBOM,
                stage + " nonphysical marker excluded from bill of materials: " + component.Stem,
                "gauge/keepout must not be represented as a purchased physical assembly part");

            if (SameStem(component, "FitGauge_104HP_3U"))
            {
                gaugeRows.Add(component.Y);
            }

            if (SameStem(component, ModuleDepthEnvelope))
            {
                nominalModuleRows.Add(component.Y);
            }
        }

        gaugeRows.Sort();
        nominalModuleRows.Sort();
        double rowPitch = Number(baseParameters, "eurorack", "row_pitch");
        Check(gaugeRows.Count == 3 && Almost(gaugeRows[0], -rowPitch) &&
              Almost(gaugeRows[1], 0.0) && Almost(gaugeRows[2], rowPitch),
            stage + " 3 x 104HP fit-gauge row positions", "y = -133.35, 0 and +133.35 mm");
        Check(nominalModuleRows.Count == 3 && Almost(nominalModuleRows[0], -rowPitch) &&
              Almost(nominalModuleRows[1], 0.0) && Almost(nominalModuleRows[2], rowPitch),
            stage + " 3 nominal 85 mm module-envelope row positions",
            "y = -133.35, 0 and +133.35 mm; each marker is excluded from the physical BOM");

        ComponentSnapshot central = FindUnique(components, "ReservedPowerSupply_210x90x45");
        ComponentSnapshot distributed = FindUnique(components, "ReservedPowerBus_500x85x20");
        if (central != null)
        {
            Check(Almost(central.Z, 60.0), "Central PSU footprint local module-depth limit",
                "only 60 mm before the reserved 45 mm PSU volume");
        }

        if (distributed != null)
        {
            Check(Almost(distributed.Z, 73.0), "Distributed bus footprint local module-depth limit",
                "only 73 mm before the reserved 20 mm bus volume");
        }

        if (central != null)
        {
            double centralOverlap = 85.0 - central.Z;
            Check(Almost(centralOverlap, 25.0),
                "Documented central PSU versus nominal module-depth conflict",
                "85 mm nominal module intersects the PSU reservation by " + Format(centralOverlap) +
                " mm; maximum module depth in that footprint is 60 mm");
            Warn("Unresolved central PSU packaging constraint",
                "A full-depth 85 mm module and the central 45 mm PSU reservation cannot coexist; 25 mm overlap remains.");
        }

        if (distributed != null)
        {
            double distributedOverlap = 85.0 - distributed.Z;
            Check(Almost(distributedOverlap, 12.0),
                "Documented distributed-bus versus nominal module-depth conflict",
                "85 mm nominal module intersects the bus reservation by " + Format(distributedOverlap) +
                " mm; maximum module depth in that footprint is 73 mm");
            Warn("Unresolved distributed-bus packaging constraint",
                "A full-depth 85 mm module and the distributed power-bus reservation cannot coexist; 12 mm overlap remains.");
        }
    }

    private void ValidateStepImport(string stage, string stepPath)
    {
        ModelDoc2 preexisting = application.GetOpenDocumentByName(stepPath) as ModelDoc2;
        if (preexisting != null)
        {
            Pass(stage + " STEP file is already open in SOLIDWORKS", stepPath);
            return;
        }

        ModelDoc2 imported = null;
        string temporaryDirectory = null;
        string uniqueImportPath = null;
        try
        {
            temporaryDirectory = Path.Combine(Path.GetTempPath(),
                "Rack4ModulesV03StepImport-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);
            uniqueImportPath = Path.Combine(temporaryDirectory,
                Path.GetFileNameWithoutExtension(stepPath) + "-validation-copy.STEP");
            File.Copy(stepPath, uniqueImportPath, false);

            object importOptions = application.GetImportFileData(uniqueImportPath);
            int importErrors = 0;
            imported = application.LoadFile4(uniqueImportPath, "r", importOptions, ref importErrors) as ModelDoc2;
            Check(imported != null && importErrors == 0,
                stage + " STEP export can be imported by SOLIDWORKS",
                "unique temporary copy of the exact export; import errors=" +
                importErrors.ToString(CultureInfo.InvariantCulture) + "; source=" + stepPath);

            if (imported != null)
            {
                AssemblyDoc importedAssembly = imported as AssemblyDoc;
                PartDoc importedPart = imported as PartDoc;
                if (importedAssembly != null)
                {
                    Check(importedAssembly.GetComponentCount(false) > 0,
                        stage + " STEP import contains assembly geometry",
                        importedAssembly.GetComponentCount(false).ToString(CultureInfo.InvariantCulture) + " components");
                }
                else if (importedPart != null)
                {
                    Array bodies = importedPart.GetBodies2((int)swBodyType_e.swSolidBody, false) as Array;
                    Check(bodies != null && bodies.Length > 0,
                        stage + " STEP import contains solid geometry",
                        bodies == null ? "0 solid bodies" : bodies.Length + " solid bodies");
                }
                else
                {
                    Fail(stage + " STEP import document type", "Imported document is neither a part nor an assembly.");
                }
            }
        }
        catch (Exception exception)
        {
            Fail(stage + " STEP export opens in SOLIDWORKS", exception.Message);
        }
        finally
        {
            if (imported != null)
            {
                application.CloseDoc(imported.GetTitle());
            }

            if (!string.IsNullOrEmpty(temporaryDirectory))
            {
                string fullTemporaryDirectory = Path.GetFullPath(temporaryDirectory);
                string fullSystemTemporaryRoot = Path.GetFullPath(Path.GetTempPath());
                if (fullTemporaryDirectory.StartsWith(fullSystemTemporaryRoot, StringComparison.OrdinalIgnoreCase) &&
                    Path.GetFileName(fullTemporaryDirectory).StartsWith("Rack4ModulesV03StepImport-",
                        StringComparison.Ordinal) && Directory.Exists(fullTemporaryDirectory))
                {
                    try
                    {
                        Directory.Delete(fullTemporaryDirectory, true);
                    }
                    catch (IOException exception)
                    {
                        Warn(stage + " temporary STEP-import cleanup", exception.Message);
                    }
                    catch (UnauthorizedAccessException exception)
                    {
                        Warn(stage + " temporary STEP-import cleanup", exception.Message);
                    }
                }
            }
        }
    }

    private void DetectInterference(string stage, ModelDoc2 model)
    {
        AssemblyDoc assembly = model as AssemblyDoc;
        InterferenceDetectionMgr manager = null;
        int physical = 0;
        int keepoutViolations = 0;
        int intentionalPackagingConflicts = 0;
        int conceptual = 0;
        int contacts = 0;

        try
        {
            int activationError = 0;
            application.ActivateDoc3(model.GetTitle(), false, 0, ref activationError);
            manager = assembly.InterferenceDetectionManager;
            if (manager == null)
            {
                Warn(stage + " volumetric interference check", "SOLIDWORKS did not expose its interference manager.");
                return;
            }

            manager.TreatCoincidenceAsInterference = false;
            manager.IncludeMultibodyPartInterferences = false;
            manager.MakeInterferingPartsTransparent = false;
            manager.IgnoreHiddenBodies = false;
            int total = manager.GetInterferenceCount();
            Array items = manager.GetInterferences() as Array;

            if (items != null)
            {
                foreach (object item in items)
                {
                    Interference interference = item as Interference;
                    if (interference == null)
                    {
                        continue;
                    }

                    double volume = interference.Volume * 1000000000.0;
                    Array participants = interference.Components as Array;
                    bool containsKeepout = false;
                    bool containsModuleEnvelope = false;
                    bool containsConcept = false;
                    List<string> names = new List<string>();

                    if (participants != null)
                    {
                        foreach (object participant in participants)
                        {
                            Component2 component = participant as Component2;
                            if (component == null)
                            {
                                continue;
                            }

                            string stem = Path.GetFileNameWithoutExtension(component.GetPathName());
                            names.Add(stem);
                            containsKeepout = containsKeepout || stem.StartsWith("ReservedPower", StringComparison.OrdinalIgnoreCase);
                            containsModuleEnvelope = containsModuleEnvelope ||
                                string.Equals(stem, ModuleDepthEnvelope, StringComparison.OrdinalIgnoreCase);
                            containsConcept = containsConcept || IsConceptualHardware(stem);
                        }
                    }

                    string detail = string.Join(" <-> ", names.ToArray()) +
                        "; overlapping volume " + Format(volume) + " mm^3";

                    if (Math.Abs(volume) <= NegligibleInterferenceVolume)
                    {
                        contacts++;
                        Note(stage + " contact/tolerance candidate: " + detail);
                    }
                    else if (containsKeepout && containsModuleEnvelope)
                    {
                        intentionalPackagingConflicts++;
                        Warn(stage + " intentional 85 mm module versus power-reservation overlap", detail +
                            "; known packaging limit: central PSU permits 60 mm and bus region permits 73 mm");
                    }
                    else if (containsKeepout)
                    {
                        keepoutViolations++;
                        Fail(stage + " reserved power volume conflicts with a non-envelope part", detail +
                            "; this is a keepout violation, not ordinary physical-part interference");
                    }
                    else if (containsConcept)
                    {
                        conceptual++;
                        Warn(stage + " conceptual fit-gauge or reference-hardware overlap", detail +
                            "; do not report as a validated physical collision-free design");
                    }
                    else
                    {
                        physical++;
                        Fail(stage + " nonzero physical solid interference", detail);
                    }
                }
            }

            Check(total == 0 || total == physical + keepoutViolations + intentionalPackagingConflicts + conceptual + contacts,
                stage + " interference results were classified",
                "API total=" + total + "; physical=" + physical + "; real keepout violations=" + keepoutViolations +
                "; intentional module/power conflicts=" + intentionalPackagingConflicts +
                "; conceptual=" + conceptual + "; contact/tolerance=" + contacts);
            if (physical == 0 && keepoutViolations == 0)
            {
                Pass(stage + " no detected physical collision or unclassified power-reserve violation",
                    intentionalPackagingConflicts + " intentional packaging conflicts, " + conceptual +
                    " conceptual overlaps and " + contacts + " contact/tolerance candidates are separately reported");
            }
        }
        catch (Exception exception)
        {
            Warn(stage + " interference detection could not complete",
                exception.GetType().Name + ": " + exception.Message + "; geometry and fit checks remain separately reported");
        }
        finally
        {
            if (manager != null)
            {
                try
                {
                    manager.Done();
                }
                catch (COMException)
                {
                    // The interference pane can have been closed by SOLIDWORKS.
                }
            }
        }
    }

    private PartSnapshot GetPart(string stem)
    {
        PartSnapshot existing;
        if (partCache.TryGetValue(stem, out existing))
        {
            return existing;
        }

        string path = Path.Combine(root, "cad", "parts", stem + ".SLDPRT");
        if (!File.Exists(path) || IsLockFile(path))
        {
            Fail("Required native V0.3 part exists: " + stem, path);
            partCache.Add(stem, null);
            return null;
        }

        try
        {
            ModelDoc2 document = OpenNative(path, swDocumentTypes_e.swDocPART);
            PartDoc part = document as PartDoc;
            if (part == null)
            {
                throw new InvalidDataException("Native file did not open as a SOLIDWORKS part.");
            }

            PartSnapshot snapshot = new PartSnapshot();
            snapshot.Stem = stem;
            snapshot.Document = document;
            snapshot.Box = part.GetPartBox(true) as double[];
            Array rawBodies = part.GetBodies2((int)swBodyType_e.swSolidBody, false) as Array;
            if (rawBodies != null)
            {
                foreach (object item in rawBodies)
                {
                    Body2 body = item as Body2;
                    if (body != null)
                    {
                        snapshot.Bodies.Add(body);
                    }
                }
            }

            if (snapshot.Box == null || snapshot.Box.Length < 6 || snapshot.Bodies.Count == 0)
            {
                throw new InvalidDataException("Part has no usable solid body or bounding box.");
            }

            string database;
            snapshot.Material = part.GetMaterialPropertyName2(string.Empty, out database);
            if (string.IsNullOrEmpty(snapshot.Material) && document.ConfigurationManager != null &&
                document.ConfigurationManager.ActiveConfiguration != null)
            {
                snapshot.Material = part.GetMaterialPropertyName2(
                    document.ConfigurationManager.ActiveConfiguration.Name, out database);
            }

            partCache.Add(stem, snapshot);
            return snapshot;
        }
        catch (Exception exception)
        {
            Fail("Native V0.3 part opens and has solid geometry: " + stem, exception.Message);
            partCache.Add(stem, null);
            return null;
        }
    }

    private ModelDoc2 OpenNative(string path, swDocumentTypes_e documentType)
    {
        string fullPath = Path.GetFullPath(path);
        ModelDoc2 alreadyOpen = application.GetOpenDocumentByName(fullPath) as ModelDoc2;
        if (alreadyOpen != null)
        {
            return alreadyOpen;
        }

        int errors = 0;
        int openWarnings = 0;
        ModelDoc2 document = application.OpenDoc6(fullPath, (int)documentType,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, string.Empty,
            ref errors, ref openWarnings) as ModelDoc2;
        if (document == null || errors != 0)
        {
            throw new InvalidOperationException("SOLIDWORKS open errors=" + errors +
                ", warnings=" + openWarnings + ", path=" + fullPath);
        }

        ownedDocumentTitles.Add(document.GetTitle());
        int relevantWarnings = openWarnings & ~(int)swFileLoadWarning_e.swFileLoadWarning_AlreadyOpen;
        if (relevantWarnings != 0)
        {
            Warn("SOLIDWORKS open warning for " + Path.GetFileName(fullPath),
                "warning bitmask " + relevantWarnings.ToString(CultureInfo.InvariantCulture));
        }
        else if (openWarnings != 0)
        {
            Note("SOLIDWORKS load warning 128 for " + Path.GetFileName(fullPath) +
                " is swFileLoadWarning_AlreadyOpen, not a file corruption or validation error.");
        }

        return document;
    }

    private static List<CylindricalFace> Cylinders(PartSnapshot part, int axis)
    {
        List<CylindricalFace> result = new List<CylindricalFace>();

        foreach (Body2 body in part.Bodies)
        {
            Array faces = body.GetFaces() as Array;
            if (faces == null)
            {
                continue;
            }

            foreach (object item in faces)
            {
                Face2 face = item as Face2;
                Surface surface = face == null ? null : face.GetSurface() as Surface;
                if (surface == null || !surface.IsCylinder())
                {
                    continue;
                }

                double[] parameters = surface.CylinderParams as double[];
                if (parameters == null || parameters.Length < 7 || Math.Abs(parameters[3 + axis]) < 0.99)
                {
                    continue;
                }

                CylindricalFace cylinder = new CylindricalFace();
                cylinder.X = parameters[0] * MetresToMillimetres;
                cylinder.Y = parameters[1] * MetresToMillimetres;
                cylinder.Z = parameters[2] * MetresToMillimetres;
                cylinder.Diameter = Math.Abs(parameters[6]) * 2.0 * MetresToMillimetres;
                result.Add(cylinder);
            }
        }

        return result;
    }

    private static List<CylindricalFace> FilterDiameter(List<CylindricalFace> cylinders, double diameter)
    {
        List<CylindricalFace> selected = new List<CylindricalFace>();
        foreach (CylindricalFace cylinder in cylinders)
        {
            if (Almost(cylinder.Diameter, diameter))
            {
                selected.Add(cylinder);
            }
        }

        return selected;
    }

    private static bool ContainsCylinder(List<CylindricalFace> cylinders, double first,
        double second, double diameter, int axis)
    {
        foreach (CylindricalFace cylinder in cylinders)
        {
            double actualSecond = axis == 2 ? cylinder.Y : cylinder.Z;
            if (Almost(cylinder.X, first) && Almost(actualSecond, second) &&
                Almost(cylinder.Diameter, diameter))
            {
                return true;
            }
        }

        return false;
    }

    private static int CountPlanarInnerLoops(PartSnapshot part, int normalAxis)
    {
        int maximum = 0;

        foreach (Body2 body in part.Bodies)
        {
            Array faces = body.GetFaces() as Array;
            if (faces == null)
            {
                continue;
            }

            foreach (object item in faces)
            {
                Face2 face = item as Face2;
                if (!IsExteriorPlateFace(face, part.Box, normalAxis))
                {
                    continue;
                }

                int count = 0;
                Array loops = face.GetLoops() as Array;
                if (loops != null)
                {
                    foreach (object rawLoop in loops)
                    {
                        Loop2 loop = rawLoop as Loop2;
                        if (loop != null && !loop.IsOuter())
                        {
                            count++;
                        }
                    }
                }

                maximum = Math.Max(maximum, count);
            }
        }

        return maximum;
    }

    private static List<RectangularOpening> RectangularOpenings(PartSnapshot part)
    {
        List<RectangularOpening> result = new List<RectangularOpening>();

        foreach (Body2 body in part.Bodies)
        {
            Array faces = body.GetFaces() as Array;
            if (faces == null)
            {
                continue;
            }

            foreach (object rawFace in faces)
            {
                Face2 face = rawFace as Face2;
                if (!IsExteriorPlateFace(face, part.Box, 1))
                {
                    continue;
                }

                Array loops = face.GetLoops() as Array;
                if (loops == null)
                {
                    continue;
                }

                foreach (object rawLoop in loops)
                {
                    Loop2 loop = rawLoop as Loop2;
                    if (loop == null || loop.IsOuter())
                    {
                        continue;
                    }

                    Array edges = loop.GetEdges() as Array;
                    if (edges == null || edges.Length != 4)
                    {
                        continue;
                    }

                    double minX = double.PositiveInfinity;
                    double maxX = double.NegativeInfinity;
                    double minZ = double.PositiveInfinity;
                    double maxZ = double.NegativeInfinity;
                    bool allLinear = true;

                    foreach (object rawEdge in edges)
                    {
                        Edge edge = rawEdge as Edge;
                        Curve curve = edge == null ? null : edge.GetCurve() as Curve;
                        if (curve == null || !curve.IsLine())
                        {
                            allLinear = false;
                            break;
                        }

                        Vertex[] vertices = { edge.GetStartVertex() as Vertex, edge.GetEndVertex() as Vertex };
                        foreach (Vertex vertex in vertices)
                        {
                            double[] point = vertex == null ? null : vertex.GetPoint() as double[];
                            if (point == null || point.Length < 3)
                            {
                                allLinear = false;
                                break;
                            }

                            minX = Math.Min(minX, point[0] * MetresToMillimetres);
                            maxX = Math.Max(maxX, point[0] * MetresToMillimetres);
                            minZ = Math.Min(minZ, point[2] * MetresToMillimetres);
                            maxZ = Math.Max(maxZ, point[2] * MetresToMillimetres);
                        }
                    }

                    if (!allLinear || double.IsInfinity(minX))
                    {
                        continue;
                    }

                    RectangularOpening opening = new RectangularOpening();
                    opening.CenterX = (minX + maxX) / 2.0;
                    opening.CenterZ = (minZ + maxZ) / 2.0;
                    opening.Width = maxX - minX;
                    opening.Depth = maxZ - minZ;
                    if (!ContainsRectangle(result, opening.CenterX, opening.CenterZ,
                        opening.Width, opening.Depth))
                    {
                        result.Add(opening);
                    }
                }
            }
        }

        return result;
    }

    private static bool IsExteriorPlateFace(Face2 face, double[] partBounds, int normalAxis)
    {
        if (face == null || partBounds == null || partBounds.Length < 6)
        {
            return false;
        }

        Surface surface = face.GetSurface() as Surface;
        double[] box = face.GetBox() as double[];
        if (surface == null || !surface.IsPlane() || box == null || box.Length < 6 ||
            AxisLength(box, normalAxis) > DimensionTolerance)
        {
            return false;
        }

        for (int axis = 0; axis < 3; axis++)
        {
            if (axis != normalAxis && AxisLength(box, axis) < AxisLength(partBounds, axis) * 0.5)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsRectangle(List<RectangularOpening> openings, double x,
        double z, double width, double depth)
    {
        foreach (RectangularOpening opening in openings)
        {
            if (Almost(opening.CenterX, x) && Almost(opening.CenterZ, z) &&
                Almost(opening.Width, width) && Almost(opening.Depth, depth))
            {
                return true;
            }
        }

        return false;
    }

    private static ComponentSnapshot Snapshot(Component2 component)
    {
        MathTransform transform = component.Transform2;
        Array values = transform == null ? null : transform.ArrayData as Array;
        if (values == null || values.Length < 12)
        {
            throw new InvalidDataException("Missing transform for component " + component.Name2);
        }

        ComponentSnapshot snapshot = new ComponentSnapshot();
        snapshot.Component = component;
        snapshot.Stem = Path.GetFileNameWithoutExtension(component.GetPathName());
        snapshot.X = Convert.ToDouble(values.GetValue(9), CultureInfo.InvariantCulture) * MetresToMillimetres;
        snapshot.Y = Convert.ToDouble(values.GetValue(10), CultureInfo.InvariantCulture) * MetresToMillimetres;
        snapshot.Z = Convert.ToDouble(values.GetValue(11), CultureInfo.InvariantCulture) * MetresToMillimetres;
        return snapshot;
    }

    private void VerifyUniquePosition(string stage, List<ComponentSnapshot> components,
        string stem, double x, double y, double z)
    {
        ComponentSnapshot found = FindUnique(components, stem);
        if (found == null)
        {
            return;
        }

        Check(Almost(found.X, x) && Almost(found.Y, y) && Almost(found.Z, z),
            stage + " position `" + stem + "`",
            "expected (" + Format(x) + ", " + Format(y) + ", " + Format(z) +
            ") mm; actual " + Coordinates(found));
    }

    private static ComponentSnapshot FindUnique(List<ComponentSnapshot> components, string stem)
    {
        ComponentSnapshot found = null;
        foreach (ComponentSnapshot component in components)
        {
            if (!SameStem(component, stem))
            {
                continue;
            }

            if (found != null)
            {
                return null;
            }

            found = component;
        }

        return found;
    }

    private static bool SameStem(ComponentSnapshot component, string stem)
    {
        return string.Equals(component.Stem, stem, StringComparison.OrdinalIgnoreCase);
    }

    private static bool WithinCaseWidth(Component2 component)
    {
        double[] box = component.GetBox(false, false) as double[];
        return box != null && box.Length >= 6 &&
               box[0] * MetresToMillimetres >= -274.0 - DimensionTolerance &&
               box[3] * MetresToMillimetres <= 274.0 + DimensionTolerance;
    }

    private static bool IsConceptualHardware(string stem)
    {
        return stem.StartsWith("FitGauge_", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(stem, ModuleDepthEnvelope, StringComparison.OrdinalIgnoreCase) ||
               stem.StartsWith("RearCarryHandle_", StringComparison.OrdinalIgnoreCase) ||
               stem.StartsWith("SideRecessedLeg_", StringComparison.OrdinalIgnoreCase) ||
               stem.StartsWith("InternalLidCatch_", StringComparison.OrdinalIgnoreCase) ||
               stem.StartsWith("FourBackFeet_", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSheetPart(string stem)
    {
        return stem == "BackPanel_V03_VESAOnly" ||
               stem == "RearEdge_V03_IO_Handle_Power" ||
               stem == "LowerEdge_V03_HiddenVent" ||
               stem == "RearEdgeAudio_V03_8xTRS635" ||
               stem == "RearEdgeMidiUsb_V03_3xDIN_USB_C" ||
               stem == "RearEdgePowerBlank_V03" ||
               stem == "DeepTravelLid_70mmClearance";
    }

    private string ResolveAssemblyStem(string preferred, string fallback)
    {
        if (File.Exists(Path.Combine(root, "cad", "assemblies", preferred + ".SLDASM")))
        {
            return preferred;
        }

        if (File.Exists(Path.Combine(root, "cad", "assemblies", fallback + ".SLDASM")))
        {
            return fallback;
        }

        return null;
    }

    private void AssertSize(PartSnapshot part, double x, double y, double z)
    {
        double actualX = AxisLength(part.Box, 0);
        double actualY = AxisLength(part.Box, 1);
        double actualZ = AxisLength(part.Box, 2);
        Check(Almost(actualX, x) && Almost(actualY, y) && Almost(actualZ, z),
            part.Stem + " solid envelope",
            "expected " + Format(x) + " x " + Format(y) + " x " + Format(z) +
            " mm; actual " + Format(actualX) + " x " + Format(actualY) + " x " + Format(actualZ) + " mm");
    }

    private static int CountUnlockedFiles(string directory, string extension)
    {
        int count = 0;
        foreach (string path in Directory.GetFiles(directory))
        {
            if (!IsLockFile(path) && string.Equals(Path.GetExtension(path), extension,
                StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsLockFile(string path)
    {
        return Path.GetFileName(path).StartsWith("~$", StringComparison.Ordinal);
    }

    private static double Number(Dictionary<string, object> parameters, string group, string key)
    {
        object rawGroup;
        if (!parameters.TryGetValue(group, out rawGroup))
        {
            throw new InvalidDataException("Missing design-parameter group " + group);
        }

        Dictionary<string, object> dictionary = rawGroup as Dictionary<string, object>;
        object value;
        if (dictionary == null || !dictionary.TryGetValue(key, out value))
        {
            throw new InvalidDataException("Missing design parameter " + group + "." + key);
        }

        return Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    private static double AxisLength(double[] box, int axis)
    {
        return (box[axis + 3] - box[axis]) * MetresToMillimetres;
    }

    private static bool Almost(double actual, double expected)
    {
        return Math.Abs(actual - expected) <= DimensionTolerance;
    }

    private static string Coordinates(ComponentSnapshot component)
    {
        return "(" + Format(component.X) + ", " + Format(component.Y) + ", " +
               Format(component.Z) + ") mm";
    }

    private static string Describe(double actual, string expected)
    {
        return "expected " + expected + ", actual " + Format(actual) + " mm";
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private void Section(string title)
    {
        report.AppendLine();
        report.AppendLine("## " + title);
        report.AppendLine();
    }

    private void Check(bool condition, string title, string detail)
    {
        if (condition)
        {
            Pass(title, detail);
        }
        else
        {
            Fail(title, detail);
        }
    }

    private void Pass(string title, string detail)
    {
        passes++;
        report.AppendLine("- PASS: " + title + " -- " + detail);
    }

    private void Warn(string title, string detail)
    {
        warnings++;
        report.AppendLine("- WARNING: " + title + " -- " + detail);
    }

    private void Fail(string title, string detail)
    {
        failures++;
        report.AppendLine("- FAIL: " + title + " -- " + detail);
    }

    private void Note(string detail)
    {
        report.AppendLine("- " + detail);
    }

    private sealed class PartSnapshot
    {
        internal string Stem;
        internal string Material;
        internal ModelDoc2 Document;
        internal double[] Box;
        internal readonly List<Body2> Bodies = new List<Body2>();
    }

    private sealed class CylindricalFace
    {
        internal double X;
        internal double Y;
        internal double Z;
        internal double Diameter;
    }

    private sealed class RectangularOpening
    {
        internal double CenterX;
        internal double CenterZ;
        internal double Width;
        internal double Depth;
    }

    private sealed class ComponentSnapshot
    {
        internal string Stem;
        internal double X;
        internal double Y;
        internal double Z;
        internal Component2 Component;
    }
}
