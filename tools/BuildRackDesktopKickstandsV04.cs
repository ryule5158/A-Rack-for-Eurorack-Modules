using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

internal static class BuildRackDesktopKickstandsV04
{
    private const string PreviousLegFileName = "SideRecessedLeg_V03_TwoPosition.SLDPRT";
    private const string LegStem = "SideKickstand_V04_LowerPivot150mm";
    private const string DesktopStem = "DesktopReferenceSurface_V04";
    private const string OpenV03Stem = "Rack4Modules_OpenCase_V03";
    private const string OpenV04Stem = "Rack4Modules_OpenCase_V04";
    private const string TransportV04Stem = "Rack4Modules_TransportClosed_V04";
    private const string ClearanceV04Stem = "Rack4Modules_ClearanceCheck_V04";
    private const string Tilt60Stem = "Rack4Modules_DesktopTilt60_V04";
    private const string Tilt75Stem = "Rack4Modules_DesktopTilt75_V04";

    private const double CaseWidth = 548.0;
    private const double CaseHeight = 420.0;
    private const double CaseDepth = 110.0;

    // The hard-shell desktop datum is the broad-back lower corner (case y=-210, case z=110).
    // The four broad-back rubber feet are centred at y=+/-185 and finish at z=116; their lower
    // y=-191 envelope remains above the desktop at both target angles, so z=116 is not the datum.
    private const double ShellContactY = -210.0;
    private const double ShellContactZ = 110.0;

    // The V0.4 part keeps the V0.3 local origin so folded component placement stays unchanged.
    // Folded origin: x=+/-271, y=-54, z=46. Local hinge: (0,-75,6).
    // Therefore the actual lower hinge is (x=+/-271, y=-129, z=52).
    private const double FoldedX = 271.0;
    private const double FoldedY = -54.0;
    private const double FoldedZ = 46.0;
    private const double ExpandedX = 278.0;
    private const double HingeLocalY = -75.0;
    private const double HingeLocalZ = 6.0;
    private const double HingeCaseY = -129.0;
    private const double HingeCaseZ = 52.0;
    private const double OldHingeCaseY = 19.0;
    private const double ArmContactLength = 150.0;
    private const double TipLocalY = 75.0;
    private const double TipLocalZ = 6.0;
    private const double GroundTolerance = 0.1;

    private static readonly double[] DarkHardware = new double[] { 0.075, 0.085, 0.10 };
    private static readonly double[] DeskAppearance = new double[] { 0.27, 0.30, 0.34 };

    [STAThread]
    private static int Main(string[] arguments)
    {
        try
        {
            if (arguments.Length < 1 || arguments.Length > 2)
            {
                throw new ArgumentException(
                    "Usage: BuildRackDesktopKickstandsV04.exe <Rack4Modules root> [--show-tilt60|--show-open-v04|--restore-v03]");
            }

            string finalView = arguments.Length == 2 ? arguments[1] : "--show-tilt60";
            if (!string.Equals(finalView, "--show-tilt60", StringComparison.Ordinal) &&
                !string.Equals(finalView, "--show-open-v04", StringComparison.Ordinal) &&
                !string.Equals(finalView, "--restore-v03", StringComparison.Ordinal))
            {
                throw new ArgumentException("Unsupported final assembly selection: " + finalView);
            }

            RackCadSession cad = new RackCadSession(Path.GetFullPath(arguments[0]));
            VerifyFrozenCaseEnvelope(cad);
            VerifySourceAssemblies(cad);

            Stance stance60 = CalculateStance(60.0);
            Stance stance75 = CalculateStance(75.0);
            LogKinematics(cad, stance60);
            LogKinematics(cad, stance75);

            string legPath = CreateLowerPivotLeg(cad);
            string desktopPath = CreateDesktopReferenceSurface(cad);

            CreateFoldedV04Copy(cad, OpenV03Stem, OpenV04Stem, legPath);
            CreateFoldedV04Copy(
                cad,
                "Rack4Modules_TransportClosed_V03",
                TransportV04Stem,
                legPath);
            CreateFoldedV04Copy(
                cad,
                "Rack4Modules_ClearanceCheck_V03",
                ClearanceV04Stem,
                legPath);

            CreateDesktopStanceAssembly(cad, stance60, Tilt60Stem, legPath, desktopPath);
            CreateDesktopStanceAssembly(cad, stance75, Tilt75Stem, legPath, desktopPath);

            string finalStem = string.Equals(finalView, "--show-open-v04", StringComparison.Ordinal)
                ? OpenV04Stem
                : string.Equals(finalView, "--restore-v03", StringComparison.Ordinal)
                    ? OpenV03Stem
                    : Tilt60Stem;

            ShowFinalAssembly(cad, finalStem);
            cad.Log("V04_ORIGINAL_V03_ASSEMBLIES_PRESERVED=true");
            cad.Log("V04_FOLDED_TRANSPORT_WIDTH_MM=548");
            cad.Log("V04_DEPLOYED_SUPPORT_WIDTH_MM=562");
            cad.Log("V04_DESKTOP_CONTACT_TOLERANCE_MM=" + Format(GroundTolerance));
            cad.Log("V04_DESKTOP_KICKSTAND_BUILD_COMPLETE=true");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("V04_DESKTOP_KICKSTAND_BUILD_FAILED=" + exception);
            Console.Error.Flush();
            return 1;
        }
    }

