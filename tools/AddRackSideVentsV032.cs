using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Compile this entry point together with SwCadCore.cs. It deliberately creates a
// new side-frame part instead of overwriting the existing V0.3 production baseline.
internal static class AddRackSideVentsV032
{
    [STAThread]
    private static int Main(string[] arguments)
    {
        SideVentV032Updater updater = null;

        try
        {
            if (arguments.Length != 1)
            {
                throw new ArgumentException("Exactly one existing Rack4Modules project root is required.");
            }

            RackCadSession session = new RackCadSession(Path.GetFullPath(arguments[0]));
            updater = new SideVentV032Updater(session);
            updater.Update();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V032_SIDE_VENT_UPDATE_FAILED=" + exception);
            return 1;
        }
        finally
        {
            if (updater != null)
            {
                updater.CloseOnlyDocumentsOpenedByUpdater();
            }
        }
    }
}

internal sealed class SideVentV032Updater
{
    private const string OriginalPartStem = "SideFrame_V03_RecessedLeg";
    private const string VentedPartStem = "SideFrame_V03_RecessedLeg_SideVent";
    private const string FrameMaterial = "6061-T6 (SS)";
    private const double MillimetresPerMetre = 1000.0;
    private const double GeometryTolerance = 0.04;
    private const double TransformTolerance = 0.00000001;

    // Side-frame plane: y is the 420 mm body height; z is the 108 mm depth.
    // Every slot is an 18 x 4 mm R2 capsule through-opening normal to x.
    // A 14 x 4 mm rectangular core and two diameter-4 mm X-axis cylinders
    // create the rounded ends; the eight-slot row is symmetric about y = 0.
    private const double SlotLengthY = 18.0;
    private const double SlotWidthZ = 4.0;
    private const double SlotEndRadius = 2.0;
    private const double SlotCoreLengthY = SlotLengthY - 2.0 * SlotEndRadius;
    private static readonly double[] SlotCentersY =
    {
        -120.0, -96.0, -72.0, -48.0, 48.0, 72.0, 96.0, 120.0
    };
    private static readonly double[] SlotCentersZ = { 82.0 };
    private static readonly double[] FrameAppearance = { 0.67, 0.70, 0.73 };

    private readonly RackCadSession cad;
    private readonly string originalPartPath;
    private readonly string ventedPartPath;
    private readonly string projectPrefix;
    private readonly double bodyWidth;
    private readonly double bodyHeight;
    private readonly double bodyDepth;
    private readonly double sideThickness;
    private readonly double shellThickness;
    private readonly double rowPitch;
    private readonly double railSpacing;
    private readonly double expectedSideX;
    private readonly List<string> ownedDocumentTitles = new List<string>();
    private readonly List<AssemblyStage> stages = new List<AssemblyStage>();

    internal SideVentV032Updater(RackCadSession session)
    {
        if (session == null)
        {
            throw new ArgumentNullException("session");
        }

        cad = session;
        projectPrefix = cad.Root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        originalPartPath = ProjectPath(cad.PartsDirectory, OriginalPartStem + ".SLDPRT");
        ventedPartPath = ProjectPath(cad.PartsDirectory, VentedPartStem + ".SLDPRT");
        bodyWidth = cad.N("enclosure", "outer_width");
        bodyHeight = cad.N("enclosure", "outer_height");
        bodyDepth = cad.N("enclosure", "body_depth");
        sideThickness = cad.N("enclosure", "side_frame_thickness");
        shellThickness = cad.N("enclosure", "body_thickness");
        rowPitch = cad.N("eurorack", "row_pitch");
        railSpacing = cad.N("eurorack", "mounting_hole_vertical_spacing");
        expectedSideX = bodyWidth / 2.0 - sideThickness / 2.0;

        // Process the visible open case last so the requested main assembly is
        // left active after both hidden secondary deliverables are refreshed.
        stages.Add(new AssemblyStage("Rack4Modules_TransportClosed_V03", 48));
        stages.Add(new AssemblyStage("Rack4Modules_ClearanceCheck_V03", 55));
        stages.Add(new AssemblyStage("Rack4Modules_OpenCase_V03", 47));
    }

    internal void Update()
    {
        ValidateFrozenDimensions();
        PreflightAllProjectFiles();

        ModelDoc2 originalPart = OpenNativeDocument(originalPartPath, swDocumentTypes_e.swDocPART);
        ValidateSideFrameGeometry(originalPart, OriginalPartStem, false);

        foreach (AssemblyStage stage in stages)
        {
            stage.Path = ProjectPath(cad.AssembliesDirectory, stage.Stem + ".SLDASM");
            stage.Document = OpenNativeDocument(stage.Path, swDocumentTypes_e.swDocASSEMBLY);
            stage.Initial = CaptureAssembly(stage, true);
            cad.Log("V032_PREFLIGHT=" + stage.Stem + ";components=" + stage.Initial.ComponentCount +
                ";old_sides=" + stage.Initial.OriginalSides.Count +
                ";vented_sides=" + stage.Initial.VentedSides.Count);
        }

        BuildOrReuseVentedSideFrame();

        foreach (AssemblyStage stage in stages)
        {
            ReplaceSideFramesAndSave(stage);
        }

        AssemblyStage visible = stages[stages.Count - 1];
        cad.Show(visible.Document);
        cad.Application.FrameState = (int)swWindowState_e.swWindowMaximized;
        cad.Log("V032_SIDE_VENT_PART=" + ventedPartPath);
        cad.Log("V032_SIDE_VENT_SIZE_MM=18x4_R2");
        cad.Log("V032_SIDE_VENT_Y_CENTERS_MM=-120,-96,-72,-48,48,72,96,120");
        cad.Log("V032_SIDE_VENT_Z_CENTERS_MM=82");
        cad.Log("V032_SIDE_VENT_COUNT_PER_SIDE=8");
        cad.Log("V032_SIDE_VENT_TOTAL_COUNT=16");
        cad.Log("V032_SIDE_VENT_OPEN_AREA_PER_SIDE_MM2=" +
            Format(SlotCentersY.Length * (SlotCoreLengthY * SlotWidthZ +
                Math.PI * SlotEndRadius * SlotEndRadius)));
        cad.Log("V032_SIDE_VENT_ALL_THREE_ASSEMBLIES_UPDATED=true");
    }

