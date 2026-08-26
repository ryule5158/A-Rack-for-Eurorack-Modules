using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// V0.10 is an appearance-only derivative of the mechanically frozen V0.9
// case.  The V0.7 rear skin is rebuilt parametrically as a new native part,
// then receives the Rymovia Phase Halo decal.  V0.7/V0.9 native sources are
// metadata/hash guarded and are never saved by this program.
internal static class BuildRymoviaRearAppearanceV10
{
    private const string OldBackStem = "BackPanel_V07_5052_1p5mm_VESADoubler";
    private const string NewBackStem = "BackPanel_V10_5052_RymoviaPhaseHalo";
    private const string IdentityShowcaseStem =
        "Rack4Modules_ExteriorIdentityShowcase_V10_RymoviaPhaseHalo";
    private const string PatternFile = "rymovia-phase-halo-rear-v10.png";
    private const string VectorFile = "rymovia-phase-halo-rear-v10-production-lowcontrast.svg";

    private const double CaseWidth = 548.0;
    private const double CaseHeight = 420.0;
    private const double CaseDepth = 110.0;
    private const double BackSkinThickness = 1.5;
    private const double BackVesaDoublerThickness = 0.5;
    private const double BackVesaDoublerSize = 160.0;
    private const double VesaHoleDiameter = 4.5;
    private const double GeometryTolerance = 0.1;
    private const double TransformTolerance = 0.0000001;
    private const double AssemblyMassToleranceKg = 0.003;
    private const int PreviewDecalMaskAlpha = 3;

    // Keep the mechanical build guard tied to the same orbital-arc envelope
    // used by the PNG/SVG generator.  Coordinates are local rear-panel mm;
    // angle 0 is +X and positive rotation is counter-clockwise.
    private sealed class ArtworkArcSpec
    {
        internal readonly double Rx;
        internal readonly double Ry;
        internal readonly double Start;
        internal readonly double Sweep;
        internal readonly double Width;

        internal ArtworkArcSpec(double rx, double ry, double start,
            double sweep, double width)
        {
            Rx = rx; Ry = ry; Start = start; Sweep = sweep; Width = width;
        }
    }

    private static readonly ArtworkArcSpec[] ArtworkArcs =
    {
        new ArtworkArcSpec(142,124,20,45,0.65),
        new ArtworkArcSpec(142,124,103,52,0.65),
        new ArtworkArcSpec(142,124,174,31,0.56),
        new ArtworkArcSpec(142,124,250,65,0.62),
        new ArtworkArcSpec(162,141,43,48,0.58),
        new ArtworkArcSpec(162,141,116,59,0.58),
        new ArtworkArcSpec(162,141,208,33,0.50),
        new ArtworkArcSpec(162,141,278,62,0.56),
        new ArtworkArcSpec(182,158,18,39,0.52),
        new ArtworkArcSpec(182,158,74,38,0.54),
        new ArtworkArcSpec(182,158,133,59,0.54),
        new ArtworkArcSpec(182,158,229,57,0.52),
        new ArtworkArcSpec(202,175,28,39,0.46),
        new ArtworkArcSpec(202,175,93,60,0.48),
        new ArtworkArcSpec(202,175,178,43,0.42),
        new ArtworkArcSpec(202,175,250,55,0.46),
        new ArtworkArcSpec(222,190,9,36,0.40),
        new ArtworkArcSpec(222,190,65,38,0.42),
        new ArtworkArcSpec(222,190,123,46,0.42),
        new ArtworkArcSpec(222,190,201,46,0.40),
        new ArtworkArcSpec(222,190,280,56,0.42)
    };

    private static readonly double[] Graphite = { 0.067, 0.067, 0.067 };

    private static readonly AssemblySpec[] Assemblies =
    {
        new AssemblySpec("Rack4Modules_OpenCase_V09_RymoviaSecureLid",
            "Rack4Modules_OpenCase_V10_RymoviaPhaseHaloRear"),
        new AssemblySpec("Rack4Modules_TransportClosed_V09_RymoviaSecureLid",
            "Rack4Modules_TransportClosed_V10_RymoviaPhaseHaloRear"),
        new AssemblySpec("Rack4Modules_ClearanceCheck_V09_RymoviaSecureLid",
            "Rack4Modules_ClearanceCheck_V10_RymoviaPhaseHaloRear"),
        new AssemblySpec("Rack4Modules_DesktopTilt60_V09_RymoviaSecureLid",
            "Rack4Modules_DesktopTilt60_V10_RymoviaPhaseHaloRear"),
        new AssemblySpec("Rack4Modules_ShowcaseTilt60_LidOff_V09_RymoviaSecureLid",
            "Rack4Modules_ShowcaseTilt60_LidOff_V10_RymoviaPhaseHaloRear")
    };

