using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Read-only native/STEP inspection for the V0.6 stable captured double-shear kickstand.
// The only intentional project write is reports/layout-v06-validation.md.
internal static class ValidateRackStableKickstandV06
{
    [STAThread]
    private static int Main(string[] arguments)
    {
        RackStableKickstandV06Validator validator = null;
        try
        {
            if (arguments.Length != 1)
            {
                throw new ArgumentException("Pass exactly one Rack4Modules project root.");
            }

            validator = new RackStableKickstandV06Validator(Path.GetFullPath(arguments[0]));
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

            Console.Error.WriteLine("V06_VALIDATION_FAILED=" + exception);
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

internal sealed class RackStableKickstandV06Validator
{
    private const double MillimetresPerMetre = 1000.0;
    private const double PositionTolerance = 0.08;
    private const double ContactTolerance = 0.10;
    private const double SweepContactTolerance = 0.02;
    private const double MatrixTolerance = 0.000002;
    private const double NegligibleInterferenceVolume = 0.001;
    private const int SweepSampleCount = 2001;

    private const double ShellContactY = -210.0;
    private const double ShellContactZ = 110.0;
    private const double HingeCaseY = -129.0;
    private const double HingeCaseZ = 52.0;
    private const double HingeLocalY = -75.0;
    private const double HingeLocalZ = 6.0;
    private const double FootLocalY = 95.0;
    private const double FootLocalZ = 6.0;
    private const double LegLength = 170.0;
    private const double FootRadius = 8.0;

    private const double InnerFrameX = 272.5;
    private const double LegPlaneX = 277.4;
    private const double OuterCheekX = 282.3;
    private const double PinPlaneX = 277.4;
    private const double NominalOuterFaceX = 283.8;
    private const double NominalPackageWidth = 567.6;
    private const double OuterCheekMainMinY = -170.0;
    private const double OuterCheekMainMaxY = 52.0;
    private const double OuterCheekMainMinZ = 24.0;
    private const double OuterCheekMainMaxZ = 78.0;
    private const double MinimumSpacerOuterCircleEdgeMaterial = 5.0;

    // Coordinates below are relative to the hinge in the rotating leg YZ plane.
    private const double StopTargetRelativeY = 14.0;
    private const double StopTargetRelativeZ = -28.0;
    private const double LockHoleRelativeY = 0.0;
    private const double LockHoleRelativeZ = -38.0;
    private const double HeelCenterRelativeY = 8.5;
    private const double HeelCenterRelativeZ = -28.0;
    private const double FootCenterRelativeY = 170.0;
    private const double FootCenterRelativeZ = 0.0;
    private const double StorageLockCaseY = -145.0;
    private const double StorageLockCaseZ = 72.0;

    private const double StopRadius = 4.0;
    private const double RootRadius = 18.0;
    private const double TargetDeskAngle = 60.0;
    private const double SpacerRadius = 5.0;
    private const double MinimumNominalSpacerSweepClearance = 2.0;
    private const double StopContactNormalMomentArm = 28.0;
    private const double ScreenSingleLegLoad = 400.0;
    private const double ScreenLoadFactor = 1.5;
    private const double ScreenStopReaction = 3642.8571429;

    private static readonly double[,] SpacerYz =
    {
        { -160.0, 39.0 }, { -160.0, 68.0 },
        { -70.0, 34.0 }, { 42.0, 34.0 }
    };

    private const string SideFrame = "SideFrame_V06_StableDoubleShearInner";
    private const string Kickstand = "SideKickstand_V06_170mm_6mm";
    private const string OuterCheek = "KickstandOuterCheek_V06_Stable";
    private const string PivotPin = "KickstandPivotPin_V06_8mm";
    private const string Spacer = "KickstandSpacer_V06_6p8mm";
    private const string LoadStopPin = "KickstandLoadStopPin_V06_8mm";
    private const string LockPin = "KickstandLockPin_V06_5mm";
    private const string HeelInsert = "KickstandHeelInsert_V06";
    private const string FootPad = "KickstandFootPad_V06_Rubber";

    private const string BackPanel = "BackPanel_V03_VESAOnly";
    private const string UpperEdge = "UpperEdge_V04_Adapter_MIDI_Handle_Audio_ClearPower";
    private const string LowerEdge = "LowerEdge_V03_HiddenVent";
    private const string StructuralRail = "Rail_104HP_V04_SpineDualFix";
    private const string ThreadStrip = "ThreadStrip_104HP_M3_AISI304_V04";
    private const string EndBlock = "RailEndBlock_M3";
    private const string CrossBeam = "RearCrossBeam_6061";
    private const string VesaStile = "VesaStile_6061";
    private const string VesaBridge = "VesaBridge_6061_V04_DirectMount";
    private const string AudioPlate = "UpperAudio_V04_2x4_TRS635";
    private const string MidiUsbPlate = "UpperMidiUsb_V04_3xDIN_USB_C_Inline";
    private const string PowerBlank = "UpperAdapterBlank_V04_95mm";
    private const string CarryHandle = "RearCarryHandle_V03_ClearanceFit";
    private const string LidCatch = "InternalLidCatch_V03";
    private const string BackFeet = "FourBackFeet_V03";
    private const string TravelLid = "DeepTravelLid_V06_StandRelief";
    private const string FitGauge = "FitGauge_104HP_3U";
    private const string ModuleEnvelope = "ModuleDepthEnvelope_85mm_V03";
    private const string PowerBus = "ReservedPowerBus_500x85x20";
    private const string PowerSupply = "ReservedPowerSupply_210x90x45";
    private const string DesktopReference = "DesktopReferenceSurface_V04";

    private static readonly string[] V06PartStems =
    {
        SideFrame, Kickstand, OuterCheek, PivotPin, Spacer,
        LoadStopPin, LockPin, HeelInsert, FootPad, TravelLid
    };

    private static readonly string[] LegacyKickstandPrefixes =
    {
        "SideFrame_V04", "SideFrame_V05",
        "SideKickstand_V04", "SideKickstand_V05",
        "KickstandOuterCheek_V05", "KickstandPivotPin_V05",
        "KickstandSpacer_V05", "KickstandIndexPin_V05"
    };

    private readonly string root;
    private readonly string reportPath;
    private readonly StringBuilder report = new StringBuilder();
    private readonly Dictionary<string, PartSnapshot> partCache =
        new Dictionary<string, PartSnapshot>(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> ownedDocumentTitles = new List<string>();

    private SldWorks application;
    private string originalActiveTitle;
    private int passes;
    private int warnings;
    private int failures;

    internal RackStableKickstandV06Validator(string projectRoot)
    {
        if (!Directory.Exists(projectRoot))
        {
            throw new DirectoryNotFoundException("Rack4Modules root does not exist: " + projectRoot);
        }

        root = projectRoot;
        reportPath = Path.Combine(root, "reports", "layout-v06-validation.md");
        report.AppendLine("# Rack4Modules V0.6 稳定双剪折叠脚架验证");
        report.AppendLine();
        report.AppendLine("生成时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        report.AppendLine("项目根目录：" + root);
        report.AppendLine();
        report.AppendLine("本程序只读检查原生 SLDPRT/SLDASM 和临时 STEP 副本；不会保存或替换 CAD 文件。");
    }

    internal int FailureCount
    {
        get { return failures; }
    }

    internal void Run()
    {
        ValidateFilesystemInventory();
        AttachSolidWorks();
        ValidateV06PartGeometry();
        ValidateIdealizedContinuousSweep();
        ValidateEngineeringScreens();

        StageSpec[] stages =
        {
            new StageSpec("Open", "Rack4Modules_OpenCase_V06", 66, false, false, null),
            new StageSpec("Transport", "Rack4Modules_TransportClosed_V06", 67, true, false, null),
            new StageSpec("Clearance", "Rack4Modules_ClearanceCheck_V06", 74, false, true, null),
            new StageSpec("Tilt60", "Rack4Modules_DesktopTilt60_V06", 67, false, false, TargetDeskAngle)
        };

        foreach (StageSpec stage in stages)
        {
            ValidateStage(stage);
        }

        Section("必须保留的实物验证边界");
        Warn("整机质量、三维重心、摩擦和操作力尚未实测",
            "未以真实模块配置测量整机质量/三维 CG、桌面摩擦系数、20 N 解锁/折叠操作力，也未据此证明 1.5 稳定安全系数；不得记为 PASS。");
        Warn("规定载荷尚未完成实体试验",
            "400 N 单腿竖向载荷和 30 N 侧向载荷未在样机上施加；当前 CAD 与二维扫掠不等于承载认证。");
        Warn("10000 次疲劳寿命尚未验证",
            "主轴孔、锁销孔、硬止挡、脚跟嵌片、隔柱及紧固件尚未完成 10000 次折叠/锁止循环和松动复检。");
        Warn("主轴与锁销供应商件尚未冻结",
            "Ø8 主轴和 Ø5 锁销仅按 12.8 mm 贯穿包络检查；肩轴、轴套、弹簧、保持件、螺纹避开剪切面及供应商公差尚未确认。");
        Warn("橡胶脚机械卡持尚未设计验证",
            "Ø16 x 6 mm 橡胶脚只作为接触包络；防拔脱、机械卡槽/螺钉、胶粘剂老化及更换方式尚未冻结。");
        Warn("实物公差链与加工装配尚未验证",
            "板厚、弯曲/切削、孔位、轴销、阳极层、装配间隙和左右侧同步误差需做完整公差链及首件实测。");
        Warn("连续 SOLIDWORKS 运动扫掠尚未执行",
            "本程序完成 2001 点理想二维截面扫掠，但没有替代包含倒角、紧固件、三维实体和装配公差的 SOLIDWORKS Motion/连续碰撞扫掠。");

        WriteReport();
        Console.WriteLine("V06_VALIDATION_REPORT=" + reportPath);
        Console.WriteLine("V06_VALIDATION_PASS=" + passes.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("V06_VALIDATION_WARNING=" + warnings.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("V06_VALIDATION_FAIL=" + failures.ToString(CultureInfo.InvariantCulture));
    }

    internal void RecordFatal(Exception exception)
    {
        Section("未处理的验证异常");
        Fail("验证器完成全部请求检查", exception.GetType().Name + ": " + exception.Message);
    }

    internal void WriteReport()
    {
        StringBuilder output = new StringBuilder(report.ToString());
        output.AppendLine();
        output.AppendLine("## 汇总");
        output.AppendLine();
        output.AppendLine("状态：**" + (failures == 0 ? "PASS" : "FAIL") + "**");
        output.AppendLine();
        output.AppendLine("PASS: " + passes.ToString(CultureInfo.InvariantCulture));
        output.AppendLine("WARNING: " + warnings.ToString(CultureInfo.InvariantCulture));
        output.AppendLine("FAIL: " + failures.ToString(CultureInfo.InvariantCulture));
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
                // A temporary import or assembly dependency may already be closed.
            }
        }

        if (!string.IsNullOrEmpty(originalActiveTitle))
        {
            try
            {
                int activationError = 0;
                application.ActivateDoc3(originalActiveTitle, false,
                    (int)swRebuildOnActivation_e.swDontRebuildActiveDoc, ref activationError);
            }
            catch (COMException)
            {
                // Restoring the prior active document must never trigger a save.
            }
        }
    }

    private void ValidateFilesystemInventory()
    {
        Section("文件系统清单");
        string partsDirectory = Path.Combine(root, "cad", "parts");
        string assembliesDirectory = Path.Combine(root, "cad", "assemblies");
        string exportsDirectory = Path.Combine(root, "exports");
        Check(Directory.Exists(partsDirectory), "原生零件目录存在", partsDirectory);
        Check(Directory.Exists(assembliesDirectory), "原生装配目录存在", assembliesDirectory);
        Check(Directory.Exists(exportsDirectory), "STEP 导出目录存在", exportsDirectory);

        foreach (string stem in V06PartStems)
        {
            string path = PartPath(stem);
            Check(File.Exists(path) && !IsLockFile(path), "V0.6 原生零件存在：" + stem, path);
        }

        string[] assemblies =
        {
            "Rack4Modules_OpenCase_V06", "Rack4Modules_TransportClosed_V06",
            "Rack4Modules_ClearanceCheck_V06", "Rack4Modules_DesktopTilt60_V06"
        };
        foreach (string stem in assemblies)
        {
            string native = Path.Combine(assembliesDirectory, stem + ".SLDASM");
            string step = Path.Combine(exportsDirectory, stem + ".STEP");
            Check(File.Exists(native) && !IsLockFile(native), "V0.6 原生装配存在：" + stem, native);
            Check(File.Exists(step) && !IsLockFile(step), "V0.6 STEP 存在：" + stem, step);
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
                // Try the unversioned registration next.
            }
        }

        if (application == null)
        {
            throw new InvalidOperationException(
                "Start SOLIDWORKS before validation; this program never starts a new SOLIDWORKS process.");
        }

        ModelDoc2 active = application.ActiveDoc as ModelDoc2;
        originalActiveTitle = active == null ? null : active.GetTitle();
        Section("SOLIDWORKS 会话");
        Pass("连接到已运行的 SOLIDWORKS", application.RevisionNumber());
    }

    private void ValidateV06PartGeometry()
    {
        Section("V0.6 稳定双剪零件几何、实体与材料");
        PartSnapshot side = GetPart(SideFrame);
        PartSnapshot leg = GetPart(Kickstand);
        PartSnapshot cheek = GetPart(OuterCheek);
        PartSnapshot pivot = GetPart(PivotPin);
        PartSnapshot spacer = GetPart(Spacer);
        PartSnapshot stop = GetPart(LoadStopPin);
        PartSnapshot lockPin = GetPart(LockPin);
        PartSnapshot heel = GetPart(HeelInsert);
        PartSnapshot foot = GetPart(FootPad);
        PartSnapshot travelLid = GetPart(TravelLid);

        double deployment = DeploymentDetent(TargetDeskAngle);
        Point stopCase = LegRelativeToCase(StopTargetRelativeY, StopTargetRelativeZ, deployment);
        Point lockCase = LegRelativeToCase(LockHoleRelativeY, LockHoleRelativeZ, deployment);

        if (side != null)
        {
            Check(Almost(AxisLength(side.Box, 0), 3.0), "内侧框厚度", Describe(AxisLength(side.Box, 0), "3.0 mm"));
            Check(Almost(AxisLength(side.Box, 1), 420.0) && Almost(AxisLength(side.Box, 2), 108.0),
                "内侧框主体包络", "实际 " + BoxDescription(side.Box) + "；目标 3 x 420 x 108 mm");
            Check(HasAxisCylinder(side, 0, HingeCaseY, HingeCaseZ, 8.2),
                "内侧框 Ø8.2 主轴净孔", "轴线 y=-129, z=52 mm；Ø8 轴销结构净孔");
            Check(HasAxisCylinder(side, 0, HingeCaseY + stopCase.Y, HingeCaseZ + stopCase.Z, 8.2),
                "内侧框 Ø8.2 承力止挡净孔", "部署轴线 y=" + Format(HingeCaseY + stopCase.Y) +
                ", z=" + Format(HingeCaseZ + stopCase.Z) + " mm；Ø8 止挡销净孔");
            Check(HasAxisCylinder(side, 0, HingeCaseY + lockCase.Y, HingeCaseZ + lockCase.Z, 5.8),
                "内侧框 Ø5.8 部署锁销净孔", "轴线 y=" + Format(HingeCaseY + lockCase.Y) +
                ", z=" + Format(HingeCaseZ + lockCase.Z) + " mm；Ø5 锁销净孔");
            Check(HasAxisCylinder(side, 0, StorageLockCaseY, StorageLockCaseZ, 5.8),
                "内侧框 Ø5.8 收纳锁销净孔", "轴线 y=-145, z=72 mm；Ø5 锁销净孔");
            Check(MaterialIs(side, "6061"), "内侧框材料标记", MaterialDescription(side));
        }

        if (leg != null)
        {
            Check(Almost(AxisLength(leg.Box, 0), 6.0), "折叠腿厚度", Describe(AxisLength(leg.Box, 0), "6.0 mm 7075-T6"));
            Check(HasAxisCylinder(leg, 0, HingeLocalY, HingeLocalZ, 8.2),
                "折叠腿 Ø8.2 主轴净孔", "局部轴线 y=-75, z=6 mm；Ø8 轴销结构净孔");
            Check(HasAxisCylinder(leg, 0, HingeLocalY + LockHoleRelativeY,
                    HingeLocalZ + LockHoleRelativeZ, 5.2),
                "折叠腿 Ø5.2 部署锁销净孔", "局部轴线 y=-75, z=-32 mm；Ø5 锁销净孔");
            Check(HasAxisCylinder(leg, 0, HingeLocalY, HingeLocalZ, 36.0),
                "折叠腿 Ø36 加强根圆", "根圆半径 18 mm，轴线与主轴同心");
            Check(MaterialIs(leg, "7075-T6"), "折叠腿材料标记", MaterialDescription(leg));
        }

        if (cheek != null)
        {
            Check(Almost(AxisLength(cheek.Box, 0), 3.0), "外颊板厚度", Describe(AxisLength(cheek.Box, 0), "3.0 mm"));
            Check(HasAxisCylinder(cheek, 0, HingeCaseY, HingeCaseZ, 8.2), "外颊板 Ø8.2 主轴净孔", "轴线 y=-129, z=52 mm；Ø8 轴销净孔");
            Check(HasAxisCylinder(cheek, 0, HingeCaseY + stopCase.Y, HingeCaseZ + stopCase.Z, 8.2),
                "外颊板 Ø8.2 承力止挡净孔", "轴线 y=" + Format(HingeCaseY + stopCase.Y) +
                ", z=" + Format(HingeCaseZ + stopCase.Z) + " mm；Ø8 止挡销净孔");
            Check(HasAxisCylinder(cheek, 0, HingeCaseY + lockCase.Y, HingeCaseZ + lockCase.Z, 5.8),
                "外颊板 Ø5.8 部署锁销净孔", "轴线 y=" + Format(HingeCaseY + lockCase.Y) +
                ", z=" + Format(HingeCaseZ + lockCase.Z) + " mm；Ø5 锁销净孔");
            Check(HasAxisCylinder(cheek, 0, StorageLockCaseY, StorageLockCaseZ, 5.8),
                "外颊板 Ø5.8 收纳锁销净孔", "轴线 y=-145, z=72 mm；Ø5 锁销净孔");
            Check(MaterialIs(cheek, "6061"), "外颊板材料标记", MaterialDescription(cheek));
        }

        ValidatePinPart(pivot, "主轴", 8.0, 12.8);
        ValidatePinPart(stop, "承力硬止挡", 8.0, 12.8);
        ValidatePinPart(lockPin, "锁销", 5.0, 12.8);

        if (spacer != null)
        {
            Check(Almost(AxisLength(spacer.Box, 0), 6.8), "承力隔柱夹层长度", Describe(AxisLength(spacer.Box, 0), "6.8 mm"));
            Check(HasAxisCylinder(spacer, 0, 0.0, 0.0, null), "隔柱存在真实横向圆柱面", "局部轴线 y=0, z=0 mm");
            Check(MaterialIs(spacer, "AISI 304"), "隔柱材料标记", MaterialDescription(spacer));
        }

        if (heel != null)
        {
            Check(Almost(AxisLength(heel.Box, 0), 6.0) && Almost(AxisLength(heel.Box, 1), 3.0) &&
                  Almost(AxisLength(heel.Box, 2), 16.0),
                "脚跟耐磨嵌片 6 x 3 x 16 mm", "实际 " + BoxDescription(heel.Box));
            Check(MaterialIs(heel, "AISI 304"), "脚跟嵌片材料标记", MaterialDescription(heel));
        }

        if (foot != null)
        {
            Check(Almost(AxisLength(foot.Box, 0), 6.0) && Almost(AxisLength(foot.Box, 1), 16.0) &&
                  Almost(AxisLength(foot.Box, 2), 16.0),
                "橡胶脚 Ø16 x 6 mm 包络", "实际 " + BoxDescription(foot.Box));
            Check(HasAxisCylinder(foot, 0, 0.0, 0.0, 16.0), "橡胶脚真实 Ø16 圆柱面", "轴向 X，轴心为零件原点");
            Check(MaterialIs(foot, "NEOPRENE"), "橡胶脚材料标记", MaterialDescription(foot));
        }

        if (travelLid != null)
        {
            Check(travelLid.Bodies.Count == 5,
                "V0.6 运输盖五实体折弯概念",
                "面板、左右侧回边和上下回边共 " + travelLid.Bodies.Count + " 个实体");
            Check(Almost(AxisLength(travelLid.Box, 0), 552.0) &&
                  Almost(AxisLength(travelLid.Box, 1), 424.0) &&
                  Almost(AxisLength(travelLid.Box, 2), 83.5),
                "V0.6 运输盖外包络",
                "实际 " + BoxDescription(travelLid.Box) + "；目标 552 x 424 x 83.5 mm");
            Check(MaterialIs(travelLid, "5052"),
                "V0.6 运输盖材料标记", MaterialDescription(travelLid));
        }
    }

    private void ValidateEngineeringScreens()
    {
        Section("早期稳定与止挡载荷算术复核");

        double contactNormalMomentArm = Math.Abs(StopTargetRelativeZ);
        Check(Almost(contactNormalMomentArm, StopContactNormalMomentArm),
            "止挡接触法向力臂按 28.0 mm 计算",
            "Ø8 止挡圆柱与脚跟 y=10 平面相切，接触法向沿腿局部 Y；力臂=|z|=" +
            Format(contactNormalMomentArm) + " mm，而不是主轴到止挡中心的径向距离");

        double reaction = ScreenSingleLegLoad * LegLength * ScreenLoadFactor /
            contactNormalMomentArm;
        Check(Math.Abs(reaction - ScreenStopReaction) <= 0.001,
            "400 N 单腿、170 mm、1.5 倍系数的止挡反力算术",
            Format(reaction / 1000.0) + " kN；仅为早期量级筛查，不是结构认证");

        for (int index = 0; index < SpacerYz.GetLength(0); index++)
        {
            double y = SpacerYz[index, 0];
            double z = SpacerYz[index, 1];
            double outerCircleEdgeMaterial = Math.Min(
                Math.Min(y - OuterCheekMainMinY - SpacerRadius,
                    OuterCheekMainMaxY - y - SpacerRadius),
                Math.Min(z - OuterCheekMainMinZ - SpacerRadius,
                    OuterCheekMainMaxZ - z - SpacerRadius));
            Check(outerCircleEdgeMaterial >= MinimumSpacerOuterCircleEdgeMaterial - 0.000001,
                "隔柱 " + (index + 1).ToString(CultureInfo.InvariantCulture) +
                " 的 Ø10 外圆到外颊板主片边缘至少保留 5.0 mm",
                "中心=(" + Format(y) + "," + Format(z) + ") mm；最小名义实体=" +
                Format(outerCircleEdgeMaterial) + " mm");
        }
    }

    private void ValidatePinPart(PartSnapshot part, string label, double diameter, double length)
    {
        if (part == null)
        {
            return;
        }

        Check(Almost(AxisLength(part.Box, 0), length), label + "轴向长度",
            Describe(AxisLength(part.Box, 0), Format(length) + " mm"));
        Check(Almost(AxisLength(part.Box, 1), diameter) && Almost(AxisLength(part.Box, 2), diameter),
            label + "直径包络", "实际 " + BoxDescription(part.Box) + "；目标 Ø" + Format(diameter) + " mm");
        Check(HasAxisCylinder(part, 0, 0.0, 0.0, diameter), label + "真实圆柱面", "轴向 X，轴心为零件原点");
        Check(MaterialIs(part, "AISI 304"), label + "材料标记", MaterialDescription(part));
    }

    private void ValidateIdealizedContinuousSweep()
    {
        Section("2001 点理想二维连续扫掠");
        double target = DeploymentDetent(TargetDeskAngle);
        Point fixedStopInCase = LegRelativeToCase(StopTargetRelativeY, StopTargetRelativeZ, target);
        SweepMinimum overall = new SweepMinimum("联合轮廓");
        SweepMinimum root = new SweepMinimum("Ø36 根圆");
        SweepMinimum ear = new SweepMinimum("切座铝耳");
        SweepMinimum heel = new SweepMinimum("脚跟嵌片");
        int nearZeroBeforeEnd = 0;
        double terminalGap = double.NaN;

        for (int index = 0; index < SweepSampleCount; index++)
        {
            double fraction = index / (double)(SweepSampleCount - 1);
            double angle = target * fraction;
            Point stopInLeg = CaseRelativeToLeg(fixedStopInCase.Y, fixedStopInCase.Z, angle);
            double rootGap = Math.Sqrt(stopInLeg.Y * stopInLeg.Y + stopInLeg.Z * stopInLeg.Z) -
                RootRadius - StopRadius;
            double earGap = EarClearance(stopInLeg.Y, stopInLeg.Z);
            double heelGap = CircleToRectangleClearance(stopInLeg.Y, stopInLeg.Z,
                7.0, 10.0, -36.0, -20.0, StopRadius);
            double combined = Math.Min(rootGap, Math.Min(earGap, heelGap));
            root.Consider(rootGap, angle, index, stopInLeg);
            ear.Consider(earGap, angle, index, stopInLeg);
            heel.Consider(heelGap, angle, index, stopInLeg);
            overall.Consider(combined, angle, index, stopInLeg);

            if (index < SweepSampleCount - 1 && Math.Abs(combined) <= SweepContactTolerance)
            {
                nearZeroBeforeEnd++;
            }

            if (index == SweepSampleCount - 1)
            {
                terminalGap = combined;
            }
        }

        Check(SweepSampleCount >= 2001, "扫掠采样数不少于 2001", SweepSampleCount + " 点，包含收纳与部署端点");
        Check(overall.Gap >= -SweepContactTolerance,
            "收纳至 60° 目标全程联合间隙不小于 -0.02 mm", SweepDescription(overall));
        Check(Math.Abs(terminalGap) <= SweepContactTolerance && overall.Index == SweepSampleCount - 1,
            "硬止挡仅在部署终点达到零间隙", "终点间隙=" + Format(terminalGap) +
            " mm；全程最小值索引=" + overall.Index + "/" + (SweepSampleCount - 1));
        Check(nearZeroBeforeEnd == 0,
            "终点之前没有 ±0.02 mm 近接触", "终点前近零间隙采样点=" + nearZeroBeforeEnd);
        Check(Math.Abs(heel.Gap) <= SweepContactTolerance && ear.Gap > SweepContactTolerance &&
              root.Gap > SweepContactTolerance,
            "终点由脚跟嵌片承载而非根圆/铝耳卡死",
            "脚跟 " + SweepDescription(heel) + "；铝耳 " + SweepDescription(ear) +
            "；根圆 " + SweepDescription(root));
        ValidateSpacerSweep(target);
        Note("冻结部署角=" + Format(target) + "°；固定止挡相对主轴 case 坐标=(" +
            Format(fixedStopInCase.Y) + "," + Format(fixedStopInCase.Z) + ") mm。");
        Note("联合最小间隙：" + SweepDescription(overall));
        Note("分轮廓最小值：根圆 " + SweepDescription(root) + "；铝耳 " +
            SweepDescription(ear) + "；脚跟 " + SweepDescription(heel) + "。");
    }

    private void ValidateSpacerSweep(double target)
    {
        for (int spacerIndex = 0; spacerIndex < SpacerYz.GetLength(0); spacerIndex++)
        {
            double caseY = SpacerYz[spacerIndex, 0];
            double caseZ = SpacerYz[spacerIndex, 1];
            SweepMinimum clearance = new SweepMinimum(
                "隔柱(" + Format(caseY) + "," + Format(caseZ) + ")");

            for (int index = 0; index < SweepSampleCount; index++)
            {
                double fraction = index / (double)(SweepSampleCount - 1);
                double angle = target * fraction;
                Point spacerInLeg = CaseRelativeToLeg(
                    caseY - HingeCaseY, caseZ - HingeCaseZ, angle);

                double rootGap = Math.Sqrt(
                    spacerInLeg.Y * spacerInLeg.Y + spacerInLeg.Z * spacerInLeg.Z) -
                    RootRadius - SpacerRadius;
                double armGap = CircleToRectangleClearance(
                    spacerInLeg.Y, spacerInLeg.Z,
                    -5.0, 162.0, -9.0, 9.0, SpacerRadius);
                double earGap = CircleToRectangleClearance(
                    spacerInLeg.Y, spacerInLeg.Z,
                    -8.0, 8.0, -44.0, -12.0, SpacerRadius);
                double heelGap = CircleToRectangleClearance(
                    spacerInLeg.Y, spacerInLeg.Z,
                    7.0, 10.0, -36.0, -20.0, SpacerRadius);
                double footGap = Math.Sqrt(
                    (spacerInLeg.Y - FootCenterRelativeY) *
                    (spacerInLeg.Y - FootCenterRelativeY) +
                    (spacerInLeg.Z - FootCenterRelativeZ) *
                    (spacerInLeg.Z - FootCenterRelativeZ)) -
                    FootRadius - SpacerRadius;
                double combined = Math.Min(rootGap,
                    Math.Min(armGap, Math.Min(earGap, Math.Min(heelGap, footGap))));
                clearance.Consider(combined, angle, index, spacerInLeg);
            }

            Check(clearance.Gap >= MinimumNominalSpacerSweepClearance,
                "隔柱 " + (spacerIndex + 1).ToString(CultureInfo.InvariantCulture) +
                " 与折腿/脚垫全扫掠名义净距不少于 2.0 mm",
                SweepDescription(clearance));
        }
    }

    private void ValidateStage(StageSpec stage)
    {
        Section(stage.Label + "：原生装配与 STEP");
        string nativePath = Path.Combine(root, "cad", "assemblies", stage.Stem + ".SLDASM");
        string stepPath = Path.Combine(root, "exports", stage.Stem + ".STEP");
        bool nativeExists = File.Exists(nativePath) && !IsLockFile(nativePath);
        bool stepExists = File.Exists(stepPath) && !IsLockFile(stepPath);
        Check(nativeExists, stage.Label + " 原生 SLDASM 存在", nativePath);
        Check(stepExists, stage.Label + " STEP 存在", stepPath);

        List<ExpectedInstance> expected = BuildExpectedInstances(stage);
        Check(expected.Count == stage.ExpectedCount, stage.Label + " 验证公式自身的实例总数",
            "公式=" + expected.Count + "，冻结值=" + stage.ExpectedCount);

        ModelDoc2 model = null;
        if (nativeExists)
        {
            try
            {
                model = OpenNative(nativePath, swDocumentTypes_e.swDocASSEMBLY);
                Check(model is AssemblyDoc && ExactPath(model.GetPathName(), nativePath),
                    stage.Label + " 原生装配可只读打开", nativePath);
            }
            catch (Exception exception)
            {
                Fail(stage.Label + " 原生装配可只读打开", exception.Message);
            }
        }

        if (model != null)
        {
            ValidateAssemblyContents(stage, model, expected);
            DetectDiscreteInterference(stage, model);
        }

        if (stepExists)
        {
            ValidateStepImport(stage, stepPath);
        }
    }

    private void ValidateAssemblyContents(StageSpec stage, ModelDoc2 model,
        List<ExpectedInstance> expected)
    {
        AssemblyDoc assembly = model as AssemblyDoc;
        if (assembly == null)
        {
            Fail(stage.Label + " 文档类型", "打开的原生文件不是 SLDASM。");
            return;
        }

        List<ComponentSnapshot> components = ReadTopLevelComponents(assembly);
        int apiCount = assembly.GetComponentCount(true);
        Check(apiCount == stage.ExpectedCount && components.Count == stage.ExpectedCount,
            stage.Label + " 精确顶层组件数",
            "冻结=" + stage.ExpectedCount + "，API=" + apiCount + "，枚举=" + components.Count);

        Dictionary<string, List<ComponentSnapshot>> actualGroups = GroupActual(components);
        Dictionary<string, List<ExpectedInstance>> expectedGroups = GroupExpected(expected);
        foreach (string prefix in LegacyKickstandPrefixes)
        {
            List<string> found = new List<string>();
            foreach (string stem in actualGroups.Keys)
            {
                if (stem.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    found.Add(stem);
                }
            }

            Check(found.Count == 0, stage.Label + " 禁止 V0.4/V0.5 旧脚架残留：" + prefix,
                found.Count == 0 ? "未发现" : string.Join(", ", found.ToArray()));
        }

        foreach (KeyValuePair<string, List<ExpectedInstance>> pair in expectedGroups)
        {
            List<ComponentSnapshot> actual;
            actualGroups.TryGetValue(pair.Key, out actual);
            int actualCount = actual == null ? 0 : actual.Count;
            Check(actualCount == pair.Value.Count, stage.Label + " 数量：" + pair.Key,
                "冻结=" + pair.Value.Count + "，实际=" + actualCount);
            if (actual == null)
            {
                continue;
            }

            string expectedPath = PartPath(pair.Key);
            bool pathsMatch = true;
            List<string> wrongPaths = new List<string>();
            foreach (ComponentSnapshot component in actual)
            {
                if (!ExactPath(component.Path, expectedPath))
                {
                    pathsMatch = false;
                    wrongPaths.Add(string.IsNullOrEmpty(component.Path) ? "<virtual/empty>" : component.Path);
                }
            }

            Check(pathsMatch, stage.Label + " 精确源路径：" + pair.Key,
                pathsMatch ? expectedPath : "预期 " + expectedPath + "；实际 " + string.Join(" | ", wrongPaths.ToArray()));
            ValidateStemTransforms(stage, pair.Key, pair.Value, actual);
        }

        foreach (KeyValuePair<string, List<ComponentSnapshot>> pair in actualGroups)
        {
            if (!expectedGroups.ContainsKey(pair.Key))
            {
                Fail(stage.Label + " 无意外/旧版组件", pair.Key + " x " + pair.Value.Count);
            }
        }

        ValidateLayerGeometry(stage, components);
        ValidateCoaxialStacks(stage, components);
        ValidatePhysicalEnvelope(stage, components);
        ValidatePoseGeometry(stage, components);
    }

    private List<ExpectedInstance> BuildExpectedInstances(StageSpec stage)
    {
        List<ExpectedInstance> result = new List<ExpectedInstance>();
        Add(result, BackPanel, 0.0, 0.0, 0.0);
        Add(result, SideFrame, -InnerFrameX, 0.0, 0.0);
        Add(result, SideFrame, InnerFrameX, 0.0, 0.0);
        Add(result, UpperEdge, 0.0, 209.0, 0.0);
        Add(result, LowerEdge, 0.0, -209.0, 0.0);

        double[] railY = { -194.6, -72.1, -61.25, 61.25, 72.1, 194.6 };
        foreach (double y in railY)
        {
            Add(result, StructuralRail, 0.0, y, 0.0);
            Add(result, ThreadStrip, 0.0, y, 4.0);
            Add(result, EndBlock, -267.58, y, 0.0);
            Add(result, EndBlock, 267.58, y, 0.0);
        }

        Add(result, CrossBeam, 0.0, -155.0, 99.0);
        Add(result, CrossBeam, 0.0, 155.0, 99.0);
        Add(result, VesaStile, -115.0, 0.0, 93.0);
        Add(result, VesaStile, 115.0, 0.0, 93.0);
        Add(result, VesaBridge, 0.0, -50.0, 99.0);
        Add(result, VesaBridge, 0.0, 50.0, 99.0);
        Add(result, AudioPlate, 165.0, 211.0, 15.0);
        Add(result, MidiUsbPlate, -116.0, 211.0, 15.0);
        Add(result, PowerBlank, -218.5, 211.0, 15.0);
        Add(result, CarryHandle, 0.0, 215.0, 45.0);
        AddLeg(result, -LegPlaneX);
        AddLeg(result, LegPlaneX);

        foreach (double x in new double[] { -272.0, 272.0 })
        {
            foreach (double y in new double[] { -150.0, 150.0 })
            {
                Add(result, LidCatch, x, y, 55.0);
            }
        }

        Add(result, BackFeet, 0.0, 0.0, 110.0);
        Add(result, OuterCheek, -OuterCheekX, 0.0, 0.0);
        Add(result, OuterCheek, OuterCheekX, 0.0, 0.0);
        Add(result, PivotPin, -PinPlaneX, HingeCaseY, HingeCaseZ);
        Add(result, PivotPin, PinPlaneX, HingeCaseY, HingeCaseZ);

        foreach (double x in new double[] { -LegPlaneX, LegPlaneX })
        {
            for (int index = 0; index < SpacerYz.GetLength(0); index++)
            {
                Add(result, Spacer, x, SpacerYz[index, 0], SpacerYz[index, 1]);
            }
        }

        double deployment = DeploymentDetent(TargetDeskAngle);
        Point stopCase = LegRelativeToCase(StopTargetRelativeY, StopTargetRelativeZ, deployment);
        Point deployLockCase = LegRelativeToCase(LockHoleRelativeY, LockHoleRelativeZ, deployment);
        foreach (double x in new double[] { -PinPlaneX, PinPlaneX })
        {
            Add(result, LoadStopPin, x, HingeCaseY + stopCase.Y, HingeCaseZ + stopCase.Z);
            Add(result, LockPin, x,
                stage.AngleDegrees.HasValue ? HingeCaseY + deployLockCase.Y : StorageLockCaseY,
                stage.AngleDegrees.HasValue ? HingeCaseZ + deployLockCase.Z : StorageLockCaseZ);
            AddFollower(result, HeelInsert, x, HeelCenterRelativeY, HeelCenterRelativeZ);
            AddFollower(result, FootPad, x, FootCenterRelativeY, FootCenterRelativeZ);
        }

        if (stage.IncludesLid)
        {
            Add(result, TravelLid, 0.0, 0.0, 0.0);
        }

        if (stage.IncludesClearance)
        {
            foreach (double y in new double[] { -133.35, 0.0, 133.35 })
            {
                Add(result, FitGauge, 0.0, y, 0.0);
                Add(result, ModuleEnvelope, 0.0, y, 0.0);
            }

            Add(result, PowerBus, 0.0, -105.0, 73.0);
            Add(result, PowerSupply, 0.0, 0.0, 60.0);
        }

        if (stage.AngleDegrees.HasValue)
        {
            ExpectedInstance desktop = new ExpectedInstance();
            desktop.Stem = DesktopReference;
            desktop.Mode = TransformMode.WorldIdentity;
            result.Add(desktop);
        }

        return result;
    }

    private static void Add(List<ExpectedInstance> result, string stem, double x, double y, double z)
    {
        result.Add(new ExpectedInstance { Stem = stem, X = x, Y = y, Z = z, Mode = TransformMode.RigidCase });
    }

    private static void AddLeg(List<ExpectedInstance> result, double x)
    {
        result.Add(new ExpectedInstance
        {
            Stem = Kickstand, X = x, Y = -54.0, Z = 46.0, Mode = TransformMode.Kickstand
        });
    }

    private static void AddFollower(List<ExpectedInstance> result, string stem,
        double x, double relativeY, double relativeZ)
    {
        result.Add(new ExpectedInstance
        {
            Stem = stem, X = x, Y = relativeY, Z = relativeZ, Mode = TransformMode.LegFollower
        });
    }

    private void ValidateStemTransforms(StageSpec stage, string stem,
        List<ExpectedInstance> expected, List<ComponentSnapshot> actual)
    {
        List<ComponentSnapshot> unused = new List<ComponentSnapshot>(actual);
        bool allMatch = expected.Count == actual.Count;
        List<string> differences = new List<string>();
        foreach (ExpectedInstance instance in expected)
        {
            double[] target = ExpectedTransform(stage, instance);
            ComponentSnapshot best = null;
            double bestMetric = double.PositiveInfinity;
            foreach (ComponentSnapshot candidate in unused)
            {
                double metric = TransformMetric(candidate.Transform, target);
                if (metric < bestMetric)
                {
                    bestMetric = metric;
                    best = candidate;
                }
            }

            if (best == null)
            {
                allMatch = false;
                differences.Add("缺少 " + ExpectedCoordinates(stage, instance));
                continue;
            }

            unused.Remove(best);
            if (!TransformMatches(best.Transform, target))
            {
                allMatch = false;
                differences.Add("预期 " + ExpectedCoordinates(stage, instance) + "，实际 " + Coordinates(best));
            }
        }

        if (unused.Count > 0)
        {
            allMatch = false;
            differences.Add("多出 " + unused.Count + " 个实例");
        }

        Check(allMatch, stage.Label + " 精确变换：" + stem,
            allMatch ? expected.Count + " 个实例的 3x3 姿态矩阵和 XYZ 平移均匹配" :
            string.Join("；", differences.ToArray()));
    }

    private double[] ExpectedTransform(StageSpec stage, ExpectedInstance instance)
    {
        if (instance.Mode == TransformMode.WorldIdentity)
        {
            return IdentityTransform(0.0, 0.0, 0.0);
        }

        if (!stage.AngleDegrees.HasValue)
        {
            if (instance.Mode == TransformMode.LegFollower)
            {
                return IdentityTransform(instance.X, HingeCaseY + instance.Y, HingeCaseZ + instance.Z);
            }

            return IdentityTransform(instance.X, instance.Y, instance.Z);
        }

        double angle = stage.AngleDegrees.Value;
        if (instance.Mode == TransformMode.Kickstand)
        {
            return DeployedLegTransform(angle, instance.X < 0.0 ? -1.0 : 1.0);
        }

        if (instance.Mode == TransformMode.LegFollower)
        {
            return DeployedFollowerTransform(angle, instance.X < 0.0 ? -1.0 : 1.0,
                instance.Y, instance.Z);
        }

        Point mapped = CasePointToDesk(instance.X, instance.Y, instance.Z, angle);
        return CaseTransform(mapped, angle);
    }

    private void ValidateLayerGeometry(StageSpec stage, List<ComponentSnapshot> components)
    {
        List<ComponentSnapshot> sides = Components(components, SideFrame);
        List<ComponentSnapshot> legs = Components(components, Kickstand);
        List<ComponentSnapshot> cheeks = Components(components, OuterCheek);
        List<ComponentSnapshot> pivots = Components(components, PivotPin);
        List<ComponentSnapshot> spacers = Components(components, Spacer);
        List<ComponentSnapshot> stops = Components(components, LoadStopPin);
        List<ComponentSnapshot> locks = Components(components, LockPin);
        List<ComponentSnapshot> heels = Components(components, HeelInsert);
        List<ComponentSnapshot> feet = Components(components, FootPad);

        Check(OriginsAtAbsoluteX(sides, InnerFrameX), stage.Label + " 内侧框中心面 x=±272.5", OriginXDescription(sides));
        Check(OriginsAtAbsoluteX(legs, LegPlaneX), stage.Label + " 6 mm 腿中心面 x=±277.4", OriginXDescription(legs));
        Check(OriginsAtAbsoluteX(cheeks, OuterCheekX), stage.Label + " 外颊中心面 x=±282.3", OriginXDescription(cheeks));
        Check(OriginsAtAbsoluteX(pivots, PinPlaneX), stage.Label + " Ø8 主轴中心面 x=±277.4", OriginXDescription(pivots));
        Check(OriginsAtAbsoluteX(spacers, LegPlaneX) && spacers.Count == 8,
            stage.Label + " 8 个 6.8 mm 隔柱中心面 x=±277.4", OriginXDescription(spacers));
        Check(OriginsAtAbsoluteX(stops, PinPlaneX), stage.Label + " Ø8 止挡中心面 x=±277.4", OriginXDescription(stops));
        Check(OriginsAtAbsoluteX(locks, PinPlaneX), stage.Label + " Ø5 锁销中心面 x=±277.4", OriginXDescription(locks));
        Check(OriginsAtAbsoluteX(heels, LegPlaneX), stage.Label + " 脚跟嵌片中心面 x=±277.4", OriginXDescription(heels));
        Check(OriginsAtAbsoluteX(feet, LegPlaneX), stage.Label + " 橡胶脚中心面 x=±277.4", OriginXDescription(feet));
        ValidateOutermostFaces(stage, cheeks, "外颊外表面");
        ValidateOutermostFaces(stage, pivots, "主轴端面");
        ValidateOutermostFaces(stage, stops, "承力止挡端面");
        ValidateOutermostFaces(stage, locks, "锁销端面");

        ComponentSnapshot left = FindBySign(sides, -1);
        ComponentSnapshot right = FindBySign(sides, 1);
        if (left != null && right != null)
        {
            double[] leftBox = ComponentBox(left.Component);
            double[] rightBox = ComponentBox(right.Component);
            if (leftBox == null || rightBox == null)
            {
                Warn(stage.Label + " 内净宽", "SOLIDWORKS 未返回侧框组件包络。");
            }
            else
            {
                double clearWidth = (rightBox[0] - leftBox[3]) * MillimetresPerMetre;
                Check(Almost(clearWidth, 542.0), stage.Label + " 两内侧框净宽 542 mm",
                    "右内表面减左内表面=" + Format(clearWidth) + " mm");
            }
        }
    }

    private void ValidateOutermostFaces(StageSpec stage,
        List<ComponentSnapshot> components, string description)
    {
        bool match = components.Count == 2;
        List<string> actual = new List<string>();
        foreach (ComponentSnapshot component in components)
        {
            double[] box = ComponentBox(component.Component);
            if (box == null)
            {
                match = false;
                actual.Add("<无包络>");
                continue;
            }

            double outer = component.X < 0.0 ? box[0] * MillimetresPerMetre : box[3] * MillimetresPerMetre;
            actual.Add(Format(outer));
            if (!Almost(outer, component.X < 0.0 ? -NominalOuterFaceX : NominalOuterFaceX))
            {
                match = false;
            }
        }

        Check(match, stage.Label + " " + description + " x=±283.8",
            "实际外侧坐标=[" + string.Join(", ", actual.ToArray()) + "] mm");
    }

    private void ValidateCoaxialStacks(StageSpec stage, List<ComponentSnapshot> components)
    {
        double deployment = DeploymentDetent(TargetDeskAngle);
        Point stopCase = LegRelativeToCase(StopTargetRelativeY, StopTargetRelativeZ, deployment);
        Point deployLockCase = LegRelativeToCase(LockHoleRelativeY, LockHoleRelativeZ, deployment);
        foreach (int sign in new int[] { -1, 1 })
        {
            ComponentSnapshot side = FindBySign(Components(components, SideFrame), sign);
            ComponentSnapshot leg = FindBySign(Components(components, Kickstand), sign);
            ComponentSnapshot cheek = FindBySign(Components(components, OuterCheek), sign);
            ComponentSnapshot pivot = FindBySign(Components(components, PivotPin), sign);
            ComponentSnapshot stop = FindBySign(Components(components, LoadStopPin), sign);
            ComponentSnapshot lockPin = FindBySign(Components(components, LockPin), sign);
            if (side == null || leg == null || cheek == null || pivot == null || stop == null || lockPin == null)
            {
                Fail(stage.Label + " " + SideLabel(sign) + "双剪轴系齐全",
                    "内框、腿、外颊、主轴、止挡或锁销实例缺失。");
                continue;
            }

            Point sideAxis = TransformPoint(side.Transform, 0.0, HingeCaseY, HingeCaseZ);
            Point legAxis = TransformPoint(leg.Transform, 0.0, HingeLocalY, HingeLocalZ);
            Point cheekAxis = TransformPoint(cheek.Transform, 0.0, HingeCaseY, HingeCaseZ);
            Point pivotAxis = TransformPoint(pivot.Transform, 0.0, 0.0, 0.0);
            double mainLineError = MaximumAxisLineError(sideAxis, legAxis, cheekAxis, pivotAxis);
            Check(mainLineError <= ContactTolerance && Distance(pivotAxis, legAxis) <= ContactTolerance &&
                  ParallelLocalX(side.Transform, leg.Transform) && ParallelLocalX(leg.Transform, pivot.Transform),
                stage.Label + " " + SideLabel(sign) + "主轴双剪同轴",
                "最大 YZ 偏差=" + Format(mainLineError) + " mm；主轴至腿孔三维误差=" +
                Format(Distance(pivotAxis, legAxis)) + " mm");

            Point sideStop = TransformPoint(side.Transform, 0.0,
                HingeCaseY + stopCase.Y, HingeCaseZ + stopCase.Z);
            Point cheekStop = TransformPoint(cheek.Transform, 0.0,
                HingeCaseY + stopCase.Y, HingeCaseZ + stopCase.Z);
            Point stopAxis = TransformPoint(stop.Transform, 0.0, 0.0, 0.0);
            double stopError = MaximumAxisLineError(sideStop, cheekStop, stopAxis);
            Check(stopError <= ContactTolerance && ParallelLocalX(side.Transform, stop.Transform),
                stage.Label + " " + SideLabel(sign) + "Ø8 硬止挡双剪同轴",
                "最大 YZ 偏差=" + Format(stopError) + " mm");

            Point lockCase = stage.AngleDegrees.HasValue ? deployLockCase :
                new Point { Y = StorageLockCaseY - HingeCaseY, Z = StorageLockCaseZ - HingeCaseZ };
            Point sideLock = TransformPoint(side.Transform, 0.0,
                HingeCaseY + lockCase.Y, HingeCaseZ + lockCase.Z);
            Point cheekLock = TransformPoint(cheek.Transform, 0.0,
                HingeCaseY + lockCase.Y, HingeCaseZ + lockCase.Z);
            Point pinAxis = TransformPoint(lockPin.Transform, 0.0, 0.0, 0.0);
            double lockError = MaximumAxisLineError(sideLock, cheekLock, pinAxis);
            Check(lockError <= ContactTolerance && ParallelLocalX(side.Transform, lockPin.Transform),
                stage.Label + " " + SideLabel(sign) +
                (stage.AngleDegrees.HasValue ? "Ø5 部署锁销同轴" : "Ø5 收纳锁销位同轴"),
                "最大 YZ 偏差=" + Format(lockError) + " mm");

            if (stage.AngleDegrees.HasValue)
            {
                Point legLock = TransformPoint(leg.Transform, 0.0,
                    HingeLocalY + LockHoleRelativeY, HingeLocalZ + LockHoleRelativeZ);
                Check(Distance(legLock, pinAxis) <= ContactTolerance && ParallelLocalX(leg.Transform, lockPin.Transform),
                    stage.Label + " " + SideLabel(sign) + "锁销与腿部署孔同轴",
                    "三维轴心误差=" + Format(Distance(legLock, pinAxis)) + " mm");
            }
        }
    }

    private void ValidatePhysicalEnvelope(StageSpec stage, List<ComponentSnapshot> components)
    {
        double minimumX = double.PositiveInfinity;
        double maximumX = double.NegativeInfinity;
        int used = 0;
        foreach (ComponentSnapshot component in components)
        {
            if (IsReferenceComponent(component.Stem))
            {
                continue;
            }

            double[] box = ComponentBox(component.Component);
            if (box == null)
            {
                Warn(stage.Label + " 物理包络组件", "无法读取 " + component.Stem + " 的变换后包络。");
                continue;
            }

            minimumX = Math.Min(minimumX, box[0] * MillimetresPerMetre);
            maximumX = Math.Max(maximumX, box[3] * MillimetresPerMetre);
            used++;
        }

        if (used == 0)
        {
            Fail(stage.Label + " 物理组件包络", "没有可用组件包络。");
            return;
        }

        double width = maximumX - minimumX;
        Check(Almost(minimumX, -NominalOuterFaceX) && Almost(maximumX, NominalOuterFaceX) &&
              Almost(width, NominalPackageWidth),
            stage.Label + " 产品 CAD 包络总宽 567.6 mm（排除桌面参考体）",
            "x=" + Format(minimumX) + ".." + Format(maximumX) + " mm，总宽=" + Format(width) + " mm");
    }

    private void ValidatePoseGeometry(StageSpec stage, List<ComponentSnapshot> components)
    {
        List<ComponentSnapshot> legs = Components(components, Kickstand);
        List<ComponentSnapshot> feet = Components(components, FootPad);
        List<ComponentSnapshot> heels = Components(components, HeelInsert);
        List<ComponentSnapshot> stops = Components(components, LoadStopPin);
        List<ComponentSnapshot> locks = Components(components, LockPin);
        if (!stage.AngleDegrees.HasValue)
        {
            int number = 0;
            foreach (ComponentSnapshot leg in legs)
            {
                number++;
                int sign = leg.X < 0.0 ? -1 : 1;
                Point hinge = TransformPoint(leg.Transform, 0.0, HingeLocalY, HingeLocalZ);
                Check(Almost(hinge.X, sign * LegPlaneX) && Almost(hinge.Y, HingeCaseY) && Almost(hinge.Z, HingeCaseZ),
                    stage.Label + " 收纳腿铰点 " + number, PointDescription(hinge));
            }

            foreach (ComponentSnapshot pin in locks)
            {
                Point center = TransformPoint(pin.Transform, 0.0, 0.0, 0.0);
                Check(Almost(center.Y, StorageLockCaseY) && Almost(center.Z, StorageLockCaseZ),
                    stage.Label + " 折叠时锁销位于 storage 坐标",
                    "实际 YZ=(" + Format(center.Y) + "," + Format(center.Z) + ") mm；目标 (-145,72) mm");
            }

            return;
        }

        double angle = stage.AngleDegrees.Value;
        ComponentSnapshot back = FindUnique(components, BackPanel);
        if (back != null)
        {
            Point shellLeft = TransformPoint(back.Transform, -274.0, ShellContactY, ShellContactZ);
            Point shellRight = TransformPoint(back.Transform, 274.0, ShellContactY, ShellContactZ);
            double shellError = Math.Max(
                Math.Max(Math.Abs(shellLeft.Y), Math.Abs(shellLeft.Z)),
                Math.Max(Math.Abs(shellRight.Y), Math.Abs(shellRight.Z)));
            Check(shellError <= ContactTolerance, stage.Label + " 壳体后缘桌面基准 Y=0/Z=0",
                "最大 Y/Z 误差=" + Format(shellError) + " mm");
            double measuredAngle = RadiansToDegrees(Math.Atan2(Math.Abs(back.Transform[4]), Math.Abs(back.Transform[5])));
            Check(Math.Abs(measuredAngle - angle) <= 0.001,
                stage.Label + " 模块面与桌面夹角 60°", "实测变换角=" + Format(measuredAngle) + "°");
        }

        double hingeHeight = HingeHeight(angle);
        double support = SupportDistance(angle);
        Check(Math.Abs(hingeHeight - 99.1480577) <= 0.0001,
            stage.Label + " 主轴理论桌面高度", Format(hingeHeight) + " mm；冻结 99.1480577 mm");
        Check(Math.Abs(DeploymentDetent(angle) - 92.4229565) <= 0.0001,
            stage.Label + " 60° 部署相对转角", Format(DeploymentDetent(angle)) + "°；冻结 92.422956°");
        Check(Math.Abs(support - 133.7697655) <= 0.0001,
            stage.Label + " 理论支撑深度", Format(support) + " mm；冻结 133.769766 mm");

        foreach (int sign in new int[] { -1, 1 })
        {
            ComponentSnapshot leg = FindBySign(legs, sign);
            ComponentSnapshot foot = FindBySign(feet, sign);
            ComponentSnapshot heel = FindBySign(heels, sign);
            ComponentSnapshot stop = FindBySign(stops, sign);
            ComponentSnapshot lockPin = FindBySign(locks, sign);
            if (leg == null || foot == null || heel == null || stop == null || lockPin == null)
            {
                Fail(stage.Label + " " + SideLabel(sign) + "部署组件齐全", "腿、脚、脚跟、止挡或锁销实例缺失。");
                continue;
            }

            Point hinge = TransformPoint(leg.Transform, 0.0, HingeLocalY, HingeLocalZ);
            Point expectedHinge = CasePointToDesk(sign * LegPlaneX, HingeCaseY, HingeCaseZ, angle);
            Point footCenter = TransformPoint(foot.Transform, 0.0, 0.0, 0.0);
            Check(Distance(hinge, expectedHinge) <= ContactTolerance,
                stage.Label + " " + SideLabel(sign) + "腿铰点保持箱体轴位",
                "三维误差=" + Format(Distance(hinge, expectedHinge)) + " mm");
            Check(Almost(Distance(hinge, footCenter), LegLength),
                stage.Label + " " + SideLabel(sign) + "主轴至橡胶脚中心 170 mm",
                Format(Distance(hinge, footCenter)) + " mm");
            // Component2.GetBox(false,false) is an approximate tessellated envelope and
            // can extend a few millimetres past the exact analytic cylinder surface.
            // The Ø16 round foot's exact lowest point is its centre Y minus R=8 mm.
            double exactLowestY = footCenter.Y - FootRadius;
            Check(Math.Abs(exactLowestY) <= ContactTolerance,
                stage.Label + " " + SideLabel(sign) + "橡胶脚最低点 Y=0",
                "解析最低 Y=" + Format(exactLowestY) + " mm；GetBox 仅作近似包络，不用于圆柱切点判定");
            Check(Math.Abs(footCenter.Y - FootRadius) <= ContactTolerance && Math.Abs(footCenter.Z - support) <= ContactTolerance,
                stage.Label + " " + SideLabel(sign) + "橡胶脚支撑中心位置",
                "中心 Y=" + Format(footCenter.Y) + " mm，Z=" + Format(footCenter.Z) +
                " mm；目标 (8," + Format(support) + ") mm");

            Point actualStop = TransformPoint(stop.Transform, 0.0, 0.0, 0.0);
            Point stopInLeg = WorldPointToLegRelative(actualStop, hinge, leg.Transform);
            double gap = CombinedStopClearance(stopInLeg.Y, stopInLeg.Z);
            double heelGap = CircleToRectangleClearance(stopInLeg.Y, stopInLeg.Z,
                7.0, 10.0, -36.0, -20.0, StopRadius);
            Check(gap >= -SweepContactTolerance && Math.Abs(heelGap) <= SweepContactTolerance,
                stage.Label + " " + SideLabel(sign) + "硬止挡/脚跟零间隙且无负干涉",
                "止挡腿局部 q=(" + Format(stopInLeg.Y) + "," + Format(stopInLeg.Z) +
                ") mm；联合间隙=" + Format(gap) + " mm；脚跟间隙=" + Format(heelGap) + " mm");

            Point heelCenter = TransformPoint(heel.Transform, 0.0, 0.0, 0.0);
            Point heelInLeg = WorldPointToLegRelative(heelCenter, hinge, leg.Transform);
            Check(Math.Abs(heelInLeg.Y - HeelCenterRelativeY) <= ContactTolerance &&
                  Math.Abs(heelInLeg.Z - HeelCenterRelativeZ) <= ContactTolerance &&
                  ParallelLocalX(heel.Transform, leg.Transform),
                stage.Label + " " + SideLabel(sign) + "脚跟嵌片随腿且位于切座",
                "实际相对中心=(" + Format(heelInLeg.Y) + "," + Format(heelInLeg.Z) +
                ") mm；目标 (8.5,-28) mm");

            Point actualLock = TransformPoint(lockPin.Transform, 0.0, 0.0, 0.0);
            Point legLock = TransformPoint(leg.Transform, 0.0,
                HingeLocalY + LockHoleRelativeY, HingeLocalZ + LockHoleRelativeZ);
            Check(Distance(actualLock, legLock) <= ContactTolerance,
                stage.Label + " " + SideLabel(sign) + "部署锁销与腿孔同轴",
                "三维误差=" + Format(Distance(actualLock, legLock)) + " mm");
        }
    }

    private void ValidateStepImport(StageSpec stage, string stepPath)
    {
        string temporaryDirectory = null;
        ModelDoc2 imported = null;
        try
        {
            temporaryDirectory = Path.Combine(Path.GetTempPath(),
                "Rack4ModulesV06StepImport-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);
            string copy = Path.Combine(temporaryDirectory,
                Path.GetFileNameWithoutExtension(stepPath) + "-validation-copy.STEP");
            File.Copy(stepPath, copy, false);
            object importData = application.GetImportFileData(copy);
            int importErrors = 0;
            imported = application.LoadFile4(copy, "r", importData, ref importErrors) as ModelDoc2;
            Check(imported != null && importErrors == 0,
                stage.Label + " STEP 临时副本可由 SOLIDWORKS 导入",
                "importErrors=" + importErrors + "；源文件=" + stepPath);
            if (imported != null)
            {
                AssemblyDoc importedAssembly = imported as AssemblyDoc;
                PartDoc importedPart = imported as PartDoc;
                if (importedAssembly != null)
                {
                    int count = importedAssembly.GetComponentCount(false);
                    Check(count > 0, stage.Label + " STEP 导入包含装配几何", count + " 个组件");
                }
                else if (importedPart != null)
                {
                    Array bodies = importedPart.GetBodies2((int)swBodyType_e.swSolidBody, false) as Array;
                    Check(bodies != null && bodies.Length > 0, stage.Label + " STEP 导入包含实体几何",
                        bodies == null ? "0 个实体" : bodies.Length + " 个实体");
                }
                else
                {
                    Fail(stage.Label + " STEP 导入文档类型", "既不是零件也不是装配。");
                }
            }
        }
        catch (Exception exception)
        {
            Fail(stage.Label + " STEP 可打开", exception.GetType().Name + ": " + exception.Message);
        }
        finally
        {
            if (imported != null)
            {
                try { application.CloseDoc(imported.GetTitle()); }
                catch (COMException) { }
            }
            DeleteVerifiedTemporaryDirectory(stage, temporaryDirectory);
        }
    }

    private void DetectDiscreteInterference(StageSpec stage, ModelDoc2 model)
    {
        AssemblyDoc assembly = model as AssemblyDoc;
        InterferenceDetectionMgr manager = null;
        int real = 0;
        int powerFailures = 0;
        int intentionalPower = 0;
        int reference = 0;
        int contacts = 0;
        try
        {
            int activationError = 0;
            application.ActivateDoc3(model.GetTitle(), false,
                (int)swRebuildOnActivation_e.swDontRebuildActiveDoc, ref activationError);
            manager = assembly.InterferenceDetectionManager;
            if (manager == null)
            {
                Warn(stage.Label + " 离散真实干涉分类", "SOLIDWORKS 未提供干涉管理器。");
                return;
            }

            manager.TreatCoincidenceAsInterference = false;
            manager.IncludeMultibodyPartInterferences = false;
            manager.MakeInterferingPartsTransparent = false;
            manager.IgnoreHiddenBodies = false;
            int apiTotal = manager.GetInterferenceCount();
            Array raw = manager.GetInterferences() as Array;
            if (raw != null)
            {
                foreach (object value in raw)
                {
                    Interference interference = value as Interference;
                    if (interference == null) continue;
                    double volume = interference.Volume * 1000000000.0;
                    Array participants = interference.Components as Array;
                    List<string> names = new List<string>();
                    bool hasReference = false;
                    bool hasPower = false;
                    bool hasModule = false;
                    bool stopHeelPair = false;
                    if (participants != null)
                    {
                        foreach (object participant in participants)
                        {
                            Component2 component = participant as Component2;
                            if (component == null) continue;
                            string stem = Path.GetFileNameWithoutExtension(component.GetPathName());
                            names.Add(stem);
                            hasReference = hasReference || IsReferenceComponent(stem);
                            hasPower = hasPower || stem.StartsWith("ReservedPower", StringComparison.OrdinalIgnoreCase);
                            hasModule = hasModule || string.Equals(stem, ModuleEnvelope, StringComparison.OrdinalIgnoreCase);
                        }
                        stopHeelPair = ContainsStem(names, LoadStopPin) && ContainsStem(names, HeelInsert);
                    }

                    string detail = (names.Count == 0 ? "<无法解析参与组件>" : string.Join(" <-> ", names.ToArray())) +
                        "；重叠体积=" + Format(volume) + " mm^3";
                    if (Math.Abs(volume) <= NegligibleInterferenceVolume)
                    {
                        contacts++;
                        Note(stage.Label + " 接触/数值容差候选：" + detail);
                    }
                    else if (stopHeelPair)
                    {
                        real++;
                        Fail(stage.Label + " 硬止挡/脚跟出现负间隙", detail);
                    }
                    else if (hasPower && hasModule)
                    {
                        intentionalPower++;
                        Warn(stage.Label + " 已知模块深度/电源包络冲突", detail + "；仅为保留空间包络。");
                    }
                    else if (hasPower)
                    {
                        powerFailures++;
                        Fail(stage.Label + " 电源保留区与真实零件冲突", detail);
                    }
                    else if (hasReference)
                    {
                        reference++;
                        Warn(stage.Label + " 参考体重叠", detail + "；参考体不属于产品真实组件。");
                    }
                    else
                    {
                        real++;
                        Fail(stage.Label + " 真实组件非零体积干涉", detail);
                    }
                }
            }

            int classified = real + powerFailures + intentionalPower + reference + contacts;
            Check(apiTotal == classified, stage.Label + " 离散干涉结果全部分类",
                "API=" + apiTotal + "，真实=" + real + "，电源违规=" + powerFailures +
                "，已知包装=" + intentionalPower + "，参考体=" + reference + "，接触=" + contacts);
            if (real == 0 && powerFailures == 0)
            {
                Pass(stage.Label + " 离散姿态未检出真实组件负体积干涉",
                    "主轴、双剪颊板、6 mm 腿、止挡、锁销、脚跟嵌片和橡胶脚均纳入分类。");
            }
        }
        catch (Exception exception)
        {
            Warn(stage.Label + " 离散姿态干涉检查未完成", exception.GetType().Name + ": " + exception.Message);
        }
        finally
        {
            if (manager != null)
            {
                try { manager.Done(); }
                catch (COMException) { }
            }
        }
    }

    private PartSnapshot GetPart(string stem)
    {
        PartSnapshot cached;
        if (partCache.TryGetValue(stem, out cached)) return cached;
        string path = PartPath(stem);
        if (!File.Exists(path) || IsLockFile(path))
        {
            Fail("原生 V0.6 零件存在：" + stem, path);
            partCache.Add(stem, null);
            return null;
        }

        try
        {
            ModelDoc2 document = OpenNative(path, swDocumentTypes_e.swDocPART);
            PartDoc part = document as PartDoc;
            if (part == null) throw new InvalidDataException("文件未作为 SLDPRT 打开。");
            PartSnapshot snapshot = new PartSnapshot();
            snapshot.Stem = stem;
            snapshot.Document = document;
            snapshot.Box = part.GetPartBox(true) as double[];
            Array rawBodies = part.GetBodies2((int)swBodyType_e.swSolidBody, false) as Array;
            if (rawBodies != null)
            {
                foreach (object value in rawBodies)
                {
                    Body2 body = value as Body2;
                    if (body != null) snapshot.Bodies.Add(body);
                }
            }
            if (snapshot.Box == null || snapshot.Box.Length < 6 || snapshot.Bodies.Count == 0)
                throw new InvalidDataException("零件没有可用实体或包络。");
            string database;
            snapshot.Material = part.GetMaterialPropertyName2(string.Empty, out database);
            if (string.IsNullOrEmpty(snapshot.Material) && document.ConfigurationManager != null &&
                document.ConfigurationManager.ActiveConfiguration != null)
            {
                snapshot.Material = part.GetMaterialPropertyName2(
                    document.ConfigurationManager.ActiveConfiguration.Name, out database);
            }
            snapshot.Cylinders.AddRange(ReadAxisCylinders(snapshot));
            partCache.Add(stem, snapshot);
            Pass("V0.6 零件可只读打开并含实体：" + stem,
                snapshot.Bodies.Count + " 个实体；" + BoxDescription(snapshot.Box));
            return snapshot;
        }
        catch (Exception exception)
        {
            Fail("V0.6 零件可只读打开并含实体：" + stem, exception.Message);
            partCache.Add(stem, null);
            return null;
        }
    }

    private ModelDoc2 OpenNative(string path, swDocumentTypes_e type)
    {
        string expected = Path.GetFullPath(path);
        ModelDoc2 document = application.GetOpenDocumentByName(expected) as ModelDoc2;
        if (document != null)
        {
            if (!ExactPath(document.GetPathName(), expected))
                throw new InvalidOperationException("SOLIDWORKS 返回同名不同路径文档：" + expected);
            return document;
        }
        int errors = 0;
        int openWarnings = 0;
        int options = (int)swOpenDocOptions_e.swOpenDocOptions_Silent |
            (int)swOpenDocOptions_e.swOpenDocOptions_ReadOnly;
        document = application.OpenDoc6(expected, (int)type, options, string.Empty,
            ref errors, ref openWarnings) as ModelDoc2;
        if (document == null || errors != 0 || !ExactPath(document.GetPathName(), expected))
            throw new InvalidOperationException("SOLIDWORKS 只读打开失败；errors=" + errors +
                "，warnings=" + openWarnings + "，path=" + expected);
        ownedDocumentTitles.Add(document.GetTitle());
        int relevantWarnings = openWarnings & ~(int)swFileLoadWarning_e.swFileLoadWarning_AlreadyOpen;
        if (relevantWarnings != 0)
            Warn("SOLIDWORKS 打开警告：" + Path.GetFileName(expected), "warning bitmask=" + relevantWarnings);
        return document;
    }

    private static List<ComponentSnapshot> ReadTopLevelComponents(AssemblyDoc assembly)
    {
        List<ComponentSnapshot> result = new List<ComponentSnapshot>();
        Array raw = assembly.GetComponents(true) as Array;
        if (raw == null) return result;
        foreach (object value in raw)
        {
            Component2 component = value as Component2;
            if (component != null) result.Add(Snapshot(component));
        }
        return result;
    }

    private static ComponentSnapshot Snapshot(Component2 component)
    {
        MathTransform transform = component.Transform2;
        Array values = transform == null ? null : transform.ArrayData as Array;
        if (values == null || values.Length < 12)
            throw new InvalidDataException("组件缺少完整变换：" + component.Name2);
        ComponentSnapshot snapshot = new ComponentSnapshot();
        snapshot.Component = component;
        snapshot.Path = component.GetPathName();
        snapshot.Stem = Path.GetFileNameWithoutExtension(snapshot.Path);
        snapshot.Transform = new double[16];
        for (int index = 0; index < Math.Min(16, values.Length); index++)
            snapshot.Transform[index] = Convert.ToDouble(values.GetValue(index), CultureInfo.InvariantCulture);
        if (values.Length < 13) snapshot.Transform[12] = 1.0;
        snapshot.X = snapshot.Transform[9] * MillimetresPerMetre;
        snapshot.Y = snapshot.Transform[10] * MillimetresPerMetre;
        snapshot.Z = snapshot.Transform[11] * MillimetresPerMetre;
        return snapshot;
    }

    private static Dictionary<string, List<ComponentSnapshot>> GroupActual(List<ComponentSnapshot> components)
    {
        Dictionary<string, List<ComponentSnapshot>> result =
            new Dictionary<string, List<ComponentSnapshot>>(StringComparer.OrdinalIgnoreCase);
        foreach (ComponentSnapshot component in components)
        {
            List<ComponentSnapshot> list;
            if (!result.TryGetValue(component.Stem, out list))
            {
                list = new List<ComponentSnapshot>();
                result.Add(component.Stem, list);
            }
            list.Add(component);
        }
        return result;
    }

    private static Dictionary<string, List<ExpectedInstance>> GroupExpected(List<ExpectedInstance> instances)
    {
        Dictionary<string, List<ExpectedInstance>> result =
            new Dictionary<string, List<ExpectedInstance>>(StringComparer.OrdinalIgnoreCase);
        foreach (ExpectedInstance instance in instances)
        {
            List<ExpectedInstance> list;
            if (!result.TryGetValue(instance.Stem, out list))
            {
                list = new List<ExpectedInstance>();
                result.Add(instance.Stem, list);
            }
            list.Add(instance);
        }
        return result;
    }

    private static List<ComponentSnapshot> Components(List<ComponentSnapshot> all, string stem)
    {
        List<ComponentSnapshot> result = new List<ComponentSnapshot>();
        foreach (ComponentSnapshot component in all)
            if (string.Equals(component.Stem, stem, StringComparison.OrdinalIgnoreCase)) result.Add(component);
        return result;
    }

    private static ComponentSnapshot FindBySign(List<ComponentSnapshot> components, int sign)
    {
        foreach (ComponentSnapshot component in components)
            if (component.X * sign > 0.0) return component;
        return null;
    }

    private static ComponentSnapshot FindUnique(List<ComponentSnapshot> components, string stem)
    {
        ComponentSnapshot found = null;
        foreach (ComponentSnapshot component in components)
        {
            if (!string.Equals(component.Stem, stem, StringComparison.OrdinalIgnoreCase)) continue;
            if (found != null) return null;
            found = component;
        }
        return found;
    }

    private static bool OriginsAtAbsoluteX(List<ComponentSnapshot> components, double expected)
    {
        if (components.Count == 0) return false;
        foreach (ComponentSnapshot component in components)
            if (!Almost(Math.Abs(component.X), expected)) return false;
        return true;
    }

    private static string OriginXDescription(List<ComponentSnapshot> components)
    {
        List<string> values = new List<string>();
        foreach (ComponentSnapshot component in components) values.Add(Format(component.X));
        return "实际 x=[" + string.Join(", ", values.ToArray()) + "] mm";
    }

    private static List<CylindricalFace> ReadAxisCylinders(PartSnapshot part)
    {
        List<CylindricalFace> result = new List<CylindricalFace>();
        foreach (Body2 body in part.Bodies)
        {
            Array faces = body.GetFaces() as Array;
            if (faces == null) continue;
            foreach (object value in faces)
            {
                Face2 face = value as Face2;
                Surface surface = face == null ? null : face.GetSurface() as Surface;
                if (surface == null || !surface.IsCylinder()) continue;
                double[] parameters = surface.CylinderParams as double[];
                if (parameters == null || parameters.Length < 7) continue;
                result.Add(new CylindricalFace
                {
                    X = parameters[0] * MillimetresPerMetre,
                    Y = parameters[1] * MillimetresPerMetre,
                    Z = parameters[2] * MillimetresPerMetre,
                    AxisX = parameters[3], AxisY = parameters[4], AxisZ = parameters[5],
                    Diameter = Math.Abs(parameters[6]) * 2.0 * MillimetresPerMetre
                });
            }
        }
        return result;
    }

    private static bool HasAxisCylinder(PartSnapshot part, int axis,
        double y, double z, double? diameter)
    {
        foreach (CylindricalFace cylinder in part.Cylinders)
        {
            double direction = axis == 0 ? cylinder.AxisX : axis == 1 ? cylinder.AxisY : cylinder.AxisZ;
            if (Math.Abs(direction) >= 0.99 && Almost(cylinder.Y, y) && Almost(cylinder.Z, z) &&
                (!diameter.HasValue || Almost(cylinder.Diameter, diameter.Value)))
                return true;
        }
        return false;
    }

    private static double[] ComponentBox(Component2 component)
    {
        return component.GetBox(false, false) as double[];
    }

    private static bool IsReferenceComponent(string stem)
    {
        return string.Equals(stem, FitGauge, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(stem, ModuleEnvelope, StringComparison.OrdinalIgnoreCase) ||
               stem.StartsWith("ReservedPower", StringComparison.OrdinalIgnoreCase) ||
               stem.StartsWith("DesktopReferenceSurface_", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsStem(List<string> stems, string expected)
    {
        foreach (string stem in stems)
            if (string.Equals(stem, expected, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static bool MaterialIs(PartSnapshot part, string expectedFragment)
    {
        return part != null && !string.IsNullOrEmpty(part.Material) &&
            part.Material.IndexOf(expectedFragment, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string MaterialDescription(PartSnapshot part)
    {
        return part == null || string.IsNullOrEmpty(part.Material) ? "未分配材料" : part.Material;
    }

    private static double[] IdentityTransform(double x, double y, double z)
    {
        return new double[]
        {
            1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0,
            x / MillimetresPerMetre, y / MillimetresPerMetre, z / MillimetresPerMetre,
            1.0, 0.0, 0.0, 0.0
        };
    }

    private static double[] CaseTransform(Point origin, double angleDegrees)
    {
        double angle = DegreesToRadians(angleDegrees);
        double sine = Math.Sin(angle);
        double cosine = Math.Cos(angle);
        return new double[]
        {
            1.0, 0.0, 0.0, 0.0, sine, cosine, 0.0, -cosine, sine,
            origin.X / MillimetresPerMetre, origin.Y / MillimetresPerMetre,
            origin.Z / MillimetresPerMetre, 1.0, 0.0, 0.0, 0.0
        };
    }

    private static double[] DeployedLegTransform(double angleDegrees, double sign)
    {
        double detent = DeploymentDetent(angleDegrees);
        double relative = DegreesToRadians(angleDegrees - detent);
        double sine = Math.Sin(relative);
        double cosine = Math.Cos(relative);
        Point pivot = CasePointToDesk(sign * LegPlaneX, HingeCaseY, HingeCaseZ, angleDegrees);
        double originY = pivot.Y - (HingeLocalY * sine + HingeLocalZ * -cosine);
        double originZ = pivot.Z - (HingeLocalY * cosine + HingeLocalZ * sine);
        return new double[]
        {
            1.0, 0.0, 0.0, 0.0, sine, cosine, 0.0, -cosine, sine,
            pivot.X / MillimetresPerMetre, originY / MillimetresPerMetre,
            originZ / MillimetresPerMetre, 1.0, 0.0, 0.0, 0.0
        };
    }

    private static double[] DeployedFollowerTransform(double angleDegrees, double sign,
        double relativeY, double relativeZ)
    {
        double detent = DeploymentDetent(angleDegrees);
        double worldLegAngle = DegreesToRadians(angleDegrees - detent);
        double sine = Math.Sin(worldLegAngle);
        double cosine = Math.Cos(worldLegAngle);
        Point pivot = CasePointToDesk(sign * LegPlaneX, HingeCaseY, HingeCaseZ, angleDegrees);
        double centerY = pivot.Y + relativeY * sine - relativeZ * cosine;
        double centerZ = pivot.Z + relativeY * cosine + relativeZ * sine;
        return new double[]
        {
            1.0, 0.0, 0.0, 0.0, sine, cosine, 0.0, -cosine, sine,
            pivot.X / MillimetresPerMetre, centerY / MillimetresPerMetre,
            centerZ / MillimetresPerMetre, 1.0, 0.0, 0.0, 0.0
        };
    }

    private static Point CasePointToDesk(double x, double y, double z, double angleDegrees)
    {
        double angle = DegreesToRadians(angleDegrees);
        double sine = Math.Sin(angle);
        double cosine = Math.Cos(angle);
        return new Point
        {
            X = x,
            Y = (y - ShellContactY) * sine + (ShellContactZ - z) * cosine,
            Z = (y - ShellContactY) * cosine + (z - ShellContactZ) * sine
        };
    }

    private static Point TransformPoint(double[] transform, double x, double y, double z)
    {
        return new Point
        {
            X = x * transform[0] + y * transform[3] + z * transform[6] + transform[9] * MillimetresPerMetre,
            Y = x * transform[1] + y * transform[4] + z * transform[7] + transform[10] * MillimetresPerMetre,
            Z = x * transform[2] + y * transform[5] + z * transform[8] + transform[11] * MillimetresPerMetre
        };
    }

    private static Point WorldPointToLegRelative(Point point, Point hinge, double[] legTransform)
    {
        double dy = point.Y - hinge.Y;
        double dz = point.Z - hinge.Z;
        return new Point
        {
            X = point.X - hinge.X,
            Y = dy * legTransform[4] + dz * legTransform[5],
            Z = dy * legTransform[7] + dz * legTransform[8]
        };
    }

    private static Point LegRelativeToCase(double y, double z, double rotationDegrees)
    {
        double angle = DegreesToRadians(rotationDegrees);
        return new Point
        {
            Y = y * Math.Cos(angle) - z * Math.Sin(angle),
            Z = y * Math.Sin(angle) + z * Math.Cos(angle)
        };
    }

    private static Point CaseRelativeToLeg(double y, double z, double rotationDegrees)
    {
        double angle = DegreesToRadians(rotationDegrees);
        return new Point
        {
            Y = y * Math.Cos(angle) + z * Math.Sin(angle),
            Z = -y * Math.Sin(angle) + z * Math.Cos(angle)
        };
    }

    private static double HingeHeight(double angleDegrees)
    {
        double angle = DegreesToRadians(angleDegrees);
        return 81.0 * Math.Sin(angle) + 58.0 * Math.Cos(angle);
    }

    private static double DeploymentDetent(double angleDegrees)
    {
        return angleDegrees + RadiansToDegrees(Math.Asin((HingeHeight(angleDegrees) - FootRadius) / LegLength));
    }

    private static double SupportDistance(double angleDegrees)
    {
        double angle = DegreesToRadians(angleDegrees);
        double pivotZ = 81.0 * Math.Cos(angle) - 58.0 * Math.Sin(angle);
        double verticalLeg = HingeHeight(angleDegrees) - FootRadius;
        return pivotZ + Math.Sqrt(LegLength * LegLength - verticalLeg * verticalLeg);
    }

    private static double EarClearance(double y, double z)
    {
        // The heel seat removes y=[7,8], z=[-36,-20] from the aluminium ear.
        double core = CircleToRectangleClearance(y, z, -8.0, 7.0, -44.0, -12.0, StopRadius);
        double lowerLip = CircleToRectangleClearance(y, z, 7.0, 8.0, -44.0, -36.0, StopRadius);
        double upperLip = CircleToRectangleClearance(y, z, 7.0, 8.0, -20.0, -12.0, StopRadius);
        return Math.Min(core, Math.Min(lowerLip, upperLip));
    }

    private static double CombinedStopClearance(double y, double z)
    {
        double root = Math.Sqrt(y * y + z * z) - RootRadius - StopRadius;
        double heel = CircleToRectangleClearance(y, z, 7.0, 10.0, -36.0, -20.0, StopRadius);
        return Math.Min(root, Math.Min(EarClearance(y, z), heel));
    }

    private static double CircleToRectangleClearance(double y, double z,
        double minimumY, double maximumY, double minimumZ, double maximumZ, double radius)
    {
        double outsideY = Math.Max(Math.Max(minimumY - y, 0.0), y - maximumY);
        double outsideZ = Math.Max(Math.Max(minimumZ - z, 0.0), z - maximumZ);
        double signedPointDistance;
        if (outsideY > 0.0 || outsideZ > 0.0)
        {
            signedPointDistance = Math.Sqrt(outsideY * outsideY + outsideZ * outsideZ);
        }
        else
        {
            signedPointDistance = -Math.Min(
                Math.Min(y - minimumY, maximumY - y),
                Math.Min(z - minimumZ, maximumZ - z));
        }
        return signedPointDistance - radius;
    }

    private static bool TransformMatches(double[] actual, double[] expected)
    {
        if (actual == null || expected == null || actual.Length < 13 || expected.Length < 13) return false;
        for (int index = 0; index < 9; index++)
            if (Math.Abs(actual[index] - expected[index]) > MatrixTolerance) return false;
        for (int index = 9; index < 12; index++)
            if (Math.Abs(actual[index] - expected[index]) * MillimetresPerMetre > PositionTolerance) return false;
        return Math.Abs(actual[12] - expected[12]) <= MatrixTolerance;
    }

    private static double TransformMetric(double[] actual, double[] expected)
    {
        if (actual == null || expected == null || actual.Length < 12 || expected.Length < 12)
            return double.PositiveInfinity;
        double metric = 0.0;
        for (int index = 0; index < 9; index++) metric += Math.Abs(actual[index] - expected[index]) * 1000.0;
        for (int index = 9; index < 12; index++) metric += Math.Abs(actual[index] - expected[index]) * MillimetresPerMetre;
        return metric;
    }

    private static bool ParallelLocalX(double[] first, double[] second)
    {
        double dot = first[0] * second[0] + first[1] * second[1] + first[2] * second[2];
        double firstLength = Math.Sqrt(first[0] * first[0] + first[1] * first[1] + first[2] * first[2]);
        double secondLength = Math.Sqrt(second[0] * second[0] + second[1] * second[1] + second[2] * second[2]);
        return firstLength > 0.0 && secondLength > 0.0 &&
            Math.Abs(dot / (firstLength * secondLength)) >= 0.999999;
    }

    private static double MaximumAxisLineError(params Point[] points)
    {
        double maximum = 0.0;
        for (int first = 0; first < points.Length; first++)
            for (int second = first + 1; second < points.Length; second++)
            {
                double dy = points[first].Y - points[second].Y;
                double dz = points[first].Z - points[second].Z;
                maximum = Math.Max(maximum, Math.Sqrt(dy * dy + dz * dz));
            }
        return maximum;
    }

    private static double Distance(Point first, Point second)
    {
        double dx = first.X - second.X;
        double dy = first.Y - second.Y;
        double dz = first.Z - second.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private void DeleteVerifiedTemporaryDirectory(StageSpec stage, string temporaryDirectory)
    {
        if (string.IsNullOrEmpty(temporaryDirectory) || !Directory.Exists(temporaryDirectory)) return;
        string full = Path.GetFullPath(temporaryDirectory);
        string temporaryRoot = Path.GetFullPath(Path.GetTempPath());
        if (!full.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(full).StartsWith("Rack4ModulesV06StepImport-", StringComparison.Ordinal))
        {
            Warn(stage.Label + " STEP 临时目录清理", "拒绝删除未验证路径：" + full);
            return;
        }
        try { Directory.Delete(full, true); }
        catch (IOException exception) { Warn(stage.Label + " STEP 临时目录清理", exception.Message); }
        catch (UnauthorizedAccessException exception) { Warn(stage.Label + " STEP 临时目录清理", exception.Message); }
    }

    private string PartPath(string stem)
    {
        return Path.Combine(root, "cad", "parts", stem + ".SLDPRT");
    }

    private static bool ExactPath(string actual, string expected)
    {
        return !string.IsNullOrEmpty(actual) && !string.IsNullOrEmpty(expected) &&
            string.Equals(Path.GetFullPath(actual), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLockFile(string path)
    {
        return Path.GetFileName(path).StartsWith("~$", StringComparison.Ordinal);
    }

    private static bool Almost(double actual, double expected)
    {
        return Math.Abs(actual - expected) <= PositionTolerance;
    }

    private static double AxisLength(double[] box, int axis)
    {
        return (box[axis + 3] - box[axis]) * MillimetresPerMetre;
    }

    private static double DegreesToRadians(double degrees) { return degrees * Math.PI / 180.0; }
    private static double RadiansToDegrees(double radians) { return radians * 180.0 / Math.PI; }
    private static string SideLabel(int sign) { return sign < 0 ? "左侧" : "右侧"; }

    private static string ExpectedCoordinates(StageSpec stage, ExpectedInstance instance)
    {
        return instance.Mode + " (" + Format(instance.X) + "," + Format(instance.Y) + "," +
            Format(instance.Z) + ")" + (stage.AngleDegrees.HasValue ? " @" + Format(stage.AngleDegrees.Value) + "°" : "") ;
    }

    private static string Coordinates(ComponentSnapshot component)
    {
        return "(" + Format(component.X) + "," + Format(component.Y) + "," + Format(component.Z) + ") mm";
    }

    private static string PointDescription(Point point)
    {
        return "(" + Format(point.X) + "," + Format(point.Y) + "," + Format(point.Z) + ") mm";
    }

    private static string BoxDescription(double[] box)
    {
        return Format(AxisLength(box, 0)) + " x " + Format(AxisLength(box, 1)) + " x " +
            Format(AxisLength(box, 2)) + " mm";
    }

    private static string Describe(double actual, string expected)
    {
        return "预期 " + expected + "，实际 " + Format(actual) + " mm";
    }

    private static string SweepDescription(SweepMinimum minimum)
    {
        return minimum.Name + " min=" + Format(minimum.Gap) + " mm @ " +
            Format(minimum.AngleDegrees) + "°，q=(" + Format(minimum.Y) + "," + Format(minimum.Z) + ") mm";
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
        if (condition) Pass(title, detail); else Fail(title, detail);
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

    private enum TransformMode { RigidCase, Kickstand, LegFollower, WorldIdentity }

    private sealed class StageSpec
    {
        internal readonly string Label;
        internal readonly string Stem;
        internal readonly int ExpectedCount;
        internal readonly bool IncludesLid;
        internal readonly bool IncludesClearance;
        internal readonly double? AngleDegrees;
        internal StageSpec(string label, string stem, int expectedCount,
            bool includesLid, bool includesClearance, double? angleDegrees)
        {
            Label = label; Stem = stem; ExpectedCount = expectedCount;
            IncludesLid = includesLid; IncludesClearance = includesClearance; AngleDegrees = angleDegrees;
        }
    }

    private sealed class ExpectedInstance
    {
        internal string Stem;
        internal double X;
        internal double Y;
        internal double Z;
        internal TransformMode Mode;
    }

    private sealed class ComponentSnapshot
    {
        internal Component2 Component;
        internal string Path;
        internal string Stem;
        internal double[] Transform;
        internal double X;
        internal double Y;
        internal double Z;
    }

    private sealed class PartSnapshot
    {
        internal string Stem;
        internal string Material;
        internal ModelDoc2 Document;
        internal double[] Box;
        internal readonly List<Body2> Bodies = new List<Body2>();
        internal readonly List<CylindricalFace> Cylinders = new List<CylindricalFace>();
    }

    private sealed class CylindricalFace
    {
        internal double X;
        internal double Y;
        internal double Z;
        internal double AxisX;
        internal double AxisY;
        internal double AxisZ;
        internal double Diameter;
    }

    private sealed class Point
    {
        internal double X;
        internal double Y;
        internal double Z;
    }

    private sealed class SweepMinimum
    {
        internal readonly string Name;
        internal double Gap = double.PositiveInfinity;
        internal double AngleDegrees;
        internal int Index;
        internal double Y;
        internal double Z;
        internal SweepMinimum(string name) { Name = name; }
        internal void Consider(double gap, double angleDegrees, int index, Point point)
        {
            if (gap < Gap)
            {
                Gap = gap; AngleDegrees = angleDegrees; Index = index; Y = point.Y; Z = point.Z;
            }
        }
    }
}