    internal void CloseOnlyDocumentsOpenedByUpdater()
    {
        string openAssemblyPath = ProjectPath(cad.AssembliesDirectory,
            "Rack4Modules_OpenCase_V03.SLDASM");
        ModelDoc2 openAssembly = cad.Application.GetOpenDocumentByName(openAssemblyPath) as ModelDoc2;
        string keepOpenTitle = openAssembly == null ? null : openAssembly.GetTitle();

        for (int index = ownedDocumentTitles.Count - 1; index >= 0; index--)
        {
            if (string.Equals(ownedDocumentTitles[index], keepOpenTitle,
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
                cad.Log("WARNING: Could not close updater-owned document " +
                    ownedDocumentTitles[index] + ": " + exception.Message);
            }
        }

        if (openAssembly != null)
        {
            try
            {
                cad.Show(openAssembly);
            }
            catch (Exception exception)
            {
                cad.Log("WARNING: Could not restore the visible V0.3 open assembly: " + exception.Message);
            }
        }
    }

    private void ValidateFrozenDimensions()
    {
        Require(Almost(bodyWidth, 548.0), "Expected the existing 548 mm V0.3 body width.");
        Require(Almost(bodyHeight, 420.0), "Expected the existing 420 mm V0.3 side-frame height.");
        Require(Almost(bodyDepth, 110.0), "Expected the existing 110 mm V0.3 body depth.");
        Require(Almost(sideThickness, 3.0), "Expected the existing 3 mm 6061 side-frame thickness.");
        Require(Almost(shellThickness, 2.0), "Expected the existing 2 mm rear shell thickness.");
        Require(Almost(rowPitch, 133.35), "Expected the existing 133.35 mm Eurorack row pitch.");
        Require(Almost(railSpacing, 122.5), "Expected the existing 122.5 mm 3U rail spacing.");
        Require(Almost(expectedSideX, 272.5), "Expected side-frame instance centres x = +/-272.5 mm.");

        foreach (double y in SlotCentersY)
        {
            Require(Math.Abs(y) + SlotLengthY / 2.0 <= bodyHeight / 2.0 - 81.0 + GeometryTolerance,
                "Rounded vent slots must retain at least 81 mm to the side-panel end edges.");
        }

        foreach (double z in SlotCentersZ)
        {
            double lower = z - SlotWidthZ / 2.0;
            double upper = z + SlotWidthZ / 2.0;
            Require(lower >= 80.0 - GeometryTolerance,
                "Vent slots must remain at least 16 mm behind the recessed-leg pocket.");
            Require(upper <= 93.0 - 9.0 + GeometryTolerance,
                "Rounded vent slots must retain at least 9 mm to the rear load-bearing zone z >= 93 mm.");
            Require(upper <= bodyDepth - shellThickness - 24.0 + GeometryTolerance,
                "Rounded vent slots must retain at least 24 mm to the rear side-frame edge.");
        }
    }

    private void PreflightAllProjectFiles()
    {
        Require(File.Exists(originalPartPath), "The original V0.3 side frame is missing: " + originalPartPath);

        foreach (AssemblyStage stage in stages)
        {
            string nativePath = ProjectPath(cad.AssembliesDirectory, stage.Stem + ".SLDASM");
            string existingExport = ProjectPath(cad.ExportsDirectory, stage.Stem + ".STEP");
            Require(File.Exists(nativePath), "The required V0.3 assembly is missing: " + nativePath);
            Require(File.Exists(existingExport), "The existing V0.3 STEP export is missing: " + existingExport);
        }
    }

    private void BuildOrReuseVentedSideFrame()
    {
        string ventedStepPath = ProjectPath(cad.ExportsDirectory, VentedPartStem + ".STEP");
        if (File.Exists(ventedPartPath))
        {
            ModelDoc2 existing = OpenNativeDocument(ventedPartPath, swDocumentTypes_e.swDocPART);
            ValidateSideFrameGeometry(existing, VentedPartStem, true);
            if (!File.Exists(ventedStepPath) || new FileInfo(ventedStepPath).Length == 0)
            {
                cad.SavePart(existing, VentedPartStem, true);
            }

            cad.Log("V032_REUSED_EXACT_EXISTING_SIDE_VENT_PART=true");
            return;
        }

        ModelDoc2 document = cad.NewPart(VentedPartStem);
        try
        {
            Body2 body = cad.Box(0.0, 0.0, 0.0,
                sideThickness, bodyHeight, bodyDepth - shellThickness);

            foreach (double railY in RailPositions())
            {
                body = HoleThroughSide(body, railY, 6.0, 3.4,
                    "Original 3U rail-end M3 clearance retained");
            }

            body = cad.Cut(body,
                cad.Box(0.0, -55.0, 42.0, sideThickness + 0.8, 164.0, 22.0),
                "Original recessed folding-leg pocket retained exactly");

            foreach (double catchY in new double[] { -150.0, 150.0 })
            {
                body = HoleThroughSide(body, catchY, 55.0, 12.2,
                    "Original internal cover-catch aperture retained");
            }

            foreach (double slotY in SlotCentersY)
            {
                foreach (double slotZ in SlotCentersZ)
                {
                    body = cad.Cut(body,
                        cad.Box(0.0, slotY, slotZ - SlotWidthZ / 2.0,
                            sideThickness + 0.8, SlotCoreLengthY, SlotWidthZ),
                        "Rounded side-vent 14 x 4 mm core y=" + Format(slotY) +
                        " z=" + Format(slotZ) + " mm");

                    foreach (double endSign in new double[] { -1.0, 1.0 })
                    {
                        double capY = slotY + endSign * SlotCoreLengthY / 2.0;
                        body = cad.Cut(body,
                            cad.Cylinder(-sideThickness / 2.0 - 0.3, capY, slotZ,
                                1.0, 0.0, 0.0, SlotWidthZ, sideThickness + 0.6),
                            "Rounded side-vent R2 end y=" + Format(capY) +
                            " z=" + Format(slotZ) + " mm");
                    }
                }
            }

            cad.AddBody(document, body,
                "Original recessed-leg side frame plus eight symmetric 18 x 4 mm R2 capsule cooling slots");
            cad.ApplyMaterial(document, FrameMaterial, FrameAppearance);
            cad.Property(document, "Project", "Rack4Modules V0.3.2 symmetric side ventilation");
            cad.Property(document, "Base geometry", OriginalPartStem + "; 3 x 420 x 108 mm");
            cad.Property(document, "Retained interfaces", "6 M3 rail holes; 164 x 22 mm leg pocket; 2 diameter-12.2 mm cover catches");
            cad.Property(document, "Ventilation slot count", "8 per side; 16 for both side frames");
            cad.Property(document, "Ventilation slot size", "18 mm along y x 4 mm along z; R2 rounded ends; through 3 mm wall");
            cad.Property(document, "Ventilation construction", "14 x 4 mm rectangular core plus two diameter-4 mm X-axis end cylinders");
            cad.Property(document, "Ventilation y centres", "-120,-96,-72,-48,+48,+72,+96,+120 mm");
            cad.Property(document, "Ventilation z centres", "82 mm; one rounded-slot row outside the rear load path");
            cad.Property(document, "Ventilation clearances", "Leg pocket 16 mm; rear load zone 9 mm; rear edge 24 mm; end edges 81 mm");
            cad.Property(document, "Ventilation area", "548.531 mm^2 per side; 1097.062 mm^2 across both sides");
            cad.Property(document, "Thermal boundary", "Passive openings only; no airflow, temperature or thermal certification implied");

            ValidateSideFrameGeometry(document, VentedPartStem, true);
            string savedPath = cad.SavePart(document, VentedPartStem, true);
            Require(SamePath(savedPath, ventedPartPath),
                "The newly saved side-vent part did not remain at its exact project path.");
            Require(File.Exists(ventedStepPath) && new FileInfo(ventedStepPath).Length > 0,
                "The side-vent part STEP export was not generated.");
            cad.Log("V032_CREATED_SIDE_VENT_PART=" + ventedPartPath);
        }
        finally
        {
            cad.Application.CloseDoc(document.GetTitle());
        }

        ModelDoc2 reopened = OpenNativeDocument(ventedPartPath, swDocumentTypes_e.swDocPART);
        ValidateSideFrameGeometry(reopened, VentedPartStem, true);
    }

    private void ReplaceSideFramesAndSave(AssemblyStage stage)
    {
        AssemblyDoc assembly = stage.Document as AssemblyDoc;
        Require(assembly != null, "The target document is not an assembly: " + stage.Path);
        ActivateDocument(stage.Document);

        int replacements = 0;
        AssemblySnapshot current = CaptureAssembly(stage, true);
        while (current.OriginalSides.Count > 0)
        {
            SideOccurrence original = current.OriginalSides[0];
            double[] preservedTransform = CopyTransform(original.Component);

            stage.Document.ClearSelection2(true);
            Require(original.Component.Select4(false, null, false),
                "Could not select the exact original side frame " + original.Path +
                " at x=" + Format(original.X) + " mm in " + stage.Stem);
            Require(assembly.ReplaceComponents(ventedPartPath, string.Empty, false, true),
                "SOLIDWORKS refused the exact selected side-frame replacement in " + stage.Stem);
            stage.Document.ClearSelection2(true);

            AssemblySnapshot replaced = CaptureAssembly(stage, true);
            SideOccurrence replacement = FindSideAtX(replaced.VentedSides, original.X);
            Require(replacement != null,
                "The selected side-frame replacement was not found at its original x position.");
            RestoreTransformIfNecessary(replacement.Component, preservedTransform);
            Require(TransformMatches(replacement.Component, preservedTransform),
                "The replacement side frame did not preserve its complete original assembly transform.");

            replacements++;
            cad.Log("V032_REPLACED_SIDE=" + stage.Stem + ";x_mm=" + Format(original.X) +
                ";match=exact-original-full-path-and-side-sign");
            current = CaptureAssembly(stage, true);
        }

        AssemblySnapshot final = CaptureAssembly(stage, false);
        Require(final.ComponentCount == stage.Initial.ComponentCount,
            "Side-frame replacement changed the total component count in " + stage.Stem);
        Require(final.OriginalSides.Count == 0 && final.VentedSides.Count == 2,
            "The target assembly does not contain exactly two vented side frames: " + stage.Stem);
        VerifyUnrelatedComponents(stage.Initial.OtherComponentSignatures,
            final.OtherComponentSignatures, stage.Stem);

        cad.Property(stage.Document, "Side ventilation revision", "0.3.2: symmetric short-side cooling slots");
        cad.Property(stage.Document, "Side ventilation pattern", "8 per side; 18 x 4 mm R2; y +/-48,72,96,120; z 82 mm");
        cad.Property(stage.Document, "Side ventilation safety", "Original rail screws, recessed legs and four cover catches preserved");
        cad.Property(stage.Document, "Thermal validation boundary", "Passive ventilation provision; airflow and temperatures are not measured");

        stage.Document.Extension.ForceRebuildAll();
        Require(stage.Document.ForceRebuild3(false),
            "SOLIDWORKS could not rebuild the updated side-frame assembly " + stage.Stem);
        assembly.UpdateBox();

        string nativePath = cad.SaveAssembly(stage.Document, stage.Stem, true);
        string stepPath = ProjectPath(cad.ExportsDirectory, stage.Stem + ".STEP");
        Require(SamePath(nativePath, stage.Path), "Assembly save changed its original native project path.");
        Require(File.Exists(stage.Path) && new FileInfo(stage.Path).Length > 0,
            "Updated native assembly is missing or empty: " + stage.Path);
        Require(File.Exists(stepPath) && new FileInfo(stepPath).Length > 0,
            "Updated V0.3 STEP export is missing or empty: " + stepPath);

        AssemblySnapshot afterSave = CaptureAssembly(stage, false);
        Require(afterSave.ComponentCount == stage.ExpectedComponents &&
                afterSave.OriginalSides.Count == 0 && afterSave.VentedSides.Count == 2,
            "Post-save side-frame verification failed for " + stage.Stem);
        VerifyUnrelatedComponents(stage.Initial.OtherComponentSignatures,
            afterSave.OtherComponentSignatures, stage.Stem);

        cad.Log("V032_UPDATED_ASSEMBLY=" + stage.Stem +
            ";components=" + afterSave.ComponentCount +
            ";new_replacements=" + replacements + ";native_and_step_saved=true");
    }

    private AssemblySnapshot CaptureAssembly(AssemblyStage stage, bool allowOriginalSides)
    {
        AssemblyDoc assembly = stage.Document as AssemblyDoc;
        Require(assembly != null, "Expected a native V0.3 assembly: " + stage.Path);

        Array components = assembly.GetComponents(false) as Array;
        Require(components != null, "The V0.3 assembly does not expose its component instances.");

        AssemblySnapshot snapshot = new AssemblySnapshot();
        snapshot.ComponentCount = assembly.GetComponentCount(false);
        Require(snapshot.ComponentCount == stage.ExpectedComponents,
            "Unexpected V0.3 component count in " + stage.Stem + ": expected " +
            stage.ExpectedComponents + ", actual " + snapshot.ComponentCount);

        foreach (object item in components)
        {
            Component2 component = item as Component2;
            Require(component != null, "A SOLIDWORKS assembly component is invalid in " + stage.Stem);
            string fullPath = NormalizeComponentPath(component);

            bool original = SamePath(fullPath, originalPartPath);
            bool vented = SamePath(fullPath, ventedPartPath);
            string filename = Path.GetFileName(fullPath);
            bool suspiciousSideName = string.Equals(filename, OriginalPartStem + ".SLDPRT",
                StringComparison.OrdinalIgnoreCase) || string.Equals(filename,
                VentedPartStem + ".SLDPRT", StringComparison.OrdinalIgnoreCase);

            Require(!suspiciousSideName || original || vented,
                "A same-named side frame outside the exact project part paths was found: " + fullPath);

            if (original || vented)
            {
                SideOccurrence occurrence = SnapshotSide(component, fullPath);
                if (original)
                {
                    Require(allowOriginalSides,
                        "An original unvented side frame remains after replacement in " + stage.Stem);
                    snapshot.OriginalSides.Add(occurrence);
                }
                else
                {
                    snapshot.VentedSides.Add(occurrence);
                }

                continue;
            }

            string signature = ComponentSignature(component, fullPath);
            int count;
            snapshot.OtherComponentSignatures.TryGetValue(signature, out count);
            snapshot.OtherComponentSignatures[signature] = count + 1;
        }

        int sideCount = snapshot.OriginalSides.Count + snapshot.VentedSides.Count;
        Require(sideCount == 2, "Expected exactly two side-frame instances in " + stage.Stem +
            "; actual " + sideCount.ToString(CultureInfo.InvariantCulture));

        SideOccurrence negative = null;
        SideOccurrence positive = null;
        foreach (SideOccurrence side in AllSides(snapshot))
        {
            Require(Almost(Math.Abs(side.X), expectedSideX) &&
                    Almost(side.Y, 0.0) && Almost(side.Z, 0.0),
                "A side frame is not located at x=+/-272.5,y=0,z=0 mm in " + stage.Stem);
            if (side.X < 0.0)
            {
                Require(negative == null, "More than one left-side frame exists in " + stage.Stem);
                negative = side;
            }
            else
            {
                Require(positive == null, "More than one right-side frame exists in " + stage.Stem);
                positive = side;
            }
        }

        Require(negative != null && positive != null,
            "Both the left and right side-frame locations are required in " + stage.Stem);
        return snapshot;
    }

    private void ValidateSideFrameGeometry(ModelDoc2 document, string expectedStem, bool requiresVents)
    {
        PartDoc part = document as PartDoc;
        Require(part != null, "The expected side-frame document is not a SOLIDWORKS part: " + expectedStem);
        Array rawBodies = part.GetBodies2((int)swBodyType_e.swSolidBody, false) as Array;
        Require(rawBodies != null && rawBodies.Length == 1,
            "The side frame must remain one connected solid body: " + expectedStem);
        Body2 body = rawBodies.GetValue(rawBodies.GetLowerBound(0)) as Body2;
        Require(body != null, "The expected side-frame solid body is unavailable: " + expectedStem);

        double[] bounds = part.GetPartBox(true) as double[];
        Require(bounds != null && bounds.Length >= 6,
            "No side-frame bounding box is available for " + expectedStem);
        Require(Almost(AxisLength(bounds, 0), sideThickness) &&
                Almost(AxisLength(bounds, 1), bodyHeight) &&
                Almost(AxisLength(bounds, 2), bodyDepth - shellThickness),
            "Side-frame outer dimensions changed from 3 x 420 x 108 mm: " + expectedStem);

        string database;
        string material = part.GetMaterialPropertyName2(string.Empty, out database);
        if (string.IsNullOrEmpty(material) && document.ConfigurationManager != null &&
            document.ConfigurationManager.ActiveConfiguration != null)
        {
            material = part.GetMaterialPropertyName2(
                document.ConfigurationManager.ActiveConfiguration.Name, out database);
        }

        Require(string.Equals(material, FrameMaterial, StringComparison.OrdinalIgnoreCase),
            "The side frame must retain the physical 6061-T6 (SS) material: " + expectedStem);

        List<CircularOpening> circles = SideCylinders(body);
        int railHoles = 0;
        foreach (double railY in RailPositions())
        {
            Require(HasCircle(circles, railY, 6.0, 3.4),
                "An existing 3U rail M3 hole changed at y=" + Format(railY) + " mm.");
            railHoles++;
        }

        Require(railHoles == 6 && CountDiameter(circles, 3.4) == 6,
            "The side frame must retain exactly six original 3U rail screw holes.");
        foreach (double catchY in new double[] { -150.0, 150.0 })
        {
            Require(HasCircle(circles, catchY, 55.0, 12.2),
                "An existing internal cover-catch opening changed at y=" + Format(catchY));
        }

        Require(CountDiameter(circles, 12.2) == 2,
            "The side frame must retain exactly two diameter-12.2 mm cover-catch holes.");

        List<RectangularOpening> rectangles = SideRectangles(body, bounds);
        Require(ContainsRectangle(rectangles, -55.0, 53.0, 164.0, 22.0),
            "The existing 164 x 22 mm recessed-leg pocket changed.");
        Require(rectangles.Count == 1,
            "Only the original recessed-leg pocket may remain a sharp-cornered rectangular opening.");

        List<CapsuleOpening> capsules = SideCapsules(body, bounds);
        int matchedVentSlots = 0;
        foreach (double slotY in SlotCentersY)
        {
            foreach (double slotZ in SlotCentersZ)
            {
                bool exists = ContainsCapsule(capsules, slotY, slotZ,
                    SlotLengthY, SlotWidthZ, SlotEndRadius);
                if (requiresVents)
                {
                    Require(exists, "A required 18 x 4 mm R2 ventilation capsule is missing at y=" +
                        Format(slotY) + ",z=" + Format(slotZ) + " mm.");
                }
                else
                {
                    Require(!exists,
                        "The original side-frame baseline unexpectedly contains the V0.3.2 capsule pattern.");
                }

                if (exists)
                {
                    matchedVentSlots++;
                }
            }
        }

        int expectedCapsuleCount = requiresVents ? 8 : 0;
        Require(capsules.Count == expectedCapsuleCount,
            "Unexpected R2 capsule opening count in " + expectedStem + ": expected " +
            expectedCapsuleCount + ", actual " + capsules.Count);
        Require(matchedVentSlots == expectedCapsuleCount,
            "The side-frame R2 ventilation capsule count does not match the frozen geometry.");

        cad.Log("V032_PART_GEOMETRY_VERIFIED=" + expectedStem +
            ";body=3x420x108mm;rail_holes=6;catch_holes=2;leg_pocket=164x22mm;vent_slots=" +
            matchedVentSlots.ToString(CultureInfo.InvariantCulture));
    }

    private Body2 HoleThroughSide(Body2 body, double y, double z,
        double diameter, string description)
    {
        return cad.Cut(body,
            cad.Cylinder(-sideThickness / 2.0 - 0.3, y, z,
                1.0, 0.0, 0.0, diameter, sideThickness + 0.6),
            description);
    }

    private IEnumerable<double> RailPositions()
    {
        foreach (double rowCenter in new double[] { -rowPitch, 0.0, rowPitch })
        {
            yield return rowCenter - railSpacing / 2.0;
            yield return rowCenter + railSpacing / 2.0;
        }
    }

    private ModelDoc2 OpenNativeDocument(string path, swDocumentTypes_e expectedType)
    {
        string exactPath = Path.GetFullPath(path);
        ModelDoc2 existing = cad.Application.GetOpenDocumentByName(exactPath) as ModelDoc2;
        if (existing != null)
        {
            Require(SamePath(existing.GetPathName(), exactPath),
                "The already-open SOLIDWORKS document does not match its requested project path.");
            return existing;
        }

        int errors = 0;
        int warnings = 0;
        ModelDoc2 document = cad.Application.OpenDoc6(exactPath, (int)expectedType,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, string.Empty,
            ref errors, ref warnings) as ModelDoc2;
        Require(document != null && errors == 0 && SamePath(document.GetPathName(), exactPath),
            "Cannot open exact native project document " + exactPath +
            "; errors=" + errors + "; warnings=" + warnings);

        ownedDocumentTitles.Add(document.GetTitle());
        int actionableWarnings = warnings & ~(int)swFileLoadWarning_e.swFileLoadWarning_AlreadyOpen;
        if (actionableWarnings != 0)
        {
            cad.Log("WARNING: SOLIDWORKS load status " + actionableWarnings +
                " for " + Path.GetFileName(exactPath));
        }

        return document;
    }

    private void ActivateDocument(ModelDoc2 document)
    {
        int errors = 0;
        ModelDoc2 active = cad.Application.ActivateDoc3(document.GetTitle(), false,
            (int)swRebuildOnActivation_e.swDontRebuildActiveDoc, ref errors) as ModelDoc2;
        Require(active != null, "Could not activate the requested V0.3 assembly; status=" + errors);
    }

    private SideOccurrence SnapshotSide(Component2 component, string path)
    {
        double[] transform = CopyTransform(component);
        SideOccurrence occurrence = new SideOccurrence();
        occurrence.Component = component;
        occurrence.Path = path;
        occurrence.X = transform[9] * MillimetresPerMetre;
        occurrence.Y = transform[10] * MillimetresPerMetre;
        occurrence.Z = transform[11] * MillimetresPerMetre;
        return occurrence;
    }

    private static double[] CopyTransform(Component2 component)
    {
        MathTransform transform = component.Transform2;
        Array values = transform == null ? null : transform.ArrayData as Array;
        Require(values != null && values.Length >= 16,
            "The assembly component does not expose its complete SOLIDWORKS transform.");

        double[] copy = new double[16];
        for (int index = 0; index < copy.Length; index++)
        {
            copy[index] = Convert.ToDouble(values.GetValue(index), CultureInfo.InvariantCulture);
        }

        return copy;
    }

    private void RestoreTransformIfNecessary(Component2 component, double[] expected)
    {
        if (TransformMatches(component, expected))
        {
            return;
        }

        MathUtility math = cad.Application.GetMathUtility() as MathUtility;
        Require(math != null, "SOLIDWORKS did not expose its assembly-transform utility.");
        MathTransform replacement = math.CreateTransform(expected) as MathTransform;
        Require(replacement != null, "SOLIDWORKS could not restore the original side-frame transform.");
        component.Transform2 = replacement;
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

    private static SideOccurrence FindSideAtX(List<SideOccurrence> sides, double x)
    {
        SideOccurrence found = null;
        foreach (SideOccurrence side in sides)
        {
            if (!Almost(side.X, x))
            {
                continue;
            }

            Require(found == null, "Multiple replacement side frames occupy the same side position.");
            found = side;
        }

        return found;
    }

    private static IEnumerable<SideOccurrence> AllSides(AssemblySnapshot snapshot)
    {
        foreach (SideOccurrence original in snapshot.OriginalSides)
        {
            yield return original;
        }

        foreach (SideOccurrence vented in snapshot.VentedSides)
        {
            yield return vented;
        }
    }

    private static string ComponentSignature(Component2 component, string normalizedPath)
    {
        double[] transform = CopyTransform(component);
        StringBuilder signature = new StringBuilder(normalizedPath.ToUpperInvariant());
        for (int index = 0; index < transform.Length; index++)
        {
            signature.Append('|');
            signature.Append(transform[index].ToString("R", CultureInfo.InvariantCulture));
        }

        return signature.ToString();
    }

    private static void VerifyUnrelatedComponents(Dictionary<string, int> before,
        Dictionary<string, int> after, string stage)
    {
        Require(before.Count == after.Count,
            "The set of unrelated assembly component paths or transforms changed in " + stage);

        foreach (KeyValuePair<string, int> item in before)
        {
            int actual;
            Require(after.TryGetValue(item.Key, out actual) && actual == item.Value,
                "A non-side component path, transform or occurrence count changed in " + stage);
        }
    }

    private string NormalizeComponentPath(Component2 component)
    {
        string rawPath = component.GetPathName();
        Require(!string.IsNullOrWhiteSpace(rawPath),
            "A virtual or unnamed component cannot be safely matched for side-frame replacement.");
        string normalized = Path.GetFullPath(rawPath);
        Require(normalized.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase),
            "Refusing to change an assembly that references a component outside the project: " + normalized);
        return normalized;
    }

    private string ProjectPath(string directory, string filename)
    {
        string fullPath = Path.GetFullPath(Path.Combine(directory, filename));
        if (projectPrefix != null)
        {
            Require(fullPath.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase),
                "Refusing a path outside the exact Rack4Modules project root: " + fullPath);
        }

        return fullPath;
    }