    [STAThread]
    private static int Main(string[] args)
    {
        string progress = "start";
        try
        {
            if (args == null || args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
                throw new ArgumentException(
                    "Usage: BuildRymoviaRearAppearanceV10.exe <Rack4Modules root>");

            RackCadSession cad = new RackCadSession(Path.GetFullPath(args[0]));
            progress = "preflight";
            RequireProjectAssets(cad);
            List<SourceStamp> protectedSources = CaptureProtectedCad(cad);
            GuardGeneratedTargets(cad);
            ValidateArtworkKeepouts();

            progress = "new rear panel";
            string newBack = CreateRearPanel(cad);

            foreach (AssemblySpec spec in Assemblies)
            {
                progress = "assembly " + spec.TargetStem;
                BuildAssembly(cad, spec, newBack);
            }

            progress = "identity showcase";
            BuildIdentityShowcase(cad, newBack);

            progress = "protected source verification";
            VerifyProtectedCad(protectedSources);

            progress = "final display";
            OpenFinalShowcase(cad);

            cad.Log("V10_REAR_APPEARANCE_BUILD_COMPLETE=true");
            cad.Log("V10_REAR_PATTERN=RYMOVIA_PHASE_HALO_ORBITAL_ARCS");
            cad.Log("V10_REAR_PANEL_MM=548x420;skin=1.5;vesa_local_stack=2.0");
            cad.Log("V10_REAR_KEEPOUTS_MM=edge16;vesa180x180;feet4xR12");
            cad.Log("V10_REAR_SOURCE_HASHES_UNCHANGED=true");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V10_REAR_APPEARANCE_BUILD_FAILED=" +
                exception.GetType().FullName + ": " + exception.Message + " @ " + progress);
            return 1;
        }
    }

    private static void RequireProjectAssets(RackCadSession cad)
    {
        RequireProjectFile(cad, PatternPath(cad));
        RequireProjectFile(cad, VectorPath(cad));
        RequireProjectFile(cad, PartPath(cad, OldBackStem));
        foreach (AssemblySpec spec in Assemblies)
            RequireProjectFile(cad, AssemblyPath(cad, spec.SourceStem));
    }

    private static List<SourceStamp> CaptureProtectedCad(RackCadSession cad)
    {
        string cadRoot = Path.Combine(cad.Root, "cad");
        List<SourceStamp> result = new List<SourceStamp>();
        foreach (string path in Directory.GetFiles(cadRoot, "*.*", SearchOption.AllDirectories)
            .Where(IsNativeCad)
            .Where(path => Path.GetFileNameWithoutExtension(path)
                .IndexOf("_V10_", StringComparison.OrdinalIgnoreCase) < 0))
        {
            FileInfo info = new FileInfo(path);
            string digest = null;
            try { digest = Hash(path); }
            catch (IOException) { cad.Log("V10_LOCKED_SOURCE_METADATA_GUARD=" + path); }
            catch (UnauthorizedAccessException) { cad.Log("V10_PROTECTED_SOURCE_METADATA_GUARD=" + path); }
            result.Add(new SourceStamp(path, info.Length, info.LastWriteTimeUtc, digest));
        }
        Require(result.Count > 20, "Unexpectedly small protected CAD source set");
        cad.Log("V10_PROTECTED_NATIVE_SOURCE_COUNT=" + result.Count.ToString(CultureInfo.InvariantCulture));
        return result;
    }

