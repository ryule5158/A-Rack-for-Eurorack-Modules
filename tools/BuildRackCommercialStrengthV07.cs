using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// V0.7 is intentionally generated as a new set of native files.  This helper never
// saves a V0.4 source document, and it refuses to replace any generated target that
// is open with unsaved changes.  Compile together with SwCadCore.cs.
internal static class BuildRackCommercialStrengthV07
{
    private const string OldSideStem = "SideFrame_V04_Vented_DualRailFix";
    private const string OldLegStem = "SideKickstand_V04_LowerPivot150mm";
    private const string OldTravelLidStem = "DeepTravelLid_70mmClearance";
    private const string OldRailStem = "Rail_104HP_V04_SpineDualFix";
    private const string OldBackPanelStem = "BackPanel_V03_VESAOnly";

    private const string InnerSideStem = "SideFrame_V07_StableDoubleShearInner";
    private const string LegStem = "SideKickstand_V07_185mm_8x28";
    private const string OuterCheekStem = "KickstandOuterCheek_V07_4mm";
    private const string PivotPinStem = "KickstandPivotPin_V07_10mm";
    private const string SpacerStem = "KickstandSpacer_V07_8p8mm_M5";
    private const string LoadStopPinStem = "KickstandLoadStopPin_V07_10mm";
    private const string LockPinStem = "KickstandCaptiveIndexPin_V07_8mm";
    private const string HeelInsertStem = "KickstandHeelInsert_V07";
    private const string FootPadStem = "KickstandFootPad_V07_Rubber";
    private const string TravelLidStem = "DeepTravelLid_V07_StandRelief";
    private const string HandleSpreaderStem = "CarryHandleSpreader_V07_6061_4mm";
    private const string LightweightRailStem = "Rail_104HP_V07_ClosedTube_EndBoss";
    private const string LightweightBackPanelStem = "BackPanel_V07_5052_1p5mm_VESADoubler";

    private const double TravelLidThickness = 1.2;
    private const double TravelLidBeadDepth = 1.2;
    private const double TravelLidFrontZ = -70.0;
    private const double TravelLidSkirtDepth = 82.0;
    // The folded 185 mm leg, larger foot and reinforced cheek occupy the side
    // return envelope.  A bilateral top-side notch provides service clearance
    // while retaining roughly 68 mm of uninterrupted lower return wall as a lid
    // capture wall.  This is V0.7-only; the protected V0.4 lid is untouched.
    private const double TravelReliefMinY = -170.0;
    private const double TravelReliefMaxY = 76.0;
    private const double TravelReliefMinZ = -2.0;
    private const double TravelReliefMaxZ = 15.0;
    private const double ShowcaseLidX = 620.0;
    private const double ShowcaseLidY = 220.0;
    private const double ShowcaseLidZ = 0.0;

    private static readonly string[] LegacyV05KickstandStems = new string[]
    {
        "SideFrame_V05_Vented_DoubleShearInner",
        "SideKickstand_V05_DoubleShear150mm",
        "KickstandOuterCheek_V05_3mm",
        "KickstandPivotPin_V05_Flush",
        "KickstandSpacer_V05_4p8mm",
        "KickstandIndexPin_V05_SpringEnvelope"
    };

    private const double CaseWidth = 548.0;
    private const double CaseHeight = 420.0;
    private const double CaseDepth = 110.0;
    private const double ShellThickness = 2.0;
    private const double SourceSideThickness = 3.0;
    private const double InnerSideThickness = 4.0;
    private const double InnerSideCoreThickness = 3.0;
    private const double InnerSideCentreX = 273.0;
    private const double InteriorClearWidth = 542.0;

    // Stable V0.7 axial stack, mirrored left/right.
    // Right: inner side x=271..275, 8.8 cavity x=275..283.8,
    // outer cheek x=283.8..287.8.  The 8 mm leg has 0.4 mm nominal
    // clearance on both faces and is fixed at x=279.4 for every state.
    private const double CavityWidth = 8.8;
    private const double LegThickness = 8.0;
    private const double LegPlaneX = 279.4;
    private const double OuterCheekThickness = 4.0;
    private const double OuterCheekCentreX = 285.8;
    private const double OuterHalfWidth = 287.8;
    private const double OverallWidth = 575.6;
    private const double AxialClearanceEachSide = 0.4;

    private const double FoldedY = -54.0;
    private const double FoldedZ = 46.0;
    private const double HingeLocalY = -75.0;
    private const double HingeLocalZ = 6.0;
    private const double FootPadLocalY = 110.0;
    private const double FootPadLocalZ = 6.0;
    private const double HingeCaseY = -129.0;
    private const double HingeCaseZ = 52.0;
    private const double PivotToFootPadCentre = 185.0;
    private const double ArmInPlaneWidth = 28.0;
    private const double RootDiameter = 48.0;
    private const double PivotClearanceDiameter = 10.2;
    private const double PivotPinDiameter = 10.0;
    private const double PivotPinLength = 16.8;

    private const double FootPadDiameter = 26.0;
    private const double FootPadRadius = 13.0;
    private const double FootPadAxialLength = 8.4;
    private const double FootPadDeskCentreHeight = 13.0;
    private const double FootNeckThickness = 6.0;
    private const double FootNeckWidth = 16.0;
    private const double FootSocketDepth = 6.5;

    // The lower aluminium ear is offset behind the straight arm so the fixed
    // hard-stop pin remains outside the 28 mm arm during the complete fold.
    // A full-depth pocket y=-34..-20,z=-50..-26 leaves a steel heel at
    // y=-34..-26 plus an open pin path y=-26..-20.  The upper aluminium bridge
    // z=-26..-10 keys the heel and joins the diameter-48 root.
    private const double EarMinY = -37.0;
    private const double EarMaxY = -20.0;
    private const double EarMinZ = -50.0;
    private const double EarMaxZ = -10.0;
    private const double HeelMinY = -34.0;
    private const double HeelMaxY = -26.0;
    private const double HeelMinZ = -50.0;
    private const double HeelMaxZ = -26.0;
    private const double HeelCentreLocalY = -105.0;
    private const double HeelCentreLocalZ = -32.0;

    private const double LoadStopLocalY = -21.0;
    private const double LoadStopLocalZ = -38.0;
    private const double LoadStopDiameter = 10.0;
    private const double LoadStopClearanceDiameter = 10.2;
    private const double LoadStopLength = 16.8;
    // The stop pin is tangent to the steel heel's y=-26 face.  Its contact normal is
    // parallel to leg-local Y, so the moment arm about the pivot is |z|=38 mm,
    // not the radial pivot-to-pin-centre distance.
    private const double EffectiveStopLever = 38.0;
    private const int StopSweepIntervals = 2000;

    // Keep both indexing positions in real metal.  The deployed hole lies in
    // the 8 x 28 mm arm at q=(18,-9); after the 87.75 degree leg rotation the
    // same fixed plunger lands at q'=(9.699,17.633) in a local root boss for
    // the folded state.  The compact near-pivot layout leaves the offset hard
    // stop trajectory completely clear and the plunger never carries the
    // normal operating load.
    private const double LockDeployLocalY = 18.0;
    private const double LockDeployLocalZ = -9.0;
    private const double LockFoldedBossDiameter = 20.0;
    private const double LockPinDiameter = 8.0;
    private const double LockLegHoleDiameter = 8.2;
    private const double LockPlateHoleDiameter = 8.5;
    private const double LockPinLength = 12.0;
    private const double LockPinCentreX = 281.8;

    private const double ShellContactY = -210.0;
    private const double ShellContactZ = 110.0;
    private const double GroundTolerance = 0.1;
    private const double TransformTolerance = 0.0000001;
    private const double GeometryTolerance = 0.1;

    private const double StructuralHoleZ = 16.0;
    private const double LocatorHoleZ = 6.0;
    private const double StructuralClearanceDiameter = 4.5;
    private const double LocatorClearanceDiameter = 3.4;
    private const double CoverCatchDiameter = 12.2;
    private const double SpacerOuterDiameter = 12.0;
    private const double SpacerHoleDiameter = 5.5;
    private const double SpacerLength = 8.8;

    // The lightweight rail retains the original 528.32 x 10 x 12 mm solid
    // module face and its thread-strip pocket.  Only the old solid 542 x 10 x
    // 8 mm rear spine becomes a 1.5 mm closed tube; 25 mm solid end bosses
    // preserve the independent M4 structural threads and bearing area.
    private const double RailFrontLength = 528.32;
    private const double RailStructuralLength = 542.0;
    private const double RailHeight = 10.0;
    private const double RailFrontDepth = 12.0;
    private const double RailSpineDepth = 8.0;
    private const double RailTubeWall = 1.5;
    private const double RailEndBossLength = 25.0;
    private const double RailModulePitch = 5.08;
    private const int RailModuleHoleCount = 104;
    private const double RailModuleHoleDiameter = 3.2;
    private const double RailStructuralTapDiameter = 3.3;
    private const double RailThreadPocketStartZ = 3.9;
    private const double RailThreadPocketDepth = 2.2;
    private const double RailThreadPocketWidth = 6.4;

    // The broad rear sheet is no longer a uniform 2 mm plate.  A 1.5 mm skin
    // carries enclosure shear while the central 160 x 160 mm VESA doubler
    // restores the original 2.0 mm local stack at all four M4 holes.  Existing
    // VESA bridges/stiles/crossbeams remain untouched.
    private const double BackSkinThickness = 1.5;
    private const double BackVesaDoublerThickness = 0.5;
    private const double BackVesaDoublerSize = 160.0;

    // The local external strip covers the complete folded leg and rubber foot.
    // The upper-forward vent bank begins beyond this structural stand envelope.
    private const double OuterCheekMinY = -184.0;
    private const double OuterCheekMaxY = 72.0;
    private const double OuterCheekMainMinZ = 18.0;
    private const double OuterCheekMaxZ = 80.0;
    private const double OuterCheekEarMinY = -166.0;
    private const double OuterCheekEarMaxY = -92.0;
    private const double OuterCheekEarMinZ = -10.0;
    private const double OuterCheekEarMaxZ = 38.0;
    private const double FingerNotchY = 70.0;
    private const double FingerNotchZ = 52.0;
    private const double FingerNotchDiameter = 18.0;


    private const double VentLengthY = 22.0;
    private const double VentWidthZ = 4.0;

    private static readonly double[] VentCentersY = new double[]
    {
        94.0, 124.0, 154.0, 184.0
    };

    private static readonly double[] VentCentersZ = new double[]
    {
        92.0, 102.0
    };

    // Five physical spacer occurrences per side.  They sit outside the folded
    // arm/root envelope and provide a defined, non-floating outer-cheek stack.
    private static readonly MountPoint[] SpacerMounts = new MountPoint[]
    {
        new MountPoint(-173.0, 47.0),
        new MountPoint(-170.0, 68.0),
        new MountPoint(-60.0, 30.0),
        new MountPoint(0.0, 30.0),
        new MountPoint(60.0, 30.0)
    };

    private static readonly double[] NaturalAluminium = new double[] { 0.73, 0.75, 0.77 };
    private static readonly double[] DarkAluminium = new double[] { 0.12, 0.14, 0.17 };
    private static readonly double[] StainlessAppearance = new double[] { 0.66, 0.69, 0.70 };
    private static readonly double[] ElastomerAppearance = new double[] { 0.06, 0.07, 0.08 };
    private static readonly double[] GraphiteAppearance = new double[] { 0.12, 0.15, 0.18 };

    [STAThread]
    private static int Main(string[] arguments)
    {
        try
        {
            if (arguments == null || arguments.Length != 1 ||
                string.IsNullOrWhiteSpace(arguments[0]))
            {
                throw new ArgumentException(
                    "Usage: BuildRackCommercialStrengthV07.exe <Rack4Modules root>");
            }

            RackCadSession cad = new RackCadSession(Path.GetFullPath(arguments[0]));
            VerifyFrozenGeometry(cad);

            List<AssemblyStage> stages = BuildStages();
            PreflightExactSources(cad, stages);
            GuardGeneratedOutputs(cad, stages);

            List<FileSnapshot> v04Snapshots = CaptureV04Snapshots(cad, stages);
            Stance stance60 = CalculateStance(60.0);
            LogStance(cad, stance60);
            VerifyLoadStopSweep(cad, stance60);

            PartPaths parts = new PartPaths();
            parts.LightweightRail = CreateLightweightRail(cad);
            parts.LightweightBackPanel = CreateLightweightBackPanel(cad);
            parts.InnerSide = CreateInnerSideFrame(cad, stance60);
            parts.Leg = CreateStableLeg(cad, stance60);
            parts.OuterCheek = CreateOuterCheek(cad, stance60);
            parts.PivotPin = CreatePivotPin(cad);
            parts.Spacer = CreateSpacer(cad);
            parts.LoadStopPin = CreateLoadStopPin(cad);
            parts.LockPin = CreateLockPin(cad);
            parts.HeelInsert = CreateHeelInsert(cad);
            parts.FootPad = CreateFootPad(cad);
            parts.TravelLid = CreateTransportLid(cad);
            parts.HandleSpreader = CreateHandleSpreader(cad);

            foreach (AssemblyStage stage in stages)
            {
                Stance stance = null;
                if (Math.Abs(stage.FaceAngleDegrees - 60.0) < 0.001)
                {
                    stance = stance60;
                }
                BuildV07Assembly(cad, stage, stance, parts);
            }

            VerifyV04SnapshotsUnchanged(v04Snapshots);
            VerifyFinalAssemblyReadyOnDisk(cad, "Rack4Modules_ShowcaseTilt60_LidOff_V07");

            cad.Log("V07_INTERNAL_CLEAR_WIDTH_MM=542");
            cad.Log("V07_DOUBLE_SHEAR_STACK_MM=4_inner+8.8_cavity+4_outer");
            cad.Log("V07_INNER_SIDE_WEIGHT_STRATEGY=3mm_core+4mm_load_islands_and_edge_bands");
            cad.Log("V07_RAIL_WEIGHT_STRATEGY=solid_104HP_face+1.5mm_closed_spine+25mm_solid_end_bosses");
            cad.Log("V07_BACK_WEIGHT_STRATEGY=1.5mm_skin+central_local_2.0mm_VESA_stack");
            cad.Log("V07_LID_WEIGHT_STRATEGY=1.2mm_5052_returns+two_1.2mm_anti_drum_beads");
            cad.Log("V07_LEG_MM=8_thickx28_wide;7075-T6_plate;root_diameter_48");
            cad.Log("V07_LEG_PLANE_X_MM=+/-279.4;no_axial_popout");
            cad.Log("V07_OUTER_WIDTH_MM=575.6");
            cad.Log("V07_OFFICIAL_STANCE=60_degree_only;V05_75_degree_history_not_generated");
            cad.Log("V07_STOP_LOCK_ORDER=hard_stop_first;lock_pin_reverse_only");
            cad.Log("V07_V04_SOURCE_HASHES_UNCHANGED=true");
            cad.Log("V07_STABLE_KICKSTAND_BUILD_COMPLETE=true");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V07_STABLE_KICKSTAND_BUILD_FAILED=" + exception.ToString());
            Console.Error.Flush();
            return 1;
        }
    }

    private static List<AssemblyStage> BuildStages()
    {
        return new List<AssemblyStage>
        {
            new AssemblyStage("Rack4Modules_OpenCase_V04", "Rack4Modules_OpenCase_V07", 46, 0.0, false, false),
            new AssemblyStage("Rack4Modules_TransportClosed_V04", "Rack4Modules_TransportClosed_V07", 47, 0.0, true, false),
            new AssemblyStage("Rack4Modules_ClearanceCheck_V04", "Rack4Modules_ClearanceCheck_V07", 54, 0.0, false, false),
            new AssemblyStage("Rack4Modules_DesktopTilt60_V04", "Rack4Modules_DesktopTilt60_V07", 47, 60.0, false, false),
            new AssemblyStage("Rack4Modules_DesktopTilt60_V04", "Rack4Modules_ShowcaseTilt60_LidOff_V07", 47, 60.0, false, true)
        };
    }

