using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// V0.6 is intentionally generated as a new set of native files.  This helper never
// saves a V0.4 source document, and it refuses to replace any generated target that
// is open with unsaved changes.  Compile together with SwCadCore.cs.
internal static class BuildRackStableKickstandV06
{
    private const string OldSideStem = "SideFrame_V04_Vented_DualRailFix";
    private const string OldLegStem = "SideKickstand_V04_LowerPivot150mm";
    private const string OldTravelLidStem = "DeepTravelLid_70mmClearance";

    private const string InnerSideStem = "SideFrame_V06_StableDoubleShearInner";
    private const string LegStem = "SideKickstand_V06_170mm_6mm";
    private const string OuterCheekStem = "KickstandOuterCheek_V06_Stable";
    private const string PivotPinStem = "KickstandPivotPin_V06_8mm";
    private const string SpacerStem = "KickstandSpacer_V06_6p8mm";
    private const string LoadStopPinStem = "KickstandLoadStopPin_V06_8mm";
    private const string LockPinStem = "KickstandLockPin_V06_5mm";
    private const string HeelInsertStem = "KickstandHeelInsert_V06";
    private const string FootPadStem = "KickstandFootPad_V06_Rubber";
    private const string TravelLidStem = "DeepTravelLid_V06_StandRelief";

    private const double TravelLidThickness = 1.5;
    private const double TravelLidFrontZ = -70.0;
    private const double TravelLidSkirtDepth = 82.0;
    // The folded 170 mm leg occupies approximately y=-147..33, z=8..70 at
    // the side return.  A bilateral top-side notch gives 8 mm y margin and
    // 3 mm z margin while retaining the lower 75 mm of the return as a lid
    // capture wall.  This is V0.6-only; the protected V0.4 lid is untouched.
    private const double TravelReliefMinY = -155.0;
    private const double TravelReliefMaxY = 41.0;
    private const double TravelReliefMinZ = 5.0;
    private const double TravelReliefMaxZ = 15.0;

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
    private const double InnerSideThickness = 3.0;
    private const double InnerSideCentreX = 272.5;
    private const double InteriorClearWidth = 542.0;

    // Stable V0.6 axial stack, mirrored left/right.
    // Right: inner side x=271..274, 6.8 cavity x=274..280.8,
    // outer cheek x=280.8..283.8.  The 6 mm leg has 0.4 mm nominal
    // clearance on both faces and is fixed at x=277.4 for every state.
    private const double CavityWidth = 6.8;
    private const double LegThickness = 6.0;
    private const double LegPlaneX = 277.4;
    private const double OuterCheekThickness = 3.0;
    private const double OuterCheekCentreX = 282.3;
    private const double OuterHalfWidth = 283.8;
    private const double OverallWidth = 567.6;
    private const double AxialClearanceEachSide = 0.4;

    private const double FoldedY = -54.0;
    private const double FoldedZ = 46.0;
    private const double HingeLocalY = -75.0;
    private const double HingeLocalZ = 6.0;
    private const double FootPadLocalY = 95.0;
    private const double FootPadLocalZ = 6.0;
    private const double HingeCaseY = -129.0;
    private const double HingeCaseZ = 52.0;
    private const double PivotToFootPadCentre = 170.0;
    private const double ArmInPlaneWidth = 18.0;
    private const double RootDiameter = 36.0;
    private const double PivotClearanceDiameter = 8.2;
    private const double PivotPinDiameter = 8.0;
    private const double PivotPinLength = 12.8;

    private const double FootPadDiameter = 16.0;
    private const double FootPadRadius = 8.0;
    private const double FootPadAxialLength = 6.0;
    private const double FootPadDeskCentreHeight = 8.0;

    // The aluminium ear is local to the leg pivot.  The y=7..8 by z=-36..-20
    // region is cut back one millimetre; the independent steel heel occupies
    // y=7..10 and therefore has no volume overlap with the 7075-T6 leg.
    private const double EarMinY = -8.0;
    private const double EarMaxY = 8.0;
    private const double EarMinZ = -44.0;
    private const double EarMaxZ = -12.0;
    private const double HeelMinY = 7.0;
    private const double HeelMaxY = 10.0;
    private const double HeelMinZ = -36.0;
    private const double HeelMaxZ = -20.0;
    private const double HeelCentreLocalY = -66.5;
    private const double HeelCentreLocalZ = -22.0;

    private const double LoadStopLocalY = 14.0;
    private const double LoadStopLocalZ = -28.0;
    private const double LoadStopDiameter = 8.0;
    private const double LoadStopClearanceDiameter = 8.2;
    private const double LoadStopLength = 12.8;
    // The stop pin is tangent to the heel's y=10 face.  Its contact normal is
    // parallel to leg-local Y, so the moment arm about the pivot is |z|=28 mm,
    // not the radial pivot-to-pin-centre distance.
    private const double EffectiveStopLever = 28.0;
    private const int StopSweepIntervals = 1000;

    private const double LockLocalY = 0.0;
    private const double LockLocalZ = -38.0;
    private const double LockPinDiameter = 5.0;
    private const double LockLegHoleDiameter = 5.2;
    private const double LockPlateHoleDiameter = 5.8;
    private const double LockPinLength = 12.8;
    private const double LockStorageCaseY = -145.0;
    private const double LockStorageCaseZ = 72.0;

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
    private const double SpacerOuterDiameter = 10.0;
    private const double SpacerHoleDiameter = 4.5;
    private const double SpacerLength = 6.8;

    // The local external strip covers the complete folded leg and rubber foot.
    // A lower ear plate reaches z=4, while the z=80..84 vents stay visible.
    private const double OuterCheekMinY = -170.0;
    private const double OuterCheekMaxY = 52.0;
    private const double OuterCheekMainMinZ = 24.0;
    private const double OuterCheekMaxZ = 78.0;
    private const double OuterCheekEarMinY = -154.0;
    private const double OuterCheekEarMaxY = -104.0;
    private const double OuterCheekEarMinZ = 4.0;
    private const double OuterCheekEarMaxZ = 32.0;
    private const double FingerNotchY = 52.0;
    private const double FingerNotchZ = 54.0;
    private const double FingerNotchDiameter = 14.0;


    private const double VentLengthY = 18.0;
    private const double VentWidthZ = 4.0;
    private const double VentCenterZ = 82.0;

    private static readonly double[] VentCentersY = new double[]
    {
        -120.0, -96.0, -72.0, -48.0, 48.0, 72.0, 96.0, 120.0
    };