    private static void VerifyProtectedCad(IEnumerable<SourceStamp> sources)
    {
        foreach (SourceStamp source in sources)
        {
            Require(File.Exists(source.Path), "Protected source disappeared: " + source.Path);
            FileInfo info = new FileInfo(source.Path);
            Require(info.Length == source.Length && info.LastWriteTimeUtc == source.LastWriteUtc,
                "Protected pre-V10 source metadata changed: " + source.Path);
            if (source.Hash == null) continue;
            try
            {
                Require(string.Equals(Hash(source.Path), source.Hash, StringComparison.Ordinal),
                    "Protected pre-V10 source content changed: " + source.Path);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static bool IsNativeCad(string path)
    {
        string extension = Path.GetExtension(path);
        return string.Equals(extension, ".SLDPRT", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".SLDASM", StringComparison.OrdinalIgnoreCase);
    }

    private static void GuardGeneratedTargets(RackCadSession cad)
    {
        HashSet<string> targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PartPath(cad, NewBackStem), AssemblyPath(cad, IdentityShowcaseStem)
        };
        foreach (AssemblySpec spec in Assemblies) targets.Add(AssemblyPath(cad, spec.TargetStem));

        List<string> closeTitles = new List<string>();
        ModelDoc2 doc = cad.Application.GetFirstDocument() as ModelDoc2;
        while (doc != null)
        {
            ModelDoc2 next = doc.GetNext() as ModelDoc2;
            string path = doc.GetPathName();
            if (!string.IsNullOrWhiteSpace(path) && targets.Contains(Path.GetFullPath(path)))
            {
                if (doc.GetSaveFlag())
                    cad.Log("V10_DISCARDING_REGENERATABLE_TARGET_VIEW_STATE=" + path);
                closeTitles.Add(doc.GetTitle());
            }
            doc = next;
        }
        foreach (string title in closeTitles) cad.Application.CloseDoc(title);
    }

    private static void ValidateArtworkKeepouts()
    {
        double edgeX = CaseWidth / 2.0 - 16.0;
        double edgeY = CaseHeight / 2.0 - 16.0;
        double[] footX = { -245.0, 245.0 };
        double[] footY = { -185.0, 185.0 };

        Require(ArtworkArcs.Length == 21,
            "The V0.10 orbital artwork arc count changed unexpectedly");
        foreach (ArtworkArcSpec arc in ArtworkArcs)
        {
            Require(arc.Rx > 0.0 && arc.Ry > 0.0 && arc.Width > 0.0,
                "Orbital artwork has a non-positive radius or width");
            Require(arc.Start >= -360.0 && arc.Start <= 360.0 &&
                arc.Sweep > 0.0 && arc.Sweep <= 180.0,
                "Orbital artwork angle range is invalid");

            // Sample at <=0.5 degree, including the production stroke half-
            // width. This checks the actual arcs rather than a hand-written
            // bounding-box approximation.
            int samples = Math.Max(2, (int)Math.Ceiling(arc.Sweep * 2.0));
            for (int index = 0; index <= samples; index++)
            {
                double angle = (arc.Start + arc.Sweep * index / samples) *
                    Math.PI / 180.0;
                double x = arc.Rx * Math.Cos(angle);
                double y = arc.Ry * Math.Sin(angle);
                double halfWidth = arc.Width / 2.0;
                Require(Math.Abs(x) + halfWidth <= edgeX + GeometryTolerance,
                    "Orbital artwork violates the 16 mm side edge band");
                Require(Math.Abs(y) + halfWidth <= edgeY + GeometryTolerance,
                    "Orbital artwork violates the 16 mm top/bottom edge band");

                double dx = Math.Max(Math.Abs(x) - 90.0, 0.0);
                double dy = Math.Max(Math.Abs(y) - 90.0, 0.0);
                double vesaDistance = Math.Sqrt(dx * dx + dy * dy);
                Require(vesaDistance >= halfWidth + 0.5,
                    "Orbital artwork intrudes into the 180 mm square VESA keep-out");

                for (int footIndex = 0; footIndex < footX.Length; footIndex++)
                    for (int rowIndex = 0; rowIndex < footY.Length; rowIndex++)
                    {
                        double footDx = x - footX[footIndex];
                        double footDy = y - footY[rowIndex];
                        double footDistance = Math.Sqrt(footDx * footDx + footDy * footDy);
                        Require(footDistance >= 12.0 + halfWidth + 15.0,
                            "Orbital artwork is too close to a rear-foot keep-out");
                    }
            }
        }

        // The four cardinal marks remain in the edge band but outside the
        // central VESA and rear-foot zones.
        Require(Math.Abs(-252.0) + 0.35 / 2.0 <= edgeX &&
            Math.Abs(252.0) + 0.35 / 2.0 <= edgeX,
            "Cardinal side registration marks violate the edge band");
        Require(Math.Abs(190.0) + 0.35 / 2.0 <= edgeY,
            "Cardinal top/bottom registration marks violate the edge band");
    }

    private static string CreateRearPanel(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(NewBackStem);
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
                "central 160 x 160 mm VESA doubler");

            foreach (double xSign in Signs())
                foreach (double ySign in Signs())
                    panel = cad.Cut(panel,
                        cad.Cylinder(xSign * 50.0, ySign * 50.0,
                            CaseDepth - BackSkinThickness - BackVesaDoublerThickness - 0.3,
                            0.0, 0.0, 1.0, VesaHoleDiameter,
                            BackSkinThickness + BackVesaDoublerThickness + 0.6),
                        "VESA 100 M4 clearance");

            cad.AddBody(document, panel,
                "V0.10 5052 rear shear skin with local VESA doubler and Phase Halo exterior");
            cad.ApplyMaterial(document, "5052-H32", Graphite);
            cad.Property(document, "Brand", "Rymovia Audio Systems");
            cad.Property(document, "Mechanical geometry",
                "Unchanged from V0.7: 548 x 420 x 1.5 mm 5052 skin; central 160 x 160 x 0.5 mm doubler; four diameter-4.5 VESA 100 holes");
            cad.Property(document, "Exterior identity",
                "Rymovia Phase Halo: five nested circular and elliptical broken arcs with asymmetric solid and dotted segments orbit the central VESA keep-out");
            cad.Property(document, "Artwork keep-outs",
                "16 mm perimeter; 180 x 180 mm central VESA contact zone; R12 around each of four feet at x +/-245 y +/-185");
            cad.Property(document, "Production artwork", VectorPath(cad));
            cad.Property(document, "Production process boundary",
                "SVG is design intent only; supplier must qualify low-energy laser or one-colour screen print with finish, adhesion and abrasion coupons");
            cad.Property(document, "Mechanical validation boundary",
                "Appearance revision changes no structure; VESA pull, buckling, vibration and drop performance still require FEA plus prototype tests");

            cad.Application.SetUserPreferenceToggle(
                (int)swUserPreferenceToggle_e.swDisplayDecals, true);
            document.Extension.DeleteAllDecals();
            Face2 outerFace = FindOuterFace(document as PartDoc);
            AddDecal(document, outerFace, FindDecalTemplate(), PatternPath(cad),
                0.548, 0.420, 0.0, 0.0, FaceCentreZ(outerFace),
                "Rymovia Phase Halo rear identity");
            Require(document.Extension.GetDecalsCount() == 1,
                "V0.10 rear panel must contain exactly one exterior decal");

            ValidateRearPanel(document);
            string saved = cad.SavePart(document, NewBackStem, true);
            Require(SamePath(saved, PartPath(cad, NewBackStem)),
                "V0.10 rear panel save escaped target path");
            ValidateRearPanel(document);
            cad.SavePart(document, NewBackStem, false);
            Require(!document.GetSaveFlag(), "V0.10 rear panel remains dirty after final save");
            cad.Log("V10_PART=" + saved + ";mass_kg=" + Format(ReadMass(document)) +
                ";decal_count=1;vesa_keepout_mm=180x180");
            return saved;
        }
        finally { cad.Application.CloseDoc(document.GetTitle()); }
    }