    private static List<CircularOpening> SideCylinders(Body2 body)
    {
        List<CircularOpening> result = new List<CircularOpening>();
        Array faces = body.GetFaces() as Array;
        Require(faces != null, "The side-frame solid does not expose its topological faces.");

        foreach (object item in faces)
        {
            Face2 face = item as Face2;
            Surface surface = face == null ? null : face.GetSurface() as Surface;
            if (surface == null || !surface.IsCylinder())
            {
                continue;
            }

            double[] values = surface.CylinderParams as double[];
            if (values == null || values.Length < 7 || Math.Abs(values[3]) < 0.99)
            {
                continue;
            }

            CircularOpening opening = new CircularOpening();
            opening.CenterY = values[1] * MillimetresPerMetre;
            opening.CenterZ = values[2] * MillimetresPerMetre;
            opening.Diameter = Math.Abs(values[6]) * 2.0 * MillimetresPerMetre;
            result.Add(opening);
        }

        return result;
    }

    private static bool HasCircle(List<CircularOpening> openings,
        double centerY, double centerZ, double diameter)
    {
        foreach (CircularOpening opening in openings)
        {
            if (Almost(opening.CenterY, centerY) && Almost(opening.CenterZ, centerZ) &&
                Almost(opening.Diameter, diameter))
            {
                return true;
            }
        }

        return false;
    }

