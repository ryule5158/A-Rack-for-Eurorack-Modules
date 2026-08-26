using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// V0.5 is intentionally generated as a new set of native files.  This helper never
// saves a V0.4 source document, and it refuses to replace any generated target that
// is open with unsaved changes.  Compile together with SwCadCore.cs.
internal static class BuildRackDoubleShearKickstandsV05
{
    private const string OldSideStem = "SideFrame_V04_Vented_DualRailFix";
    private const string OldLegStem = "SideKickstand_V04_LowerPivot150mm";

    private const string InnerSideStem = "SideFrame_V05_Vented_DoubleShearInner";
    private const string LegStem = "SideKickstand_V05_DoubleShear150mm";
    private const string OuterCheekStem = "KickstandOuterCheek_V05_3mm";
    private const string PivotPinStem = "KickstandPivotPin_V05_Flush";
    private const string SpacerStem = "KickstandSpacer_V05_4p8mm";
    private const string IndexPinStem = "KickstandIndexPin_V05_SpringEnvelope";

    private const double CaseWidth = 548.0;
    private const double CaseHeight = 420.0;
    private const double CaseDepth = 110.0;
    private const double ShellThickness = 2.0;
    private const double InnerSideThickness = 3.0;
    private const double InnerSideCentreX = 272.5;
    private const double InteriorClearWidth = 542.0;

    // Strength-first V0.5 axial stack, mirrored left/right.
    // Right: inner side x=271..274, 4.8 cavity x=274..278.8,
    // outer cheek x=278.8..281.8.  The 4 mm leg has 0.4 mm nominal
    // clearance on both faces and is fixed at x=276.4 for every state.
    private const double CavityWidth = 4.8;
    private const double LegThickness = 4.0;
    private const double LegPlaneX = 276.4;
    private const double OuterCheekThickness = 3.0;
    private const double OuterCheekCentreX = 280.3;
    private const double OuterHalfWidth = 281.8;
    private const double OverallWidth = 563.6;
    private const double AxialClearanceEachSide = 0.4;

    private const double FoldedY = -54.0;
    private const double FoldedZ = 46.0;
    private const double HingeLocalY = -75.0;
    private const double HingeLocalZ = 6.0;
    private const double TipLocalY = 75.0;
    private const double TipLocalZ = 6.0;
    private const double HingeCaseY = -129.0;
    private const double HingeCaseZ = 52.0;
    private const double ArmContactLength = 150.0;
    private const double ArmInPlaneWidth = 18.0;
    private const double RootDiameter = 32.0;
    private const double PivotClearanceDiameter = 8.2;
    private const double PivotPinDiameter = 8.0;
    private const double PivotPinLength = 10.8;

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
    private const double SpacerLength = 4.8;

    // The local external strip covers the complete folded leg projection, while
    // leaving the z=80..84 side vents visible.  The y=42 edge carries a finger notch.
    private const double OuterCheekMinY = -170.0;
    private const double OuterCheekMaxY = 42.0;
    private const double OuterCheekMinZ = 30.0;
    private const double OuterCheekMaxZ = 78.0;
    private const double FingerNotchY = 42.0;
    private const double FingerNotchZ = 54.0;
    private const double FingerNotchDiameter = 14.0;