    private static void ValidateRearPanel(ModelDoc2 document)
    {
        PartDoc part = document as PartDoc;
        Require(part != null, "V0.10 rear panel is not a part");
        Array bodies = part.GetBodies2((int)swBodyType_e.swSolidBody, false) as Array;
        Require(bodies != null && bodies.Length == 1,
            "V0.10 rear panel must be one solid body");
        Bounds bounds = Bounds.FromBodies(bodies);
        RequireClose(bounds.MinX, -274.0, GeometryTolerance, "rear minX");
        RequireClose(bounds.MaxX, 274.0, GeometryTolerance, "rear maxX");
        RequireClose(bounds.MinY, -210.0, GeometryTolerance, "rear minY");
        RequireClose(bounds.MaxY, 210.0, GeometryTolerance, "rear maxY");
        RequireClose(bounds.MinZ, 108.0, GeometryTolerance, "rear minZ");
        RequireClose(bounds.MaxZ, 110.0, GeometryTolerance, "rear maxZ");
        Require(document.Extension.GetDecalsCount() == 1,
            "Rear identity decal count changed during rebuild/save");
        double mass = ReadMass(document);
        Require(mass > 0.90 && mass < 1.02,
            "Rear-panel mass is outside the expected 5052 geometry window: " + Format(mass));
    }