    private static int CountDiameter(List<CircularOpening> openings, double diameter)
    {
        int count = 0;
        foreach (CircularOpening opening in openings)
        {
            if (Almost(opening.Diameter, diameter))
            {
                count++;
            }
        }

        return count;
    }

    private static List<RectangularOpening> SideRectangles(Body2 body, double[] partBounds)
    {
        List<RectangularOpening> result = new List<RectangularOpening>();
        Array faces = body.GetFaces() as Array;
        Require(faces != null, "No side-frame faces are available for opening validation.");

        foreach (object rawFace in faces)
        {
            Face2 face = rawFace as Face2;
            if (!IsOuterSideFace(face, partBounds))
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

                double minY = double.PositiveInfinity;
                double maxY = double.NegativeInfinity;
                double minZ = double.PositiveInfinity;
                double maxZ = double.NegativeInfinity;
                bool linear = true;

                foreach (object rawEdge in edges)
                {
                    Edge edge = rawEdge as Edge;
                    Curve curve = edge == null ? null : edge.GetCurve() as Curve;
                    if (curve == null || !curve.IsLine())
                    {
                        linear = false;
                        break;
                    }

                    Vertex[] vertices = { edge.GetStartVertex() as Vertex,
                        edge.GetEndVertex() as Vertex };
                    foreach (Vertex vertex in vertices)
                    {
                        double[] point = vertex == null ? null : vertex.GetPoint() as double[];
                        if (point == null || point.Length < 3)
                        {
                            linear = false;
                            break;
                        }

                        minY = Math.Min(minY, point[1] * MillimetresPerMetre);
                        maxY = Math.Max(maxY, point[1] * MillimetresPerMetre);
                        minZ = Math.Min(minZ, point[2] * MillimetresPerMetre);
                        maxZ = Math.Max(maxZ, point[2] * MillimetresPerMetre);
                    }
                }

                if (!linear || double.IsInfinity(minY))
                {
                    continue;
                }

                RectangularOpening opening = new RectangularOpening();
                opening.CenterY = (minY + maxY) / 2.0;
                opening.CenterZ = (minZ + maxZ) / 2.0;
                opening.LengthY = maxY - minY;
                opening.WidthZ = maxZ - minZ;

                if (!ContainsRectangle(result, opening.CenterY, opening.CenterZ,
                    opening.LengthY, opening.WidthZ))
                {
                    result.Add(opening);
                }
            }
        }