    private static void VerifyFrozenGeometry(RackCadSession cad)
    {
        RequireClose(cad.N("enclosure", "outer_width"), CaseWidth, 0.001, "V0.4 source width");
        RequireClose(cad.N("enclosure", "outer_height"), CaseHeight, 0.001, "V0.4 source height");
        RequireClose(cad.N("enclosure", "body_depth"), CaseDepth, 0.001, "V0.4 source depth");
        RequireClose(cad.N("enclosure", "body_thickness"), ShellThickness, 0.001, "shell thickness");
        RequireClose(cad.N("enclosure", "side_frame_thickness"), SourceSideThickness, 0.001,
            "V0.4 source side-frame thickness");
        RequireClose(2.0 * (InnerSideCentreX - InnerSideThickness / 2.0),
            InteriorClearWidth, 0.000001, "unchanged internal clear width");
        RequireClose(InnerSideCentreX - InnerSideThickness / 2.0, InteriorClearWidth / 2.0,
            0.000001, "right inner clear boundary");
        RequireClose(OuterCheekCentreX - OuterCheekThickness / 2.0,
            InnerSideCentreX + InnerSideThickness / 2.0 + CavityWidth,
            0.000001, "outer-cheek inner face");
        RequireClose(LegPlaneX - LegThickness / 2.0,
            InnerSideCentreX + InnerSideThickness / 2.0 + AxialClearanceEachSide,
            0.000001, "leg inner-face clearance");
        RequireClose(OuterCheekCentreX - OuterCheekThickness / 2.0 -
            (LegPlaneX + LegThickness / 2.0), AxialClearanceEachSide, 0.000001,
            "leg outer-face clearance");
        RequireClose(OuterHalfWidth * 2.0, OverallWidth, 0.000001, "V0.7 overall width");
        RequireClose(FoldedY + HingeLocalY, HingeCaseY, 0.000001, "hinge y");
        RequireClose(FoldedZ + HingeLocalZ, HingeCaseZ, 0.000001, "hinge z");
        RequireClose(FootPadLocalY - HingeLocalY, PivotToFootPadCentre, 0.000001,
            "hinge-to-rubber-foot centre length");
        RequireClose(FootPadRadius, FootPadDeskCentreHeight, 0.000001,
            "round foot lowest-point desk contact");
        RequireClose(PivotPinLength,
            InnerSideThickness + CavityWidth + OuterCheekThickness, 0.000001,
            "flush double-shear pin grip length");
        RequireClose(LoadStopLength, PivotPinLength, 0.000001,
            "load-stop full stack length");
        RequireClose(SpacerLength, CavityWidth, 0.000001, "spacer-defined cavity");
        RequireClose(Math.Abs(LoadStopLocalZ), EffectiveStopLever,
            0.000001, "hard-stop effective contact lever");
        RequireClose(cad.N("rail", "length"), 528.32, 0.001, "unchanged 104HP rail length");
    }

    private static void PreflightExactSources(RackCadSession cad, List<AssemblyStage> stages)
    {
        RequireProjectFile(cad, PartPath(cad, OldSideStem));
        RequireProjectFile(cad, PartPath(cad, OldLegStem));
        RequireProjectFile(cad, PartPath(cad, OldTravelLidStem));
        RequireProjectFile(cad, PartPath(cad, OldRailStem));
        RequireProjectFile(cad, PartPath(cad, OldBackPanelStem));

        foreach (AssemblyStage stage in stages)
        {
            string sourcePath = AssemblyPath(cad, stage.SourceStem);
            RequireProjectFile(cad, sourcePath);
        }
    }

    private static void GuardGeneratedOutputs(RackCadSession cad, List<AssemblyStage> stages)
    {
        List<string> outputs = new List<string>();
        foreach (string stem in new string[]
        {
            InnerSideStem, LegStem, OuterCheekStem, PivotPinStem, SpacerStem,
            LoadStopPinStem, LockPinStem, HeelInsertStem, FootPadStem, TravelLidStem,
            HandleSpreaderStem, LightweightRailStem, LightweightBackPanelStem
        })
        {
            outputs.Add(PartPath(cad, stem));
            outputs.Add(Path.GetFullPath(Path.Combine(cad.ExportsDirectory, stem + ".STEP")));
        }

        foreach (AssemblyStage stage in stages)
        {
            outputs.Add(AssemblyPath(cad, stage.TargetStem));
            outputs.Add(Path.GetFullPath(Path.Combine(cad.ExportsDirectory,
                stage.TargetStem + ".STEP")));
        }

        List<string> safeCloseTitles = new List<string>();
        List<string> recentFailedBuildTitles = new List<string>();
        List<string> recentGeneratedPartTitles = new List<string>();
        ModelDoc2 document = cad.Application.GetFirstDocument() as ModelDoc2;
        while (document != null)
        {
            ModelDoc2 next = document.GetNext() as ModelDoc2;
            string openPath = document.GetPathName();
            string openFullPath = string.IsNullOrWhiteSpace(openPath)
                ? string.Empty
                : Path.GetFullPath(openPath);
            string titleStem = TitleStem(document.GetTitle());

            foreach (string output in outputs)
            {
                string outputStem = Path.GetFileNameWithoutExtension(output);
                bool exactPath = !string.IsNullOrEmpty(openFullPath) && SamePath(openFullPath, output);
                bool sameTitle = string.Equals(titleStem, outputStem,
                    StringComparison.OrdinalIgnoreCase);
                if (!exactPath && !sameTitle)
                {
                    continue;
                }

                if (!exactPath)
                {
                    throw new InvalidOperationException(
                        "Refusing to overwrite a generated V0.7 target associated with an ambiguous " +
                        "open document: title=" + document.GetTitle() + "; path=" + openFullPath);
                }

                if (document.GetSaveFlag())
                {
                    bool exactV07GeneratedPart = false;
                    foreach (string generatedPartStem in new string[]
                    {
                        InnerSideStem, LegStem, OuterCheekStem,
                        PivotPinStem, SpacerStem, LoadStopPinStem, LockPinStem,
                        HeelInsertStem, FootPadStem, TravelLidStem, HandleSpreaderStem,
                        LightweightRailStem, LightweightBackPanelStem
                    })
                    {
                        if (SamePath(openFullPath, PartPath(cad, generatedPartStem)))
                        {
                            exactV07GeneratedPart = true;
                            break;
                        }
                    }

                    bool provenGeneratedPart = false;
                    if (exactV07GeneratedPart &&
                        document.GetType() == (int)swDocumentTypes_e.swDocPART &&
                        File.Exists(openFullPath))
                    {
                        // Every path in this branch is an exact whitelisted V0.7
                        // generator target.  An interrupted run can leave it dirty
                        // for longer than fifteen minutes even though its disk file
                        // is wholly reproducible.  Close only this exact generated
                        // part without saving; protected V0.4/V0.6 sources are not
                        // present in the whitelist.
                        provenGeneratedPart = true;
                    }

                    if (provenGeneratedPart)
                    {
                        safeCloseTitles.Add(document.GetTitle());
                        recentGeneratedPartTitles.Add(document.GetTitle());
                        break;
                    }

                    AssemblyDoc dirtyAssembly = document as AssemblyDoc;
                    bool exactV07AssemblyTarget = false;
                    foreach (AssemblyStage dirtyStage in stages)
                    {
                        if (SamePath(openFullPath, AssemblyPath(cad, dirtyStage.TargetStem)))
                        {
                            exactV07AssemblyTarget = true;
                            break;
                        }
                    }

                    if (dirtyAssembly != null && exactV07AssemblyTarget)
                    {
                        // All five exact V0.7 target assemblies are wholly
                        // reproducible SaveAs-Copy outputs of this generator.  The
                        // user requested a complete rebuild after interruption, so
                        // discard only the in-memory dirty state of these exact
                        // whitelisted targets.  No V0.4/V0.6 path can reach here.
                        safeCloseTitles.Add(document.GetTitle());
                        recentFailedBuildTitles.Add(document.GetTitle());
                        break;
                    }

                    throw new InvalidOperationException(
                        "Refusing to overwrite a dirty generated V0.7 target unless it is proven to be " +
                        "an incomplete clone: title=" + document.GetTitle() + "; path=" + openFullPath);
                }

                safeCloseTitles.Add(document.GetTitle());
                break;
            }

            document = next;
        }

        foreach (string title in safeCloseTitles)
        {
            cad.Application.CloseDoc(title);
            if (recentFailedBuildTitles.Contains(title))
            {
                cad.Log("V07_CLOSED_RECENT_FAILED_BUILD=" + title);
            }
            else if (recentGeneratedPartTitles.Contains(title))
            {
                cad.Log("V07_CLOSED_RECENT_GENERATED_PART=" + title);
            }
            else
            {
                cad.Log("V07_CLOSED_CLEAN_GENERATED_TARGET=" + title);
            }
        }
    }

    private static List<FileSnapshot> CaptureV04Snapshots(
        RackCadSession cad,
        List<AssemblyStage> stages)
    {
        List<FileSnapshot> snapshots = new List<FileSnapshot>();
        foreach (AssemblyStage stage in stages)
        {
            snapshots.Add(FileSnapshot.Capture(AssemblyPath(cad, stage.SourceStem)));
        }

        snapshots.Add(FileSnapshot.Capture(PartPath(cad, OldSideStem)));
        snapshots.Add(FileSnapshot.Capture(PartPath(cad, OldLegStem)));
        snapshots.Add(FileSnapshot.Capture(PartPath(cad, OldTravelLidStem)));
        snapshots.Add(FileSnapshot.Capture(PartPath(cad, OldRailStem)));
        snapshots.Add(FileSnapshot.Capture(PartPath(cad, OldBackPanelStem)));
        return snapshots;
    }

    private static void VerifyV04SnapshotsUnchanged(List<FileSnapshot> snapshots)
    {
        foreach (FileSnapshot snapshot in snapshots)
        {
            snapshot.RequireUnchanged();
        }
    }