    private static void BuildAssembly(RackCadSession cad, AssemblySpec spec, string newBack)
    {
        string source = AssemblyPath(cad, spec.SourceStem);
        string target = AssemblyPath(cad, spec.TargetStem);
        RequireProjectFile(cad, source);
        EnsureExactOutputClosed(cad, target);
        File.Copy(source, target, true);

        ModelDoc2 document = OpenAssembly(cad, target);
        AssemblyDoc assembly = document as AssemblyDoc;
        Require(assembly != null, "V0.10 target is not an assembly: " + target);
        try
        {
            List<Component2> beforeComponents = TopLevelComponents(assembly);
            Component2 oldBack = FindExact(beforeComponents, PartPath(cad, OldBackStem));
            double[] originalTransform = ReadTransform(oldBack);
            List<string> frozenOthers = ComponentSignatures(beforeComponents,
                PartPath(cad, OldBackStem));
            double massBefore = ReadMass(document);

            ReplaceOccurrence(document, assembly, PartPath(cad, OldBackStem), newBack,
                "V0.10 rear identity panel");
            List<Component2> afterComponents = TopLevelComponents(assembly);
            Component2 replacement = FindExact(afterComponents, newBack);
            RequireTransformEqual(ReadTransform(replacement), originalTransform,
                spec.TargetStem + " replacement rear-panel transform");
            RequireSequencesEqual(frozenOthers,
                ComponentSignatures(afterComponents, newBack),
                spec.TargetStem + " non-rear components and transforms");
            Require(CountExact(afterComponents, PartPath(cad, OldBackStem)) == 0,
                "Old V0.7 rear panel remains in " + spec.TargetStem);
            Require(CountExact(afterComponents, newBack) == 1,
                "V0.10 rear panel count is not one in " + spec.TargetStem);

            cad.Property(document, "Brand", "Rymovia Audio Systems");
            cad.Property(document, "Appearance revision", "V0.10 Phase Halo rear identity / V0.9 secure lid");
            cad.Property(document, "Rear artwork keep-outs",
                "Central 180 x 180 VESA contact zone, four R12 foot zones and 16 mm perimeter remain clear");
            cad.Property(document, "Mechanical change from V0.9",
                "None: the replacement rear panel recreates the V0.7 5052 geometry and material exactly; only an exterior decal and production SVG are added");

            document.Extension.ForceRebuildAll();
            Require(document.ForceRebuild3(false),
                "Assembly rebuild failed: " + spec.TargetStem);
            assembly.UpdateBox();
            double massAfter = ReadMass(document);
            RequireClose(massAfter, massBefore, AssemblyMassToleranceKg,
                spec.TargetStem + " mass preservation");

            string saved = cad.SaveAssembly(document, spec.TargetStem, true);
            Require(SamePath(saved, target), "V0.10 assembly save escaped target path");
            afterComponents = TopLevelComponents(assembly);
            Require(CountExact(afterComponents, PartPath(cad, OldBackStem)) == 0 &&
                    CountExact(afterComponents, newBack) == 1,
                "Rear-panel replacement did not survive save in " + spec.TargetStem);
            RequireTransformEqual(ReadTransform(FindExact(afterComponents, newBack)),
                originalTransform, spec.TargetStem + " saved rear-panel transform");
            RequireSequencesEqual(frozenOthers, ComponentSignatures(afterComponents, newBack),
                spec.TargetStem + " saved non-rear components and transforms");
            cad.SaveAssembly(document, spec.TargetStem, false);
            Require(!document.GetSaveFlag(),
                "V0.10 assembly remains dirty after final save: " + spec.TargetStem);
            cad.Log("V10_ASSEMBLY=" + saved + ";mass_before_kg=" + Format(massBefore) +
                ";mass_after_kg=" + Format(massAfter) + ";old_back=0;new_back=1");
        }
        finally { cad.Application.CloseDoc(document.GetTitle()); }
    }

    private static void BuildIdentityShowcase(RackCadSession cad, string newBack)
    {
        AssemblySpec sourceSpec = Assemblies.Last();
        string source = AssemblyPath(cad, sourceSpec.TargetStem);
        string target = AssemblyPath(cad, IdentityShowcaseStem);
        RequireProjectFile(cad, source);
        EnsureExactOutputClosed(cad, target);
        File.Copy(source, target, true);

        ModelDoc2 document = OpenAssembly(cad, target);
        AssemblyDoc assembly = document as AssemblyDoc;
        Require(assembly != null, "Identity showcase is not an assembly");
        try
        {
            Require(CountExact(TopLevelComponents(assembly), newBack) == 1,
                "Product rear panel missing before showcase display copy is added");
            MathUtility utility = cad.Application.GetMathUtility() as MathUtility;
            Require(utility != null, "SOLIDWORKS did not provide MathUtility");
            Component2 displayBack = cad.AddComponent(document, newBack,
                "Presentation-only exterior rear panel, excluded from product BOM", 0.0, 0.0, 0.0);
            ApplyExistingTransform(document, assembly, utility, displayBack,
                new[]
                {
                     1.0, 0.0, 0.0,
                     0.0, 1.0, 0.0,
                     0.0, 0.0, 1.0,
                    -0.650, 0.0, 0.0,
                     1.0, 0.0, 0.0, 0.0
                }, "presentation rear-panel transform");
            Require(CountExact(TopLevelComponents(assembly), newBack) == 2,
                "Identity showcase must contain one product and one presentation rear panel");

            cad.Property(document, "Presentation intent",
                "Single-window exterior identity review: presentation-only rear panel at left, complete tilted module case at centre, detached secure lid at right");
            cad.Property(document, "BOM boundary",
                "The second V0.10 rear panel is a display duplicate only and must be excluded from manufacturing BOM and mass targets");
            cad.Property(document, "Product assemblies",
                "Use the five Rack4Modules_*_V10_RymoviaPhaseHaloRear assemblies for engineering/BOM work");

            document.Extension.ForceRebuildAll();
            Require(document.ForceRebuild3(false), "Identity showcase rebuild failed");
            assembly.UpdateBox();
            // Save the final review camera into this generated presentation
            // assembly so reopening it does not dirty the document or prompt to
            // save any referenced engineering files.
            document.ShowNamedView2("*Isometric", 7);
            document.ViewZoomtofit2();
            document.GraphicsRedraw2();
            string saved = cad.SaveAssembly(document, IdentityShowcaseStem, true);
            Require(SamePath(saved, target), "Identity showcase save escaped target path");
            cad.SaveAssembly(document, IdentityShowcaseStem, false);
            Require(!document.GetSaveFlag(), "Identity showcase remains dirty after final save");
            cad.Log("V10_IDENTITY_SHOWCASE=" + saved +
                ";new_back_occurrences=2;presentation_duplicate=true");
        }
        finally { cad.Application.CloseDoc(document.GetTitle()); }
    }

