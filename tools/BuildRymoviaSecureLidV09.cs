using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Rymovia V0.9 upgrades the V0.8 appearance derivative with a positive,
// four-point transport-lid retention system.  The long 82 mm lid returns keep
// carrying lateral/shear loads; four low-profile over-centre latches provide
// positive axial retention.  A drilled V0.9 lid, coaxial through-fastener
// patterns, integral hard-stop tongues and defined EPDM compression close the
// mechanical load path.  All V0.7/V0.8 sources remain immutable.
internal static class BuildRymoviaSecureLidV09
{
    private const string OldSideStem = "SideFrame_V07_StableDoubleShearInner";
    private const string OldCatchStem = "InternalLidCatch_V03";
    private const string OldTravelLidStem = "DeepTravelLid_V08_Rymovia_StandRelief";
    private const string TravelLidStem = "DeepTravelLid_V09_Rymovia_Secure4Point";
    private const string CaseReferenceStem = "BackPanel_V07_5052_1p5mm_VESADoubler";
    private const string DesktopReferenceStem = "DesktopReferenceSurface_V04";
    private const string SecureSideStem = "SideFrame_V09_SecureLidInner";
    private const string BridgePackStem = "LidLatchCaseBridgePack_V09_6061";
    private const string KeeperPackStem = "LidLatchKeeperPack_V09_Stainless";
    private const string DoublerPackStem = "LidLatchDoublerPack_V09_5052";
    private const string BodyPackStem = "LidLatchBodyPack_V09_Black";
    private const string BailPackStem = "LidLatchBailPack_V09_Stainless";
    private const string IndicatorPackStem = "LidLatchLockedIndicatorPack_V09_Red";
    private const string CompressionPadPackStem = "LidCompressionPadPack_V09_EPDM";
    private const string CaseFastenerPackStem = "LidLatchCaseFastenerPack_V09_A4_M3";
    private const string LidFastenerPackStem = "LidLatchLidFastenerPack_V09_A4_M3";

    private const double CaseHeight = 420.0;
    private const double CaseDepth = 110.0;
    private const double ShellThickness = 2.0;
    private const double InnerSideThickness = 4.0;
    private const double InnerSideCoreThickness = 3.0;
    private const double InnerSideCentreX = 273.0;
    private const double HingeCaseY = -129.0;
    private const double HingeCaseZ = 52.0;
    private const double LoadStopLocalY = -21.0;
    private const double LoadStopLocalZ = -38.0;
    private const double PivotToFootPadCentre = 185.0;
    private const double FootPadDeskCentreHeight = 13.0;
    private const double ShellContactY = -210.0;
    private const double ShellContactZ = 110.0;
    private const double LocatorHoleZ = 6.0;
    private const double StructuralHoleZ = 16.0;
    private const double LocatorClearanceDiameter = 3.4;
    private const double StructuralClearanceDiameter = 4.5;
    private const double PivotClearanceDiameter = 10.2;
    private const double LoadStopClearanceDiameter = 10.2;
    private const double SpacerHoleDiameter = 5.5;
    private const double VentLengthY = 22.0;
    private const double VentWidthZ = 4.0;
    private const double LatchCentreY = 196.0;
    private const double LatchBoltOffsetY = 6.0;
    private const double CaseLatchBoltZ = 24.0;
    private const double LatchBoltClearanceDiameter = 3.2;
    private const double LatchBoltDiameter = 3.0;
    private const double LatchBridgeWidthY = 18.0;
    private const double LatchBodyWidthY = 18.0;
    private const double LatchDoublerWidthY = 22.0;
    private const double BailWireDiameter = 2.5;
    private const double KeeperBarDiameter = 4.0;
    private const double KeeperBarZ = 24.0;
    private const double BailContactZ = 27.25;
    private const double HardStopContactZ = 14.0;
    private const double EpdmFreeThickness = 2.8;
    private const double EpdmCompressedThickness = 2.0;
    private const double OverallWidth = 575.6;
    private const double TravelLidThickness = 1.2;
    private const double TravelLidBeadDepth = 1.2;
    private const double TravelLidFrontZ = -70.0;
    private const double TravelLidSkirtDepth = 82.0;
    private const double TravelReliefMinY = -170.0;
    private const double TravelReliefMaxY = 76.0;
    private const double TravelReliefMinZ = -2.0;
    private const double TravelReliefMaxZ = 15.0;
    private const int PreviewDecalMaskAlpha = 3;
    private const double GeometryTolerance = 0.1;
    private const double TransformTolerance = 0.0000001;
    private const double TransportMassBudgetKg = 6.85;

    private static readonly double[] VentCentersY = { 94.0, 124.0, 154.0, 184.0 };
    private static readonly double[] VentCentersZ = { 92.0, 102.0 };
    private static readonly double[] LidLatchRowsZ = { -18.0, -6.0 };
    private static readonly MountPoint[] SpacerMounts =
    {
        new MountPoint(-173.0, 47.0),
        new MountPoint(-170.0, 68.0),
        new MountPoint(-60.0, 30.0),
        new MountPoint(0.0, 30.0),
        new MountPoint(60.0, 30.0)
    };

    private static readonly double[] Graphite = { 0.067, 0.067, 0.067 };
    private static readonly double[] DeepGraphite = { 0.105, 0.115, 0.130 };
    private static readonly double[] NaturalAluminium = { 0.73, 0.75, 0.77 };
    private static readonly double[] Stainless = { 0.60, 0.63, 0.65 };
    private static readonly double[] RubberBlack = { 0.035, 0.040, 0.045 };
    private static readonly double[] RymoviaRed = { 1.0, 0.055, 0.035 };

    private static readonly AssemblySpec[] Assemblies =
    {
        new AssemblySpec("Rack4Modules_OpenCase_V08_Rymovia",
            "Rack4Modules_OpenCase_V09_RymoviaSecureLid", false, false, false),
        new AssemblySpec("Rack4Modules_TransportClosed_V08_Rymovia",
            "Rack4Modules_TransportClosed_V09_RymoviaSecureLid", false, true, false),
        new AssemblySpec("Rack4Modules_ClearanceCheck_V08_Rymovia",
            "Rack4Modules_ClearanceCheck_V09_RymoviaSecureLid", false, false, false),
        new AssemblySpec("Rack4Modules_DesktopTilt60_V08_Rymovia",
            "Rack4Modules_DesktopTilt60_V09_RymoviaSecureLid", true, false, false),
        new AssemblySpec("Rack4Modules_ShowcaseTilt60_LidOff_V08_Rymovia",
            "Rack4Modules_ShowcaseTilt60_LidOff_V09_RymoviaSecureLid", true, true, true)
    };

