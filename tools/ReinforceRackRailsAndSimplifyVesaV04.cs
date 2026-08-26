using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Compile this independent entry point together with SwCadCore.cs. It creates
// four new parts and revises only the explicitly identified structural
// occurrences in the three existing V0.3 native assemblies.
internal static class ReinforceRackRailsAndSimplifyVesaV04
{
    [STAThread]
    private static int Main(string[] arguments)
    {
        RackStructuralRevisionV04 revision = null;

        try
        {
            if (arguments.Length != 1)
            {
                throw new ArgumentException("Exactly one existing Rack4Modules project root is required.");
            }

            revision = new RackStructuralRevisionV04(
                new RackCadSession(Path.GetFullPath(arguments[0])));
            revision.Update();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V04_RAIL_VESA_STRUCTURE_FAILED=" + exception);
            return 1;
        }
        finally
        {
            if (revision != null)
            {
                revision.CloseOnlyOwnedDocuments();
            }
        }
    }
}

internal sealed class RackStructuralRevisionV04
{
    private const string OriginalRailStem = "Rail_104HP_104xM3";
    private const string RevisedRailStem = "Rail_104HP_V04_SpineDualFix";
    private const string OriginalStripStem = "ThreadStrip_104HP_M3Pilot";
    private const string RevisedStripStem = "ThreadStrip_104HP_M3_AISI304_V04";
    private const string OriginalSideStem = "SideFrame_V03_RecessedLeg";
    private const string VentedSideStem = "SideFrame_V03_RecessedLeg_SideVent";
    private const string RevisedSideStem = "SideFrame_V04_Vented_DualRailFix";
    private const string OriginalBridgeStem = "VesaBridge_6061";
    private const string RevisedBridgeStem = "VesaBridge_6061_V04_DirectMount";
    private const string RemovedPlateStem = "VesaReinforcement_100x100_M4";
    private const string EndBlockStem = "RailEndBlock_M3";
    private const string BackPanelStem = "BackPanel_V03_VESAOnly";
    private const string StileStem = "VesaStile_6061";
    private const string CrossbeamStem = "RearCrossBeam_6061";
    private const string CentralPowerStem = "ReservedPowerSupply_210x90x45";
    private const string FrameMaterial = "6061-T6 (SS)";
    private const string StainlessMaterial = "AISI 304";

    private const double MillimetresPerMetre = 1000.0;
    private const double GeometryTolerance = 0.04;
    private const double TransformTolerance = 0.00000001;
    private const double FrontRailDepth = 12.0;
    private const double SpineDepth = 8.0;
    private const double StructuralHoleZ = 16.0;
    private const double LocatorHoleZ = 6.0;
    private const double StructuralClearanceDiameter = 4.5;
    private const double StructuralTapDiameter = 3.3;
    private const double LocatorClearanceDiameter = 3.4;
    private const double ModuleClearanceDiameter = 3.2;
    private const double ThreadStripInstallationZ = 4.0;
    private const double ThreadChannelStartZ = 3.9;
    private const double ThreadChannelDepth = 2.2;
    private const double BridgeWidth = 240.0;
    private const double BridgeHeight = 10.0;
    private const double BridgeDepth = 9.0;
    private const double BridgeCenterY = 50.0;
    private const double BridgeStartZ = 99.0;
    private const double CentralPowerHalfHeight = 45.0;
    private const double VentLengthY = 18.0;
    private const double VentWidthZ = 4.0;
    private const double VentCenterZ = 82.0;

    private static readonly double[] VentCentersY =
    {
        -120.0, -96.0, -72.0, -48.0, 48.0, 72.0, 96.0, 120.0
    };

    private static readonly double[] NaturalAluminium = { 0.73, 0.75, 0.77 };
    private static readonly double[] DarkAluminium = { 0.43, 0.47, 0.50 };
    private static readonly double[] StainlessAppearance = { 0.66, 0.69, 0.70 };

    private readonly RackCadSession cad;
    private readonly string projectPrefix;
    private readonly Dictionary<string, string> paths =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly List<StructuralAssemblyStage> stages =
        new List<StructuralAssemblyStage>();
    private readonly List<string> ownedDocumentTitles = new List<string>();
    private readonly double outerWidth;
    private readonly double outerHeight;
    private readonly double bodyDepth;
    private readonly double shellThickness;
    private readonly double sideThickness;
    private readonly double railLength;
    private readonly double railHeight;
    private readonly double holePitch;
    private readonly int moduleHoleCount;
    private readonly double stripWidth;
    private readonly double stripThickness;
    private readonly double stripTapDiameter;
    private readonly double rowPitch;
    private readonly double rowRailSpacing;
    private readonly double interiorWidth;
    private readonly double expectedSideX;
    private readonly double endBlockX;