    private static void VerifyFrozenCaseEnvelope(RackCadSession cad)
    {
        RequireClose(cad.N("enclosure", "outer_width"), CaseWidth, 0.001, "case outer width");
        RequireClose(cad.N("enclosure", "outer_height"), CaseHeight, 0.001, "case outer height");
        RequireClose(cad.N("enclosure", "body_depth"), CaseDepth, 0.001, "case body depth");
        RequireClose(FoldedY + HingeLocalY, HingeCaseY, 0.000001, "lower hinge y");
        RequireClose(FoldedZ + HingeLocalZ, HingeCaseZ, 0.000001, "lower hinge z");
        RequireClose(TipLocalY - HingeLocalY, ArmContactLength, 0.000001, "hinge-to-tip arm length");
    }

    private static void VerifySourceAssemblies(RackCadSession cad)
    {
        foreach (string stem in new string[]
        {
            OpenV03Stem,
            "Rack4Modules_TransportClosed_V03",
            "Rack4Modules_ClearanceCheck_V03"
        })
        {
            string path = AssemblyPath(cad, stem);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The required original V0.3 assembly does not exist.", path);
            }
        }
    }

    private static Stance CalculateStance(double faceAngleDegrees)
    {
        double angle = DegreesToRadians(faceAngleDegrees);
        double sine = Math.Sin(angle);
        double cosine = Math.Cos(angle);

        // Case-to-desk map, in millimetres:
        //   Ydesk = (y + 210) sin(alpha) + (110 - z) cos(alpha)
        //   Zdesk = (y + 210) cos(alpha) + (z - 110) sin(alpha)
        // Consequently the shell corner (-210,110) lies exactly on Ydesk=0,Zdesk=0.
        double hingeHeight = (HingeCaseY - ShellContactY) * sine +
            (ShellContactZ - HingeCaseZ) * cosine;
        if (hingeHeight <= 0.0 || hingeHeight >= ArmContactLength)
        {
            throw new InvalidOperationException(
                "The lower hinge cannot reach the desktop at " + Format(faceAngleDegrees) + " degrees.");
        }

        double horizontalArmReach = Math.Sqrt(
            ArmContactLength * ArmContactLength - hingeHeight * hingeHeight);
        double detentDegrees = faceAngleDegrees +
            RadiansToDegrees(Math.Asin(hingeHeight / ArmContactLength));
        double hingeHorizontalOffset = (HingeCaseY - ShellContactY) * cosine +
            (HingeCaseZ - ShellContactZ) * sine;
        double supportFootprint = hingeHorizontalOffset + horizontalArmReach;

        if (supportFootprint <= 40.0)
        {
            throw new InvalidOperationException(
                "The rear kickstand footprint is too small at " + Format(faceAngleDegrees) + " degrees.");
        }

        double previousHingeHeight = (OldHingeCaseY - ShellContactY) * sine +
            (ShellContactZ - HingeCaseZ) * cosine;
        if (previousHingeHeight <= ArmContactLength)
        {
            throw new InvalidOperationException(
                "The prior upper-hinge reach diagnosis no longer matches the case geometry.");
        }

        double lowBackFootHeight = (-191.0 - ShellContactY) * sine +
            (ShellContactZ - 116.0) * cosine;
        if (lowBackFootHeight <= GroundTolerance)
        {
            throw new InvalidOperationException(
                "A broad-back foot would replace the selected hard-shell desktop contact datum.");
        }

        Stance result = new Stance();
        result.FaceAngleDegrees = faceAngleDegrees;
        result.AngleRadians = angle;
        result.DetentDegrees = detentDegrees;
        result.DetentRadians = DegreesToRadians(detentDegrees);
        result.HingeHeight = hingeHeight;
        result.HingeHorizontalOffset = hingeHorizontalOffset;
        result.HorizontalArmReach = horizontalArmReach;
        result.SupportFootprint = supportFootprint;
        result.OldUpperHingeHeight = previousHingeHeight;
        result.BackRubberFootClearance = lowBackFootHeight;
        return result;
    }

    private static void LogKinematics(RackCadSession cad, Stance stance)
    {
        string prefix = "V04_" + Format(stance.FaceAngleDegrees) + "DEG_";
        cad.Log(prefix + "SHELL_CONTACT_CASE_YZ_MM=-210,110");
        cad.Log(prefix + "LOWER_HINGE_CASE_YZ_MM=-129,52");
        cad.Log(prefix + "OLD_UPPER_HINGE_HEIGHT_MM=" + Format(stance.OldUpperHingeHeight));
        cad.Log(prefix + "LOWER_HINGE_HEIGHT_MM=" + Format(stance.HingeHeight));
        cad.Log(prefix + "DETENT_ROTATION_DEG=" + Format(stance.DetentDegrees));
        cad.Log(prefix + "REAR_SUPPORT_FOOTPRINT_MM=" + Format(stance.SupportFootprint));
        cad.Log(prefix + "BACK_FOOT_CLEARANCE_MM=" + Format(stance.BackRubberFootClearance));
        cad.Log(prefix + "STABILITY_RULE=loaded CG projection must remain 20 mm inside both support boundaries");
        if (Math.Abs(stance.FaceAngleDegrees - 75.0) < 0.001)
        {
            cad.Log(prefix + "WARNING=75-degree stability depends on measured fully-loaded module CG, detents and anti-slip feet");
        }
    }

    private static string CreateLowerPivotLeg(RackCadSession cad)
    {
        ModelDoc2 part = cad.NewPart(LegStem);

        // Aluminium arm spans local y=-75..65 and z=0..12. The last 10 mm is a removable
        // anti-slip end. Its local z=4..6 stays on the safe side of both desktop contact planes.
        cad.AddBody(part, cad.Box(0.0, -5.0, 0.0, 6.0, 140.0, 12.0),
            "6061 folding arm; lower-pivot to replaceable foot carrier");

        // Diameter 16 shoulder is centred at local (y,z)=(-75,6), giving y=-137 at the
        // folded lower envelope and clearing the existing side pocket without extending it.
        cad.AddBody(part, cad.Cylinder(-3.0, HingeLocalY, HingeLocalZ,
            1.0, 0.0, 0.0, 16.0, 6.0),
            "Diameter 16 lower spring-loaded hinge shoulder; seven millimetre axial release");

        cad.AddBody(part, cad.Box(0.0, 70.0, 4.0, 6.0, 10.0, 2.0),
            "Replaceable anti-slip elastomer end envelope; desktop contact datum y75 z6");

        cad.ApplyMaterial(part, "6061-T6 (SS)", DarkHardware);
        cad.Property(part, "Assembly state", "Folded centre x +/-271, y -54, z 46; deployed centre x +/-278");
        cad.Property(part, "Lower hinge local coordinates", "x 0 mm; y -75 mm; z 6 mm");
        cad.Property(part, "Lower hinge case coordinates", "y -129 mm; z 52 mm");
        cad.Property(part, "Hinge to desktop contact datum", "150 mm; local foot contact y 75 mm, z 6 mm");
        cad.Property(part, "Spring shoulder release", "7 mm outward before rotating; supplier spring and bushing required");
        cad.Property(part, "Anti-slip end", "Replaceable elastomer envelope only; supplier material and compression pending");
        cad.Property(part, "Mechanical detents", "101.375 degrees for 60-degree face; 113.439 degrees for 75-degree face");
        cad.Property(part, "Safety status", "No physical hinge, detent, slip, fatigue or loaded-CG validation completed");

        string path = cad.SavePart(part, LegStem, true);
        cad.Application.CloseDoc(part.GetTitle());
        cad.Log("V04_KICKSTAND_PART=" + path + "; solid_bodies=3");
        return path;
    }

    private static string CreateDesktopReferenceSurface(RackCadSession cad)
    {
        ModelDoc2 desktop = cad.NewPart(DesktopStem);

        // Box is centred at y=-1 with a height of 2, so its exact top face is the Y=0 desk.
        cad.AddBody(desktop, cad.Box(0.0, -1.0, -180.0, 680.0, 2.0, 540.0),
            "Display-only desktop reference surface; top plane Y equals zero");
        cad.ApplyMaterial(desktop, "6061-T6 (SS)", DeskAppearance);
        cad.Property(desktop, "Role", "Display and desk-contact reference only; never part of the delivered product");
        cad.Property(desktop, "BOM and mass", "Excluded from BOM; do not include in enclosure mass or shipping weight");
        cad.Property(desktop, "Desktop datum", "Exact upper surface Y = 0 mm");

        string path = cad.SavePart(desktop, DesktopStem, false);
        cad.Application.CloseDoc(desktop.GetTitle());
        cad.Log("V04_DESKTOP_REFERENCE_PART=" + path + "; exclude_from_product_mass=true");
        return path;
    }

    private static void CreateFoldedV04Copy(
        RackCadSession cad,
        string originalStem,
        string targetStem,
        string replacementLegPath)
    {
        ModelDoc2 document = CloneNativeAssembly(cad, AssemblyPath(cad, originalStem), targetStem);
        AssemblyDoc assembly = document as AssemblyDoc;
        if (assembly == null)
        {
            throw new InvalidOperationException("The cloned V0.4 document is not an assembly: " + targetStem);
        }

        ReplaceExactlyTwoKickstands(document, assembly, replacementLegPath);
        MathUtility utility = RequireMathUtility(cad);
        int foldedCount = 0;

        foreach (Component2 component in TopLevelComponents(assembly))
        {
            if (!IsPart(component, LegStem + ".SLDPRT"))
            {
                continue;
            }

            double[] previous = ReadTransform(component, "replacement folding leg");
            double sign = previous[9] < 0.0 ? -1.0 : 1.0;
            double[] folded = IdentityTransform(sign * FoldedX, FoldedY, FoldedZ);
            ApplyComponentTransform(document, assembly, utility, component, folded, "folded lower-pivot leg");
            VerifyFoldedBounds(component, sign);
            foldedCount++;
        }

        if (foldedCount != 2)
        {
            throw new InvalidOperationException(
                "A V0.4 assembly must contain exactly two lower-pivot kickstands: " + targetStem);
        }

        cad.Property(document, "Desktop support revision", "V0.4 lower-pivot, axially released two-position kickstands");
        cad.Property(document, "Folded kickstand origin", "x +/-271 mm; y -54 mm; z 46 mm");
        cad.Property(document, "Folded lower hinge", "x +/-271 mm; y -129 mm; z 52 mm");
        cad.Property(document, "Transport envelope", "Folded side kickstands remain within 548 mm overall width");
        cad.Property(document, "Deployed kickstand width", "562 mm after 7 mm outward release on each side");
        cad.Property(document, "Source assembly", originalStem + ".SLDASM; original V0.3 file preserved");
        cad.Property(document, "75 degree stability", "Pending loaded centre-of-gravity, detent and anti-slip validation");
        cad.SaveAssembly(document, targetStem, true);
        cad.Application.CloseDoc(document.GetTitle());
        cad.Log("V04_FOLDED_VARIANT=" + targetStem + "; replaced_legs=2; original_preserved=true");
    }

    private static void ReplaceExactlyTwoKickstands(
        ModelDoc2 document,
        AssemblyDoc assembly,
        string replacementPath)
    {
        int replacements = 0;
        while (true)
        {
            Component2 original = null;
            foreach (Component2 component in TopLevelComponents(assembly))
            {
                if (IsPart(component, PreviousLegFileName))
                {
                    original = component;
                    break;
                }
            }

            if (original == null)
            {
                break;
            }

            if (++replacements > 2)
            {
                throw new InvalidOperationException("More than two obsolete side kickstands were found.");
            }

            document.ClearSelection2(true);
            if (!original.Select4(false, null, false))
            {
                throw new InvalidOperationException("The obsolete folding leg could not be selected for replacement.");
            }

            if (!assembly.ReplaceComponents(replacementPath, string.Empty, false, true))
            {
                throw new InvalidOperationException("SOLIDWORKS refused the lower-pivot folding-leg replacement.");
            }

            document.ClearSelection2(true);
        }

        int currentCount = 0;
        foreach (Component2 component in TopLevelComponents(assembly))
        {
            if (IsPart(component, LegStem + ".SLDPRT"))
            {
                currentCount++;
            }
        }

        if (replacements != 2 || currentCount != 2)
        {
            throw new InvalidOperationException(
                "Expected exactly two obsolete legs and exactly two replacements; replaced=" +
                replacements.ToString(CultureInfo.InvariantCulture) + "; new=" +
                currentCount.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void VerifyFoldedBounds(Component2 component, double sign)
    {
        Array raw = component.GetBox(false, false) as Array;
        if (raw == null || raw.Length < 6)
        {
            throw new InvalidOperationException("The folded kickstand bounding box could not be checked.");
        }

        double minX = Millimetres(raw.GetValue(0));
        double minY = Millimetres(raw.GetValue(1));
        double minZ = Millimetres(raw.GetValue(2));
        double maxX = Millimetres(raw.GetValue(3));
        double maxY = Millimetres(raw.GetValue(4));
        double maxZ = Millimetres(raw.GetValue(5));

        if (minX < -274.1 || maxX > 274.1 || minY < -137.1 || maxY > 27.1 ||
            minZ < 41.9 || maxZ > 64.1)
        {
            throw new InvalidOperationException(
                "A folded lower-pivot leg escapes its existing side-frame recess: " +
                Format(minX) + "," + Format(minY) + "," + Format(minZ) + ".." +
                Format(maxX) + "," + Format(maxY) + "," + Format(maxZ));
        }

        if (sign < 0.0 && maxX > -267.9 || sign > 0.0 && minX < 267.9)
        {
            throw new InvalidOperationException("A folded kickstand no longer sits against its correct side frame.");
        }
    }

    private static void CreateDesktopStanceAssembly(
        RackCadSession cad,
        Stance stance,
        string targetStem,
        string replacementLegPath,
        string desktopPath)
    {
        ModelDoc2 document = CloneNativeAssembly(cad, AssemblyPath(cad, OpenV04Stem), targetStem);
        AssemblyDoc assembly = document as AssemblyDoc;
        if (assembly == null)
        {
            throw new InvalidOperationException("The desktop stance clone is not an assembly: " + targetStem);
        }

        MathUtility utility = RequireMathUtility(cad);
        int deployedLegCount = 0;
        bool verifiedShellContact = false;

        foreach (Component2 component in TopLevelComponents(assembly))
        {
            double[] existing = ReadTransform(component, "source component in " + targetStem);
            VerifyUnrotatedSource(existing, component);

            if (IsPart(component, Path.GetFileName(replacementLegPath)))
            {
                double sign = existing[9] < 0.0 ? -1.0 : 1.0;
                double[] deployed = DeployedLegTransform(stance, sign);
                ApplyComponentTransform(document, assembly, utility, component, deployed,
                    "expanded and rotated lower-pivot desktop kickstand");
                VerifyDeployedContact(component, deployed, stance, sign);
                deployedLegCount++;
                continue;
            }

            Point point = CasePointToDesk(
                existing[9] * 1000.0,
                existing[10] * 1000.0,
                existing[11] * 1000.0,
                stance);

            double[] transformed = CaseTransform(point, stance);
            ApplyComponentTransform(document, assembly, utility, component, transformed,
                "rigid enclosure component at " + Format(stance.FaceAngleDegrees) + " degrees");

            if (IsPart(component, "BackPanel_V03_VESAOnly.SLDPRT"))
            {
                VerifyShellContact(component, stance);
                verifiedShellContact = true;
            }
        }

        if (deployedLegCount != 2 || !verifiedShellContact)
        {
            throw new InvalidOperationException(
                "The stance assembly requires two deployed legs and a verified broad-back lower corner.");
        }

        Component2 desktop = cad.AddComponent(document, desktopPath,
            "Display-only desktop surface; excluded from BOM and product mass", 0.0, 0.0, 0.0);
        desktop.ExcludeFromBOM = true;

        cad.Property(document, "Module-face desktop angle", Format(stance.FaceAngleDegrees) + " degrees");
        cad.Property(document, "Hard-shell desktop contact", "Case lower broad-back corner y -210 mm, z 110 mm");
        cad.Property(document, "Kickstand lower hinge", "Case y -129 mm, z 52 mm; x released from +/-271 to +/-278 mm");
        cad.Property(document, "Hinge-to-anti-slip contact", "150 mm; local rubber contact point y 75 mm, z 6 mm");
        cad.Property(document, "Mechanical stop angle", Format(stance.DetentDegrees) + " degrees from folded arm");
        cad.Property(document, "Rear support distance", Format(stance.SupportFootprint) + " mm behind shell contact");
        cad.Property(document, "Deployed overall width", "562 mm; folded transport width remains 548 mm");
        cad.Property(document, "Desktop reference", "Display-only reference surface excluded from BOM and product mass");
        cad.Property(document, "Contact verification", "Both anti-slip contact points and shell contact lie on Y = 0 within 0.1 mm");
        cad.Property(document, "Stability acceptance",
            "Loaded CG projection must remain at least 20 mm inside both support boundaries; physical validation pending");
        if (Math.Abs(stance.FaceAngleDegrees - 75.0) < 0.001)
        {
            cad.Property(document, "75 degree warning",
                "Measure fully loaded module CG, both positive detents and anti-slip coefficient before approving this position");
        }

        cad.SaveAssembly(document, targetStem, true);
        cad.Application.CloseDoc(document.GetTitle());
        cad.Log("V04_DESKTOP_STANCE=" + targetStem + "; face_angle=" +
            Format(stance.FaceAngleDegrees) + "; support_mm=" + Format(stance.SupportFootprint));
    }

    private static double[] DeployedLegTransform(Stance stance, double sign)
    {
        // SOLIDWORKS transform rows are the local X/Y/Z axes expressed in assembly coordinates.
        // Case axes: X=(1,0,0), Y=(0,sin(alpha),cos(alpha)), Z=(0,-cos(alpha),sin(alpha)).
        // The leg first rotates by phi about its local hinge X axis, then the complete case
        // rotates onto the desktop. Its net local axes therefore contain alpha-phi.
        double relative = stance.AngleRadians - stance.DetentRadians;
        double sine = Math.Sin(relative);
        double cosine = Math.Cos(relative);

        Point pivot = CasePointToDesk(sign * ExpandedX, HingeCaseY, HingeCaseZ, stance);

        // The replacement part retains its V0.3 local origin: hinge local=(0,-75,6).
        // Translate that local hinge to the desired desk-world pivot without moving the axle.
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

    private static void VerifyDeployedContact(
        Component2 component,
        double[] requested,
        Stance stance,
        double sign)
    {
        double[] actual = ReadTransform(component, "deployed kickstand");
        Point hinge = ApplyTransformToPoint(actual, 0.0, HingeLocalY, HingeLocalZ);
        Point tip = ApplyTransformToPoint(actual, 0.0, TipLocalY, TipLocalZ);

        RequireClose(hinge.X, sign * ExpandedX, GroundTolerance, "expanded hinge x");
        RequireClose(hinge.Y, stance.HingeHeight, GroundTolerance, "expanded hinge height");
        RequireClose(tip.X, sign * ExpandedX, GroundTolerance, "anti-slip contact x");
        RequireClose(tip.Y, 0.0, GroundTolerance, "anti-slip tip desktop height");
        RequireClose(tip.Z, stance.SupportFootprint, GroundTolerance, "rear anti-slip support distance");

        double[] reread = ReadTransform(component, "deployed kickstand readback");
        for (int index = 0; index < 12; index++)
        {
            RequireClose(reread[index], requested[index], 0.0000001,
                "deployed transform readback element " + index.ToString(CultureInfo.InvariantCulture));
        }

        Array bounds = component.GetBox(false, false) as Array;
        if (bounds != null && bounds.Length >= 6)
        {
            double minX = Millimetres(bounds.GetValue(0));
            double maxX = Millimetres(bounds.GetValue(3));
            if (minX < -281.1 || maxX > 281.1)
            {
                throw new InvalidOperationException("A released kickstand exceeds the 562 mm deployed width.");
            }
        }
    }

    private static void VerifyShellContact(Component2 backPanel, Stance stance)
    {
        double[] transform = ReadTransform(backPanel, "tilted broad-back panel");
        Point left = ApplyTransformToPoint(transform, -274.0, ShellContactY, ShellContactZ);
        Point right = ApplyTransformToPoint(transform, 274.0, ShellContactY, ShellContactZ);

        RequireClose(left.Y, 0.0, GroundTolerance, "left shell-to-desktop contact");
        RequireClose(right.Y, 0.0, GroundTolerance, "right shell-to-desktop contact");
        RequireClose(left.Z, 0.0, GroundTolerance, "left shell contact rear-depth origin");
        RequireClose(right.Z, 0.0, GroundTolerance, "right shell contact rear-depth origin");

        double measuredAngle = RadiansToDegrees(Math.Atan2(
            Math.Abs(transform[4]), Math.Abs(transform[5])));
        RequireClose(measuredAngle, stance.FaceAngleDegrees, 0.0001, "module face-to-desktop angle");
    }

    private static double[] CaseTransform(Point componentOrigin, Stance stance)
    {
        double sine = Math.Sin(stance.AngleRadians);
        double cosine = Math.Cos(stance.AngleRadians);
        return new double[]
        {
            1.0, 0.0, 0.0,
            0.0, sine, cosine,
            0.0, -cosine, sine,
            componentOrigin.X / 1000.0,
            componentOrigin.Y / 1000.0,
            componentOrigin.Z / 1000.0,
            1.0, 0.0, 0.0, 0.0
        };
    }

    private static Point CasePointToDesk(double x, double y, double z, Stance stance)
    {
        double sine = Math.Sin(stance.AngleRadians);
        double cosine = Math.Cos(stance.AngleRadians);
        Point point = new Point();
        point.X = x;
        point.Y = (y - ShellContactY) * sine + (ShellContactZ - z) * cosine;
        point.Z = (y - ShellContactY) * cosine + (z - ShellContactZ) * sine;
        return point;
    }

    private static Point ApplyTransformToPoint(double[] transform, double x, double y, double z)
    {
        Point point = new Point();
        point.X = x * transform[0] + y * transform[3] + z * transform[6] + transform[9] * 1000.0;
        point.Y = x * transform[1] + y * transform[4] + z * transform[7] + transform[10] * 1000.0;
        point.Z = x * transform[2] + y * transform[5] + z * transform[8] + transform[11] * 1000.0;
        return point;
    }

    private static ModelDoc2 CloneNativeAssembly(RackCadSession cad, string originalPath, string targetStem)
    {
        ModelDoc2 original = OpenExactAssembly(cad, originalPath);
        PreservePendingSourceChanges(original, originalPath);

        string targetPath = AssemblyPath(cad, targetStem);
        ModelDoc2 existingTarget = cad.Application.GetOpenDocumentByName(targetPath) as ModelDoc2;
        if (existingTarget != null)
        {
            if (existingTarget.GetSaveFlag())
            {
                throw new InvalidOperationException(
                    "The generated target is already open with unsaved changes; refusing to overwrite it: " + targetPath);
            }

            cad.Application.CloseDoc(existingTarget.GetTitle());
        }

        int activationStatus = 0;
        ModelDoc2 active = cad.Application.ActivateDoc3(
            original.GetTitle(), false, (int)swRebuildOnActivation_e.swDontRebuildActiveDoc,
            ref activationStatus) as ModelDoc2;
        if (active == null)
        {
            throw new InvalidOperationException("Cannot activate the source assembly for safe native cloning.");
        }

        int errors = 0;
        int warnings = 0;
        bool copied = original.Extension.SaveAs(
            targetPath,
            (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
            (int)swSaveAsOptions_e.swSaveAsOptions_Silent,
            null,
            ref errors,
            ref warnings);

        if (!copied || errors != 0 || !File.Exists(targetPath) || new FileInfo(targetPath).Length <= 0)
        {
            throw new InvalidOperationException(
                "Cannot create the independent V0.4 assembly copy '" + targetPath +
                "'; errors=" + errors.ToString(CultureInfo.InvariantCulture) +
                "; warnings=" + warnings.ToString(CultureInfo.InvariantCulture));
        }

        string actualPath = Path.GetFullPath(original.GetPathName());
        if (!string.Equals(actualPath, targetPath, StringComparison.OrdinalIgnoreCase))
        {
            ModelDoc2 copiedDocument = cad.Application.GetOpenDocumentByName(targetPath) as ModelDoc2;
            if (copiedDocument == null)
            {
                copiedDocument = OpenExactAssembly(cad, targetPath);
            }

            original = copiedDocument;
        }

        if (!File.Exists(originalPath))
        {
            throw new InvalidOperationException("The source V0.3/V0.4 assembly was not preserved: " + originalPath);
        }

        return original;
    }

    private static void PreservePendingSourceChanges(ModelDoc2 source, string sourcePath)
    {
        if (!source.GetSaveFlag())
        {
            return;
        }

        int errors = 0;
        int warnings = 0;
        bool saved = source.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref errors, ref warnings);
        if (!saved || errors != 0)
        {
            throw new InvalidOperationException(
                "Existing project assembly changes could not be preserved before cloning: " + sourcePath);
        }
    }

    private static ModelDoc2 OpenExactAssembly(RackCadSession cad, string path)
    {
        string expected = Path.GetFullPath(path);
        if (!File.Exists(expected))
        {
            throw new FileNotFoundException("The requested project assembly was not found.", expected);
        }

        ModelDoc2 document = cad.Application.GetOpenDocumentByName(expected) as ModelDoc2;
        if (document == null)
        {
            int errors = 0;
            int warnings = 0;
            document = cad.Application.OpenDoc6(
                expected,
                (int)swDocumentTypes_e.swDocASSEMBLY,
                (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                string.Empty,
                ref errors,
                ref warnings) as ModelDoc2;

            if (document == null || errors != 0)
            {
                throw new InvalidOperationException(
                    "Cannot open the exact project assembly '" + expected + "'; errors=" +
                    errors.ToString(CultureInfo.InvariantCulture));
            }
        }

        if (!(document is AssemblyDoc) ||
            !string.Equals(Path.GetFullPath(document.GetPathName()), expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("SOLIDWORKS returned the wrong assembly: " + expected);
        }

        return document;
    }

    private static List<Component2> TopLevelComponents(AssemblyDoc assembly)
    {
        Array values = assembly.GetComponents(true) as Array;
        if (values == null || values.Length == 0)
        {
            throw new InvalidOperationException("The project assembly has no top-level components.");
        }

        List<Component2> components = new List<Component2>();
        foreach (object value in values)
        {
            Component2 component = value as Component2;
            if (component != null)
            {
                components.Add(component);
            }
        }

        if (components.Count == 0)
        {
            throw new InvalidOperationException("No usable top-level project components were returned.");
        }

        return components;
    }

    private static bool IsPart(Component2 component, string expectedFileName)
    {
        string path = component.GetPathName();
        return !string.IsNullOrEmpty(path) &&
            string.Equals(Path.GetFileName(path), expectedFileName, StringComparison.OrdinalIgnoreCase);
    }

    private static double[] ReadTransform(Component2 component, string description)
    {
        MathTransform transform = component.Transform2;
        Array values = transform == null ? null : transform.ArrayData as Array;
        if (values == null || values.Length < 16)
        {
            throw new InvalidOperationException("A complete component transform is unavailable for " + description);
        }

        double[] result = new double[16];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = Convert.ToDouble(values.GetValue(index), CultureInfo.InvariantCulture);
        }

        return result;
    }

    private static void VerifyUnrotatedSource(double[] transform, Component2 component)
    {
        double[] identity = IdentityTransform(0.0, 0.0, 0.0);
        for (int index = 0; index < 9; index++)
        {
            if (Math.Abs(transform[index] - identity[index]) > 0.000001)
            {
                throw new InvalidOperationException(
                    "The current source component already has a non-identity orientation and needs explicit matrix " +
                    "composition before safe desktop rotation: " + component.Name2);
            }
        }
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
            if (!component.Select4(false, null, false))
            {
                throw new InvalidOperationException("Cannot select the fixed component before moving " + context);
            }

            assembly.UnfixComponent();
            document.ClearSelection2(true);
            if (component.IsFixed())
            {
                throw new InvalidOperationException("A fixed component could not be released for " + context);
            }
        }

        MathTransform replacement = utility.CreateTransform(requested) as MathTransform;
        if (replacement == null)
        {
            throw new InvalidOperationException("Cannot create the requested SOLIDWORKS transform for " + context);
        }

        component.Transform2 = replacement;
        double[] actual = ReadTransform(component, context);
        for (int index = 0; index < 12; index++)
        {
            if (Math.Abs(actual[index] - requested[index]) > 0.0000001)
            {
                throw new InvalidOperationException(
                    "Component transform readback mismatch in " + context + "; index=" +
                    index.ToString(CultureInfo.InvariantCulture));
            }
        }
    }

    private static MathUtility RequireMathUtility(RackCadSession cad)
    {
        MathUtility utility = cad.Application.GetMathUtility() as MathUtility;
        if (utility == null)
        {
            throw new InvalidOperationException("SOLIDWORKS did not provide its math-transform utility.");
        }

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
        cad.Log("V04_FINAL_VISIBLE_ASSEMBLY=" + stem);
    }

    private static string AssemblyPath(RackCadSession cad, string stem)
    {
        return Path.GetFullPath(Path.Combine(cad.AssembliesDirectory, stem + ".SLDASM"));
    }

    private static double Millimetres(object value)
    {
        return Convert.ToDouble(value, CultureInfo.InvariantCulture) * 1000.0;
    }

    private static void RequireClose(double actual, double expected, double tolerance, string context)
    {
        if (double.IsNaN(actual) || double.IsInfinity(actual) || Math.Abs(actual - expected) > tolerance)
        {
            throw new InvalidOperationException(
                "Geometry verification failed for " + context + "; actual=" + Format(actual) +
                "; expected=" + Format(expected) + "; tolerance=" + Format(tolerance));
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

    private sealed class Stance
    {
        internal double FaceAngleDegrees;
        internal double AngleRadians;
        internal double DetentDegrees;
        internal double DetentRadians;
        internal double HingeHeight;
        internal double HingeHorizontalOffset;
        internal double HorizontalArmReach;
        internal double SupportFootprint;
        internal double OldUpperHingeHeight;
        internal double BackRubberFootClearance;
    }

    private sealed class Point
    {
        internal double X;
        internal double Y;
        internal double Z;
    }
}