    [STAThread]
    private static int Main(string[] arguments)
    {
        string progress = "start";
        try
        {
            if (arguments == null || arguments.Length != 1 ||
                string.IsNullOrWhiteSpace(arguments[0]))
                throw new ArgumentException("Usage: BuildRymoviaSecureLidV09.exe <Rack4Modules root>");

            RackCadSession cad = new RackCadSession(Path.GetFullPath(arguments[0]));
            progress = "preflight";
            Dictionary<string, SourceStamp> sourceHashes = CaptureSourceHashes(cad);
            GuardGeneratedOutputs(cad);
            ValidateAnalyticLayout();

            progress = "secure branded lid";
            string secureLid = CreateSecureBrandedLid(cad);
            progress = "secure side frame";
            string secureSide = CreateSecureSideFrame(cad);
            progress = "case bridge pack";
            string bridgePack = CreateCaseBridgePack(cad);
            progress = "keeper pack";
            string keeperPack = CreateKeeperPack(cad);
            progress = "lid doubler pack";
            string doublerPack = CreateDoublerPack(cad);
            progress = "latch body pack";
            string bodyPack = CreateLatchBodyPack(cad);
            progress = "latch bail pack";
            string bailPack = CreateBailPack(cad);
            progress = "lock indicator pack";
            string indicatorPack = CreateIndicatorPack(cad);
            progress = "EPDM compression pad pack";
            string compressionPadPack = CreateCompressionPadPack(cad);
            progress = "case latch fastener pack";
            string caseFastenerPack = CreateCaseFastenerPack(cad);
            progress = "lid latch fastener pack";
            string lidFastenerPack = CreateLidFastenerPack(cad);

            HardwarePaths hardware = new HardwarePaths
            {
                SecureLid = secureLid,
                SecureSide = secureSide,
                BridgePack = bridgePack,
                KeeperPack = keeperPack,
                DoublerPack = doublerPack,
                BodyPack = bodyPack,
                BailPack = bailPack,
                IndicatorPack = indicatorPack,
                CompressionPadPack = compressionPadPack,
                CaseFastenerPack = caseFastenerPack,
                LidFastenerPack = lidFastenerPack
            };

            foreach (AssemblySpec spec in Assemblies)
            {
                progress = "assembly " + spec.TargetStem;
                BuildAssembly(cad, spec, hardware);
            }

            progress = "source verification";
            VerifySourceHashes(sourceHashes);
            progress = "final display";
            OpenFinalShowcase(cad);

            cad.Log("V09_SECURE_LID_BUILD_COMPLETE=true");
            cad.Log("V09_LATCH_COUNT=4");
            cad.Log("V09_LATCH_CENTRES_CASE_MM=x+/-side,y+/-196,z_keeper_24");
            cad.Log("V09_LID_RETENTION=82mm_perimeter_guidance+four_positive_over_center_latches+0.8mm_EPDM_preload");
            cad.Log("V09_SOURCE_HASHES_UNCHANGED=true");
            return 0;
        }
        catch (Exception exception)
        {
            string message;
            try { message = exception.GetType().FullName + ": " + exception.Message; }
            catch { message = "unprintable exception"; }
            Console.Error.WriteLine("V09_SECURE_LID_BUILD_FAILED=" + message + " @ " + progress);
            return 1;
        }
    }