    internal RackStructuralRevisionV04(RackCadSession session)
    {
        Require(session != null, "A connected SOLIDWORKS project session is required.");
        cad = session;
        projectPrefix = cad.Root.TrimEnd(Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        outerWidth = cad.N("enclosure", "outer_width");
        outerHeight = cad.N("enclosure", "outer_height");
        bodyDepth = cad.N("enclosure", "body_depth");
        shellThickness = cad.N("enclosure", "body_thickness");
        sideThickness = cad.N("enclosure", "side_frame_thickness");
        railLength = cad.N("rail", "length");
        railHeight = cad.N("rail", "profile_height");
        holePitch = cad.N("rail", "thread_hole_pitch");
        moduleHoleCount = (int)cad.N("rail", "thread_hole_count");
        stripWidth = cad.N("rail", "thread_strip_width");
        stripThickness = cad.N("rail", "thread_strip_thickness");
        stripTapDiameter = cad.N("rail", "thread_hole_minor_diameter");
        rowPitch = cad.N("eurorack", "row_pitch");
        rowRailSpacing = cad.N("eurorack", "mounting_hole_vertical_spacing");
        interiorWidth = outerWidth - 2.0 * sideThickness;
        expectedSideX = outerWidth / 2.0 - sideThickness / 2.0;
        endBlockX = railLength / 2.0 + (interiorWidth - railLength) / 4.0;

        foreach (string stem in new string[]
        {
            OriginalRailStem, RevisedRailStem, OriginalStripStem, RevisedStripStem,
            OriginalSideStem, VentedSideStem, RevisedSideStem, OriginalBridgeStem,
            RevisedBridgeStem, RemovedPlateStem, EndBlockStem, BackPanelStem,
            StileStem, CrossbeamStem, CentralPowerStem
        })
        {
            paths.Add(stem, ProjectPath(cad.PartsDirectory, stem + ".SLDPRT"));
        }

        // Save the visible open configuration last. The expected counts are
        // the frozen V0.3 counts before removing exactly one VESA sheet.
        stages.Add(new StructuralAssemblyStage("Rack4Modules_TransportClosed_V03", 48));
        stages.Add(new StructuralAssemblyStage("Rack4Modules_ClearanceCheck_V03", 55));
        stages.Add(new StructuralAssemblyStage("Rack4Modules_OpenCase_V03", 47));
    }

    internal void Update()
    {
        ValidateFrozenDesign();
        PreflightProjectFiles();

        foreach (StructuralAssemblyStage stage in stages)
        {
            stage.Path = ProjectPath(cad.AssembliesDirectory, stage.Stem + ".SLDASM");
            stage.Document = OpenExact(stage.Path, swDocumentTypes_e.swDocASSEMBLY);
            stage.Initial = Capture(stage, true);
            cad.Log("V04_STRUCTURE_PREFLIGHT=" + stage.Stem +
                ";components=" + stage.Initial.ComponentCount +
                ";old_plate=" + stage.Initial.Plates.Count);
        }

        BuildOrReuseRail();
        BuildOrReuseThreadStrip();
        BuildOrReuseSideFrame();
        BuildOrReuseVesaBridge();

        foreach (StructuralAssemblyStage stage in stages)
        {
            ReviseAndSaveAssembly(stage);
        }

        ModelDoc2 visible = stages[stages.Count - 1].Document;
        cad.Show(visible);
        cad.Application.FrameState = (int)swWindowState_e.swWindowMaximized;
        cad.Log("V04_STRUCTURAL_RAIL_ENVELOPE_MM=542x10x20;front_104HP=528.32x10x12;spine=542x10x8");
        cad.Log("V04_RAIL_END_STRUCTURE=M4_tap_pilot_3.3_at_z16_plus_independent_M3_locator_at_z6");
        cad.Log("V04_MODULE_THREAD_STRIPS=AISI_304;104_M3_positions_per_row;installation_z_mm=4");
        cad.Log("V04_VESA_DIRECT_SHELL=existing_2mm_back_four_4.5mm_holes;no_160x160x3_plate");
        cad.Log("V04_VESA_BRIDGES=2x240x10x9;y=-50,+50;z=99..108;M4_tap_pilots_at_x=-50,+50");
        cad.Log("V04_PSU_KEEP_OUT_Y_MM=-45..45;bridge_inner_edges=-45,+45;volume_overlap=0");
        cad.Log("V04_STRUCTURAL_VALIDATION_BOUNDARY=CAD_geometry_only;no_physical_vibration_pullout_or_load_test");
        cad.Log("V04_RAIL_VESA_STRUCTURE_COMPLETE=true");
    }

    internal void CloseOnlyOwnedDocuments()
    {
        string openPath = ProjectPath(cad.AssembliesDirectory,
            "Rack4Modules_OpenCase_V03.SLDASM");
        ModelDoc2 open = cad.Application.GetOpenDocumentByName(openPath) as ModelDoc2;
        string keepTitle = open == null ? null : open.GetTitle();

        for (int index = ownedDocumentTitles.Count - 1; index >= 0; index--)
        {
            if (string.Equals(ownedDocumentTitles[index], keepTitle,
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                cad.Application.CloseDoc(ownedDocumentTitles[index]);
            }
            catch (Exception exception)
            {
                cad.Log("WARNING: Cannot close structure-helper-owned document " +
                    ownedDocumentTitles[index] + ": " + exception.Message);
            }
        }

        if (open != null)
        {
            try
            {
                cad.Show(open);
            }
            catch (Exception exception)
            {
                cad.Log("WARNING: Cannot restore the existing open assembly: " + exception.Message);
            }
        }
    }

    private void ValidateFrozenDesign()
    {
        Require(Almost(outerWidth, 548.0), "The existing rack must remain 548 mm wide.");
        Require(Almost(outerHeight, 420.0), "The existing rack must remain 420 mm high.");
        Require(Almost(bodyDepth, 110.0), "The existing rack must remain 110 mm deep.");
        Require(Almost(shellThickness, 2.0), "VESA mounting must use the existing 2 mm rear shell.");
        Require(Almost(sideThickness, 3.0), "The existing side-frame thickness must remain 3 mm.");
        Require(Almost(interiorWidth, 542.0), "The structural rail spine must span exactly 542 mm.");
        Require(Almost(expectedSideX, 272.5), "Side frames must remain centred at x=+/-272.5 mm.");
        Require(Almost(railLength, 528.32), "The visible module rail must retain exactly 104HP.");
        Require(Almost(railHeight, 10.0), "The original rail height must remain 10 mm.");
        Require(Almost(cad.N("rail", "profile_depth"), FrontRailDepth),
            "The original 104HP rail front profile must remain 12 mm deep.");
        Require(Almost(holePitch, 5.08) && moduleHoleCount == 104,
            "Exactly 104 original module-hole positions at 5.08 mm pitch are required.");
        Require(Almost(stripWidth, 6.0) && Almost(stripThickness, 2.0) &&
                Almost(stripTapDiameter, 2.5),
            "The relocated AISI 304 strip must retain the original 6 x 2 mm M3 dimensions.");
        Require(Almost(rowPitch, 133.35) && Almost(rowRailSpacing, 122.5),
            "The frozen three-row Eurorack mounting locations changed.");
        Require(Almost(cad.N("vesa", "pitch_x"), 100.0) &&
                Almost(cad.N("vesa", "pitch_y"), 100.0) &&
                Almost(cad.N("vesa", "hole_diameter"), 4.5),
            "The existing VESA 100 M4 clearance pattern must remain unchanged.");
        Require(Almost(BridgeCenterY - BridgeHeight / 2.0, CentralPowerHalfHeight),
            "The direct-mount bridges must touch, not enter, the central PSU keepout.");
        Require(Almost(BridgeStartZ + BridgeDepth, bodyDepth - shellThickness),
            "Both VESA bridges must reach the inside face of the original 2 mm rear shell.");
        Require(ThreadStripInstallationZ >= ThreadChannelStartZ &&
                ThreadStripInstallationZ + stripThickness <=
                    ThreadChannelStartZ + ThreadChannelDepth,
            "The relocated stainless strip must fit entirely inside the rail channel.");
        Require(FrontRailDepth - (ThreadChannelStartZ + ThreadChannelDepth) >= 5.8,
            "M3 module screws need rear tip clearance beyond the relocated stainless strip.");
    }

    private void PreflightProjectFiles()
    {
        foreach (string required in new string[]
        {
            OriginalRailStem, OriginalStripStem, OriginalSideStem, OriginalBridgeStem,
            RemovedPlateStem, EndBlockStem, BackPanelStem, StileStem, CrossbeamStem
        })
        {
            Require(File.Exists(paths[required]),
                "A required existing project-owned structural part is missing: " + paths[required]);
        }

        foreach (StructuralAssemblyStage stage in stages)
        {
            string native = ProjectPath(cad.AssembliesDirectory, stage.Stem + ".SLDASM");
            string step = ProjectPath(cad.ExportsDirectory, stage.Stem + ".STEP");
            Require(File.Exists(native), "An existing V0.3 native assembly is missing: " + native);
            Require(File.Exists(step), "An existing V0.3 STEP export is missing: " + step);
        }
    }

    private void BuildOrReuseRail()
    {
        if (ReusePartIfPresent(RevisedRailStem, ValidateRailGeometry))
        {
            return;
        }

        ModelDoc2 document = cad.NewPart(RevisedRailStem);
        try
        {
            // A 0.02 mm union overlap is wholly inside the rear spine. The final
            // exterior is still exactly 528.32 x 10 x 12 in front and
            // 542 x 10 x 8 behind, with one connected structural solid.
            Body2 front = cad.Box(0.0, 0.0, 0.0,
                railLength, railHeight, FrontRailDepth + 0.02);
            front = cad.Cut(front,
                cad.Box(0.0, 0.0, ThreadChannelStartZ,
                    railLength + 0.8, stripWidth + 0.4, ThreadChannelDepth),
                "Continuous relocated 6.4 mm AISI 304 M3 thread-strip pocket z3.9..6.1");

            for (int index = 0; index < moduleHoleCount; index++)
            {
                double x = ModuleHoleX(index);
                front = cad.Cut(front,
                    cad.Cylinder(x, 0.0, -0.3, 0.0, 0.0, 1.0,
                        ModuleClearanceDiameter, FrontRailDepth + 0.8),
                    "Independent front-facing 104HP M3 module clearance position " +
                    (index + 1).ToString(CultureInfo.InvariantCulture));

                if ((index + 1) % 26 == 0)
                {
                    cad.Log("V04_STRUCTURAL_RAIL_MODULE_HOLES=" +
                        (index + 1).ToString(CultureInfo.InvariantCulture) + "/104");
                }
            }

            Body2 spine = cad.Box(0.0, 0.0, FrontRailDepth,
                interiorWidth, railHeight, SpineDepth);
            Body2 united = Unite(front, spine,
                "Single-piece 104HP front rail and full-width 542 mm structural rear spine");

            foreach (double side in Signs())
            {
                united = cad.Cut(united,
                    cad.Cylinder(side * (interiorWidth / 2.0 + 0.3), 0.0,
                        StructuralHoleZ, -side, 0.0, 0.0,
                        StructuralTapDiameter, 12.6),
                    "Independent side-facing M4 x 0.7 structural tap pilot; engagement >12 mm");
            }

            cad.AddBody(document, united,
                "One-piece 104HP front rail with full-width dual-end M4 load-bearing spine");
            ApplyVerifiedMaterial(document, FrameMaterial, NaturalAluminium);
            cad.Property(document, "Visible module standard", "104HP; 528.32 x 10 x 12 mm; 104 M3 positions");
            cad.Property(document, "Structural load path", "Continuous 542 x 10 x 8 mm rear spine directly between both 3 mm side frames");
            cad.Property(document, "Independent structural fixing", "Two end-facing diameter 3.3 mm M4 x 0.7 tap pilots at z16; usable penetration >12 mm");
            cad.Property(document, "Independent locator", "Existing M3 side screw and 6.84 mm front end block retained separately at z6");
            cad.Property(document, "Module screw pocket", "6.4 mm wide; z3.9..6.1; stainless strip z4..6; screw relief through z12");
            cad.Property(document, "Anti-loosening specification", "Specify suitably rated M4 fasteners and prevailing-torque patch or medium threadlocker after supplier selection");
            SaveNewPart(document, RevisedRailStem, ValidateRailGeometry);
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private void BuildOrReuseThreadStrip()
    {
        if (ReusePartIfPresent(RevisedStripStem, ValidateThreadStripGeometry))
        {
            return;
        }

        ModelDoc2 document = cad.NewPart(RevisedStripStem);
        try
        {
            Body2 strip = cad.Box(0.0, 0.0, 0.0,
                railLength, stripWidth, stripThickness);

            for (int index = 0; index < moduleHoleCount; index++)
            {
                strip = cad.Cut(strip,
                    cad.Cylinder(ModuleHoleX(index), 0.0, -0.3,
                        0.0, 0.0, 1.0, stripTapDiameter, stripThickness + 0.6),
                    "AISI 304 M3 x 0.5 module thread tap pilot position " +
                    (index + 1).ToString(CultureInfo.InvariantCulture));

                if ((index + 1) % 26 == 0)
                {
                    cad.Log("V04_AISI304_THREAD_STRIP_HOLES=" +
                        (index + 1).ToString(CultureInfo.InvariantCulture) + "/104");
                }
            }

            cad.AddBody(document, strip,
                "104-position AISI 304 stainless M3 module thread strip");
            ApplyVerifiedMaterial(document, StainlessMaterial, StainlessAppearance);
            cad.Property(document, "Physical material", "AISI 304 stainless steel; verified SOLIDWORKS material assignment");
            cad.Property(document, "Module threads", "104 x M3 x 0.5; diameter 2.5 mm tap pilot; 5.08 mm pitch");
            cad.Property(document, "Installation", "Install at assembly z4..6 mm inside the dedicated structural-rail pocket");
            cad.Property(document, "Fastener compatibility", "2 mm panel plus M3 x 8 reaches the relocated stainless insert with rear tip relief");
            SaveNewPart(document, RevisedStripStem, ValidateThreadStripGeometry);
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private void BuildOrReuseSideFrame()
    {
        if (ReusePartIfPresent(RevisedSideStem, ValidateSideFrameGeometry))
        {
            return;
        }

        ModelDoc2 document = cad.NewPart(RevisedSideStem);
        try
        {
            Body2 side = cad.Box(0.0, 0.0, 0.0,
                sideThickness, outerHeight, bodyDepth - shellThickness);

            foreach (double railY in RailPositions())
            {
                side = SideHole(side, railY, LocatorHoleZ,
                    LocatorClearanceDiameter, "Retained independent M3 front end-block locator");
                side = SideHole(side, railY, StructuralHoleZ,
                    StructuralClearanceDiameter, "New independent M4 rear-spine structural fastener");
            }

            side = cad.Cut(side,
                cad.Box(0.0, -55.0, 42.0,
                    sideThickness + 0.8, 164.0, 22.0),
                "Unchanged recessed folding-leg opening y=-137..27 and z=42..64");

            foreach (double catchY in new double[] { -150.0, 150.0 })
            {
                side = SideHole(side, catchY, 55.0, 12.2,
                    "Unchanged internal transport-cover latch aperture");
            }

            foreach (double y in VentCentersY)
            {
                double coreLength = VentLengthY - VentWidthZ;
                side = cad.Cut(side,
                    cad.Box(0.0, y, VentCenterZ - VentWidthZ / 2.0,
                        sideThickness + 0.8, coreLength, VentWidthZ),
                    "18 x 4 mm rounded side-vent rectangular 14 x 4 mm core");

                foreach (double direction in Signs())
                {
                    side = SideHole(side, y + direction * coreLength / 2.0,
                        VentCenterZ, VentWidthZ,
                        "Rounded R2 end for independent side ventilation slot");
                }
            }

            cad.AddBody(document, side,
                "Three-millimetre recessed-leg side frame with independent M3/M4 rail fixings and eight R2 vents");
            ApplyVerifiedMaterial(document, FrameMaterial, NaturalAluminium);
            cad.Property(document, "Retained outside dimensions", "3 x 420 x 108 mm; left/right centres x=-272.5,+272.5 mm");
            cad.Property(document, "Independent structural rail fixings", "6 x diameter 4.5 mm M4 clearance at rail row centres,z16");
            cad.Property(document, "Independent existing rail locators", "6 x diameter 3.4 mm M3 clearance at rail row centres,z6");
            cad.Property(document, "Retained leg opening", "164 x 22 mm; y=-137..27; z=42..64; unchanged for folding support");
            cad.Property(document, "Retained cover catches", "2 x diameter 12.2 mm; y=-150,+150; z55");
            cad.Property(document, "Rounded ventilation", "8 x 18 x 4 mm R2; y=-120,-96,-72,-48,+48,+72,+96,+120; z82");
            cad.Property(document, "Load-path boundary", "Rounded slots stop at z84, leaving 9 mm to the rear structural zone z93");
            SaveNewPart(document, RevisedSideStem, ValidateSideFrameGeometry);
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private void BuildOrReuseVesaBridge()
    {
        if (ReusePartIfPresent(RevisedBridgeStem, ValidateVesaBridgeGeometry))
        {
            return;
        }

        ModelDoc2 document = cad.NewPart(RevisedBridgeStem);
        try
        {
            Body2 bridge = cad.Box(0.0, 0.0, 0.0,
                BridgeWidth, BridgeHeight, BridgeDepth);

            foreach (double x in new double[] { -50.0, 50.0 })
            {
                bridge = cad.Cut(bridge,
                    cad.Cylinder(x, 0.0, -0.3, 0.0, 0.0, 1.0,
                        StructuralTapDiameter, BridgeDepth + 0.6),
                    "Direct VESA 100 M4 x 0.7 tap pilot through local 9 mm aluminium bridge");
            }

            cad.AddBody(document, bridge,
                "Local 240 x 10 x 9 mm VESA 100 M4 bridge from existing support stiles to the 2 mm rear shell");
            ApplyVerifiedMaterial(document, FrameMaterial, DarkAluminium);
            cad.Property(document, "Direct VESA load path", "M4 bracket -> existing 2 mm rear shell -> 9 mm local bridge -> existing vertical stiles");
            cad.Property(document, "Bridge dimensions", "240 x 10 x 9 mm; two instances y=-50,+50; z99..108");
            cad.Property(document, "VESA 100 fastener geometry", "2 x diameter 3.3 mm M4 x 0.7 tap pilots per bridge; x=-50,+50 mm");
            cad.Property(document, "Power keepout protection", "Inner bridge edges y=-45,+45 exactly coincide with 210 x 90 x 45 mm PSU boundary without volume overlap");
            cad.Property(document, "Removed material", "Replaces the former full 160 x 160 x 3 mm VESA reinforcement plate without deleting its legacy part file");
            cad.Property(document, "Acceptance boundary", "CAD alignment only; selected fastener engagement, pull-out, shock and VESA loading require real validation");
            SaveNewPart(document, RevisedBridgeStem, ValidateVesaBridgeGeometry);
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private void ReviseAndSaveAssembly(StructuralAssemblyStage stage)
    {
        AssemblyDoc assembly = stage.Document as AssemblyDoc;
        Require(assembly != null, "The selected project document is not an assembly: " + stage.Path);
        Activate(stage.Document);

        foreach (double side in Signs())
        {
            ReplaceAt(stage, StructuralKind.Side, side * expectedSideX, 0.0, 0.0,
                paths[RevisedSideStem], side * expectedSideX, 0.0, 0.0);
        }

        foreach (double y in RailPositions())
        {
            ReplaceAt(stage, StructuralKind.Rail, 0.0, y, 0.0,
                paths[RevisedRailStem], 0.0, y, 0.0);
            StructuralOccurrence strip = FindAt(Capture(stage, true).Strips,
                0.0, y, double.NaN, stage.Stem + " module threaded strip");
            ReplaceAt(stage, StructuralKind.Strip, 0.0, y, strip.Z,
                paths[RevisedStripStem], 0.0, y, ThreadStripInstallationZ);
        }

        foreach (double sign in Signs())
        {
            StructuralAssemblySnapshot before = Capture(stage, true);
            StructuralOccurrence bridge = FindByYSign(before.Bridges, sign,
                stage.Stem + " local VESA bridge");
            ReplaceAt(stage, StructuralKind.Bridge, 0.0, bridge.Y, bridge.Z,
                paths[RevisedBridgeStem], 0.0, sign * BridgeCenterY, BridgeStartZ);
        }

        RemoveOnlyLegacyPlate(stage);

        stage.Document.Extension.ForceRebuildAll();
        Require(stage.Document.ForceRebuild3(false),
            "SOLIDWORKS could not rebuild the revised rack structure: " + stage.Stem);
        assembly.UpdateBox();

        StructuralAssemblySnapshot revised = Capture(stage, false);
        ValidateFinalStage(stage, revised);
        VerifyUnchanged(stage.Initial.OtherSignatures, revised.OtherSignatures, stage.Stem);

        cad.Property(stage.Document, "Rail structure revision", "0.4: six full-width 542 mm one-piece 6061 spines with independent end M4 structural and M3 locating fixings");
        cad.Property(stage.Document, "Module installation threads", "Six replaceable 104-position AISI 304 M3 strips at assembly z4..6 mm; module holes remain independent from frame fasteners");
        cad.Property(stage.Document, "Direct rear-shell VESA", "Existing 2 mm broad rear shell has only four VESA 100 diameter 4.5 mm openings; no full-size reinforcement sheet");
        cad.Property(stage.Document, "Local VESA load path", "Two 240 x 10 x 9 mm 6061 bridges y=-50,+50,z99..108; diameter 3.3 mm M4 tap pilots x=-50,+50");
        cad.Property(stage.Document, "Central power reservation", "Existing y=-45..45 PSU volume remains free; direct VESA bridge edges coincide with its boundary only");
        cad.Property(stage.Document, "Retained independent locators", "Twelve original 6.84 mm M3 rail end blocks preserved without movement");
        cad.Property(stage.Document, "Side ventilation and folding support", "Eight rounded 18 x 4 mm R2 vents per side; recessed-leg pocket and four cover-lock interfaces retained");
        cad.Property(stage.Document, "Validation boundary", "Native CAD dimensions and material assignment only; vibration, pull-out, VESA load and physical prototype are not validated");

        string saved = cad.SaveAssembly(stage.Document, stage.Stem, true);
        string step = ProjectPath(cad.ExportsDirectory, stage.Stem + ".STEP");
        Require(SamePath(saved, stage.Path), "The assembly save changed its original project filename.");
        Require(File.Exists(stage.Path) && new FileInfo(stage.Path).Length > 0,
            "The revised native assembly was not written: " + stage.Path);
        Require(File.Exists(step) && new FileInfo(step).Length > 0,
            "The revised native assembly STEP export was not written: " + step);

        StructuralAssemblySnapshot afterSave = Capture(stage, false);
        ValidateFinalStage(stage, afterSave);
        VerifyUnchanged(stage.Initial.OtherSignatures, afterSave.OtherSignatures, stage.Stem);

        cad.Log("V04_STRUCTURE_UPDATED_ASSEMBLY=" + stage.Stem +
            ";components=" + afterSave.ComponentCount +
            ";rails=6;aisi304_strips=6;vented_dual_fix_sides=2;direct_vesa_bridges=2;legacy_vesa_plate=0");
    }

    private void ReplaceAt(StructuralAssemblyStage stage, StructuralKind kind,
        double oldX, double oldY, double oldZ, string replacementPath,
        double targetX, double targetY, double targetZ)
    {
        StructuralAssemblySnapshot initial = Capture(stage, true);
        List<StructuralOccurrence> items = KindOccurrences(initial, kind);
        StructuralOccurrence current = FindAt(items, oldX, oldY, oldZ,
            stage.Stem + " " + kind.ToString());
        double[] preserved = CopyTransform(current.Component);
        preserved[9] = targetX / MillimetresPerMetre;
        preserved[10] = targetY / MillimetresPerMetre;
        preserved[11] = targetZ / MillimetresPerMetre;

        if (!SamePath(current.Path, replacementPath))
        {
            AssemblyDoc assembly = stage.Document as AssemblyDoc;
            stage.Document.ClearSelection2(true);
            Require(current.Component.Select4(false, null, false),
                "Could not select exact project structural component " + current.Path);
            Require(assembly.ReplaceComponents(replacementPath, string.Empty, false, true),
                "SOLIDWORKS refused exact selected structural replacement " + current.Path);
            stage.Document.ClearSelection2(true);
        }

        // A replacement inherits the old transform before its new translation
        // is applied. Avoid a fully validated assembly snapshot in that brief
        // transition: new strips temporarily remain at z9.9 and new bridges
        // temporarily remain at y=+/-70 until we restore the target transform.
        StructuralOccurrence replacement = FindExactOccurrenceAt(
            stage.Document as AssemblyDoc, replacementPath, oldX, oldY, oldZ,
            stage.Stem + " selected structural replacement");
        RestoreTransform(replacement.Component, preserved);
        Require(TransformMatches(replacement.Component, preserved),
            "The structural replacement did not preserve its rotation and requested translation.");

        cad.Log("V04_REPLACED_STRUCTURE=" + stage.Stem + ";kind=" + kind +
            ";from_mm=" + Format(oldX) + "," + Format(oldY) + "," + Format(oldZ) +
            ";to_mm=" + Format(targetX) + "," + Format(targetY) + "," + Format(targetZ));
    }

    private void RemoveOnlyLegacyPlate(StructuralAssemblyStage stage)
    {
        StructuralAssemblySnapshot before = Capture(stage, true);
        if (before.Plates.Count == 0)
        {
            return;
        }

        Require(before.Plates.Count == 1, "More than one VESA reinforcement sheet was found.");
        StructuralOccurrence plate = before.Plates[0];
        Require(SamePath(plate.Path, paths[RemovedPlateStem]) &&
                Almost(plate.X, 0.0) && Almost(plate.Y, 0.0) && Almost(plate.Z, 105.0),
            "Refusing to remove anything except the exact legacy VESA plate at x0,y0,z105.");

        stage.Document.ClearSelection2(true);
        Require(plate.Component.Select4(false, null, false),
            "The exact single legacy VESA plate occurrence could not be selected.");
        Require(stage.Document.Extension.DeleteSelection2(0),
            "SOLIDWORKS refused removal of the selected assembly occurrence only.");
        stage.Document.ClearSelection2(true);

        Require(File.Exists(paths[RemovedPlateStem]),
            "The source legacy VESA plate file must remain preserved on disk.");
        cad.Log("V04_REMOVED_ASSEMBLY_OCCURRENCE_ONLY=" + stage.Stem +
            ";exact_part=" + paths[RemovedPlateStem] + ";legacy_part_file_preserved=true");
    }

    private StructuralAssemblySnapshot Capture(StructuralAssemblyStage stage, bool allowOriginal)
    {
        AssemblyDoc assembly = stage.Document as AssemblyDoc;
        Require(assembly != null, "The structural inspection target is not an assembly.");
        Array raw = assembly.GetComponents(false) as Array;
        Require(raw != null, "The target assembly does not expose its component instances.");

        StructuralAssemblySnapshot result = new StructuralAssemblySnapshot();
        result.ComponentCount = assembly.GetComponentCount(false);

        foreach (object value in raw)
        {
            Component2 component = value as Component2;
            Require(component != null, "An invalid assembly occurrence prevents safe replacement.");
            string path = NormalizeComponentPath(component);
            string stem = Path.GetFileNameWithoutExtension(path);
            string recognized;
            if (paths.TryGetValue(stem, out recognized))
            {
                Require(SamePath(path, recognized),
                    "A same-named structural component is outside its exact frozen project part path: " + path);
            }

            StructuralOccurrence occurrence = Occurrence(component, path);
            if (SamePath(path, paths[OriginalSideStem]) ||
                SamePath(path, paths[VentedSideStem]) ||
                SamePath(path, paths[RevisedSideStem]))
            {
                Require(allowOriginal || SamePath(path, paths[RevisedSideStem]),
                    "A legacy side frame remains in the final revised assembly.");
                result.Sides.Add(occurrence);
                continue;
            }

            if (SamePath(path, paths[OriginalRailStem]) ||
                SamePath(path, paths[RevisedRailStem]))
            {
                Require(allowOriginal || SamePath(path, paths[RevisedRailStem]),
                    "A legacy nonstructural rail remains in the final revised assembly.");
                result.Rails.Add(occurrence);
                continue;
            }

            if (SamePath(path, paths[OriginalStripStem]) ||
                SamePath(path, paths[RevisedStripStem]))
            {
                Require(allowOriginal || SamePath(path, paths[RevisedStripStem]),
                    "A legacy aluminium thread strip remains in the final revised assembly.");
                result.Strips.Add(occurrence);
                continue;
            }

            if (SamePath(path, paths[OriginalBridgeStem]) ||
                SamePath(path, paths[RevisedBridgeStem]))
            {
                Require(allowOriginal || SamePath(path, paths[RevisedBridgeStem]),
                    "A legacy VESA bridge remains in the final revised assembly.");
                result.Bridges.Add(occurrence);
                continue;
            }

            if (SamePath(path, paths[RemovedPlateStem]))
            {
                Require(allowOriginal, "A full VESA reinforcement plate remains in the final revised assembly.");
                result.Plates.Add(occurrence);
                continue;
            }

            if (SamePath(path, paths[EndBlockStem]))
            {
                result.EndBlocks.Add(occurrence);
            }
            else if (SamePath(path, paths[BackPanelStem]))
            {
                result.BackPanels.Add(occurrence);
            }
            else if (SamePath(path, paths[StileStem]))
            {
                result.Stiles.Add(occurrence);
            }
            else if (SamePath(path, paths[CrossbeamStem]))
            {
                result.Crossbeams.Add(occurrence);
            }
            else if (SamePath(path, paths[CentralPowerStem]))
            {
                result.PowerEnvelopes.Add(occurrence);
            }

            string signature = ComponentSignature(component, path);
            int count;
            result.OtherSignatures.TryGetValue(signature, out count);
            result.OtherSignatures[signature] = count + 1;
        }

        Require(result.Sides.Count == 2, "Exactly two old or revised short side frames are required.");
        Require(result.Rails.Count == 6, "Exactly six old or revised 104HP module rails are required.");
        Require(result.Strips.Count == 6, "Exactly six old or revised M3 module thread strips are required.");
        Require(result.Bridges.Count == 2, "Exactly two old or revised local VESA bridges are required.");
        Require(result.EndBlocks.Count == 12, "Exactly twelve original independent M3 rail end blocks must be preserved.");
        Require(result.BackPanels.Count == 1, "Exactly one existing VESA-only 2 mm broad rear shell is required.");
        Require(result.Stiles.Count == 2 && result.Crossbeams.Count == 2,
            "The existing pair of VESA stiles and pair of full-width crossbeams must remain untouched.");
        Require(result.Plates.Count <= 1, "More than one legacy full-size VESA reinforcement sheet was found.");

        int expectedCount = stage.LegacyComponents - (result.Plates.Count == 0 ? 1 : 0);
        Require(result.ComponentCount == expectedCount,
            "Unexpected assembly occurrence count in " + stage.Stem + ": expected " +
            expectedCount.ToString(CultureInfo.InvariantCulture) + ", actual " +
            result.ComponentCount.ToString(CultureInfo.InvariantCulture));

        ValidateCommonPositions(stage, result);
        return result;
    }

    private void ValidateCommonPositions(StructuralAssemblyStage stage,
        StructuralAssemblySnapshot snapshot)
    {
        foreach (double side in Signs())
        {
            FindAt(snapshot.Sides, side * expectedSideX, 0.0, 0.0,
                stage.Stem + " unchanged side-frame location");
        }

        foreach (double y in RailPositions())
        {
            FindAt(snapshot.Rails, 0.0, y, 0.0,
                stage.Stem + " unchanged 104HP rail location");
            StructuralOccurrence strip = FindAt(snapshot.Strips, 0.0, y, double.NaN,
                stage.Stem + " row module thread strip");
            bool oldStrip = SamePath(strip.Path, paths[OriginalStripStem]);
            Require(Almost(strip.Z, oldStrip ? FrontRailDepth - 2.1 : ThreadStripInstallationZ),
                "A module thread strip has neither its legacy z9.9 nor revised z4 installation depth.");

            foreach (double side in Signs())
            {
                FindAt(snapshot.EndBlocks, side * endBlockX, y, 0.0,
                    stage.Stem + " unchanged 6.84 mm M3 end locator");
            }
        }

        FindAt(snapshot.BackPanels, 0.0, 0.0, 0.0,
            stage.Stem + " unchanged VESA-only broad rear shell");
        foreach (double side in Signs())
        {
            FindAt(snapshot.Stiles, side * 115.0, 0.0, 93.0,
                stage.Stem + " unchanged existing VESA load stile");
            FindAt(snapshot.Crossbeams, 0.0, side * 155.0, 99.0,
                stage.Stem + " unchanged existing rear crossbeam");

            StructuralOccurrence bridge = FindByYSign(snapshot.Bridges, side,
                stage.Stem + " old or revised VESA bridge");
            bool oldBridge = SamePath(bridge.Path, paths[OriginalBridgeStem]);
            Require(Almost(bridge.X, 0.0) && Almost(bridge.Z, BridgeStartZ) &&
                    Almost(bridge.Y, side * (oldBridge ? 70.0 : BridgeCenterY)),
                "A local VESA bridge does not have its frozen legacy or revised location.");
        }

        foreach (StructuralOccurrence envelope in snapshot.PowerEnvelopes)
        {
            Require(Almost(envelope.X, 0.0) && Almost(envelope.Y, 0.0) &&
                    Almost(envelope.Z, 60.0),
                "The existing central PSU keepout must remain fixed at x0,y0,z60.");
        }

        foreach (StructuralOccurrence plate in snapshot.Plates)
        {
            Require(Almost(plate.X, 0.0) && Almost(plate.Y, 0.0) &&
                    Almost(plate.Z, 105.0),
                "The legacy VESA plate is not at its exact frozen removable location.");
        }
    }

    private void ValidateFinalStage(StructuralAssemblyStage stage,
        StructuralAssemblySnapshot snapshot)
    {
        Require(snapshot.ComponentCount == stage.LegacyComponents - 1,
            "Exactly one legacy VESA sheet must be removed from " + stage.Stem);
        Require(snapshot.Plates.Count == 0, "The legacy full VESA plate occurrence was not removed.");

        foreach (StructuralOccurrence side in snapshot.Sides)
        {
            Require(SamePath(side.Path, paths[RevisedSideStem]),
                "Both side frames must use the V04 vented independent M3/M4 part.");
        }

        foreach (StructuralOccurrence rail in snapshot.Rails)
        {
            Require(SamePath(rail.Path, paths[RevisedRailStem]),
                "All six module rails must contain the full-width M4 structural spine.");
        }

        foreach (StructuralOccurrence strip in snapshot.Strips)
        {
            Require(SamePath(strip.Path, paths[RevisedStripStem]) &&
                    Almost(strip.Z, ThreadStripInstallationZ),
                "All six AISI 304 M3 thread strips must move to assembly z4 mm.");
        }

        foreach (StructuralOccurrence bridge in snapshot.Bridges)
        {
            Require(SamePath(bridge.Path, paths[RevisedBridgeStem]) &&
                    Almost(Math.Abs(bridge.Y), BridgeCenterY) &&
                    Almost(bridge.Z, BridgeStartZ),
                "Both direct VESA bridges must be 240 x 10 x 9 mm at y=+/-50,z99.");
            double innerEdge = Math.Abs(bridge.Y) - BridgeHeight / 2.0;
            Require(innerEdge >= CentralPowerHalfHeight - GeometryTolerance,
                "A direct VESA bridge invades the central PSU power reservation.");
        }
    }

    private bool ReusePartIfPresent(string stem, Action<ModelDoc2> validate)
    {
        string path = paths[stem];
        if (!File.Exists(path))
        {
            return false;
        }

        ModelDoc2 existing = OpenExact(path, swDocumentTypes_e.swDocPART);
        validate(existing);
        string step = ProjectPath(cad.ExportsDirectory, stem + ".STEP");
        if (!File.Exists(step) || new FileInfo(step).Length == 0)
        {
            cad.SavePart(existing, stem, true);
        }

        cad.Log("V04_REUSED_VERIFIED_STRUCTURAL_PART=" + path);
        return true;
    }

    private void SaveNewPart(ModelDoc2 document, string stem,
        Action<ModelDoc2> validate)
    {
        validate(document);
        string actual = cad.SavePart(document, stem, true);
        string step = ProjectPath(cad.ExportsDirectory, stem + ".STEP");
        Require(SamePath(actual, paths[stem]),
            "A new structural part was saved outside its frozen exact project location.");
        Require(File.Exists(actual) && new FileInfo(actual).Length > 0 &&
                File.Exists(step) && new FileInfo(step).Length > 0,
            "A new structural part must produce both non-empty native and STEP files: " + stem);
        validate(document);
        cad.Log("V04_CREATED_STRUCTURAL_PART=" + actual);
    }

    private void ValidateRailGeometry(ModelDoc2 document)
    {
        PartDoc part;
        Body2 body = SingleSolid(document, RevisedRailStem, out part);
        ValidateBounds(part, interiorWidth, railHeight,
            FrontRailDepth + SpineDepth, RevisedRailStem);
        ValidateMaterial(document, FrameMaterial, RevisedRailStem);

        List<StructuralCylinder> cylinders = Cylinders(body);
        int sidePilots = 0;
        foreach (StructuralCylinder cylinder in cylinders)
        {
            if (cylinder.Axis == 'X' && Almost(cylinder.Diameter, StructuralTapDiameter) &&
                Almost(cylinder.Y, 0.0) && Almost(cylinder.Z, StructuralHoleZ))
            {
                sidePilots++;
            }
        }

        Require(sidePilots == 2,
            "The structural rail must contain exactly two independent X-axis M4 pilots at z16.");
        ValidateModuleHolePositions(cylinders, ModuleClearanceDiameter, RevisedRailStem);
    }

    private void ValidateThreadStripGeometry(ModelDoc2 document)
    {
        PartDoc part;
        Body2 body = SingleSolid(document, RevisedStripStem, out part);
        ValidateBounds(part, railLength, stripWidth, stripThickness, RevisedStripStem);
        ValidateMaterial(document, StainlessMaterial, RevisedStripStem);
        ValidateModuleHolePositions(Cylinders(body), stripTapDiameter, RevisedStripStem);
    }

    private void ValidateSideFrameGeometry(ModelDoc2 document)
    {
        PartDoc part;
        Body2 body = SingleSolid(document, RevisedSideStem, out part);
        ValidateBounds(part, sideThickness, outerHeight,
            bodyDepth - shellThickness, RevisedSideStem);
        ValidateMaterial(document, FrameMaterial, RevisedSideStem);

        List<StructuralCylinder> cylinders = Cylinders(body);
        foreach (double y in RailPositions())
        {
            Require(HasCylinder(cylinders, 'X', double.NaN, y,
                LocatorHoleZ, LocatorClearanceDiameter),
                "A retained side-frame M3 locator hole is missing at y=" + Format(y));
            Require(HasCylinder(cylinders, 'X', double.NaN, y,
                StructuralHoleZ, StructuralClearanceDiameter),
                "An independent side-frame M4 structural hole is missing at y=" + Format(y));
        }

        Require(CountCylinders(cylinders, 'X', LocatorClearanceDiameter) == 6 &&
                CountCylinders(cylinders, 'X', StructuralClearanceDiameter) == 6,
            "Each side frame must contain exactly six M3 locators and six separate M4 structural clearances.");

        foreach (double y in new double[] { -150.0, 150.0 })
        {
            Require(HasCylinder(cylinders, 'X', double.NaN, y, 55.0, 12.2),
                "An original cover-lock aperture is missing or moved.");
        }

        Require(CountCylinders(cylinders, 'X', 12.2) == 2,
            "The existing pair of cover-lock apertures must remain unchanged.");

        foreach (double y in VentCentersY)
        {
            foreach (double sign in Signs())
            {
                Require(HasCylinder(cylinders, 'X', double.NaN,
                    y + sign * 7.0, VentCenterZ, VentWidthZ),
                    "An 18 x 4 mm R2 rounded side-vent end is missing at y=" + Format(y));
            }
        }

        Require(CountCylinders(cylinders, 'X', VentWidthZ) == 16,
            "Each side frame must contain exactly eight two-ended R2 ventilation capsules.");
    }

    private void ValidateVesaBridgeGeometry(ModelDoc2 document)
    {
        PartDoc part;
        Body2 body = SingleSolid(document, RevisedBridgeStem, out part);
        ValidateBounds(part, BridgeWidth, BridgeHeight, BridgeDepth, RevisedBridgeStem);
        ValidateMaterial(document, FrameMaterial, RevisedBridgeStem);
        List<StructuralCylinder> cylinders = Cylinders(body);

        foreach (double x in new double[] { -50.0, 50.0 })
        {
            Require(HasCylinder(cylinders, 'Z', x, 0.0,
                double.NaN, StructuralTapDiameter),
                "A direct VESA 100 M4 tap pilot is missing at x=" + Format(x));
        }

        Require(CountCylinders(cylinders, 'Z', StructuralTapDiameter) == 2,
            "Each local VESA bridge must contain exactly two independent M4 tap pilots.");
    }

    private void ValidateModuleHolePositions(List<StructuralCylinder> cylinders,
        double diameter, string stem)
    {
        List<double> unique = new List<double>();
        foreach (StructuralCylinder cylinder in cylinders)
        {
            if (cylinder.Axis != 'Z' || !Almost(cylinder.Diameter, diameter) ||
                !Almost(cylinder.Y, 0.0))
            {
                continue;
            }

            bool known = false;
            foreach (double existing in unique)
            {
                if (Almost(existing, cylinder.X))
                {
                    known = true;
                    break;
                }
            }

            if (!known)
            {
                unique.Add(cylinder.X);
            }
        }

        // The internal stainless-strip channel splits each module-hole wall
        // into two cylindrical faces. Count unique x locations, not faces.
        Require(unique.Count == moduleHoleCount,
            "Exactly 104 unique original front-facing module positions are required in " + stem);
        foreach (int index in ModuleIndices())
        {
            bool found = false;
            foreach (double position in unique)
            {
                if (Almost(position, ModuleHoleX(index)))
                {
                    found = true;
                    break;
                }
            }

            Require(found, "An original 5.08 mm-pitch module position changed in " + stem);
        }
    }

    private Body2 Unite(Body2 first, Body2 second, string description)
    {
        int error;
        object result = first.Operations2((int)swBodyOperationType_e.SWBODYADD,
            second, out error);
        Require(error == (int)swBodyOperationError_e.swBodyOperationNoError,
            "SOLIDWORKS could not unite the continuous structural rail: " + description +
            "; error=" + error.ToString(CultureInfo.InvariantCulture));
        Array bodies = result as Array;
        Require(bodies != null && bodies.Length == 1,
            "The structural rail front and rear spine must form exactly one solid body.");
        Body2 united = bodies.GetValue(bodies.GetLowerBound(0)) as Body2;
        Require(united != null, "The structural union returned an invalid solid body.");
        return united;
    }

    private Body2 SideHole(Body2 body, double y, double z,
        double diameter, string description)
    {
        return cad.Cut(body,
            cad.Cylinder(-sideThickness / 2.0 - 0.3,
                y, z, 1.0, 0.0, 0.0, diameter, sideThickness + 0.6),
            description);
    }

    private void ApplyVerifiedMaterial(ModelDoc2 document,
        string material, double[] appearance)
    {
        cad.ApplyMaterial(document, material, appearance);
        ValidateMaterial(document, material, document.GetTitle());
    }

    private static Body2 SingleSolid(ModelDoc2 document, string context,
        out PartDoc part)
    {
        part = document as PartDoc;
        Require(part != null, "A requested structural geometry is not a SOLIDWORKS part: " + context);
        Array bodies = part.GetBodies2((int)swBodyType_e.swSolidBody, false) as Array;
        Require(bodies != null && bodies.Length == 1,
            "Structural part " + context + " must contain one connected solid body.");
        Body2 solid = bodies.GetValue(bodies.GetLowerBound(0)) as Body2;
        Require(solid != null, "No valid structural solid body exists in " + context);
        return solid;
    }

    private static void ValidateBounds(PartDoc part, double x,
        double y, double z, string context)
    {
        double[] bounds = part.GetPartBox(true) as double[];
        Require(bounds != null && bounds.Length >= 6,
            "The structural part has no bounding box: " + context);
        Require(Almost(Math.Abs(bounds[3] - bounds[0]) * MillimetresPerMetre, x) &&
                Almost(Math.Abs(bounds[4] - bounds[1]) * MillimetresPerMetre, y) &&
                Almost(Math.Abs(bounds[5] - bounds[2]) * MillimetresPerMetre, z),
            "Structural part " + context + " does not retain its exact frozen outside dimensions.");
    }

    private static void ValidateMaterial(ModelDoc2 document,
        string required, string context)
    {
        PartDoc part = document as PartDoc;
        Require(part != null, "Cannot verify a material on a non-part document: " + context);
        string database;
        string material = part.GetMaterialPropertyName2(string.Empty, out database);

        if (string.IsNullOrWhiteSpace(material) && document.ConfigurationManager != null &&
            document.ConfigurationManager.ActiveConfiguration != null)
        {
            material = part.GetMaterialPropertyName2(
                document.ConfigurationManager.ActiveConfiguration.Name, out database);
        }

        Require(string.Equals(material, required, StringComparison.OrdinalIgnoreCase),
            "Physical SOLIDWORKS material mismatch for " + context +
            ": expected '" + required + "', actual '" + material + "'.");
    }

    private static List<StructuralCylinder> Cylinders(Body2 body)
    {
        List<StructuralCylinder> result = new List<StructuralCylinder>();
        Array faces = body.GetFaces() as Array;
        Require(faces != null, "The structural solid does not expose topological faces.");

        foreach (object value in faces)
        {
            Face2 face = value as Face2;
            Surface surface = face == null ? null : face.GetSurface() as Surface;
            if (surface == null || !surface.IsCylinder())
            {
                continue;
            }

            double[] parameters = surface.CylinderParams as double[];
            if (parameters == null || parameters.Length < 7)
            {
                continue;
            }

            char axis;
            if (Math.Abs(parameters[3]) > 0.99)
            {
                axis = 'X';
            }
            else if (Math.Abs(parameters[5]) > 0.99)
            {
                axis = 'Z';
            }
            else
            {
                continue;
            }

            StructuralCylinder cylinder = new StructuralCylinder();
            cylinder.Axis = axis;
            cylinder.X = parameters[0] * MillimetresPerMetre;
            cylinder.Y = parameters[1] * MillimetresPerMetre;
            cylinder.Z = parameters[2] * MillimetresPerMetre;
            cylinder.Diameter = Math.Abs(parameters[6]) * 2.0 * MillimetresPerMetre;
            result.Add(cylinder);
        }

        return result;
    }

    private static bool HasCylinder(List<StructuralCylinder> cylinders,
        char axis, double x, double y, double z, double diameter)
    {
        foreach (StructuralCylinder item in cylinders)
        {
            if (item.Axis == axis && Almost(item.Diameter, diameter) &&
                (double.IsNaN(x) || Almost(item.X, x)) &&
                (double.IsNaN(y) || Almost(item.Y, y)) &&
                (double.IsNaN(z) || Almost(item.Z, z)))
            {
                return true;
            }
        }

        return false;
    }

    private static int CountCylinders(List<StructuralCylinder> cylinders,
        char axis, double diameter)
    {
        int count = 0;
        foreach (StructuralCylinder item in cylinders)
        {
            if (item.Axis == axis && Almost(item.Diameter, diameter))
            {
                count++;
            }
        }

        return count;
    }

    private StructuralOccurrence Occurrence(Component2 component, string path)
    {
        double[] transform = CopyTransform(component);
        StructuralOccurrence result = new StructuralOccurrence();
        result.Component = component;
        result.Path = path;
        result.X = transform[9] * MillimetresPerMetre;
        result.Y = transform[10] * MillimetresPerMetre;
        result.Z = transform[11] * MillimetresPerMetre;
        return result;
    }

    private ModelDoc2 OpenExact(string path, swDocumentTypes_e type)
    {
        string fullPath = Path.GetFullPath(path);
        ModelDoc2 existing = cad.Application.GetOpenDocumentByName(fullPath) as ModelDoc2;
        if (existing != null)
        {
            Require(SamePath(existing.GetPathName(), fullPath),
                "The already-open SOLIDWORKS document has a different project file path.");
            return existing;
        }

        int errors = 0;
        int warnings = 0;
        ModelDoc2 document = cad.Application.OpenDoc6(fullPath, (int)type,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
            string.Empty, ref errors, ref warnings) as ModelDoc2;
        Require(document != null && errors == 0 &&
                SamePath(document.GetPathName(), fullPath),
            "Cannot open exact project native document " + fullPath +
            "; errors=" + errors.ToString(CultureInfo.InvariantCulture) +
            "; warnings=" + warnings.ToString(CultureInfo.InvariantCulture));
        ownedDocumentTitles.Add(document.GetTitle());

        int actionable = warnings & ~(int)swFileLoadWarning_e.swFileLoadWarning_AlreadyOpen;
        if (actionable != 0)
        {
            cad.Log("WARNING: Structural document load status " +
                actionable.ToString(CultureInfo.InvariantCulture) + " for " + fullPath);
        }

        return document;
    }

    private void Activate(ModelDoc2 document)
    {
        int errors = 0;
        ModelDoc2 actual = cad.Application.ActivateDoc3(document.GetTitle(), false,
            (int)swRebuildOnActivation_e.swDontRebuildActiveDoc,
            ref errors) as ModelDoc2;
        Require(actual != null && SamePath(actual.GetPathName(), document.GetPathName()),
            "Cannot activate exact existing native assembly; status=" + errors);
    }

    private void RestoreTransform(Component2 component, double[] expected)
    {
        if (TransformMatches(component, expected))
        {
            return;
        }

        MathUtility utility = cad.Application.GetMathUtility() as MathUtility;
        Require(utility != null, "SOLIDWORKS cannot provide its component-transform utility.");
        MathTransform transform = utility.CreateTransform(expected) as MathTransform;
        Require(transform != null, "Cannot create the preserved structural component transformation.");
        component.Transform2 = transform;
    }

    private static bool TransformMatches(Component2 component, double[] expected)
    {
        double[] actual = CopyTransform(component);
        for (int index = 0; index < expected.Length; index++)
        {
            if (Math.Abs(actual[index] - expected[index]) > TransformTolerance)
            {
                return false;
            }
        }

        return true;
    }

    private static double[] CopyTransform(Component2 component)
    {
        MathTransform transform = component.Transform2;
        Array source = transform == null ? null : transform.ArrayData as Array;
        Require(source != null && source.Length >= 16,
            "An existing assembly component lacks a complete safe-to-preserve transform.");
        double[] result = new double[16];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = Convert.ToDouble(source.GetValue(index),
                CultureInfo.InvariantCulture);
        }

        return result;
    }

    private StructuralOccurrence FindExactOccurrenceAt(AssemblyDoc assembly,
        string exactPath, double x, double y, double z, string context)
    {
        Require(assembly != null, "The transient structural replacement is not in an assembly.");
        Array components = assembly.GetComponents(false) as Array;
        Require(components != null, "Cannot inspect the selected transient structural replacement.");
        StructuralOccurrence found = null;

        foreach (object value in components)
        {
            Component2 component = value as Component2;
            if (component == null)
            {
                continue;
            }

            string path = NormalizeComponentPath(component);
            if (!SamePath(path, exactPath))
            {
                continue;
            }

            StructuralOccurrence occurrence = Occurrence(component, path);
            if (Almost(occurrence.X, x) && Almost(occurrence.Y, y) &&
                Almost(occurrence.Z, z))
            {
                Require(found == null,
                    "More than one exact replacement file occupies " + context);
                found = occurrence;
            }
        }

        Require(found != null,
            "The exact selected replacement did not preserve its original temporary transform: " +
            context);
        return found;
    }

    private static StructuralOccurrence FindAt(List<StructuralOccurrence> occurrences,
        double x, double y, double z, string context)
    {
        StructuralOccurrence found = null;
        foreach (StructuralOccurrence occurrence in occurrences)
        {
            if ((double.IsNaN(x) || Almost(occurrence.X, x)) &&
                (double.IsNaN(y) || Almost(occurrence.Y, y)) &&
                (double.IsNaN(z) || Almost(occurrence.Z, z)))
            {
                Require(found == null,
                    "More than one exact project component occupies " + context);
                found = occurrence;
            }
        }

        Require(found != null, "No uniquely positioned structural component exists for " + context);
        return found;
    }

    private static StructuralOccurrence FindByYSign(
        List<StructuralOccurrence> occurrences, double sign, string context)
    {
        StructuralOccurrence found = null;
        foreach (StructuralOccurrence occurrence in occurrences)
        {
            if (sign * occurrence.Y > 0.0)
            {
                Require(found == null,
                    "More than one positive or negative VESA bridge exists: " + context);
                found = occurrence;
            }
        }

        Require(found != null, "A positive or negative existing VESA bridge is missing: " + context);
        return found;
    }

    private static List<StructuralOccurrence> KindOccurrences(
        StructuralAssemblySnapshot snapshot, StructuralKind kind)
    {
        switch (kind)
        {
            case StructuralKind.Side:
                return snapshot.Sides;
            case StructuralKind.Rail:
                return snapshot.Rails;
            case StructuralKind.Strip:
                return snapshot.Strips;
            case StructuralKind.Bridge:
                return snapshot.Bridges;
            default:
                throw new ArgumentOutOfRangeException("kind");
        }
    }

    private static string ComponentSignature(Component2 component, string path)
    {
        double[] transform = CopyTransform(component);
        StringBuilder signature = new StringBuilder(path.ToUpperInvariant());
        foreach (double value in transform)
        {
            signature.Append('|');
            signature.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        return signature.ToString();
    }

    private static void VerifyUnchanged(Dictionary<string, int> before,
        Dictionary<string, int> after, string context)
    {
        Require(before.Count == after.Count,
            "The unaffected assembly occurrence set changed in " + context);
        foreach (KeyValuePair<string, int> original in before)
        {
            int count;
            Require(after.TryGetValue(original.Key, out count) && count == original.Value,
                "An unrelated component path, full transform or occurrence count changed in " + context);
        }
    }

    private string NormalizeComponentPath(Component2 component)
    {
        string raw = component.GetPathName();
        Require(!string.IsNullOrWhiteSpace(raw),
            "A virtual or unnamed component prevents safe exact-path structural replacement.");
        string full = Path.GetFullPath(raw);
        Require(full.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase),
            "Refusing to revise an assembly containing a component outside this exact project: " + full);
        return full;
    }

    private string ProjectPath(string directory, string filename)
    {
        string full = Path.GetFullPath(Path.Combine(directory, filename));
        Require(full.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase),
            "Refusing a structural operation outside the existing Rack4Modules project: " + full);
        return full;
    }

    private IEnumerable<double> RailPositions()
    {
        foreach (double center in new double[] { -rowPitch, 0.0, rowPitch })
        {
            yield return center - rowRailSpacing / 2.0;
            yield return center + rowRailSpacing / 2.0;
        }
    }

    private IEnumerable<int> ModuleIndices()
    {
        for (int index = 0; index < moduleHoleCount; index++)
        {
            yield return index;
        }
    }

    private double ModuleHoleX(int index)
    {
        return -railLength / 2.0 + holePitch / 2.0 + index * holePitch;
    }

    private static IEnumerable<double> Signs()
    {
        yield return -1.0;
        yield return 1.0;
    }

    private static bool SamePath(string left, string right)
    {
        return !string.IsNullOrWhiteSpace(left) &&
            !string.IsNullOrWhiteSpace(right) &&
            string.Equals(Path.GetFullPath(left), Path.GetFullPath(right),
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool Almost(double actual, double expected)
    {
        return Math.Abs(actual - expected) <= GeometryTolerance;
    }

    private static string Format(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private enum StructuralKind
    {
        Side,
        Rail,
        Strip,
        Bridge
    }

    private sealed class StructuralAssemblyStage
    {
        internal readonly string Stem;
        internal readonly int LegacyComponents;
        internal string Path;
        internal ModelDoc2 Document;
        internal StructuralAssemblySnapshot Initial;

        internal StructuralAssemblyStage(string stem, int legacyComponents)
        {
            Stem = stem;
            LegacyComponents = legacyComponents;
        }
    }

    private sealed class StructuralAssemblySnapshot
    {
        internal int ComponentCount;
        internal readonly List<StructuralOccurrence> Sides = new List<StructuralOccurrence>();
        internal readonly List<StructuralOccurrence> Rails = new List<StructuralOccurrence>();
        internal readonly List<StructuralOccurrence> Strips = new List<StructuralOccurrence>();
        internal readonly List<StructuralOccurrence> Bridges = new List<StructuralOccurrence>();
        internal readonly List<StructuralOccurrence> Plates = new List<StructuralOccurrence>();
        internal readonly List<StructuralOccurrence> EndBlocks = new List<StructuralOccurrence>();
        internal readonly List<StructuralOccurrence> BackPanels = new List<StructuralOccurrence>();
        internal readonly List<StructuralOccurrence> Stiles = new List<StructuralOccurrence>();
        internal readonly List<StructuralOccurrence> Crossbeams = new List<StructuralOccurrence>();
        internal readonly List<StructuralOccurrence> PowerEnvelopes = new List<StructuralOccurrence>();
        internal readonly Dictionary<string, int> OtherSignatures =
            new Dictionary<string, int>(StringComparer.Ordinal);
    }

    private sealed class StructuralOccurrence
    {
        internal Component2 Component;
        internal string Path;
        internal double X;
        internal double Y;
        internal double Z;
    }

    private sealed class StructuralCylinder
    {
        internal char Axis;
        internal double X;
        internal double Y;
        internal double Z;
        internal double Diameter;
    }
}