    // Four physical spacer occurrences per side.  They sit outside the folded
    // arm/root envelope and provide a defined, non-floating outer-cheek stack.
    private static readonly MountPoint[] SpacerMounts = new MountPoint[]
    {
        new MountPoint(-160.0, 39.0),
        new MountPoint(-160.0, 68.0),
        new MountPoint(-70.0, 34.0),
        new MountPoint(42.0, 34.0)
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
                    "Usage: BuildRackStableKickstandV06.exe <Rack4Modules root>");
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
            parts.InnerSide = CreateInnerSideFrame(cad, stance60);
            parts.Leg = CreateStableLeg(cad);
            parts.OuterCheek = CreateOuterCheek(cad, stance60);
            parts.PivotPin = CreatePivotPin(cad);
            parts.Spacer = CreateSpacer(cad);
            parts.LoadStopPin = CreateLoadStopPin(cad);
            parts.LockPin = CreateLockPin(cad);
            parts.HeelInsert = CreateHeelInsert(cad);
            parts.FootPad = CreateFootPad(cad);
            parts.TravelLid = CreateTransportLid(cad);

            foreach (AssemblyStage stage in stages)
            {
                Stance stance = null;
                if (Math.Abs(stage.FaceAngleDegrees - 60.0) < 0.001)
                {
                    stance = stance60;
                }
                BuildV06Assembly(cad, stage, stance, parts);
            }

            VerifyV04SnapshotsUnchanged(v04Snapshots);
            VerifyFinalAssemblyReadyOnDisk(cad, "Rack4Modules_DesktopTilt60_V06");

