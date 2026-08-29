using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// V0.11 is a new-file-only mechanical release candidate.  It repairs the
// CAD-verifiable weaknesses found during the final V0.10 audit: unsupported
// assembly transforms, V7 latch-envelope mismatch/interference, an
// unmanufacturable variable-section rail concept (replaced by a constant
// section custom extrusion with separately replaceable structural inserts),
// unretained stand pins,
// insufficient bidirectional desktop support, a non-continuous lid, a fused
// rear-skin/doubler manufacturing ambiguity, and missing explicit fastener
// load paths.  It never saves an older V03..V10 native document.
internal static class BuildRackMechanicalReleaseV11
{
    private const string Version = "V0.11";

    private const string OldSide = "SideFrame_V09_SecureLidInner";
    private const string OldBackLeg = "SideKickstand_V07_185mm_8x28";
    private const string OldOuterCheek = "KickstandOuterCheek_V07_4mm";
    private const string OldPivot = "KickstandPivotPin_V07_10mm";
    private const string OldSpacer = "KickstandSpacer_V07_8p8mm_M5";
    private const string OldStop = "KickstandLoadStopPin_V07_10mm";
    private const string OldLock = "KickstandCaptiveIndexPin_V07_8mm";
    private const string OldHeel = "KickstandHeelInsert_V07";
    private const string OldFoot = "KickstandFootPad_V07_Rubber";
    private const string OldHandleSpreader = "CarryHandleSpreader_V07_6061_4mm";
    private const string OldRail = "Rail_104HP_V07_ClosedTube_EndBoss";
    private const string OldRailEndBlock = "RailEndBlock_M3";
    private const string OldThreadStrip = "ThreadStrip_104HP_M3_AISI304_V04";
    private const string OldUpperEdge = "UpperEdge_V04_Adapter_MIDI_Handle_Audio_ClearPower";
    private const string OldLowerEdge = "LowerEdge_V03_HiddenVent";
    private const string OldHandle = "RearCarryHandle_V03_ClearanceFit";
    private const string OldBack = "BackPanel_V10_5052_RymoviaPhaseHalo";
    private const string OldBackFeet = "FourBackFeet_V03";
    private const string OldLid = "DeepTravelLid_V09_Rymovia_Secure4Point";
    private const string OldVesaBridge = "VesaBridge_6061_V04_DirectMount";
    private const string OldVesaStile = "VesaStile_6061";
    private const string OldRearCrossbeam = "RearCrossBeam_6061";

    private static readonly string[] OldLatchPacks =
    {
        "LidLatchCaseBridgePack_V09_6061",
        "LidLatchKeeperPack_V09_Stainless",
        "LidLatchDoublerPack_V09_5052",
        "LidLatchBodyPack_V09_Black",
        "LidLatchBailPack_V09_Stainless",
        "LidLatchLockedIndicatorPack_V09_Red",
        "LidCompressionPadPack_V09_EPDM",
        "LidLatchCaseFastenerPack_V09_A4_M3",
        "LidLatchLidFastenerPack_V09_A4_M3"
    };

    private static readonly string[] ObsoleteV11PrototypeStems =
    {
        "RailFastenerPack_V11_12xM3_12xM4",
        "RailLocatorEndBlock_V11_M3",
        "RailStructuralInsertPack_V11_12x_M4",
        "Rail_104HP_V11_SolidSpineDualFix",
        "Rail_104HP_V11_UniformClosedExtrusion",
        "Tiptop_ZRails_104HP_SL_V11_COTS_Proxy",
        "Tiptop_ZRails_104HP_V11_EndSpacer_6p84",
        "Tiptop_ZRails_104HP_V11_MS_ThreadStrip_COTS_Proxy",
        "Tiptop_ZRails_104HP_V11_RailFastenerPack_12xM4",
        "StandOuterCheek_V11_3mm_LocalBoss",
        "RearKickstand_V11_240mm_Bushed",
        "FrontAntiTipLink_V11_17_4PH"
    };

    private const string SideStem = "SideFrame_V11_ReleaseDoubleShear";
    private const string BackLegStem = "RearKickstand_V11_262mm_Bushed";
    private const string OuterCheekStem = "StandOuterCheek_V11_4mm_FullPlate";
    private const string BackBushingStem = "RearKickstandBushing_V11_G1SM_1012_08";
    private const string BackPivotStem = "RearKickstandPivotAxle_V11_Retained10mm";
    private const string BackStopStem = "RearKickstandStopAxle_V11_Retained10mm";
    private const string SpacerStem = "StandCheekSpacer_V11_8p8mm_M5";
    private const string CheekFastenerPackStem = "StandCheekFastenerPack_V11_M5";
    private const string BackLockPackStem = "RearKickstandLockScrewPack_V11_M6_FlushCaptive";
    private const string BackHeelStem = "RearKickstandHeel_V11_17_4PH_Keyed";
    private const string BackHeelPinStem = "RearKickstandHeelPin_V11_M4";
    private const string BackFootStem = "RearKickstandFoot_V11_OvermouldCaptured";
    private const string RailStem = "RymoviaRail_104HP_V11_ClosedTubeDualFix";
    private const string RailThreadStripStem = "RymoviaRail_104HP_V11_AISI304_ThreadStrip";
    private const string RailInsertPackStem = "RymoviaRailEndInsertPack_V11_12x7075_M4";
    private const string RailFastenerPackStem = "RymoviaRailFastenerPack_V11_12xM3_12xM4";
    private const string UpperEdgeStem = "UpperEdge_V11_P9_Handle_ShellJoints";
    private const string LowerEdgeStem = "LowerEdge_V11_Vented_AntiTipMounts";
    private const string HandleStem = "CarryHandle_V11_SouthcoP9_128";
    private const string HandleSpreaderStem = "CarryHandleSpreader_V11_P9_128";
    private const string HandleFastenerPackStem = "CarryHandleFastenerPack_V11_2xM4";
    private const string CassetteFastenerPackStem = "UpperCassetteFastenerPack_V11_12xM3";
    private const string ShellCornerFastenerPackStem = "ShellCornerFastenerPack_V11_8xM4";
    private const string LidStem = "DeepTravelLid_V11_WeldedContinuous_SouthcoV7";
    private const string CaseLatchPackStem = "LidLatchCasePack_V11_SouthcoV7";
    private const string CaseLatchFastenerPackStem = "LidLatchCaseFastenerPack_V11_8xM3";
    private const string LidLatchDoublerPackStem = "LidLatchDoublerPack_V11_4x5052";
    private const string LidLatchBodyPackStem = "LidLatchBodyPack_V11_SouthcoV7_31x72";
    private const string LidLatchBailPackStem = "LidLatchBailPack_V11_ClosedState";
    private const string LidLatchFastenerPackStem = "LidLatchFastenerPack_V11_16xM3";
    private const string LidCompressionPackStem = "LidCompressionStopPack_V11_EPDM_MetalStop";
    private const string BackSkinStem = "BackSkin_V11_5052_RymoviaPhaseHalo";
    private const string BackDoublerStem = "BackVesaDoubler_V11_5052_Separate";
    private const string BackPerimeterFastenerPackStem = "BackPerimeterFastenerPack_V11_12xM4";
    private const string BackFeetPackStem = "BackFeetPack_V11_4xCapturedEPDM";
    private const string BackFeetFastenerPackStem = "BackFeetFastenerPack_V11_4xM4";
    private const string VesaFrameStem = "VesaLoadFrame_V11_OnePiece6061";
    private const string VesaFastenerPackStem = "VesaFastenerPack_V11_4xM4_4xSideM4";
    private const string FrontBracketPackStem = "FrontAntiTipBracketPack_V11_DoubleShear";
    private const string FrontLinkStem = "FrontAntiTipLink_V11_124mm_17_4PH";
    private const string FrontFootStem = "FrontAntiTipFoot_V11_OvermouldCaptured";
    private const string FrontPivotPackStem = "FrontAntiTipPivotPack_V11_Retained8mm";
    private const string FrontStopPackStem = "FrontAntiTipStopPack_V11_Retained8mm";
    private const string FrontLockPackStem = "FrontAntiTipLockPack_V11_M6_FlushCaptive";
    private const string FrontBracketFastenerPackStem = "FrontAntiTipBracketFastenerPack_V11_8xM4";
    private const string UpperMidiStem = "UpperMidiUsb_V04_3xDIN_USB_C_Inline";
    private const string UpperAudioStem = "UpperAudio_V04_2x4_TRS635";

    private const string IdentityShowcaseStem =
        "Rack4Modules_ExteriorIdentityShowcase_V11_MechanicalRelease";

    private const double CaseWidth = 548.0;
    private const double CaseHeight = 420.0;
    private const double CaseDepth = 110.0;
    private const double ShellThickness = 2.0;
    private const double SideCoreThickness = 3.0;
    private const double SideLoadThickness = 4.0;
    private const double SideCentreX = 273.0;
    private const double LegPlaneX = 279.4;
    private const double OuterCheekCentreX = 285.8;
    private const double OuterCheekBaseThickness = 4.0;
    private const double OuterBossCentreX = 285.8;
    private const double OuterBossThickness = 4.0;
    private const double CavityWidth = 8.8;
    private const double BackLegThickness = 8.0;
    private const double BackLegWidth = 26.0;
    private const double HingeCaseY = -129.0;
    private const double HingeCaseZ = 52.0;
    private const double HingeLocalY = -75.0;
    private const double HingeLocalZ = 6.0;
    private const double FoldedBackLegOriginY = -54.0;
    private const double FoldedBackLegOriginZ = 46.0;
    private const double BackPivotToFoot = 262.0;
    private const double BackFootLocalY = 187.0;
    private const double BackFootLocalZ = 6.0;
    private const double RearFootDeskCentreHeight = 13.0;
    private const double BackFootDiameter = 26.0;
    private const double BackFootRadius = 13.0;
    private const double BackFootAxialLength = 8.4;
    private const double BackPivotHole = 12.2;
    private const double AxleClearance10 = 10.2;
    private const double AxleDiameter10 = 9.8;
    private const double AxleGrip = 16.8;
    private const double StopLocalY = -21.0;
    private const double StopLocalZ = -38.0;
    private const double LockDeployLocalY = 18.0;
    private const double LockDeployLocalZ = -9.0;
    private const double HeelLocalY = -105.0;
    private const double HeelLocalZ = -32.0;
    private const double ShellContactY = -210.0;
    private const double ShellContactZ = 110.0;

    private const double FrontPivotCaseY = -202.5;
    private const double FrontPivotCaseZ = 85.0;
    private const double FrontFootDeployedCaseY = -272.126716368744;
    private const double FrontFootDeployedCaseZ = -17.6066292580863;
    private const double FrontFootFoldedCaseY = -224.032374030699;
    private const double FrontFootFoldedCaseZ = -37.1161613735138;
    private const double FrontLinkLength = 124.0;
    private const double FrontFootDiameter = 20.0;
    private const double FrontFootRadius = 10.0;
    private const double FrontFootDeskCentreHeight = 10.0;
    private const double FrontFootPlaneX = 205.0;
    private const double FrontBracketCheekOffsetX = 6.4;
    private const double FrontBracketMountOffsetX = 18.0;
    // Balanced retained-stop vector: r=12.5 mm at -131.5 degrees.  This
    // leaves nominal material at all four critical interfaces instead of
    // merely touching the old 3/4 mm analytic limits.
    private const double FrontStopCaseY = -210.78275060269672;
    private const double FrontStopCaseZ = 75.63805349013748;
    // Keep the common folded/deployed lock screw behind the 85 mm bottom-row
    // module envelope. The previous y=-199.5 position made the unused
    // deployed-hole reinforcement island cross the envelope when folded.
    private const double FrontLockCaseY = -211.0;
    private const double FrontLockCaseZ = 63.5;
    private const double FrontLockIslandRadius = 7.5;
    // CreateBodyFromBox3 uses z as the base-face coordinate.  Keep this
    // swept-stop reinforcement below the module-facing side of the folded
    // link: z=-14..+10, not the previous accidental z=-2..+22 projection.
    // It must not be extended down to z=-22: that apparently simple mass
    // restoration crosses the desk plane in the deployed 60-degree state.
    private const double FrontSweptIslandBaseZ = -14.0;
    private const double FrontSweptIslandDepth = 24.0;
    // A rounded monolithic mid-span rib restores the effective material that
    // was lost when the misplaced swept island was moved away from modules.
    // It sits on the safe side of both the folded module envelope and desk.
    private const double FrontLinkRibLeftCentreY = 33.0;
    private const double FrontLinkRibRightCentreY = 98.0;
    private const double FrontLinkRibCentreZ = 10.0;
    private const double FrontLinkRibRadius = 2.75;
    private const double FrontLinkRibBoxBaseZ = 9.8;
    private const double FrontLinkRibBoxDepth = 2.5;
    private const double BottomModuleEnvelopeMinY = -189.35;
    private const double MidiCassetteCentreX = -120.0;
    private const double AudioCassetteCentreX = 173.0;
    private const double HandleLeftMountX = -60.0;
    private const double HandleRightMountX = 68.0;

    private const double LatchCentreY = 194.0;
    private const double LatchHalfMountPitch = 6.0;
    private const double LatchCaseMountZ = 24.0;
    private const double LatchBodyLowerRowZ = -18.0;
    private const double LatchBodyUpperRowZ = -6.0;
    private const double LatchBodyWidth = 31.0;
    private const double LatchBodyLength = 72.0;
    private const double LatchBodyMinZ = -58.0;
    private const double LatchBodyMaxZ = 14.0;
    private const double LatchMountHole = 3.4;
    private const double LatchShaft = 2.8;
    private const double KeeperBarDiameter = 4.0;
    private const double BailWireDiameter = 2.5;
    private const double KeeperBarZ = 29.0;
    private const double BailContactZ = 32.25;

    private const double LidThickness = 1.2;
    private const double LidFaceZ = -70.0;
    private const double LidSkirtDepth = 82.0;
    private const double LidReliefMinY = -170.0;
    private const double LidReliefMaxY = 130.0;
    private const double LidReliefMinZ = -2.0;
    private const double LidReliefMaxZ = 15.0;
    private const double OverallBossWidth = 575.6;

    private const double BackSkinThickness = 1.5;
    private const double BackDoublerThickness = 0.5;
    private const double BackDoublerSize = 160.0;
    private const double VesaClearanceHole = 4.5;

    private const double RailVisibleLength = 528.32;
    private const double RailStructuralLength = 542.0;
    private const double RailHeight = 10.0;
    private const double RailFrontDepth = 12.0;
    private const double RailSpineDepth = 10.0;
    private const double RailTubeWall = 1.5;
    private const double RailOverallDepth = RailFrontDepth + RailSpineDepth;
    private const double RailM3AxisDepth = 9.0;
    private const double RailM4AxisDepth = 17.0;
    private const double RailInsertLength = 24.0;
    private const double RailInsertWidth = 6.8;
    private const double RailInsertDepth = 6.8;
    private const double RailPitch = 5.08;
    private const int RailHoleCount = 104;
    private const double RailModuleClearanceDiameter = 3.2;
    private const double RailThreadPocketStart = 3.9;
    private const double RailThreadPocketDepth = 2.2;
    private const double RailThreadPocketWidth = 6.4;
    private const double RailThreadStripWidth = 6.0;
    private const double RailThreadStripThickness = 2.0;
    private const double RailThreadMinorDiameter = 2.5;
    private const double RailCommercialProxyIy = 3157.406396;
    private const double RailCommercialProxyIz = 1133.112664;

    private const double TiltAngleDegrees = 60.0;
    private const double GeometryTolerance = 0.12;
    private const double TransformTolerance = 0.0000001;
    private const int PreviewDecalMaskAlpha = 3;

    private static readonly double[] NaturalAluminium = { 0.72, 0.74, 0.76 };
    private static readonly double[] Graphite = { 0.067, 0.067, 0.067 };
    private static readonly double[] DarkAluminium = { 0.11, 0.12, 0.14 };
    private static readonly double[] Stainless = { 0.62, 0.65, 0.67 };
    private static readonly double[] RubberBlack = { 0.03, 0.035, 0.04 };
    private static readonly double[] RymoviaRed = { 1.0, 0.055, 0.035 };

    private static readonly MountPoint[] CheekSpacers =
    {
        new MountPoint(-190.0, 47.0), new MountPoint(-170.0, 68.0),
        new MountPoint(-60.0, 30.0), new MountPoint(0.0, 30.0),
        new MountPoint(60.0, 30.0), new MountPoint(120.0, 74.0)
    };

    private static readonly double[] FrontBracketMountZ = { 48.0, 104.0 };

    private static readonly double[] SideVentY = { 142.0, 164.0, 186.0 };
    private static readonly double[] SideVentZ = { 86.0, 92.0 };
    private static readonly double[] BackFastenerX = { -245.0, -150.0, -50.0, 50.0, 150.0, 245.0 };

    private static readonly AssemblySpec[] Assemblies =
    {
        new AssemblySpec("Rack4Modules_OpenCase_V10_RymoviaPhaseHaloRear",
            "Rack4Modules_OpenCase_V11_MechanicalRelease", false, false, false),
        new AssemblySpec("Rack4Modules_TransportClosed_V10_RymoviaPhaseHaloRear",
            "Rack4Modules_TransportClosed_V11_MechanicalRelease", false, true, false),
        new AssemblySpec("Rack4Modules_ClearanceCheck_V10_RymoviaPhaseHaloRear",
            "Rack4Modules_ClearanceCheck_V11_MechanicalRelease", false, false, false),
        new AssemblySpec("Rack4Modules_DesktopTilt60_V10_RymoviaPhaseHaloRear",
            "Rack4Modules_DesktopTilt60_V11_MechanicalRelease", true, false, false),
        new AssemblySpec("Rack4Modules_ShowcaseTilt60_LidOff_V10_RymoviaPhaseHaloRear",
            "Rack4Modules_ShowcaseTilt60_LidOff_V11_MechanicalRelease", true, true, true)
    };