    private static string CreateLightweightRail(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(LightweightRailStem);
        try
        {
            Body2 rail = cad.Box(0.0, 0.0, 0.0,
                RailFrontLength, RailHeight, RailFrontDepth + 0.02);
            rail = cad.Cut(rail,
                cad.Box(0.0, 0.0, RailThreadPocketStartZ,
                    RailFrontLength + 0.8, RailThreadPocketWidth,
                    RailThreadPocketDepth),
                "retained 6.4 mm stainless thread-strip pocket z3.9..6.1");

            double wallCentreY = RailHeight / 2.0 - RailTubeWall / 2.0;
            foreach (double sign in Signs())
            {
                rail = Unite(rail,
                    cad.Box(0.0, sign * wallCentreY, RailFrontDepth,
                        RailStructuralLength, RailTubeWall,
                        RailSpineDepth),
                    "1.5 mm closed-spine top/bottom wall");
            }
            rail = Unite(rail,
                cad.Box(0.0, 0.0, RailFrontDepth,
                    RailStructuralLength, RailHeight, RailTubeWall),
                "1.5 mm closed-spine front wall");
            rail = Unite(rail,
                cad.Box(0.0, 0.0,
                    RailFrontDepth + RailSpineDepth - RailTubeWall,
                    RailStructuralLength, RailHeight, RailTubeWall),
                "1.5 mm closed-spine rear wall");

            double bossCentreX =
                RailStructuralLength / 2.0 - RailEndBossLength / 2.0;
            foreach (double sign in Signs())
            {
                rail = Unite(rail,
                    cad.Box(sign * bossCentreX, 0.0, RailFrontDepth,
                        RailEndBossLength, RailHeight, RailSpineDepth),
                    "25 mm solid end boss retaining the M4 structural thread");
            }

            for (int index = 0; index < RailModuleHoleCount; index++)
            {
                double x = -RailFrontLength / 2.0 +
                    RailModulePitch / 2.0 + index * RailModulePitch;
                rail = cad.Cut(rail,
                    cad.Cylinder(x, 0.0, -0.3, 0.0, 0.0, 1.0,
                        RailModuleHoleDiameter, RailFrontDepth + 2.0),
                    "retained independent 104HP M3 module clearance position " +
                    (index + 1).ToString(CultureInfo.InvariantCulture));
            }

            foreach (double sign in Signs())
            {
                rail = cad.Cut(rail,
                    cad.Cylinder(sign * (RailStructuralLength / 2.0 + 0.3),
                        0.0, 16.0, -sign, 0.0, 0.0,
                        RailStructuralTapDiameter, RailEndBossLength + 0.8),
                    "retained end-facing M4 structural tap pilot in solid boss");
            }

            cad.AddBody(document, rail,
                "solid 104HP face plus 1.5 mm closed rear spine and 25 mm solid end bosses");
            cad.ApplyMaterial(document, "6061-T6 (SS)", NaturalAluminium);
            cad.Property(document, "Visible module standard",
                "104HP; 528.32 x 10 x 12 mm solid front; 104 independent diameter-3.2 module holes at 5.08 mm pitch");
            cad.Property(document, "Structural load path",
                "542 x 10 x 8 mm 1.5 mm closed-section spine; closed torsion path spans between 25 mm solid end bosses");
            cad.Property(document, "End fixing",
                "Both 25 mm end bosses retain diameter-3.3 M4 tap pilots at z16; the end-bearing and thread region is not pocketed");
            cad.Property(document, "Thread strip",
                "Existing 6 x 2 mm AISI 304 strip, pocket z3.9..6.1 and all 104 module positions remain unchanged");
            cad.Property(document, "Manufacturing route",
                "6061/6063 custom extrusion or equivalent machined prototype; wall, corner radius and extrusion tolerance require supplier DFM");
            cad.Property(document, "Validation boundary",
                "Closed-section inertia is an engineering geometry choice; rail proof load, screw pullout, vibration and fatigue remain physical tests");

            ValidatePart(document, 1,
                new Bounds(-271.0, -5.0, 0.0, 271.0, 5.0, 20.0),
                LightweightRailStem);
            string path = cad.SavePart(document, LightweightRailStem, true);
            cad.Log("V07_PART=" + path +
                ";solid_bodies=1;rail_closed_spine_wall_mm=1.5;solid_end_boss_mm=25");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static string CreateLightweightBackPanel(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(LightweightBackPanelStem);
        try
        {
            Body2 panel = cad.Box(0.0, 0.0,
                CaseDepth - BackSkinThickness,
                CaseWidth, CaseHeight, BackSkinThickness);
            panel = Unite(panel,
                cad.Box(0.0, 0.0,
                    CaseDepth - BackSkinThickness - BackVesaDoublerThickness,
                    BackVesaDoublerSize, BackVesaDoublerSize,
                    BackVesaDoublerThickness + 0.02),
                "central VESA doubler restoring a 2.0 mm local sheet stack");

            foreach (double xSign in Signs())
            {
                foreach (double ySign in Signs())
                {
                    panel = cad.Cut(panel,
                        cad.Cylinder(xSign * 50.0, ySign * 50.0,
                            CaseDepth - BackSkinThickness -
                                BackVesaDoublerThickness - 0.3,
                            0.0, 0.0, 1.0, 4.5,
                            BackSkinThickness + BackVesaDoublerThickness + 0.6),
                        "retained VESA 100 M4 clearance through local 2.0 mm stack");
                }
            }

            cad.AddBody(document, panel,
                "1.5 mm 5052 rear shear skin with central 160 x 160 mm local VESA doubler");
            cad.ApplyMaterial(document, "5052-H32", GraphiteAppearance);
            cad.Property(document, "Rear-sheet construction",
                "1.5 mm full 548 x 420 mm shear skin; central 160 x 160 x 0.5 mm doubler gives 2.0 mm local VESA stack");
            cad.Property(document, "VESA pattern",
                "VESA 100 four diameter-4.5 M4 clearances at x/y +/-50; no broad-back I/O or ventilation holes");
            cad.Property(document, "Retained reinforcement",
                "Existing two VESA bridges, two stiles and two rear crossbeams remain unchanged and contact the reinforced centre/skin");
            cad.Property(document, "Manufacturing route",
                "5052-H32 folded/pressed rear skin; final shallow anti-oil-can beads and edge attachment pitch require supplier DFM");
            cad.Property(document, "Validation boundary",
                "VESA pull, sheet buckling, corner drop and panel vibration require FEA plus prototype tests; CAD geometry alone is not certification");

            ValidatePart(document, 1,
                new Bounds(-274.0, -210.0, 108.0,
                    274.0, 210.0, 110.0), LightweightBackPanelStem);
            string path = cad.SavePart(document, LightweightBackPanelStem, true);
            cad.Log("V07_PART=" + path +
                ";solid_bodies=1;back_skin_mm=1.5;vesa_local_stack_mm=2.0");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static string CreateInnerSideFrame(RackCadSession cad, Stance stance60)
    {
        ModelDoc2 document = cad.NewPart(InnerSideStem);
        try
        {
            Point loadStop = DeployedLegLocalPointInCase(stance60,
                LoadStopLocalY, LoadStopLocalZ);
            // Keep the original 4 mm bearing stack wherever load enters the
            // plate, but remove one millimetre only from the broad low-stress
            // fields.  The 3 mm continuous core still closes the full case;
            // 4 mm edge bands, rail band, leg block and circular fastener
            // islands preserve all primary interfaces.
            Body2 side = cad.Box(0.0, 0.0, 0.0,
                InnerSideCoreThickness, CaseHeight, CaseDepth - ShellThickness);
            side = Unite(side,
                cad.Box(0.0, 0.0, 0.0,
                    InnerSideThickness, CaseHeight, 24.0),
                "4 mm continuous front rail-fixing band over all six M3/M4 pairs");
            side = Unite(side,
                cad.Box(0.0, 0.0, 96.0,
                    InnerSideThickness, CaseHeight, 12.0),
                "4 mm continuous rear enclosure shear band");
            side = Unite(side,
                cad.Box(0.0, -122.0, 18.0,
                    InnerSideThickness, 124.0, 72.0),
                "4 mm folding-leg pivot stop and rear-spacer load block");
            foreach (double edgeY in new double[] { -204.0, 204.0 })
            {
                side = Unite(side,
                    cad.Box(0.0, edgeY, 24.0,
                        InnerSideThickness, 12.0, 72.0),
                    "4 mm formed-edge-equivalent side band");
            }
            foreach (MountPoint mount in SpacerMounts)
            {
                side = Unite(side,
                    cad.Cylinder(-InnerSideThickness / 2.0,
                        mount.Y, mount.Z, 1.0, 0.0, 0.0, 26.0,
                        InnerSideThickness),
                    "4 mm diameter-26 spacer bearing island");
            }
            foreach (double catchY in new double[] { -150.0, 150.0 })
            {
                side = Unite(side,
                    cad.Cylinder(-InnerSideThickness / 2.0,
                        catchY, 55.0, 1.0, 0.0, 0.0, 26.0,
                        InnerSideThickness),
                    "4 mm transport-cover catch island");
            }

            foreach (double railY in RailPositions(cad))
            {
                side = SideHole(cad, side, railY, LocatorHoleZ,
                    LocatorClearanceDiameter, "retained independent M3 rail locator");
                side = SideHole(cad, side, railY, StructuralHoleZ,
                    StructuralClearanceDiameter, "retained independent M4 structural rail fixing");
            }

            foreach (double catchY in new double[] { -150.0, 150.0 })
            {
                side = SideHole(cad, side, catchY, 55.0, CoverCatchDiameter,
                    "retained internal transport-cover catch opening");
            }

            side = SideHole(cad, side, HingeCaseY, HingeCaseZ,
                PivotClearanceDiameter,
                "V0.7 inner double-shear pivot clearance; restored frame material surrounds hole");
            side = SideHole(cad, side, loadStop.Y, loadStop.Z,
                LoadStopClearanceDiameter,
                "V0.7 fixed hard-stop pin clearance; normal down-load reacts here");
            foreach (MountPoint mount in SpacerMounts)
            {
                side = SideHole(cad, side, mount.Y, mount.Z, SpacerHoleDiameter,
                    "flush-inside M4 outer-cheek spacer fixing");
            }

            foreach (double z in VentCentersZ)
            {
                foreach (double y in VentCentersY)
                {
                    double coreLength = VentLengthY - VentWidthZ;
                    side = cad.Cut(side,
                        cad.Box(0.0, y, z - VentWidthZ / 2.0,
                            InnerSideThickness + 0.8, coreLength, VentWidthZ),
                        "22 x 4 mm upper-forward side-vent core");
                    foreach (double sign in Signs())
                    {
                        side = SideHole(cad, side, y + sign * coreLength / 2.0,
                            z, VentWidthZ, "retained R2 side-vent end");
                    }
                }
            }

            cad.AddBody(document, side,
                "V0.7 3 mm continuous side core with 4 mm primary load paths and edge bands");
            cad.ApplyMaterial(document, "6061-T6 (SS)", NaturalAluminium);
            cad.Property(document, "Physical geometry",
                "3 mm continuous core; 4 mm rail, rear edge, case edge, leg and fastener islands; overall envelope 4 x 420 x 108 mm");
            cad.Property(document, "Module envelope",
                "542 mm internal clear width retained; no hinge, pin or fastener may project inward of x +/-271");
            cad.Property(document, "Double-shear pivot",
                "Diameter 10.2 mm at case y -129,z52; 16.8 mm full-stack axle envelope");
            cad.Property(document, "Hard-stop hole",
                "Diameter 10.2 at case y " + Format(loadStop.Y) + ",z " +
                Format(loadStop.Z) + "; calculated from deployed leg q=(-21,-38)");
            cad.Property(document, "Captive lock boundary",
                "Indexing plunger is mounted from the outer cheek and does not perforate the inner module-side plate");
            cad.Property(document, "Outer-cheek mounting",
                "Five diameter 5.5 M5 clearances per side; inner heads must be flush and supplier fasteners remain pending");
            cad.Property(document, "Cover-lock clearance",
                "Original diameter 12.2 openings y +/-150,z55 retained");
            cad.Property(document, "Rail and ventilation",
                "Six independent M3 plus six independent M4 rail holes; 2 x 4 upper-forward 22 x 4 R2 vents with structural separation");
            cad.Property(document, "Weight reduction boundary",
                "Only broad low-stress fields are 3 mm; pivot, hard stop, five spacer seats, rail band, cover catches and perimeter load bands remain 4 mm");

            ValidatePart(document, 1,
                new Bounds(-2.0, -210.0, 0.0, 2.0, 210.0, 108.0), InnerSideStem);
            string path = cad.SavePart(document, InnerSideStem, true);
            cad.Log("V07_PART=" + path + ";solid_bodies=1");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static string CreateStableLeg(RackCadSession cad, Stance stance60)
    {
        ModelDoc2 document = cad.NewPart(LegStem);
        try
        {
            Point fixedLock = DeployedLegLocalPointInCase(stance60,
                LockDeployLocalY, LockDeployLocalZ);
            double storageLockLocalY = fixedLock.Y - HingeCaseY;
            double storageLockLocalZ = fixedLock.Z - HingeCaseZ;

            Body2 arm = cad.Box(0.0, 4.5, -8.0,
                LegThickness, 175.0, ArmInPlaneWidth);
            Body2 footNeck = cad.Box(0.0, 93.5, -2.0,
                FootNeckThickness, 17.0, FootNeckWidth);
            arm = Unite(arm, footNeck,
                "locally thinned captured-foot neck and main arm union");
            Body2 root = cad.Cylinder(-LegThickness / 2.0,
                HingeLocalY, HingeLocalZ, 1.0, 0.0, 0.0,
                RootDiameter, LegThickness);
            Body2 metal = Unite(arm, root, "8 mm arm and diameter-48 root union");
            Body2 foldedLockBoss = cad.Cylinder(-LegThickness / 2.0,
                HingeLocalY + storageLockLocalY,
                HingeLocalZ + storageLockLocalZ,
                1.0, 0.0, 0.0,
                LockFoldedBossDiameter, LegThickness);
            metal = Unite(metal, foldedLockBoss,
                "diameter-20 folded indexing boss integrated with the pivot root");
            Body2 ear = cad.Box(0.0,
                HingeLocalY + (EarMinY + EarMaxY) / 2.0,
                HingeLocalZ + EarMinZ,
                LegThickness, EarMaxY - EarMinY, EarMaxZ - EarMinZ);
            metal = Unite(metal, ear, "7075 load ear union");

            metal = cad.Cut(metal,
                cad.Box(0.0,
                    HingeLocalY + (HeelMinY + EarMaxY) / 2.0,
                    HingeLocalZ + HeelMinZ,
                    LegThickness + 0.6, EarMaxY - HeelMinY,
                    HeelMaxZ - HeelMinZ),
                "full-depth steel-heel pocket plus open hard-stop sweep path");
            metal = cad.Cut(metal,
                cad.Cylinder(-LegThickness / 2.0 - 0.3,
                    HingeLocalY, HingeLocalZ, 1.0, 0.0, 0.0,
                    PivotClearanceDiameter, LegThickness + 0.6),
                "diameter 10.2 double-shear pivot and supplier-bushing clearance");
            metal = cad.Cut(metal,
                cad.Cylinder(-LegThickness / 2.0 - 0.3,
                    HingeLocalY + LockDeployLocalY, HingeLocalZ + LockDeployLocalZ,
                    1.0, 0.0, 0.0,
                    LockLegHoleDiameter, LegThickness + 0.6),
                "diameter 8.2 deployed captive-lock hole in the main arm at local q=(18,-9)");
            metal = cad.Cut(metal,
                cad.Cylinder(-LegThickness / 2.0 - 0.3,
                    HingeLocalY + storageLockLocalY,
                    HingeLocalZ + storageLockLocalZ,
                    1.0, 0.0, 0.0,
                    LockLegHoleDiameter, LegThickness + 0.6),
                "diameter 8.2 folded captive-lock hole aligned to the one fixed plunger");

            cad.AddBody(document, metal,
                "8 x 28 mm 7075-T6 arm, diameter-48 root, load ear and captured-foot neck");

            cad.ApplyMaterial(document, "7075-T6 (SN)", DarkAluminium);
            cad.Property(document, "Stable section",
                "7075-T6 Plate nominal 8 mm thickness x 28 mm in-plane arm width; 6 x 16 mm local captured-foot neck");
            cad.Property(document, "Root geometry",
                "Diameter 48 mm root around diameter 10.2 pivot plus a diameter-20 folded-lock boss; production blend and stress concentration remain DFM/FEA items");
            cad.Property(document, "Load ear",
                "Offset behind arm at relative y -37..-20,z -50..-10; pocket y -34..-20,z -50..-26 holds the steel heel and leaves an open pin path");
            cad.Property(document, "Captive indexing holes",
                "Diameter 8.2 at deployed q=(18,-9) in the main arm and folded q=(" +
                Format(storageLockLocalY) + "," + Format(storageLockLocalZ) +
                ") in a diameter-20 root boss; one fixed spring plunger locks both states");
            cad.Property(document, "Assembly position",
                "Fixed leg plane x +/-279.4 in folded and official 60-degree states; no axial pop-out");
            cad.Property(document, "Hinge and rubber-foot centre",
                "Local hinge y -75,z6; captured pad centre y110,z6; exact centre distance 185 mm");
            cad.Property(document, "Foot interface",
                "6 x 16 mm neck inserts 5 mm into the diameter-26 slotted rubber boot; compound, moulding and retention still require DFM");
            cad.Property(document, "Safety status",
                "Engineering geometry only; no physical bearing, pin shear, arm fatigue, stop load, anti-slip or loaded-CG validation completed");

            ValidatePart(document, 1,
                new Bounds(-4.0, -112.0, -44.0, 4.0, 102.0,
                    HingeLocalZ + storageLockLocalZ + LockFoldedBossDiameter / 2.0), LegStem);
            string path = cad.SavePart(document, LegStem, true);
            cad.Log("V07_PART=" + path + ";solid_bodies=1;material=7075-T6_Plate");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static string CreateOuterCheek(RackCadSession cad, Stance stance60)
    {
        ModelDoc2 document = cad.NewPart(OuterCheekStem);
        try
        {
            Point loadStop = DeployedLegLocalPointInCase(stance60,
                LoadStopLocalY, LoadStopLocalZ);
            Point fixedLock = DeployedLegLocalPointInCase(stance60,
                LockDeployLocalY, LockDeployLocalZ);
            double centreY = (OuterCheekMinY + OuterCheekMaxY) / 2.0;
            Body2 cheek = cad.Box(0.0, centreY, OuterCheekMainMinZ,
                OuterCheekThickness,
                OuterCheekMaxY - OuterCheekMinY,
                OuterCheekMaxZ - OuterCheekMainMinZ);
            Body2 lowerEarCover = cad.Box(0.0,
                (OuterCheekEarMinY + OuterCheekEarMaxY) / 2.0,
                OuterCheekEarMinZ,
                OuterCheekThickness,
                OuterCheekEarMaxY - OuterCheekEarMinY,
                OuterCheekEarMaxZ - OuterCheekEarMinZ);
            cheek = Unite(cheek, lowerEarCover,
                "lower local cheek ear covering folded load-ear projection");

            cheek = ThroughCheekHole(cad, cheek, HingeCaseY, HingeCaseZ,
                PivotClearanceDiameter, "outer double-shear pivot clearance");
            cheek = ThroughCheekHole(cad, cheek, -150.0, 55.0,
                CoverCatchDiameter, "rear cover-lock access/through clearance");
            cheek = ThroughCheekHole(cad, cheek, loadStop.Y, loadStop.Z,
                LoadStopClearanceDiameter,
                "fixed diameter-8 hard-stop pin clearance");
            cheek = ThroughCheekHole(cad, cheek, fixedLock.Y, fixedLock.Z,
                LockPlateHoleDiameter,
                "one fixed captive indexing-pin clearance for deployed and folded states");

            foreach (MountPoint mount in SpacerMounts)
            {
                cheek = ThroughCheekHole(cad, cheek, mount.Y, mount.Z,
                    SpacerHoleDiameter, "M4 spacer-stack outer clearance");
            }

            cheek = ThroughCheekHole(cad, cheek, FingerNotchY, FingerNotchZ,
                FingerNotchDiameter, "open-edge finger notch for folded foot extraction");

            cad.AddBody(document, cheek,
                "local 4 mm external cheek covering folded leg, captured foot and lower load ear");
            cad.ApplyMaterial(document, "6061-T6 (SS)", DarkAluminium);
            cad.Property(document, "Visual treatment",
                "Main strip y -184..72,z18..80 plus local lower ear y -166..-92,z-10..38; five M5 spacer stacks distribute pivot and stop loads");
            cad.Property(document, "Ventilation boundary",
                "Cheek ends at y72,z80; first 22 x 4 vent begins at y83,z90, leaving an 11 mm forward gap and 10 mm upper gap");
            cad.Property(document, "Double-shear stack",
                "4 mm outer cheek at centres x +/-285.8; inner faces x +/-283.8; outer faces x +/-287.8");
            cad.Property(document, "Hard stop and reverse lock",
                "Hard stop y " + Format(loadStop.Y) + ",z " + Format(loadStop.Z) +
                "; one fixed captive lock y " + Format(fixedLock.Y) + ",z " + Format(fixedLock.Z));
            cad.Property(document, "Cover-lock access",
                "Diameter 12.2 opening at case y -150,z55 retained through the local outer shell strip");
            cad.Property(document, "Pivot edge margin",
                "Diameter 10.2 pivot at y -129,z52 lies inside the diameter-48 root load region and reinforced outer cheek");
            cad.Property(document, "Manufacturing",
                "Five physical 8.8 mm spacers and flush M5 fasteners per side; fastener supplier and torque pending");

            ValidatePart(document, 1,
                new Bounds(-2.0, OuterCheekMinY, OuterCheekEarMinZ,
                    2.0, OuterCheekMaxY, OuterCheekMaxZ), OuterCheekStem);
            string path = cad.SavePart(document, OuterCheekStem, true);
            cad.Log("V07_PART=" + path + ";solid_bodies=1");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static string CreatePivotPin(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(PivotPinStem);
        try
        {
            Body2 pin = cad.Cylinder(-PivotPinLength / 2.0, 0.0, 0.0,
                1.0, 0.0, 0.0, PivotPinDiameter, PivotPinLength);
            cad.AddBody(document, pin,
                "flush diameter-10 double-shear pivot envelope; 16.8 mm grip");
            cad.ApplyMaterial(document, "AISI 304", StainlessAppearance);
            cad.Property(document, "Nominal grip",
                "16.8 mm from inner side-frame inside face to outer-cheek outside face");
            cad.Property(document, "Assembly origin",
                "Pin geometric centre at case x +/-279.4,y -129,z52");
            cad.Property(document, "Retention boundary",
                "Both ends must remain flush within x +/-287.8; full shank through both shear planes required; supplier retention not frozen");
            cad.Property(document, "Structural status",
                "Diameter is a CAD envelope only; double-shear, bearing, wear and fatigue calculations require selected hardware");

            ValidatePart(document, 1,
                new Bounds(-8.4, -5.0, -5.0, 8.4, 5.0, 5.0), PivotPinStem);
            string path = cad.SavePart(document, PivotPinStem, true);
            cad.Log("V07_PART=" + path + ";solid_bodies=1");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static string CreateSpacer(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(SpacerStem);
        try
        {
            Body2 spacer = cad.Cylinder(-SpacerLength / 2.0, 0.0, 0.0,
                1.0, 0.0, 0.0, SpacerOuterDiameter, SpacerLength);
            spacer = cad.Cut(spacer,
                cad.Cylinder(-SpacerLength / 2.0 - 0.3, 0.0, 0.0,
                    1.0, 0.0, 0.0, SpacerHoleDiameter, SpacerLength + 0.6),
                    "diameter 5.5 M5 through bore in 8.8 mm physical spacer");
            cad.AddBody(document, spacer,
                "diameter-12 x 8.8 mm physical outer-cheek spacer with M5 through bore");
            cad.ApplyMaterial(document, "7075-T6 (SN)", NaturalAluminium);
            cad.Property(document, "Physical role",
                "Five instances per side prevent the outer cheek from floating and define the 8.8 mm leg cavity");
            cad.Property(document, "Material and surface",
                "7075-T6 hard-anodized compression spacer; pivot axle, load stop and wear heel remain stainless steel");
            cad.Property(document, "Fastener boundary",
                "M5 through fastener, flush inner head and flush/countersunk outer retention required; supplier pending");

            ValidatePart(document, 1,
                new Bounds(-4.4, -6.0, -6.0, 4.4, 6.0, 6.0), SpacerStem);
            string path = cad.SavePart(document, SpacerStem, true);
            cad.Log("V07_PART=" + path + ";solid_bodies=1");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static string CreateLoadStopPin(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(LoadStopPinStem);
        try
        {
            Body2 pin = cad.Cylinder(-LoadStopLength / 2.0, 0.0, 0.0,
                1.0, 0.0, 0.0, LoadStopDiameter, LoadStopLength);
            cad.AddBody(document, pin,
                "diameter-10 x 16.8 full-shank hard-stop pin envelope");
            cad.ApplyMaterial(document, "AISI 304", StainlessAppearance);
            cad.Property(document, "Production material candidate",
                "17-4 PH stainless candidate; CAD uses AISI 304 appearance/material placeholder; final grade and heat treatment are not selected");
            cad.Property(document, "Load path",
                "Normal down-load reacts through the steel heel before the captive index pin; contact-normal moment arm 38.0 mm");
            cad.Property(document, "Full-shank requirement",
                "No thread may cross either shear plane or the 8.8 mm cavity; supplier retention and bearing calculations pending");

            ValidatePart(document, 1,
                new Bounds(-8.4, -5.0, -5.0, 8.4, 5.0, 5.0), LoadStopPinStem);
            string path = cad.SavePart(document, LoadStopPinStem, true);
            cad.Log("V07_PART=" + path + ";solid_bodies=1;production_material_unselected=true");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static string CreateLockPin(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(LockPinStem);
        try
        {
            Body2 pin = cad.Cylinder(-LockPinLength / 2.0, 0.0, 0.0,
                1.0, 0.0, 0.0, LockPinDiameter, LockPinLength);
            cad.AddBody(document, pin,
                "diameter-8 x 12 mm captive indexing-pin engaged envelope");
            cad.ApplyMaterial(document, "AISI 304", StainlessAppearance);
            cad.Property(document, "Function",
                "Spring-return indexing plunger locks folded and deployed states; must not carry normal downward operating load");
            cad.Property(document, "Positions",
                "One fixed case position aligns with two real-metal leg holes: deployed q=(18,-9) in the main arm and folded q calculated in the diameter-20 root boss");
            cad.Property(document, "Retention boundary",
                "GN 817-class captive spring plunger is the dimensional principle only; final supplier, boss/thread and accidental-release protection require DFM");

            ValidatePart(document, 1,
                new Bounds(-6.0, -4.0, -4.0, 6.0, 4.0, 4.0), LockPinStem);
            string path = cad.SavePart(document, LockPinStem, true);
            cad.Log("V07_PART=" + path + ";solid_bodies=1;reverse_lock_only=true");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static string CreateHeelInsert(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(HeelInsertStem);
        try
        {
            Body2 heel = cad.Box(0.0, 0.0, -12.0, 8.0, 8.0, 24.0);
            cad.AddBody(document, heel,
                "8 x 8 x 24 mm steel heel insert; origin at body centre");
            cad.ApplyMaterial(document, "AISI 304", StainlessAppearance);
            cad.Property(document, "Leg-local placement",
                "Centre relative pivot q=(-30,-38), or leg-part absolute y -105,z -32");
            cad.Property(document, "Bearing interface",
                "Occupies relative y -34..-26,z -50..-26; stop centre q=(-21,-38) is tangent to y=-26 while the adjacent ear pocket remains open");
            cad.Property(document, "Production retention",
                "Final mechanically keyed capture and supplier process require DFM; this part is not declared adhesive-bonded completion");

            ValidatePart(document, 1,
                new Bounds(-4.0, -4.0, -12.0, 4.0, 4.0, 12.0), HeelInsertStem);
            string path = cad.SavePart(document, HeelInsertStem, true);
            cad.Log("V07_PART=" + path + ";solid_bodies=1;mechanical_keying_DFM_pending=true");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static string CreateFootPad(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(FootPadStem);
        try
        {
            Body2 pad = cad.Cylinder(-FootPadAxialLength / 2.0, 0.0, 0.0,
                1.0, 0.0, 0.0, FootPadDiameter, FootPadAxialLength);
            pad = cad.Cut(pad,
                cad.Box(0.0, -10.25, -8.2,
                    FootNeckThickness + 0.4, FootSocketDepth, FootNeckWidth + 0.4),
                "open-ended captured-foot socket with 0.2 mm nominal clearance");
            cad.AddBody(document, pad,
                "diameter-26 x 8.4 slotted rubber boot; origin at crown centre");
            cad.ApplyMaterial(document, "NEOPRENE", ElastomerAppearance);
            cad.Property(document, "Contact geometry",
                "Centre is exactly 185 mm from pivot and 13 mm above desk at 60 degrees; radius 13 gives lowest point Y=0");
            cad.Property(document, "Metal interface",
                "6 x 16 mm metal neck enters the open socket by 5 mm with 0.2 mm nominal clearance per socket face");
            cad.Property(document, "Production retention",
                "Moulded-over boot or equivalent positive capture is required; rubber compound, friction and supplier require DFM");

            ValidatePart(document, 1,
                new Bounds(-4.2, -13.0, -13.0, 4.2, 13.0, 13.0), FootPadStem);
            string path = cad.SavePart(document, FootPadStem, true);
            cad.Log("V07_PART=" + path + ";solid_bodies=1;material=NEOPRENE");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static string CreateTransportLid(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(TravelLidStem);
        try
        {
            double cavityWidth = OverallWidth + 1.0;
            double cavityHeight = CaseHeight + 1.0;
            double externalWidth = cavityWidth + 2.0 * TravelLidThickness;
            double externalHeight = cavityHeight + 2.0 * TravelLidThickness;
            double sideReturnCentreX = cavityWidth / 2.0 + TravelLidThickness / 2.0;
            double reliefCentreY = (TravelReliefMinY + TravelReliefMaxY) / 2.0;

            Body2 face = cad.Box(0.0, 0.0, TravelLidFrontZ - TravelLidThickness,
                externalWidth, externalHeight, TravelLidThickness);
            foreach (double ribY in new double[] { -120.0, 120.0 })
            {
                Body2 rib = cad.Box(0.0, ribY,
                    TravelLidFrontZ - TravelLidThickness - TravelLidBeadDepth,
                    externalWidth - 40.0, 8.0, TravelLidBeadDepth);
                face = Unite(face, rib,
                    "shallow external formed-bead equivalent for anti-drum stiffness");
            }
            cad.AddBody(document, face,
                "V0.7 deep travel-lid face with two shallow external anti-drum ribs");

            foreach (double sign in Signs())
            {
                Body2 sideReturn = cad.Box(
                    sign * sideReturnCentreX, 0.0, TravelLidFrontZ,
                    TravelLidThickness, cavityHeight, TravelLidSkirtDepth);
                sideReturn = cad.Cut(sideReturn,
                    cad.Box(
                        sign * sideReturnCentreX,
                        reliefCentreY,
                        TravelReliefMinZ,
                        TravelLidThickness + 1.0,
                        TravelReliefMaxY - TravelReliefMinY,
                        TravelReliefMaxZ - TravelReliefMinZ),
                    (sign < 0.0 ? "left" : "right") +
                    " V0.7 folded-kickstand side-return relief");
                cad.AddBody(document, sideReturn,
                    sign < 0.0
                        ? "Left lid return with folded-stand relief"
                        : "Right lid return with folded-stand relief");

                cad.AddBody(document,
                    cad.Box(0.0,
                        sign * (cavityHeight / 2.0 + TravelLidThickness / 2.0),
                        TravelLidFrontZ,
                        externalWidth, TravelLidThickness, TravelLidSkirtDepth),
                    sign < 0.0 ? "Lower lid return" : "Upper lid return");
            }

            cad.ApplyMaterial(document, "5052-H32", GraphiteAppearance);
            cad.Property(document, "Front patch clearance", "70 mm");
            cad.Property(document, "Construction",
                "1.2 mm 5052-H32 folded-panel concept with deep returns and two 1.2 mm external formed-bead equivalents; final bends and latch doublers require DFM");
            cad.Property(document, "Folded-stand relief",
                "Bilateral side-return notch y[-170,76], z[-2,15] mm; clears the offset folded load ear while retaining a continuous lower return capture wall");
            cad.Property(document, "Fit envelope",
                "576.6 mm inside width provides 0.5 mm nominal clearance per side over the 575.6 mm reinforced case width");
            cad.Property(document, "Strength retained while reducing mass",
                "82 mm deep perimeter returns, two anti-drum beads and full lower capture walls are retained; four latch locations require local 1.5 mm doublers at supplier DFM");
            cad.Property(document, "Source preservation",
                "Independent V0.7 part; DeepTravelLid_70mmClearance.SLDPRT remains unchanged");

            ValidatePart(document, 5,
                new Bounds(-289.5, -211.7, -72.4, 289.5, 211.7, 12.0),
                TravelLidStem);
            string path = cad.SavePart(document, TravelLidStem, true);
            cad.Log("V07_PART=" + path +
                ";solid_bodies=5;bilateral_folded_leg_relief=y[-170,76],z[-2,15]");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static string CreateHandleSpreader(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(HandleSpreaderStem);
        try
        {
            Body2 spreader = cad.Box(0.0, 0.0, 40.0, 128.0, 4.0, 30.0);
            foreach (double x in new double[] { -55.0, 55.0 })
            {
                foreach (double z in new double[] { 50.0, 61.0 })
                {
                    spreader = cad.Cut(spreader,
                        cad.Cylinder(x, -2.3, z, 0.0, 1.0, 0.0, 5.2, 4.6),
                        "M5 carry-handle spreader clearance");
                }
            }
            cad.AddBody(document, spreader,
                "128 x 30 x 4 mm internal carry-handle load spreader");
            cad.ApplyMaterial(document, "6061-T6 (SS)", NaturalAluminium);
            cad.Property(document, "Load path",
                "Four M5 handle fasteners clamp the 2 mm upper sheet against this 4 mm 6061-T6 spreader");
            cad.Property(document, "Assembly position",
                "Case-fixed at x0,y206,z0; plate touches the upper-edge inner face y208 over x +/-64 without entering either neighbouring cassette opening");
            cad.Property(document, "Validation boundary",
                "Purchased handle rating, fastener grade, thread engagement, pull test and fatigue remain production acceptance items");

            ValidatePart(document, 1,
                new Bounds(-64.0, -2.0, 40.0, 64.0, 2.0, 70.0), HandleSpreaderStem);
            string path = cad.SavePart(document, HandleSpreaderStem, true);
            cad.Log("V07_PART=" + path + ";solid_bodies=1;handle_load_spreader=true");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static void BuildV07Assembly(
        RackCadSession cad,
        AssemblyStage stage,
        Stance stance,
        PartPaths parts)
    {
        string sourcePath = AssemblyPath(cad, stage.SourceStem);
        ModelDoc2 document = CloneAssemblyCopy(cad, sourcePath, stage.TargetStem);
        AssemblyDoc assembly = document as AssemblyDoc;
        if (assembly == null)
        {
            throw new InvalidOperationException("The cloned V0.7 target is not an assembly: " + stage.TargetStem);
        }

        try
        {
            List<Component2> initialComponents = TopLevelComponents(assembly);
            Require(initialComponents.Count == stage.SourceComponentCount,
                "Unexpected exact V0.4 source count for " + stage.SourceStem + "; expected " +
                stage.SourceComponentCount.ToString(CultureInfo.InvariantCulture) + ", actual " +
                initialComponents.Count.ToString(CultureInfo.InvariantCulture));

            Dictionary<string, int> unchangedBefore = CaptureUnchangedSignatures(initialComponents,
                OldSideStem, OldLegStem, OldTravelLidStem,
                OldRailStem, OldBackPanelStem);
            Dictionary<int, double[]> sideTransforms = CaptureSignedTransforms(
                initialComponents, OldSideStem, "V0.4 side frames");

            ReplaceExactOccurrences(document, assembly,
                PartPath(cad, OldRailStem), parts.LightweightRail, 6,
                "closed-section lightweight structural rail");
            ReplaceExactlyOne(document, assembly,
                PartPath(cad, OldBackPanelStem), parts.LightweightBackPanel,
                "lightweight rear shear skin with local VESA doubler");

            ReplaceExactlyTwo(document, assembly, PartPath(cad, OldSideStem), parts.InnerSide,
                "inner side frame");
            RestoreSignedTransforms(cad, document, assembly, InnerSideStem,
                sideTransforms, "V0.7 inner side frame");

            ReplaceExactlyTwo(document, assembly, PartPath(cad, OldLegStem), parts.Leg,
                "folding leg");
            PositionLegs(cad, document, assembly, stance);
            if (stage.IncludesClosedLid)
            {
                ReplaceExactlyOne(document, assembly,
                    PartPath(cad, OldTravelLidStem), parts.TravelLid,
                    "V0.7 transport lid with folded-stand relief");
            }
            AddStableKickstandHardware(cad, document, assembly, stance, parts);
            if (stage.IncludesDisplayLid)
            {
                AddTransformed(cad, document, assembly, RequireMathUtility(cad),
                    parts.TravelLid, "V07 detached travel lid for final presentation",
                    IdentityTransform(ShowcaseLidX, ShowcaseLidY, ShowcaseLidZ));
            }

            document.Extension.ForceRebuildAll();
            Require(document.ForceRebuild3(false),
                "SOLIDWORKS could not rebuild " + stage.TargetStem);
            assembly.UpdateBox();

            ValidateAssembly(cad, stage, stance, document, assembly, unchangedBefore);
            WriteAssemblyProperties(cad, stage, stance, document);
            string saved = cad.SaveAssembly(document, stage.TargetStem, true);
            Require(SamePath(saved, AssemblyPath(cad, stage.TargetStem)),
                "The V0.7 native assembly save escaped its exact target path.");

            ValidateAssembly(cad, stage, stance, document, assembly, unchangedBefore);
            cad.Log("V07_ASSEMBLY=" + saved + ";top_level_components=" +
                (stage.SourceComponentCount + 23 + (stage.IncludesDisplayLid ? 1 : 0))
                    .ToString(CultureInfo.InvariantCulture));

            // STEP export and the post-export readback above can mark the native
            // assembly as needing regeneration.  Make the native SLDASM the last
            // written product and prove it is clean before this owned document is
            // closed.  This prevents warning 32 (NeedsRegen) on the next open.
            string finalNative = cad.SaveAssembly(document, stage.TargetStem, false);
            Require(SamePath(finalNative, AssemblyPath(cad, stage.TargetStem)),
                "The final clean V0.7 native save escaped its exact target path.");
            Require(!document.GetSaveFlag(),
                "The final V0.7 native assembly remains dirty after its post-validation save: " +
                stage.TargetStem);
            cad.Log("V07_FINAL_NATIVE_CLEAN=" + finalNative + ";save_flag=false");
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static ModelDoc2 CloneAssemblyCopy(
        RackCadSession cad,
        string sourcePath,
        string targetStem)
    {
        ModelDoc2 source = OpenExactAssembly(cad, sourcePath);
        bool sourceWasDirty = source.GetSaveFlag();
        string sourceHash = FileSnapshot.HashFile(sourcePath);
        string targetPath = AssemblyPath(cad, targetStem);
        ModelDoc2 targetOpen = cad.Application.GetOpenDocumentByName(targetPath) as ModelDoc2;
        if (targetOpen != null)
        {
            if (targetOpen.GetSaveFlag())
            {
                throw new InvalidOperationException(
                    "Refusing to overwrite an open V0.7 target with unsaved changes: " + targetPath);
            }

            cad.Application.CloseDoc(targetOpen.GetTitle());
        }

        ActivateExact(cad, source, sourcePath);
        int errors = 0;
        int warnings = 0;
        int options = (int)swSaveAsOptions_e.swSaveAsOptions_Silent |
            (int)swSaveAsOptions_e.swSaveAsOptions_Copy;
        bool copied = source.Extension.SaveAs(
            targetPath,
            (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
            options,
            null,
            ref errors,
            ref warnings);

        Require(copied && errors == 0 && File.Exists(targetPath) &&
                new FileInfo(targetPath).Length > 0,
            "Cannot create the exact independent V0.7 assembly copy; errors=" +
            errors.ToString(CultureInfo.InvariantCulture) + "; warnings=" +
            warnings.ToString(CultureInfo.InvariantCulture) + "; target=" + targetPath);
        Require(SamePath(Path.GetFullPath(source.GetPathName()), sourcePath),
            "Save-as-copy unexpectedly changed the active V0.4 source document identity.");
        if (source.GetSaveFlag() != sourceWasDirty)
        {
            cad.Log("WARNING: V0.4 source became dirty in memory after read/open-copy; " +
                "it will not be saved; source=" + sourcePath);
        }
        Require(string.Equals(sourceHash, FileSnapshot.HashFile(sourcePath), StringComparison.Ordinal),
            "The V0.4 source bytes changed during V0.7 cloning: " + sourcePath);
        cad.Log("V07_SOURCE_DIRTY_PRESERVED=" + sourceWasDirty.ToString(CultureInfo.InvariantCulture) +
            "; source_dirty_preserved=true; source=" + sourcePath);

        ModelDoc2 target = OpenExactAssembly(cad, targetPath);
        cad.Log("V07_CLONED_COPY=" + sourcePath + " -> " + targetPath +
            "; warnings=" + warnings.ToString(CultureInfo.InvariantCulture));
        return target;
    }

    private static void ReplaceExactlyTwo(
        ModelDoc2 document,
        AssemblyDoc assembly,
        string oldExactPath,
        string replacementExactPath,
        string context)
    {
        RequireProjectFilePath(oldExactPath);
        RequireProjectFilePath(replacementExactPath);
        int replacements = 0;

        while (true)
        {
            Component2 oldComponent = null;
            foreach (Component2 component in TopLevelComponents(assembly))
            {
                if (SameComponentPath(component, oldExactPath))
                {
                    oldComponent = component;
                    break;
                }
            }

            if (oldComponent == null)
            {
                break;
            }

            replacements++;
            Require(replacements <= 2,
                "More than two exact V0.4 " + context + " occurrences were found.");
            document.ClearSelection2(true);
            Require(oldComponent.Select4(false, null, false),
                "Cannot select exact old " + context + " for replacement.");
            Require(assembly.ReplaceComponents(replacementExactPath, string.Empty, false, true),
                "SOLIDWORKS refused exact " + context + " replacement.");
            document.ClearSelection2(true);
        }

        Require(replacements == 2,
            "Expected exactly two exact V0.4 " + context + " occurrences; actual " +
            replacements.ToString(CultureInfo.InvariantCulture));
        Require(CountStem(TopLevelComponents(assembly),
                Path.GetFileNameWithoutExtension(replacementExactPath)) == 2,
            "Expected exactly two V0.7 replacement " + context + " occurrences.");
    }

    private static void ReplaceExactOccurrences(
        ModelDoc2 document,
        AssemblyDoc assembly,
        string oldExactPath,
        string replacementExactPath,
        int expectedOccurrences,
        string context)
    {
        RequireProjectFilePath(oldExactPath);
        RequireProjectFilePath(replacementExactPath);
        Require(expectedOccurrences > 0,
            "A positive exact replacement count is required for " + context);
        int replacements = 0;

        while (true)
        {
            Component2 oldComponent = null;
            foreach (Component2 component in TopLevelComponents(assembly))
            {
                if (SameComponentPath(component, oldExactPath))
                {
                    oldComponent = component;
                    break;
                }
            }

            if (oldComponent == null)
            {
                break;
            }

            replacements++;
            Require(replacements <= expectedOccurrences,
                "More than " + expectedOccurrences.ToString(CultureInfo.InvariantCulture) +
                " exact source occurrences were found for " + context);
            document.ClearSelection2(true);
            Require(oldComponent.Select4(false, null, false),
                "Cannot select exact source occurrence for " + context);
            Require(assembly.ReplaceComponents(
                    replacementExactPath, string.Empty, false, true),
                "SOLIDWORKS refused exact replacement for " + context);
            document.ClearSelection2(true);
        }

        Require(replacements == expectedOccurrences,
            "Expected " + expectedOccurrences.ToString(CultureInfo.InvariantCulture) +
            " exact source occurrences for " + context + "; actual " +
            replacements.ToString(CultureInfo.InvariantCulture));
        Require(CountStem(TopLevelComponents(assembly),
                Path.GetFileNameWithoutExtension(replacementExactPath)) ==
                expectedOccurrences,
            "Replacement occurrence count is wrong for " + context);
    }

    private static void ReplaceExactlyOne(
        ModelDoc2 document,
        AssemblyDoc assembly,
        string oldExactPath,
        string replacementExactPath,
        string context)
    {
        RequireProjectFilePath(oldExactPath);
        RequireProjectFilePath(replacementExactPath);
        List<Component2> before = TopLevelComponents(assembly);
        Require(CountExactPath(before, oldExactPath) == 1,
            "Expected exactly one source " + context + " occurrence before replacement.");
        Require(CountStem(before,
                Path.GetFileNameWithoutExtension(replacementExactPath)) == 0,
            "The V0.7 replacement " + context + " unexpectedly already exists.");

        Component2 oldComponent = null;
        foreach (Component2 component in before)
        {
            if (SameComponentPath(component, oldExactPath))
            {
                oldComponent = component;
                break;
            }
        }

        Require(oldComponent != null,
            "Cannot locate the exact source " + context + " for replacement.");
        document.ClearSelection2(true);
        Require(oldComponent.Select4(false, null, false),
            "Cannot select the exact source " + context + " for replacement.");
        Require(assembly.ReplaceComponents(replacementExactPath, string.Empty, false, true),
            "SOLIDWORKS refused exact " + context + " replacement.");
        document.ClearSelection2(true);

        List<Component2> after = TopLevelComponents(assembly);
        Require(CountExactPath(after, oldExactPath) == 0,
            "The source " + context + " remains after exact replacement.");
        Require(CountStem(after,
                Path.GetFileNameWithoutExtension(replacementExactPath)) == 1,
            "Expected exactly one V0.7 replacement " + context + " occurrence.");
    }

    private static void RestoreSignedTransforms(
        RackCadSession cad,
        ModelDoc2 document,
        AssemblyDoc assembly,
        string stem,
        Dictionary<int, double[]> transforms,
        string context)
    {
        MathUtility utility = RequireMathUtility(cad);
        int count = 0;
        foreach (Component2 component in TopLevelComponents(assembly))
        {
            if (!SameStem(component, stem))
            {
                continue;
            }

            int sign = ReadTransform(component, context)[9] < 0.0 ? -1 : 1;
            double[] requested;
            Require(transforms.TryGetValue(sign, out requested),
                "No preserved signed source transform exists for " + context);
            double[] corrected = (double[])requested.Clone();
            corrected[9] = sign * InnerSideCentreX / 1000.0;
            ApplyComponentTransform(document, assembly, utility, component, corrected, context);
            count++;
        }

        Require(count == 2, "Exactly two signed components are required for " + context);
    }

    private static void PositionLegs(
        RackCadSession cad,
        ModelDoc2 document,
        AssemblyDoc assembly,
        Stance stance)
    {
        MathUtility utility = RequireMathUtility(cad);
        int count = 0;
        foreach (Component2 component in TopLevelComponents(assembly))
        {
            if (!SameStem(component, LegStem))
            {
                continue;
            }

            int sign = ReadTransform(component, "replacement V0.7 leg")[9] < 0.0 ? -1 : 1;
            double[] requested = stance == null
                ? IdentityTransform(sign * LegPlaneX, FoldedY, FoldedZ)
                : DeployedLegTransform(stance, sign);
            ApplyComponentTransform(document, assembly, utility, component, requested,
                stance == null ? "folded V0.7 stable leg" : "deployed V0.7 stable leg");
            VerifyLegContact(component, requested, stance, sign);
            count++;
        }

        Require(count == 2, "Every V0.7 assembly must contain exactly two positioned V0.7 legs.");
    }

    private static void AddStableKickstandHardware(
        RackCadSession cad,
        ModelDoc2 document,
        AssemblyDoc assembly,
        Stance stance,
        PartPaths parts)
    {
        MathUtility utility = RequireMathUtility(cad);
        Stance deployment60 = CalculateStance(60.0);
        Point loadStopCase = DeployedLegLocalPointInCase(deployment60,
            LoadStopLocalY, LoadStopLocalZ);
        Point fixedLockCase = DeployedLegLocalPointInCase(deployment60,
            LockDeployLocalY, LockDeployLocalZ);
        foreach (int sign in IntegerSigns())
        {
            AddTransformed(cad, document, assembly, utility,
                parts.OuterCheek, "V07 " + SideName(sign) + " captured outer structural cheek",
                FixedCaseTransform(stance, sign * OuterCheekCentreX, 0.0, 0.0));

            AddTransformed(cad, document, assembly, utility,
                parts.PivotPin, "V07 " + SideName(sign) + " full-stack double-shear pivot pin",
                FixedCaseTransform(stance, sign * LegPlaneX, HingeCaseY, HingeCaseZ));

            for (int index = 0; index < SpacerMounts.Length; index++)
            {
                MountPoint mount = SpacerMounts[index];
                AddTransformed(cad, document, assembly, utility,
                    parts.Spacer,
                    "V07 " + SideName(sign) + " physical cheek spacer " +
                    (index + 1).ToString(CultureInfo.InvariantCulture),
                    FixedCaseTransform(stance, sign * LegPlaneX, mount.Y, mount.Z));
            }

            AddTransformed(cad, document, assembly, utility,
                parts.LoadStopPin, "V07 " + SideName(sign) + " fixed positive hard-stop pin",
                FixedCaseTransform(stance, sign * LegPlaneX,
                    loadStopCase.Y, loadStopCase.Z));

            AddTransformed(cad, document, assembly, utility,
                parts.LockPin, "V07 " + SideName(sign) +
                    " fixed captive indexing pin in the engaged state",
                FixedCaseTransform(stance, sign * LockPinCentreX,
                    fixedLockCase.Y, fixedLockCase.Z));

            AddTransformed(cad, document, assembly, utility,
                parts.HeelInsert, "V07 " + SideName(sign) + " mechanically keyed heel insert",
                LegAttachedTransform(stance, sign, HeelCentreLocalY, HeelCentreLocalZ));

            AddTransformed(cad, document, assembly, utility,
                parts.FootPad, "V07 " + SideName(sign) + " independent rubber round foot",
                LegAttachedTransform(stance, sign, FootPadLocalY, FootPadLocalZ));
        }

        AddTransformed(cad, document, assembly, utility,
            parts.HandleSpreader, "V07 internal carry-handle load spreader",
            FixedCaseTransform(stance, 0.0, 206.0, 0.0));
    }

    private static Component2 AddTransformed(
        RackCadSession cad,
        ModelDoc2 document,
        AssemblyDoc assembly,
        MathUtility utility,
        string partPath,
        string label,
        double[] transform)
    {
        Component2 component = cad.AddComponent(document, partPath, label, 0.0, 0.0, 0.0);
        ApplyComponentTransform(document, assembly, utility, component, transform, label);
        return component;
    }

    private static void ValidateAssembly(
        RackCadSession cad,
        AssemblyStage stage,
        Stance stance,
        ModelDoc2 document,
        AssemblyDoc assembly,
        Dictionary<string, int> unchangedBefore)
    {
        List<Component2> components = TopLevelComponents(assembly);
        int expected = stage.SourceComponentCount + 23 +
            (stage.IncludesDisplayLid ? 1 : 0);
        Require(components.Count == expected,
            "V0.7 top-level count mismatch for " + stage.TargetStem + "; expected " +
            expected.ToString(CultureInfo.InvariantCulture) + ", actual " +
            components.Count.ToString(CultureInfo.InvariantCulture));

        Require(CountStem(components, OldSideStem) == 0 && CountStem(components, OldLegStem) == 0,
            "A legacy V0.4 side frame or axial-popout leg remains in " + stage.TargetStem);
        Require(CountStem(components, OldRailStem) == 0 &&
                CountStem(components, OldBackPanelStem) == 0,
            "A solid-spine V0.4 rail or uniform 2 mm back panel remains in " +
            stage.TargetStem);
        foreach (string legacyStem in LegacyV05KickstandStems)
        {
            Require(CountStem(components, legacyStem) == 0,
                "A legacy V0.5 kickstand component remains: " + legacyStem);
        }
        Require(CountStem(components, InnerSideStem) == 2, "V0.7 inner-side count must be two.");
        Require(CountStem(components, LegStem) == 2, "V0.7 leg count must be two.");
        Require(CountStem(components, OuterCheekStem) == 2, "V0.7 outer-cheek count must be two.");
        Require(CountStem(components, PivotPinStem) == 2, "V0.7 pivot-pin count must be two.");
        Require(CountStem(components, SpacerStem) == 10, "V0.7 physical spacer count must be ten.");
        Require(CountStem(components, LoadStopPinStem) == 2, "V0.7 hard-stop count must be two.");
        Require(CountStem(components, LockPinStem) == 2, "V0.7 reverse-lock count must be two.");
        Require(CountStem(components, HeelInsertStem) == 2, "V0.7 heel-insert count must be two.");
        Require(CountStem(components, FootPadStem) == 2, "V0.7 rubber-foot count must be two.");
        Require(CountStem(components, HandleSpreaderStem) == 1,
            "V0.7 carry-handle spreader count must be one.");
        Require(CountStem(components, LightweightRailStem) == 6,
            "V0.7 closed-section lightweight rail count must be six.");
        Require(CountStem(components, LightweightBackPanelStem) == 1,
            "V0.7 lightweight rear skin count must be one.");
        Require(CountStem(components, OldTravelLidStem) == 0,
            "The protected V0.4 travel lid must not remain in a V0.7 target.");
        Require(CountStem(components, TravelLidStem) ==
                ((stage.IncludesClosedLid || stage.IncludesDisplayLid) ? 1 : 0),
            "The V0.7 relieved travel-lid count does not match the assembly stage.");

        Dictionary<string, int> unchangedAfter = CaptureUnchangedSignatures(components,
            InnerSideStem, LegStem, OuterCheekStem, PivotPinStem, SpacerStem,
            LoadStopPinStem, LockPinStem, HeelInsertStem, FootPadStem,
            OldTravelLidStem, TravelLidStem, HandleSpreaderStem,
            OldRailStem, LightweightRailStem,
            OldBackPanelStem, LightweightBackPanelStem);
        Require(DictionaryEqual(unchangedBefore, unchangedAfter),
            "A non-kickstand V0.4 component path or transform changed while producing " + stage.TargetStem);

        int leftSides = 0;
        int rightSides = 0;
        foreach (Component2 component in components)
        {
            if (!IsV07SideHardware(component))
            {
                continue;
            }

            double[] transform = ReadTransform(component, "V0.7 side hardware bounds");
            int sign = transform[9] < 0.0 ? -1 : 1;
            double[] box = component.GetBox(false, false) as double[];
            Require(box != null && box.Length >= 6,
                "SOLIDWORKS did not expose V0.7 side-hardware bounds for " + component.Name2);
            double minX = box[0] * 1000.0;
            double maxX = box[3] * 1000.0;
            if (sign < 0)
            {
                Require(maxX <= -InteriorClearWidth / 2.0 + GeometryTolerance,
                    "Left V0.7 side hardware invades the 542 mm internal module width: " + component.Name2);
                leftSides++;
            }
            else
            {
                Require(minX >= InteriorClearWidth / 2.0 - GeometryTolerance,
                    "Right V0.7 side hardware invades the 542 mm internal module width: " + component.Name2);
                rightSides++;
            }
            Require(minX >= -OuterHalfWidth - GeometryTolerance &&
                    maxX <= OuterHalfWidth + GeometryTolerance,
                "V0.7 side hardware exceeds the 575.6 mm width: " + component.Name2);
        }

        Require(leftSides == 13 && rightSides == 13,
            "Each V0.7 side must contain inner frame, leg, cheek, pivot, five spacers, stop, lock, heel and foot.");

        double productMinX = double.PositiveInfinity;
        double productMaxX = double.NegativeInfinity;
        int productBoundsCount = 0;
        foreach (Component2 component in components)
        {
            string componentPath = component.GetPathName();
            string componentStem = string.IsNullOrWhiteSpace(componentPath)
                ? TitleStem(component.Name2)
                : Path.GetFileNameWithoutExtension(componentPath);
            if (componentStem.StartsWith(
                    "DesktopReferenceSurface_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if ((stage.IncludesClosedLid || stage.IncludesDisplayLid) &&
                string.Equals(componentStem, TravelLidStem, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            double[] productBox = component.GetBox(false, false) as double[];
            Require(productBox != null && productBox.Length >= 6,
                "No product-component bounding box is available for " + component.Name2);
            productMinX = Math.Min(productMinX, productBox[0] * 1000.0);
            productMaxX = Math.Max(productMaxX, productBox[3] * 1000.0);
            productBoundsCount++;
        }

        Require(productBoundsCount > 0,
            "No physical product components remain after excluding desktop references.");
        RequireClose(productMinX, -OuterHalfWidth, GeometryTolerance,
            "V0.7 physical-product nominal minimum X");
        RequireClose(productMaxX, OuterHalfWidth, GeometryTolerance,
            "V0.7 physical-product nominal maximum X");

        VerifyLegInstances(components, stance);
        VerifyStableHardwareTransforms(components, stance);
        Require(document.ForceRebuild3(false),
            "Final V0.7 assembly rebuild readback failed for " + stage.TargetStem);
    }

    private static void VerifyLegInstances(List<Component2> components, Stance stance)
    {
        int count = 0;
        foreach (Component2 component in components)
        {
            if (!SameStem(component, LegStem))
            {
                continue;
            }

            double[] transform = ReadTransform(component, "V0.7 leg verification");
            int sign = transform[9] < 0.0 ? -1 : 1;
            VerifyLegContact(component, transform, stance, sign);
            count++;
        }
        Require(count == 2, "Two V0.7 leg transforms must be verified.");
    }

    private static void VerifyStableHardwareTransforms(List<Component2> components, Stance stance)
    {
        foreach (int sign in IntegerSigns())
        {
            Point expectedPivot = FixedCasePoint(stance,
                sign * LegPlaneX, HingeCaseY, HingeCaseZ);
            Component2 pin = FindSignedStem(components, PivotPinStem, sign);
            Point actualPin = TransformOrigin(ReadTransform(pin, "pivot pin origin"));
            RequirePointClose(actualPin, expectedPivot, GroundTolerance,
                SideName(sign) + " pivot pin origin");

            Component2 leg = FindSignedStem(components, LegStem, sign);
            Point actualHinge = ApplyTransformToPoint(
                ReadTransform(leg, "leg hinge"), 0.0, HingeLocalY, HingeLocalZ);
            RequirePointClose(actualHinge, expectedPivot, GroundTolerance,
                SideName(sign) + " leg/pin coaxial origin");

            Component2 outer = FindSignedStem(components, OuterCheekStem, sign);
            double[] outerTransform = ReadTransform(outer, "outer cheek pivot frame");
            Point outerHole = ApplyTransformToPoint(outerTransform, 0.0, HingeCaseY, HingeCaseZ);
            RequireClose(outerHole.X, sign * OuterCheekCentreX, GroundTolerance,
                SideName(sign) + " outer-cheek pivot-plane x");
            RequireClose(outerHole.Y, expectedPivot.Y, GroundTolerance,
                SideName(sign) + " outer-cheek/pin coaxial y");
            RequireClose(outerHole.Z, expectedPivot.Z, GroundTolerance,
                SideName(sign) + " outer-cheek/pin coaxial z");

            Component2 inner = FindSignedStem(components, InnerSideStem, sign);
            Point innerHole = ApplyTransformToPoint(
                ReadTransform(inner, "inner side pivot frame"), 0.0, HingeCaseY, HingeCaseZ);
            RequireClose(innerHole.X, sign * InnerSideCentreX, GroundTolerance,
                SideName(sign) + " inner-side pivot-plane x");
            RequireClose(innerHole.Y, expectedPivot.Y, GroundTolerance,
                SideName(sign) + " inner-side/pin coaxial y");
            RequireClose(innerHole.Z, expectedPivot.Z, GroundTolerance,
                SideName(sign) + " inner-side/pin coaxial z");

            foreach (MountPoint mount in SpacerMounts)
            {
                Point expectedSpacer = FixedCasePoint(stance,
                    sign * LegPlaneX, mount.Y, mount.Z);
                int spacerMatches = 0;
                foreach (Component2 candidate in components)
                {
                    if (!SameStem(candidate, SpacerStem))
                    {
                        continue;
                    }
                    Point candidateOrigin = TransformOrigin(
                        ReadTransform(candidate, "spacer origin"));
                    if (Math.Abs(candidateOrigin.X - expectedSpacer.X) <= GroundTolerance &&
                        Math.Abs(candidateOrigin.Y - expectedSpacer.Y) <= GroundTolerance &&
                        Math.Abs(candidateOrigin.Z - expectedSpacer.Z) <= GroundTolerance)
                    {
                        spacerMatches++;
                    }
                }
                Require(spacerMatches == 1,
                    "Exactly one V0.7 spacer must occupy each signed mounting point.");
            }

            Stance deployment60 = CalculateStance(60.0);
            Point stopInCase = DeployedLegLocalPointInCase(deployment60,
                LoadStopLocalY, LoadStopLocalZ);
            Point expectedStop = FixedCasePoint(stance,
                sign * LegPlaneX, stopInCase.Y, stopInCase.Z);
            Component2 stopPin = FindSignedStem(components, LoadStopPinStem, sign);
            RequirePointClose(TransformOrigin(ReadTransform(stopPin, "hard-stop origin")),
                expectedStop, GroundTolerance, SideName(sign) + " fixed hard-stop origin");

            Point lockInCase = DeployedLegLocalPointInCase(deployment60,
                LockDeployLocalY, LockDeployLocalZ);
            Point expectedLock = FixedCasePoint(stance, sign * LockPinCentreX,
                lockInCase.Y, lockInCase.Z);
            Component2 lockPin = FindSignedStem(components, LockPinStem, sign);
            RequirePointClose(TransformOrigin(ReadTransform(lockPin, "reverse-lock origin")),
                expectedLock, GroundTolerance, SideName(sign) + " fixed captive-lock origin");

            double[] expectedHeelTransform = LegAttachedTransform(
                stance, sign, HeelCentreLocalY, HeelCentreLocalZ);
            Component2 heel = FindSignedStem(components, HeelInsertStem, sign);
            RequireTransformClose(ReadTransform(heel, "heel insert transform"),
                expectedHeelTransform, SideName(sign) + " heel insert");

            double[] expectedFootTransform = LegAttachedTransform(
                stance, sign, FootPadLocalY, FootPadLocalZ);
            Component2 foot = FindSignedStem(components, FootPadStem, sign);
            RequireTransformClose(ReadTransform(foot, "rubber foot transform"),
                expectedFootTransform, SideName(sign) + " rubber foot");

            double[] legTransform = ReadTransform(leg, "stable leg contact checks");
            Point neckTip = ApplyTransformToPoint(
                legTransform, 0.0, 102.0, FootPadLocalZ);
            Point footCentre = TransformOrigin(expectedFootTransform);
            double neckTipDistance = Math.Sqrt(
                Math.Pow(neckTip.Y - footCentre.Y, 2.0) +
                Math.Pow(neckTip.Z - footCentre.Z, 2.0));
            RequireClose(FootPadRadius - neckTipDistance, 5.0, GroundTolerance,
                SideName(sign) + " captured rubber-foot insertion depth");

            if (stance != null)
            {
                RequireClose(footCentre.Y, FootPadDeskCentreHeight, GroundTolerance,
                    SideName(sign) + " round-foot centre above desk");
                RequireClose(footCentre.Y - FootPadRadius, 0.0, GroundTolerance,
                    SideName(sign) + " round-foot lowest desk contact");

                Point heelContact = ApplyTransformToPoint(legTransform, 0.0,
                    HingeLocalY + HeelMaxY, HingeLocalZ + LoadStopLocalZ);
                double stopContactDistance = Math.Sqrt(
                    Math.Pow(heelContact.Y - expectedStop.Y, 2.0) +
                    Math.Pow(heelContact.Z - expectedStop.Z, 2.0));
                RequireClose(stopContactDistance, LoadStopDiameter / 2.0,
                    GroundTolerance, SideName(sign) + " heel-to-hard-stop zero-gap contact");

                Point legLockHole = ApplyTransformToPoint(legTransform, 0.0,
                    HingeLocalY + LockDeployLocalY,
                    HingeLocalZ + LockDeployLocalZ);
                Point expectedLegLock = FixedCasePoint(stance, sign * LegPlaneX,
                    lockInCase.Y, lockInCase.Z);
                RequirePointClose(legLockHole, expectedLegLock, GroundTolerance,
                    SideName(sign) + " deployed captive-lock alignment");
            }
            else
            {
                double storageLocalY = lockInCase.Y - HingeCaseY;
                double storageLocalZ = lockInCase.Z - HingeCaseZ;
                Point legLockHole = ApplyTransformToPoint(legTransform, 0.0,
                    HingeLocalY + storageLocalY,
                    HingeLocalZ + storageLocalZ);
                Point expectedLegLock = FixedCasePoint(null, sign * LegPlaneX,
                    lockInCase.Y, lockInCase.Z);
                RequirePointClose(legLockHole, expectedLegLock, GroundTolerance,
                    SideName(sign) + " folded captive-lock alignment");
            }
        }

        Component2 spreader = FindUniqueStem(components, HandleSpreaderStem);
        Point expectedSpreader = FixedCasePoint(stance, 0.0, 206.0, 0.0);
        RequirePointClose(TransformOrigin(ReadTransform(spreader, "handle spreader origin")),
            expectedSpreader, GroundTolerance, "carry-handle spreader origin");
    }

    private static void WriteAssemblyProperties(
        RackCadSession cad,
        AssemblyStage stage,
        Stance stance,
        ModelDoc2 document)
    {
        cad.Property(document, "Desktop support revision",
            "V0.7 captured 8 x 28 mm 7075-T6 double-shear kickstands; official 60-degree stance only; V0.4 source preserved");
        cad.Property(document, "Internal module width",
            "542 mm between inner faces x -271 and +271; six 104HP faces and AISI 304 thread strips unchanged");
        cad.Property(document, "Axial stack",
            "Per side: 4 mm inner frame + 8.8 mm cavity + 4 mm outer cheek; leg 8 mm with 0.4 mm clearance each face");
        cad.Property(document, "Overall width",
            "Nominal 575.6 mm, outer cheek faces x +/-287.8; desktop reference surfaces excluded from product width");
        cad.Property(document, "Mass-optimized structural rail",
            "Six rails retain solid 528.32 x 10 x 12 module faces and 25 mm solid M4 end bosses; only the rear spine becomes a 1.5 mm closed section");
        cad.Property(document, "Mass-optimized rear shell",
            "1.5 mm full rear shear skin with central 160 x 160 doubler retains 2.0 mm locally at VESA 100; VESA bridges/stiles/crossbeams unchanged");
        cad.Property(document, "Mass-optimized inner side",
            "3 mm continuous core with 4 mm rail band, rear band, case edges, leg block, spacer/catch islands; primary bearing thickness remains 4 mm");
        cad.Property(document, "Physical outer-cheek support",
            "Five diameter-12 x 8.8 mm M5-through spacers per side; ten occurrences total");
        cad.Property(document, "Cover-lock and visual treatment",
            "Outer cheek ends y72,z80; vent bank starts y83,z90, separating ventilation visually and structurally from the stand zone");
        cad.Property(document, "Primary load stop",
            "Diameter-10 full-stack hard-stop pin contacts the mechanically keyed steel heel at 60 degrees; contact-normal moment arm 38.0 mm");
        cad.Property(document, "Reverse lock",
            "One fixed diameter-8 spring-return indexing pin aligns with two leg holes and prevents reverse folding only; downward load seats on the hard stop");
        cad.Property(document, "Hard-stop screen",
            "400 N at 185 mm with factor 1.5 implies about 2.92 kN heel/stop reaction at a 38.0 mm contact-normal moment arm; engineering screen only, not physical PASS");
        cad.Property(document, "Source preservation",
            stage.SourceStem + ".SLDASM copied with SaveAs Copy; source native bytes are hash-checked unchanged");

        if (stage.IncludesClosedLid)
        {
            cad.Property(document, "Transport lid relief",
                "V0.7 independent 1.2 mm 5052-H32 lid replaces only the V0.4 travel lid; 576.6 mm inner width, deep returns, anti-drum beads and bilateral stand relief are included");
        }
        if (stage.IncludesDisplayLid)
        {
            cad.Property(document, "Detached lid presentation",
                "The complete V0.7 lid is placed beside the tilted case at world x620,y220,z0 so the module interior remains unobstructed");
        }

        if (stance == null)
        {
            cad.Property(document, "Folded leg origin",
                "x +/-279.4,y -54,z46; hinge y -129,z52; one fixed captive lock engages the folded leg hole");
        }
        else
        {
            cad.Property(document, "Module-face desktop angle",
                Format(stance.FaceAngleDegrees) + " degrees; geometry display only");
            cad.Property(document, "Mechanical rotation target",
                Format(stance.DetentDegrees) + " degrees from folded arm");
            cad.Property(document, "Rear support distance",
                Format(stance.SupportFootprint) + " mm from lower rear shell contact");
            cad.Property(document, "Desk contact check",
                "Both diameter-26 foot centres are Y=13 and round-crown lowest points are desk Y=0 within 0.1 mm in CAD");
            cad.Property(document, "Preliminary CG window",
                "The 185 mm support geometry increases the rear support footprint; actual loaded CG, cable pull and anti-tip margin require prototype mass measurement");
        }

        cad.Property(document, "Validation boundary",
            "Actual CG, friction, full tolerance, fasteners, pin/plate bearing, fatigue, misuse, pinch safety, drop and loaded prototype remain unverified");
    }

    private static Stance CalculateStance(double faceAngleDegrees)
    {
        double angle = DegreesToRadians(faceAngleDegrees);
        double sine = Math.Sin(angle);
        double cosine = Math.Cos(angle);
        double hingeHeight = (HingeCaseY - ShellContactY) * sine +
            (ShellContactZ - HingeCaseZ) * cosine;
        double footCentreDrop = hingeHeight - FootPadDeskCentreHeight;
        Require(footCentreDrop > 0.0 && footCentreDrop < PivotToFootPadCentre,
            "The fixed V0.7 hinge cannot place the round foot on the desktop at " +
            Format(faceAngleDegrees) + " degrees.");

        double horizontalReach = Math.Sqrt(
            PivotToFootPadCentre * PivotToFootPadCentre -
            footCentreDrop * footCentreDrop);
        double detent = faceAngleDegrees +
            RadiansToDegrees(Math.Asin(footCentreDrop / PivotToFootPadCentre));
        double hingeHorizontal = (HingeCaseY - ShellContactY) * cosine +
            (HingeCaseZ - ShellContactZ) * sine;

        Stance result = new Stance();
        result.FaceAngleDegrees = faceAngleDegrees;
        result.AngleRadians = angle;
        result.DetentDegrees = detent;
        result.DetentRadians = DegreesToRadians(detent);
        result.HingeHeight = hingeHeight;
        result.SupportFootprint = hingeHorizontal + horizontalReach;
        return result;
    }

    private static void LogStance(RackCadSession cad, Stance stance)
    {
        cad.Log("V07_" + Format(stance.FaceAngleDegrees) + "DEG_HINGE_HEIGHT_MM=" +
            Format(stance.HingeHeight));
        cad.Log("V07_" + Format(stance.FaceAngleDegrees) + "DEG_ROTATION_DEG=" +
            Format(stance.DetentDegrees));
        cad.Log("V07_" + Format(stance.FaceAngleDegrees) + "DEG_SUPPORT_MM=" +
            Format(stance.SupportFootprint));
        cad.Log("V07_" + Format(stance.FaceAngleDegrees) +
            "DEG_FOOT_CENTRE_HEIGHT_MM=" + Format(FootPadDeskCentreHeight));
    }

    private static double[] DeployedLegTransform(Stance stance, int sign)
    {
        double relative = stance.AngleRadians - stance.DetentRadians;
        double sine = Math.Sin(relative);
        double cosine = Math.Cos(relative);
        Point pivot = CasePointToDesk(sign * LegPlaneX, HingeCaseY, HingeCaseZ, stance);
        double originY = pivot.Y - (HingeLocalY * sine + HingeLocalZ * -cosine);
        double originZ = pivot.Z - (HingeLocalY * cosine + HingeLocalZ * sine);
        return new double[]
        {
            1.0, 0.0, 0.0,
            0.0, sine, cosine,
            0.0, -cosine, sine,
            pivot.X / 1000.0, originY / 1000.0, originZ / 1000.0,
            1.0, 0.0, 0.0, 0.0
        };
    }

    private static void VerifyLegContact(
        Component2 component,
        double[] transform,
        Stance stance,
        int sign)
    {
        Point hinge = ApplyTransformToPoint(transform, 0.0, HingeLocalY, HingeLocalZ);
        Point footCentre = ApplyTransformToPoint(
            transform, 0.0, FootPadLocalY, FootPadLocalZ);
        if (stance == null)
        {
            RequireClose(hinge.X, sign * LegPlaneX, GroundTolerance, "folded hinge x");
            RequireClose(hinge.Y, HingeCaseY, GroundTolerance, "folded hinge y");
            RequireClose(hinge.Z, HingeCaseZ, GroundTolerance, "folded hinge z");
            RequireClose(footCentre.X, sign * LegPlaneX, GroundTolerance,
                "folded rubber-foot centre x");
            RequireClose(footCentre.Y, 56.0, GroundTolerance,
                "folded rubber-foot centre y");
            RequireClose(footCentre.Z, 52.0, GroundTolerance,
                "folded rubber-foot centre z");
        }
        else
        {
            RequireClose(hinge.X, sign * LegPlaneX, GroundTolerance, "deployed hinge x");
            RequireClose(hinge.Y, stance.HingeHeight, GroundTolerance, "deployed hinge height");
            RequireClose(footCentre.X, sign * LegPlaneX, GroundTolerance,
                "deployed rubber-foot centre x");
            RequireClose(footCentre.Y, FootPadDeskCentreHeight, GroundTolerance,
                "deployed rubber-foot centre height");
            RequireClose(footCentre.Y - FootPadRadius, 0.0, GroundTolerance,
                "deployed rubber-foot lowest desk point");
            RequireClose(footCentre.Z, stance.SupportFootprint, GroundTolerance,
                "deployed rubber-foot rear support distance");
        }

        double[] reread = ReadTransform(component, "V0.7 leg transform readback");
        for (int index = 0; index < 12; index++)
        {
            RequireClose(reread[index], transform[index], TransformTolerance,
                "V0.7 leg transform element " + index.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static double[] LegAttachedTransform(
        Stance stance,
        int sign,
        double localY,
        double localZ)
    {
        double[] leg = stance == null
            ? IdentityTransform(sign * LegPlaneX, FoldedY, FoldedZ)
            : DeployedLegTransform(stance, sign);
        Point origin = ApplyTransformToPoint(leg, 0.0, localY, localZ);
        double[] result = (double[])leg.Clone();
        result[9] = origin.X / 1000.0;
        result[10] = origin.Y / 1000.0;
        result[11] = origin.Z / 1000.0;
        return result;
    }

    private static Point DeployedLegLocalPointInCase(
        Stance deployment,
        double relativePivotY,
        double relativePivotZ)
    {
        double sine = Math.Sin(deployment.DetentRadians);
        double cosine = Math.Cos(deployment.DetentRadians);
        return new Point(
            0.0,
            HingeCaseY + relativePivotY * cosine - relativePivotZ * sine,
            HingeCaseZ + relativePivotY * sine + relativePivotZ * cosine);
    }

    private static void VerifyLoadStopSweep(RackCadSession cad, Stance deployment)
    {
        Point fixedStop = DeployedLegLocalPointInCase(deployment,
            LoadStopLocalY, LoadStopLocalZ);
        double fixedY = fixedStop.Y - HingeCaseY;
        double fixedZ = fixedStop.Z - HingeCaseZ;
        Point foldedLockBoss = DeployedLegLocalPointInCase(deployment,
            LockDeployLocalY, LockDeployLocalZ);
        double foldedLockBossY = foldedLockBoss.Y - HingeCaseY;
        double foldedLockBossZ = foldedLockBoss.Z - HingeCaseZ;
        double minimumPreTerminalGap = double.PositiveInfinity;
        double finalGap = double.NaN;

        for (int index = 0; index <= StopSweepIntervals; index++)
        {
            double rotation = deployment.DetentRadians * index / StopSweepIntervals;
            double sine = Math.Sin(rotation);
            double cosine = Math.Cos(rotation);
            double centreY = fixedY * cosine + fixedZ * sine;
            double centreZ = -fixedY * sine + fixedZ * cosine;
            double pinRadius = LoadStopDiameter / 2.0;

            double rootGap = Math.Sqrt(centreY * centreY + centreZ * centreZ) -
                (RootDiameter / 2.0 + pinRadius);
            double earBackGap = RectangleGap(centreY, centreZ,
                EarMinY, HeelMinY, EarMinZ, EarMaxZ, pinRadius);
            double earBridgeGap = RectangleGap(centreY, centreZ,
                HeelMinY, EarMaxY, HeelMaxZ, EarMaxZ, pinRadius);
            double earGap = Math.Min(earBackGap, earBridgeGap);
            double heelGap = RectangleGap(centreY, centreZ,
                HeelMinY, HeelMaxY, HeelMinZ, HeelMaxZ, pinRadius);
            double armGap = RectangleGap(centreY, centreZ,
                -8.0, 167.0, -14.0, 14.0, pinRadius);
            double neckGap = RectangleGap(centreY, centreZ,
                160.0, 177.0, -8.0, 8.0, pinRadius);
            double foldedLockBossGap = Math.Sqrt(
                Math.Pow(centreY - foldedLockBossY, 2.0) +
                Math.Pow(centreZ - foldedLockBossZ, 2.0)) -
                (LockFoldedBossDiameter / 2.0 + pinRadius);
            double gap = Math.Min(rootGap,
                Math.Min(earGap, Math.Min(heelGap,
                    Math.Min(armGap, Math.Min(neckGap, foldedLockBossGap)))));

            if (index < StopSweepIntervals)
            {
                Require(gap > 0.000001,
                    "The fixed hard stop touches or penetrates before the 60-degree endpoint; sample=" +
                    index.ToString(CultureInfo.InvariantCulture) + "; gap=" + Format(gap));
                minimumPreTerminalGap = Math.Min(minimumPreTerminalGap, gap);
            }
            else
            {
                finalGap = gap;
                RequireClose(heelGap, 0.0, 0.000001,
                    "60-degree steel heel/hard-stop terminal gap");
                Require(rootGap > 0.0 && earGap > 0.0 &&
                        armGap > 0.0 && neckGap > 0.0 && foldedLockBossGap > 0.0,
                    "The 60-degree endpoint must be controlled by the steel heel, not root, aluminium ear, straight arm, foot neck or folded-lock boss.");
            }
        }

        RequireClose(finalGap, 0.0, 0.000001,
            "60-degree combined hard-stop terminal gap");
        cad.Log("V07_STOP_SWEEP_SAMPLES=" +
            (StopSweepIntervals + 1).ToString(CultureInfo.InvariantCulture) +
            ";minimum_preterminal_gap_mm=" + Format(minimumPreTerminalGap) +
            ";terminal_gap_mm=" + Format(finalGap) +
            ";terminal_contact=steel_heel_only");
        cad.Log("V07_LOAD_STOP_CASE_YZ_MM=" + Format(fixedStop.Y) + "," +
            Format(fixedStop.Z));
        Point lockPoint = DeployedLegLocalPointInCase(deployment,
            LockDeployLocalY, LockDeployLocalZ);
        cad.Log("V07_DEPLOY_LOCK_CASE_YZ_MM=" + Format(lockPoint.Y) + "," +
            Format(lockPoint.Z));
    }

    private static double RectangleGap(
        double centreY,
        double centreZ,
        double minY,
        double maxY,
        double minZ,
        double maxZ,
        double pinRadius)
    {
        double dy = Math.Max(Math.Max(minY - centreY, 0.0), centreY - maxY);
        double dz = Math.Max(Math.Max(minZ - centreZ, 0.0), centreZ - maxZ);
        return Math.Sqrt(dy * dy + dz * dz) - pinRadius;
    }

    private static double[] FixedCaseTransform(Stance stance, double x, double y, double z)
    {
        if (stance == null)
        {
            return IdentityTransform(x, y, z);
        }
        return CaseTransform(CasePointToDesk(x, y, z, stance), stance);
    }

    private static Point FixedCasePoint(Stance stance, double x, double y, double z)
    {
        if (stance == null)
        {
            return new Point(x, y, z);
        }
        return CasePointToDesk(x, y, z, stance);
    }

    private static double[] CaseTransform(Point origin, Stance stance)
    {
        double sine = Math.Sin(stance.AngleRadians);
        double cosine = Math.Cos(stance.AngleRadians);
        return new double[]
        {
            1.0, 0.0, 0.0,
            0.0, sine, cosine,
            0.0, -cosine, sine,
            origin.X / 1000.0, origin.Y / 1000.0, origin.Z / 1000.0,
            1.0, 0.0, 0.0, 0.0
        };
    }

    private static Point CasePointToDesk(double x, double y, double z, Stance stance)
    {
        double sine = Math.Sin(stance.AngleRadians);
        double cosine = Math.Cos(stance.AngleRadians);
        return new Point(
            x,
            (y - ShellContactY) * sine + (ShellContactZ - z) * cosine,
            (y - ShellContactY) * cosine + (z - ShellContactZ) * sine);
    }

    private static Point ApplyTransformToPoint(
        double[] transform,
        double x,
        double y,
        double z)
    {
        return new Point(
            x * transform[0] + y * transform[3] + z * transform[6] + transform[9] * 1000.0,
            x * transform[1] + y * transform[4] + z * transform[7] + transform[10] * 1000.0,
            x * transform[2] + y * transform[5] + z * transform[8] + transform[11] * 1000.0);
    }

    private static Point TransformOrigin(double[] transform)
    {
        return new Point(transform[9] * 1000.0,
            transform[10] * 1000.0, transform[11] * 1000.0);
    }

    private static double[] IdentityTransform(double x, double y, double z)
    {
        return new double[]
        {
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0,
            x / 1000.0, y / 1000.0, z / 1000.0,
            1.0, 0.0, 0.0, 0.0
        };
    }

    private static void ApplyComponentTransform(
        ModelDoc2 document,
        AssemblyDoc assembly,
        MathUtility utility,
        Component2 component,
        double[] requested,
        string context)
    {
        if (component.IsFixed())
        {
            document.ClearSelection2(true);
            Require(component.Select4(false, null, false),
                "Cannot select fixed component before moving " + context);
            assembly.UnfixComponent();
            document.ClearSelection2(true);
            Require(!component.IsFixed(), "Fixed component could not be released for " + context);
        }

        MathTransform replacement = utility.CreateTransform(requested) as MathTransform;
        Require(replacement != null, "Cannot create SOLIDWORKS transform for " + context);
        component.Transform2 = replacement;
        double[] actual = ReadTransform(component, context);
        for (int index = 0; index < 12; index++)
        {
            RequireClose(actual[index], requested[index], TransformTolerance,
                context + " transform element " + index.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static double[] ReadTransform(Component2 component, string context)
    {
        MathTransform transform = component.Transform2;
        Array raw = transform == null ? null : transform.ArrayData as Array;
        Require(raw != null && raw.Length >= 16,
            "A complete SOLIDWORKS transform is unavailable for " + context);
        double[] result = new double[16];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = Convert.ToDouble(raw.GetValue(index), CultureInfo.InvariantCulture);
        }
        return result;
    }

    private static Dictionary<int, double[]> CaptureSignedTransforms(
        List<Component2> components,
        string stem,
        string context)
    {
        Dictionary<int, double[]> result = new Dictionary<int, double[]>();
        foreach (Component2 component in components)
        {
            if (!SameStem(component, stem))
            {
                continue;
            }
            double[] transform = ReadTransform(component, context);
            int sign = transform[9] < 0.0 ? -1 : 1;
            Require(!result.ContainsKey(sign), "Duplicate signed transform in " + context);
            result.Add(sign, transform);
        }
        Require(result.Count == 2 && result.ContainsKey(-1) && result.ContainsKey(1),
            "Exactly one left and one right transform are required for " + context);
        return result;
    }

    private static Dictionary<string, int> CaptureUnchangedSignatures(
        List<Component2> components,
        params string[] excludedStems)
    {
        Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (Component2 component in components)
        {
            bool excluded = false;
            foreach (string stem in excludedStems)
            {
                if (SameStem(component, stem))
                {
                    excluded = true;
                    break;
                }
            }
            if (excluded)
            {
                continue;
            }

            string path = component.GetPathName();
            Require(!string.IsNullOrWhiteSpace(path),
                "An unchanged component has no exact file path: " + component.Name2);
            double[] transform = ReadTransform(component, "unchanged component signature");
            string signature = Path.GetFullPath(path).ToUpperInvariant();
            for (int index = 0; index < 12; index++)
            {
                signature += "|" + transform[index].ToString("R", CultureInfo.InvariantCulture);
            }
            int count;
            result.TryGetValue(signature, out count);
            result[signature] = count + 1;
        }
        return result;
    }

    private static bool DictionaryEqual(
        Dictionary<string, int> first,
        Dictionary<string, int> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }
        foreach (KeyValuePair<string, int> pair in first)
        {
            int value;
            if (!second.TryGetValue(pair.Key, out value) || value != pair.Value)
            {
                return false;
            }
        }
        return true;
    }

    private static ModelDoc2 OpenExactAssembly(RackCadSession cad, string path)
    {
        string expected = Path.GetFullPath(path);
        RequireProjectFile(cad, expected);
        ModelDoc2 document = cad.Application.GetOpenDocumentByName(expected) as ModelDoc2;
        int errors = 0;
        int warnings = 0;
        if (document == null)
        {
            document = cad.Application.OpenDoc6(
                expected,
                (int)swDocumentTypes_e.swDocASSEMBLY,
                (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                string.Empty,
                ref errors,
                ref warnings) as ModelDoc2;
        }
        Require(document != null && errors == 0,
            "Cannot open exact project assembly; errors=" + errors.ToString(CultureInfo.InvariantCulture) +
            "; warnings=" + warnings.ToString(CultureInfo.InvariantCulture) + "; path=" + expected);
        Require(document is AssemblyDoc && SamePath(Path.GetFullPath(document.GetPathName()), expected),
            "SOLIDWORKS returned the wrong exact assembly for " + expected);
        if (warnings != 0)
        {
            cad.Log("WARNING: Opening " + expected + " returned " +
                warnings.ToString(CultureInfo.InvariantCulture));
        }
        return document;
    }

    private static void ActivateExact(RackCadSession cad, ModelDoc2 document, string expectedPath)
    {
        int error = 0;
        ModelDoc2 active = cad.Application.ActivateDoc3(
            document.GetTitle(), false,
            (int)swRebuildOnActivation_e.swDontRebuildActiveDoc,
            ref error) as ModelDoc2;
        Require(active != null && SamePath(Path.GetFullPath(active.GetPathName()), expectedPath),
            "Cannot activate exact document " + expectedPath + "; status=" +
            error.ToString(CultureInfo.InvariantCulture));
    }

    private static List<Component2> TopLevelComponents(AssemblyDoc assembly)
    {
        Array raw = assembly.GetComponents(true) as Array;
        Require(raw != null, "Assembly exposes no top-level component array.");
        List<Component2> result = new List<Component2>();
        foreach (object value in raw)
        {
            Component2 component = value as Component2;
            if (component != null)
            {
                result.Add(component);
            }
        }
        Require(result.Count != 0, "Assembly has no usable top-level components.");
        return result;
    }

    private static Component2 FindSignedStem(
        List<Component2> components,
        string stem,
        int sign)
    {
        Component2 found = null;
        foreach (Component2 component in components)
        {
            if (!SameStem(component, stem))
            {
                continue;
            }
            int componentSign = ReadTransform(component, stem + " signed lookup")[9] < 0.0 ? -1 : 1;
            if (componentSign != sign)
            {
                continue;
            }
            Require(found == null, "Duplicate signed component " + stem);
            found = component;
        }
        Require(found != null, "Missing signed component " + stem + " on " + SideName(sign));
        return found;
    }

    private static Component2 FindUniqueStem(List<Component2> components, string stem)
    {
        Component2 found = null;
        foreach (Component2 component in components)
        {
            if (!SameStem(component, stem))
            {
                continue;
            }
            Require(found == null, "Duplicate unique component " + stem);
            found = component;
        }
        Require(found != null, "Missing unique component " + stem);
        return found;
    }

    private static int CountStem(List<Component2> components, string stem)
    {
        int count = 0;
        foreach (Component2 component in components)
        {
            if (SameStem(component, stem))
            {
                count++;
            }
        }
        return count;
    }

    private static int CountExactPath(List<Component2> components, string exactPath)
    {
        int count = 0;
        foreach (Component2 component in components)
        {
            if (SameComponentPath(component, exactPath))
            {
                count++;
            }
        }
        return count;
    }

    private static bool SameStem(Component2 component, string stem)
    {
        string path = component == null ? null : component.GetPathName();
        return !string.IsNullOrWhiteSpace(path) &&
            string.Equals(Path.GetFileNameWithoutExtension(path), stem,
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool SameComponentPath(Component2 component, string exactPath)
    {
        string path = component == null ? null : component.GetPathName();
        return !string.IsNullOrWhiteSpace(path) && SamePath(Path.GetFullPath(path), exactPath);
    }

    private static bool IsV07SideHardware(Component2 component)
    {
        return SameStem(component, InnerSideStem) ||
            SameStem(component, LegStem) ||
            SameStem(component, OuterCheekStem) ||
            SameStem(component, PivotPinStem) ||
            SameStem(component, SpacerStem) ||
            SameStem(component, LoadStopPinStem) ||
            SameStem(component, LockPinStem) ||
            SameStem(component, HeelInsertStem) ||
            SameStem(component, FootPadStem);
    }

    private static Body2 SideHole(
        RackCadSession cad,
        Body2 body,
        double y,
        double z,
        double diameter,
        string context)
    {
        return cad.Cut(body,
            cad.Cylinder(-InnerSideThickness / 2.0 - 0.3,
                y, z, 1.0, 0.0, 0.0, diameter, InnerSideThickness + 0.6),
            context);
    }

    private static Body2 ThroughCheekHole(
        RackCadSession cad,
        Body2 cheek,
        double y,
        double z,
        double diameter,
        string context)
    {
        return cad.Cut(cheek,
            cad.Cylinder(-OuterCheekThickness / 2.0 - 0.3,
                y, z, 1.0, 0.0, 0.0, diameter, OuterCheekThickness + 0.6),
            context);
    }

    private static Body2 Unite(Body2 first, Body2 second, string context)
    {
        int error = 0;
        object raw = first.Operations2((int)swBodyOperationType_e.SWBODYADD, second, out error);
        Require(error == (int)swBodyOperationError_e.swBodyOperationNoError,
            "SOLIDWORKS body union failed for " + context + "; error=" +
            error.ToString(CultureInfo.InvariantCulture));
        Array bodies = raw as Array;
        Require(bodies != null && bodies.Length == 1,
            "Body union must yield exactly one solid for " + context);
        Body2 result = bodies.GetValue(bodies.GetLowerBound(0)) as Body2;
        Require(result != null, "Body union returned no valid solid for " + context);
        return result;
    }

    private static IEnumerable<double> RailPositions(RackCadSession cad)
    {
        double rowPitch = cad.N("eurorack", "row_pitch");
        double railSpacing = cad.N("eurorack", "mounting_hole_vertical_spacing");
        foreach (double centre in new double[] { -rowPitch, 0.0, rowPitch })
        {
            yield return centre - railSpacing / 2.0;
            yield return centre + railSpacing / 2.0;
        }
    }

    private static void ValidatePart(
        ModelDoc2 document,
        int expectedBodyCount,
        Bounds expected,
        string context)
    {
        PartDoc part = document as PartDoc;
        Require(part != null, context + " is not a SOLIDWORKS part.");
        Array raw = part.GetBodies2((int)swBodyType_e.swSolidBody, false) as Array;
        Require(raw != null && raw.Length == expectedBodyCount,
            context + " solid-body count mismatch; expected " +
            expectedBodyCount.ToString(CultureInfo.InvariantCulture) + ", actual " +
            (raw == null ? "none" : raw.Length.ToString(CultureInfo.InvariantCulture)));

        Bounds actual = Bounds.FromBodies(raw);
        RequireClose(actual.MinX, expected.MinX, GeometryTolerance, context + " min x");
        RequireClose(actual.MinY, expected.MinY, GeometryTolerance, context + " min y");
        RequireClose(actual.MinZ, expected.MinZ, GeometryTolerance, context + " min z");
        RequireClose(actual.MaxX, expected.MaxX, GeometryTolerance, context + " max x");
        RequireClose(actual.MaxY, expected.MaxY, GeometryTolerance, context + " max y");
        RequireClose(actual.MaxZ, expected.MaxZ, GeometryTolerance, context + " max z");
    }

    private static MathUtility RequireMathUtility(RackCadSession cad)
    {
        MathUtility utility = cad.Application.GetMathUtility() as MathUtility;
        Require(utility != null, "SOLIDWORKS did not provide its math utility.");
        return utility;
    }

    private static void VerifyFinalAssemblyReadyOnDisk(RackCadSession cad, string stem)
    {
        string path = AssemblyPath(cad, stem);
        Require(File.Exists(path) && new FileInfo(path).Length > 0,
            "The final V0.7 target is missing on disk: " + path);
        ModelDoc2 open = cad.Application.GetOpenDocumentByName(path) as ModelDoc2;
        Require(open == null,
            "The builder must leave the final V0.7 target closed; the preview tool exclusively owns final view activation: " + path);
        cad.Log("V07_FINAL_TARGET_READY=" + stem +
            ";closed_on_disk=true;preview_tool_owns_final_view=true");
    }

    private static string PartPath(RackCadSession cad, string stem)
    {
        return Path.GetFullPath(Path.Combine(cad.PartsDirectory, stem + ".SLDPRT"));
    }

    private static string AssemblyPath(RackCadSession cad, string stem)
    {
        return Path.GetFullPath(Path.Combine(cad.AssembliesDirectory, stem + ".SLDASM"));
    }

    private static void RequireProjectFile(RackCadSession cad, string path)
    {
        string full = Path.GetFullPath(path);
        string prefix = cad.Root.TrimEnd(Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Require(full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase),
            "Refusing a V0.7 operation outside Rack4Modules: " + full);
        Require(File.Exists(full) && new FileInfo(full).Length > 0,
            "Required exact project file is missing or empty: " + full);
    }

    private static void RequireProjectFilePath(string path)
    {
        string full = Path.GetFullPath(path);
        Require(File.Exists(full) && new FileInfo(full).Length > 0,
            "Required exact component file is missing or empty: " + full);
    }

    private static bool SamePath(string first, string second)
    {
        return string.Equals(Path.GetFullPath(first), Path.GetFullPath(second),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string TitleStem(string title)
    {
        string clean = (title ?? string.Empty).Trim().TrimEnd('*').Trim();
        return Path.GetFileNameWithoutExtension(clean);
    }

    private static IEnumerable<double> Signs()
    {
        yield return -1.0;
        yield return 1.0;
    }

    private static IEnumerable<int> IntegerSigns()
    {
        yield return -1;
        yield return 1;
    }

    private static string SideName(int sign)
    {
        return sign < 0 ? "left" : "right";
    }

    private static void RequirePointClose(
        Point actual,
        Point expected,
        double tolerance,
        string context)
    {
        RequireClose(actual.X, expected.X, tolerance, context + " x");
        RequireClose(actual.Y, expected.Y, tolerance, context + " y");
        RequireClose(actual.Z, expected.Z, tolerance, context + " z");
    }

    private static void RequireTransformClose(
        double[] actual,
        double[] expected,
        string context)
    {
        Require(actual != null && expected != null &&
                actual.Length >= 12 && expected.Length >= 12,
            "Complete transforms are required for " + context);
        for (int index = 0; index < 12; index++)
        {
            RequireClose(actual[index], expected[index], TransformTolerance,
                context + " transform element " +
                index.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void RequireClose(
        double actual,
        double expected,
        double tolerance,
        string context)
    {
        Require(!double.IsNaN(actual) && !double.IsInfinity(actual) &&
                Math.Abs(actual - expected) <= tolerance,
            "Geometry verification failed for " + context + "; actual=" + Format(actual) +
            "; expected=" + Format(expected) + "; tolerance=" + Format(tolerance));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static double RadiansToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }

    private static string Format(double value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private sealed class AssemblyStage
    {
        internal readonly string SourceStem;
        internal readonly string TargetStem;
        internal readonly int SourceComponentCount;
        internal readonly double FaceAngleDegrees;
        internal readonly bool IncludesClosedLid;
        internal readonly bool IncludesDisplayLid;

        internal AssemblyStage(
            string sourceStem,
            string targetStem,
            int sourceComponentCount,
            double faceAngleDegrees,
            bool includesClosedLid,
            bool includesDisplayLid)
        {
            SourceStem = sourceStem;
            TargetStem = targetStem;
            SourceComponentCount = sourceComponentCount;
            FaceAngleDegrees = faceAngleDegrees;
            IncludesClosedLid = includesClosedLid;
            IncludesDisplayLid = includesDisplayLid;
        }
    }

    private sealed class PartPaths
    {
        internal string LightweightRail;
        internal string LightweightBackPanel;
        internal string InnerSide;
        internal string Leg;
        internal string OuterCheek;
        internal string PivotPin;
        internal string Spacer;
        internal string LoadStopPin;
        internal string LockPin;
        internal string HeelInsert;
        internal string FootPad;
        internal string TravelLid;
        internal string HandleSpreader;
    }

    private sealed class Stance
    {
        internal double FaceAngleDegrees;
        internal double AngleRadians;
        internal double DetentDegrees;
        internal double DetentRadians;
        internal double HingeHeight;
        internal double SupportFootprint;
    }

    private sealed class Point
    {
        internal double X;
        internal double Y;
        internal double Z;

        internal Point()
        {
        }

        internal Point(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    private sealed class MountPoint
    {
        internal readonly double Y;
        internal readonly double Z;

        internal MountPoint(double y, double z)
        {
            Y = y;
            Z = z;
        }
    }

    private sealed class Bounds
    {
        internal double MinX;
        internal double MinY;
        internal double MinZ;
        internal double MaxX;
        internal double MaxY;
        internal double MaxZ;

        internal Bounds(double minX, double minY, double minZ,
            double maxX, double maxY, double maxZ)
        {
            MinX = minX;
            MinY = minY;
            MinZ = minZ;
            MaxX = maxX;
            MaxY = maxY;
            MaxZ = maxZ;
        }

        internal static Bounds FromBodies(Array bodies)
        {
            Bounds result = new Bounds(
                double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity,
                double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);
            foreach (object value in bodies)
            {
                Body2 body = value as Body2;
                Require(body != null, "A part body is invalid during bounds validation.");
                double[] box = body.GetBodyBox() as double[];
                Require(box != null && box.Length >= 6,
                    "A part body exposes no valid bounds.");
                result.MinX = Math.Min(result.MinX, box[0] * 1000.0);
                result.MinY = Math.Min(result.MinY, box[1] * 1000.0);
                result.MinZ = Math.Min(result.MinZ, box[2] * 1000.0);
                result.MaxX = Math.Max(result.MaxX, box[3] * 1000.0);
                result.MaxY = Math.Max(result.MaxY, box[4] * 1000.0);
                result.MaxZ = Math.Max(result.MaxZ, box[5] * 1000.0);
            }
            return result;
        }
    }

    private sealed class FileSnapshot
    {
        internal readonly string Path;
        internal readonly long Length;
        internal readonly DateTime LastWriteUtc;
        internal readonly string Hash;

        private FileSnapshot(string path, long length, DateTime lastWriteUtc, string hash)
        {
            Path = path;
            Length = length;
            LastWriteUtc = lastWriteUtc;
            Hash = hash;
        }

        internal static FileSnapshot Capture(string path)
        {
            FileInfo file = new FileInfo(path);
            Require(file.Exists && file.Length > 0,
                "Cannot snapshot missing V0.4 source: " + path);
            return new FileSnapshot(file.FullName, file.Length, file.LastWriteTimeUtc,
                HashFile(file.FullName));
        }

        internal void RequireUnchanged()
        {
            FileInfo file = new FileInfo(Path);
            Require(file.Exists && file.Length == Length &&
                    file.LastWriteTimeUtc == LastWriteUtc &&
                    string.Equals(HashFile(Path), Hash, StringComparison.Ordinal),
                "A protected V0.4 source changed during V0.7 generation: " + Path);
        }

        internal static string HashFile(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete))
            using (SHA256 algorithm = SHA256.Create())
            {
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }
    }
}