    // A fixed retractable indexing-pin envelope is represented, but the supplier,
    // three real detent profiles and load-bearing stop faces are deliberately not frozen.
    private const double IndexPinCentreX = 280.1;
    private const double IndexPinCaseY = -99.0;
    private const double IndexPinCaseZ = 52.0;
    private const double IndexEnvelopeDiameter = 10.0;
    private const double IndexEnvelopeLength = 3.4;
    private const double IndexHousingClearanceDiameter = 10.2;

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
        new MountPoint(27.0, 39.0),
        new MountPoint(27.0, 68.0)
    };

    private static readonly double[] NaturalAluminium = new double[] { 0.73, 0.75, 0.77 };
    private static readonly double[] DarkAluminium = new double[] { 0.12, 0.14, 0.17 };
    private static readonly double[] StainlessAppearance = new double[] { 0.66, 0.69, 0.70 };
    private static readonly double[] ElastomerAppearance = new double[] { 0.06, 0.07, 0.08 };

    [STAThread]
    private static int Main(string[] arguments)
    {
        try
        {
            if (arguments == null || arguments.Length != 1 ||
                string.IsNullOrWhiteSpace(arguments[0]))
            {
                throw new ArgumentException(
                    "Usage: BuildRackDoubleShearKickstandsV05.exe <Rack4Modules root>");
            }

            RackCadSession cad = new RackCadSession(Path.GetFullPath(arguments[0]));
            VerifyFrozenGeometry(cad);

            List<AssemblyStage> stages = BuildStages();
            PreflightExactSources(cad, stages);
            GuardGeneratedOutputs(cad, stages);

            List<FileSnapshot> v04Snapshots = CaptureV04Snapshots(cad, stages);
            Stance stance60 = CalculateStance(60.0);
            Stance stance75 = CalculateStance(75.0);
            LogStance(cad, stance60);
            LogStance(cad, stance75);

            PartPaths parts = new PartPaths();
            parts.InnerSide = CreateInnerSideFrame(cad);
            parts.Leg = CreateDoubleShearLeg(cad);
            parts.OuterCheek = CreateOuterCheek(cad);
            parts.PivotPin = CreatePivotPin(cad);
            parts.Spacer = CreateSpacer(cad);
            parts.IndexPin = CreateConceptIndexPinEnvelope(cad);

            foreach (AssemblyStage stage in stages)
            {
                Stance stance = null;
                if (Math.Abs(stage.FaceAngleDegrees - 60.0) < 0.001)
                {
                    stance = stance60;
                }
                else if (Math.Abs(stage.FaceAngleDegrees - 75.0) < 0.001)
                {
                    stance = stance75;
                }

                BuildV05Assembly(cad, stage, stance, parts);
            }

            VerifyV04SnapshotsUnchanged(v04Snapshots);
            ShowFinalAssembly(cad, "Rack4Modules_DesktopTilt60_V05");

            cad.Log("V05_INTERNAL_CLEAR_WIDTH_MM=542");
            cad.Log("V05_DOUBLE_SHEAR_STACK_MM=3_inner+4.8_cavity+3_outer");
            cad.Log("V05_LEG_MM=4_thickx18_wide;root_diameter_32;R8_transition_DFM_required");
            cad.Log("V05_LEG_PLANE_X_MM=+/-276.4;no_axial_popout");
            cad.Log("V05_OUTER_WIDTH_MM=563.6;strength_first_revision_supersedes_old_562_target");
            cad.Log("V05_INDEX_WARNING=concept_envelope_only;supplier_detents_and_load_stops_not_frozen");
            cad.Log("V05_V04_SOURCE_HASHES_UNCHANGED=true");
            cad.Log("V05_DOUBLE_SHEAR_BUILD_COMPLETE=true");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V05_DOUBLE_SHEAR_BUILD_FAILED=" + exception.ToString());
            Console.Error.Flush();
            return 1;
        }
    }

    private static List<AssemblyStage> BuildStages()
    {
        return new List<AssemblyStage>
        {
            new AssemblyStage("Rack4Modules_OpenCase_V04", "Rack4Modules_OpenCase_V05", 46, 0.0),
            new AssemblyStage("Rack4Modules_TransportClosed_V04", "Rack4Modules_TransportClosed_V05", 47, 0.0),
            new AssemblyStage("Rack4Modules_ClearanceCheck_V04", "Rack4Modules_ClearanceCheck_V05", 54, 0.0),
            new AssemblyStage("Rack4Modules_DesktopTilt60_V04", "Rack4Modules_DesktopTilt60_V05", 47, 60.0),
            new AssemblyStage("Rack4Modules_DesktopTilt75_V04", "Rack4Modules_DesktopTilt75_V05", 47, 75.0)
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
        RequireClose(OuterHalfWidth * 2.0, OverallWidth, 0.000001, "V0.5 overall width");
        RequireClose(FoldedY + HingeLocalY, HingeCaseY, 0.000001, "hinge y");
        RequireClose(FoldedZ + HingeLocalZ, HingeCaseZ, 0.000001, "hinge z");
        RequireClose(TipLocalY - HingeLocalY, ArmContactLength, 0.000001,
            "hinge-to-foot contact length");
        RequireClose(PivotPinLength,
            InnerSideThickness + CavityWidth + OuterCheekThickness, 0.000001,
            "flush double-shear pin grip length");
        RequireClose(SpacerLength, CavityWidth, 0.000001, "spacer-defined cavity");
        RequireClose(cad.N("rail", "length"), 528.32, 0.001, "unchanged 104HP rail length");
    }

    private static void PreflightExactSources(RackCadSession cad, List<AssemblyStage> stages)
    {
        RequireProjectFile(cad, PartPath(cad, OldSideStem));
        RequireProjectFile(cad, PartPath(cad, OldLegStem));

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
            InnerSideStem, LegStem, OuterCheekStem, PivotPinStem, SpacerStem, IndexPinStem
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
                        "Refusing to overwrite a generated V0.5 target associated with an ambiguous " +
                        "open document: title=" + document.GetTitle() + "; path=" + openFullPath);
                }

                if (document.GetSaveFlag())
                {
                    bool exactV05GeneratedPart = false;
                    foreach (string generatedPartStem in new string[]
                    {
                        InnerSideStem, LegStem, OuterCheekStem,
                        PivotPinStem, SpacerStem, IndexPinStem
                    })
                    {
                        if (SamePath(openFullPath, PartPath(cad, generatedPartStem)))
                        {
                            exactV05GeneratedPart = true;
                            break;
                        }
                    }

                    bool recentGeneratedPart = false;
                    if (exactV05GeneratedPart &&
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
                    bool exactV05AssemblyTarget = false;
                    foreach (AssemblyStage dirtyStage in stages)
                    {
                        if (SamePath(openFullPath, AssemblyPath(cad, dirtyStage.TargetStem)))
                        {
                            exactV05AssemblyTarget = true;
                            break;
                        }
                    }

                    if (dirtyAssembly != null && exactV05AssemblyTarget)
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
                            CountStem(dirtyComponents, IndexPinStem) == 0;
                        if (incompleteClone)
                        {
                            safeCloseTitles.Add(document.GetTitle());
                            incompleteCloneTitles.Add(document.GetTitle());
                            break;
                        }

                        bool completeV05SideHardware =
                            CountStem(dirtyComponents, OldSideStem) == 0 &&
                            CountStem(dirtyComponents, OldLegStem) == 0 &&
                            CountStem(dirtyComponents, InnerSideStem) == 2 &&
                            CountStem(dirtyComponents, LegStem) == 2 &&
                            CountStem(dirtyComponents, OuterCheekStem) == 2 &&
                            CountStem(dirtyComponents, PivotPinStem) == 2 &&
                            CountStem(dirtyComponents, SpacerStem) == 8 &&
                            CountStem(dirtyComponents, IndexPinStem) == 2;

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

                        if (completeV05SideHardware &&
                            revisionMissingOrEmpty &&
                            targetFileIsRecent)
                        {
                            safeCloseTitles.Add(document.GetTitle());
                            recentFailedBuildTitles.Add(document.GetTitle());
                            break;
                        }
                    }

                    throw new InvalidOperationException(
                        "Refusing to overwrite a dirty generated V0.5 target unless it is proven to be " +
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
                cad.Log("V05_CLOSED_INCOMPLETE_CLONE=" + title);
            }
            else if (recentFailedBuildTitles.Contains(title))
            {
                cad.Log("V05_CLOSED_RECENT_FAILED_BUILD=" + title);
            }
            else if (recentGeneratedPartTitles.Contains(title))
            {
                cad.Log("V05_CLOSED_RECENT_GENERATED_PART=" + title);
            }
            else
            {
                cad.Log("V05_CLOSED_CLEAN_GENERATED_TARGET=" + title);
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
        return snapshots;
    }

    private static void VerifyV04SnapshotsUnchanged(List<FileSnapshot> snapshots)
    {
        foreach (FileSnapshot snapshot in snapshots)
        {
            snapshot.RequireUnchanged();
        }
    }

    private static string CreateInnerSideFrame(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(InnerSideStem);
        try
        {
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
                "V0.5 inner double-shear pivot clearance; restored frame material surrounds hole");

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
                "V0.5 3 mm inner side frame; no recessed-leg through pocket at pivot");
            cad.ApplyMaterial(document, "6061-T6 (SS)", NaturalAluminium);
            cad.Property(document, "Physical geometry",
                "3 x 420 x 108 mm; assembly centres x +/-272.5; inside faces remain x +/-271");
            cad.Property(document, "Module envelope",
                "542 mm internal clear width retained; no hinge, pin or fastener may project inward of x +/-271");
            cad.Property(document, "Double-shear pivot",
                "Diameter 8.2 mm at case y -129,z52; solid 3 mm inner cheek restored around pivot");
            cad.Property(document, "Outer-cheek mounting",
                "Four diameter 4.5 M4 clearances per side; inner heads must be flush and supplier fasteners remain pending");
            cad.Property(document, "Cover-lock clearance",
                "Original diameter 12.2 openings y +/-150,z55 retained");
            cad.Property(document, "Rail and ventilation",
                "Six independent M3 plus six independent M4 rail holes; eight visible 18 x 4 R2 vents retained");

            ValidatePart(document, 1,
                new Bounds(-1.5, -210.0, 0.0, 1.5, 210.0, 108.0), InnerSideStem);
            string path = cad.SavePart(document, InnerSideStem, true);
            cad.Log("V05_PART=" + path + ";solid_bodies=1");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static string CreateDoubleShearLeg(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(LegStem);
        try
        {
            Body2 arm = cad.Box(0.0, -5.0, -3.0,
                LegThickness, 140.0, ArmInPlaneWidth);
            Body2 root = cad.Cylinder(-LegThickness / 2.0,
                HingeLocalY, HingeLocalZ, 1.0, 0.0, 0.0,
                RootDiameter, LegThickness);
            Body2 metal = Unite(arm, root, "4 mm arm and diameter-32 root union");
            metal = cad.Cut(metal,
                cad.Cylinder(-LegThickness / 2.0 - 0.3,
                    HingeLocalY, HingeLocalZ, 1.0, 0.0, 0.0,
                    PivotClearanceDiameter, LegThickness + 0.6),
                "diameter 8.2 double-shear pivot and supplier-bushing clearance");

            cad.AddBody(document, metal,
                "4 mm 6061 arm, 18 mm in-plane width, diameter-32 reinforced root");
            cad.AddBody(document,
                cad.Box(0.0, 70.0, 4.0, LegThickness, 10.0, 2.0),
                "replaceable anti-slip end envelope; exact contact datum local y75,z6");

            cad.ApplyMaterial(document, "6061-T6 (SS)", DarkAluminium);
            cad.Property(document, "Strength-first section",
                "6061-T6 nominal 4 mm thickness x 18 mm in-plane arm width; supersedes rejected 3 mm leg");
            cad.Property(document, "Root geometry",
                "Minimum diameter 32 mm root around diameter 8.2 pivot; manufacturing transition must be R8 or larger");
            cad.Property(document, "Transition validation",
                "CAD union represents the reinforced root envelope; actual machined R8 blend and stress concentration remain DFM/FEA items");
            cad.Property(document, "Assembly position",
                "Fixed leg plane x +/-276.4 in folded, 60 degree and 75 degree states; no axial pop-out");
            cad.Property(document, "Hinge and contact",
                "Local hinge y -75,z6; tip y75,z6; exact hinge-to-contact length 150 mm");
            cad.Property(document, "Indexing warning",
                "No production three-hole/dog/tooth geometry is frozen in this leg; the separate spring-pin part is an envelope only");
            cad.Property(document, "Safety status",
                "No physical bearing, pin shear, arm fatigue, stop load, anti-slip or loaded-CG validation completed");

            ValidatePart(document, 2,
                new Bounds(-2.0, -91.0, -10.0, 2.0, 75.0, 22.0), LegStem);
            string path = cad.SavePart(document, LegStem, true);
            cad.Log("V05_PART=" + path + ";solid_bodies=2");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static string CreateOuterCheek(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(OuterCheekStem);
        try
        {
            double centreY = (OuterCheekMinY + OuterCheekMaxY) / 2.0;
            Body2 cheek = cad.Box(0.0, centreY, OuterCheekMinZ,
                OuterCheekThickness,
                OuterCheekMaxY - OuterCheekMinY,
                OuterCheekMaxZ - OuterCheekMinZ);

            cheek = ThroughCheekHole(cad, cheek, HingeCaseY, HingeCaseZ,
                PivotClearanceDiameter, "outer double-shear pivot clearance");
            cheek = ThroughCheekHole(cad, cheek, -150.0, 55.0,
                CoverCatchDiameter, "rear cover-lock access/through clearance");
            cheek = ThroughCheekHole(cad, cheek, IndexPinCaseY, IndexPinCaseZ,
                IndexHousingClearanceDiameter, "concept retractable-index-pin housing clearance");

            foreach (MountPoint mount in SpacerMounts)
            {
                cheek = ThroughCheekHole(cad, cheek, mount.Y, mount.Z,
                    SpacerHoleDiameter, "M4 spacer-stack outer clearance");
            }

            cheek = ThroughCheekHole(cad, cheek, FingerNotchY, FingerNotchZ,
                FingerNotchDiameter, "open-edge finger notch for folded foot extraction");

            cad.AddBody(document, cheek,
                "local 3 mm external cheek strip covering the complete folded-leg projection");
            cad.ApplyMaterial(document, "6061-T6 (SS)", DarkAluminium);
            cad.Property(document, "Visual treatment",
                "Local side-shell strip y -170..42,z30..78 covers the folded leg; finger notch at open forward edge");
            cad.Property(document, "Ventilation boundary",
                "Strip ends at z78; existing side vents z80..84 remain visible above the local cover");
            cad.Property(document, "Double-shear stack",
                "3 mm outer cheek at centres x +/-280.3; inner faces x +/-278.8; outer faces x +/-281.8");
            cad.Property(document, "Cover-lock access",
                "Diameter 12.2 opening at case y -150,z55 retained through the local outer shell strip");
            cad.Property(document, "Pivot edge margin",
                "Diameter 8.2 pivot at y -129,z52 retains at least 9.9 mm nominal material beyond hole edge");
            cad.Property(document, "Manufacturing",
                "Four physical 4.8 mm spacers and flush M4 fasteners per side; fastener supplier and torque pending");

            ValidatePart(document, 1,
                new Bounds(-1.5, OuterCheekMinY, OuterCheekMinZ,
                    1.5, OuterCheekMaxY, OuterCheekMaxZ), OuterCheekStem);
            string path = cad.SavePart(document, OuterCheekStem, true);
            cad.Log("V05_PART=" + path + ";solid_bodies=1");
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
                "flush diameter-8 double-shear pivot envelope; 10.8 mm grip");
            cad.ApplyMaterial(document, "AISI 304", StainlessAppearance);
            cad.Property(document, "Nominal grip",
                "10.8 mm from inner side-frame inside face to outer-cheek outside face");
            cad.Property(document, "Assembly origin",
                "Pin geometric centre at case x +/-276.4,y -129,z52");
            cad.Property(document, "Retention boundary",
                "Both ends must remain flush within x +/-281.8; supplier shoulder, bushing and retention are not frozen");
            cad.Property(document, "Structural status",
                "Diameter is a CAD envelope only; double-shear, bearing, wear and fatigue calculations require selected hardware");

            ValidatePart(document, 1,
                new Bounds(-5.4, -4.0, -4.0, 5.4, 4.0, 4.0), PivotPinStem);
            string path = cad.SavePart(document, PivotPinStem, true);
            cad.Log("V05_PART=" + path + ";solid_bodies=1");
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
                "diameter 4.5 M4 through bore in 4.8 mm physical spacer");
            cad.AddBody(document, spacer,
                "diameter-10 x 4.8 mm physical outer-cheek spacer with M4 through bore");
            cad.ApplyMaterial(document, "AISI 304", StainlessAppearance);
            cad.Property(document, "Physical role",
                "Four instances per side prevent the outer cheek from floating and define the 4.8 mm leg cavity");
            cad.Property(document, "Fastener boundary",
                "M4 through fastener, flush inner head and flush/countersunk outer retention required; supplier pending");

            ValidatePart(document, 1,
                new Bounds(-2.4, -5.0, -5.0, 2.4, 5.0, 5.0), SpacerStem);
            string path = cad.SavePart(document, SpacerStem, true);
            cad.Log("V05_PART=" + path + ";solid_bodies=1");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static string CreateConceptIndexPinEnvelope(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(IndexPinStem);
        try
        {
            Body2 envelope = cad.Cylinder(-IndexEnvelopeLength / 2.0, 0.0, 0.0,
                1.0, 0.0, 0.0, IndexEnvelopeDiameter, IndexEnvelopeLength);
            cad.AddBody(document, envelope,
                "retractable spring-index-pin supplier envelope only");
            cad.ApplyMaterial(document, "AISI 304", StainlessAppearance);
            cad.Property(document, "Fixed case location",
                "Axis X; centre case y -99,z52, exactly 30 mm forward of the main pivot");
            cad.Property(document, "Concept only",
                "Envelope represents one retractable approximately diameter-4 locator per side; supplier not selected");
            cad.Property(document, "Three-position warning",
                "Folded, 60 degree and 75 degree leg holes/dogs are NOT cut or claimed aligned in V0.5 CAD");
            cad.Property(document, "Load-stop warning",
                "Index pin is positioning/locking only; production positive stop faces and load rating remain unfrozen");
            cad.Property(document, "Safety warning",
                "No retention, accidental-release, wear, pinch, contamination or fatigue validation completed");

            ValidatePart(document, 1,
                new Bounds(-1.7, -5.0, -5.0, 1.7, 5.0, 5.0), IndexPinStem);
            string path = cad.SavePart(document, IndexPinStem, true);
            cad.Log("V05_PART=" + path + ";solid_bodies=1;concept_only=true");
            return path;
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }
    }

    private static void BuildV05Assembly(
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
            throw new InvalidOperationException("The cloned V0.5 target is not an assembly: " + stage.TargetStem);
        }

        try
        {
            List<Component2> initialComponents = TopLevelComponents(assembly);
            Require(initialComponents.Count == stage.SourceComponentCount,
                "Unexpected exact V0.4 source count for " + stage.SourceStem + "; expected " +
                stage.SourceComponentCount.ToString(CultureInfo.InvariantCulture) + ", actual " +
                initialComponents.Count.ToString(CultureInfo.InvariantCulture));

            Dictionary<string, int> unchangedBefore = CaptureUnchangedSignatures(initialComponents,
                OldSideStem, OldLegStem);
            Dictionary<int, double[]> sideTransforms = CaptureSignedTransforms(
                initialComponents, OldSideStem, "V0.4 side frames");

            ReplaceExactlyTwo(document, assembly, PartPath(cad, OldSideStem), parts.InnerSide,
                "inner side frame");
            RestoreSignedTransforms(cad, document, assembly, InnerSideStem,
                sideTransforms, "V0.5 inner side frame");

            ReplaceExactlyTwo(document, assembly, PartPath(cad, OldLegStem), parts.Leg,
                "folding leg");
            PositionLegs(cad, document, assembly, stance);
            AddDoubleShearHardware(cad, document, assembly, stance, parts);

            document.Extension.ForceRebuildAll();
            Require(document.ForceRebuild3(false),
                "SOLIDWORKS could not rebuild " + stage.TargetStem);
            assembly.UpdateBox();

            ValidateAssembly(cad, stage, stance, document, assembly, unchangedBefore);
            WriteAssemblyProperties(cad, stage, stance, document);
            string saved = cad.SaveAssembly(document, stage.TargetStem, true);
            Require(SamePath(saved, AssemblyPath(cad, stage.TargetStem)),
                "The V0.5 native assembly save escaped its exact target path.");

            ValidateAssembly(cad, stage, stance, document, assembly, unchangedBefore);
            cad.Log("V05_ASSEMBLY=" + saved + ";top_level_components=" +
                (stage.SourceComponentCount + 14).ToString(CultureInfo.InvariantCulture));
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
                    "Refusing to overwrite an open V0.5 target with unsaved changes: " + targetPath);
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
            "Cannot create the exact independent V0.5 assembly copy; errors=" +
            errors.ToString(CultureInfo.InvariantCulture) + "; warnings=" +
            warnings.ToString(CultureInfo.InvariantCulture) + "; target=" + targetPath);
        Require(SamePath(Path.GetFullPath(source.GetPathName()), sourcePath),
            "Save-as-copy unexpectedly changed the active V0.4 source document identity.");
        Require(source.GetSaveFlag() == sourceWasDirty,
            "Save-as-copy changed the V0.4 source document dirty state: " + sourcePath);
        Require(string.Equals(sourceHash, FileSnapshot.HashFile(sourcePath), StringComparison.Ordinal),
            "The V0.4 source bytes changed during V0.5 cloning: " + sourcePath);
        cad.Log("V05_SOURCE_DIRTY_PRESERVED=" + sourceWasDirty.ToString(CultureInfo.InvariantCulture) +
            "; source_dirty_preserved=true; source=" + sourcePath);

        ModelDoc2 target = OpenExactAssembly(cad, targetPath);
        cad.Log("V05_CLONED_COPY=" + sourcePath + " -> " + targetPath +
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
            "Expected exactly two V0.5 replacement " + context + " occurrences.");
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

            int sign = ReadTransform(component, "replacement V0.5 leg")[9] < 0.0 ? -1 : 1;
            double[] requested = stance == null
                ? IdentityTransform(sign * LegPlaneX, FoldedY, FoldedZ)
                : DeployedLegTransform(stance, sign);
            ApplyComponentTransform(document, assembly, utility, component, requested,
                stance == null ? "folded V0.5 double-shear leg" : "deployed V0.5 double-shear leg");
            VerifyLegContact(component, requested, stance, sign);
            count++;
        }

        Require(count == 2, "Every V0.5 assembly must contain exactly two positioned V0.5 legs.");
    }

    private static void AddDoubleShearHardware(
        RackCadSession cad,
        ModelDoc2 document,
        AssemblyDoc assembly,
        Stance stance,
        PartPaths parts)
    {
        MathUtility utility = RequireMathUtility(cad);
        foreach (int sign in IntegerSigns())
        {
            AddTransformed(cad, document, assembly, utility,
                parts.OuterCheek, "V05 " + SideName(sign) + " local folded-leg cover cheek",
                FixedCaseTransform(stance, sign * OuterCheekCentreX, 0.0, 0.0));

            AddTransformed(cad, document, assembly, utility,
                parts.PivotPin, "V05 " + SideName(sign) + " flush double-shear pivot pin",
                FixedCaseTransform(stance, sign * LegPlaneX, HingeCaseY, HingeCaseZ));

            for (int index = 0; index < SpacerMounts.Length; index++)
            {
                MountPoint mount = SpacerMounts[index];
                AddTransformed(cad, document, assembly, utility,
                    parts.Spacer,
                    "V05 " + SideName(sign) + " physical cheek spacer " +
                    (index + 1).ToString(CultureInfo.InvariantCulture),
                    FixedCaseTransform(stance, sign * LegPlaneX, mount.Y, mount.Z));
            }

            AddTransformed(cad, document, assembly, utility,
                parts.IndexPin, "V05 " + SideName(sign) + " concept spring-index envelope WARNING",
                FixedCaseTransform(stance, sign * IndexPinCentreX, IndexPinCaseY, IndexPinCaseZ));
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
        int expected = stage.SourceComponentCount + 14;
        Require(components.Count == expected,
            "V0.5 top-level count mismatch for " + stage.TargetStem + "; expected " +
            expected.ToString(CultureInfo.InvariantCulture) + ", actual " +
            components.Count.ToString(CultureInfo.InvariantCulture));

        Require(CountStem(components, OldSideStem) == 0 && CountStem(components, OldLegStem) == 0,
            "A legacy V0.4 side frame or axial-popout leg remains in " + stage.TargetStem);
        Require(CountStem(components, InnerSideStem) == 2, "V0.5 inner-side count must be two.");
        Require(CountStem(components, LegStem) == 2, "V0.5 leg count must be two.");
        Require(CountStem(components, OuterCheekStem) == 2, "V0.5 outer-cheek count must be two.");
        Require(CountStem(components, PivotPinStem) == 2, "V0.5 pivot-pin count must be two.");
        Require(CountStem(components, SpacerStem) == 8, "V0.5 physical spacer count must be eight.");
        Require(CountStem(components, IndexPinStem) == 2,
            "V0.5 concept index-envelope count must be two.");

        Dictionary<string, int> unchangedAfter = CaptureUnchangedSignatures(components,
            InnerSideStem, LegStem, OuterCheekStem, PivotPinStem, SpacerStem, IndexPinStem);
        Require(DictionaryEqual(unchangedBefore, unchangedAfter),
            "A non-kickstand V0.4 component path or transform changed while producing " + stage.TargetStem);

        int leftSides = 0;
        int rightSides = 0;
        foreach (Component2 component in components)
        {
            if (!IsV05SideHardware(component))
            {
                continue;
            }

            double[] transform = ReadTransform(component, "V0.5 side hardware bounds");
            int sign = transform[9] < 0.0 ? -1 : 1;
            double[] box = component.GetBox(false, false) as double[];
            Require(box != null && box.Length >= 6,
                "SOLIDWORKS did not expose V0.5 side-hardware bounds for " + component.Name2);
            double minX = box[0] * 1000.0;
            double maxX = box[3] * 1000.0;
            if (sign < 0)
            {
                Require(maxX <= -InteriorClearWidth / 2.0 + GeometryTolerance,
                    "Left V0.5 side hardware invades the 542 mm internal module width: " + component.Name2);
                leftSides++;
            }
            else
            {
                Require(minX >= InteriorClearWidth / 2.0 - GeometryTolerance,
                    "Right V0.5 side hardware invades the 542 mm internal module width: " + component.Name2);
                rightSides++;
            }
            Require(minX >= -OuterHalfWidth - GeometryTolerance &&
                    maxX <= OuterHalfWidth + GeometryTolerance,
                "V0.5 side hardware exceeds the strength-first 563.6 mm width: " + component.Name2);
        }

        Require(leftSides == 9 && rightSides == 9,
            "Each V0.5 side must contain inner frame, leg, cheek, pivot, four spacers and one index envelope.");

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
            "V0.5 physical-product nominal minimum X");
        RequireClose(productMaxX, OuterHalfWidth, GeometryTolerance,
            "V0.5 physical-product nominal maximum X");

        VerifyLegInstances(components, stance);
        VerifyCoaxialComponentOrigins(components, stance);
        Require(document.ForceRebuild3(false),
            "Final V0.5 assembly rebuild readback failed for " + stage.TargetStem);
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

            double[] transform = ReadTransform(component, "V0.5 leg verification");
            int sign = transform[9] < 0.0 ? -1 : 1;
            VerifyLegContact(component, transform, stance, sign);
            count++;
        }
        Require(count == 2, "Two V0.5 leg transforms must be verified.");
    }

    private static void VerifyCoaxialComponentOrigins(List<Component2> components, Stance stance)
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
        }
    }

    private static void WriteAssemblyProperties(
        RackCadSession cad,
        AssemblyStage stage,
        Stance stance,
        ModelDoc2 document)
    {
        cad.Property(document, "Desktop support revision",
            "V0.5 fixed-plane 4 mm double-shear kickstands; V0.4 axial-popout source preserved");
        cad.Property(document, "Internal module width",
            "542 mm between inner faces x -271 and +271; 104HP rail system unchanged");
        cad.Property(document, "Axial stack",
            "Per side: 3 mm inner frame + 4.8 mm cavity + 3 mm outer cheek; leg 4 mm with 0.4 mm clearance each face");
        cad.Property(document, "Overall width",
            "Nominal 563.6 mm, outer cheek faces x +/-281.8; reliability-first revision supersedes old 562 mm target");
        cad.Property(document, "Physical outer-cheek support",
            "Four diameter-10 x 4.8 mm M4-through spacers per side; eight occurrences total");
        cad.Property(document, "Cover-lock and visual treatment",
            "Local outer strip covers folded leg, keeps z80..84 vents visible, retains diameter-12.2 rear lock clearance and finger notch");
        cad.Property(document, "Index-pin warning",
            "Two spring-index envelopes are conceptual only; supplier, three real detents and load stops are not frozen or validated");
        cad.Property(document, "Source preservation",
            stage.SourceStem + ".SLDASM copied with SaveAs Copy; source native bytes are hash-checked unchanged");

        if (stance == null)
        {
            cad.Property(document, "Folded leg origin",
                "x +/-276.4,y -54,z46; hinge y -129,z52; no axial release");
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
                "Both leg contact datums lie on desk Y=0 within 0.1 mm in CAD; no physical stability claim");
        }

        cad.Property(document, "Validation boundary",
            "Pivot hardware, true R8 blend, spacer fasteners, positive stops, loaded CG, friction, fatigue and prototype remain unverified");
    }

    private static Stance CalculateStance(double faceAngleDegrees)
    {
        double angle = DegreesToRadians(faceAngleDegrees);
        double sine = Math.Sin(angle);
        double cosine = Math.Cos(angle);
        double hingeHeight = (HingeCaseY - ShellContactY) * sine +
            (ShellContactZ - HingeCaseZ) * cosine;
        Require(hingeHeight > 0.0 && hingeHeight < ArmContactLength,
            "The fixed V0.5 hinge cannot reach the desktop at " + Format(faceAngleDegrees) + " degrees.");

        double horizontalReach = Math.Sqrt(
            ArmContactLength * ArmContactLength - hingeHeight * hingeHeight);
        double detent = faceAngleDegrees +
            RadiansToDegrees(Math.Asin(hingeHeight / ArmContactLength));
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
        cad.Log("V05_" + Format(stance.FaceAngleDegrees) + "DEG_HINGE_HEIGHT_MM=" +
            Format(stance.HingeHeight));
        cad.Log("V05_" + Format(stance.FaceAngleDegrees) + "DEG_ROTATION_DEG=" +
            Format(stance.DetentDegrees));
        cad.Log("V05_" + Format(stance.FaceAngleDegrees) + "DEG_SUPPORT_MM=" +
            Format(stance.SupportFootprint));
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
        Point tip = ApplyTransformToPoint(transform, 0.0, TipLocalY, TipLocalZ);
        if (stance == null)
        {
            RequireClose(hinge.X, sign * LegPlaneX, GroundTolerance, "folded hinge x");
            RequireClose(hinge.Y, HingeCaseY, GroundTolerance, "folded hinge y");
            RequireClose(hinge.Z, HingeCaseZ, GroundTolerance, "folded hinge z");
            RequireClose(tip.X, sign * LegPlaneX, GroundTolerance, "folded tip x");
            RequireClose(tip.Y, 21.0, GroundTolerance, "folded tip y");
            RequireClose(tip.Z, 52.0, GroundTolerance, "folded tip z");
        }
        else
        {
            RequireClose(hinge.X, sign * LegPlaneX, GroundTolerance, "deployed hinge x");
            RequireClose(hinge.Y, stance.HingeHeight, GroundTolerance, "deployed hinge height");
            RequireClose(tip.X, sign * LegPlaneX, GroundTolerance, "deployed tip x");
            RequireClose(tip.Y, 0.0, GroundTolerance, "deployed tip desk height");
            RequireClose(tip.Z, stance.SupportFootprint, GroundTolerance,
                "deployed rear support distance");
        }

        double[] reread = ReadTransform(component, "V0.5 leg transform readback");
        for (int index = 0; index < 12; index++)
        {
            RequireClose(reread[index], transform[index], TransformTolerance,
                "V0.5 leg transform element " + index.ToString(CultureInfo.InvariantCulture));
        }
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

    private static bool IsV05SideHardware(Component2 component)
    {
        return SameStem(component, InnerSideStem) ||
            SameStem(component, LegStem) ||
            SameStem(component, OuterCheekStem) ||
            SameStem(component, PivotPinStem) ||
            SameStem(component, SpacerStem) ||
            SameStem(component, IndexPinStem);
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

    private static void ShowFinalAssembly(RackCadSession cad, string stem)
    {
        ModelDoc2 document = OpenExactAssembly(cad, AssemblyPath(cad, stem));
        cad.Application.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY);
        cad.Application.Visible = true;
        cad.Application.UserControl = true;
        cad.Show(document);
        cad.Application.FrameState = (int)swWindowState_e.swWindowMaximized;
        cad.Log("V05_FINAL_VISIBLE_ASSEMBLY=" + stem);
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
            "Refusing a V0.5 operation outside Rack4Modules: " + full);
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

        internal AssemblyStage(
            string sourceStem,
            string targetStem,
            int sourceComponentCount,
            double faceAngleDegrees)
        {
            SourceStem = sourceStem;
            TargetStem = targetStem;
            SourceComponentCount = sourceComponentCount;
            FaceAngleDegrees = faceAngleDegrees;
        }
    }

    private sealed class PartPaths
    {
        internal string InnerSide;
        internal string Leg;
        internal string OuterCheek;
        internal string PivotPin;
        internal string Spacer;
        internal string IndexPin;
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
                "A protected V0.4 source changed during V0.5 generation: " + Path);
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