            cad.Log("V06_INTERNAL_CLEAR_WIDTH_MM=542");
            cad.Log("V06_DOUBLE_SHEAR_STACK_MM=3_inner+6.8_cavity+3_outer");
            cad.Log("V06_LEG_MM=6_thickx18_wide;7075-T6_plate;root_diameter_36");
            cad.Log("V06_LEG_PLANE_X_MM=+/-277.4;no_axial_popout");
            cad.Log("V06_OUTER_WIDTH_MM=567.6");
            cad.Log("V06_OFFICIAL_STANCE=60_degree_only;V05_75_degree_history_not_generated");
            cad.Log("V06_STOP_LOCK_ORDER=hard_stop_first;lock_pin_reverse_only");
            cad.Log("V06_V04_SOURCE_HASHES_UNCHANGED=true");
            cad.Log("V06_STABLE_KICKSTAND_BUILD_COMPLETE=true");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V06_STABLE_KICKSTAND_BUILD_FAILED=" + exception.ToString());
            Console.Error.Flush();
            return 1;
        }
    }

    private static List<AssemblyStage> BuildStages()
    {
        return new List<AssemblyStage>
        {
            new AssemblyStage("Rack4Modules_OpenCase_V04", "Rack4Modules_OpenCase_V06", 46, 0.0, false),
            new AssemblyStage("Rack4Modules_TransportClosed_V04", "Rack4Modules_TransportClosed_V06", 47, 0.0, true),
            new AssemblyStage("Rack4Modules_ClearanceCheck_V04", "Rack4Modules_ClearanceCheck_V06", 54, 0.0, false),
            new AssemblyStage("Rack4Modules_DesktopTilt60_V04", "Rack4Modules_DesktopTilt60_V06", 47, 60.0, false)
        };
    }

    private static void VerifyFrozenGeometry(RackCadSession cad)
    {
        RequireClose(cad.N("enclosure", "outer_width"), CaseWidth, 0.001, "V0.4 source width");
        RequireClose(cad.N("enclosure", "outer_height"), CaseHeight, 0.001, "V0.4 source height");
        RequireClose(cad.N("enclosure", "body_depth"), CaseDepth, 0.001, "V0.4 source depth");
        RequireClose(cad.N("enclosure", "body_thickness"), ShellThickness, 0.001, "shell thickness");
        RequireClose(cad.N("enclosure", "side_frame_thickness"), InnerSideThickness, 0.001,
            "inner side-frame thickness");
        RequireClose(CaseWidth - 2.0 * InnerSideThickness, InteriorClearWidth, 0.000001,
            "unchanged internal clear width");
        RequireClose(InnerSideCentreX - InnerSideThickness / 2.0, InteriorClearWidth / 2.0,
            0.000001, "right inner clear boundary");
        RequireClose(OuterCheekCentreX - OuterCheekThickness / 2.0,
            CaseWidth / 2.0 + CavityWidth, 0.000001, "outer-cheek inner face");
        RequireClose(LegPlaneX - LegThickness / 2.0,
            CaseWidth / 2.0 + AxialClearanceEachSide, 0.000001, "leg inner-face clearance");
        RequireClose(OuterCheekCentreX - OuterCheekThickness / 2.0 -
            (LegPlaneX + LegThickness / 2.0), AxialClearanceEachSide, 0.000001,
            "leg outer-face clearance");
        RequireClose(OuterHalfWidth * 2.0, OverallWidth, 0.000001, "V0.6 overall width");
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
        RequireClose(LockPinLength, PivotPinLength, 0.000001,
            "reverse-lock full stack length");
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
            LoadStopPinStem, LockPinStem, HeelInsertStem, FootPadStem, TravelLidStem
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
        List<string> incompleteCloneTitles = new List<string>();
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
                        "Refusing to overwrite a generated V0.6 target associated with an ambiguous " +
                        "open document: title=" + document.GetTitle() + "; path=" + openFullPath);
                }

                if (document.GetSaveFlag())
                {
                    bool exactV06GeneratedPart = false;
                    foreach (string generatedPartStem in new string[]
                    {
                        InnerSideStem, LegStem, OuterCheekStem,
                        PivotPinStem, SpacerStem, LoadStopPinStem, LockPinStem,
                        HeelInsertStem, FootPadStem, TravelLidStem
                    })
                    {
                        if (SamePath(openFullPath, PartPath(cad, generatedPartStem)))
                        {
                            exactV06GeneratedPart = true;
                            break;
                        }
                    }

                    bool recentGeneratedPart = false;
                    if (exactV06GeneratedPart &&
                        document.GetType() == (int)swDocumentTypes_e.swDocPART &&
                        File.Exists(openFullPath))
                    {
                        TimeSpan generatedPartAge =
                            DateTime.UtcNow - File.GetLastWriteTimeUtc(openFullPath);
                        recentGeneratedPart =
                            generatedPartAge >= TimeSpan.Zero &&
                            generatedPartAge <= TimeSpan.FromMinutes(15.0);
                    }

                    if (recentGeneratedPart)
                    {
                        safeCloseTitles.Add(document.GetTitle());
                        recentGeneratedPartTitles.Add(document.GetTitle());
                        break;
                    }

                    AssemblyDoc dirtyAssembly = document as AssemblyDoc;
                    bool exactV06AssemblyTarget = false;
                    foreach (AssemblyStage dirtyStage in stages)
                    {
                        if (SamePath(openFullPath, AssemblyPath(cad, dirtyStage.TargetStem)))
                        {
                            exactV06AssemblyTarget = true;
                            break;
                        }
                    }

                    if (dirtyAssembly != null && exactV06AssemblyTarget)
                    {
                        List<Component2> dirtyComponents = TopLevelComponents(dirtyAssembly);
                        bool incompleteClone =
                            CountStem(dirtyComponents, OldSideStem) == 2 &&
                            CountStem(dirtyComponents, OldLegStem) == 2 &&
                            CountStem(dirtyComponents, InnerSideStem) == 0 &&
                            CountStem(dirtyComponents, LegStem) == 0 &&
                            CountStem(dirtyComponents, OuterCheekStem) == 0 &&
                            CountStem(dirtyComponents, PivotPinStem) == 0 &&
                            CountStem(dirtyComponents, SpacerStem) == 0 &&
                            CountStem(dirtyComponents, LoadStopPinStem) == 0 &&
                            CountStem(dirtyComponents, LockPinStem) == 0 &&
                            CountStem(dirtyComponents, HeelInsertStem) == 0 &&
                            CountStem(dirtyComponents, FootPadStem) == 0;
                        if (incompleteClone)
                        {
                            safeCloseTitles.Add(document.GetTitle());
                            incompleteCloneTitles.Add(document.GetTitle());
                            break;
                        }

                        bool completeV06SideHardware =
                            CountStem(dirtyComponents, OldSideStem) == 0 &&
                            CountStem(dirtyComponents, OldLegStem) == 0 &&
                            CountStem(dirtyComponents, InnerSideStem) == 2 &&
                            CountStem(dirtyComponents, LegStem) == 2 &&
                            CountStem(dirtyComponents, OuterCheekStem) == 2 &&
                            CountStem(dirtyComponents, PivotPinStem) == 2 &&
                            CountStem(dirtyComponents, SpacerStem) == 8 &&
                            CountStem(dirtyComponents, LoadStopPinStem) == 2 &&
                            CountStem(dirtyComponents, LockPinStem) == 2 &&
                            CountStem(dirtyComponents, HeelInsertStem) == 2 &&
                            CountStem(dirtyComponents, FootPadStem) == 2;

                        bool revisionMissingOrEmpty = false;
                        CustomPropertyManager properties =
                            document.Extension.CustomPropertyManager[string.Empty];
                        if (properties != null)
                        {
                            string revisionValue;
                            string revisionResolved;
                            bool revisionWasResolved;
                            bool revisionIsLinked;
                            properties.Get6(
                                "Desktop support revision",
                                false,
                                out revisionValue,
                                out revisionResolved,
                                out revisionWasResolved,
                                out revisionIsLinked);
                            revisionMissingOrEmpty =
                                string.IsNullOrWhiteSpace(revisionValue) &&
                                string.IsNullOrWhiteSpace(revisionResolved);
                        }

                        bool targetFileIsRecent = false;
                        if (File.Exists(openFullPath))
                        {
                            TimeSpan targetAge =
                                DateTime.UtcNow - File.GetLastWriteTimeUtc(openFullPath);
                            targetFileIsRecent =
                                targetAge >= TimeSpan.Zero &&
                                targetAge <= TimeSpan.FromMinutes(15.0);
                        }

                        if (completeV06SideHardware &&
                            revisionMissingOrEmpty &&
                            targetFileIsRecent)
                        {
                            safeCloseTitles.Add(document.GetTitle());
                            recentFailedBuildTitles.Add(document.GetTitle());
                            break;
                        }
                    }

                    throw new InvalidOperationException(
                        "Refusing to overwrite a dirty generated V0.6 target unless it is proven to be " +
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
            if (incompleteCloneTitles.Contains(title))
            {
                cad.Log("V06_CLOSED_INCOMPLETE_CLONE=" + title);
            }
            else if (recentFailedBuildTitles.Contains(title))
            {
                cad.Log("V06_CLOSED_RECENT_FAILED_BUILD=" + title);
            }
            else if (recentGeneratedPartTitles.Contains(title))
            {
                cad.Log("V06_CLOSED_RECENT_GENERATED_PART=" + title);
            }
            else
            {
                cad.Log("V06_CLOSED_CLEAN_GENERATED_TARGET=" + title);
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
        return snapshots;
    }

    private static void VerifyV04SnapshotsUnchanged(List<FileSnapshot> snapshots)
    {
        foreach (FileSnapshot snapshot in snapshots)
        {
            snapshot.RequireUnchanged();
        }
    }

    private static string CreateInnerSideFrame(RackCadSession cad, Stance stance60)
    {
        ModelDoc2 document = cad.NewPart(InnerSideStem);
        try
        {
            Point loadStop = DeployedLegLocalPointInCase(stance60,
                LoadStopLocalY, LoadStopLocalZ);
            Point lockDeploy = DeployedLegLocalPointInCase(stance60,
                LockLocalY, LockLocalZ);
            Body2 side = cad.Box(0.0, 0.0, 0.0,
                InnerSideThickness, CaseHeight, CaseDepth - ShellThickness);

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
                "V0.6 inner double-shear pivot clearance; restored frame material surrounds hole");
            side = SideHole(cad, side, loadStop.Y, loadStop.Z,
                LoadStopClearanceDiameter,
                "V0.6 fixed hard-stop pin clearance; normal down-load reacts here");
            side = SideHole(cad, side, lockDeploy.Y, lockDeploy.Z,
                LockPlateHoleDiameter,
                "V0.6 60-degree reverse-lock deployment hole");
            side = SideHole(cad, side, LockStorageCaseY, LockStorageCaseZ,
                LockPlateHoleDiameter,
                "V0.6 lock-pin folded storage hole");

            foreach (MountPoint mount in SpacerMounts)
            {
                side = SideHole(cad, side, mount.Y, mount.Z, SpacerHoleDiameter,
                    "flush-inside M4 outer-cheek spacer fixing");
            }

            foreach (double y in VentCentersY)
            {
                double coreLength = VentLengthY - VentWidthZ;
                side = cad.Cut(side,
                    cad.Box(0.0, y, VentCenterZ - VentWidthZ / 2.0,
                        InnerSideThickness + 0.8, coreLength, VentWidthZ),
                    "retained 18 x 4 mm side-vent core");
                foreach (double sign in Signs())
                {
                    side = SideHole(cad, side, y + sign * coreLength / 2.0,
                        VentCenterZ, VentWidthZ, "retained R2 side-vent end");
                }
            }

            cad.AddBody(document, side,
                "V0.6 3 mm inner side frame with pivot, hard-stop and two lock-pin locations");
            cad.ApplyMaterial(document, "6061-T6 (SS)", NaturalAluminium);
            cad.Property(document, "Physical geometry",
                "3 x 420 x 108 mm; assembly centres x +/-272.5; inside faces remain x +/-271");
            cad.Property(document, "Module envelope",
                "542 mm internal clear width retained; no hinge, pin or fastener may project inward of x +/-271");
            cad.Property(document, "Double-shear pivot",
                "Diameter 8.2 mm at case y -129,z52; 12.8 mm full-stack axle envelope");
            cad.Property(document, "Hard-stop hole",
                "Diameter 8.2 at case y " + Format(loadStop.Y) + ",z " +
                Format(loadStop.Z) + "; calculated from deployed leg q=(14,-28)");
            cad.Property(document, "Reverse-lock holes",
                "Diameter 5.8 deployment y " + Format(lockDeploy.Y) + ",z " +
                Format(lockDeploy.Z) + "; storage y -145,z72");
            cad.Property(document, "Outer-cheek mounting",
                "Four diameter 4.5 M4 clearances per side; inner heads must be flush and supplier fasteners remain pending");
            cad.Property(document, "Cover-lock clearance",
                "Original diameter 12.2 openings y +/-150,z55 retained");
            cad.Property(document, "Rail and ventilation",
                "Six independent M3 plus six independent M4 rail holes; eight visible 18 x 4 R2 vents retained");

            ValidatePart(document, 1,
                new Bounds(-1.5, -210.0, 0.0, 1.5, 210.0, 108.0), InnerSideStem);
            string path = cad.SavePart(document, InnerSideStem, true);
            cad.Log("V06_PART=" + path + ";solid_bodies=1");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static string CreateStableLeg(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(LegStem);
        try
        {
            Body2 arm = cad.Box(0.0, 3.5, -3.0,
                LegThickness, 167.0, ArmInPlaneWidth);
            Body2 root = cad.Cylinder(-LegThickness / 2.0,
                HingeLocalY, HingeLocalZ, 1.0, 0.0, 0.0,
                RootDiameter, LegThickness);
            Body2 metal = Unite(arm, root, "6 mm arm and diameter-36 root union");
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
                "one-millimetre heel-insert bearing seat; removes aluminium overlap");
            metal = cad.Cut(metal,
                cad.Cylinder(-LegThickness / 2.0 - 0.3,
                    HingeLocalY, HingeLocalZ, 1.0, 0.0, 0.0,
                    PivotClearanceDiameter, LegThickness + 0.6),
                "diameter 8.2 double-shear pivot and supplier-bushing clearance");
            metal = cad.Cut(metal,
                cad.Cylinder(-LegThickness / 2.0 - 0.3,
                    HingeLocalY + LockLocalY, HingeLocalZ + LockLocalZ,
                    1.0, 0.0, 0.0,
                    LockLegHoleDiameter, LegThickness + 0.6),
                "diameter 5.2 reverse-lock leg hole at local q=(0,-38)");

            cad.AddBody(document, metal,
                "6 mm 7075-T6 arm, diameter-36 root, load ear and non-overlapping heel seat");

            cad.ApplyMaterial(document, "7075-T6 (SN)", DarkAluminium);
            cad.Property(document, "Stable section",
                "7075-T6 Plate nominal 6 mm thickness x 18 mm in-plane arm width");
            cad.Property(document, "Root geometry",
                "Diameter 36 mm root around diameter 8.2 pivot; production blend and stress concentration remain DFM/FEA items");
            cad.Property(document, "Load ear",
                "Relative pivot y -8..+8,z -44..-12; 1 mm-deep y7..8 bearing seat clears the independent steel heel volume");
            cad.Property(document, "Reverse-lock hole",
                "Diameter 5.2 at relative pivot q=(0,-38); lock pin prevents reverse folding only");
            cad.Property(document, "Assembly position",
                "Fixed leg plane x +/-277.4 in folded and official 60-degree states; no axial pop-out");
            cad.Property(document, "Hinge and rubber-foot centre",
                "Local hinge y -75,z6; independent pad centre y95,z6; exact centre distance 170 mm");
            cad.Property(document, "Foot interface",
                "Metal arm terminates at local y87 tangent to diameter-16 rubber pad; final mechanical pad retention requires DFM, not adhesive-only completion");
            cad.Property(document, "Safety status",
                "Engineering geometry only; no physical bearing, pin shear, arm fatigue, stop load, anti-slip or loaded-CG validation completed");

            ValidatePart(document, 1,
                new Bounds(-3.0, -93.0, -38.0, 3.0, 87.0, 24.0), LegStem);
            string path = cad.SavePart(document, LegStem, true);
            cad.Log("V06_PART=" + path + ";solid_bodies=1;material=7075-T6_Plate");
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
            Point lockDeploy = DeployedLegLocalPointInCase(stance60,
                LockLocalY, LockLocalZ);
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
            cheek = ThroughCheekHole(cad, cheek, lockDeploy.Y, lockDeploy.Z,
                LockPlateHoleDiameter,
                "60-degree reverse-lock deployment hole");
            cheek = ThroughCheekHole(cad, cheek, LockStorageCaseY, LockStorageCaseZ,
                LockPlateHoleDiameter,
                "folded lock-pin storage hole");

            foreach (MountPoint mount in SpacerMounts)
            {
                cheek = ThroughCheekHole(cad, cheek, mount.Y, mount.Z,
                    SpacerHoleDiameter, "M4 spacer-stack outer clearance");
            }

            cheek = ThroughCheekHole(cad, cheek, FingerNotchY, FingerNotchZ,
                FingerNotchDiameter, "open-edge finger notch for folded foot extraction");

            cad.AddBody(document, cheek,
                "local 3 mm external cheek covering folded leg, foot and lower load ear");
            cad.ApplyMaterial(document, "6061-T6 (SS)", DarkAluminium);
            cad.Property(document, "Visual treatment",
                "Main strip y -170..52,z24..78 plus local lower ear y -154..-104,z4..32; lowered edge gives the two forward M4 spacer holes at z34 adequate nominal ligament");
            cad.Property(document, "Ventilation boundary",
                "Strip ends at z78; existing side vents z80..84 remain visible above the local cover");
            cad.Property(document, "Double-shear stack",
                "3 mm outer cheek at centres x +/-282.3; inner faces x +/-280.8; outer faces x +/-283.8");
            cad.Property(document, "Hard stop and reverse lock",
                "Hard stop y " + Format(loadStop.Y) + ",z " + Format(loadStop.Z) +
                "; deploy lock y " + Format(lockDeploy.Y) + ",z " + Format(lockDeploy.Z) +
                "; storage lock y -145,z72");
            cad.Property(document, "Cover-lock access",
                "Diameter 12.2 opening at case y -150,z55 retained through the local outer shell strip");
            cad.Property(document, "Pivot edge margin",
                "Diameter 8.2 pivot at y -129,z52 retains at least 9.9 mm nominal material beyond hole edge");
            cad.Property(document, "Manufacturing",
                "Four physical 6.8 mm spacers and flush M4 fasteners per side; fastener supplier and torque pending");

            ValidatePart(document, 1,
                new Bounds(-1.5, OuterCheekMinY, OuterCheekEarMinZ,
                    1.5, OuterCheekMaxY, OuterCheekMaxZ), OuterCheekStem);
            string path = cad.SavePart(document, OuterCheekStem, true);
            cad.Log("V06_PART=" + path + ";solid_bodies=1");
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
                "flush diameter-8 double-shear pivot envelope; 12.8 mm grip");
            cad.ApplyMaterial(document, "AISI 304", StainlessAppearance);
            cad.Property(document, "Nominal grip",
                "12.8 mm from inner side-frame inside face to outer-cheek outside face");
            cad.Property(document, "Assembly origin",
                "Pin geometric centre at case x +/-277.4,y -129,z52");
            cad.Property(document, "Retention boundary",
                "Both ends must remain flush within x +/-283.8; full shank through both shear planes required; supplier retention not frozen");
            cad.Property(document, "Structural status",
                "Diameter is a CAD envelope only; double-shear, bearing, wear and fatigue calculations require selected hardware");

            ValidatePart(document, 1,
                new Bounds(-6.4, -4.0, -4.0, 6.4, 4.0, 4.0), PivotPinStem);
            string path = cad.SavePart(document, PivotPinStem, true);
            cad.Log("V06_PART=" + path + ";solid_bodies=1");
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
                    "diameter 4.5 M4 through bore in 6.8 mm physical spacer");
            cad.AddBody(document, spacer,
                "diameter-10 x 6.8 mm physical outer-cheek spacer with M4 through bore");
            cad.ApplyMaterial(document, "AISI 304", StainlessAppearance);
            cad.Property(document, "Physical role",
                "Four instances per side prevent the outer cheek from floating and define the 6.8 mm leg cavity");
            cad.Property(document, "Fastener boundary",
                "M4 through fastener, flush inner head and flush/countersunk outer retention required; supplier pending");

            ValidatePart(document, 1,
                new Bounds(-3.4, -5.0, -5.0, 3.4, 5.0, 5.0), SpacerStem);
            string path = cad.SavePart(document, SpacerStem, true);
            cad.Log("V06_PART=" + path + ";solid_bodies=1");
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
                "diameter-8 x 12.8 full-shank hard-stop pin envelope");
            cad.ApplyMaterial(document, "AISI 304", StainlessAppearance);
            cad.Property(document, "Production material candidate",
                "17-4 PH stainless candidate; CAD uses AISI 304 appearance/material placeholder; final grade and heat treatment are not selected");
            cad.Property(document, "Load path",
                "Normal down-load reacts through the steel heel before the reverse-lock pin; contact-normal moment arm 28.0 mm");
            cad.Property(document, "Full-shank requirement",
                "No thread may cross either shear plane or the 6.8 mm cavity; supplier retention and bearing calculations pending");

            ValidatePart(document, 1,
                new Bounds(-6.4, -4.0, -4.0, 6.4, 4.0, 4.0), LoadStopPinStem);
            string path = cad.SavePart(document, LoadStopPinStem, true);
            cad.Log("V06_PART=" + path + ";solid_bodies=1;production_material_unselected=true");
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
                "diameter-5 x 12.8 removable reverse-lock pin envelope");
            cad.ApplyMaterial(document, "AISI 304", StainlessAppearance);
            cad.Property(document, "Function",
                "Prevents reverse folding after the hard stop is seated; must not carry normal downward operating load");
            cad.Property(document, "Positions",
                "Folded assemblies use case storage hole y -145,z72; 60-degree assembly uses the calculated deployment lock hole");
            cad.Property(document, "Retention boundary",
                "Pull ring, tether, detent and accidental-release protection require supplier selection and prototype validation");

            ValidatePart(document, 1,
                new Bounds(-6.4, -2.5, -2.5, 6.4, 2.5, 2.5), LockPinStem);
            string path = cad.SavePart(document, LockPinStem, true);
            cad.Log("V06_PART=" + path + ";solid_bodies=1;reverse_lock_only=true");
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
            Body2 heel = cad.Box(0.0, 0.0, -8.0, 6.0, 3.0, 16.0);
            cad.AddBody(document, heel,
                "6 x 3 x 16 mm steel heel insert; origin at body centre");
            cad.ApplyMaterial(document, "AISI 304", StainlessAppearance);
            cad.Property(document, "Leg-local placement",
                "Centre relative pivot q=(8.5,-28), or leg-part absolute y -66.5,z -22");
            cad.Property(document, "Bearing interface",
                "Occupies relative y7..10,z-36..-20; aluminium is recessed over y7..8 so CAD volumes do not overlap");
            cad.Property(document, "Production retention",
                "Final mechanically keyed capture and supplier process require DFM; this part is not declared adhesive-bonded completion");

            ValidatePart(document, 1,
                new Bounds(-3.0, -1.5, -8.0, 3.0, 1.5, 8.0), HeelInsertStem);
            string path = cad.SavePart(document, HeelInsertStem, true);
            cad.Log("V06_PART=" + path + ";solid_bodies=1;mechanical_keying_DFM_pending=true");
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
            cad.AddBody(document, pad,
                "diameter-16 x 6 rubber round-crown foot; origin at centre");
            cad.ApplyMaterial(document, "NEOPRENE", ElastomerAppearance);
            cad.Property(document, "Contact geometry",
                "Centre is exactly 170 mm from pivot and 8 mm above desk at 60 degrees; radius 8 gives lowest point Y=0");
            cad.Property(document, "Metal interface",
                "Round crown is tangent to the metal arm end in ideal CAD and is a separate non-interfering component");
            cad.Property(document, "Production retention",
                "Final mechanical capture, rubber compound, friction and supplier require DFM; adhesive-only completion is not claimed");

            ValidatePart(document, 1,
                new Bounds(-3.0, -8.0, -8.0, 3.0, 8.0, 8.0), FootPadStem);
            string path = cad.SavePart(document, FootPadStem, true);
            cad.Log("V06_PART=" + path + ";solid_bodies=1;material=NEOPRENE");
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
            double cavityWidth = CaseWidth + 1.0;
            double cavityHeight = CaseHeight + 1.0;
            double externalWidth = cavityWidth + 2.0 * TravelLidThickness;
            double externalHeight = cavityHeight + 2.0 * TravelLidThickness;
            double sideReturnCentreX = cavityWidth / 2.0 + TravelLidThickness / 2.0;
            double reliefCentreY = (TravelReliefMinY + TravelReliefMaxY) / 2.0;

            cad.AddBody(document,
                cad.Box(0.0, 0.0, TravelLidFrontZ - TravelLidThickness,
                    externalWidth, externalHeight, TravelLidThickness),
                "V0.6 deep travel-lid face preserving 70 mm patched-cable clearance");

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
                    " V0.6 folded-kickstand side-return relief");
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
                "1.5 mm 5052-H32 folded-panel concept; final bends and local reinforcement require DFM");
            cad.Property(document, "Folded-stand relief",
                "Bilateral side-return notch y[-155,41], z[5,15] mm; clears the V0.6 folded 170 mm kickstand while retaining the lower 75 mm return wall");
            cad.Property(document, "Source preservation",
                "Independent V0.6 part; DeepTravelLid_70mmClearance.SLDPRT remains unchanged");

            ValidatePart(document, 5,
                new Bounds(-276.0, -212.0, -71.5, 276.0, 212.0, 12.0),
                TravelLidStem);
            string path = cad.SavePart(document, TravelLidStem, true);
            cad.Log("V06_PART=" + path +
                ";solid_bodies=5;bilateral_folded_leg_relief=y[-155,41],z[5,15]");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static void BuildV06Assembly(
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
            throw new InvalidOperationException("The cloned V0.6 target is not an assembly: " + stage.TargetStem);
        }

        try
        {
            List<Component2> initialComponents = TopLevelComponents(assembly);
            Require(initialComponents.Count == stage.SourceComponentCount,
                "Unexpected exact V0.4 source count for " + stage.SourceStem + "; expected " +
                stage.SourceComponentCount.ToString(CultureInfo.InvariantCulture) + ", actual " +
                initialComponents.Count.ToString(CultureInfo.InvariantCulture));

            Dictionary<string, int> unchangedBefore = CaptureUnchangedSignatures(initialComponents,
                OldSideStem, OldLegStem, OldTravelLidStem);
            Dictionary<int, double[]> sideTransforms = CaptureSignedTransforms(
                initialComponents, OldSideStem, "V0.4 side frames");

            ReplaceExactlyTwo(document, assembly, PartPath(cad, OldSideStem), parts.InnerSide,
                "inner side frame");
            RestoreSignedTransforms(cad, document, assembly, InnerSideStem,
                sideTransforms, "V0.6 inner side frame");

            ReplaceExactlyTwo(document, assembly, PartPath(cad, OldLegStem), parts.Leg,
                "folding leg");
            PositionLegs(cad, document, assembly, stance);
            if (stage.IncludesLid)
            {
                ReplaceExactlyOne(document, assembly,
                    PartPath(cad, OldTravelLidStem), parts.TravelLid,
                    "V0.6 transport lid with folded-stand relief");
            }
            AddStableKickstandHardware(cad, document, assembly, stance, parts);

            document.Extension.ForceRebuildAll();
            Require(document.ForceRebuild3(false),
                "SOLIDWORKS could not rebuild " + stage.TargetStem);
            assembly.UpdateBox();

            ValidateAssembly(cad, stage, stance, document, assembly, unchangedBefore);
            WriteAssemblyProperties(cad, stage, stance, document);
            string saved = cad.SaveAssembly(document, stage.TargetStem, true);
            Require(SamePath(saved, AssemblyPath(cad, stage.TargetStem)),
                "The V0.6 native assembly save escaped its exact target path.");

            ValidateAssembly(cad, stage, stance, document, assembly, unchangedBefore);
            cad.Log("V06_ASSEMBLY=" + saved + ";top_level_components=" +
                (stage.SourceComponentCount + 20).ToString(CultureInfo.InvariantCulture));

            // STEP export and the post-export readback above can mark the native
            // assembly as needing regeneration.  Make the native SLDASM the last
            // written product and prove it is clean before this owned document is
            // closed.  This prevents warning 32 (NeedsRegen) on the next open.
            string finalNative = cad.SaveAssembly(document, stage.TargetStem, false);
            Require(SamePath(finalNative, AssemblyPath(cad, stage.TargetStem)),
                "The final clean V0.6 native save escaped its exact target path.");
            Require(!document.GetSaveFlag(),
                "The final V0.6 native assembly remains dirty after its post-validation save: " +
                stage.TargetStem);
            cad.Log("V06_FINAL_NATIVE_CLEAN=" + finalNative + ";save_flag=false");
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
                    "Refusing to overwrite an open V0.6 target with unsaved changes: " + targetPath);
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
            "Cannot create the exact independent V0.6 assembly copy; errors=" +
            errors.ToString(CultureInfo.InvariantCulture) + "; warnings=" +
            warnings.ToString(CultureInfo.InvariantCulture) + "; target=" + targetPath);
        Require(SamePath(Path.GetFullPath(source.GetPathName()), sourcePath),
            "Save-as-copy unexpectedly changed the active V0.4 source document identity.");
        Require(source.GetSaveFlag() == sourceWasDirty,
            "Save-as-copy changed the V0.4 source document dirty state: " + sourcePath);
        Require(string.Equals(sourceHash, FileSnapshot.HashFile(sourcePath), StringComparison.Ordinal),
            "The V0.4 source bytes changed during V0.6 cloning: " + sourcePath);
        cad.Log("V06_SOURCE_DIRTY_PRESERVED=" + sourceWasDirty.ToString(CultureInfo.InvariantCulture) +
            "; source_dirty_preserved=true; source=" + sourcePath);

        ModelDoc2 target = OpenExactAssembly(cad, targetPath);
        cad.Log("V06_CLONED_COPY=" + sourcePath + " -> " + targetPath +
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
            "Expected exactly two V0.6 replacement " + context + " occurrences.");
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
            "The V0.6 replacement " + context + " unexpectedly already exists.");

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
            "Expected exactly one V0.6 replacement " + context + " occurrence.");
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
            ApplyComponentTransform(document, assembly, utility, component, requested, context);
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

            int sign = ReadTransform(component, "replacement V0.6 leg")[9] < 0.0 ? -1 : 1;
            double[] requested = stance == null
                ? IdentityTransform(sign * LegPlaneX, FoldedY, FoldedZ)
                : DeployedLegTransform(stance, sign);
            ApplyComponentTransform(document, assembly, utility, component, requested,
                stance == null ? "folded V0.6 stable leg" : "deployed V0.6 stable leg");
            VerifyLegContact(component, requested, stance, sign);
            count++;
        }

        Require(count == 2, "Every V0.6 assembly must contain exactly two positioned V0.6 legs.");
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
        Point lockDeployCase = DeployedLegLocalPointInCase(deployment60,
            LockLocalY, LockLocalZ);
        foreach (int sign in IntegerSigns())
        {
            AddTransformed(cad, document, assembly, utility,
                parts.OuterCheek, "V06 " + SideName(sign) + " captured outer structural cheek",
                FixedCaseTransform(stance, sign * OuterCheekCentreX, 0.0, 0.0));

            AddTransformed(cad, document, assembly, utility,
                parts.PivotPin, "V06 " + SideName(sign) + " full-stack double-shear pivot pin",
                FixedCaseTransform(stance, sign * LegPlaneX, HingeCaseY, HingeCaseZ));

            for (int index = 0; index < SpacerMounts.Length; index++)
            {
                MountPoint mount = SpacerMounts[index];
                AddTransformed(cad, document, assembly, utility,
                    parts.Spacer,
                    "V06 " + SideName(sign) + " physical cheek spacer " +
                    (index + 1).ToString(CultureInfo.InvariantCulture),
                    FixedCaseTransform(stance, sign * LegPlaneX, mount.Y, mount.Z));
            }

            AddTransformed(cad, document, assembly, utility,
                parts.LoadStopPin, "V06 " + SideName(sign) + " fixed positive hard-stop pin",
                FixedCaseTransform(stance, sign * LegPlaneX,
                    loadStopCase.Y, loadStopCase.Z));

            double lockCaseY = stance == null ? LockStorageCaseY : lockDeployCase.Y;
            double lockCaseZ = stance == null ? LockStorageCaseZ : lockDeployCase.Z;
            AddTransformed(cad, document, assembly, utility,
                parts.LockPin, "V06 " + SideName(sign) +
                    (stance == null ? " folded storage lock pin" : " deployed reverse-lock pin"),
                FixedCaseTransform(stance, sign * LegPlaneX, lockCaseY, lockCaseZ));

            AddTransformed(cad, document, assembly, utility,
                parts.HeelInsert, "V06 " + SideName(sign) + " mechanically keyed heel insert",
                LegAttachedTransform(stance, sign, HeelCentreLocalY, HeelCentreLocalZ));

            AddTransformed(cad, document, assembly, utility,
                parts.FootPad, "V06 " + SideName(sign) + " independent rubber round foot",
                LegAttachedTransform(stance, sign, FootPadLocalY, FootPadLocalZ));
        }
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
        int expected = stage.SourceComponentCount + 20;
        Require(components.Count == expected,
            "V0.6 top-level count mismatch for " + stage.TargetStem + "; expected " +
            expected.ToString(CultureInfo.InvariantCulture) + ", actual " +
            components.Count.ToString(CultureInfo.InvariantCulture));

        Require(CountStem(components, OldSideStem) == 0 && CountStem(components, OldLegStem) == 0,
            "A legacy V0.4 side frame or axial-popout leg remains in " + stage.TargetStem);
        foreach (string legacyStem in LegacyV05KickstandStems)
        {
            Require(CountStem(components, legacyStem) == 0,
                "A legacy V0.5 kickstand component remains: " + legacyStem);
        }
        Require(CountStem(components, InnerSideStem) == 2, "V0.6 inner-side count must be two.");
        Require(CountStem(components, LegStem) == 2, "V0.6 leg count must be two.");
        Require(CountStem(components, OuterCheekStem) == 2, "V0.6 outer-cheek count must be two.");
        Require(CountStem(components, PivotPinStem) == 2, "V0.6 pivot-pin count must be two.");
        Require(CountStem(components, SpacerStem) == 8, "V0.6 physical spacer count must be eight.");
        Require(CountStem(components, LoadStopPinStem) == 2, "V0.6 hard-stop count must be two.");
        Require(CountStem(components, LockPinStem) == 2, "V0.6 reverse-lock count must be two.");
        Require(CountStem(components, HeelInsertStem) == 2, "V0.6 heel-insert count must be two.");
        Require(CountStem(components, FootPadStem) == 2, "V0.6 rubber-foot count must be two.");
        Require(CountStem(components, OldTravelLidStem) == 0,
            "The protected V0.4 travel lid must not remain in a V0.6 target.");
        Require(CountStem(components, TravelLidStem) == (stage.IncludesLid ? 1 : 0),
            "The V0.6 relieved travel-lid count does not match the assembly stage.");

        Dictionary<string, int> unchangedAfter = CaptureUnchangedSignatures(components,
            InnerSideStem, LegStem, OuterCheekStem, PivotPinStem, SpacerStem,
            LoadStopPinStem, LockPinStem, HeelInsertStem, FootPadStem,
            OldTravelLidStem, TravelLidStem);
        Require(DictionaryEqual(unchangedBefore, unchangedAfter),
            "A non-kickstand V0.4 component path or transform changed while producing " + stage.TargetStem);

        int leftSides = 0;
        int rightSides = 0;
        foreach (Component2 component in components)
        {
            if (!IsV06SideHardware(component))
            {
                continue;
            }

            double[] transform = ReadTransform(component, "V0.6 side hardware bounds");
            int sign = transform[9] < 0.0 ? -1 : 1;
            double[] box = component.GetBox(false, false) as double[];
            Require(box != null && box.Length >= 6,
                "SOLIDWORKS did not expose V0.6 side-hardware bounds for " + component.Name2);
            double minX = box[0] * 1000.0;
            double maxX = box[3] * 1000.0;
            if (sign < 0)
            {
                Require(maxX <= -InteriorClearWidth / 2.0 + GeometryTolerance,
                    "Left V0.6 side hardware invades the 542 mm internal module width: " + component.Name2);
                leftSides++;
            }
            else
            {
                Require(minX >= InteriorClearWidth / 2.0 - GeometryTolerance,
                    "Right V0.6 side hardware invades the 542 mm internal module width: " + component.Name2);
                rightSides++;
            }
            Require(minX >= -OuterHalfWidth - GeometryTolerance &&
                    maxX <= OuterHalfWidth + GeometryTolerance,
                "V0.6 side hardware exceeds the 567.6 mm width: " + component.Name2);
        }

        Require(leftSides == 12 && rightSides == 12,
            "Each V0.6 side must contain inner frame, leg, cheek, pivot, four spacers, stop, lock, heel and foot.");

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
            "V0.6 physical-product nominal minimum X");
        RequireClose(productMaxX, OuterHalfWidth, GeometryTolerance,
            "V0.6 physical-product nominal maximum X");

        VerifyLegInstances(components, stance);
        VerifyStableHardwareTransforms(components, stance);
        Require(document.ForceRebuild3(false),
            "Final V0.6 assembly rebuild readback failed for " + stage.TargetStem);
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

            double[] transform = ReadTransform(component, "V0.6 leg verification");
            int sign = transform[9] < 0.0 ? -1 : 1;
            VerifyLegContact(component, transform, stance, sign);
            count++;
        }
        Require(count == 2, "Two V0.6 leg transforms must be verified.");
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
                    "Exactly one V0.6 spacer must occupy each signed mounting point.");
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
                LockLocalY, LockLocalZ);
            Point expectedLock = stance == null
                ? FixedCasePoint(null, sign * LegPlaneX, LockStorageCaseY, LockStorageCaseZ)
                : FixedCasePoint(stance, sign * LegPlaneX, lockInCase.Y, lockInCase.Z);
            Component2 lockPin = FindSignedStem(components, LockPinStem, sign);
            RequirePointClose(TransformOrigin(ReadTransform(lockPin, "reverse-lock origin")),
                expectedLock, GroundTolerance, SideName(sign) + " reverse-lock origin");

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
            Point armEnd = ApplyTransformToPoint(
                legTransform, 0.0, FootPadLocalY - FootPadRadius, FootPadLocalZ);
            Point footCentre = TransformOrigin(expectedFootTransform);
            double armPadDistance = Math.Sqrt(
                Math.Pow(armEnd.Y - footCentre.Y, 2.0) +
                Math.Pow(armEnd.Z - footCentre.Z, 2.0));
            RequireClose(armPadDistance, FootPadRadius, GroundTolerance,
                SideName(sign) + " ideal tangent metal-to-rubber interface");

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
                    HingeLocalY + LockLocalY, HingeLocalZ + LockLocalZ);
                RequirePointClose(legLockHole, expectedLock, GroundTolerance,
                    SideName(sign) + " deployed reverse-lock alignment");
            }
        }
    }

    private static void WriteAssemblyProperties(
        RackCadSession cad,
        AssemblyStage stage,
        Stance stance,
        ModelDoc2 document)
    {
        cad.Property(document, "Desktop support revision",
            "V0.6 captured 6 mm 7075-T6 double-shear kickstands; official 60-degree stance only; V0.4 source preserved");
        cad.Property(document, "Internal module width",
            "542 mm between inner faces x -271 and +271; 104HP rail system unchanged");
        cad.Property(document, "Axial stack",
            "Per side: 3 mm inner frame + 6.8 mm cavity + 3 mm outer cheek; leg 6 mm with 0.4 mm clearance each face");
        cad.Property(document, "Overall width",
            "Nominal 567.6 mm, outer cheek faces x +/-283.8; desktop reference surfaces excluded from product width");
        cad.Property(document, "Physical outer-cheek support",
            "Four diameter-10 x 6.8 mm M4-through spacers per side; eight occurrences total");
        cad.Property(document, "Cover-lock and visual treatment",
            "Local outer cheek covers the folded leg, rubber foot and load ear to z4; z80..84 vents and finger access remain visible");
        cad.Property(document, "Primary load stop",
            "Diameter-8 full-stack hard-stop pin contacts the mechanically keyed steel heel at 60 degrees; contact-normal moment arm 28.0 mm");
        cad.Property(document, "Reverse lock",
            "Diameter-5 removable pin prevents reverse folding only; normal downward load must seat on the hard stop first");
        cad.Property(document, "Hard-stop screen",
            "400 N at 170 mm with factor 1.5 implies about 3.64 kN heel/stop reaction at a 28.0 mm contact-normal moment arm; engineering screen only, not physical PASS");
        cad.Property(document, "Source preservation",
            stage.SourceStem + ".SLDASM copied with SaveAs Copy; source native bytes are hash-checked unchanged");

        if (stage.IncludesLid)
        {
            cad.Property(document, "Transport lid relief",
                "V0.6 independent 5052-H32 lid replaces only the V0.4 travel lid; bilateral side-return notch y[-155,41], z[5,15] clears the folded leg");
        }

        if (stance == null)
        {
            cad.Property(document, "Folded leg origin",
                "x +/-277.4,y -54,z46; hinge y -129,z52; lock pins stored at case y -145,z72");
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
                "Both diameter-16 foot centres are Y=8 and round-crown lowest points are desk Y=0 within 0.1 mm in CAD");
            cad.Property(document, "Preliminary CG window",
                "Ideal centred fore-aft margin 66.885 mm; preliminary 20 kg plus 20 N at 300 mm, SF1.5 screen requires 45.87 mm, suggesting CG within about +/-21 mm of midpoint; not certification");
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
            "The fixed V0.6 hinge cannot place the round foot on the desktop at " +
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
        cad.Log("V06_" + Format(stance.FaceAngleDegrees) + "DEG_HINGE_HEIGHT_MM=" +
            Format(stance.HingeHeight));
        cad.Log("V06_" + Format(stance.FaceAngleDegrees) + "DEG_ROTATION_DEG=" +
            Format(stance.DetentDegrees));
        cad.Log("V06_" + Format(stance.FaceAngleDegrees) + "DEG_SUPPORT_MM=" +
            Format(stance.SupportFootprint));
        cad.Log("V06_" + Format(stance.FaceAngleDegrees) +
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
            RequireClose(footCentre.Y, 41.0, GroundTolerance,
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

        double[] reread = ReadTransform(component, "V0.6 leg transform readback");
        for (int index = 0; index < 12; index++)
        {
            RequireClose(reread[index], transform[index], TransformTolerance,
                "V0.6 leg transform element " + index.ToString(CultureInfo.InvariantCulture));
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
            double earGap = RectangleGap(centreY, centreZ,
                EarMinY, EarMaxY, EarMinZ, EarMaxZ, pinRadius);
            double heelGap = RectangleGap(centreY, centreZ,
                HeelMinY, HeelMaxY, HeelMinZ, HeelMaxZ, pinRadius);
            double gap = Math.Min(rootGap, Math.Min(earGap, heelGap));

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
                Require(rootGap > 0.0 && earGap > 0.0,
                    "The 60-degree endpoint must be controlled by the steel heel, not root or aluminium ear.");
            }
        }

        RequireClose(finalGap, 0.0, 0.000001,
            "60-degree combined hard-stop terminal gap");
        cad.Log("V06_STOP_SWEEP_SAMPLES=" +
            (StopSweepIntervals + 1).ToString(CultureInfo.InvariantCulture) +
            ";minimum_preterminal_gap_mm=" + Format(minimumPreTerminalGap) +
            ";terminal_gap_mm=" + Format(finalGap) +
            ";terminal_contact=steel_heel_only");
        cad.Log("V06_LOAD_STOP_CASE_YZ_MM=" + Format(fixedStop.Y) + "," +
            Format(fixedStop.Z));
        Point lockPoint = DeployedLegLocalPointInCase(deployment, LockLocalY, LockLocalZ);
        cad.Log("V06_DEPLOY_LOCK_CASE_YZ_MM=" + Format(lockPoint.Y) + "," +
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

    private static bool IsV06SideHardware(Component2 component)
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
            "The final V0.6 target is missing on disk: " + path);
        ModelDoc2 open = cad.Application.GetOpenDocumentByName(path) as ModelDoc2;
        Require(open == null,
            "The builder must leave the final V0.6 target closed; the preview tool exclusively owns final view activation: " + path);
        cad.Log("V06_FINAL_TARGET_READY=" + stem +
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
            "Refusing a V0.6 operation outside Rack4Modules: " + full);
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
        internal readonly bool IncludesLid;

        internal AssemblyStage(
            string sourceStem,
            string targetStem,
            int sourceComponentCount,
            double faceAngleDegrees,
            bool includesLid)
        {
            SourceStem = sourceStem;
            TargetStem = targetStem;
            SourceComponentCount = sourceComponentCount;
            FaceAngleDegrees = faceAngleDegrees;
            IncludesLid = includesLid;
        }
    }

    private sealed class PartPaths
    {
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
                "A protected V0.4 source changed during V0.6 generation: " + Path);
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