        return result;
    }

    private static List<CapsuleOpening> SideCapsules(Body2 body, double[] partBounds)
    {
        List<CapsuleOpening> result = new List<CapsuleOpening>();
        Array faces = body.GetFaces() as Array;
        Require(faces != null, "No side-frame faces are available for rounded-slot validation.");

        foreach (object rawFace in faces)
        {
            Face2 face = rawFace as Face2;
            if (!IsOuterSideFace(face, partBounds))
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

                int lineCount = 0;
                List<CircularOpening> roundedEnds = new List<CircularOpening>();
                bool valid = true;

                foreach (object rawEdge in edges)
                {
                    Edge edge = rawEdge as Edge;
                    Curve curve = edge == null ? null : edge.GetCurve() as Curve;
                    if (curve == null)
                    {
                        valid = false;
                        break;
                    }

                    if (curve.IsLine())
                    {
                        Vertex first = edge.GetStartVertex() as Vertex;
                        Vertex second = edge.GetEndVertex() as Vertex;
                        double[] start = first == null ? null : first.GetPoint() as double[];
                        double[] end = second == null ? null : second.GetPoint() as double[];
                        if (start == null || end == null || start.Length < 3 || end.Length < 3 ||
                            !Almost(Math.Abs((end[1] - start[1]) * MillimetresPerMetre),
                                SlotCoreLengthY) ||
                            !Almost((end[2] - start[2]) * MillimetresPerMetre, 0.0))
                        {
                            valid = false;
                            break;
                        }

                        lineCount++;
                    }
                    else if (curve.IsCircle())
                    {
                        double[] circle = curve.CircleParams as double[];
                        if (circle == null || circle.Length < 7 || Math.Abs(circle[3]) < 0.99)
                        {
                            valid = false;
                            break;
                        }

                        CircularOpening roundedEnd = new CircularOpening();
                        roundedEnd.CenterY = circle[1] * MillimetresPerMetre;
                        roundedEnd.CenterZ = circle[2] * MillimetresPerMetre;
                        roundedEnd.Diameter = Math.Abs(circle[6]) * 2.0 * MillimetresPerMetre;
                        roundedEnds.Add(roundedEnd);
                    }
                    else
                    {
                        valid = false;
                        break;
                    }
                }

                if (!valid || lineCount != 2 || roundedEnds.Count != 2 ||
                    !Almost(roundedEnds[0].CenterZ, roundedEnds[1].CenterZ) ||
                    !Almost(roundedEnds[0].Diameter, roundedEnds[1].Diameter))
                {
                    continue;
                }

                double radius = roundedEnds[0].Diameter / 2.0;
                CapsuleOpening opening = new CapsuleOpening();
                opening.CenterY = (roundedEnds[0].CenterY + roundedEnds[1].CenterY) / 2.0;
                opening.CenterZ = (roundedEnds[0].CenterZ + roundedEnds[1].CenterZ) / 2.0;
                opening.LengthY = Math.Abs(roundedEnds[1].CenterY - roundedEnds[0].CenterY) +
                    2.0 * radius;
                opening.WidthZ = 2.0 * radius;
                opening.EndRadius = radius;

                if (!ContainsCapsule(result, opening.CenterY, opening.CenterZ,
                    opening.LengthY, opening.WidthZ, opening.EndRadius))
                {
                    result.Add(opening);
                }
            }
        }

        return result;
    }

    private static bool ContainsCapsule(List<CapsuleOpening> openings,
        double centerY, double centerZ, double lengthY, double widthZ, double radius)
    {
        foreach (CapsuleOpening opening in openings)
        {
            if (Almost(opening.CenterY, centerY) && Almost(opening.CenterZ, centerZ) &&
                Almost(opening.LengthY, lengthY) && Almost(opening.WidthZ, widthZ) &&
                Almost(opening.EndRadius, radius))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOuterSideFace(Face2 face, double[] partBounds)
    {
        if (face == null || partBounds == null || partBounds.Length < 6)
        {
            return false;
        }

        Surface surface = face.GetSurface() as Surface;
        double[] faceBounds = face.GetBox() as double[];
        return surface != null && surface.IsPlane() &&
               faceBounds != null && faceBounds.Length >= 6 &&
               AxisLength(faceBounds, 0) <= GeometryTolerance &&
               AxisLength(faceBounds, 1) >= AxisLength(partBounds, 1) * 0.5 &&
               AxisLength(faceBounds, 2) >= AxisLength(partBounds, 2) * 0.5;
    }

    private static bool ContainsRectangle(List<RectangularOpening> openings,
        double centerY, double centerZ, double lengthY, double widthZ)
    {
        foreach (RectangularOpening opening in openings)
        {
            if (Almost(opening.CenterY, centerY) && Almost(opening.CenterZ, centerZ) &&
                Almost(opening.LengthY, lengthY) && Almost(opening.WidthZ, widthZ))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SamePath(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right),
            StringComparison.OrdinalIgnoreCase);
    }

    private static double AxisLength(double[] box, int axis)
    {
        return (box[axis + 3] - box[axis]) * MillimetresPerMetre;
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

    private sealed class AssemblyStage
    {
        internal readonly string Stem;
        internal readonly int ExpectedComponents;
        internal string Path;
        internal ModelDoc2 Document;
        internal AssemblySnapshot Initial;

        internal AssemblyStage(string stem, int expectedComponents)
        {
            Stem = stem;
            ExpectedComponents = expectedComponents;
        }
    }

    private sealed class AssemblySnapshot
    {
        internal int ComponentCount;
        internal readonly List<SideOccurrence> OriginalSides = new List<SideOccurrence>();
        internal readonly List<SideOccurrence> VentedSides = new List<SideOccurrence>();
        internal readonly Dictionary<string, int> OtherComponentSignatures =
            new Dictionary<string, int>(StringComparer.Ordinal);
    }

    private sealed class SideOccurrence
    {
        internal Component2 Component;
        internal string Path;
        internal double X;
        internal double Y;
        internal double Z;
    }

    private sealed class CircularOpening
    {
        internal double CenterY;
        internal double CenterZ;
        internal double Diameter;
    }

    private sealed class RectangularOpening
    {
        internal double CenterY;
        internal double CenterZ;
        internal double LengthY;
        internal double WidthZ;
    }

    private sealed class CapsuleOpening
    {
        internal double CenterY;
        internal double CenterZ;
        internal double LengthY;
        internal double WidthZ;
        internal double EndRadius;
    }
}