    [STAThread]
    private static int Main(string[] args)
    {
        string progress = "start";
        try
        {
            if (args == null || args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
                throw new ArgumentException("Usage: BuildRackMechanicalReleaseV11.exe <Rack4Modules root>");

            RackCadSession cad = new RackCadSession(Path.GetFullPath(args[0]));
            progress = "preflight";
            RequireProjectInputs(cad);
            List<SourceStamp> protectedSources = CaptureProtectedCad(cad);
            GuardV11Targets(cad);
            ClosePathlessGeneratedAssemblies(cad);
            // A previous successful release run intentionally leaves the final
            // V11 showcase open. Close only our exact clean generated targets
            // before mass readback/rebuild so referenced parts are not held by
            // that assembly. EnsureTargetClosed refuses any dirty document.
            progress = "close clean generated targets for rerun";
            foreach(AssemblySpec existing in Assemblies)
                EnsureTargetClosed(cad,AssemblyPath(cad,existing.TargetStem));
            EnsureTargetClosed(cad,AssemblyPath(cad,IdentityShowcaseStem));
            Stance stance = CalculateBackStance(TiltAngleDegrees);
            ValidateAnalyticDesign(stance);

            progress = "parts";
            PartPaths parts = BuildParts(cad, stance);

            string debugStart=System.Environment.GetEnvironmentVariable("RACK_V11_START_ASSEMBLY")??string.Empty;
            bool startReached=string.IsNullOrWhiteSpace(debugStart);
            foreach (AssemblySpec spec in Assemblies)
            {
                if(!startReached)
                {
                    startReached=string.Equals(debugStart,spec.TargetStem,
                        StringComparison.OrdinalIgnoreCase);
                    if(!startReached)
                    {
                        cad.Log("V11_DEBUG_SKIP_ASSEMBLY="+spec.TargetStem);
                        continue;
                    }
                }
                progress = "assembly " + spec.TargetStem;
                BuildProductAssembly(cad, spec, stance, parts);
            }
            Require(startReached,"RACK_V11_START_ASSEMBLY did not match a target: "+debugStart);

            progress = "identity showcase";
            BuildIdentityShowcase(cad, parts);
            progress = "source guard";
            VerifyProtectedCad(protectedSources);
            progress = "close generated documents";
            CloseCleanV11DocumentsExcept(cad, IdentityShowcaseStem);
            progress = "final display";
            OpenFinalShowcase(cad);

            cad.Log("V11_MECHANICAL_RELEASE_BUILD_COMPLETE=true");
            cad.Log("V11_OLD_NATIVE_DOCUMENTS_SAVED=false");
            cad.Log("V11_DESKTOP_SUPPORT=rear_262mm_plus_front_124mm_true_contact_four_point");
            cad.Log("V11_LATCH=Southco_V7_small_31x72_12x12_30mm_separation_envelope");
            cad.Log("V11_HANDLE=Southco_P9_128mm_2xM4_envelope");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("V11_MECHANICAL_RELEASE_BUILD_FAILED=" +
                ex.GetType().FullName + ": " + ex.Message + " @ " + progress);
            return 1;
        }
    }

    private static PartPaths BuildParts(RackCadSession cad, Stance stance)
    {
        PartPaths p = new PartPaths();
        p.Side = PartOrBuild(cad, SideStem, () => CreateSideFrame(cad, stance));
        p.BackLeg = PartOrBuild(cad, BackLegStem, () => CreateBackLeg(cad, stance));
        p.OuterCheek = PartOrBuild(cad, OuterCheekStem, () => CreateOuterCheek(cad, stance));
        p.BackBushing = PartOrBuild(cad, BackBushingStem, () => CreateBackBushing(cad));
        p.BackPivot = PartOrBuild(cad, BackPivotStem, () => CreateRetainedAxle(cad,
            BackPivotStem, AxleDiameter10, AxleGrip, 11.8, "rear kickstand pivot"));
        p.BackStop = PartOrBuild(cad, BackStopStem, () => CreateRetainedAxle(cad,
            BackStopStem, AxleDiameter10, AxleGrip, 11.8, "rear kickstand hard stop"));
        p.Spacer = PartOrBuild(cad, SpacerStem, () => CreateSpacer(cad));
        p.CheekFasteners = PartOrBuild(cad, CheekFastenerPackStem, () => CreateCheekFastenerPack(cad));
        p.BackLockPack = PartOrBuild(cad, BackLockPackStem, () => CreateBackLockPack(cad, stance));
        p.BackHeel = PartOrBuild(cad, BackHeelStem, () => CreateBackHeel(cad));
        p.BackHeelPin = PartOrBuild(cad, BackHeelPinStem, () => CreateBackHeelPin(cad));
        p.BackFoot = PartOrBuild(cad, BackFootStem, () => CreateBackFoot(cad));
        p.RailThreadStrip = PartOrBuild(cad, RailThreadStripStem,
            () => CreateCustomThreadStrip(cad));
        p.Rail = PartOrBuild(cad, RailStem, () => CreateCustomRail(cad));
        p.RailInsertPack = PartOrBuild(cad, RailInsertPackStem,
            () => CreateRailInsertPack(cad));
        p.RailFastenerPack = PartOrBuild(cad, RailFastenerPackStem, () => CreateRailFastenerPack(cad));
        double railUnitMass=ReadSavedPartMass(cad,p.Rail)+
            ReadSavedPartMass(cad,p.RailThreadStrip);
        double railSystemMass=6.0*railUnitMass+
            ReadSavedPartMass(cad,p.RailInsertPack)+
            ReadSavedPartMass(cad,p.RailFastenerPack);
        Require(railUnitMass>=0.245 && railUnitMass<=0.255,
            "Custom rail plus threaded strip mass is outside the verified design range; actual="+
            F(railUnitMass));
        Require(railSystemMass>=1.50 && railSystemMass<=1.60,
            "Complete six-rail custom subsystem mass is outside the verified design range; actual="+
            F(railSystemMass));
        cad.Log("V11_RYMOVIA_CUSTOM_RAIL_UNIT_WITH_STRIP_MASS_KG="+F(railUnitMass)+
            ";six_rail_complete_subsystem_mass_kg="+F(railSystemMass)+
            ";manufacturing=CONSTANT_EXTRUSION_PLUS_REPLACEABLE_END_INSERTS");
        p.UpperEdge = PartOrBuild(cad, UpperEdgeStem, () => CreateUpperEdge(cad));
        p.LowerEdge = PartOrBuild(cad, LowerEdgeStem, () => CreateLowerEdge(cad));
        p.Handle = PartOrBuild(cad, HandleStem, () => CreateHandle(cad));
        p.HandleSpreader = PartOrBuild(cad, HandleSpreaderStem, () => CreateHandleSpreader(cad));
        p.HandleFasteners = PartOrBuild(cad, HandleFastenerPackStem, () => CreateHandleFastenerPack(cad));
        p.CassetteFasteners = PartOrBuild(cad, CassetteFastenerPackStem, () => CreateCassetteFastenerPack(cad));
        p.ShellCornerFasteners = PartOrBuild(cad, ShellCornerFastenerPackStem, () => CreateShellCornerFastenerPack(cad));
        p.Lid = PartOrBuild(cad, LidStem, () => CreateContinuousLid(cad));
        p.CaseLatchPack = PartOrBuild(cad, CaseLatchPackStem, () => CreateCaseLatchPack(cad));
        p.CaseLatchFasteners = PartOrBuild(cad, CaseLatchFastenerPackStem, () => CreateCaseLatchFastenerPack(cad));
        p.LidLatchDoublers = PartOrBuild(cad, LidLatchDoublerPackStem, () => CreateLidLatchDoublerPack(cad));
        p.LidLatchBodies = PartOrBuild(cad, LidLatchBodyPackStem, () => CreateLidLatchBodyPack(cad));
        p.LidLatchBails = PartOrBuild(cad, LidLatchBailPackStem, () => CreateLidLatchBailPack(cad));
        p.LidLatchFasteners = PartOrBuild(cad, LidLatchFastenerPackStem, () => CreateLidLatchFastenerPack(cad));
        p.LidCompression = PartOrBuild(cad, LidCompressionPackStem, () => CreateLidCompressionPack(cad));
        p.BackSkin = PartOrBuild(cad, BackSkinStem, () => CreateBackSkin(cad));
        p.BackDoubler = PartOrBuild(cad, BackDoublerStem, () => CreateBackDoubler(cad));
        p.BackPerimeterFasteners = PartOrBuild(cad, BackPerimeterFastenerPackStem, () => CreateBackPerimeterFastenerPack(cad));
        p.BackFeet = PartOrBuild(cad, BackFeetPackStem, () => CreateBackFeetPack(cad));
        p.BackFeetFasteners = PartOrBuild(cad, BackFeetFastenerPackStem, () => CreateBackFeetFastenerPack(cad));
        p.VesaFrame = PartOrBuild(cad, VesaFrameStem, () => CreateVesaFrame(cad));
        p.VesaFasteners = PartOrBuild(cad, VesaFastenerPackStem, () => CreateVesaFastenerPack(cad));
        p.FrontBracketPack = PartOrBuild(cad, FrontBracketPackStem, () => CreateFrontBracketPack(cad));
        p.FrontLink = PartOrBuild(cad, FrontLinkStem, () => CreateFrontLink(cad));
        p.FrontFoot = PartOrBuild(cad, FrontFootStem, () => CreateFrontFoot(cad));
        p.FrontPivotPack = PartOrBuild(cad, FrontPivotPackStem, () => CreateFrontPivotPack(cad));
        p.FrontStopPack = PartOrBuild(cad, FrontStopPackStem, () => CreateFrontStopPack(cad));
        p.FrontLockPack = PartOrBuild(cad, FrontLockPackStem, () => CreateFrontLockPack(cad));
        p.FrontBracketFasteners = PartOrBuild(cad, FrontBracketFastenerPackStem, () => CreateFrontBracketFastenerPack(cad));
        return p;
    }

    private static string PartOrBuild(RackCadSession cad,string stem,Func<string> builder)
    {
        string path=PartPath(cad,stem);
        bool force=IsForcedStem(stem);
        if(string.Equals(System.Environment.GetEnvironmentVariable("RACK_V11_RESUME"),"1",
            StringComparison.Ordinal)&&!force&&File.Exists(path)&&new FileInfo(path).Length>0)
        {
            cad.Log("V11_RESUME_EXISTING_PART="+Path.GetFileName(path));
            return path;
        }
        if(force)
        {
            EnsureTargetClosed(cad,path);
            cad.Log("V11_FORCE_REBUILD_PART="+Path.GetFileName(path));
        }
        return builder();
    }

    private static bool IsForcedStem(string stem)
    {
        string forced=System.Environment.GetEnvironmentVariable("RACK_V11_FORCE_STEMS")??
            string.Empty;
        return forced.Split(new[]{',',';'},StringSplitOptions.RemoveEmptyEntries)
            .Any(x=>string.Equals(x.Trim(),stem,StringComparison.OrdinalIgnoreCase));
    }

    private static void RequireProjectInputs(RackCadSession cad)
    {
        foreach (AssemblySpec spec in Assemblies)
            RequireFile(AssemblyPath(cad, spec.SourceStem));
        foreach (string stem in new[]
        {
            OldSide, OldBackLeg, OldOuterCheek, OldPivot, OldSpacer, OldStop,
            OldLock, OldHeel, OldFoot, OldHandleSpreader, OldRail,
            OldRailEndBlock, OldThreadStrip, OldUpperEdge, OldLowerEdge, OldHandle, OldBack,
            OldBackFeet, OldLid, OldVesaBridge, OldVesaStile, OldRearCrossbeam
        }) RequireFile(PartPath(cad, stem));
        foreach (string stem in OldLatchPacks) RequireFile(PartPath(cad, stem));
        RequireFile(Path.Combine(cad.Root, "logo", "logo-mark-white.png"));
        RequireFile(Path.Combine(cad.Root, "logo", "logo-lockup-white.png"));
        RequireFile(Path.Combine(cad.Root, "logo", "rymovia-timegrid-v09.png"));
        RequireFile(Path.Combine(cad.Root, "logo", "rymovia-phase-halo-rear-v10.png"));
    }

    private static List<SourceStamp> CaptureProtectedCad(RackCadSession cad)
    {
        List<SourceStamp> result = new List<SourceStamp>();
        foreach (string path in Directory.GetFiles(Path.Combine(cad.Root, "cad"), "*.*",
            SearchOption.AllDirectories).Where(IsNativeCad)
            .Where(p => !Path.GetFileName(p).StartsWith("~$", StringComparison.Ordinal))
            .Where(p => Path.GetFileNameWithoutExtension(p)
                .IndexOf("_V11_", StringComparison.OrdinalIgnoreCase) < 0))
        {
            FileInfo info = new FileInfo(path);
            string hash = null;
            try { hash = Hash(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            result.Add(new SourceStamp(path, info.Length, info.LastWriteTimeUtc, hash));
        }
        Require(result.Count > 40, "Protected CAD set is unexpectedly small");
        cad.Log("V11_PROTECTED_NATIVE_SOURCE_COUNT=" + result.Count);
        return result;
    }

    private static void VerifyProtectedCad(IEnumerable<SourceStamp> sources)
    {
        foreach (SourceStamp source in sources)
        {
            FileInfo info = new FileInfo(source.Path);
            Require(info.Exists, "Protected source disappeared: " + source.Path);
            Require(info.Length == source.Length && info.LastWriteTimeUtc == source.LastWriteUtc,
                "Protected source metadata changed: " + source.Path);
            if (source.Hash != null)
            {
                try { Require(string.Equals(Hash(source.Path), source.Hash, StringComparison.Ordinal),
                    "Protected source bytes changed: " + source.Path); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    private static void GuardV11Targets(RackCadSession cad)
    {
        HashSet<string> targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string stem in V11PartStems()) targets.Add(PartPath(cad, stem));
        foreach (string stem in ObsoleteV11PrototypeStems)
            targets.Add(PartPath(cad, stem));
        foreach (AssemblySpec spec in Assemblies) targets.Add(AssemblyPath(cad, spec.TargetStem));
        targets.Add(AssemblyPath(cad, IdentityShowcaseStem));

        ModelDoc2 doc = cad.Application.GetFirstDocument() as ModelDoc2;
        while (doc != null)
        {
            ModelDoc2 next = doc.GetNext() as ModelDoc2;
            string path = doc.GetPathName();
            if (!string.IsNullOrWhiteSpace(path) && targets.Contains(Path.GetFullPath(path)))
            {
                if(doc.GetSaveFlag()) cad.Log("V11_DISCARD_REGENERABLE_DIRTY_TARGET="+path);
                cad.Application.CloseDoc(doc.GetTitle());
            }
            doc = next;
        }
    }

    private static IEnumerable<string> V11PartStems()
    {
        return typeof(BuildRackMechanicalReleaseV11)
            .GetFields(System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string) &&
                f.Name.EndsWith("Stem", StringComparison.Ordinal) &&
                f.Name != "IdentityShowcaseStem")
            .Select(f => f.GetValue(null) as string)
            .Where(v => !string.IsNullOrWhiteSpace(v) &&
                v.IndexOf("_V11_", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool IsNativeCad(string path)
    {
        string ext = Path.GetExtension(path);
        return string.Equals(ext, ".SLDPRT", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ext, ".SLDASM", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateAnalyticDesign(Stance stance)
    {
        ValidateCustomRailScreening();
        RequireClose(BackFootLocalY - HingeLocalY, BackPivotToFoot, 0.001,
            "rear pivot-to-foot distance");
        RequireClose(RearFootDeskCentreHeight,BackFootRadius,0.001,
            "rear pad centre height must equal its crown radius");
        RequireClose(FrontFootDeskCentreHeight,FrontFootRadius,0.001,
            "front pad centre height must equal its crown radius");
        Require(stance.SupportFootprint > 237.5 && stance.SupportFootprint < 238.0,
            "262 mm rear support must produce approximately 237.7 mm footprint");
        double frontWorldY, frontWorldZ;
        CasePointToDesk(FrontFootDeployedCaseY, FrontFootDeployedCaseZ,
            stance, out frontWorldY, out frontWorldZ);
        RequireClose(frontWorldY, FrontFootDeskCentreHeight, 0.01,
            "front anti-tip pad centre height");
        Require(frontWorldZ < -141.2 && frontWorldZ > -141.9,
            "124 mm front anti-tip link must produce approximately -141.57 mm support coordinate");
        double frontDistance = Distance(FrontPivotCaseY, FrontPivotCaseZ,
            FrontFootDeployedCaseY, FrontFootDeployedCaseZ);
        double foldedDistance = Distance(FrontPivotCaseY, FrontPivotCaseZ,
            FrontFootFoldedCaseY, FrontFootFoldedCaseZ);
        RequireClose(frontDistance, FrontLinkLength, 0.05, "front deployed link length");
        RequireClose(foldedDistance, FrontLinkLength, 0.25, "front folded link length");
        Require(FrontFoldedAngle() < Degrees(-99.0) &&
            FrontFoldedAngle() > Degrees(-101.0),
            "front folded link must remain on the -100 degree rail-clearance trajectory");
        Require(FrontPivotCaseY + 12.0 <= -190.0,
            "front pivot root must remain below the bottom-row module clearance envelope");
        Point deployedLockLocal=FrontLockDeployLocalPoint();
        Point foldedLockLocal=InverseRotate(FrontLockCaseY-FrontPivotCaseY,
            FrontLockCaseZ-FrontPivotCaseZ,FrontFoldedAngle());
        Point unusedDeployIslandFolded=RotateFromPivot(FrontPivotCaseY,
            FrontPivotCaseZ,deployedLockLocal.Y,deployedLockLocal.Z,
            FrontFoldedAngle());
        Point engagedFoldedIsland=RotateFromPivot(FrontPivotCaseY,
            FrontPivotCaseZ,foldedLockLocal.Y,foldedLockLocal.Z,
            FrontFoldedAngle());
        double foldedLockClearance=Math.Min(
            BottomModuleEnvelopeMinY-(unusedDeployIslandFolded.Y+FrontLockIslandRadius),
            BottomModuleEnvelopeMinY-(engagedFoldedIsland.Y+FrontLockIslandRadius));
        Require(foldedLockClearance>=4.5,
            "both folded front-link lock islands require at least 4.5 mm nominal module-envelope clearance; actual="+
            F(foldedLockClearance));
        double foldedPivotBossClearance=BottomModuleEnvelopeMinY-
            (FrontPivotCaseY+12.0);
        Require(foldedPivotBossClearance>=1.0,
            "folded front-link 24 mm pivot boss requires at least 1.0 mm nominal module-envelope clearance; actual="+
            F(foldedPivotBossClearance));
        double foldedSweptIslandMaxY=double.NegativeInfinity;
        foreach(double localY in new[]{0.0,22.0})
            foreach(double localZ in new[]{FrontSweptIslandBaseZ,
                FrontSweptIslandBaseZ+FrontSweptIslandDepth})
                foldedSweptIslandMaxY=Math.Max(foldedSweptIslandMaxY,
                    RotateFromPivot(FrontPivotCaseY,FrontPivotCaseZ,
                        localY,localZ,FrontFoldedAngle()).Y);
        double foldedSweptIslandClearance=BottomModuleEnvelopeMinY-
            foldedSweptIslandMaxY;
        Require(foldedSweptIslandClearance>=3.0,
            "folded front-link swept-stop reinforcement requires at least 3.0 mm nominal module-envelope clearance; actual="+
            F(foldedSweptIslandClearance));
        double[] deployedFrontLinkTransform=FrontLinkTransform(stance,1,
            FrontDeployedAngle());
        double sweptIslandDeskClearance=double.PositiveInfinity;
        foreach(double localY in new[]{0.0,22.0})
            foreach(double localZ in new[]{FrontSweptIslandBaseZ,
                FrontSweptIslandBaseZ+FrontSweptIslandDepth})
                sweptIslandDeskClearance=Math.Min(sweptIslandDeskClearance,
                    ApplyTransform(deployedFrontLinkTransform,0,localY,localZ).Y);
        Require(sweptIslandDeskClearance>=3.0,
            "deployed front-link swept-stop reinforcement requires at least 3.0 mm desk clearance; actual="+
            F(sweptIslandDeskClearance));
        double foldedRibMaxY=double.NegativeInfinity;
        foreach(double localY in new[]{FrontLinkRibLeftCentreY,
            FrontLinkRibRightCentreY})
        {
            Point ribCentre=RotateFromPivot(FrontPivotCaseY,FrontPivotCaseZ,
                localY,FrontLinkRibCentreZ,FrontFoldedAngle());
            foldedRibMaxY=Math.Max(foldedRibMaxY,ribCentre.Y+FrontLinkRibRadius);
        }
        foreach(double localY in new[]{FrontLinkRibLeftCentreY,
            FrontLinkRibRightCentreY})
            foreach(double localZ in new[]{FrontLinkRibBoxBaseZ,
                FrontLinkRibBoxBaseZ+FrontLinkRibBoxDepth})
                foldedRibMaxY=Math.Max(foldedRibMaxY,
                    RotateFromPivot(FrontPivotCaseY,FrontPivotCaseZ,
                        localY,localZ,FrontFoldedAngle()).Y);
        double foldedRibClearance=BottomModuleEnvelopeMinY-foldedRibMaxY;
        Require(foldedRibClearance>=4.0,
            "folded rounded front-link mid-span rib requires at least 4.0 mm module-envelope clearance; actual="+
            F(foldedRibClearance));
        double ribDeskClearance=double.PositiveInfinity;
        foreach(double localY in new[]{FrontLinkRibLeftCentreY,
            FrontLinkRibRightCentreY})
        {
            Point ribCentre=ApplyTransform(deployedFrontLinkTransform,0,localY,
                FrontLinkRibCentreZ);
            ribDeskClearance=Math.Min(ribDeskClearance,
                ribCentre.Y-FrontLinkRibRadius);
        }
        foreach(double localY in new[]{FrontLinkRibLeftCentreY,
            FrontLinkRibRightCentreY})
            foreach(double localZ in new[]{FrontLinkRibBoxBaseZ,
                FrontLinkRibBoxBaseZ+FrontLinkRibBoxDepth})
                ribDeskClearance=Math.Min(ribDeskClearance,
                    ApplyTransform(deployedFrontLinkTransform,0,localY,localZ).Y);
        Require(ribDeskClearance>=5.0,
            "deployed rounded front-link mid-span rib requires at least 5.0 mm desk clearance; actual="+
            F(ribDeskClearance));
        double lockHoleBridge=Distance(deployedLockLocal.Y,deployedLockLocal.Z,
            foldedLockLocal.Y,foldedLockLocal.Z)-6.4;
        Require(lockHoleBridge>=3.0,
            "front-link position holes require at least 3.0 mm solid bridge; actual="+
            F(lockHoleBridge));
        double stopDy=FrontStopCaseY-FrontPivotCaseY;
        double stopDz=FrontStopCaseZ-FrontPivotCaseZ;
        double lockToStopBridge=double.PositiveInfinity;
        double sweptStopIslandMargin=double.PositiveInfinity;
        const double stopSlotRadius=4.1;
        const int analyticStopSweepSamples=257;
        for(int i=0;i<analyticStopSweepSamples;i++)
        {
            double t=(double)i/(analyticStopSweepSamples-1);
            double a=FrontFoldedAngle()+
                (FrontDeployedAngle()-FrontFoldedAngle())*t;
            Point slot=InverseRotate(stopDy,stopDz,a);
            lockToStopBridge=Math.Min(lockToStopBridge,
                Distance(deployedLockLocal.Y,deployedLockLocal.Z,slot.Y,slot.Z)-7.3);
            lockToStopBridge=Math.Min(lockToStopBridge,
                Distance(foldedLockLocal.Y,foldedLockLocal.Z,slot.Y,slot.Z)-7.3);
            sweptStopIslandMargin=Math.Min(sweptStopIslandMargin,Math.Min(
                Math.Min(slot.Y-stopSlotRadius,
                    22.0-slot.Y-stopSlotRadius),
                Math.Min(slot.Z-FrontSweptIslandBaseZ-stopSlotRadius,
                    FrontSweptIslandBaseZ+FrontSweptIslandDepth-slot.Z-
                    stopSlotRadius)));
        }
        Require(lockToStopBridge>=3.25,
            "front-link lock holes require at least 3.25 mm solid bridge to the continuous swept hard-stop slot; actual="+
            F(lockToStopBridge));
        Require(sweptStopIslandMargin>=3.25,
            "front-link swept hard-stop slot requires at least 3.25 mm material to every rectangular reinforcement boundary; actual="+
            F(sweptStopIslandMargin));
        double pivotToStopBridge=Distance(0,0,stopDy,stopDz)-8.2;
        Require(pivotToStopBridge>=4.25,
            "front-link pivot and swept hard-stop slot require at least 4.25 mm nominal edge bridge; actual="+
            F(pivotToStopBridge));
        double bracketStopToLockBridge=Distance(FrontStopCaseY,FrontStopCaseZ,
            FrontLockCaseY,FrontLockCaseZ)-7.3;
        Require(bracketStopToLockBridge>=4.75,
            "front bracket stop and lock through-holes require at least 4.75 mm nominal edge bridge; actual="+
            F(bracketStopToLockBridge));
        double bracketStopIslandOverlap=FrontStopCaseY+10.0-(-207.5);
        Require(bracketStopIslandOverlap>=4.0,
            "front bracket 20 mm stop island requires at least 4.0 mm radial overlap with the primary cheek; actual="+
            F(bracketStopIslandOverlap));
        Point unusedFoldedIslandDeployed=RotateFromPivot(FrontPivotCaseY,
            FrontPivotCaseZ,foldedLockLocal.Y,foldedLockLocal.Z,
            FrontDeployedAngle());
        Point fixedLockDesk=CasePointToDesk(0,FrontLockCaseY,FrontLockCaseZ,stance);
        Point unusedLockDesk=CasePointToDesk(0,unusedFoldedIslandDeployed.Y,
            unusedFoldedIslandDeployed.Z,stance);
        double deployedDeskClearance=Math.Min(fixedLockDesk.Y,unusedLockDesk.Y)-
            FrontLockIslandRadius;
        Require(deployedDeskClearance>=5.0,
            "both deployed front-link lock islands require at least 5.0 mm nominal desk clearance; actual="+
            F(deployedDeskClearance));
        double bracketLockIslandOverlap=FrontLockCaseY+8.0-(-207.5);
        Require(bracketLockIslandOverlap>=4.0,
            "front bracket lock island requires at least 4.0 mm overlap with the primary cheek before the added neck; actual="+
            F(bracketLockIslandOverlap));

        // Conservative empty-case screening based on the measured V0.10 tilt
        // state.  The strict post-build validator repeats this using V0.11
        // native mass properties and a +/-30 mm loaded-CG sensitivity band.
        double mass = 5.55;
        double weight = mass * 9.80665;
        double operatingMoment = 20.0 * 0.200;
        double cgZ = 55.0;
        double rearSf = weight * (stance.SupportFootprint - (cgZ+30.0)) /
            1000.0 / operatingMoment;
        double frontSf = weight * ((cgZ-30.0) - frontWorldZ) /
            1000.0 / operatingMoment;
        Require(rearSf >= 2.0 && frontSf >= 2.0,
            "V11 four-point support must exceed SF 2.0 with +/-30 mm loaded-CG sensitivity");

        Require(LatchCaseMountZ - LatchBodyUpperRowZ == 30.0,
            "Southco V7 door/frame separation must be 30 mm");
        Require(LatchBodyWidth == 31.0 && LatchBodyLength == 72.0,
            "Southco V7 small latch envelope must remain 31 x 72 mm");
        Require(OuterCheekBaseThickness>=4.0 && OuterBossThickness>=4.0,
            "Rear-stand outer cheek must remain full-area 4 mm minimum; local thinning is forbidden");
    }

    private static void ValidateCustomRailScreening()
    {
        SectionProperties gross=CalculateRailSection(false);
        SectionProperties atHole=CalculateRailSection(true);
        RequireClose(gross.Area,156.92,0.001,"custom rail gross section area");
        Require(gross.Iy>=6116.0 && gross.Iz>=1585.0,
            "Custom closed rail gross section inertia fell below the frozen design");
        Require(atHole.Iy>=5203.0 && atHole.Iz>=1558.0,
            "Custom rail worst module-hole section inertia fell below the frozen design");
        Require(atHole.Iy/RailCommercialProxyIy>=1.64 &&
            atHole.Iz/RailCommercialProxyIz>=1.37,
            "Custom rail no longer preserves its verified stiffness margin over the prior commercial proxy");

        double elasticModulus=69000.0;
        double distributedLoad=10.0*9.80665/2.0/RailVisibleLength;
        double span=RailStructuralLength;
        double staticDeflection=5.0*distributedLoad*Math.Pow(span,4.0)/
            (384.0*elasticModulus*atHole.Iy);
        double sectionModulus=atHole.Iy/Math.Max(atHole.CentroidZ,
            RailOverallDepth-atHole.CentroidZ);
        double staticMoment=distributedLoad*span*span/8.0;
        double staticStress=staticMoment/sectionModulus;
        Require(staticDeflection<=0.30 && staticStress<=7.6,
            "Custom rail 10 kg-row simply-supported screening exceeded the design gate");

        double proofLoad=200.0;
        double pointDeflection=proofLoad*Math.Pow(span,3.0)/
            (48.0*elasticModulus*atHole.Iy);
        double pointStress=(proofLoad*span/4.0)/sectionModulus;
        Require(pointDeflection<=1.86 && 276.0/pointStress>=4.6,
            "Custom rail 200 N-per-rail centre proof screening exceeded the design gate");
    }

    private static SectionProperties CalculateRailSection(bool atModuleHole)
    {
        List<SectionRectangle> rectangles=new List<SectionRectangle>();
        rectangles.Add(new SectionRectangle(1.0,10.0,12.0,0.0,6.0));
        rectangles.Add(new SectionRectangle(-1.0,6.4,2.2,0.0,5.0));
        rectangles.Add(new SectionRectangle(1.0,10.0,10.0,0.0,17.0));
        rectangles.Add(new SectionRectangle(-1.0,7.0,7.0,0.0,17.0));
        if(atModuleHole)
        {
            rectangles.Add(new SectionRectangle(-1.0,3.2,12.0,0.0,6.0));
            rectangles.Add(new SectionRectangle(1.0,3.2,2.2,0.0,5.0));
        }

        double area=rectangles.Sum(r=>r.Sign*r.Width*r.Depth);
        double cy=rectangles.Sum(r=>r.Sign*r.Width*r.Depth*r.CentreY)/area;
        double cz=rectangles.Sum(r=>r.Sign*r.Width*r.Depth*r.CentreZ)/area;
        double iy=rectangles.Sum(r=>r.Sign*((r.Width*Math.Pow(r.Depth,3.0)/12.0)+
            r.Width*r.Depth*Math.Pow(r.CentreZ-cz,2.0)));
        double iz=rectangles.Sum(r=>r.Sign*((r.Depth*Math.Pow(r.Width,3.0)/12.0)+
            r.Width*r.Depth*Math.Pow(r.CentreY-cy,2.0)));
        return new SectionProperties(area,cy,cz,iy,iz);
    }

    private static string CreateSideFrame(RackCadSession cad, Stance stance)
    {
        ModelDoc2 doc = cad.NewPart(SideStem);
        try
        {
            Point stop = DeployedBackPointInCase(stance, StopLocalY, StopLocalZ);
            Point lockPoint = DeployedBackPointInCase(stance,
                LockDeployLocalY, LockDeployLocalZ);
            Body2 side = cad.Box(0, 0, 0, SideCoreThickness, CaseHeight,
                CaseDepth - ShellThickness);
            side = Unite(side, cad.Box(0, 0, 0, SideLoadThickness, CaseHeight, 24),
                "continuous rail band");
            side = Unite(side, cad.Box(0, 0, 96, SideLoadThickness, CaseHeight, 12),
                "continuous rear shear band");
            side = Unite(side, cad.Box(0, -122, 18, SideLoadThickness, 130, 72),
                "rear stand load block");
            foreach (double edgeY in new[] { -207.0, 207.0 })
                side = Unite(side, cad.Box(0, edgeY, 24, SideLoadThickness, 6, 72),
                    "formed shell-joint band");
            foreach (MountPoint m in CheekSpacers)
                side = Unite(side, cad.Cylinder(-2, m.Y, m.Z, 1, 0, 0, 26, 4),
                    "cheek spacer bearing island");
            foreach (Point p in new[]
            {
                new Point(0,HingeCaseY,HingeCaseZ), new Point(0,stop.Y,stop.Z),
                new Point(0,lockPoint.Y,lockPoint.Z)
            }) side = Unite(side, cad.Cylinder(-2, p.Y, p.Z, 1,0,0,28,4),
                "stand bearing island");

            foreach (double y in RailPositions(cad))
            {
                side = SideHole(cad, side, y, RailM3AxisDepth, 3.4,
                    "Rymovia integral-front M3 anti-rotation locator clearance");
                side = SymmetricCounterboreX(cad,side,y,RailM3AxisDepth,6.0,1.0,
                    SideLoadThickness,"flush ultra-low-head rail M3 seat");
                side = SideHole(cad, side, y, RailM4AxisDepth, 4.5,
                    "Rymovia replaceable-insert axial M4 structural clearance");
                side = SymmetricCounterboreX(cad,side,y,RailM4AxisDepth,7.4,1.2,
                    SideLoadThickness,"flush ultra-low-head rail M4 seat");
            }
            side = SideHole(cad, side, HingeCaseY, HingeCaseZ, AxleClearance10,
                "rear pivot axle clearance");
            side = SymmetricCounterboreX(cad, side, HingeCaseY, HingeCaseZ, 12.2, 1.1,
                SideLoadThickness, "rear pivot flush retention");
            side = SideHole(cad, side, stop.Y, stop.Z, AxleClearance10,
                "rear hard-stop axle clearance");
            side = SymmetricCounterboreX(cad, side, stop.Y, stop.Z, 12.2, 1.1,
                SideLoadThickness, "rear stop flush retention");
            side = SideHole(cad, side, lockPoint.Y, lockPoint.Z, 6.4,
                "M6 captive shoulder-lock interface");
            foreach (MountPoint m in CheekSpacers)
                side = SideHole(cad, side, m.Y, m.Z, 5.5, "M5 spacer fastener");
            foreach (double y in new[] { -209.0, 209.0 })
                foreach (double z in new[] { 30.0, 80.0 })
                    side = SideHole(cad, side, y, z, 4.5, "M4 upper/lower shell corner joint");
            foreach (double y in new[] { -155.0, 155.0 })
                side = SideHole(cad, side, y, 102.0, 4.5, "M4 VESA frame side tie");
            foreach (double signY in Signs())
                foreach (double offset in new[] { -LatchHalfMountPitch, LatchHalfMountPitch })
                    side = SideHole(cad, side, signY * LatchCentreY + offset,
                        LatchCaseMountZ, LatchMountHole, "Southco keeper M3 mount");

            foreach (double y in SideVentY)
                foreach (double z in SideVentZ)
                    side = CapsuleHoleX(cad, side, y, z, 22.0, 4.0,
                        "stand-separated side vent");

            cad.AddBody(doc, side, "V11 continuous 6061 side frame with explicit joint holes");
            cad.ApplyMaterial(doc, "6061-T6 (SS)", NaturalAluminium);
            cad.Property(doc, "Mechanical revision", Version + " release candidate");
            cad.Property(doc, "Rail load path", "Self-manufactured 542 mm constant-section 6061-T6 closed rail: one M3 locator tapped directly into each integral solid front land at z9 plus one M4 structural screw into each captive 7075 end insert at z17; both pass through the continuous 4 mm side-frame band");
            cad.Property(doc, "Rail mounting axes", "M3 locator z=9 mm and M4 structural z=17 mm provide a positive 8 mm anti-rotation couple at every rail end; all 24 external heads are recessed in the full-thickness side-frame band");
            cad.Property(doc, "Shell load path", "Two M4 joints at each upper/lower corner; rear skin is independently joined to twelve formed edge tabs");
            cad.Property(doc, "Stand load path", "10 mm retained shoulder axles in double shear; 12 mm press-fit sleeve in the 8 mm rear leg; M6 flush captive position screw is not the normal stop");
            cad.Property(doc, "Vent keepout", "Six 22 x 4 mm vents per side at y142..186,z86/92; clear of the stand cheek, latch bay and VESA side screws");
            return SavePart(cad, doc, SideStem, true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateBackLeg(RackCadSession cad, Stance stance)
    {
        ModelDoc2 doc = cad.NewPart(BackLegStem);
        try
        {
            Point fixedLock = DeployedBackPointInCase(stance,
                LockDeployLocalY, LockDeployLocalZ);
            double foldedHoleY = HingeLocalY + fixedLock.Y - HingeCaseY;
            double foldedHoleZ = HingeLocalZ + fixedLock.Z - HingeCaseZ;

            double armStart=HingeLocalY-8.0;
            double armEnd=BackFootLocalY-18.0;
            Body2 leg = cad.Box(0, (armStart+armEnd)/2.0, -7.0,
                BackLegThickness, armEnd-armStart, BackLegWidth);
            leg = Unite(leg, cad.Cylinder(-4, HingeLocalY, HingeLocalZ,
                1,0,0,48,8), "48 mm pivot root");
            leg = Unite(leg, cad.Cylinder(-4, foldedHoleY, foldedHoleZ,
                1,0,0,20,8), "folded lock boss");
            leg = Unite(leg, cad.Box(0, BackFootLocalY-15.0, -2.0,
                6.0, 22.0, 16.0),
                "captured foot neck");
            leg = Unite(leg, cad.Box(0, BackFootLocalY-5.0, -3.0,
                7.6, 10.0, 18.0),
                "overmould retention mushroom");
            // Full-thickness ear extends into the diameter-48 root, while its
            // rear shoulder and bottom wall carry the keyed heel in bearing.
            // The earlier 20 x 40 mm ear left only a narrow corner wedge and
            // a 3.7/1.8 mm pocket shoulder/bottom, which is not release-grade.
            leg = Unite(leg, cad.Box(0, -106.5,
                -49.0, 8.0, 27.0, 55.0), "rear stop full-web load ear");

            // A 5.8 mm steel heel sits in a 6.2 mm central pocket, leaving
            // 0.9 mm integral side cheeks.  The M4 cross pin makes retention
            // geometric rather than adhesive-only.
            leg = cad.Cut(leg, cad.Box(0, HeelLocalY, HeelLocalZ - 12.2,
                6.2, 8.6, 24.4), "keyed steel heel pocket");
            leg = cad.Cut(leg, cad.Cylinder(-4.4, HeelLocalY, HeelLocalZ,
                1,0,0,4.5,8.8), "heel retention cross-hole");
            leg = cad.Cut(leg, cad.Cylinder(-4.4, HeelLocalY, HeelLocalZ,
                1,0,0,6.2,0.9), "heel-pin flush-head counterbore");
            leg = cad.Cut(leg, cad.Cylinder(-4.4, HingeLocalY, HingeLocalZ,
                1,0,0,BackPivotHole,8.8), "12 mm press-fit bushing bore");
            const int rearStopSweepSamples=9;
            const double rearStopReleaseDegrees=14.0;
            for(int i=0;i<rearStopSweepSamples;i++)
            {
                double a=Degrees(rearStopReleaseDegrees*i/(rearStopSweepSamples-1));
                double c=Math.Cos(a),s=Math.Sin(a);
                double qy=StopLocalY*c-StopLocalZ*s;
                double qz=StopLocalY*s+StopLocalZ*c;
                leg=cad.Cut(leg,cad.Cylinder(-4.4,HingeLocalY+qy,HingeLocalZ+qz,
                    1,0,0,10.2,8.8),"rear hard-stop true fold-trajectory U cradle");
            }
            foreach (Point p in new[]
            {
                new Point(0,HingeLocalY+LockDeployLocalY,HingeLocalZ+LockDeployLocalZ),
                new Point(0,foldedHoleY,foldedHoleZ)
            }) leg = cad.Cut(leg, cad.Cylinder(-4.4,p.Y,p.Z,1,0,0,6.4,8.8),
                "M6 deployed/folded shoulder-lock hole");

            cad.AddBody(doc, leg, "262 mm 7075 rear support arm with bushed pivot and keyed steel stop heel");
            cad.ApplyMaterial(doc, "7075-T6 (SN)", DarkAluminium);
            cad.Property(doc, "Support geometry", "Pivot-to-pad centre 262 mm; 60 degree rear support coordinate approximately 237.70 mm");
            cad.Property(doc, "Section", "8 x 26 mm 7075 arm compensates the longer moment arm; diameter-48 root; 27 x 55 mm full-web stop ear with at least 10.7 mm rear heel shoulder and 4.8 mm pocket bottom; R8 minimum production blends required");
            cad.Property(doc, "Pivot", "Press-fit iglide G1SM-1012-08 dimensional envelope; production supplier ID10 OD12 L8");
            cad.Property(doc, "Position lock", "Two real 6.4 mm holes align with one flush captive M6 shoulder screw; a nine-sample 14 degree trajectory-cut U-cradle opens toward actual fold travel while a tangent 17-4PH heel bears on the fixed 10 mm stop axle");
            cad.Property(doc, "Heel retention", "5.8 mm 17-4PH tangent stop heel in captive central pocket plus M4 transverse pin; adhesive is not the primary retainer");
            cad.Property(doc, "Safety boundary", "Root fatigue, pin bearing, one-leg misuse and proof load still require FEA and physical validation");
            return SavePart(cad, doc, BackLegStem, true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateOuterCheek(RackCadSession cad, Stance stance)
    {
        ModelDoc2 doc = cad.NewPart(OuterCheekStem);
        try
        {
            Point stop = DeployedBackPointInCase(stance, StopLocalY, StopLocalZ);
            Point lockPoint = DeployedBackPointInCase(stance,
                LockDeployLocalY, LockDeployLocalZ);
            Body2 cheek = cad.Box(0, -39.0, 18.0, OuterCheekBaseThickness,
                342.0, 62.0);
            cheek = Unite(cheek, cad.Box(0,-128.0,-10.0,
                OuterCheekBaseThickness,76.0,48.0), "lower load-ear cover");
            foreach (Point p in new[]
            {
                new Point(0,HingeCaseY,HingeCaseZ), new Point(0,stop.Y,stop.Z),
                new Point(0,lockPoint.Y,lockPoint.Z)
            }) cheek = Unite(cheek, cad.Cylinder(-2,p.Y,p.Z,1,0,0,28,4),
                "4 mm local stand boss");
            foreach (MountPoint m in CheekSpacers)
                cheek = Unite(cheek, cad.Cylinder(-2,m.Y,m.Z,1,0,0,26,4),
                    "4 mm spacer island");

            cheek = cad.Cut(cheek, cad.Box(0,-LatchCentreY,12.0,4.8,32.0,28.0),
                "lower Southco V7 keeper bay");
            cheek = ThroughCheekHole(cad, cheek, HingeCaseY,HingeCaseZ,
                AxleClearance10,"rear pivot through-hole");
            cheek = SymmetricCounterboreX(cad,cheek,HingeCaseY,HingeCaseZ,
                12.2,1.1,4.0,"rear pivot flush retention");
            cheek = ThroughCheekHole(cad,cheek,stop.Y,stop.Z,
                AxleClearance10,"rear stop through-hole");
            cheek = SymmetricCounterboreX(cad,cheek,stop.Y,stop.Z,
                12.2,1.1,4.0,"rear stop flush retention");
            cheek = ThroughCheekHole(cad,cheek,lockPoint.Y,lockPoint.Z,
                6.4,"flush M6 lock screw");
            cheek = SymmetricCounterboreX(cad,cheek,lockPoint.Y,lockPoint.Z,
                10.7,1.7,4.0,"flush M6 lock screw head");
            foreach (MountPoint m in CheekSpacers)
                cheek = ThroughCheekHole(cad,cheek,m.Y,m.Z,5.5,"M5 spacer fixing");
            cheek = ThroughCheekHole(cad,cheek,126.0,52.0,18.0,
                "folded rear-foot extraction notch");

            cad.AddBody(doc, cheek, "full-area 4 mm 6061 rear-stand outer cheek");
            cad.ApplyMaterial(doc, "6061-T6 (SS)", DarkAluminium);
            cad.Property(doc, "Thickness rule", "Full 4 mm over the complete cheek envelope; no broad-area thinning, lightening pocket or cutout in the stand support plate");
            cad.Property(doc, "Latch clearance", "32 x 28 mm lower keeper bay y[-210,-178],z[12,40] prevents the actual 31 mm V7 envelope colliding with the stand cheek; the adjacent custom-rail M3/M4 heads are recessed fully inside the 4 mm side band");
            cad.Property(doc, "Vent clearance", "Cheek storage field stops at z80; first side vent begins at z84 with no visual or airflow overlap");
            cad.Property(doc, "Double shear", "Cheek inner face x +/-283.8; 8 mm rear leg at x +/-279.4 retains 0.4 mm nominal running clearance");
            return SavePart(cad, doc, OuterCheekStem, true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateBackBushing(RackCadSession cad)
    {
        ModelDoc2 doc = cad.NewPart(BackBushingStem);
        try
        {
            Body2 b = cad.Cylinder(-4,0,0,1,0,0,12.0,8.0);
            b = cad.Cut(b,cad.Cylinder(-4.3,0,0,1,0,0,10.2,8.6),
                "10.2 mm CAD running envelope");
            cad.AddBody(doc,b,"igus G1SM-1012-08 dimensional sleeve envelope");
            cad.ApplyMaterial(doc,"NEOPRENE",DarkAluminium);
            cad.Property(doc,"Supplier envelope","igus iglide G1SM-1012-08; nominal ID10 OD12 length8 mm");
            cad.Property(doc,"Mass-property surrogate","SOLIDWORKS NEOPRENE is a conservative available-library density proxy only; production material remains iglide G1 and vendor data governs");
            cad.Property(doc,"CAD clearance","10.2 mm bore and 9.8 mm axle are clearance representations; use supplier press-fit and shaft tolerances in production drawing");
            return SavePart(cad,doc,BackBushingStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateRetainedAxle(RackCadSession cad, string stem,
        double shaftDiameter, double grip, double retainerDiameter, string label)
    {
        ModelDoc2 doc = cad.NewPart(stem);
        try
        {
            Body2 axle = cad.Cylinder(-grip/2,0,0,1,0,0,shaftDiameter,grip);
            axle = Unite(axle,cad.Cylinder(-grip/2,0,0,1,0,0,
                retainerDiameter,1.0),label+" first flush retainer");
            axle = Unite(axle,cad.Cylinder(grip/2-1.0,0,0,1,0,0,
                retainerDiameter,1.0),label+" second flush retainer");
            cad.AddBody(doc,axle,"full-shank retained "+label+" axle");
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Production grade","17-4PH H900 or approved hardened shoulder axle; AISI304 is mass/appearance proxy");
            cad.Property(doc,"Retention","Two flush end retainers sit in real counterbores; no unretained plain rod and no thread in either shear plane");
            cad.Property(doc,"Grip","16.8 mm full shank across 4 mm side, 8.8 mm cavity and 4 mm local outer boss");
            return SavePart(cad,doc,stem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateSpacer(RackCadSession cad)
    {
        ModelDoc2 doc = cad.NewPart(SpacerStem);
        try
        {
            Body2 b=cad.Cylinder(-4.4,0,0,1,0,0,12,8.8);
            b=cad.Cut(b,cad.Cylinder(-4.7,0,0,1,0,0,5.5,9.4),"M5 through bore");
            cad.AddBody(doc,b,"12 x 8.8 mm hard-anodized physical spacer");
            cad.ApplyMaterial(doc,"7075-T6 (SN)",NaturalAluminium);
            cad.Property(doc,"Fastening","M5 full-shank through screw with flush inner head and low-profile locknut; six spacers per side");
            return SavePart(cad,doc,SpacerStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateCheekFastenerPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(CheekFastenerPackStem);
        try
        {
            int count=0;
            foreach(double sx in Signs()) foreach(MountPoint m in CheekSpacers)
            {
                double start=sx>0?270.9:-270.9;
                Body2 bolt=cad.Cylinder(start,m.Y,m.Z,sx,0,0,4.8,17.0);
                bolt=Unite(bolt,cad.Cylinder(sx*287.8,m.Y,m.Z,sx,0,0,8.5,1.0),"M5 external low-profile retainer");
                cad.AddBody(doc,bolt,"M5 A4-80 cheek through fastener"); count++;
            }
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Count",count+" M5 A4-80 fasteners; thread/prevailing nut lies outside the shear cavity");
            return SavePart(cad,doc,CheekFastenerPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateBackLockPack(RackCadSession cad, Stance stance)
    {
        ModelDoc2 doc=cad.NewPart(BackLockPackStem);
        try
        {
            Point p=DeployedBackPointInCase(stance,LockDeployLocalY,LockDeployLocalZ);
            foreach(double sx in Signs())
            {
                Body2 screw=cad.Cylinder(sx*271.2,p.Y,p.Z,sx,0,0,5.8,16.2);
                screw=Unite(screw,cad.Cylinder(sx*286.2,p.Y,p.Z,sx,0,0,10.5,1.5),"flush captive M6 head");
                cad.AddBody(doc,screw,(sx<0?"Left":"Right")+" M6 captive shoulder lock screw");
            }
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Function","Tool-secured flush captive shoulder screws engage either folded or deployed leg hole; zero external projection for lid closure");
            cad.Property(doc,"Load boundary","Position retention only; the separate 10 mm steel hard stop carries normal operating load");
            cad.Property(doc,"Service","Captured retracting screw detail and replaceable thread insert require supplier DFM before drawing release");
            return SavePart(cad,doc,BackLockPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateBackHeel(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(BackHeelStem);
        try
        {
            Body2 heel=cad.Box(0,0,-12,5.8,8.1,24.0);
            heel=cad.Cut(heel,cad.Cylinder(-3.2,0,0,1,0,0,4.2,6.4),"M4 retention hole");
            cad.AddBody(doc,heel,"keyed 17-4PH stop heel");
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Production","17-4PH H900, ground tangent stop face with 0.05 mm nominal CAD running gap to the retained axle; mass uses AISI304 proxy");
            cad.Property(doc,"Retention","Central pocket leaves integral leg side cheeks; transverse M4 pin is positive mechanical retention");
            return SavePart(cad,doc,BackHeelStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateBackHeelPin(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(BackHeelPinStem);
        try
        {
            Body2 p=cad.Cylinder(-4.3,0,0,1,0,0,3.8,8.6);
            p=Unite(p,cad.Cylinder(-4.3,0,0,1,0,0,6.0,0.8),"flush heel pin head");
            cad.AddBody(doc,p,"M4 retained heel cross pin");
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Retention","Peened or prevailing-thread flush retention selected during supplier DFM; full shank crosses heel");
            return SavePart(cad,doc,BackHeelPinStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateBackFoot(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(BackFootStem);
        try
        {
            Body2 pad=cad.Cylinder(-BackFootAxialLength/2,0,0,1,0,0,
                BackFootDiameter,BackFootAxialLength);
            pad=cad.Cut(pad,cad.Box(0,-9.0,-8.2,6.4,8.0,16.4),"narrow mould entry");
            pad=cad.Cut(pad,cad.Box(0,-5.0,-9.2,7.9,10.4,18.4),"internal undercut capture");
            cad.AddBody(doc,pad,"70A EPDM overmoulded captured rear foot");
            cad.ApplyMaterial(doc,"NEOPRENE",RubberBlack);
            cad.Property(doc,"Positive capture","Narrow entry plus wider internal mushroom cavity; production part is moulded over the metal neck, not glued into an open slot");
            cad.Property(doc,"Desk contact","26 mm round crown, centre 13 mm above desk in the 60 degree state");
            return SavePart(cad,doc,BackFootStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateCustomThreadStrip(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(RailThreadStripStem);
        try
        {
            Body2 strip=cad.Box(0,0,0,RailVisibleLength,RailThreadStripWidth,
                RailThreadStripThickness);
            double endMargin=(RailVisibleLength-(RailHoleCount-1)*RailPitch)/2.0;
            RequireClose(endMargin,2.54,0.001,"104HP threaded-strip end phase");
            for(int i=0;i<RailHoleCount;i++)
            {
                double x=-RailVisibleLength/2.0+endMargin+i*RailPitch;
                strip=cad.Cut(strip,cad.Cylinder(x,0,-0.3,0,0,1,
                    RailThreadMinorDiameter,RailThreadStripThickness+0.6),
                    "M3 x 0.5 production tap pilot "+(i+1));
            }
            cad.AddBody(doc,strip,"Rymovia replaceable 104HP AISI304 threaded strip");
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Manufacturing","6 x 2 x 528.32 mm AISI304 strip; CNC drill and form-tap 104 x M3-0.5 positions at 5.08 mm pitch after stress-relief/straightness control");
            cad.Property(doc,"Module interface","First and last thread centres are 2.54 mm from the 104HP visible envelope; 3.2 mm rail clearances permit nominal floating alignment without weakening the strip threads");
            cad.Property(doc,"Service","Strip slides out axially after one side frame is removed and is replaceable independently of the structural extrusion");
            double mass=ReadMass(doc);
            Require(mass>=0.040 && mass<=0.045,
                "Custom AISI304 threaded-strip mass is outside the design range; actual="+F(mass));
            cad.Property(doc,"Native CAD mass",F(mass)+" kg per strip");
            return SavePart(cad,doc,RailThreadStripStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateCustomRail(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(RailStem);
        try
        {
            Body2 rail=cad.Box(0,0,0,RailStructuralLength,RailHeight,
                RailFrontDepth+0.02);
            rail=cad.Cut(rail,cad.Box(0,0,RailThreadPocketStart,
                RailStructuralLength+0.8,RailThreadPocketWidth,
                RailThreadPocketDepth),"continuous replaceable M3-strip pocket");

            double endMargin=(RailVisibleLength-(RailHoleCount-1)*RailPitch)/2.0;
            for(int i=0;i<RailHoleCount;i++)
            {
                double x=-RailVisibleLength/2.0+endMargin+i*RailPitch;
                rail=cad.Cut(rail,cad.Cylinder(x,0,-0.3,0,0,1,
                    RailModuleClearanceDiameter,RailFrontDepth+0.8),
                    "104HP M3 module clearance "+(i+1));
            }

            double wallCentreY=RailHeight/2.0-RailTubeWall/2.0;
            foreach(double sy in Signs())
                rail=Unite(rail,cad.Box(0,sy*wallCentreY,RailFrontDepth,
                    RailStructuralLength,RailTubeWall,RailSpineDepth),
                    "1.5 mm closed-spine side wall");
            rail=Unite(rail,cad.Box(0,0,RailFrontDepth,
                RailStructuralLength,RailHeight,RailTubeWall),
                "1.5 mm closed-spine front wall");
            rail=Unite(rail,cad.Box(0,0,
                RailFrontDepth+RailSpineDepth-RailTubeWall,
                RailStructuralLength,RailHeight,RailTubeWall),
                "1.5 mm closed-spine rear wall");

            foreach(double sx in Signs())
                rail=cad.Cut(rail,cad.Cylinder(
                    sx*(RailStructuralLength/2.0+0.3),0,RailM3AxisDepth,
                    -sx,0,0,3.2,7.1),
                    "M3 x 0.5 integral-front end tap envelope");

            cad.AddBody(doc,rail,
                "542 mm constant-section 6061 rail with 12 mm solid face and 10 mm closed spine");
            cad.ApplyMaterial(doc,"6061-T6 (SS)",NaturalAluminium);
            SectionProperties gross=CalculateRailSection(false);
            SectionProperties atHole=CalculateRailSection(true);
            cad.Property(doc,"Manufacturing route","One constant 542 x 10 x 22 mm 6061-T6 extrusion, cut to length; post-machine 104 module clearances plus two axial M3 locator taps. There are no integral variable-section end bosses");
            cad.Property(doc,"Eurorack interface","Central 528.32 mm usable face; 104 diameter-3.2 clearances at exact 5.08 mm pitch; replaceable 6 x 2 mm AISI304 M3 strip in a 6.4 x 2.2 mm pocket");
            cad.Property(doc,"Structural section","12 mm solid front plus 10 mm deep, 1.5 mm wall closed torsion spine; gross Iy="+F(gross.Iy)+" mm^4, Iz="+F(gross.Iz)+" mm^4; module-hole-section Iy="+F(atHole.Iy)+" mm^4");
            cad.Property(doc,"End fixing","Integral front land takes M3 locator at z9; rectangular cavity accepts replaceable 7075 M4 insert at z17. The 8 mm positive fastener couple prevents reliance on one screw or clamp friction");
            cad.Property(doc,"Analytic screening","Simply-supported conservative screen: 10 kg per row static and 200 N centre proof per rail; CAD equations gate <=0.30 mm static deflection and >=4.6 yield safety factor at the worst module-hole section");
            cad.Property(doc,"Validation boundary","Analytic beam screening and CAD fit do not replace extrusion coupon, M3/M4 pullout, loaded vibration, drop, fatigue and real-module first-article tests");
            double mass=ReadMass(doc);
            Require(mass>=0.202 && mass<=0.212,
                "Custom closed rail mass is outside the frozen design range; actual="+F(mass));
            cad.Property(doc,"Native CAD mass",F(mass)+" kg per extrusion");
            return SavePart(cad,doc,RailStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateRailInsertPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(RailInsertPackStem);
        try
        {
            int count=0;
            foreach(double y in RailPositions(cad)) foreach(double sx in Signs())
            {
                double centreX=sx*(RailStructuralLength/2.0-
                    RailInsertLength/2.0-0.1);
                double startX=sx*(RailStructuralLength/2.0-0.1);
                Body2 insert=cad.Box(centreX,y,
                    RailFrontDepth+RailTubeWall+0.1,
                    RailInsertLength,RailInsertWidth,RailInsertDepth);
                insert=cad.Cut(insert,cad.Cylinder(startX,y,RailM4AxisDepth,
                    -sx,0,0,4.2,RailInsertLength),
                    "M4 production-thread clearance envelope");
                cad.AddBody(doc,insert,"24 mm keyed 7075 rail-end insert");
                count++;
            }
            Require(count==12,"Custom rail insert pack must contain twelve inserts");
            cad.ApplyMaterial(doc,"7075-T6 (SN)",DarkAluminium);
            cad.Property(doc,"Count","12 replaceable 24 x 6.8 x 6.8 mm 7075-T6 keyed end inserts; one at each end of six rails");
            cad.Property(doc,"Fit","0.1 mm nominal clearance per side in the 7 x 7 mm closed-spine cavity; rectangular section positively prevents rotation");
            cad.Property(doc,"Thread","Production insert is tapped M4 x 0.7 with at least 15 mm available screw engagement; diameter 4.2 mm is only the zero-interference CAD thread envelope");
            cad.Property(doc,"Retention","In the assembled case the M4 screw and side frame positively capture the insert. Supplier DFM shall add two qualified handling stakes or retaining compound so the loose insert cannot migrate during service disassembly");
            cad.Property(doc,"Corrosion control","Hard-anodize or conversion-coat the 7075 insert and 6061 rail; isolate and lubricate per supplier process to prevent fretting and galvanic seizure");
            double mass=ReadMass(doc);
            Require(mass>=0.024 && mass<=0.029,
                "Custom rail insert-pack mass is outside the design range; actual="+F(mass));
            cad.Property(doc,"Native CAD mass",F(mass)+" kg for all twelve inserts");
            return SavePart(cad,doc,RailInsertPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateRailFastenerPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(RailFastenerPackStem);
        try
        {
            int m3Count=0,m4Count=0;
            foreach(double y in RailPositions(cad)) foreach(double sx in Signs())
            {
                double outer=sx*275.0;
                Body2 m3=cad.Cylinder(outer,y,RailM3AxisDepth,-sx,0,0,2.8,9.8);
                m3=Unite(m3,cad.Cylinder(outer,y,RailM3AxisDepth,-sx,0,0,
                    5.8,1.0),"M3 ultra-low-profile head");
                cad.AddBody(doc,m3,"M3 x 10 integral-front locator screw");
                m3Count++;

                Body2 m4=cad.Cylinder(outer,y,RailM4AxisDepth,-sx,0,0,3.8,19.8);
                m4=Unite(m4,cad.Cylinder(outer,y,RailM4AxisDepth,-sx,0,0,
                    7.2,1.0),"M4 ultra-low-profile head");
                cad.AddBody(doc,m4,"M4 x 20 structural insert screw");
                m4Count++;
            }
            Require(m3Count==12&&m4Count==12,
                "Custom rail fastener pack must contain 12 M3 plus 12 M4 screws");
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Count","12 x M3 x 10 locator screws plus 12 x M4 x 20 structural screws; two positive fixing axes at each of six rail ends");
            cad.Property(doc,"Specification","Use traceable A4-80 ultra-low-head screws or approved equivalent; M3 engages the integral 6061 front land >=5.5 mm and M4 engages the replaceable 7075 insert >=15 mm");
            cad.Property(doc,"Locking","Clean threads and apply qualified removable medium-strength threadlocker; freeze torque only after supplier coupon pullout and prevailing-torque tests. Add witness marks after final torque");
            cad.Property(doc,"Service","Replace any screw whose low head, drive recess or locking patch is damaged; never substitute longer screws that can bottom before clamp-up");
            double mass=ReadMass(doc);
            Require(mass>=0.028 && mass<=0.036,
                "Custom rail fastener-pack mass is outside the design range; actual="+F(mass));
            cad.Property(doc,"Native CAD mass",F(mass)+" kg for all 24 screws");
            return SavePart(cad,doc,RailFastenerPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateUpperEdge(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(UpperEdgeStem);
        try
        {
            Body2 edge=cad.Box(0,0,0,542.0,2.0,108.0);
            edge=CutEdgeWindow(cad,edge,-218.5,55.0,75.0,60.0,"adapter reserve window");
            edge=CutEdgeWindow(cad,edge,MidiCassetteCentreX,55.0,80.0,60.0,"MIDI USB window");
            edge=CutEdgeWindow(cad,edge,AudioCassetteCentreX,55.0,166.0,60.0,"audio matrix window");
            edge=AddEdgeCassetteHoles(cad,edge,-218.5,41.0,"adapter cassette");
            edge=AddEdgeCassetteHoles(cad,edge,MidiCassetteCentreX,43.0,"MIDI USB cassette");
            edge=AddEdgeCassetteHoles(cad,edge,AudioCassetteCentreX,86.0,"audio cassette");
            foreach(double x in new[]{HandleLeftMountX,HandleRightMountX})
                edge=HoleY(cad,edge,x,45.0,4.5,2.0,"Southco P9 M4 handle mount");

            foreach(double x in BackFastenerX)
            {
                edge=Unite(edge,cad.Box(x,-3.0,96.0,30.0,6.0,12.5),
                    "rear skin formed fastening tab");
                edge=HoleZ(cad,edge,x,-3.0,4.2,95.7,13.1,
                    "M4 rear skin tab thread envelope");
            }
            foreach(double sx in Signs()) foreach(double z in new[]{30.0,80.0})
                edge=HoleXFromEnd(cad,edge,sx*271.0,0,z,-sx,4.2,12.4,
                    "M4 upper-to-side corner thread envelope");

            cad.AddBody(doc,edge,"V11 upper structural edge retaining the approved I/O order");
            cad.ApplyMaterial(doc,"5052-H32",NaturalAluminium);
            cad.Property(doc,"Interface order","95 mm adapter reserve | inline 3xDIN plus USB-C | one central P9 handle | 2x4 audio");
            cad.Property(doc,"Handle interface","Southco P9-128-40-M4N-15-3 envelope; two M4 holes on 128 mm centres; separate 160 x 32 x 4 mm spreader");
            cad.Property(doc,"Shell joints","Two M4 end joints per side plus six rear-skin formed tabs; old slot-only/unjoined edge is not used");
            cad.Property(doc,"Manufacturing","2 mm 5052-H32 brake-formed edge with local rear tabs; final bend relief and radii by sheet-metal DFM");
            return SavePart(cad,doc,UpperEdgeStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateLowerEdge(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(LowerEdgeStem);
        try
        {
            Body2 edge=cad.Box(0,0,0,542.0,2.0,108.0);
            foreach(double groupX in new[]{-150.0,150.0})
                foreach(double dx in new[]{-36.0,-12.0,12.0,36.0})
                    foreach(double z in new[]{44.0,60.0})
                        edge=cad.Cut(edge,cad.Box(groupX+dx,0,z-2.0,22.0,2.8,4.0),
                            "hidden lower passive vent");
            foreach(double x in BackFastenerX)
            {
                edge=Unite(edge,cad.Box(x,3.0,96.0,30.0,6.0,12.5),
                    "rear skin formed fastening tab");
                edge=HoleZ(cad,edge,x,3.0,4.2,95.7,13.1,
                    "M4 rear skin tab thread envelope");
            }
            foreach(double sx in Signs()) foreach(double z in new[]{30.0,80.0})
                edge=HoleXFromEnd(cad,edge,sx*271.0,0,z,-sx,4.2,12.4,
                    "M4 lower-to-side corner thread envelope");
            foreach(double sx in Signs()) foreach(double dx in new[]{-FrontBracketMountOffsetX,FrontBracketMountOffsetX})
                foreach(double z in FrontBracketMountZ)
                {
                    edge=HoleY(cad,edge,sx*FrontFootPlaneX+dx,z,4.5,2.0,
                        "front anti-tip bracket M4 mount");
                    edge=cad.Cut(edge,cad.Cylinder(sx*FrontFootPlaneX+dx,-1.4,z,
                        0,1,0,7.4,1.45),"front anti-tip bracket flush-head counterbore");
                }
            foreach(double sx in Signs())
            {
                edge=cad.Cut(edge,cad.Box(sx*FrontFootPlaneX,0,0,
                    18.0,2.8,98.0),"front-link and bracket sweep opening below continuous top web");
                edge=cad.Cut(edge,cad.Box(sx*FrontFootPlaneX,0,
                    FrontStopCaseZ-5.5,20.4,2.8,11.0),
                    "local retained front-stop collar service relief");
            }

            cad.AddBody(doc,edge,"V11 lower edge with hidden vents and front anti-tip mounts");
            cad.ApplyMaterial(doc,"5052-H32",NaturalAluminium);
            cad.Property(doc,"Four-point stability","Eight M4 holes lie outside two 18 x 98 mm front-link sweep windows at x +/-205 mm; each window retains a continuous 10 mm shell web and four independent cheek wings restore the local load path");
            cad.Property(doc,"Stop service relief","Each 18 mm sweep window widens locally to 20.4 mm for only 11 mm at the retained stop collars; the top web and all four independent bracket mounting wings remain continuous");
            cad.Property(doc,"Vent separation","Lower vents remain between the anti-tip brackets and away from corner joints");
            cad.Property(doc,"Shell joints","Two M4 end joints per side plus six rear-skin formed tabs");
            return SavePart(cad,doc,LowerEdgeStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateHandle(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(HandleStem);
        try
        {
            Body2 handle=cad.Cylinder(HandleLeftMountX-0.2,4.8,0,
                1,0,0,8.0,HandleRightMountX-HandleLeftMountX+0.4);
            foreach(double x in new[]{HandleLeftMountX,HandleRightMountX})
            {
                Body2 basePlate=cad.Box(x,-4.0,-9.0,18.0,2.0,18.0);
                Body2 endArm=cad.Cylinder(x,-3.1,0,0,1,0,8.0,8.2);
                basePlate=Unite(basePlate,endArm,"P9 mounting base and end arm");
                basePlate=cad.Cut(basePlate,cad.Cylinder(x,-5.3,0,0,1,0,4.5,4.2),
                    "P9 M4 mounting hole through completed base");
                handle=Unite(handle,basePlate,"folding handle end assembly");
            }
            cad.AddBody(doc,handle,"Southco P9 128 mm two-hole folding handle envelope");
            cad.ApplyMaterial(doc,"6061-T6 (SS)",Graphite);
            cad.Property(doc,"Selected hardware","Southco P9-128-40-M4N-15-3 or exact approved equivalent; internal-threaded black aluminium folding handle");
            cad.Property(doc,"Mount pattern","Two M4 mounting axes at x -60/+68 mm; 128 mm centres; offset preserves one-row MIDI and two-row audio service clearances");
            cad.Property(doc,"Published boundary","600 N maximum static load and 1.7 N m maximum tightening torque are supplier catalogue limits, not case proof-test results");
            cad.Property(doc,"CAD boundary","Simplified folded envelope; import exact supplier STEP before production drawing release");
            return SavePart(cad,doc,HandleStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateHandleSpreader(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(HandleSpreaderStem);
        try
        {
            Body2 b=cad.Box((HandleLeftMountX+HandleRightMountX)/2.0,
                0,29.0,160.0,4.0,32.0);
            foreach(double x in new[]{HandleLeftMountX,HandleRightMountX})
                b=HoleY(cad,b,x,45.0,4.2,4.0,"M4 simplified thread envelope");
            cad.AddBody(doc,b,"160 x 32 x 4 mm 6061 handle load spreader");
            cad.ApplyMaterial(doc,"6061-T6 (SS)",NaturalAluminium);
            cad.Property(doc,"Load path","P9 handle -> two M4 full-shank screws -> 2 mm upper edge -> 4 mm x 160 mm spreader");
            cad.Property(doc,"Torque","Assembly torque must not exceed Southco catalogue limit 1.7 N m unless vendor approves otherwise");
            return SavePart(cad,doc,HandleSpreaderStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateHandleFastenerPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(HandleFastenerPackStem);
        try
        {
            foreach(double x in new[]{HandleLeftMountX,HandleRightMountX})
            {
                Body2 s=cad.Cylinder(x,203.95,45.0,0,1,0,3.8,9.05);
                s=Unite(s,cad.Cylinder(x,203.0,45.0,0,1,0,7.2,1.0),"internal P9 low-profile M4 head");
                cad.AddBody(doc,s,"P9 M4 A4-80 handle screw");
            }
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Count","Two M4 A4-80 screws; screw length panel stack plus 5 mm engagement per Southco guidance");
            return SavePart(cad,doc,HandleFastenerPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateCassetteFastenerPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(CassetteFastenerPackStem);
        try
        {
            int count=0;
            foreach(CassetteMount c in new[]{
                new CassetteMount(-218.5,41.0),new CassetteMount(MidiCassetteCentreX,43.0),
                new CassetteMount(AudioCassetteCentreX,86.0)})
                foreach(double sx in Signs()) foreach(double z in new[]{20.0,90.0})
                {
                    double x=c.CentreX+sx*c.HalfPitch;
                    Body2 s=cad.Cylinder(x,207.8,z,0,1,0,2.8,5.4);
                    s=Unite(s,cad.Cylinder(x,212.2,z,0,1,0,5.5,1.0),"cassette low-profile head");
                    cad.AddBody(doc,s,"M3 cassette screw"); count++;
                }
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Count",count+" M3 A4-70 screws for three independently removable top cassettes");
            cad.Property(doc,"Serviceability","Adapter, MIDI/USB and audio plates can be removed independently without disturbing the case frame");
            return SavePart(cad,doc,CassetteFastenerPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateShellCornerFastenerPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(ShellCornerFastenerPackStem);
        try
        {
            int count=0;
            foreach(double sx in Signs()) foreach(double y in new[]{-209.0,209.0})
                foreach(double z in new[]{30.0,80.0})
                {
                    Body2 s=cad.Cylinder(sx*266.2,y,z,sx,0,0,3.8,9.8);
                    s=Unite(s,cad.Cylinder(sx*275.0,y,z,sx,0,0,7.2,1.0),"external corner screw head");
                    cad.AddBody(doc,s,"M4 shell corner screw"); count++;
                }
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Count",count+" M4 A4-80 upper/lower edge-to-side-frame corner screws");
            cad.Property(doc,"Load path","Two independent M4 screws at each of four corners; no friction-only face contact");
            return SavePart(cad,doc,ShellCornerFastenerPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateContinuousLid(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(LidStem);
        try
        {
            double cavityW=OverallBossWidth+1.0;
            double cavityH=CaseHeight+1.0;
            double outerW=cavityW+2*LidThickness;
            double outerH=cavityH+2*LidThickness;
            Body2 lid=cad.Box(0,0,LidFaceZ-LidThickness,outerW,outerH,LidThickness+0.02);
            foreach(double ribY in new[]{-120.0,120.0})
                lid=Unite(lid,cad.Box(0,ribY,LidFaceZ-LidThickness-1.2,
                    outerW-40.0,8.0,1.22),"anti-drum formed bead");
            foreach(double sx in Signs())
            {
                double cx=sx*(cavityW/2+LidThickness/2);
                Body2 ret=cad.Box(cx,0,LidFaceZ-0.02,LidThickness,cavityH,LidSkirtDepth+0.02);
                ret=cad.Cut(ret,cad.Box(cx,(LidReliefMinY+LidReliefMaxY)/2,
                    LidReliefMinZ,LidThickness+1.0,LidReliefMaxY-LidReliefMinY,
                    LidReliefMaxZ-LidReliefMinZ),"folded stand relief");
                foreach(double sy in Signs()) foreach(double oy in new[]{-6.0,6.0})
                    foreach(double z in new[]{LatchBodyLowerRowZ,LatchBodyUpperRowZ})
                        ret=cad.Cut(ret,cad.Cylinder(sx*287.8,sy*LatchCentreY+oy,z,
                            sx,0,0,LatchMountHole,2.2),"V7 latch M3 through-hole");
                lid=Unite(lid,ret,"continuous side return and welded corner");
            }
            foreach(double sy in Signs())
                lid=Unite(lid,cad.Box(0,sy*(cavityH/2+LidThickness/2),
                    LidFaceZ-0.02,outerW,LidThickness,LidSkirtDepth+0.02),
                    "continuous top/bottom return and welded corner");

            // The folded 124 mm front links deliberately stay full-section
            // and clear the custom closed-spine bottom rail by folding to -100 deg.
            // Two shallow local doghouse pockets preserve a continuous lid
            // return while protecting those links during transport.  A bare
            // open notch would interrupt the return and leave the links open
            // to impact, so it is not used.
            double bottomReturnY=-(cavityH/2+LidThickness/2);
            foreach(double sx in Signs())
            {
                double pocketX=sx*FrontFootPlaneX;
                lid=cad.Cut(lid,cad.Box(pocketX,bottomReturnY,-49.8,
                    14.0,LidThickness+0.8,63.8),"front-link transport pocket opening");
                lid=Unite(lid,cad.Box(pocketX,-224.9,-50.2,
                    14.4,26.8,1.6),"front-link pocket continuous bottom bridge");
                foreach(double side in Signs())
                    lid=Unite(lid,cad.Box(pocketX+side*6.6,-224.9,-49.8,
                        1.2,26.8,61.8),"front-link pocket formed side wall");
                lid=Unite(lid,cad.Box(pocketX,-238.8,-49.8,
                    14.4,1.2,61.8),"front-link pocket protective outer wall");
            }

            cad.AddBody(doc,lid,"one continuous welded 5052 travel-lid subassembly");
            cad.ApplyMaterial(doc,"5052-H32",Graphite);
            cad.Property(doc,"Construction","Five laser-cut/brake-formed 1.2 mm 5052 panels with continuous TIG corner seams, ground exterior and two formed anti-drum beads; modeled as one continuous solid");
            cad.Property(doc,"Patch clearance","70 mm front clearance and 82 mm perimeter guidance; two welded/formed local doghouse pockets protect the full-section folded front links; lid remains detached in the final showcase");
            cad.Property(doc,"Latch pattern","Four Southco V7 small sites, each four M3 holes on 12 x 12 mm pattern; 30.0 mm body-to-keeper mounting separation");
            cad.Property(doc,"Stand relief","Bilateral side-return relief retained for the folded 262 mm rear legs; the -100 degree front links retain about 2.0 mm pocket-wall and 1.5 mm pocket-bottom CAD clearance, verified by assembly interference test");

            cad.Application.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swDisplayDecals,true);
            doc.Extension.DeleteAllDecals();
            Face2 outer,inner;
            FindLargeLidFaces(doc as PartDoc,out outer,out inner);
            string template=FindDecalTemplate();
            AddDecal(doc,outer,template,Path.Combine(cad.Root,"logo","rymovia-timegrid-v09.png"),
                0.579,0.423,0,0,FaceCentreZ(outer),"Rymovia Time Grid exterior");
            AddDecal(doc,outer,template,Path.Combine(cad.Root,"logo","logo-mark-white.png"),
                0.075,0.088,-0.205,0.135,FaceCentreZ(outer),"Rymovia mark");
            AddDecal(doc,inner,template,Path.Combine(cad.Root,"logo","logo-lockup-white.png"),
                0.200,0.0505,0,-0.155,FaceCentreZ(inner),"Rymovia inner lockup");
            Require(doc.Extension.GetDecalsCount()==3,"V11 lid decals are incomplete");
            return SavePart(cad,doc,LidStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateCaseLatchPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(CaseLatchPackStem);
        try
        {
            int count=0;
            foreach(double sx in Signs()) foreach(double sy in Signs())
            {
                double y=sy*LatchCentreY;
                Body2 bracket=cad.Box(sx*289.9,y,16.0,2.0,LatchBodyWidth,16.0);
                foreach(double oy in new[]{-6.0,6.0})
                {
                    Body2 rib=cad.Box(sx*282.0,y+oy,21.5,14.0,5.0,5.0);
                    rib=cad.Cut(rib,cad.Cylinder(sx*274.6,y+oy,LatchCaseMountZ,
                        sx,0,0,LatchMountHole,16.0),"keeper bridge M3 hole");
                    bracket=Unite(bracket,rib,"keeper compression bridge");
                }
                foreach(double oy in new[]{-13.5,13.5})
                    bracket=Unite(bracket,cad.Box(sx*292.0,y+oy,22.0,4.0,3.0,9.0),
                        "keeper bar support outside screw heads");
                bracket=Unite(bracket,cad.Cylinder(sx*294.0,y-13.5,KeeperBarZ,
                    0,1,0,KeeperBarDiameter,27.0),"positive keeper bar");
                foreach(double oy in new[]{-6.0,6.0})
                    bracket=cad.Cut(bracket,cad.Cylinder(sx*288.6,y+oy,LatchCaseMountZ,
                        sx,0,0,LatchMountHole,3.0),"keeper plate M3 hole");
                cad.AddBody(doc,bracket,(sx<0?"Left":"Right")+" "+(sy<0?"lower":"upper")+" V7 keeper bracket");
                count++;
            }
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Hardware envelope","Southco V7 small V7-10-105-50 keeper interface; latch body envelope 31 x 72 mm, keeper two M3 holes at 12 mm spacing");
            cad.Property(doc,"Load path","Keeper bar -> welded support arms outside screw-head sweep -> 2 mm keeper plate -> two hollow-drilled 13 mm compression bridges -> 4 mm side band");
            cad.Property(doc,"Working-load boundary","Southco family catalogue maximum working load 1200 N; case and substrate require independent proof/cycle/drop tests");
            cad.Property(doc,"Count",count+" positive keeper assemblies");
            return SavePart(cad,doc,CaseLatchPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateCaseLatchFastenerPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(CaseLatchFastenerPackStem);
        try
        {
            int count=0;
            foreach(double sx in Signs()) foreach(double sy in Signs()) foreach(double oy in new[]{-6.0,6.0})
            {
                double y=sy*LatchCentreY+oy;
                Body2 s=cad.Cylinder(sx*271.05,y,LatchCaseMountZ,sx,0,0,LatchShaft,22.25);
                s=Unite(s,cad.Cylinder(sx*292.2,y,LatchCaseMountZ,sx,0,0,5.5,1.0),"external M3 locknut/head");
                cad.AddBody(doc,s,"M3 A4-70 keeper through fastener"); count++;
            }
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Count",count+" M3 A4-70 full-stack keeper fasteners");
            cad.Property(doc,"Interference repair","Heads are centred y +/-6; keeper supports begin at |y|=12 so every head has at least 3.25 mm edge gap");
            return SavePart(cad,doc,CaseLatchFastenerPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateLidLatchDoublerPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(LidLatchDoublerPackStem);
        try
        {
            int count=0;
            foreach(double sx in Signs()) foreach(double sy in Signs())
            {
                double y=sy*LatchCentreY;
                Body2 d=cad.Box(sx*290.5,y,-62.0,2.0,35.0,76.0);
                foreach(double oy in new[]{-6.0,6.0}) foreach(double z in new[]{-18.0,-6.0})
                    d=cad.Cut(d,cad.Cylinder(sx*289.2,y+oy,z,sx,0,0,LatchMountHole,2.6),"V7 doubler M3 hole");
                cad.AddBody(doc,d,"2 mm 5052 lid latch doubler"); count++;
            }
            cad.ApplyMaterial(doc,"5052-H32",Graphite);
            cad.Property(doc,"Count",count+" independent 35 x 76 x 2 mm doublers; local lid stack 3.2 mm");
            cad.Property(doc,"Manufacturing","Separate laser-cut doublers bonded only for sealing and retained structurally by four M3 through fasteners");
            return SavePart(cad,doc,LidLatchDoublerPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateLidLatchBodyPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(LidLatchBodyPackStem);
        try
        {
            int count=0;
            foreach(double sx in Signs()) foreach(double sy in Signs())
            {
                double y=sy*LatchCentreY;
                Body2 b=cad.Box(sx*293.0,y,LatchBodyMinZ,3.0,LatchBodyWidth,LatchBodyLength);
                foreach(double oy in new[]{-6.0,6.0}) foreach(double z in new[]{-18.0,-6.0})
                    b=cad.Cut(b,cad.Cylinder(sx*291.2,y+oy,z,sx,0,0,LatchMountHole,3.6),"V7 body M3 hole");
                cad.AddBody(doc,b,"Southco V7 small 31 x 72 mm closed-body envelope"); count++;
            }
            cad.ApplyMaterial(doc,"AISI 304",Graphite);
            cad.Property(doc,"Selected hardware","Southco V7-10-105-50 black powder-coated zinc-alloy small draw latch or exact approved equivalent");
            cad.Property(doc,"Exact envelope","31 mm width x 72 mm length; four diameter-3.4 CAD holes on 12 x 12 mm pattern");
            cad.Property(doc,"CAD boundary","Simplified closed-state envelope; exact supplier STEP and opening sweep are mandatory before production machining");
            cad.Property(doc,"Count",count+" latch bodies");
            return SavePart(cad,doc,LidLatchBodyPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateLidLatchBailPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(LidLatchBailPackStem);
        try
        {
            int count=0;
            foreach(double sx in Signs()) foreach(double sy in Signs())
            {
                double y=sy*LatchCentreY;
                Body2 bail=cad.Cylinder(sx*295.8,y-15.0,4.0,0,1,0,BailWireDiameter,30.0);
                foreach(double oy in new[]{-15.0,15.0})
                    bail=Unite(bail,cad.Cylinder(sx*295.8,y+oy,4.0,0,0,1,
                        BailWireDiameter,BailContactZ-4.0),"bail side arm");
                bail=Unite(bail,cad.Cylinder(sx*295.8,y-15.0,BailContactZ,
                    0,1,0,BailWireDiameter,30.0),"closed bail contact bar");
                cad.AddBody(doc,bail,"closed-state stainless latch bail"); count++;
            }
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Closed contact","2.5 mm bail centre z32.25 is tangent to 4 mm keeper centre z29; no solid overlap");
            cad.Property(doc,"Body clearance","Simplified bail centre x +/-295.8 leaves 0.05 mm display clearance from the simplified body envelope; exact vendor STEP governs the production mechanism");
            cad.Property(doc,"Keeper clearance","Bail side-arm axes y +/-15 lie beyond the 27 mm keeper-bar ends; only the closed contact bar is tangent to the keeper bar");
            cad.Property(doc,"Count",count+" closed-state bails");
            return SavePart(cad,doc,LidLatchBailPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateLidLatchFastenerPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(LidLatchFastenerPackStem);
        try
        {
            int count=0;
            foreach(double sx in Signs()) foreach(double sy in Signs())
                foreach(double oy in new[]{-6.0,6.0}) foreach(double z in new[]{-18.0,-6.0})
                {
                    double y=sy*LatchCentreY+oy;
                    Body2 s=cad.Cylinder(sx*288.1,y,z,sx,0,0,LatchShaft,7.8);
                    s=Unite(s,cad.Cylinder(sx*295.0,y,z,sx,0,0,5.5,0.9),"external latch fastener head");
                    cad.AddBody(doc,s,"M3 A4-70 latch/doubler/lid fastener"); count++;
                }
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Count",count+" M3 A4-70 through fasteners; flush inner side preserves lid guide clearance");
            return SavePart(cad,doc,LidLatchFastenerPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateLidCompressionPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(LidCompressionPackStem);
        try
        {
            int count=0;
            foreach(double sx in Signs()) foreach(double sy in Signs())
            {
                double y=sy*LatchCentreY;
                foreach(double oy in new[]{-10.0,10.0})
                {
                    cad.AddBody(doc,cad.Box(sx*289.4,y+oy,14.0,3.8,5.0,2.0),"closed-state EPDM preload pad");
                    count++;
                }
                cad.AddBody(doc,cad.Box(sx*289.4,y,14.0,3.8,4.0,2.0),"metal compression hard stop");
                count++;
            }
            cad.ApplyMaterial(doc,"NEOPRENE",RubberBlack);
            cad.Property(doc,"Compression","Eight 70A EPDM pads nominally compress to 2.0 mm; four adjacent metal stops prevent creep over-compression");
            cad.Property(doc,"Mixed-material note","This pack is a positional assembly envelope; production metal stops are 5052 and pads are separately bonded EPDM");
            cad.Property(doc,"Count",count+" bodies");
            return SavePart(cad,doc,LidCompressionPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateBackSkin(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(BackSkinStem);
        try
        {
            Body2 skin=cad.Box(0,0,108.5,CaseWidth,CaseHeight,BackSkinThickness);
            foreach(double sx in Signs()) foreach(double sy in Signs())
                skin=HoleZ(cad,skin,sx*50.0,sy*50.0,VesaClearanceHole,107.7,2.8,
                    "VESA 100 M4 clearance");
            foreach(double x in BackFastenerX) foreach(double sy in Signs())
                skin=HoleZ(cad,skin,x,sy*206.0,4.5,107.7,2.8,
                    "rear perimeter M4 clearance");
            foreach(double sx in Signs()) foreach(double sy in Signs())
                skin=HoleZ(cad,skin,sx*245.0,sy*185.0,4.5,107.7,2.8,
                    "captured rear foot M4 clearance");
            cad.AddBody(doc,skin,"separate 1.5 mm 5052 rear shear skin");
            cad.ApplyMaterial(doc,"5052-H32",Graphite);
            cad.Property(doc,"Manufacturing","Independent flat/edge-formed 1.5 mm 5052-H32 skin; no fictitious fused 0.5 mm step");
            cad.Property(doc,"Perimeter joint","Twelve M4 screws into upper/lower formed tabs at y +/-206 mm");
            cad.Property(doc,"VESA","Four 4.5 mm clearances at 100 x 100 mm; separate 0.5 mm doubler and one-piece load frame are distinct BOM items");

            cad.Application.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swDisplayDecals,true);
            doc.Extension.DeleteAllDecals();
            Face2 outer=FindPositiveZFace(doc as PartDoc);
            AddDecal(doc,outer,FindDecalTemplate(),
                Path.Combine(cad.Root,"logo","rymovia-phase-halo-rear-v10.png"),
                0.548,0.420,0,0,FaceCentreZ(outer),"Rymovia Phase Halo rear identity");
            Require(doc.Extension.GetDecalsCount()==1,"V11 rear artwork is missing");
            return SavePart(cad,doc,BackSkinStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateBackDoubler(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(BackDoublerStem);
        try
        {
            Body2 d=cad.Box(0,0,108.0,BackDoublerSize,BackDoublerSize,BackDoublerThickness);
            foreach(double sx in Signs()) foreach(double sy in Signs())
                d=HoleZ(cad,d,sx*50.0,sy*50.0,VesaClearanceHole,107.7,1.1,
                    "VESA doubler M4 clearance");
            cad.AddBody(doc,d,"separate 160 x 160 x 0.5 mm VESA doubler");
            cad.ApplyMaterial(doc,"5052-H32",NaturalAluminium);
            cad.Property(doc,"Assembly","Bonded only for sealing/anti-rattle and clamped by four VESA M4 screws; manufacture as separate stock thickness");
            cad.Property(doc,"Local stack","1.5 mm rear skin plus 0.5 mm doubler equals 2.0 mm at the four VESA holes");
            return SavePart(cad,doc,BackDoublerStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateBackPerimeterFastenerPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(BackPerimeterFastenerPackStem);
        try
        {
            int count=0;
            foreach(double x in BackFastenerX) foreach(double sy in Signs())
            {
                Body2 s=cad.Cylinder(x,sy*206.0,110.0,0,0,-1,3.8,13.0);
                s=Unite(s,cad.Cylinder(x,sy*206.0,110.0,0,0,1,7.2,1.0),"rear M4 low-profile head");
                cad.AddBody(doc,s,"M4 A4-80 rear perimeter screw"); count++;
            }
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Count",count+" M4 A4-80 perimeter screws, six at each long edge");
            cad.Property(doc,"Sealing","Use bonded EPDM sealing washers outside the artwork keep-out and thread locking compatible with service removal");
            return SavePart(cad,doc,BackPerimeterFastenerPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateBackFeetPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(BackFeetPackStem);
        try
        {
            foreach(double sx in Signs()) foreach(double sy in Signs())
            {
                Body2 foot=cad.Cylinder(sx*245.0,sy*185.0,110.0,0,0,1,22.0,8.0);
                foot=cad.Cut(foot,cad.Cylinder(sx*245.0,sy*185.0,109.7,0,0,1,4.5,8.6),"M4 rear foot clearance");
                cad.AddBody(doc,foot,"captured EPDM rear foot");
            }
            cad.ApplyMaterial(doc,"NEOPRENE",RubberBlack);
            cad.Property(doc,"Count","Four replaceable 22 x 8 mm EPDM feet, each positively retained by one M4 screw and large washer");
            cad.Property(doc,"Artwork keepout","Centres x +/-245,y +/-185 remain inside the existing Phase Halo R12 keep-outs");
            return SavePart(cad,doc,BackFeetPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateBackFeetFastenerPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(BackFeetFastenerPackStem);
        try
        {
            foreach(double sx in Signs()) foreach(double sy in Signs())
            {
                Body2 s=cad.Cylinder(sx*245.0,sy*185.0,107.8,0,0,1,3.8,11.0);
                s=Unite(s,cad.Cylinder(sx*245.0,sy*185.0,118.0,0,0,1,12.0,1.0),"rear foot retaining washer");
                cad.AddBody(doc,s,"M4 rear foot retaining screw and washer");
            }
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Retention","Four M4 A4-70 screws plus diameter-12 washers; feet are not adhesive-only");
            return SavePart(cad,doc,BackFeetFastenerPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateVesaFrame(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(VesaFrameStem);
        try
        {
            Body2 frame=cad.Box(0,-155.0,98.98,542.0,12.0,6.04);
            foreach(double sx in Signs())
                frame=Unite(frame,cad.Box(sx*115.0,0,93.0,10.0,322.0,6.02),
                    "VESA vertical stile");
            frame=Unite(frame,cad.Box(0,155.0,98.98,542.0,12.0,6.04),
                "side-to-side VESA crossbeam");
            foreach(double sy in Signs())
                frame=Unite(frame,cad.Box(0,sy*50.0,99.0,240.0,10.0,9.0),
                    "VESA local bridge");
            foreach(double sx in Signs()) foreach(double sy in Signs())
                frame=HoleZ(cad,frame,sx*50.0,sy*50.0,4.2,98.7,10.0,
                    "VESA M4 simplified thread envelope");
            foreach(double sx in Signs()) foreach(double sy in Signs())
                frame=HoleXFromEnd(cad,frame,sx*271.0,sy*155.0,102.0,-sx,4.2,14.0,
                    "VESA side-tie M4 simplified thread envelope");
            cad.AddBody(doc,frame,"one continuous 6061 VESA ladder frame");
            cad.ApplyMaterial(doc,"6061-T6 (SS)",DarkAluminium);
            cad.Property(doc,"Construction","One welded/machined 6061 load frame replaces six merely touching loose members; two stiles, two local bridges and two crossbeams are one solid load path");
            cad.Property(doc,"VESA load path","Bracket M4 -> rear skin -> separate 0.5 mm doubler -> 9 mm local bridge -> stiles -> full-width crossbeams -> four M4 side-frame ties");
            cad.Property(doc,"Power keepout","Bridge inner edges remain y +/-45 and stiles remain x +/-110..120, outside the central 210 x 90 mm reserved PSU plan area");
            cad.Property(doc,"Validation boundary","VESA arm rating, M4 engagement, pullout, cyclic bending and loaded-case proof remain physical gates");
            return SavePart(cad,doc,VesaFrameStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateVesaFastenerPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(VesaFastenerPackStem);
        try
        {
            int count=0;
            foreach(double sx in Signs()) foreach(double sy in Signs())
            {
                Body2 v=cad.Cylinder(sx*50.0,sy*50.0,110.0,0,0,-1,3.8,13.0);
                v=Unite(v,cad.Cylinder(sx*50.0,sy*50.0,110.0,0,0,1,8.0,1.0),"VESA M4 washer/head");
                cad.AddBody(doc,v,"VESA M4 bracket screw envelope"); count++;
                Body2 tie=cad.Cylinder(sx*276.2,sy*155.0,102.0,-sx,0,0,3.8,18.0);
                tie=Unite(tie,cad.Cylinder(sx*276.2,sy*155.0,102.0,-sx,0,0,7.2,1.0),"VESA side-tie head");
                cad.AddBody(doc,tie,"M4 VESA frame side tie"); count++;
            }
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Count","Four external VESA M4 bracket screws plus four internal frame-to-side M4 ties");
            cad.Property(doc,"External screw limit","Use only bracket screws whose engagement matches the selected arm; Intellijel reference specifies M4 8-10 mm for its case, but this case requires its own stack calculation");
            return SavePart(cad,doc,VesaFastenerPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateFrontBracketPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(FrontBracketPackStem);
        try
        {
            Point stop=new Point(0,FrontStopCaseY,FrontStopCaseZ);
            Point lockPoint=new Point(0,FrontLockCaseY,FrontLockCaseZ);
            foreach(double sx in Signs())
            {
                Body2 bracket=cad.Box(sx*FrontFootPlaneX-FrontBracketCheekOffsetX,
                    -199.25,44.0,4.0,16.5,62.5);
                bracket=Unite(bracket,cad.Box(sx*FrontFootPlaneX,-191.5,98.0,
                    34.0,2.0,8.0),"front U-bracket upper remote cheek bridge");
                bracket=Unite(bracket,cad.Box(sx*FrontFootPlaneX+FrontBracketCheekOffsetX,
                    -199.25,44.0,4.0,16.5,62.5),"front U-bracket second cheek");
                foreach(double cheekX in new[]{sx*FrontFootPlaneX-FrontBracketCheekOffsetX,
                    sx*FrontFootPlaneX+FrontBracketCheekOffsetX})
                {
                    bracket=Unite(bracket,cad.Cylinder(cheekX-2.0,
                        FrontPivotCaseY,FrontPivotCaseZ,1,0,0,24.0,4.0),
                        "front pivot full-bearing island");
                    bracket=Unite(bracket,cad.Cylinder(cheekX-2.0,
                        stop.Y,stop.Z,1,0,0,20.0,4.0),
                        "front hard-stop full-bearing island");
                    bracket=Unite(bracket,cad.Cylinder(cheekX-2.0,
                        lockPoint.Y,lockPoint.Z,1,0,0,16.0,4.0),
                        "front lock full-bearing island");
                    bracket=Unite(bracket,cad.Box(cheekX,-209.0,44.0,
                        4.0,12.0,32.0),
                        "4 mm full-thickness front-lock impact neck");
                }
                foreach(double dx in new[]{-FrontBracketMountOffsetX,FrontBracketMountOffsetX})
                    foreach(double z in FrontBracketMountZ)
                    {
                        double wingCentre=dx<0?-13.2:13.2;
                        bracket=Unite(bracket,cad.Box(sx*FrontFootPlaneX+wingCentre,
                            -205.95,z-4.0,17.6,4.1,8.0),
                            "outside-window independent lower-edge mounting wing");
                    }
                foreach(double dx in new[]{-FrontBracketMountOffsetX,FrontBracketMountOffsetX}) foreach(double z in FrontBracketMountZ)
                    bracket=HoleYGlobal(cad,bracket,sx*FrontFootPlaneX+dx,-208.4,z,
                        4.5,5.0,"front bracket M4 mount");
                bracket=HoleXGlobal(cad,bracket,sx*FrontFootPlaneX,FrontPivotCaseY,
                    FrontPivotCaseZ,8.2,20.0,"front pivot through-hole");
                bracket=HoleXGlobal(cad,bracket,sx*FrontFootPlaneX,stop.Y,stop.Z,
                    8.2,20.0,"front hard-stop through-hole");
                bracket=HoleXGlobal(cad,bracket,sx*FrontFootPlaneX,lockPoint.Y,lockPoint.Z,
                    6.4,20.0,"front captive lock screw");
                bracket=cad.Cut(bracket,cad.Cylinder(sx*FrontFootPlaneX+sx*6.5,
                    lockPoint.Y,lockPoint.Z,sx,0,0,10.2,2.1),
                    "front lock external-head counterbore");
                cad.AddBody(doc,bracket,(sx<0?"Left":"Right")+" internal double-shear anti-tip U bracket");
            }
            cad.ApplyMaterial(doc,"6061-T6 (SS)",DarkAluminium);
            cad.Property(doc,"Mounting","Two internal U brackets, four M4 lower-edge screws each; bracket inner face touches y=-208 lower-edge inner face");
            cad.Property(doc,"Support function","Deploys two front pads to world longitudinal coordinate approximately -141.57 mm with the 20 mm crowns truly tangent to the desk");
            cad.Property(doc,"Load separation","8 mm hardened stop axles travel in real swept guide slots and carry end contact; 6 mm captive screws retain position only");
            cad.Property(doc,"Lock reinforcement","Each 16 mm lock island has at least 4.0 mm primary-cheek overlap plus a 4 x 12 x 32 mm full-thickness connection neck inside the lower-edge sweep window");
            return SavePart(cad,doc,FrontBracketPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateFrontLink(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(FrontLinkStem);
        try
        {
            double deploy=FrontDeployedAngle();
            double folded=FrontFoldedAngle();
            Point deployLockLocal=FrontLockDeployLocalPoint();
            Point fixedLock=RotateFromPivot(0,0,deployLockLocal.Y,
                deployLockLocal.Z,deploy);
            Point foldedHole=InverseRotate(fixedLock.Y,fixedLock.Z,folded);
            Body2 link=cad.Cylinder(-4,0,0,1,0,0,24.0,8.0);
            double armLength=FrontLinkLength-15.5;
            link=Unite(link,cad.Box(0,armLength/2,-10.0,8.0,
                armLength,20.0),"8 x 20 mm front anti-tip arm ending before captured foot neck");
            link=Unite(link,cad.Box(0,11.0,FrontSweptIslandBaseZ,8.0,22.0,
                FrontSweptIslandDepth),
                "reinforced swept-stop load island");
            link=Unite(link,cad.Box(0,
                (FrontLinkRibLeftCentreY+FrontLinkRibRightCentreY)/2.0,
                FrontLinkRibBoxBaseZ,8.0,
                FrontLinkRibRightCentreY-FrontLinkRibLeftCentreY,
                FrontLinkRibBoxDepth),
                "rounded monolithic mid-span strength rib web");
            foreach(double ribY in new[]{FrontLinkRibLeftCentreY,
                FrontLinkRibRightCentreY})
                link=Unite(link,cad.Cylinder(-4.0,ribY,FrontLinkRibCentreZ,
                    1,0,0,FrontLinkRibRadius*2.0,8.0),
                    "rounded monolithic mid-span strength rib end");
            foreach(Point p in new[]{deployLockLocal,
                new Point(0,foldedHole.Y,foldedHole.Z)})
                link=Unite(link,cad.Cylinder(-4.0,p.Y,p.Z,1,0,0,15.0,8.0),
                    "reinforced front position-lock island");
            link=Unite(link,cad.Box(0,FrontLinkLength-8.0,-7.0,6.0,16.0,14.0),"front foot neck");
            link=Unite(link,cad.Box(0,FrontLinkLength-2.0,-8.0,7.6,8.0,16.0),"front foot overmould mushroom");
            link=cad.Cut(link,cad.Cylinder(-4.4,0,0,1,0,0,8.2,8.8),"front pivot clearance");
            double stopDy=FrontStopCaseY-FrontPivotCaseY;
            double stopDz=FrontStopCaseZ-FrontPivotCaseZ;
            const int stopSweepSamples=17;
            for(int i=0;i<stopSweepSamples;i++)
            {
                double t=(double)i/(stopSweepSamples-1);
                double a=folded+(deploy-folded)*t;
                Point slot=InverseRotate(stopDy,stopDz,a);
                link=cad.Cut(link,cad.Cylinder(-4.4,slot.Y,slot.Z,1,0,0,
                    8.2,8.8),"front hardened stop swept guide slot");
            }
            foreach(Point p in new[]{deployLockLocal,
                new Point(0,foldedHole.Y,foldedHole.Z)})
                link=cad.Cut(link,cad.Cylinder(-4.4,p.Y,p.Z,1,0,0,6.4,8.8),"front deployed/folded lock hole");
            cad.AddBody(doc,link,"124 mm 17-4PH front anti-tip link with swept hardened-stop guide slot");
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            double mass=ReadMass(doc);
            Require(mass>=0.1720 && mass<=0.1745,
                "front anti-tip link mass is outside the restored-strength/lightweight window; actual="+
                F(mass));
            cad.Property(doc,"Production material","17-4PH H900; AISI304 used only as SOLIDWORKS mass proxy");
            cad.Property(doc,"Length","124 mm pivot-to-pad centre; 8 x 20 mm arm restores section margin after the support-span increase");
            cad.Property(doc,"Hard stop","A 17-body true swept guide slot clears the retained 8 mm axle throughout folding and bears at its deployed endpoint; a 257-point continuous analytic screen gates the surrounding ligaments; the M6 position screw is unloaded in normal contact");
            cad.Property(doc,"Mid-span reinforcement","Integral rounded 8 mm-wide rib from local y=30.25..100.75 mm restores the effective material removed by relocating the old module-facing stop island without reducing deployed desk clearance");
            cad.Property(doc,"Native CAD mass",F(mass)+" kg per link using the AISI304 density proxy");
            cad.Property(doc,"Storage","Folds on a -100 degree lower corridor at x +/-205 with pad centre case y=-224.032,z=-37.116; both full-size lock islands remain at least 4.5 mm behind the 85 mm module envelope while the link clears the 22 mm deep custom bottom rail/thread strip");
            return SavePart(cad,doc,FrontLinkStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateFrontFoot(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(FrontFootStem);
        try
        {
            Body2 p=cad.Cylinder(-4.2,0,0,1,0,0,FrontFootDiameter,8.4);
            p=cad.Cut(p,cad.Box(0,-6.5,-7.2,6.4,7.0,14.4),"front pad narrow mould entry");
            p=cad.Cut(p,cad.Box(0,-2.0,-8.2,7.9,8.4,16.4),"front pad internal undercut");
            cad.AddBody(doc,p,"70A EPDM overmould-captured front anti-tip pad");
            cad.ApplyMaterial(doc,"NEOPRENE",RubberBlack);
            cad.Property(doc,"Desk contact","20 mm round crown; deployed centre maps to world desk height 10 mm, exactly one pad radius, and longitudinal support approximately -141.57 mm at 60 degrees");
            cad.Property(doc,"Friction gate","CAD does not establish friction; physical acceptance must demonstrate effective mu >=0.73 on specified contaminated desktop surfaces for the 20 N/SF2.0 screening case");
            return SavePart(cad,doc,FrontFootStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateFrontPivotPack(RackCadSession cad)
    {
        return CreateFrontAxlePack(cad,FrontPivotPackStem,FrontPivotCaseY,
            FrontPivotCaseZ,7.8,10.0,"front pivot");
    }

    private static string CreateFrontStopPack(RackCadSession cad)
    {
        return CreateFrontAxlePack(cad,FrontStopPackStem,
            FrontStopCaseY,FrontStopCaseZ,7.8,10.0,
            "front positive hard stop");
    }

    private static string CreateFrontAxlePack(RackCadSession cad,string stem,double y,
        double z,double shaftDia,double retainerDia,string label)
    {
        ModelDoc2 doc=cad.NewPart(stem);
        try
        {
            foreach(double sx in Signs())
            {
                double centre=sx*FrontFootPlaneX;
                Body2 p=cad.Cylinder(centre-8.7,y,z,1,0,0,shaftDia,17.4);
                p=Unite(p,cad.Cylinder(centre-9.7,y,z,1,0,0,retainerDia,1.0),label+" first external retainer");
                p=Unite(p,cad.Cylinder(centre+8.7,y,z,1,0,0,retainerDia,1.0),label+" second external retainer");
                cad.AddBody(doc,p,(sx<0?"Left":"Right")+" retained "+label+" axle");
            }
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Production","17-4PH full-shank retained axle; no threads in the two bracket shear planes");
            cad.Property(doc,"Axial fit","17.4 mm grip spans the 16.8 mm bracket outside faces with 0.30 mm clearance per retainer");
            return SavePart(cad,doc,stem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateFrontLockPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(FrontLockPackStem);
        try
        {
            Point p=new Point(0,FrontLockCaseY,FrontLockCaseZ);
            foreach(double sx in Signs())
            {
                Body2 s=cad.Cylinder(sx*FrontFootPlaneX-8.2,p.Y,p.Z,1,0,0,5.8,16.4);
                s=Unite(s,cad.Cylinder(sx*FrontFootPlaneX+sx*6.7,p.Y,p.Z,
                    sx,0,0,10.0,1.5),"outward flush captive front lock head");
                cad.AddBody(doc,s,(sx<0?"Left":"Right")+" M6 front position screw");
            }
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Function","Flush captive M6 shoulder screw locks folded/deployed holes; separate 8 mm stop axle carries contact load");
            return SavePart(cad,doc,FrontLockPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string CreateFrontBracketFastenerPack(RackCadSession cad)
    {
        ModelDoc2 doc=cad.NewPart(FrontBracketFastenerPackStem);
        try
        {
            int count=0;
            foreach(double sx in Signs()) foreach(double dx in new[]{-FrontBracketMountOffsetX,FrontBracketMountOffsetX})
                foreach(double z in FrontBracketMountZ)
                {
                    double x=sx*FrontFootPlaneX+dx;
                    Body2 s=cad.Cylinder(x,-210.05,z,0,1,0,3.8,6.45);
                    s=Unite(s,cad.Cylinder(x,-211.0,z,0,1,0,7.2,1.0),"external tangent lower-edge head");
                    cad.AddBody(doc,s,"M4 front bracket screw"); count++;
                }
            cad.ApplyMaterial(doc,"AISI 304",Stainless);
            cad.Property(doc,"Count",count+" M4 A4-80 bracket screws; four per internal U bracket");
            return SavePart(cad,doc,FrontBracketFastenerPackStem,true);
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static void BuildProductAssembly(RackCadSession cad, AssemblySpec spec,
        Stance stance, PartPaths p)
    {
        string source=AssemblyPath(cad,spec.SourceStem);
        string target=AssemblyPath(cad,spec.TargetStem);
        EnsureTargetClosed(cad,target);
        File.Copy(source,target,true);
        ModelDoc2 doc=OpenAssembly(cad,target);
        AssemblyDoc asm=doc as AssemblyDoc;
        Require(asm!=null,"V11 target is not an assembly: "+target);
        try
        {
            List<Component2> initial=TopLevelComponents(asm);
            double[] caseTransform=ReadTransform(FindExact(initial,PartPath(cad,OldBack)));
            double[] lidTransform=null;
            if(spec.HasLid)
                lidTransform=ReadTransform(FindExact(initial,PartPath(cad,OldLid)));

            ReplaceExact(doc,asm,PartPath(cad,OldSide),p.Side,2,"V11 side frames");
            ReplaceExact(doc,asm,PartPath(cad,OldBackLeg),p.BackLeg,2,"240 mm rear legs");
            ReplaceExact(doc,asm,PartPath(cad,OldRail),p.Rail,6,
                "Rymovia constant-section closed custom 104HP rails");
            ReplaceExact(doc,asm,PartPath(cad,OldThreadStrip),p.RailThreadStrip,6,
                "Rymovia replaceable AISI304 M3 threaded strips");
            RemoveExact(doc,asm,PartPath(cad,OldRailEndBlock),12,
                "obsolete separate end blocks now replaced by the full-width integral rail front");
            ReplaceExact(doc,asm,PartPath(cad,OldUpperEdge),p.UpperEdge,1,"joined upper edge");
            ReplaceExact(doc,asm,PartPath(cad,OldLowerEdge),p.LowerEdge,1,"joined lower edge");
            ReplaceExact(doc,asm,PartPath(cad,OldHandle),p.Handle,1,"Southco P9 handle envelope");
            ReplaceExact(doc,asm,PartPath(cad,OldHandleSpreader),p.HandleSpreader,1,"P9 handle spreader");
            ReplaceExact(doc,asm,PartPath(cad,OldBack),p.BackSkin,1,"separate rear skin");
            if(spec.HasLid) ReplaceExact(doc,asm,PartPath(cad,OldLid),p.Lid,1,"continuous secure lid");

            foreach(Tuple<string,int> item in new[]
            {
                Tuple.Create(OldOuterCheek,2),Tuple.Create(OldPivot,2),
                Tuple.Create(OldSpacer,10),Tuple.Create(OldStop,2),
                Tuple.Create(OldLock,2),Tuple.Create(OldHeel,2),
                Tuple.Create(OldFoot,2),Tuple.Create(OldBackFeet,1),
                Tuple.Create(OldVesaBridge,2),Tuple.Create(OldVesaStile,2),
                Tuple.Create(OldRearCrossbeam,2)
            }) RemoveExact(doc,asm,PartPath(cad,item.Item1),item.Item2,"obsolete "+item.Item1);

            foreach(string stem in OldLatchPacks)
            {
                int found=CountExact(TopLevelComponents(asm),PartPath(cad,stem));
                int expected=IsCaseLatchOldStem(stem)?1:(spec.HasLid?1:0);
                Require(found==expected,"Unexpected old latch-pack count for "+stem+
                    "; expected="+expected+" actual="+found);
                if(found>0) RemoveExact(doc,asm,PartPath(cad,stem),found,"obsolete latch pack "+stem);
            }

            PositionBackLegs(cad,doc,asm,spec.Tilt60?stance:null);
            MathUtility math=RequireMath(cad);
            TranslateExactX(doc,asm,math,PartPath(cad,UpperMidiStem),
                MidiCassetteCentreX-(-116.0),"MIDI USB cassette clearance shift");
            TranslateExactX(doc,asm,math,PartPath(cad,UpperAudioStem),
                AudioCassetteCentreX-165.0,"audio cassette clearance shift");

            AddAt(cad,doc,asm,math,p.BackDoubler,"separate VESA doubler",caseTransform);
            AddAt(cad,doc,asm,math,p.VesaFrame,"one-piece VESA load frame",caseTransform);
            AddAt(cad,doc,asm,math,p.VesaFasteners,"VESA and side-tie fasteners",caseTransform);
            AddAt(cad,doc,asm,math,p.BackPerimeterFasteners,"rear perimeter fasteners",caseTransform);
            AddAt(cad,doc,asm,math,p.BackFeet,"four captured rear feet",caseTransform);
            AddAt(cad,doc,asm,math,p.BackFeetFasteners,"rear foot fasteners",caseTransform);
            AddAt(cad,doc,asm,math,p.RailInsertPack,
                "twelve keyed 7075 M4 rail-end inserts",caseTransform);
            AddAt(cad,doc,asm,math,p.RailFastenerPack,
                "twelve M3 locator plus twelve M4 structural rail fasteners",caseTransform);
            AddAt(cad,doc,asm,math,p.ShellCornerFasteners,"eight shell corner fasteners",caseTransform);
            AddAt(cad,doc,asm,math,p.HandleFasteners,"two P9 handle fasteners",caseTransform);
            AddAt(cad,doc,asm,math,p.CassetteFasteners,"twelve service cassette fasteners",caseTransform);
            AddAt(cad,doc,asm,math,p.CheekFasteners,"twelve outer-cheek fasteners",caseTransform);
            AddAt(cad,doc,asm,math,p.BackLockPack,"flush captive rear-leg locks",caseTransform);
            AddAt(cad,doc,asm,math,p.CaseLatchPack,"four Southco V7 keeper brackets",caseTransform);
            AddAt(cad,doc,asm,math,p.CaseLatchFasteners,"eight case latch fasteners",caseTransform);
            AddAt(cad,doc,asm,math,p.FrontBracketPack,"two front anti-tip U brackets",caseTransform);
            AddAt(cad,doc,asm,math,p.FrontPivotPack,"front retained pivot axles",caseTransform);
            AddAt(cad,doc,asm,math,p.FrontStopPack,"front retained hard-stop axles",caseTransform);
            AddAt(cad,doc,asm,math,p.FrontLockPack,"front captive position locks",caseTransform);
            AddAt(cad,doc,asm,math,p.FrontBracketFasteners,"front bracket M4 fasteners",caseTransform);

            Point stop=DeployedBackPointInCase(stance,StopLocalY,StopLocalZ);
            foreach(int sign in new[]{-1,1})
            {
                AddAt(cad,doc,asm,math,p.OuterCheek,
                    (sign<0?"left":"right")+" V11 full-thickness 4 mm outer cheek",
                    FixedCaseTransform(spec.Tilt60?stance:null,sign*OuterCheekCentreX,0,0));
                AddAt(cad,doc,asm,math,p.BackPivot,
                    (sign<0?"left":"right")+" retained rear pivot axle",
                    FixedCaseTransform(spec.Tilt60?stance:null,sign*LegPlaneX,HingeCaseY,HingeCaseZ));
                AddAt(cad,doc,asm,math,p.BackStop,
                    (sign<0?"left":"right")+" retained rear hard stop",
                    FixedCaseTransform(spec.Tilt60?stance:null,sign*LegPlaneX,stop.Y,stop.Z));
                foreach(MountPoint m in CheekSpacers)
                    AddAt(cad,doc,asm,math,p.Spacer,"V11 physical cheek spacer",
                        FixedCaseTransform(spec.Tilt60?stance:null,sign*LegPlaneX,m.Y,m.Z));
                AddAt(cad,doc,asm,math,p.BackBushing,"V11 iglide rear pivot sleeve",
                    BackLegAttachedTransform(spec.Tilt60?stance:null,sign,HingeLocalY,HingeLocalZ));
                AddAt(cad,doc,asm,math,p.BackHeel,"V11 keyed rear steel heel",
                    BackLegAttachedTransform(spec.Tilt60?stance:null,sign,HeelLocalY,HeelLocalZ));
                AddAt(cad,doc,asm,math,p.BackHeelPin,"V11 rear heel retention pin",
                    BackLegAttachedTransform(spec.Tilt60?stance:null,sign,HeelLocalY,HeelLocalZ));
                AddAt(cad,doc,asm,math,p.BackFoot,"V11 captured rear rubber foot",
                    BackLegAttachedTransform(spec.Tilt60?stance:null,sign,BackFootLocalY,BackFootLocalZ));

                double frontAngle=spec.Tilt60?FrontDeployedAngle():FrontFoldedAngle();
                double[] frontLink=FrontLinkTransform(spec.Tilt60?stance:null,sign,frontAngle);
                AddAt(cad,doc,asm,math,p.FrontLink,
                    (sign<0?"left":"right")+" front anti-tip link",frontLink);
                AddAt(cad,doc,asm,math,p.FrontFoot,
                    (sign<0?"left":"right")+" captured front anti-tip pad",
                    AttachedTransform(frontLink,0,FrontLinkLength,0));
            }

            if(spec.HasLid)
            {
                AddAt(cad,doc,asm,math,p.LidLatchDoublers,"four V7 lid doublers",lidTransform);
                AddAt(cad,doc,asm,math,p.LidLatchBodies,"four V7 31 x 72 bodies",lidTransform);
                AddAt(cad,doc,asm,math,p.LidLatchBails,"four closed V7 bails",lidTransform);
                AddAt(cad,doc,asm,math,p.LidLatchFasteners,"sixteen lid latch fasteners",lidTransform);
                AddAt(cad,doc,asm,math,p.LidCompression,"defined EPDM and metal compression stops",lidTransform);
            }

            cad.Property(doc,"Brand","Rymovia Audio Systems");
            cad.Property(doc,"Mechanical revision",Version+" final-audit release candidate");
            cad.Property(doc,"Assembly constraint","Every top-level occurrence is fixed after verified transform placement; separate folded, transport and 60 degree state assemblies carry no floating degrees of freedom");
            double supportWorldY,supportWorldZ;
            CasePointToDesk(FrontFootDeployedCaseY,FrontFootDeployedCaseZ,
                stance,out supportWorldY,out supportWorldZ);
            cad.Property(doc,"Support polygon","At 60 degrees: front pads world longitudinal coordinate "+F(supportWorldZ)+" mm and true crown contact; rear pads "+F(stance.SupportFootprint)+" mm; left/right x +/-205 front and +/-279.4 rear");
            cad.Property(doc,"Digital validation","Full rebuild, resolved-reference, exact-count, all-fixed and zero non-gauge solid-interference checks required before release");
            cad.Property(doc,"Physical validation boundary","Loaded FEA, proof load, one-leg misuse, 10k stand/latch cycles, vibration/drop, VESA pull, handle lift, friction and thermal tests remain mandatory");

            doc.Extension.ForceRebuildAll();
            Require(doc.ForceRebuild3(false),"V11 rebuild failed: "+spec.TargetStem);
            asm.UpdateBox();
            FixAllTopLevel(doc,asm);
            Require(doc.ForceRebuild3(false),"V11 post-fix rebuild failed: "+spec.TargetStem);
            asm.UpdateBox();
            ValidateProductAssembly(cad,doc,asm,spec,stance);
            if(string.Equals(System.Environment.GetEnvironmentVariable("RACK_V11_DEBUG_SAVE"),
                "1",StringComparison.Ordinal))
            {
                cad.SaveAssembly(doc,spec.TargetStem,false);
                cad.Log("V11_DEBUG_ASSEMBLY_SAVED="+target);
            }
            if(spec.TargetStem.IndexOf("ClearanceCheck",StringComparison.OrdinalIgnoreCase)>=0)
                RequireExpectedClearanceInterferences(asm,spec.TargetStem);
            else
                RequireZeroInterference(asm,spec.TargetStem);

            string saved=cad.SaveAssembly(doc,spec.TargetStem,true);
            Require(SamePath(saved,target),"V11 assembly save escaped target path");
            ValidateProductAssembly(cad,doc,asm,spec,stance);
            if(spec.TargetStem.IndexOf("ClearanceCheck",StringComparison.OrdinalIgnoreCase)<0)
                RequireZeroInterference(asm,spec.TargetStem+" after STEP");
            cad.SaveAssembly(doc,spec.TargetStem,false);
            Require(!doc.GetSaveFlag(),"V11 native assembly remains dirty: "+spec.TargetStem);
            cad.Log("V11_ASSEMBLY="+target+";components="+TopLevelComponents(asm).Count+
                ";mass_kg="+F(ReadMass(doc))+";all_fixed=true");
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static void BuildIdentityShowcase(RackCadSession cad, PartPaths p)
    {
        string source=AssemblyPath(cad,Assemblies.Last().TargetStem);
        string target=AssemblyPath(cad,IdentityShowcaseStem);
        EnsureTargetClosed(cad,target);
        File.Copy(source,target,true);
        ModelDoc2 doc=OpenAssembly(cad,target);
        AssemblyDoc asm=doc as AssemblyDoc;
        Require(asm!=null,"V11 identity showcase is not an assembly");
        try
        {
            MathUtility math=RequireMath(cad);
            double[] display=IdentityTransform(-650.0,0,0);
            AddAt(cad,doc,asm,math,p.BackSkin,"presentation-only Phase Halo rear skin",display);
            AddAt(cad,doc,asm,math,p.BackDoubler,"presentation-only rear doubler",display);
            cad.Property(doc,"Presentation intent","Only final V11 view: detached decorated rear skin at left, complete 60 degree four-point-supported case at centre, detached latched-lid assembly at right");
            cad.Property(doc,"BOM boundary","The extra rear skin and doubler at x=-650 mm are display duplicates and excluded from product BOM/mass");
            FixAllTopLevel(doc,asm);
            Require(doc.ForceRebuild3(false),"V11 identity showcase rebuild failed");
            asm.UpdateBox();
            RequireZeroInterference(asm,IdentityShowcaseStem);
            doc.ShowNamedView2("*Isometric",7);
            doc.ViewZoomtofit2();
            doc.GraphicsRedraw2();
            cad.SaveAssembly(doc,IdentityShowcaseStem,true);
            cad.SaveAssembly(doc,IdentityShowcaseStem,false);
            Require(!doc.GetSaveFlag(),"V11 identity showcase remains dirty");
        }
        finally { cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static void ValidateProductAssembly(RackCadSession cad,ModelDoc2 doc,
        AssemblyDoc asm,AssemblySpec spec,Stance stance)
    {
        List<Component2> c=TopLevelComponents(asm);
        foreach(string old in new[]{OldSide,OldBackLeg,OldOuterCheek,OldPivot,
            OldSpacer,OldStop,OldLock,OldHeel,OldFoot,OldHandleSpreader,OldRail,
            OldRailEndBlock,OldThreadStrip,OldUpperEdge,OldLowerEdge,OldHandle,OldBack,OldBackFeet,
            OldVesaBridge,OldVesaStile,OldRearCrossbeam})
            Require(CountStem(c,old)==0,"Obsolete component remains: "+old);
        foreach(string old in OldLatchPacks)
            Require(CountStem(c,old)==0,"Obsolete latch pack remains: "+old);
        Require(CountStem(c,SideStem)==2,"V11 side-frame count is not two");
        Require(CountStem(c,BackLegStem)==2,"V11 rear-leg count is not two");
        Require(CountStem(c,OuterCheekStem)==2,"V11 outer-cheek count is not two");
        Require(CountStem(c,BackBushingStem)==2,"V11 bushing count is not two");
        Require(CountStem(c,RailStem)==6,"V11 rail count is not six");
        Require(CountStem(c,RailThreadStripStem)==6,
            "V11 custom AISI304 threaded-strip count is not six");
        Require(CountStem(c,RailInsertPackStem)==1,
            "V11 twelve-insert custom rail pack is missing");
        Require(CountStem(c,RailFastenerPackStem)==1,
            "V11 dual-fix 12xM3 plus 12xM4 rail fastener pack is missing");
        Require(CountStem(c,BackSkinStem)==1,"V11 rear skin count is not one");
        Require(CountStem(c,BackDoublerStem)==1,"V11 rear doubler count is not one");
        Require(CountStem(c,VesaFrameStem)==1,"V11 VESA load frame count is not one");
        Require(CountStem(c,FrontBracketPackStem)==1,"V11 front bracket pack is missing");
        Require(CountStem(c,FrontLinkStem)==2,"V11 front-link count is not two");
        Require(CountStem(c,FrontFootStem)==2,"V11 front-foot count is not two");
        Require(CountStem(c,CaseLatchPackStem)==1,"V11 case latch pack is missing");
        Require(CountStem(c,LidStem)==(spec.HasLid?1:0),"V11 lid count mismatch");
        foreach(string stem in new[]{LidLatchDoublerPackStem,LidLatchBodyPackStem,
            LidLatchBailPackStem,LidLatchFastenerPackStem,LidCompressionPackStem})
            Require(CountStem(c,stem)==(spec.HasLid?1:0),"V11 lid hardware count mismatch: "+stem);

        int unresolved=0,missing=0,unfixed=0;
        foreach(Component2 component in c)
        {
            if(component.IsSuppressed() || component.GetModelDoc2()==null) unresolved++;
            string path=component.GetPathName();
            if(string.IsNullOrWhiteSpace(path)||!File.Exists(path)) missing++;
            if(!component.IsFixed()) unfixed++;
        }
        Require(unresolved==0,"V11 has suppressed or unresolved top-level components: "+unresolved);
        Require(missing==0,"V11 has missing component files: "+missing);
        Require(unfixed==0,"V11 has floating top-level components: "+unfixed);
        Require(doc.Extension.NeedsRebuild==false,"V11 document still needs rebuild");
        double mass=ReadMass(doc);
        bool isClearance=spec.TargetStem.IndexOf("ClearanceCheck",
            StringComparison.OrdinalIgnoreCase)>=0;
        // The clearance source assembly intentionally contains removable
        // solid gauges.  They are not product BOM items, so its native mass
        // is expected to exceed the product envelope; still require a valid
        // positive mass and keep the <9 kg gate on every product state.
        if(isClearance)
        {
            Require(mass>0,"V11 clearance assembly mass is invalid: "+F(mass));
            cad.Property(doc,"Mass boundary","Clearance gauges are excluded from product BOM; reported native mass includes gauges="+F(mass)+" kg");
        }
        else
            Require(mass>0 && mass<9.0,"V11 assembly mass is invalid or excessive: "+F(mass));

        if(spec.TargetStem.IndexOf("DesktopTilt60",StringComparison.OrdinalIgnoreCase)>=0)
        {
            MassProperty mp=doc.Extension.CreateMassProperty();
            Array center=mp.CenterOfMass as Array;
            Require(center!=null&&center.Length>=3,"V11 tilt COM unavailable");
            double cgZ=Convert.ToDouble(center.GetValue(2),CultureInfo.InvariantCulture)*1000.0;
            double weight=mass*9.80665;
            double moment=20.0*0.200;
            double frontWorldY,frontWorldZ;
            CasePointToDesk(FrontFootDeployedCaseY,FrontFootDeployedCaseZ,
                stance,out frontWorldY,out frontWorldZ);
            RequireClose(frontWorldY,FrontFootDeskCentreHeight,0.01,
                "built front-pad centre height");
            double rearWorst=weight*(stance.SupportFootprint-(cgZ+30.0))/1000.0/moment;
            double frontWorst=weight*((cgZ-30.0)-frontWorldZ)/1000.0/moment;
            Require(rearWorst>=2.0,"V11 rear loaded-CG anti-tip SF below 2.0: "+F(rearWorst));
            Require(frontWorst>=2.0,"V11 front loaded-CG anti-tip SF below 2.0: "+F(frontWorst));
            cad.Property(doc,"Stability screening","20 N horizontal at 200 mm; measured empty mass/CG with +/-30 mm longitudinal loaded-CG sensitivity; rear SF="+F(rearWorst)+", front SF="+F(frontWorst));
            cad.Log("V11_STABILITY_SF_REAR="+F(rearWorst)+";FRONT="+F(frontWorst)+";CGZ_MM="+F(cgZ)+";FRONT_SUPPORT_MM="+F(frontWorldZ)+";REAR_SUPPORT_MM="+F(stance.SupportFootprint));
        }
    }

    private static void RequireZeroInterference(AssemblyDoc asm,string context)
    {
        InterferenceDetectionMgr manager=null;
        try
        {
            manager=asm.InterferenceDetectionManager;
            Require(manager!=null,"Interference manager unavailable for "+context);
            manager.TreatCoincidenceAsInterference=false;
            manager.IncludeMultibodyPartInterferences=false;
            manager.ShowIgnoredInterferences=false;
            int api=manager.GetInterferenceCount();
            Array found=manager.GetInterferences() as Array;
            int count=found==null?0:found.Length;
            if(api!=0||count!=0)
            {
                List<string> detail=new List<string>();
                if(found!=null) foreach(object raw in found)
                {
                    Interference i=raw as Interference;
                    if(i==null) continue;
                    Array participants=i.Components as Array;
                    string names=participants==null?"?":string.Join(" + ",participants.Cast<object>()
                        .Select(o=>o as Component2).Where(x=>x!=null)
                        .Select(x=>Path.GetFileNameWithoutExtension(x.GetPathName())));
                    detail.Add(names+" volume_mm3="+F(i.Volume*1000000000.0));
                }
                throw new InvalidOperationException("Solid interference in "+context+
                    "; api="+api+" enum="+count+"; "+string.Join(" | ",detail));
            }
        }
        finally { if(manager!=null) try { manager.Done(); } catch { } }
    }

    private static void RequireExpectedClearanceInterferences(AssemblyDoc asm,
        string context)
    {
        InterferenceDetectionMgr manager=null;
        try
        {
            manager=asm.InterferenceDetectionManager;
            Require(manager!=null,"Interference manager unavailable for "+context);
            manager.TreatCoincidenceAsInterference=false;
            manager.IncludeMultibodyPartInterferences=false;
            manager.ShowIgnoredInterferences=false;
            int api=manager.GetInterferenceCount();
            Array found=manager.GetInterferences() as Array;
            int count=found==null?0:found.Length;
            Require(api==2&&count==2,
                "Clearance assembly must contain exactly two intentional gauge overlaps; api="+
                api+" enum="+count);
            bool powerSupply=false,powerBus=false;
            foreach(object raw in found)
            {
                Interference interference=raw as Interference;
                Require(interference!=null,"Invalid interference result in "+context);
                Array participants=interference.Components as Array;
                List<string> names=participants==null?new List<string>():
                    participants.Cast<object>().Select(o=>o as Component2)
                        .Where(x=>x!=null)
                        .Select(x=>Path.GetFileNameWithoutExtension(x.GetPathName()))
                        .ToList();
                bool module=names.Contains("ModuleDepthEnvelope_85mm_V03",
                    StringComparer.OrdinalIgnoreCase);
                double volume=interference.Volume*1000000000.0;
                if(module&&names.Contains("ReservedPowerSupply_210x90x45",
                    StringComparer.OrdinalIgnoreCase))
                {
                    Require(!powerSupply,"Duplicate power-supply gauge overlap in "+context);
                    RequireClose(volume,472500.0,1.0,
                        "module/power-supply gauge overlap volume");
                    powerSupply=true;
                }
                else if(module&&names.Contains("ReservedPowerBus_500x85x20",
                    StringComparer.OrdinalIgnoreCase))
                {
                    Require(!powerBus,"Duplicate power-bus gauge overlap in "+context);
                    RequireClose(volume,420900.0,1.0,
                        "module/power-bus gauge overlap volume");
                    powerBus=true;
                }
                else
                    throw new InvalidOperationException(
                        "Unexpected clearance interference in "+context+": "+
                        string.Join(" + ",names)+" volume_mm3="+F(volume));
            }
            Require(powerSupply&&powerBus,
                "Required power-supply and power-bus gauge overlaps are incomplete in "+context);
        }
        finally { if(manager!=null) try { manager.Done(); } catch { } }
    }

    private static bool IsCaseLatchOldStem(string stem)
    {
        return stem=="LidLatchCaseBridgePack_V09_6061" ||
            stem=="LidLatchKeeperPack_V09_Stainless" ||
            stem=="LidLatchCaseFastenerPack_V09_A4_M3";
    }

    private static void PositionBackLegs(RackCadSession cad,ModelDoc2 doc,
        AssemblyDoc asm,Stance stance)
    {
        MathUtility math=RequireMath(cad);
        int count=0;
        foreach(Component2 component in TopLevelComponents(asm).Where(c=>SameStem(c,BackLegStem)))
        {
            int sign=ReadTransform(component)[9]<0?-1:1;
            double[] transform=stance==null?
                IdentityTransform(sign*LegPlaneX,FoldedBackLegOriginY,FoldedBackLegOriginZ):
                DeployedBackLegTransform(stance,sign);
            ApplyTransform(doc,asm,math,component,transform,"positioned rear leg");
            count++;
        }
        Require(count==2,"Exactly two V11 rear legs must be positioned");
    }

    private static void ReplaceExact(ModelDoc2 doc,AssemblyDoc asm,string oldPath,
        string newPath,int expected,string context)
    {
        RequireFile(oldPath); RequireFile(newPath);
        int count=0;
        while(true)
        {
            Component2 old=TopLevelComponents(asm).FirstOrDefault(c=>SameComponentPath(c,oldPath));
            if(old==null) break;
            count++;
            Require(count<=expected,"Too many source occurrences for "+context);
            doc.ClearSelection2(true);
            Require(old.Select4(false,null,false),"Cannot select source for "+context);
            Require(asm.ReplaceComponents(newPath,string.Empty,false,true),
                "SOLIDWORKS refused replacement for "+context);
            doc.ClearSelection2(true);
        }
        Require(count==expected,"Expected "+expected+" replacements for "+context+
            "; actual="+count);
        Require(CountExact(TopLevelComponents(asm),oldPath)==0,"Old path remains for "+context);
        Require(CountExact(TopLevelComponents(asm),newPath)==expected,
            "Replacement count mismatch for "+context);
    }

    private static void RemoveExact(ModelDoc2 doc,AssemblyDoc asm,string path,
        int expected,string context)
    {
        int count=0;
        while(true)
        {
            Component2 found=TopLevelComponents(asm).FirstOrDefault(c=>SameComponentPath(c,path));
            if(found==null) break;
            count++;
            Require(count<=expected,"Too many occurrences while removing "+context);
            doc.ClearSelection2(true);
            Require(found.Select4(false,null,false),"Cannot select "+context);
            Require(doc.Extension.DeleteSelection2(0),"Cannot remove "+context);
            doc.ClearSelection2(true);
        }
        Require(count==expected,"Expected "+expected+" removals for "+context+
            "; actual="+count);
        Require(File.Exists(path),"Protected source disappeared while removing "+context);
    }

    private static Component2 AddAt(RackCadSession cad,ModelDoc2 doc,AssemblyDoc asm,
        MathUtility math,string path,string label,double[] transform)
    {
        Component2 component=cad.AddComponent(doc,path,label,0,0,0);
        ApplyTransform(doc,asm,math,component,transform,label);
        return component;
    }

    private static void TranslateExactX(ModelDoc2 doc,AssemblyDoc asm,
        MathUtility math,string path,double deltaMillimetres,string context)
    {
        List<Component2> found=TopLevelComponents(asm)
            .Where(c=>SameComponentPath(c,path)).ToList();
        Require(found.Count==1,"Expected one component for "+context+
            "; actual="+found.Count+"; path="+path);
        double[] transform=ReadTransform(found[0]);
        transform[9]+=deltaMillimetres/1000.0;
        ApplyTransform(doc,asm,math,found[0],transform,context);
    }

    private static void ApplyTransform(ModelDoc2 doc,AssemblyDoc asm,MathUtility math,
        Component2 component,double[] requested,string context)
    {
        if(component.IsFixed())
        {
            doc.ClearSelection2(true);
            Require(component.Select4(false,null,false),"Cannot select fixed "+context);
            asm.UnfixComponent(); doc.ClearSelection2(true);
        }
        MathTransform transform=math.CreateTransform(requested) as MathTransform;
        Require(transform!=null,"Cannot create transform for "+context);
        component.Transform2=transform;
        RequireTransformEqual(ReadTransform(component),requested,context);
        asm.UpdateBox();
    }

    private static void FixAllTopLevel(ModelDoc2 doc,AssemblyDoc asm)
    {
        foreach(Component2 component in TopLevelComponents(asm))
        {
            if(component.IsFixed()) continue;
            doc.ClearSelection2(true);
            Require(component.Select4(false,null,false),"Cannot select component to fix: "+component.Name2);
            asm.FixComponent();
            doc.ClearSelection2(true);
            Require(component.IsFixed(),"Component remains floating after fix: "+component.Name2);
        }
    }

    private static Stance CalculateBackStance(double faceAngleDegrees)
    {
        double angle=Degrees(faceAngleDegrees);
        double sine=Math.Sin(angle),cosine=Math.Cos(angle);
        double hingeHeight=(HingeCaseY-ShellContactY)*sine+
            (ShellContactZ-HingeCaseZ)*cosine;
        double drop=hingeHeight-RearFootDeskCentreHeight;
        Require(drop>0&&drop<BackPivotToFoot,"Rear support cannot reach desk");
        double reach=Math.Sqrt(BackPivotToFoot*BackPivotToFoot-drop*drop);
        double detent=faceAngleDegrees+Radians(Math.Asin(drop/BackPivotToFoot));
        double hingeHorizontal=(HingeCaseY-ShellContactY)*cosine+
            (HingeCaseZ-ShellContactZ)*sine;
        return new Stance(faceAngleDegrees,angle,detent,Degrees(detent),
            hingeHeight,hingeHorizontal+reach);
    }

    private static Point DeployedBackPointInCase(Stance stance,double qy,double qz)
    {
        double s=Math.Sin(stance.DetentRadians),c=Math.Cos(stance.DetentRadians);
        return new Point(0,HingeCaseY+qy*c-qz*s,HingeCaseZ+qy*s+qz*c);
    }

    private static double[] DeployedBackLegTransform(Stance stance,int sign)
    {
        double relative=stance.AngleRadians-stance.DetentRadians;
        double s=Math.Sin(relative),c=Math.Cos(relative);
        Point pivot=CasePointToDesk(sign*LegPlaneX,HingeCaseY,HingeCaseZ,stance);
        double oy=pivot.Y-(HingeLocalY*s+HingeLocalZ*-c);
        double oz=pivot.Z-(HingeLocalY*c+HingeLocalZ*s);
        return new[]{1.0,0.0,0.0,0.0,s,c,0.0,-c,s,
            pivot.X/1000.0,oy/1000.0,oz/1000.0,1.0,0.0,0.0,0.0};
    }

    private static double[] BackLegAttachedTransform(Stance stance,int sign,
        double localY,double localZ)
    {
        double[] leg=stance==null?
            IdentityTransform(sign*LegPlaneX,FoldedBackLegOriginY,FoldedBackLegOriginZ):
            DeployedBackLegTransform(stance,sign);
        return AttachedTransform(leg,0,localY,localZ);
    }

    private static double[] FrontLinkTransform(Stance stance,int sign,double phi)
    {
        double cy=Math.Cos(phi),sy=Math.Sin(phi);
        if(stance==null)
            return new[]{1.0,0.0,0.0,0.0,cy,sy,0.0,-sy,cy,
                sign*FrontFootPlaneX/1000.0,FrontPivotCaseY/1000.0,
                FrontPivotCaseZ/1000.0,1.0,0.0,0.0,0.0};

        double st=Math.Sin(stance.AngleRadians),ct=Math.Cos(stance.AngleRadians);
        Point pivot=CasePointToDesk(sign*FrontFootPlaneX,FrontPivotCaseY,
            FrontPivotCaseZ,stance);
        double yBasisY=cy*st-sy*ct;
        double yBasisZ=cy*ct+sy*st;
        double zBasisY=-sy*st-cy*ct;
        double zBasisZ=-sy*ct+cy*st;
        return new[]{1.0,0.0,0.0,0.0,yBasisY,yBasisZ,0.0,zBasisY,zBasisZ,
            pivot.X/1000.0,pivot.Y/1000.0,pivot.Z/1000.0,1.0,0.0,0.0,0.0};
    }

    private static double[] AttachedTransform(double[] source,double x,double y,double z)
    {
        Point p=ApplyTransform(source,x,y,z);
        double[] result=(double[])source.Clone();
        result[9]=p.X/1000.0; result[10]=p.Y/1000.0; result[11]=p.Z/1000.0;
        return result;
    }

    private static double[] FixedCaseTransform(Stance stance,double x,double y,double z)
    {
        if(stance==null) return IdentityTransform(x,y,z);
        return CaseTransform(CasePointToDesk(x,y,z,stance),stance);
    }

    private static double[] CaseTransform(Point origin,Stance stance)
    {
        double s=Math.Sin(stance.AngleRadians),c=Math.Cos(stance.AngleRadians);
        return new[]{1.0,0.0,0.0,0.0,s,c,0.0,-c,s,
            origin.X/1000.0,origin.Y/1000.0,origin.Z/1000.0,
            1.0,0.0,0.0,0.0};
    }

    private static Point CasePointToDesk(double x,double y,double z,Stance stance)
    {
        double s=Math.Sin(stance.AngleRadians),c=Math.Cos(stance.AngleRadians);
        return new Point(x,(y-ShellContactY)*s+(ShellContactZ-z)*c,
            (y-ShellContactY)*c+(z-ShellContactZ)*s);
    }

    private static void CasePointToDesk(double y,double z,Stance stance,
        out double worldY,out double worldZ)
    {
        Point p=CasePointToDesk(0,y,z,stance); worldY=p.Y; worldZ=p.Z;
    }

    private static Point ApplyTransform(double[] t,double x,double y,double z)
    {
        return new Point(x*t[0]+y*t[3]+z*t[6]+t[9]*1000.0,
            x*t[1]+y*t[4]+z*t[7]+t[10]*1000.0,
            x*t[2]+y*t[5]+z*t[8]+t[11]*1000.0);
    }

    private static double[] IdentityTransform(double x,double y,double z)
    {
        return new[]{1.0,0.0,0.0,0.0,1.0,0.0,0.0,0.0,1.0,
            x/1000.0,y/1000.0,z/1000.0,1.0,0.0,0.0,0.0};
    }

    private static double FrontDeployedAngle()
    {
        return Math.Atan2(FrontFootDeployedCaseZ-FrontPivotCaseZ,
            FrontFootDeployedCaseY-FrontPivotCaseY);
    }

    private static double FrontFoldedAngle()
    {
        return Math.Atan2(FrontFootFoldedCaseZ-FrontPivotCaseZ,
            FrontFootFoldedCaseY-FrontPivotCaseY);
    }

    private static Point FrontLockDeployLocalPoint()
    {
        return InverseRotate(FrontLockCaseY-FrontPivotCaseY,
            FrontLockCaseZ-FrontPivotCaseZ,FrontDeployedAngle());
    }

    private static Point RotateFromPivot(double py,double pz,double qy,double qz,double angle)
    {
        double c=Math.Cos(angle),s=Math.Sin(angle);
        return new Point(0,py+qy*c-qz*s,pz+qy*s+qz*c);
    }

    private static Point InverseRotate(double y,double z,double angle)
    {
        double c=Math.Cos(angle),s=Math.Sin(angle);
        return new Point(0,y*c+z*s,-y*s+z*c);
    }

    private static double Distance(double y1,double z1,double y2,double z2)
    {
        return Math.Sqrt((y2-y1)*(y2-y1)+(z2-z1)*(z2-z1));
    }

    private static Body2 Unite(Body2 a,Body2 b,string context)
    {
        int error=0;
        Array result=a.Operations2((int)swBodyOperationType_e.SWBODYADD,b,out error) as Array;
        Require(error==(int)swBodyOperationError_e.swBodyOperationNoError,
            "Body union failed for "+context+"; error="+error);
        Require(result!=null&&result.Length==1,"Body union did not produce one solid for "+context);
        Body2 body=result.GetValue(result.GetLowerBound(0)) as Body2;
        Require(body!=null,"Body union returned null for "+context);
        return body;
    }

    private static Body2 SideHole(RackCadSession cad,Body2 body,double y,double z,
        double diameter,string label)
    {
        return cad.Cut(body,cad.Cylinder(-2.4,y,z,1,0,0,diameter,4.8),label);
    }

    private static Body2 ThroughCheekHole(RackCadSession cad,Body2 body,double y,
        double z,double diameter,string label)
    {
        return cad.Cut(body,cad.Cylinder(-2.4,y,z,1,0,0,diameter,4.8),label);
    }

    private static Body2 SymmetricCounterboreX(RackCadSession cad,Body2 body,
        double y,double z,double diameter,double depth,double thickness,string label)
    {
        body=cad.Cut(body,cad.Cylinder(-thickness/2-0.3,y,z,1,0,0,
            diameter,depth+0.3),label+" first face");
        body=cad.Cut(body,cad.Cylinder(thickness/2+0.3,y,z,-1,0,0,
            diameter,depth+0.3),label+" second face");
        return body;
    }

    private static Body2 CapsuleHoleX(RackCadSession cad,Body2 body,double y,
        double z,double length,double width,string label)
    {
        double core=length-width;
        body=cad.Cut(body,cad.Box(-0.4,y-core/2,z-width/2,
            SideLoadThickness+0.8,core,width),label+" core");
        foreach(double sy in Signs())
            body=SideHole(cad,body,y+sy*core/2,z,width,label+" rounded end");
        return body;
    }

    private static Body2 CutEdgeWindow(RackCadSession cad,Body2 body,double x,
        double centreZ,double width,double height,string label)
    {
        return cad.Cut(body,cad.Box(x,0,centreZ-height/2,width,2.8,height),label);
    }

    private static Body2 AddEdgeCassetteHoles(RackCadSession cad,Body2 body,
        double centreX,double halfPitch,string label)
    {
        foreach(double sx in Signs()) foreach(double z in new[]{20.0,90.0})
            body=HoleY(cad,body,centreX+sx*halfPitch,z,3.2,2.0,label+" M3 hole");
        return body;
    }

    private static Body2 HoleY(RackCadSession cad,Body2 body,double x,double z,
        double diameter,double thickness,string label)
    {
        return cad.Cut(body,cad.Cylinder(x,-thickness/2-0.4,z,0,1,0,
            diameter,thickness+0.8),label);
    }

    private static Body2 HoleZ(RackCadSession cad,Body2 body,double x,double y,
        double diameter,double startZ,double length,string label)
    {
        return cad.Cut(body,cad.Cylinder(x,y,startZ,0,0,1,diameter,length),label);
    }

    private static Body2 HoleXFromEnd(RackCadSession cad,Body2 body,double x,
        double y,double z,double direction,double diameter,double length,string label)
    {
        return cad.Cut(body,cad.Cylinder(x+direction*-0.3,y,z,direction,0,0,
            diameter,length),label);
    }

    private static Body2 HoleYGlobal(RackCadSession cad,Body2 body,double x,
        double startY,double z,double diameter,double length,string label)
    {
        return cad.Cut(body,cad.Cylinder(x,startY,z,0,1,0,diameter,length),label);
    }

    private static Body2 HoleXGlobal(RackCadSession cad,Body2 body,double x,
        double y,double z,double diameter,double length,string label)
    {
        return cad.Cut(body,cad.Cylinder(x-length/2,y,z,1,0,0,diameter,length),label);
    }

    private static IEnumerable<double> RailPositions(RackCadSession cad)
    {
        double pitch=cad.N("eurorack","row_pitch");
        double spacing=cad.N("eurorack","mounting_hole_vertical_spacing");
        foreach(double row in new[]{-pitch,0.0,pitch})
        {
            yield return row-spacing/2;
            yield return row+spacing/2;
        }
    }

    private static IEnumerable<double> Signs()
    {
        yield return -1.0; yield return 1.0;
    }

    private static string SavePart(RackCadSession cad,ModelDoc2 doc,string stem,
        bool exportStep)
    {
        string saved=cad.SavePart(doc,stem,exportStep);
        Require(SamePath(saved,PartPath(cad,stem)),"Part save escaped target: "+stem);
        cad.SavePart(doc,stem,false);
        Require(!doc.GetSaveFlag(),"Part remains dirty after final native save: "+stem);
        return saved;
    }

    private static Face2 FindPositiveZFace(PartDoc part)
    {
        Require(part!=null,"Part document unavailable for positive-Z face");
        Face2 best=null; double area=0;
        Array bodies=part.GetBodies2((int)swBodyType_e.swSolidBody,false) as Array;
        Require(bodies!=null,"Part has no solid bodies");
        foreach(object ob in bodies)
        {
            Body2 b=ob as Body2; Array faces=b==null?null:b.GetFaces() as Array;
            if(faces==null) continue;
            foreach(object of in faces)
            {
                Face2 f=of as Face2; if(f==null) continue;
                Array n=f.Normal as Array; if(n==null||n.Length<3) continue;
                double nz=Convert.ToDouble(n.GetValue(2),CultureInfo.InvariantCulture);
                if(nz>0.99&&f.GetArea()>area){best=f;area=f.GetArea();}
            }
        }
        Require(best!=null,"No positive-Z face found"); return best;
    }

    private static void FindLargeLidFaces(PartDoc part,out Face2 outer,out Face2 inner)
    {
        Require(part!=null,"Lid part document unavailable");
        outer=null; inner=null; double min=double.PositiveInfinity,max=double.NegativeInfinity;
        Array bodies=part.GetBodies2((int)swBodyType_e.swSolidBody,false) as Array;
        Require(bodies!=null&&bodies.Length==1,"V11 lid must be one continuous solid body");
        Body2 body=bodies.GetValue(bodies.GetLowerBound(0)) as Body2;
        Array faces=body.GetFaces() as Array;
        foreach(object ob in faces)
        {
            Face2 f=ob as Face2; if(f==null||f.GetArea()<0.05) continue;
            Array n=f.Normal as Array; if(n==null||n.Length<3) continue;
            double nz=Math.Abs(Convert.ToDouble(n.GetValue(2),CultureInfo.InvariantCulture));
            if(nz<0.99) continue;
            double z=FaceCentreZ(f);
            if(z<min){min=z;outer=f;} if(z>max){max=z;inner=f;}
        }
        Require(outer!=null&&inner!=null&&min<max,"Cannot identify lid front faces");
    }

    private static double FaceCentreZ(Face2 face)
    {
        Array b=face.GetBox() as Array;
        Require(b!=null&&b.Length>=6,"Face box unavailable");
        return (Convert.ToDouble(b.GetValue(2),CultureInfo.InvariantCulture)+
            Convert.ToDouble(b.GetValue(5),CultureInfo.InvariantCulture))/2.0;
    }

    private static string FindDecalTemplate()
    {
        string dir=@"E:\SW2025\SOLIDWORKS\data\graphics\Decals\Logos";
        foreach(string name in new[]{"decals logo.p2d","sw logo transparent.p2d","sw.p2d"})
        {
            string path=Path.Combine(dir,name); if(File.Exists(path)) return path;
        }
        throw new FileNotFoundException("No SOLIDWORKS decal template",dir);
    }

    private static void AddDecal(ModelDoc2 doc,Face2 face,string template,
        string image,double width,double height,double x,double y,double z,string label)
    {
        Require(face!=null,"No face for decal "+label); RequireFile(image);
        Decal decal=doc.Extension.CreateDecal(); RenderMaterial m=(RenderMaterial)decal;
        Require(m.AddEntity(face),"Cannot attach decal entity "+label);
        m.FileName=template; m.TextureFilename=image; m.MappingType=0;
        m.ProjectionReference=0; m.FixedAspectRatio=true; m.FitWidth=false;
        m.FitHeight=false; m.Width=width; m.Height=height;
        m.SetCenterPoint2(x,y,z); m.SetUDirection2(1,0,0); m.SetVDirection2(0,1,0);
        decal.MaskType=PreviewDecalMaskAlpha; decal.Hidden=false; int id=0;
        Require(doc.Extension.AddDecal(decal,out id),"Cannot add decal "+label);
    }

    private static ModelDoc2 OpenAssembly(RackCadSession cad,string path)
    {
        int errors=0,warnings=0;
        ModelDoc2 doc=cad.Application.OpenDoc6(path,(int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent|
            (int)swOpenDocOptions_e.swOpenDocOptions_LoadModel,
            string.Empty,ref errors,ref warnings) as ModelDoc2;
        Require(doc!=null&&errors==0,"Cannot open assembly; errors="+errors+" path="+path);
        Require(SamePath(doc.GetPathName(),path),"Opened wrong assembly identity: "+path);
        if(warnings!=0) cad.Log("WARNING: opening "+path+" returned "+warnings);
        return doc;
    }

    private static void EnsureTargetClosed(RackCadSession cad,string path)
    {
        ModelDoc2 doc=FindOpenDocumentByPath(cad,path);
        if(doc==null) return;
        Require(!doc.GetSaveFlag(),"Refusing to overwrite dirty V11 target: "+path);
        cad.Application.CloseDoc(doc.GetTitle());
    }

    private static void ClosePathlessGeneratedAssemblies(RackCadSession cad)
    {
        HashSet<string> allowed=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(AssemblySpec spec in Assemblies) allowed.Add(spec.TargetStem);
        allowed.Add(IdentityShowcaseStem);
        List<string> close=new List<string>();
        ModelDoc2 doc=cad.Application.GetFirstDocument() as ModelDoc2;
        while(doc!=null)
        {
            string path=doc.GetPathName();
            string title=doc.GetTitle();
            string stem=Path.GetFileNameWithoutExtension(title??string.Empty);
            if(string.IsNullOrWhiteSpace(path)&&allowed.Contains(stem))
            {
                // STEP imports are pathless new documents and therefore carry
                // GetSaveFlag=true even when untouched. The exact title
                // whitelist plus empty path distinguishes our disposable
                // readback ghosts from every saved native document.
                cad.Log("V11_PATHLESS_STEP_READBACK_GHOST_DIRTY_FLAG="+
                    doc.GetSaveFlag()+";title="+title);
                close.Add(title);
            }
            doc=doc.GetNext() as ModelDoc2;
        }
        foreach(string title in close)
        {
            cad.Application.CloseDoc(title);
            cad.Log("V11_CLOSED_PATHLESS_STEP_READBACK_GHOST="+title);
        }
    }

    private static ModelDoc2 FindOpenDocumentByPath(RackCadSession cad,string path)
    {
        ModelDoc2 doc=cad.Application.GetFirstDocument() as ModelDoc2;
        while(doc!=null)
        {
            string openPath=doc.GetPathName();
            if(!string.IsNullOrWhiteSpace(openPath)&&SamePath(openPath,path))
                return doc;
            doc=doc.GetNext() as ModelDoc2;
        }
        return null;
    }

    private static void CloseCleanV11DocumentsExcept(RackCadSession cad,string keepStem)
    {
        string keep=AssemblyPath(cad,keepStem);
        List<string> close=new List<string>();
        ModelDoc2 doc=cad.Application.GetFirstDocument() as ModelDoc2;
        while(doc!=null)
        {
            string path=doc.GetPathName();
            if(!string.IsNullOrWhiteSpace(path)&&
                path.IndexOf("_V11_",StringComparison.OrdinalIgnoreCase)>=0&&
                !SamePath(path,keep))
            {
                string full=Path.GetFullPath(path);
                string root=Path.GetFullPath(cad.Root).TrimEnd(Path.DirectorySeparatorChar)+
                    Path.DirectorySeparatorChar;
                Require(full.StartsWith(root,StringComparison.OrdinalIgnoreCase),
                    "Refusing to save a generated V11 document outside the project: "+path);
                if(doc.GetSaveFlag())
                {
                    string stem=Path.GetFileNameWithoutExtension(path);
                    bool resumedPart=string.Equals(
                        System.Environment.GetEnvironmentVariable("RACK_V11_RESUME"),"1",
                        StringComparison.Ordinal)&&
                        string.Equals(Path.GetExtension(path),".SLDPRT",
                            StringComparison.OrdinalIgnoreCase)&&
                        !IsForcedStem(stem);
                    if(resumedPart)
                        cad.Log("V11_DISCARD_SESSION_ONLY_DIRTY_RESUMED_PART="+path);
                    else
                    {
                        int errors=0,warnings=0;
                        bool saved=doc.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent,
                            ref errors,ref warnings);
                        Require(saved&&errors==0,"Cannot cleanly save generated V11 document: "+
                            path+" errors="+errors);
                        cad.Log("V11_SAVED_DIRTY_GENERATED_DOCUMENT="+path+
                            ";warnings="+warnings);
                    }
                }
                bool allowDiscard=string.Equals(
                    System.Environment.GetEnvironmentVariable("RACK_V11_RESUME"),"1",
                    StringComparison.Ordinal)&&
                    string.Equals(Path.GetExtension(path),".SLDPRT",
                        StringComparison.OrdinalIgnoreCase)&&
                    !IsForcedStem(Path.GetFileNameWithoutExtension(path));
                if(!allowDiscard)
                    Require(!doc.GetSaveFlag(),
                        "Generated V11 document remains dirty after explicit save: "+path);
                close.Add(doc.GetTitle());
            }
            doc=doc.GetNext() as ModelDoc2;
        }
        foreach(string title in close) cad.Application.CloseDoc(title);
    }

    private static void OpenFinalShowcase(RackCadSession cad)
    {
        string path=AssemblyPath(cad,IdentityShowcaseStem);
        ModelDoc2 doc=OpenAssembly(cad,path);
        Require(doc.ForceRebuild3(false),"Final V11 showcase rebuild failed on reopen");
        cad.SaveAssembly(doc,IdentityShowcaseStem,false);
        Require(!doc.GetSaveFlag(),"Final V11 showcase remains dirty after reopen save");
        cad.Application.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swDisplayDecals,true);
        cad.Application.Visible=true; cad.Application.UserControl=true;
        cad.Application.FrameState=(int)swWindowState_e.swWindowMaximized;
        cad.Show(doc); doc.GraphicsRedraw2();
    }

    private static List<Component2> TopLevelComponents(AssemblyDoc asm)
    {
        Array raw=asm.GetComponents(true) as Array; List<Component2> list=new List<Component2>();
        if(raw!=null) foreach(object ob in raw){Component2 c=ob as Component2;if(c!=null)list.Add(c);}
        return list;
    }

    private static Component2 FindExact(IEnumerable<Component2> c,string path)
    {
        List<Component2> found=c.Where(x=>SameComponentPath(x,path)).ToList();
        Require(found.Count==1,"Expected one exact component; actual="+found.Count+" path="+path);
        return found[0];
    }

    private static bool SameComponentPath(Component2 c,string path)
    {
        return c!=null&&!string.IsNullOrWhiteSpace(c.GetPathName())&&SamePath(c.GetPathName(),path);
    }

    private static bool SameStem(Component2 c,string stem)
    {
        return c!=null&&string.Equals(Path.GetFileNameWithoutExtension(c.GetPathName()),stem,
            StringComparison.OrdinalIgnoreCase);
    }

    private static int CountExact(IEnumerable<Component2> c,string path)
    {
        return c.Count(x=>SameComponentPath(x,path));
    }

    private static int CountStem(IEnumerable<Component2> c,string stem)
    {
        return c.Count(x=>SameStem(x,stem));
    }

    private static double[] ReadTransform(Component2 component)
    {
        MathTransform t=component.Transform2; Array raw=t==null?null:t.ArrayData as Array;
        Require(raw!=null&&raw.Length>=16,"Component transform unavailable: "+component.Name2);
        double[] r=new double[16];
        for(int i=0;i<16;i++) r[i]=Convert.ToDouble(raw.GetValue(i),CultureInfo.InvariantCulture);
        return r;
    }

    private static void RequireTransformEqual(double[] actual,double[] expected,string label)
    {
        for(int i=0;i<12;i++) RequireClose(actual[i],expected[i],TransformTolerance,
            label+" transform "+i);
    }

    private static MathUtility RequireMath(RackCadSession cad)
    {
        MathUtility m=cad.Application.GetMathUtility() as MathUtility;
        Require(m!=null,"SOLIDWORKS MathUtility unavailable"); return m;
    }

    private static double ReadMass(ModelDoc2 doc)
    {
        MassProperty m=doc.Extension.CreateMassProperty();
        Require(m!=null,"MassProperty unavailable"); return m.Mass;
    }

    private static double ReadSavedPartMass(RackCadSession cad,string path)
    {
        RequireFile(path);
        ModelDoc2 doc=FindOpenDocumentByPath(cad,path);
        bool openedHere=false;
        int errors=0,warnings=0;
        if(doc==null)
        {
            doc=cad.Application.OpenDoc6(path,(int)swDocumentTypes_e.swDocPART,
                (int)swOpenDocOptions_e.swOpenDocOptions_Silent|
                (int)swOpenDocOptions_e.swOpenDocOptions_LoadModel,
                string.Empty,ref errors,ref warnings) as ModelDoc2;
            openedHere=true;
        }
        Require(doc!=null,"Cannot obtain part for native mass readback; errors="+
            errors+" path="+path);
        try
        {
            Require(SamePath(doc.GetPathName(),path),"Opened wrong part identity for mass gate: "+path);
            if(errors!=0||warnings!=0) cad.Log("WARNING: mass readback obtaining "+path+
                " returned errors="+errors+" warnings="+warnings);
            return ReadMass(doc);
        }
        finally { if(openedHere) cad.Application.CloseDoc(doc.GetTitle()); }
    }

    private static string PartPath(RackCadSession cad,string stem)
    {
        return Path.Combine(cad.PartsDirectory,stem+".SLDPRT");
    }

    private static string AssemblyPath(RackCadSession cad,string stem)
    {
        return Path.Combine(cad.AssembliesDirectory,stem+".SLDASM");
    }

    private static void RequireFile(string path)
    {
        if(!File.Exists(path)||new FileInfo(path).Length<=0)
            throw new FileNotFoundException("Required file missing or empty",path);
    }

    private static bool SamePath(string a,string b)
    {
        return !string.IsNullOrWhiteSpace(a)&&!string.IsNullOrWhiteSpace(b)&&
            string.Equals(Path.GetFullPath(a),Path.GetFullPath(b),StringComparison.OrdinalIgnoreCase);
    }

    private static string Hash(string path)
    {
        using(SHA256 sha=SHA256.Create()) using(FileStream f=File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(f)).Replace("-",string.Empty);
    }

    private static void RequireClose(double actual,double expected,double tolerance,string label)
    {
        if(double.IsNaN(actual)||double.IsInfinity(actual)||Math.Abs(actual-expected)>tolerance)
            throw new InvalidOperationException(label+" mismatch; expected="+F(expected)+
                " actual="+F(actual)+" tolerance="+F(tolerance));
    }

    private static void Require(bool condition,string message)
    {
        if(!condition) throw new InvalidOperationException(message);
    }

    private static double Degrees(double degrees){return degrees*Math.PI/180.0;}
    private static double Radians(double radians){return radians*180.0/Math.PI;}
    private static string F(double value){return value.ToString("0.######",CultureInfo.InvariantCulture);}

    private sealed class AssemblySpec
    {
        internal readonly string SourceStem,TargetStem;
        internal readonly bool Tilt60,HasLid,DetachedLid;
        internal AssemblySpec(string source,string target,bool tilt,bool lid,bool detached)
        {SourceStem=source;TargetStem=target;Tilt60=tilt;HasLid=lid;DetachedLid=detached;}
    }

    private sealed class SectionRectangle
    {
        internal readonly double Sign,Width,Depth,CentreY,CentreZ;
        internal SectionRectangle(double sign,double width,double depth,
            double centreY,double centreZ)
        {Sign=sign;Width=width;Depth=depth;CentreY=centreY;CentreZ=centreZ;}
    }

    private sealed class SectionProperties
    {
        internal readonly double Area,CentroidY,CentroidZ,Iy,Iz;
        internal SectionProperties(double area,double cy,double cz,double iy,double iz)
        {Area=area;CentroidY=cy;CentroidZ=cz;Iy=iy;Iz=iz;}
    }

    private sealed class PartPaths
    {
        internal string Side,BackLeg,OuterCheek,BackBushing,BackPivot,BackStop,
            Spacer,CheekFasteners,BackLockPack,BackHeel,BackHeelPin,BackFoot,
            Rail,RailThreadStrip,RailInsertPack,RailFastenerPack,UpperEdge,LowerEdge,
            Handle,HandleSpreader,HandleFasteners,CassetteFasteners,
            ShellCornerFasteners,Lid,CaseLatchPack,CaseLatchFasteners,
            LidLatchDoublers,LidLatchBodies,LidLatchBails,LidLatchFasteners,
            LidCompression,BackSkin,BackDoubler,BackPerimeterFasteners,BackFeet,
            BackFeetFasteners,VesaFrame,VesaFasteners,FrontBracketPack,FrontLink,
            FrontFoot,FrontPivotPack,FrontStopPack,FrontLockPack,FrontBracketFasteners;
    }

    private sealed class SourceStamp
    {
        internal readonly string Path,Hash; internal readonly long Length;
        internal readonly DateTime LastWriteUtc;
        internal SourceStamp(string p,long l,DateTime t,string h)
        {Path=p;Length=l;LastWriteUtc=t;Hash=h;}
    }

    private sealed class MountPoint
    {
        internal readonly double Y,Z; internal MountPoint(double y,double z){Y=y;Z=z;}
    }

    private sealed class CassetteMount
    {
        internal readonly double CentreX,HalfPitch;
        internal CassetteMount(double c,double h){CentreX=c;HalfPitch=h;}
    }

    private sealed class Point
    {
        internal readonly double X,Y,Z; internal Point(double x,double y,double z){X=x;Y=y;Z=z;}
    }

    private sealed class Stance
    {
        internal readonly double FaceAngleDegrees,AngleRadians,DetentDegrees,
            DetentRadians,HingeHeight,SupportFootprint;
        internal Stance(double face,double angle,double detentDeg,double detentRad,
            double hinge,double support)
        {FaceAngleDegrees=face;AngleRadians=angle;DetentDegrees=detentDeg;
            DetentRadians=detentRad;HingeHeight=hinge;SupportFootprint=support;}
    }
}