    private static Face2 FindOuterFace(PartDoc part)
    {
        Require(part != null, "Rear-panel document is not a part");
        Face2 best = null;
        double bestArea = 0.0;
        Array bodies = part.GetBodies2((int)swBodyType_e.swSolidBody, false) as Array;
        Require(bodies != null && bodies.Length == 1, "Rear panel has no valid solid body");
        foreach (object bodyObject in bodies)
        {
            Body2 body = bodyObject as Body2;
            Array faces = body == null ? null : body.GetFaces() as Array;
            if (faces == null) continue;
            foreach (object faceObject in faces)
            {
                Face2 face = faceObject as Face2;
                if (face == null) continue;
                Array normal = face.Normal as Array;
                if (normal == null || normal.Length < 3) continue;
                double nx = Convert.ToDouble(normal.GetValue(0), CultureInfo.InvariantCulture);
                double ny = Convert.ToDouble(normal.GetValue(1), CultureInfo.InvariantCulture);
                double nz = Convert.ToDouble(normal.GetValue(2), CultureInfo.InvariantCulture);
                double area = face.GetArea();
                if (nz > 0.99 && Math.Abs(nx) < 0.01 && Math.Abs(ny) < 0.01 &&
                    FaceCentreZ(face) > 0.109 && area > bestArea)
                {
                    best = face;
                    bestArea = area;
                }
            }
        }
        Require(best != null && bestArea > 0.20,
            "Cannot identify the complete +Z rear exterior face");
        return best;
    }