    private static Dictionary<string, SourceStamp> CaptureSourceHashes(RackCadSession cad)
    {
        List<string> sources = new List<string>();
        sources.Add(PartPath(cad, OldSideStem));
        sources.Add(PartPath(cad, OldTravelLidStem));
        foreach (AssemblySpec spec in Assemblies) sources.Add(AssemblyPath(cad, spec.SourceStem));
        Dictionary<string, SourceStamp> result = new Dictionary<string, SourceStamp>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in sources)
        {
            RequireProjectFile(cad, path);
            FileInfo info = new FileInfo(path);
            string digest = null;
            try { digest = Hash(path); }
            catch (IOException) { cad.Log("V09_LOCKED_SOURCE_METADATA_GUARD=" + path); }
            catch (UnauthorizedAccessException) { cad.Log("V09_PROTECTED_SOURCE_METADATA_GUARD=" + path); }
            result[path] = new SourceStamp(path, info.Length, info.LastWriteTimeUtc, digest);
        }
        return result;
    }

    private static void VerifySourceHashes(Dictionary<string, SourceStamp> before)
    {
        foreach (SourceStamp item in before.Values)
        {
            Require(File.Exists(item.Path), "Protected source disappeared: " + item.Path);
            FileInfo info = new FileInfo(item.Path);
            Require(info.Length == item.Length && info.LastWriteTimeUtc == item.LastWriteUtc,
                "Protected V0.7/V0.8 source metadata changed: " + item.Path);
            if (item.Hash != null)
            {
                try
                {
                    Require(string.Equals(Hash(item.Path), item.Hash, StringComparison.Ordinal),
                        "Protected V0.7/V0.8 source content changed: " + item.Path);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    private static void GuardGeneratedOutputs(RackCadSession cad)
    {
        HashSet<string> outputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string stem in new[]
        {
            TravelLidStem, SecureSideStem, BridgePackStem, KeeperPackStem,
            DoublerPackStem, BodyPackStem, BailPackStem, IndicatorPackStem,
            CompressionPadPackStem, CaseFastenerPackStem, LidFastenerPackStem
        }) outputs.Add(PartPath(cad, stem));
        foreach (AssemblySpec spec in Assemblies) outputs.Add(AssemblyPath(cad, spec.TargetStem));

        List<string> closeTitles = new List<string>();
        ModelDoc2 doc = cad.Application.GetFirstDocument() as ModelDoc2;
        while (doc != null)
        {
            ModelDoc2 next = doc.GetNext() as ModelDoc2;
            string path = doc.GetPathName();
            if (!string.IsNullOrWhiteSpace(path) && outputs.Contains(Path.GetFullPath(path)))
            {
                if (doc.GetSaveFlag())
                    throw new InvalidOperationException("Refusing to replace dirty generated V0.9 target: " + path);
                closeTitles.Add(doc.GetTitle());
            }
            doc = next;
        }
        foreach (string title in closeTitles) cad.Application.CloseDoc(title);
    }

    private static void ValidateAnalyticLayout()
    {
        double lowerBridgeMaxY = -LatchCentreY + LatchBridgeWidthY / 2.0;
        double lowerBridgeGap = -184.0 - lowerBridgeMaxY;
        Require(lowerBridgeGap >= 3.0 - GeometryTolerance,
            "Lower latch bridge does not clear the V0.7 kickstand outer cheek");

        double sideEdgeLigament = CaseHeight / 2.0 -
            (LatchCentreY + LatchBoltOffsetY + LatchBoltClearanceDiameter / 2.0);
        Require(sideEdgeLigament >= 6.0,
            "M3 case-latch hole edge ligament is below 6.0 mm");

        double lidReturnInnerEdge = (CaseHeight + 1.0) / 2.0;
        double bailOuterY = LatchCentreY + 11.5 + BailWireDiameter / 2.0;
        Require(lidReturnInnerEdge - bailOuterY >= 1.5,
            "Latch bail does not clear the upper/lower lid returns");

        Require(90.0 - 32.0 >= 58.0,
            "Latch bridge is not separated from the side vent bank");
        RequireClose(HardStopContactZ - 12.0, EpdmCompressedThickness,
            0.000001, "closed EPDM thickness");
        RequireClose(EpdmFreeThickness - EpdmCompressedThickness, 0.8,
            0.000001, "nominal EPDM compression");
        RequireClose(BailContactZ - KeeperBarZ,
            KeeperBarDiameter / 2.0 + BailWireDiameter / 2.0,
            0.000001, "closed-state bail/keeper tangency");

        double bodyArmGap = (11.5 - BailWireDiameter / 2.0) -
            (LatchBodyWidthY / 2.0);
        Require(bodyArmGap >= 1.0,
            "Bail arms overlap the latch body envelope");

        double lidOuterX = (OverallWidth + 1.0) / 2.0 + TravelLidThickness;
        double keeperOuterX = 293.5 + KeeperBarDiameter / 2.0;
        Require(keeperOuterX - lidOuterX <= 6.0 + GeometryTolerance,
            "Latch hardware projects more than 6 mm beyond the lid side");
    }

    private static string CreateSecureBrandedLid(RackCadSession cad)
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
            foreach (double ribY in new[] { -120.0, 120.0 })
            {
                face = Unite(face,
                    cad.Box(0.0, ribY,
                        TravelLidFrontZ - TravelLidThickness - TravelLidBeadDepth,
                        externalWidth - 40.0, 8.0, TravelLidBeadDepth),
                    "V0.9 anti-drum bead");
            }
            cad.AddBody(document, face,
                "Rymovia deep-lid face with two shallow anti-drum beads");

            foreach (double signX in Signs())
            {
                Body2 sideReturn = cad.Box(signX * sideReturnCentreX, 0.0,
                    TravelLidFrontZ, TravelLidThickness, cavityHeight,
                    TravelLidSkirtDepth);
                sideReturn = cad.Cut(sideReturn,
                    cad.Box(signX * sideReturnCentreX, reliefCentreY,
                        TravelReliefMinZ, TravelLidThickness + 1.0,
                        TravelReliefMaxY - TravelReliefMinY,
                        TravelReliefMaxZ - TravelReliefMinZ),
                    (signX < 0.0 ? "left" : "right") +
                    " folded-kickstand side-return relief");

                foreach (double signY in Signs())
                    foreach (double offset in new[] { -LatchBoltOffsetY, LatchBoltOffsetY })
                        foreach (double rowZ in LidLatchRowsZ)
                            sideReturn = cad.Cut(sideReturn,
                                cad.Cylinder(signX * 288.0,
                                    signY * LatchCentreY + offset,
                                    rowZ, signX, 0.0, 0.0,
                                    LatchBoltClearanceDiameter, 1.8),
                                "coaxial M3 V7-pattern lid-latch through-hole");

                cad.AddBody(document, sideReturn,
                    signX < 0.0
                        ? "Left drilled lid return with stand relief"
                        : "Right drilled lid return with stand relief");

                cad.AddBody(document,
                    cad.Box(0.0,
                        signX * (cavityHeight / 2.0 + TravelLidThickness / 2.0),
                        TravelLidFrontZ, externalWidth, TravelLidThickness,
                        TravelLidSkirtDepth),
                    signX < 0.0 ? "Lower lid return" : "Upper lid return");
            }

            cad.ApplyMaterial(document, "5052-H32", Graphite);
            cad.Property(document, "Brand", "Rymovia Audio Systems");
            cad.Property(document, "Construction",
                "1.2 mm 5052-H32 deep folded lid; four latch sites each have four real diameter-3.2 mm M3 clearance holes");
            cad.Property(document, "Lid fastener chain",
                "M3 A4-70 flat-head fastener -> latch body -> 2 mm 5052 doubler -> drilled 1.2 mm return; 12 x 12 mm V7-small pattern");
            cad.Property(document, "Compression control",
                "Four integral doubler tongues provide metal hard stops at z14; adjacent 70A EPDM pads compress from 2.8 to 2.0 mm");
            cad.Property(document, "Fit envelope",
                "576.6 mm inside width leaves 0.5 mm nominal clearance per side over the 575.6 mm case");
            cad.Property(document, "Appearance",
                "Fine-matte graphite; single outer mark and one inner lockup; production vector artwork remains required");

            cad.Application.SetUserPreferenceToggle(
                (int)swUserPreferenceToggle_e.swDisplayDecals, true);
            document.Extension.DeleteAllDecals();
            Face2 outerFace;
            Face2 innerFace;
            FindLidMainFaces(document as PartDoc, out outerFace, out innerFace);
            string decalTemplate = FindDecalTemplate();
            string mark = Path.Combine(cad.Root, "logo", "logo-mark-white.png");
            string lockup = Path.Combine(cad.Root, "logo", "logo-lockup-white.png");
            string pattern = Path.Combine(cad.Root, "logo", "rymovia-timegrid-v09.png");
            RequireProjectFile(cad, mark);
            RequireProjectFile(cad, lockup);
            RequireProjectFile(cad, pattern);
            AddDecal(document, outerFace, decalTemplate, pattern,
                0.579, 0.423, 0.0, 0.0, FaceCentreZ(outerFace),
                "Rymovia three-row time-grid exterior pattern");
            AddDecal(document, outerFace, decalTemplate, mark,
                0.075, 0.088, -0.205, 0.135, FaceCentreZ(outerFace),
                "Rymovia outer mark");
            AddDecal(document, innerFace, decalTemplate, lockup,
                0.200, 0.0505, 0.0, -0.155, FaceCentreZ(innerFace),
                "Rymovia inner lockup");
            cad.Property(document, "Exterior pattern",
                "Rymovia Time Grid: 22 broken laser-etch paths in three bands echo the three 3U module rows; SVG production master included");
            Require(document.Extension.GetDecalsCount() == 3,
                "V0.9 branded lid must contain pattern, outer mark and inner lockup decals");

            ValidatePart(document, 5,
                new Bounds(-289.5, -211.7, -72.4, 289.5, 211.7, 12.0),
                TravelLidStem);
            return cad.SavePart(document, TravelLidStem, true);
        }
        finally { cad.Application.CloseDoc(document.GetTitle()); }
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
        Require(face != null, "Selected lid entity is not a face for " + label);
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
        Require(document.Extension.AddDecal(decal, out id),
            "Cannot add decal " + label);
    }

    private static void FindLidMainFaces(PartDoc part, out Face2 outer, out Face2 inner)
    {
        Require(part != null, "V0.9 lid is not a part");
        outer = null;
        inner = null;
        double outerZ = double.PositiveInfinity;
        double innerZ = double.NegativeInfinity;
        Array bodies = part.GetBodies2((int)swBodyType_e.swSolidBody, false) as Array;
        Require(bodies != null, "V0.9 lid has no solid bodies");
        foreach (object bodyObject in bodies)
        {
            Body2 body = bodyObject as Body2;
            if (body == null) continue;
            Array faces = body.GetFaces() as Array;
            if (faces == null) continue;
            foreach (object faceObject in faces)
            {
                Face2 face = faceObject as Face2;
                if (face == null || face.GetArea() < 0.05) continue;
                Array normal = face.Normal as Array;
                if (normal == null || normal.Length < 3) continue;
                double nx = Convert.ToDouble(normal.GetValue(0), CultureInfo.InvariantCulture);
                double ny = Convert.ToDouble(normal.GetValue(1), CultureInfo.InvariantCulture);
                double nz = Convert.ToDouble(normal.GetValue(2), CultureInfo.InvariantCulture);
                if (Math.Abs(nz) < 0.99 || Math.Abs(nx) > 0.01 || Math.Abs(ny) > 0.01) continue;
                double z = FaceCentreZ(face);
                if (z < outerZ) { outerZ = z; outer = face; }
                if (z > innerZ) { innerZ = z; inner = face; }
            }
        }
        Require(outer != null && inner != null && !object.ReferenceEquals(outer, inner),
            "Cannot identify the two large parallel lid faces");
    }

    private static double FaceCentreZ(Face2 face)
    {
        Array box = face.GetBox() as Array;
        Require(box != null && box.Length >= 6, "A lid face has no valid bounding box");
        return (Convert.ToDouble(box.GetValue(2), CultureInfo.InvariantCulture) +
            Convert.ToDouble(box.GetValue(5), CultureInfo.InvariantCulture)) / 2.0;
    }

    private static string CreateSecureSideFrame(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(SecureSideStem);
        try
        {
            Point loadStop = CalculateLoadStopCasePoint();
            Body2 side = cad.Box(0.0, 0.0, 0.0,
                InnerSideCoreThickness, CaseHeight, CaseDepth - ShellThickness);
            side = Unite(side, cad.Box(0.0, 0.0, 0.0,
                InnerSideThickness, CaseHeight, 24.0), "front structural band");
            side = Unite(side, cad.Box(0.0, 0.0, 96.0,
                InnerSideThickness, CaseHeight, 12.0), "rear enclosure shear band");
            side = Unite(side, cad.Box(0.0, -122.0, 18.0,
                InnerSideThickness, 124.0, 72.0), "folding-leg load block");
            foreach (double edgeY in new[] { -204.0, 204.0 })
                side = Unite(side, cad.Box(0.0, edgeY, 24.0,
                    InnerSideThickness, 12.0, 72.0), "formed-edge-equivalent side band");
            foreach (MountPoint mount in SpacerMounts)
                side = Unite(side, cad.Cylinder(-InnerSideThickness / 2.0,
                    mount.Y, mount.Z, 1.0, 0.0, 0.0, 26.0, InnerSideThickness),
                    "outer-cheek spacer bearing island");

            foreach (double railY in RailPositions(cad))
            {
                side = SideHole(cad, side, railY, LocatorHoleZ,
                    LocatorClearanceDiameter, "M3 rail locator");
                side = SideHole(cad, side, railY, StructuralHoleZ,
                    StructuralClearanceDiameter, "M4 structural rail fixing");
            }
            side = SideHole(cad, side, HingeCaseY, HingeCaseZ,
                PivotClearanceDiameter, "double-shear pivot clearance");
            side = SideHole(cad, side, loadStop.Y, loadStop.Z,
                LoadStopClearanceDiameter, "fixed hard-stop clearance");
            foreach (MountPoint mount in SpacerMounts)
                side = SideHole(cad, side, mount.Y, mount.Z, SpacerHoleDiameter,
                    "outer-cheek spacer fixing");

            foreach (double latchY in new[] { -LatchCentreY, LatchCentreY })
                foreach (double boltOffset in new[] { -LatchBoltOffsetY, LatchBoltOffsetY })
                    side = SideHole(cad, side, latchY + boltOffset, CaseLatchBoltZ,
                        LatchBoltClearanceDiameter, "V0.9 M3 secure-latch keeper fixing");

            foreach (double z in VentCentersZ)
                foreach (double y in VentCentersY)
                {
                    double coreLength = VentLengthY - VentWidthZ;
                    side = cad.Cut(side, cad.Box(0.0, y, z - VentWidthZ / 2.0,
                        InnerSideThickness + 0.8, coreLength, VentWidthZ), "side vent core");
                    foreach (double sign in Signs())
                        side = SideHole(cad, side, y + sign * coreLength / 2.0,
                            z, VentWidthZ, "side vent radius end");
                }

            cad.AddBody(document, side,
                "V0.9 secure-lid inner side frame; old non-engaging catch holes removed");
            cad.ApplyMaterial(document, "6061-T6 (SS)", NaturalAluminium);
            cad.Property(document, "Secure lid interface",
                "Two Southco V7-small envelope keepers per side at y +/-196; paired M3 holes y offset +/-6,z24");
            cad.Property(document, "Removed placeholder",
                "V0.7 diameter-12.2 openings at y +/-150,z55 omitted because they did not engage the lid return");
            cad.Property(document, "Module envelope",
                "542 mm internal clear width retained; fastener heads must be flush inward of x +/-271");
            cad.Property(document, "Manufacturing",
                "4 mm 6061 front load band; M3 A4-70 through fasteners with locking nuts; no thread in thin sheet");
            ValidatePart(document, 1, new Bounds(-2.0, -210.0, 0.0, 2.0, 210.0, 108.0), SecureSideStem);
            return cad.SavePart(document, SecureSideStem, true);
        }
        finally { cad.Application.CloseDoc(document.GetTitle()); }
    }

    private static string CreateCaseBridgePack(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(BridgePackStem);
        try
        {
            int bodies = 0;
            foreach (double signX in Signs())
                foreach (double signY in Signs())
                {
                    double y = signY * LatchCentreY;
                    Body2 bridge = cad.Box(signX * 285.8, y, HardStopContactZ,
                        4.0, LatchBridgeWidthY, 18.0);
                    foreach (double offset in new[] { -LatchBoltOffsetY, LatchBoltOffsetY })
                    {
                        bridge = Unite(bridge,
                            cad.Box(signX * 279.4, y + offset, 18.0,
                                8.8, 5.0, 12.0),
                            "latch bridge compression rib");
                        bridge = cad.Cut(bridge,
                            cad.Cylinder(signX * 274.7, y + offset,
                                CaseLatchBoltZ, signX, 0.0, 0.0,
                                LatchBoltClearanceDiameter, 15.5),
                            "M3 bridge/keeper through-hole");
                    }
                    bridge = Unite(bridge,
                        cad.Box(signX * 288.4, y, HardStopContactZ,
                            1.2, LatchBridgeWidthY, 4.0),
                        "four-point axial-stop shoulder");
                    cad.AddBody(document, bridge,
                        (signX < 0.0 ? "Left" : "Right") + " " +
                        (signY < 0.0 ? "lower" : "upper") + " 6061 latch bridge");
                    bodies++;
                }
            cad.ApplyMaterial(document, "6061-T6 (SS)", DeepGraphite);
            cad.Property(document, "Load path",
                "Each keeper shares two M3 A4-70 through fasteners with the 4 mm side band, 8.8 mm compression ribs and 4 mm bridge plate");
            cad.Property(document, "Clearance",
                "Centres y +/-196; lower bridge ends y=-187, leaving 3 mm to the kickstand outer cheek at y=-184; vents start at z90");
            cad.Property(document, "Axial stop",
                "Four integral 6061 shoulders begin at z14 and contact the four folded 5052 lid stop tongues");
            cad.Property(document, "Boundary",
                "Target hardware envelope is Southco V7 small; import final supplier CAD and verify torque, proof load, cycles and drop before release");
            ValidatePart(document, bodies,
                new Bounds(-289.0, -205.0, 14.0, 289.0, 205.0, 32.0), BridgePackStem);
            return cad.SavePart(document, BridgePackStem, true);
        }
        finally { cad.Application.CloseDoc(document.GetTitle()); }
    }

    private static string CreateKeeperPack(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(KeeperPackStem);
        try
        {
            int bodies = 0;
            foreach (double signX in Signs())
                foreach (double signY in Signs())
                {
                    double y = signY * LatchCentreY;
                    Body2 keeper = cad.Box(signX * 288.8, y, 18.0,
                        2.0, LatchBridgeWidthY, 14.0);
                    foreach (double offset in new[] { -LatchBoltOffsetY, LatchBoltOffsetY })
                    {
                        keeper = cad.Cut(keeper,
                            cad.Cylinder(signX * 287.5, y + offset,
                                CaseLatchBoltZ, signX, 0.0, 0.0,
                                LatchBoltClearanceDiameter, 2.6),
                            "M3 keeper through-hole");
                        keeper = Unite(keeper,
                            cad.Box(signX * 291.65, y + offset, 22.0,
                                3.7, 3.0, 4.0),
                            "keeper bar support arm");
                    }
                    keeper = Unite(keeper,
                        cad.Cylinder(signX * 293.5, y - 8.0, KeeperBarZ,
                            0.0, 1.0, 0.0, KeeperBarDiameter, 16.0),
                        "positive keeper crossbar");
                    cad.AddBody(document, keeper,
                        (signX < 0.0 ? "Left" : "Right") + " " +
                        (signY < 0.0 ? "lower" : "upper") + " stainless keeper");
                    bodies++;
                }
            cad.ApplyMaterial(document, "AISI 304", Stainless);
            cad.Property(document, "Retention",
                "Four stainless keeper crossbars provide positive over-centre hook capture; no friction-only or magnetic retention");
            cad.Property(document, "Wear interface",
                "Replaceable keeper; two diameter-3.2 mm holes align with the bridge and side-frame holes for shared M3 through bolts");
            ValidatePart(document, bodies,
                new Bounds(-295.5, -205.0, 18.0, 295.5, 205.0, 32.0), KeeperPackStem);
            return cad.SavePart(document, KeeperPackStem, true);
        }
        finally { cad.Application.CloseDoc(document.GetTitle()); }
    }

    private static string CreateDoublerPack(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(DoublerPackStem);
        try
        {
            int bodies = 0;
            foreach (double signX in Signs())
                foreach (double signY in Signs())
                {
                    double y = signY * LatchCentreY;
                    Body2 plate = cad.Box(signX * 290.5, y, -54.0,
                        2.0, LatchDoublerWidthY, 66.0);
                    foreach (double offset in new[] { -LatchBoltOffsetY, LatchBoltOffsetY })
                        foreach (double rowZ in LidLatchRowsZ)
                            plate = cad.Cut(plate,
                                cad.Cylinder(signX * 289.2, y + offset, rowZ,
                                    signX, 0.0, 0.0,
                                    LatchBoltClearanceDiameter, 2.6),
                                "flush M3 V7-pattern lid-latch mounting hole");
                    plate = Unite(plate,
                        cad.Box(signX * 289.4, y, 12.0,
                            4.2, 4.0, HardStopContactZ - 12.0),
                        "folded metal axial hard-stop tongue");
                    cad.AddBody(document, plate,
                        (signX < 0.0 ? "Left" : "Right") + " " +
                        (signY < 0.0 ? "lower" : "upper") + " external lid doubler");
                    bodies++;
                }
            cad.ApplyMaterial(document, "5052-H32", Graphite);
            cad.Property(document, "Lid reinforcement",
                "Four external 2 mm 5052 doublers spread each V7-small latch over a 22 x 66 mm area; local lid stack is 3.2 mm");
            cad.Property(document, "Fastener finish",
                "Four M3 flat-head fasteners per latch; heads remain flush inside the 0.5 mm side-guidance clearance");
            cad.Property(document, "Hard stop",
                "Each doubler includes a 4.2 x 4 mm folded tongue ending at z14; metal-to-metal contact limits EPDM compression");
            ValidatePart(document, bodies,
                new Bounds(-291.5, -207.0, -54.0, 291.5, 207.0, 14.0), DoublerPackStem);
            return cad.SavePart(document, DoublerPackStem, true);
        }
        finally { cad.Application.CloseDoc(document.GetTitle()); }
    }

    private static string CreateLatchBodyPack(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(BodyPackStem);
        try
        {
            int bodies = 0;
            foreach (double signX in Signs())
                foreach (double signY in Signs())
                {
                    double y = signY * LatchCentreY;
                    Body2 body = cad.Box(signX * 293.0, y, -50.0,
                        3.0, LatchBodyWidthY, 58.0);
                    foreach (double offset in new[] { -LatchBoltOffsetY, LatchBoltOffsetY })
                        foreach (double rowZ in LidLatchRowsZ)
                            body = cad.Cut(body,
                                cad.Cylinder(signX * 291.2, y + offset, rowZ,
                                    signX, 0.0, 0.0,
                                    LatchBoltClearanceDiameter, 3.6),
                                "M3 V7-pattern latch-body mounting hole");
                    body = cad.Cut(body,
                        cad.Cylinder(signX * 293.0, y - 10.0, 4.0,
                            0.0, 1.0, 0.0, 2.8, 20.0),
                        "2.8 mm latch pivot channel");
                    cad.AddBody(document, body,
                        (signX < 0.0 ? "Left" : "Right") + " " +
                        (signY < 0.0 ? "lower" : "upper") + " low-profile latch body");
                    bodies++;
                }
            cad.ApplyMaterial(document, "AISI 304", Graphite);
            cad.Property(document, "Hardware class",
                "Southco V7-small black-powder-coated non-locking draw-latch functional envelope; target V7-10-105-50 or approved equivalent");
            cad.Property(document, "Reference working load",
                "Southco V7 family published maximum working load 1200 N; final exact part and mounting substrate still require supplier approval");
            cad.Property(document, "Projection",
                "Keeper bar controls maximum nominal side projection: 6.0 mm beyond the 579.0 mm lid outside width");
            cad.Property(document, "Supplier boundary",
                "This is a simplified closed-state envelope, not redistributed vendor CAD; import the selected supplier STEP before production machining");
            ValidatePart(document, bodies,
                new Bounds(-294.5, -205.0, -50.0, 294.5, 205.0, 8.0), BodyPackStem);
            return cad.SavePart(document, BodyPackStem, true);
        }
        finally { cad.Application.CloseDoc(document.GetTitle()); }
    }

    private static string CreateBailPack(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(BailPackStem);
        try
        {
            int bodies = 0;
            foreach (double signX in Signs())
                foreach (double signY in Signs())
                {
                    double y = signY * LatchCentreY;
                    Body2 bail = cad.Cylinder(signX * 293.0, y - 11.5, 4.0,
                        0.0, 1.0, 0.0, BailWireDiameter, 23.0);
                    foreach (double armOffset in new[] { -11.5, 11.5 })
                    {
                        bail = Unite(bail,
                            cad.Cylinder(signX * 293.0, y + armOffset, 4.0,
                                signX, 0.0, 0.0, BailWireDiameter, 0.5),
                            "latch bail pivot-to-arm offset");
                        bail = Unite(bail,
                            cad.Cylinder(signX * 293.5, y + armOffset, 4.0,
                                0.0, 0.0, 1.0, BailWireDiameter,
                                BailContactZ - 4.0),
                            "latch bail side arm");
                    }
                    bail = Unite(bail,
                        cad.Cylinder(signX * 293.5, y - 11.5, BailContactZ,
                            0.0, 1.0, 0.0, BailWireDiameter, 23.0),
                        "closed-state latch bail contact bar");
                    cad.AddBody(document, bail,
                        (signX < 0.0 ? "Left" : "Right") + " " +
                        (signY < 0.0 ? "lower" : "upper") + " stainless draw bail");
                    bodies++;
                }
            cad.ApplyMaterial(document, "AISI 304", Stainless);
            cad.Property(document, "Engagement",
                "Closed-state 2.5 mm bail is exactly tangent behind the diameter-4 mm keeper bar and pulls the lid along +Z");
            cad.Property(document, "Pivot clearance",
                "Diameter-2.5 mm pivot pin runs in a diameter-2.8 mm body channel; radial CAD clearance is 0.15 mm");
            cad.Property(document, "Kinematics",
                "Closed-state functional representation only; replace with exact V7-small supplier CAD before opening-sweep release");
            ValidatePart(document, bodies,
                new Bounds(-294.75, -208.75, 2.75, 294.75, 208.75, 28.5), BailPackStem);
            return cad.SavePart(document, BailPackStem, true);
        }
        finally { cad.Application.CloseDoc(document.GetTitle()); }
    }

    private static string CreateIndicatorPack(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(IndicatorPackStem);
        try
        {
            int bodies = 0;
            foreach (double signX in Signs())
                foreach (double signY in Signs())
                {
                    cad.AddBody(document,
                        cad.Box(signX * 294.65, signY * LatchCentreY, -47.0,
                            0.3, 6.0, 4.0),
                        "Rymovia red locked-state witness mark");
                    bodies++;
                }
            cad.ApplyMaterial(document, "AISI 304", RymoviaRed);
            cad.Property(document, "Brand rule",
                "#FF3B30 is used only as a genuine locked-state indicator, not decoration");
            ValidatePart(document, bodies,
                new Bounds(-294.8, -199.0, -47.0, 294.8, 199.0, -43.0), IndicatorPackStem);
            return cad.SavePart(document, IndicatorPackStem, true);
        }
        finally { cad.Application.CloseDoc(document.GetTitle()); }
    }

    private static string CreateCompressionPadPack(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(CompressionPadPackStem);
        try
        {
            int bodies = 0;
            foreach (double signX in Signs())
                foreach (double signY in Signs())
                    foreach (double padOffset in new[] { -5.25, 5.25 })
                    {
                        cad.AddBody(document,
                            cad.Box(signX * 289.4,
                                signY * LatchCentreY + padOffset, 12.0,
                                4.2, 5.5, EpdmCompressedThickness),
                            "Closed-state 70A EPDM axial preload pad");
                        bodies++;
                    }
            cad.ApplyMaterial(document, "NEOPRENE", RubberBlack);
            cad.Property(document, "Specified production material",
                "70A closed-cell EPDM; SOLIDWORKS NEOPRENE is used only as a mass-property proxy");
            cad.Property(document, "Compression",
                "Free 2.8 mm, closed-state 2.0 mm, nominal compression 0.8 mm (28.6 percent)");
            cad.Property(document, "Creep control",
                "Adjacent integral 5052 hard-stop tongues contact the 6061 shoulders at z14 so the pads cannot be over-compressed");
            ValidatePart(document, bodies,
                new Bounds(-291.5, -204.0, 12.0, 291.5, 204.0, 14.0),
                CompressionPadPackStem);
            return cad.SavePart(document, CompressionPadPackStem, true);
        }
        finally { cad.Application.CloseDoc(document.GetTitle()); }
    }

    private static string CreateCaseFastenerPack(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(CaseFastenerPackStem);
        try
        {
            int bodies = 0;
            foreach (double signX in Signs())
                foreach (double signY in Signs())
                    foreach (double offset in new[] { -LatchBoltOffsetY, LatchBoltOffsetY })
                    {
                        double y = signY * LatchCentreY + offset;
                        Body2 bolt = cad.Cylinder(signX * 270.8, y,
                            CaseLatchBoltZ, signX, 0.0, 0.0,
                            LatchBoltDiameter, 19.0);
                        bolt = Unite(bolt,
                            cad.Cylinder(signX * 289.8, y, CaseLatchBoltZ,
                                signX, 0.0, 0.0, 5.5, 1.2),
                            "M3 case-latch outer locknut/head");
                        cad.AddBody(document, bolt,
                            "M3 A4-70 shared keeper/bridge/side-frame through fastener");
                        bodies++;
                    }
            cad.ApplyMaterial(document, "AISI 304", Stainless);
            cad.Property(document, "Fastener specification",
                "Eight M3 A4-70 through fasteners; flush inward head and prevailing-torque outer locknut; final grip length by DFM stack-up");
            ValidatePart(document, bodies,
                new Bounds(-291.0, -204.75, 21.25, 291.0, 204.75, 26.75),
                CaseFastenerPackStem);
            return cad.SavePart(document, CaseFastenerPackStem, true);
        }
        finally { cad.Application.CloseDoc(document.GetTitle()); }
    }

    private static string CreateLidFastenerPack(RackCadSession cad)
    {
        ModelDoc2 document = cad.NewPart(LidFastenerPackStem);
        try
        {
            int bodies = 0;
            foreach (double signX in Signs())
                foreach (double signY in Signs())
                    foreach (double offset in new[] { -LatchBoltOffsetY, LatchBoltOffsetY })
                        foreach (double rowZ in LidLatchRowsZ)
                        {
                            double y = signY * LatchCentreY + offset;
                            Body2 bolt = cad.Cylinder(signX * 288.2, y, rowZ,
                                signX, 0.0, 0.0, LatchBoltDiameter, 6.3);
                            bolt = Unite(bolt,
                                cad.Cylinder(signX * 294.5, y, rowZ,
                                    signX, 0.0, 0.0, 5.5, 1.2),
                                "M3 lid-latch external low-profile head");
                            cad.AddBody(document, bolt,
                                "M3 A4-70 V7-pattern latch/doubler/lid through fastener");
                            bodies++;
                        }
            cad.ApplyMaterial(document, "AISI 304", Stainless);
            cad.Property(document, "Fastener specification",
                "Sixteen M3 A4-70 flat-head fasteners; flush countersunk head is inside the lid guide surface");
            ValidatePart(document, bodies,
                new Bounds(-295.7, -204.75, -20.75, 295.7, 204.75, -3.25),
                LidFastenerPackStem);
            return cad.SavePart(document, LidFastenerPackStem, true);
        }
        finally { cad.Application.CloseDoc(document.GetTitle()); }
    }

    private static void BuildAssembly(RackCadSession cad, AssemblySpec spec, HardwarePaths hardware)
    {
        string source = AssemblyPath(cad, spec.SourceStem);
        string target = AssemblyPath(cad, spec.TargetStem);
        RequireProjectFile(cad, source);
        EnsureExactOutputClosed(cad, target);
        File.Copy(source, target, true);

        ModelDoc2 document = OpenAssembly(cad, target);
        AssemblyDoc assembly = document as AssemblyDoc;
        Require(assembly != null, "Target is not an assembly: " + target);
        try
        {
            double[] caseTransform = ReadTransform(
                FindStem(TopLevelComponents(assembly), CaseReferenceStem));
            double[] lidTransform = null;
            if (spec.HasLid)
                lidTransform = ReadTransform(
                    FindStem(TopLevelComponents(assembly), OldTravelLidStem));

            ReplaceOccurrences(document, assembly, PartPath(cad, OldSideStem),
                hardware.SecureSide, 2, "secure inner side frame");
            RemoveExactOccurrences(document, assembly, PartPath(cad, OldCatchStem), 4,
                "obsolete non-engaging internal catch");
            if (spec.HasLid)
            {
                ReplaceOccurrences(document, assembly, PartPath(cad, OldTravelLidStem),
                    hardware.SecureLid, 1, "drilled secure branded travel lid");
                RequireTransformEqual(ReadTransform(
                    FindStem(TopLevelComponents(assembly), TravelLidStem)),
                    lidTransform, "replacement lid transform");
            }
            if (spec.CaseTilt60)
                RemoveExactOccurrences(document, assembly,
                    PartPath(cad, DesktopReferenceStem), 1,
                    "display-only desktop reference surface");

            MathUtility utility = cad.Application.GetMathUtility() as MathUtility;
            Require(utility != null, "SOLIDWORKS did not provide MathUtility");
            AddTransformed(cad, document, assembly, utility, hardware.BridgePack,
                "V09 four-point case-side latch bridge pack", caseTransform);
            AddTransformed(cad, document, assembly, utility, hardware.KeeperPack,
                "V09 four-point stainless keeper pack", caseTransform);
            AddTransformed(cad, document, assembly, utility, hardware.CaseFastenerPack,
                "V09 shared M3 keeper/bridge/side-frame fasteners", caseTransform);

            if (spec.HasLid)
            {
                AddTransformed(cad, document, assembly, utility, hardware.DoublerPack,
                    "V09 lid-side external doubler pack", lidTransform);
                AddTransformed(cad, document, assembly, utility, hardware.BodyPack,
                    "V09 low-profile over-centre latch body pack", lidTransform);
                AddTransformed(cad, document, assembly, utility, hardware.BailPack,
                    "V09 stainless latch bail pack", lidTransform);
                AddTransformed(cad, document, assembly, utility, hardware.IndicatorPack,
                    "V09 red locked-state witness pack", lidTransform);
                AddTransformed(cad, document, assembly, utility, hardware.CompressionPadPack,
                    "V09 defined-compression EPDM pad pack", lidTransform);
                AddTransformed(cad, document, assembly, utility, hardware.LidFastenerPack,
                    "V09 M3 latch/doubler/lid fasteners", lidTransform);
            }

            cad.Property(document, "Brand", "Rymovia Audio Systems");
            cad.Property(document, "Mechanical revision", "V0.9 secure transport lid / V0.8 Rymovia exterior");
            cad.Property(document, "Lid retention",
                "82 mm deep perimeter guide returns carry lateral loads; four side over-centre hooks provide positive axial retention");
            cad.Property(document, "Latch positions",
                "Two per side at y +/-196; 18 mm bridge envelope leaves 3 mm to the folded-leg outer cheek and 58 mm to the vent bank");
            cad.Property(document, "Preload path",
                "V7-small latch -> four M3 fasteners -> 2 mm doubler -> drilled 1.2 mm lid; keeper -> two shared M3 bolts -> 4 mm bridge -> 4 mm side band");
            cad.Property(document, "Compression control",
                "Four metal hard stops at z14 plus eight EPDM pad segments; free 2.8 mm, closed 2.0 mm, nominal compression 0.8 mm");
            cad.Property(document, "Production test boundary",
                "CAD verifies hole chains and closed-state clearances; import exact supplier STEP and perform locked-lid lift, cycle, vibration and drop tests");

            document.Extension.ForceRebuildAll();
            Require(document.ForceRebuild3(false), "Assembly rebuild failed: " + spec.TargetStem);
            assembly.UpdateBox();
            ValidateAssembly(document, assembly, spec);
            string saved = cad.SaveAssembly(document, spec.TargetStem, true);
            Require(SamePath(saved, target), "V0.9 assembly save escaped target path");
            ValidateAssembly(document, assembly, spec);
            cad.SaveAssembly(document, spec.TargetStem, false);
            Require(!document.GetSaveFlag(), "V0.9 assembly remains dirty after final save: " + spec.TargetStem);
            cad.Log("V09_ASSEMBLY=" + target + ";mass_kg=" + Format(ReadMass(document)));
        }
        finally { cad.Application.CloseDoc(document.GetTitle()); }
    }

    private static void ValidateAssembly(ModelDoc2 document, AssemblyDoc assembly, AssemblySpec spec)
    {
        List<Component2> components = TopLevelComponents(assembly);
        Require(CountStem(components, SecureSideStem) == 2, "Secure side-frame count is not two");
        Require(CountStem(components, OldSideStem) == 0, "Old side frame remains");
        Require(CountStem(components, OldCatchStem) == 0, "Old non-engaging catches remain");
        Require(CountStem(components, BridgePackStem) == 1, "Case bridge pack count is not one");
        Require(CountStem(components, KeeperPackStem) == 1, "Keeper pack count is not one");
        Require(CountStem(components, CaseFastenerPackStem) == 1,
            "Case latch fastener pack count is not one");
        Require(CountStem(components, DesktopReferenceStem) == 0,
            "Display-only desktop reference remains in V0.9 product/showcase assembly");
        int lidCount = spec.HasLid ? 1 : 0;
        Require(CountStem(components, TravelLidStem) == lidCount,
            "Drilled V0.9 travel-lid count mismatch");
        Require(CountStem(components, OldTravelLidStem) == 0,
            "Undrilled V0.8 travel lid remains");
        Require(CountStem(components, DoublerPackStem) == lidCount, "Lid doubler pack count mismatch");
        Require(CountStem(components, BodyPackStem) == lidCount, "Latch body pack count mismatch");
        Require(CountStem(components, BailPackStem) == lidCount, "Latch bail pack count mismatch");
        Require(CountStem(components, IndicatorPackStem) == lidCount, "Lock indicator pack count mismatch");
        Require(CountStem(components, CompressionPadPackStem) == lidCount,
            "Compression-pad pack count mismatch");
        Require(CountStem(components, LidFastenerPackStem) == lidCount,
            "Lid latch fastener pack count mismatch");

        double[] caseTransform = ReadTransform(FindStem(components, CaseReferenceStem));
        foreach (string caseStem in new[]
        {
            BridgePackStem, KeeperPackStem, CaseFastenerPackStem
        })
            RequireTransformEqual(ReadTransform(FindStem(components, caseStem)),
                caseTransform, caseStem + " follows actual case transform");

        if (spec.HasLid)
        {
            double[] lidTransform = ReadTransform(FindStem(components, TravelLidStem));
            foreach (string lidStem in new[]
            {
                DoublerPackStem, BodyPackStem, BailPackStem, IndicatorPackStem,
                CompressionPadPackStem, LidFastenerPackStem
            })
                RequireTransformEqual(ReadTransform(FindStem(components, lidStem)),
                    lidTransform, lidStem + " follows actual lid transform");
        }

        double mass = ReadMass(document);
        Require(mass > 0.0 && !double.IsNaN(mass) && !double.IsInfinity(mass),
            "V0.9 assembly mass property is invalid");
        if (spec.SourceStem.IndexOf("TransportClosed", StringComparison.OrdinalIgnoreCase) >= 0)
            Require(mass <= TransportMassBudgetKg,
                "Transport mass exceeds V0.9 budget; mass=" + Format(mass));

        ValidateAnalyticLayout();
    }

    private static void ReplaceOccurrences(ModelDoc2 document, AssemblyDoc assembly,
        string oldPath, string replacementPath, int expected, string context)
    {
        int count = 0;
        while (true)
        {
            Component2 found = TopLevelComponents(assembly).FirstOrDefault(c => SameComponentPath(c, oldPath));
            if (found == null) break;
            count++;
            Require(count <= expected, "Too many source occurrences for " + context);
            document.ClearSelection2(true);
            Require(found.Select4(false, null, false), "Cannot select source " + context);
            Require(assembly.ReplaceComponents(replacementPath, string.Empty, false, true),
                "SOLIDWORKS refused replacement for " + context);
            document.ClearSelection2(true);
        }
        Require(count == expected, "Expected " + expected + " replacements for " + context + "; actual " + count);
    }

    private static void RemoveExactOccurrences(ModelDoc2 document, AssemblyDoc assembly,
        string exactPath, int expected, string context)
    {
        int count = 0;
        while (true)
        {
            Component2 found = TopLevelComponents(assembly).FirstOrDefault(c => SameComponentPath(c, exactPath));
            if (found == null) break;
            count++;
            Require(count <= expected, "Too many occurrences while removing " + context);
            document.ClearSelection2(true);
            Require(found.Select4(false, null, false), "Cannot select exact " + context);
            Require(document.Extension.DeleteSelection2(0), "SOLIDWORKS refused to remove exact " + context);
            document.ClearSelection2(true);
        }
        Require(count == expected, "Expected " + expected + " removals for " + context + "; actual " + count);
        Require(File.Exists(exactPath), "Source part file must remain preserved: " + exactPath);
    }

    private static Component2 AddTransformed(RackCadSession cad, ModelDoc2 document,
        AssemblyDoc assembly, MathUtility utility, string partPath, string label, double[] transform)
    {
        Component2 component = cad.AddComponent(document, partPath, label, 0.0, 0.0, 0.0);
        if (component.IsFixed())
        {
            document.ClearSelection2(true);
            Require(component.Select4(false, null, false), "Cannot select fixed new component: " + label);
            assembly.UnfixComponent();
            document.ClearSelection2(true);
        }
        MathTransform math = utility.CreateTransform(transform) as MathTransform;
        Require(math != null, "Cannot create transform for " + label);
        component.Transform2 = math;
        double[] actual = ReadTransform(component);
        for (int i = 0; i < 12; i++) RequireClose(actual[i], transform[i], TransformTolerance,
            label + " transform " + i.ToString(CultureInfo.InvariantCulture));
        assembly.UpdateBox();
        return component;
    }

    private static void ApplyExistingTransform(ModelDoc2 document,
        AssemblyDoc assembly, MathUtility utility, Component2 component,
        double[] transform, string label)
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

    private static double[] OuterFaceDisplayTransform(double[] source)
    {
        Require(source != null && source.Length >= 16,
            "Detached lid source transform is unavailable");
        RequireClose(source[0], 1.0, TransformTolerance, "detached source r00");
        RequireClose(source[4], 1.0, TransformTolerance, "detached source r11");
        RequireClose(source[8], 1.0, TransformTolerance, "detached source r22");
        foreach (int index in new[] { 1, 2, 3, 5, 6, 7 })
            RequireClose(source[index], 0.0, TransformTolerance,
                "detached source rotation element " + index.ToString(CultureInfo.InvariantCulture));
        return new[]
        {
            -1.0, 0.0, 0.0,
             0.0, 1.0, 0.0,
             0.0, 0.0, -1.0,
             source[9], source[10], source[11],
             1.0, 0.0, 0.0, 0.0
        };
    }

    private static double[] CaseTransform60()
    {
        double angle = Math.PI / 3.0;
        double sine = Math.Sin(angle);
        double cosine = Math.Cos(angle);
        double originY = (0.0 - ShellContactY) * sine + (ShellContactZ - 0.0) * cosine;
        double originZ = (0.0 - ShellContactY) * cosine + (0.0 - ShellContactZ) * sine;
        return new[]
        {
            1.0, 0.0, 0.0,
            0.0, sine, cosine,
            0.0, -cosine, sine,
            0.0, originY / 1000.0, originZ / 1000.0,
            1.0, 0.0, 0.0, 0.0
        };
    }

    private static double[] IdentityTransform(double x, double y, double z)
    {
        return new[]
        {
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0,
            x / 1000.0, y / 1000.0, z / 1000.0,
            1.0, 0.0, 0.0, 0.0
        };
    }

    private static Point CalculateLoadStopCasePoint()
    {
        double angle = Math.PI / 3.0;
        double sine = Math.Sin(angle);
        double cosine = Math.Cos(angle);
        double hingeHeight = (HingeCaseY - ShellContactY) * sine +
            (ShellContactZ - HingeCaseZ) * cosine;
        double drop = hingeHeight - FootPadDeskCentreHeight;
        double detent = angle + Math.Asin(drop / PivotToFootPadCentre);
        double ds = Math.Sin(detent);
        double dc = Math.Cos(detent);
        return new Point(0.0,
            HingeCaseY + LoadStopLocalY * dc - LoadStopLocalZ * ds,
            HingeCaseZ + LoadStopLocalY * ds + LoadStopLocalZ * dc);
    }

    private static Body2 SideHole(RackCadSession cad, Body2 body,
        double y, double z, double diameter, string context)
    {
        return cad.Cut(body, cad.Cylinder(-InnerSideThickness / 2.0 - 0.3,
            y, z, 1.0, 0.0, 0.0, diameter, InnerSideThickness + 0.6), context);
    }

    private static Body2 Unite(Body2 first, Body2 second, string context)
    {
        int error = 0;
        object raw = first.Operations2((int)swBodyOperationType_e.SWBODYADD, second, out error);
        Require(error == (int)swBodyOperationError_e.swBodyOperationNoError,
            "Body union failed for " + context + "; error=" + error);
        Array array = raw as Array;
        Require(array != null && array.Length == 1, "Body union did not yield one solid for " + context);
        Body2 result = array.GetValue(array.GetLowerBound(0)) as Body2;
        Require(result != null, "Body union returned null for " + context);
        return result;
    }

    private static IEnumerable<double> RailPositions(RackCadSession cad)
    {
        double rowPitch = cad.N("eurorack", "row_pitch");
        double spacing = cad.N("eurorack", "mounting_hole_vertical_spacing");
        foreach (double centre in new[] { -rowPitch, 0.0, rowPitch })
        {
            yield return centre - spacing / 2.0;
            yield return centre + spacing / 2.0;
        }
    }

    private static IEnumerable<double> Signs()
    {
        yield return -1.0;
        yield return 1.0;
    }

    private static void ValidatePart(ModelDoc2 document, int expectedBodies, Bounds expected, string context)
    {
        PartDoc part = document as PartDoc;
        Require(part != null, context + " is not a part");
        Array raw = part.GetBodies2((int)swBodyType_e.swSolidBody, false) as Array;
        Require(raw != null && raw.Length == expectedBodies,
            context + " body count mismatch; expected " + expectedBodies + "; actual " + (raw == null ? 0 : raw.Length));
        Bounds actual = Bounds.FromBodies(raw);
        RequireClose(actual.MinX, expected.MinX, GeometryTolerance, context + " minX");
        RequireClose(actual.MinY, expected.MinY, GeometryTolerance, context + " minY");
        RequireClose(actual.MinZ, expected.MinZ, GeometryTolerance, context + " minZ");
        RequireClose(actual.MaxX, expected.MaxX, GeometryTolerance, context + " maxX");
        RequireClose(actual.MaxY, expected.MaxY, GeometryTolerance, context + " maxY");
        RequireClose(actual.MaxZ, expected.MaxZ, GeometryTolerance, context + " maxZ");
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
                if (doc.GetSaveFlag()) throw new InvalidOperationException("Dirty target is open: " + path);
                cad.Application.CloseDoc(doc.GetTitle());
                return;
            }
            doc = next;
        }
    }

    private static void OpenFinalShowcase(RackCadSession cad)
    {
        AssemblySpec final = Assemblies.Last();
        string path = AssemblyPath(cad, final.TargetStem);
        ModelDoc2 document = OpenAssembly(cad, path);
        cad.Application.Visible = true;
        cad.Application.UserControl = true;
        cad.Application.FrameState = (int)swWindowState_e.swWindowMaximized;
        cad.Show(document);
        document.ViewZoomtofit2();
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

    private static int CountStem(IEnumerable<Component2> components, string stem)
    {
        return components.Count(c => SameStem(c, stem));
    }

    private static Component2 FindStem(IEnumerable<Component2> components, string stem)
    {
        Component2 result = components.FirstOrDefault(c => SameStem(c, stem));
        Require(result != null, "Component not found: " + stem);
        return result;
    }

    private static bool SameStem(Component2 component, string stem)
    {
        string path = component.GetPathName();
        return !string.IsNullOrWhiteSpace(path) &&
            string.Equals(Path.GetFileNameWithoutExtension(path), stem, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SameComponentPath(Component2 component, string path)
    {
        return SamePath(component.GetPathName(), path);
    }

    private static double[] ReadTransform(Component2 component)
    {
        MathTransform transform = component.Transform2;
        Array raw = transform == null ? null : transform.ArrayData as Array;
        Require(raw != null && raw.Length >= 16, "Component transform is unavailable: " + component.Name2);
        double[] result = new double[16];
        for (int i = 0; i < 16; i++) result[i] = Convert.ToDouble(raw.GetValue(i), CultureInfo.InvariantCulture);
        return result;
    }

    private static double ReadMass(ModelDoc2 document)
    {
        MassProperty mass = document.Extension.CreateMassProperty();
        return mass == null ? double.NaN : mass.Mass;
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
        string root = Path.GetFullPath(cad.Root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Require(full.StartsWith(root, StringComparison.OrdinalIgnoreCase), "Path escaped project root: " + full);
        Require(File.Exists(full) && new FileInfo(full).Length > 0, "Required project file missing: " + full);
    }

    private static bool SamePath(string first, string second)
    {
        return !string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(second) &&
            string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), StringComparison.OrdinalIgnoreCase);
    }

    private static string Hash(string path)
    {
        using (SHA256 sha = SHA256.Create())
        using (FileStream stream = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
    }

    private static void RequireClose(double actual, double expected, double tolerance, string context)
    {
        Require(Math.Abs(actual - expected) <= tolerance,
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

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string Format(double value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private sealed class AssemblySpec
    {
        internal readonly string SourceStem;
        internal readonly string TargetStem;
        internal readonly bool CaseTilt60;
        internal readonly bool HasLid;
        internal readonly bool DetachedLid;

        internal AssemblySpec(string source, string target, bool caseTilt60, bool hasLid, bool detachedLid)
        {
            SourceStem = source;
            TargetStem = target;
            CaseTilt60 = caseTilt60;
            HasLid = hasLid;
            DetachedLid = detachedLid;
        }
    }

    private sealed class HardwarePaths
    {
        internal string SecureLid;
        internal string SecureSide;
        internal string BridgePack;
        internal string KeeperPack;
        internal string DoublerPack;
        internal string BodyPack;
        internal string BailPack;
        internal string IndicatorPack;
        internal string CompressionPadPack;
        internal string CaseFastenerPack;
        internal string LidFastenerPack;
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

    private sealed class MountPoint
    {
        internal readonly double Y;
        internal readonly double Z;
        internal MountPoint(double y, double z) { Y = y; Z = z; }
    }

    private sealed class Point
    {
        internal readonly double X;
        internal readonly double Y;
        internal readonly double Z;
        internal Point(double x, double y, double z) { X = x; Y = y; Z = z; }
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
            double minX = double.PositiveInfinity, minY = double.PositiveInfinity, minZ = double.PositiveInfinity;
            double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity, maxZ = double.NegativeInfinity;
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