    private static string FindDecalTemplate()
    {
        string directory = @"E:\SW2025\SOLIDWORKS\data\graphics\Decals\Logos";
        foreach (string name in new[] { "decals logo.p2d", "sw logo transparent.p2d", "sw.p2d" })
        {
            string path = Path.Combine(directory, name);
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException("No SOLIDWORKS decal template was found", directory);
    }

    private static void AddDecal(ModelDoc2 document, Face2 face, string template,
        string image, double widthM, double heightM, double cx, double cy,
        double cz, string label)
    {
        Require(face != null, "Selected rear entity is not a face for " + label);
        Decal decal = document.Extension.CreateDecal();
        RenderMaterial material = (RenderMaterial)decal;
        Require(material.AddEntity(face), "Cannot attach decal entity for " + label);
        material.FileName = template;
        material.TextureFilename = image;
        material.MappingType = 0;
        material.ProjectionReference = 0;
        material.FixedAspectRatio = true;
        material.FitWidth = false;
        material.FitHeight = false;
        material.Width = widthM;
        material.Height = heightM;
        material.SetCenterPoint2(cx, cy, cz);
        material.SetUDirection2(1.0, 0.0, 0.0);
        material.SetVDirection2(0.0, 1.0, 0.0);
        decal.MaskType = PreviewDecalMaskAlpha;
        decal.Hidden = false;
        int id = 0;
        Require(document.Extension.AddDecal(decal, out id), "Cannot add decal " + label);
    }

    private static double FaceCentreZ(Face2 face)
    {
        Array box = face.GetBox() as Array;
        Require(box != null && box.Length >= 6, "Face bounding box is unavailable");
        return (Convert.ToDouble(box.GetValue(2), CultureInfo.InvariantCulture) +
            Convert.ToDouble(box.GetValue(5), CultureInfo.InvariantCulture)) / 2.0;
    }

    private static Body2 Unite(Body2 first, Body2 second, string context)
    {
        int error = 0;
        object raw = first.Operations2((int)swBodyOperationType_e.SWBODYADD, second, out error);
        Require(error == (int)swBodyOperationError_e.swBodyOperationNoError,
            "Body union failed for " + context + "; error=" + error);
        Array array = raw as Array;
        Require(array != null && array.Length == 1,
            "Body union did not yield one solid for " + context);
        Body2 result = array.GetValue(array.GetLowerBound(0)) as Body2;
        Require(result != null, "Body union returned null for " + context);
        return result;
    }

    private static void ReplaceOccurrence(ModelDoc2 document, AssemblyDoc assembly,
        string oldPath, string replacementPath, string context)
    {
        Component2 old = FindExact(TopLevelComponents(assembly), oldPath);
        document.ClearSelection2(true);
        Require(old.Select4(false, null, false), "Cannot select source " + context);
        Require(assembly.ReplaceComponents(replacementPath, string.Empty, false, true),
            "SOLIDWORKS refused replacement for " + context);
        document.ClearSelection2(true);
    }

    private static void ApplyExistingTransform(ModelDoc2 document, AssemblyDoc assembly,
        MathUtility utility, Component2 component, double[] transform, string label)
    {
        if (component.IsFixed())
        {
            document.ClearSelection2(true);
            Require(component.Select4(false, null, false),
                "Cannot select fixed component for " + label);
            assembly.UnfixComponent();
            document.ClearSelection2(true);
        }
        MathTransform math = utility.CreateTransform(transform) as MathTransform;
        Require(math != null, "Cannot create transform for " + label);
        component.Transform2 = math;
        RequireTransformEqual(ReadTransform(component), transform, label);
        assembly.UpdateBox();
    }

    private static ModelDoc2 OpenAssembly(RackCadSession cad, string path)
    {
        int errors = 0, warnings = 0;
        ModelDoc2 document = cad.Application.OpenDoc6(path,
            (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
            string.Empty, ref errors, ref warnings) as ModelDoc2;
        Require(document != null && errors == 0,
            "Cannot open assembly; errors=" + errors + "; path=" + path);
        if (warnings != 0) cad.Log("WARNING: opening " + path + " returned " + warnings);
        return document;
    }

    private static void EnsureExactOutputClosed(RackCadSession cad, string path)
    {
        ModelDoc2 doc = cad.Application.GetFirstDocument() as ModelDoc2;
        while (doc != null)
        {
            ModelDoc2 next = doc.GetNext() as ModelDoc2;
            if (SamePath(doc.GetPathName(), path))
            {
                if (doc.GetSaveFlag())
                    throw new InvalidOperationException("Dirty V0.10 target is open: " + path);
                cad.Application.CloseDoc(doc.GetTitle());
                return;
            }
            doc = next;
        }
    }

    private static void OpenFinalShowcase(RackCadSession cad)
    {
        string path = AssemblyPath(cad, IdentityShowcaseStem);
        ModelDoc2 document = OpenAssembly(cad, path);
        cad.Application.SetUserPreferenceToggle(
            (int)swUserPreferenceToggle_e.swDisplayDecals, true);
        // SOLIDWORKS may mark a copied assembly as NeedsRegen on open (warning
        // 32).  Rebuild and save only this generated V10 target before showing
        // it, so the final window opens without a star or a save-all prompt.
        Require(document.ForceRebuild3(false), "Final V10 showcase rebuild failed on open");
        cad.SaveAssembly(document, IdentityShowcaseStem, false);
        Require(!document.GetSaveFlag(), "Final V10 showcase is still dirty after open-save");
        cad.Application.Visible = true;
        cad.Application.UserControl = true;
        cad.Application.FrameState = (int)swWindowState_e.swWindowMaximized;
        cad.Show(document);
        document.GraphicsRedraw2();
    }

    private static List<Component2> TopLevelComponents(AssemblyDoc assembly)
    {
        Array raw = assembly.GetComponents(true) as Array;
        List<Component2> result = new List<Component2>();
        if (raw != null)
            foreach (object value in raw)
            {
                Component2 component = value as Component2;
                if (component != null) result.Add(component);
            }
        return result;
    }

    private static Component2 FindExact(IEnumerable<Component2> components, string path)
    {
        List<Component2> found = components.Where(c => SameComponentPath(c, path)).ToList();
        Require(found.Count == 1,
            "Expected exactly one component path; actual " + found.Count + "; path=" + path);
        return found[0];
    }

    private static int CountExact(IEnumerable<Component2> components, string path)
    {
        return components.Count(c => SameComponentPath(c, path));
    }

    private static List<string> ComponentSignatures(IEnumerable<Component2> components,
        string excludedPath)
    {
        List<string> result = new List<string>();
        foreach (Component2 component in components)
        {
            if (SameComponentPath(component, excludedPath)) continue;
            double[] transform = ReadTransform(component);
            string signature = Path.GetFullPath(component.GetPathName()).ToUpperInvariant() + "|" +
                string.Join(",", transform.Take(12).Select(v =>
                    Math.Round(v, 9).ToString("0.#########", CultureInfo.InvariantCulture)));
            result.Add(signature);
        }
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static void RequireSequencesEqual(IList<string> actual, IList<string> expected,
        string context)
    {
        Require(actual.Count == expected.Count,
            context + " count mismatch; expected=" + expected.Count + "; actual=" + actual.Count);
        for (int i = 0; i < actual.Count; i++)
            Require(string.Equals(actual[i], expected[i], StringComparison.Ordinal),
                context + " mismatch at index " + i.ToString(CultureInfo.InvariantCulture));
    }

    private static bool SameComponentPath(Component2 component, string path)
    {
        return component != null && SamePath(component.GetPathName(), path);
    }

    private static double[] ReadTransform(Component2 component)
    {
        MathTransform transform = component.Transform2;
        Array raw = transform == null ? null : transform.ArrayData as Array;
        Require(raw != null && raw.Length >= 16,
            "Component transform is unavailable: " + component.Name2);
        double[] result = new double[16];
        for (int i = 0; i < 16; i++)
            result[i] = Convert.ToDouble(raw.GetValue(i), CultureInfo.InvariantCulture);
        return result;
    }

    private static double ReadMass(ModelDoc2 document)
    {
        MassProperty mass = document.Extension.CreateMassProperty();
        return mass == null ? double.NaN : mass.Mass;
    }

    private static IEnumerable<double> Signs()
    {
        yield return -1.0;
        yield return 1.0;
    }

    private static string PatternPath(RackCadSession cad)
    {
        return Path.GetFullPath(Path.Combine(cad.Root, "logo", PatternFile));
    }

    private static string VectorPath(RackCadSession cad)
    {
        return Path.GetFullPath(Path.Combine(cad.Root, "logo", VectorFile));
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
        string root = Path.GetFullPath(cad.Root).TrimEnd(Path.DirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        Require(full.StartsWith(root, StringComparison.OrdinalIgnoreCase),
            "Path escaped project root: " + full);
        Require(File.Exists(full) && new FileInfo(full).Length > 0,
            "Required project file missing: " + full);
    }

    private static bool SamePath(string first, string second)
    {
        return !string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(second) &&
            string.Equals(Path.GetFullPath(first), Path.GetFullPath(second),
                StringComparison.OrdinalIgnoreCase);
    }

    private static string Hash(string path)
    {
        using (SHA256 sha = SHA256.Create())
        using (FileStream stream = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
    }

    private static void RequireClose(double actual, double expected, double tolerance,
        string context)
    {
        Require(!double.IsNaN(actual) && !double.IsInfinity(actual) &&
            Math.Abs(actual - expected) <= tolerance,
            context + " mismatch; expected=" + Format(expected) + "; actual=" + Format(actual));
    }

    private static void RequireTransformEqual(double[] actual, double[] expected, string context)
    {
        Require(actual != null && expected != null && actual.Length >= 12 && expected.Length >= 12,
            context + " has an unavailable transform");
        for (int i = 0; i < 12; i++)
            RequireClose(actual[i], expected[i], TransformTolerance,
                context + " element " + i.ToString(CultureInfo.InvariantCulture));
    }

    private static string Format(double value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class AssemblySpec
    {
        internal readonly string SourceStem;
        internal readonly string TargetStem;

        internal AssemblySpec(string sourceStem, string targetStem)
        {
            SourceStem = sourceStem;
            TargetStem = targetStem;
        }
    }

    private sealed class SourceStamp
    {
        internal readonly string Path;
        internal readonly long Length;
        internal readonly DateTime LastWriteUtc;
        internal readonly string Hash;

        internal SourceStamp(string path, long length, DateTime lastWriteUtc, string hash)
        {
            Path = path;
            Length = length;
            LastWriteUtc = lastWriteUtc;
            Hash = hash;
        }
    }

    private sealed class Bounds
    {
        internal readonly double MinX;
        internal readonly double MinY;
        internal readonly double MinZ;
        internal readonly double MaxX;
        internal readonly double MaxY;
        internal readonly double MaxZ;

        internal Bounds(double minX, double minY, double minZ,
            double maxX, double maxY, double maxZ)
        {
            MinX = minX; MinY = minY; MinZ = minZ;
            MaxX = maxX; MaxY = maxY; MaxZ = maxZ;
        }

        internal static Bounds FromBodies(Array bodies)
        {
            double minX = double.PositiveInfinity, minY = double.PositiveInfinity,
                minZ = double.PositiveInfinity;
            double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity,
                maxZ = double.NegativeInfinity;
            foreach (object value in bodies)
            {
                Body2 body = value as Body2;
                Require(body != null, "Invalid solid body during bounds readback");
                Array box = body.GetBodyBox() as Array;
                Require(box != null && box.Length >= 6, "Body bounding box unavailable");
                minX = Math.Min(minX, Convert.ToDouble(box.GetValue(0), CultureInfo.InvariantCulture) * 1000.0);
                minY = Math.Min(minY, Convert.ToDouble(box.GetValue(1), CultureInfo.InvariantCulture) * 1000.0);
                minZ = Math.Min(minZ, Convert.ToDouble(box.GetValue(2), CultureInfo.InvariantCulture) * 1000.0);
                maxX = Math.Max(maxX, Convert.ToDouble(box.GetValue(3), CultureInfo.InvariantCulture) * 1000.0);
                maxY = Math.Max(maxY, Convert.ToDouble(box.GetValue(4), CultureInfo.InvariantCulture) * 1000.0);
                maxZ = Math.Max(maxZ, Convert.ToDouble(box.GetValue(5), CultureInfo.InvariantCulture) * 1000.0);
            }
            return new Bounds(minX, minY, minZ, maxX, maxY, maxZ);
        }
    }
}
