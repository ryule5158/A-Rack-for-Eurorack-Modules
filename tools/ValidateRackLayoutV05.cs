using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Read-only native/STEP inspection for the V0.5 double-shear kickstand revision.
// The only intentional project write is reports/layout-v05-validation.md.
internal static class ValidateRackLayoutV05
{
    [STAThread]
    private static int Main(string[] arguments)
    {
        RackLayoutV05Validator validator = null;

        try
        {
            if (arguments.Length != 1)
            {
                throw new ArgumentException("Pass exactly one Rack4Modules project root.");
            }

            validator = new RackLayoutV05Validator(Path.GetFullPath(arguments[0]));
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

            Console.Error.WriteLine("V05_VALIDATION_FAILED=" + exception);
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

internal sealed class RackLayoutV05Validator
{
    private const double MillimetresPerMetre = 1000.0;
    private const double PositionTolerance = 0.08;
    private const double ContactTolerance = 0.10;
    private const double MatrixTolerance = 0.000002;
    private const double NegligibleInterferenceVolume = 0.001;

    private const double ShellContactY = -210.0;
    private const double ShellContactZ = 110.0;
    private const double HingeCaseY = -129.0;
    private const double HingeCaseZ = 52.0;
    private const double HingeLocalY = -75.0;
    private const double HingeLocalZ = 6.0;
    private const double TipLocalY = 75.0;
    private const double TipLocalZ = 6.0;
    private const double LegLength = 150.0;

    private const double InnerFrameX = 272.5;
    private const double LegPlaneX = 276.4;
    private const double OuterCheekX = 280.3;
    private const double PivotX = 276.4;
    private const double IndexPinX = 280.1;
    private const double NominalOuterFaceX = 281.8;
    private const double MaximumCadPackageX = 282.0;

    private const string SideFrame = "SideFrame_V05_Vented_DoubleShearInner";
    private const string Kickstand = "SideKickstand_V05_DoubleShear150mm";
    private const string OuterCheek = "KickstandOuterCheek_V05_3mm";
    private const string PivotPin = "KickstandPivotPin_V05_Flush";
    private const string Spacer = "KickstandSpacer_V05_4p8mm";
    private const string IndexPin = "KickstandIndexPin_V05_SpringEnvelope";

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
    private const string TravelLid = "DeepTravelLid_70mmClearance";
    private const string FitGauge = "FitGauge_104HP_3U";
    private const string ModuleEnvelope = "ModuleDepthEnvelope_85mm_V03";
    private const string PowerBus = "ReservedPowerBus_500x85x20";
    private const string PowerSupply = "ReservedPowerSupply_210x90x45";
    private const string DesktopReference = "DesktopReferenceSurface_V04";

    private static readonly string[] V05PartStems =
    {
        SideFrame,
        Kickstand,
        OuterCheek,
        PivotPin,
        Spacer,
        IndexPin
    };

    private static readonly string[] LegacySideHardwarePrefixes =
    {
        "SideFrame_V04",
        "SideKickstand_V04"
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

    internal RackLayoutV05Validator(string projectRoot)
    {
        if (!Directory.Exists(projectRoot))
        {
            throw new DirectoryNotFoundException("Rack4Modules root does not exist: " + projectRoot);
        }

        root = projectRoot;
        reportPath = Path.Combine(root, "reports", "layout-v05-validation.md");
        report.AppendLine("# Rack4Modules V0.5 双剪折叠脚架验证");
        report.AppendLine();
        report.AppendLine("生成时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        report.AppendLine("项目根目录：`" + root + "`");
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
        ValidateV05PartGeometry();

        StageSpec[] stages =
        {
            new StageSpec("Open", "Rack4Modules_OpenCase_V05", 60, false, false, null),
            new StageSpec("Transport", "Rack4Modules_TransportClosed_V05", 61, true, false, null),
            new StageSpec("Clearance", "Rack4Modules_ClearanceCheck_V05", 68, false, true, null),
            new StageSpec("Tilt60", "Rack4Modules_DesktopTilt60_V05", 61, false, false, 60.0),
            new StageSpec("Tilt75", "Rack4Modules_DesktopTilt75_V05", 61, false, false, 75.0)
        };

        foreach (StageSpec stage in stages)
        {
            ValidateStage(stage);
        }

        Section("未验证边界");
        Warn("连续运动扫掠尚未实现",
            "本程序只检查收纳、60° 与 75° 三个离散 CAD 姿态；未执行从收纳到解锁、旋转、复锁全过程的连续扫掠，因此绝不记为 PASS。");
        Warn("静强度与载荷路径尚未认证",
            "4 mm 6061 腿、双剪颊板、轴、隔柱、内侧框和紧固件仍需按实际满载质量、重心、偏载及侧碰工况进行计算和样机试验。");
        Warn("疲劳、跌落与寿命尚未验证",
            "折叠循环、孔磨损、松动、运输振动、跌落、夹手风险与防滑稳定性均未通过实体样机试验。");
        Warn("供应商锁止机构尚未冻结",
            "V0.5 腿上未切收纳/60°/75°三档真实孔、齿或止挡；弹簧锁止销、轴套、肩轴、防脱件、硬限位、紧固件和公差链没有冻结供应商料号；SpringEnvelope 仅是当前机械包络，不是已确认采购件。");
        Warn("最终运输宽仍需实测",
            "CAD 名义外表面为 x=±281.8 mm、总宽 563.6 mm，并限制当前 CAD 包络不超过 564.0 mm；阳极氧化、板厚及加工装配公差后的实物宽度尚未测量。");

        WriteReport();
        Console.WriteLine("V05_VALIDATION_REPORT=" + reportPath);
        Console.WriteLine("V05_VALIDATION_PASS=" + passes.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("V05_VALIDATION_WARNING=" + warnings.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("V05_VALIDATION_FAIL=" + failures.ToString(CultureInfo.InvariantCulture));
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
                // A temporary import or an assembly dependency may already be closed.
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
                // Restoring the prior active view must never trigger a native save.
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

        foreach (string stem in V05PartStems)
        {
            string path = PartPath(stem);
            Check(File.Exists(path) && !IsLockFile(path), "V0.5 原生零件存在：" + stem, path);
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
                "Start SOLIDWORKS before running validation; this program never starts a new SOLIDWORKS process.");
        }

        ModelDoc2 active = application.ActiveDoc as ModelDoc2;
        originalActiveTitle = active == null ? null : active.GetTitle();
        Section("SOLIDWORKS 会话");
        Pass("连接到已运行的 SOLIDWORKS", application.RevisionNumber());
    }

    private void ValidateV05PartGeometry()
    {
        Section("V0.5 双剪零件几何");
        PartSnapshot side = GetPart(SideFrame);
        PartSnapshot leg = GetPart(Kickstand);
        PartSnapshot cheek = GetPart(OuterCheek);
        PartSnapshot pivot = GetPart(PivotPin);
        PartSnapshot spacer = GetPart(Spacer);
        PartSnapshot indexPin = GetPart(IndexPin);

        if (side != null)
        {
            Check(Almost(AxisLength(side.Box, 0), 3.0), "内侧框厚度", Describe(AxisLength(side.Box, 0), "3.0 mm"));
            Check(Almost(AxisLength(side.Box, 1), 420.0) && Almost(AxisLength(side.Box, 2), 108.0),
                "内侧框主体包络", "实际 " + BoxDescription(side.Box) + "；目标 3 x 420 x 108 mm");
            Check(HasAxisCylinder(side, 0, HingeCaseY, HingeCaseZ),
                "内侧框存在真实横向枢轴圆柱面", "局部轴线 y=-129, z=52 mm");
            Check(MaterialIs(side, "6061"), "内侧框材料标记", MaterialDescription(side));
        }

        if (leg != null)
        {
            Check(Almost(AxisLength(leg.Box, 0), 4.0), "折叠腿厚度", Describe(AxisLength(leg.Box, 0), "4.0 mm 6061"));
            Check(HasAxisCylinder(leg, 0, HingeLocalY, HingeLocalZ),
                "折叠腿存在真实横向枢轴圆柱面", "局部轴线 y=-75, z=6 mm");
            double maximumY = leg.Box[4] * MillimetresPerMetre;
            Check(Almost(maximumY - HingeLocalY, LegLength), "枢轴至脚端几何长度",
                "局部最大 y=" + Format(maximumY) + " mm；与 y=-75 mm 枢轴相距 " +
                Format(maximumY - HingeLocalY) + " mm");
            Check(Almost(AxisLength(leg.Box, 1), 166.0) && Almost(AxisLength(leg.Box, 2), 32.0),
                "折叠腿加强根部包络", "实际 " + BoxDescription(leg.Box) + "；目标 4 x 166 x 32 mm");
            Check(MaterialIs(leg, "6061"), "折叠腿材料标记", MaterialDescription(leg));
        }

        if (cheek != null)
        {
            Check(Almost(AxisLength(cheek.Box, 0), 3.0), "外颊板厚度", Describe(AxisLength(cheek.Box, 0), "3.0 mm"));
            Check(Almost(AxisLength(cheek.Box, 1), 212.0) && Almost(AxisLength(cheek.Box, 2), 48.0),
                "局部外颊条包络", "实际 " + BoxDescription(cheek.Box) + "；目标 3 x 212 x 48 mm");
            Check(HasAxisCylinder(cheek, 0, HingeCaseY, HingeCaseZ),
                "外颊板存在真实横向枢轴圆柱面", "局部轴线 y=-129, z=52 mm");
            Check(MaterialIs(cheek, "6061"), "外颊板材料标记", MaterialDescription(cheek));
        }

        if (pivot != null)
        {
            Check(Almost(AxisLength(pivot.Box, 0), 10.8), "齐平枢轴销轴向长度",
                Describe(AxisLength(pivot.Box, 0), "10.8 mm"));
            Check(Almost(AxisLength(pivot.Box, 1), 8.0) && Almost(AxisLength(pivot.Box, 2), 8.0),
                "枢轴销直径包络", "实际 " + BoxDescription(pivot.Box) + "；目标直径 8 mm");
            Check(HasAxisCylinder(pivot, 0, 0.0, 0.0), "枢轴销存在真实横向圆柱面", "局部轴线 y=0, z=0 mm");
            Check(MaterialIs(pivot, "AISI 304"), "枢轴销材料标记", MaterialDescription(pivot));
        }

        if (spacer != null)
        {
            Check(Almost(AxisLength(spacer.Box, 0), 4.8), "承力隔柱夹层长度",
                Describe(AxisLength(spacer.Box, 0), "4.8 mm"));
            Check(Almost(AxisLength(spacer.Box, 1), 10.0) && Almost(AxisLength(spacer.Box, 2), 10.0),
                "承力隔柱外径包络", "实际 " + BoxDescription(spacer.Box) + "；目标直径 10 mm");
            Check(HasAxisCylinder(spacer, 0, 0.0, 0.0), "隔柱存在真实横向圆柱面", "局部轴线 y=0, z=0 mm");
            Check(MaterialIs(spacer, "AISI 304"), "承力隔柱材料标记", MaterialDescription(spacer));
        }

        if (indexPin != null)
        {
            Check(Almost(AxisLength(indexPin.Box, 0), 3.4) &&
                  Almost(AxisLength(indexPin.Box, 1), 10.0) && Almost(AxisLength(indexPin.Box, 2), 10.0),
                "概念锁止销包络", "实际 " + BoxDescription(indexPin.Box) + "；目标 3.4 x 10 x 10 mm");
            Check(HasAxisCylinder(indexPin, 0, 0.0, 0.0),
                "锁止销机械包络存在真实横向圆柱面", "局部轴线 y=0, z=0 mm；供应商机构仍未冻结");
            Check(MaterialIs(indexPin, "AISI 304"), "概念锁止销材料标记", MaterialDescription(indexPin));
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
        Check(expected.Count == stage.ExpectedCount,
            stage.Label + " 验证公式自身的实例总数",
            "公式=" + expected.Count.ToString(CultureInfo.InvariantCulture) +
            "，冻结值=" + stage.ExpectedCount.ToString(CultureInfo.InvariantCulture));

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

        foreach (string prefix in LegacySideHardwarePrefixes)
        {
            List<string> found = new List<string>();
            foreach (string stem in actualGroups.Keys)
            {
                if (stem.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    found.Add(stem);
                }
            }

            Check(found.Count == 0, stage.Label + " 禁止残留 " + prefix,
                found.Count == 0 ? "未发现" : string.Join(", ", found.ToArray()));
        }

        foreach (KeyValuePair<string, List<ExpectedInstance>> pair in expectedGroups)
        {
            List<ComponentSnapshot> actual;
            actualGroups.TryGetValue(pair.Key, out actual);
            int actualCount = actual == null ? 0 : actual.Count;
            Check(actualCount == pair.Value.Count,
                stage.Label + " 数量：" + pair.Key,
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
        ValidateCoaxialStack(stage, components);
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
        Add(result, PivotPin, -PivotX, HingeCaseY, HingeCaseZ);
        Add(result, PivotPin, PivotX, HingeCaseY, HingeCaseZ);

        double[,] spacerYz =
        {
            { -160.0, 39.0 },
            { -160.0, 68.0 },
            { 27.0, 39.0 },
            { 27.0, 68.0 }
        };
        foreach (double x in new double[] { -LegPlaneX, LegPlaneX })
        {
            for (int index = 0; index < spacerYz.GetLength(0); index++)
            {
                Add(result, Spacer, x, spacerYz[index, 0], spacerYz[index, 1]);
            }
        }

        Add(result, IndexPin, -IndexPinX, -99.0, 52.0);
        Add(result, IndexPin, IndexPinX, -99.0, 52.0);

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
            desktop.X = 0.0;
            desktop.Y = 0.0;
            desktop.Z = 0.0;
            desktop.Mode = TransformMode.WorldIdentity;
            result.Add(desktop);
        }

        return result;
    }

    private static void Add(List<ExpectedInstance> instances, string stem,
        double x, double y, double z)
    {
        ExpectedInstance instance = new ExpectedInstance();
        instance.Stem = stem;
        instance.X = x;
        instance.Y = y;
        instance.Z = z;
        instance.Mode = TransformMode.RigidCase;
        instances.Add(instance);
    }

    private static void AddLeg(List<ExpectedInstance> instances, double x)
    {
        ExpectedInstance instance = new ExpectedInstance();
        instance.Stem = Kickstand;
        instance.X = x;
        instance.Y = -54.0;
        instance.Z = 46.0;
        instance.Mode = TransformMode.Kickstand;
        instances.Add(instance);
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
                differences.Add("预期 " + ExpectedCoordinates(stage, instance) +
                    "，实际 " + Coordinates(best));
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
        if (instance.Mode == TransformMode.WorldIdentity || !stage.AngleDegrees.HasValue)
        {
            return IdentityTransform(instance.X, instance.Y, instance.Z);
        }

        if (instance.Mode == TransformMode.Kickstand)
        {
            return DeployedLegTransform(stage.AngleDegrees.Value, instance.X < 0.0 ? -1.0 : 1.0);
        }

        Point mapped = CasePointToDesk(instance.X, instance.Y, instance.Z, stage.AngleDegrees.Value);
        return CaseTransform(mapped, stage.AngleDegrees.Value);
    }

    private void ValidateLayerGeometry(StageSpec stage, List<ComponentSnapshot> components)
    {
        List<ComponentSnapshot> sides = Components(components, SideFrame);
        List<ComponentSnapshot> legs = Components(components, Kickstand);
        List<ComponentSnapshot> cheeks = Components(components, OuterCheek);
        List<ComponentSnapshot> pivots = Components(components, PivotPin);
        List<ComponentSnapshot> spacers = Components(components, Spacer);
        List<ComponentSnapshot> indexPins = Components(components, IndexPin);

        Check(OriginsAtAbsoluteX(sides, InnerFrameX), stage.Label + " 内侧框中心面 x=±272.5",
            OriginXDescription(sides));
        Check(OriginsAtAbsoluteX(legs, LegPlaneX), stage.Label + " 4 mm 腿固定平面 x=±276.4",
            OriginXDescription(legs));
        Check(OriginsAtAbsoluteX(cheeks, OuterCheekX), stage.Label + " 外颊中心 x=±280.3",
            OriginXDescription(cheeks));
        Check(OriginsAtAbsoluteX(pivots, PivotX), stage.Label + " 枢轴销中心 x=±276.4",
            OriginXDescription(pivots));
        Check(OriginsAtAbsoluteX(spacers, LegPlaneX), stage.Label + " 8 个 4.8 mm 隔柱中心 x=±276.4",
            OriginXDescription(spacers));
        Check(OriginsAtAbsoluteX(indexPins, IndexPinX), stage.Label + " 锁止销包络中心 x=±280.1",
            OriginXDescription(indexPins));
        ValidateOutermostFaces(stage, cheeks, "外颊外表面");
        ValidateOutermostFaces(stage, pivots, "枢轴销端面");

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
        bool allAvailable = components.Count == 2;
        List<string> actual = new List<string>();
        foreach (ComponentSnapshot component in components)
        {
            double[] box = ComponentBox(component.Component);
            if (box == null)
            {
                allAvailable = false;
                actual.Add("<无包络>");
                continue;
            }

            double outer = component.X < 0.0
                ? box[0] * MillimetresPerMetre
                : box[3] * MillimetresPerMetre;
            actual.Add(Format(outer));
            if (!Almost(outer, component.X < 0.0 ? -NominalOuterFaceX : NominalOuterFaceX))
            {
                allAvailable = false;
            }
        }

        Check(allAvailable, stage.Label + " " + description + " x=±281.8",
            "实际外侧坐标=[" + string.Join(", ", actual.ToArray()) + "] mm");
    }

    private void ValidateCoaxialStack(StageSpec stage, List<ComponentSnapshot> components)
    {
        foreach (int sign in new int[] { -1, 1 })
        {
            ComponentSnapshot side = FindBySign(Components(components, SideFrame), sign);
            ComponentSnapshot leg = FindBySign(Components(components, Kickstand), sign);
            ComponentSnapshot cheek = FindBySign(Components(components, OuterCheek), sign);
            ComponentSnapshot pin = FindBySign(Components(components, PivotPin), sign);
            if (side == null || leg == null || cheek == null || pin == null)
            {
                Fail(stage.Label + " " + SideLabel(sign) + "双剪轴系齐全",
                    "内侧框、腿、外颊或枢轴销实例缺失。");
                continue;
            }

            Point sideAxis = TransformPoint(side.Transform, 0.0, HingeCaseY, HingeCaseZ);
            Point legAxis = TransformPoint(leg.Transform, 0.0, HingeLocalY, HingeLocalZ);
            Point cheekAxis = TransformPoint(cheek.Transform, 0.0, HingeCaseY, HingeCaseZ);
            Point pinAxis = TransformPoint(pin.Transform, 0.0, 0.0, 0.0);
            double lineError = MaximumAxisLineError(sideAxis, legAxis, cheekAxis, pinAxis);
            double pinToLegError = Distance(pinAxis, legAxis);
            bool axesParallel = ParallelLocalX(side.Transform, leg.Transform) &&
                ParallelLocalX(side.Transform, cheek.Transform) && ParallelLocalX(side.Transform, pin.Transform);
            Check(lineError <= ContactTolerance && pinToLegError <= ContactTolerance && axesParallel,
                stage.Label + " " + SideLabel(sign) + "内框/腿/外颊/轴同轴",
                "内框与外颊孔只比较轴线 Y/Z，不要求其 X 板面与轴销中心重合；最大 YZ 偏差=" +
                Format(lineError) + " mm；轴销原点至腿铰点三维误差=" +
                Format(pinToLegError) + " mm；局部 X 轴平行=" + axesParallel);
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
              Almost(width, 2.0 * NominalOuterFaceX),
            stage.Label + " CAD 名义总宽 563.6 mm",
            "x=" + Format(minimumX) + ".." + Format(maximumX) + " mm，总宽=" + Format(width) + " mm");
        Check(minimumX >= -MaximumCadPackageX - PositionTolerance &&
              maximumX <= MaximumCadPackageX + PositionTolerance && width <= 564.0 + PositionTolerance,
            stage.Label + " 当前 CAD 包络不超过 564.0 mm",
            "x=" + Format(minimumX) + ".." + Format(maximumX) + " mm，总宽=" + Format(width) + " mm");
    }

    private void ValidatePoseGeometry(StageSpec stage, List<ComponentSnapshot> components)
    {
        List<ComponentSnapshot> legs = Components(components, Kickstand);
        if (!stage.AngleDegrees.HasValue)
        {
            int index = 0;
            foreach (ComponentSnapshot leg in legs)
            {
                index++;
                int sign = leg.X < 0.0 ? -1 : 1;
                Point hinge = TransformPoint(leg.Transform, 0.0, HingeLocalY, HingeLocalZ);
                Point tip = TransformPoint(leg.Transform, 0.0, TipLocalY, TipLocalZ);
                Check(Almost(hinge.X, sign * LegPlaneX) && Almost(hinge.Y, HingeCaseY) &&
                      Almost(hinge.Z, HingeCaseZ),
                    stage.Label + " 收纳腿铰点 " + index,
                    PointDescription(hinge) + "；目标 (±276.4,-129,52) mm");
                Check(Almost(Distance(hinge, tip), LegLength),
                    stage.Label + " 收纳腿铰点至脚端长度 " + index,
                    Format(Distance(hinge, tip)) + " mm；目标 150 mm");
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
            Check(shellError <= ContactTolerance,
                stage.Label + " 后壳下缘桌面接触误差≤0.1 mm",
                "最大 Y/Z 误差=" + Format(shellError) + " mm");

            double measuredAngle = RadiansToDegrees(Math.Atan2(
                Math.Abs(back.Transform[4]), Math.Abs(back.Transform[5])));
            Check(Math.Abs(measuredAngle - angle) <= 0.001,
                stage.Label + " 模块面与桌面夹角 " + Format(angle) + "°",
                "实测变换角=" + Format(measuredAngle) + "°");
        }

        double support = SupportDistance(angle);
        int legNumber = 0;
        foreach (ComponentSnapshot leg in legs)
        {
            legNumber++;
            int sign = leg.X < 0.0 ? -1 : 1;
            Point hinge = TransformPoint(leg.Transform, 0.0, HingeLocalY, HingeLocalZ);
            Point expectedHinge = CasePointToDesk(sign * LegPlaneX, HingeCaseY, HingeCaseZ, angle);
            Point tip = TransformPoint(leg.Transform, 0.0, TipLocalY, TipLocalZ);
            double hingeError = Distance(hinge, expectedHinge);
            double contactError = Math.Max(Math.Abs(tip.Y), Math.Abs(tip.Z - support));
            Check(hingeError <= ContactTolerance,
                stage.Label + " 展开腿铰轴保持在箱体轴位 " + legNumber,
                "三维误差=" + Format(hingeError) + " mm");
            Check(contactError <= ContactTolerance,
                stage.Label + " 展开脚端桌面接触误差≤0.1 mm " + legNumber,
                "Y=" + Format(tip.Y) + " mm，Z=" + Format(tip.Z) +
                " mm，目标后支撑距离=" + Format(support) + " mm");
            Check(Almost(Distance(hinge, tip), LegLength),
                stage.Label + " 展开腿有效长度 " + legNumber,
                Format(Distance(hinge, tip)) + " mm；目标 150 mm");
        }
    }

    private void ValidateStepImport(StageSpec stage, string stepPath)
    {
        string temporaryDirectory = null;
        ModelDoc2 imported = null;
        try
        {
            temporaryDirectory = Path.Combine(Path.GetTempPath(),
                "Rack4ModulesV05StepImport-" + Guid.NewGuid().ToString("N"));
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
                    Check(bodies != null && bodies.Length > 0,
                        stage.Label + " STEP 导入包含实体几何",
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
                try
                {
                    application.CloseDoc(imported.GetTitle());
                }
                catch (COMException)
                {
                    // The imported temporary document may already be closed.
                }
            }

            DeleteVerifiedTemporaryDirectory(stage, temporaryDirectory);
        }
    }

    private void DetectDiscreteInterference(StageSpec stage, ModelDoc2 model)
    {
        AssemblyDoc assembly = model as AssemblyDoc;
        InterferenceDetectionMgr manager = null;
        int realInterferences = 0;
        int powerKeepoutFailures = 0;
        int intentionalPowerPackaging = 0;
        int referenceOverlaps = 0;
        int contacts = 0;

        try
        {
            int activationError = 0;
            application.ActivateDoc3(model.GetTitle(), false,
                (int)swRebuildOnActivation_e.swDontRebuildActiveDoc, ref activationError);
            manager = assembly.InterferenceDetectionManager;
            if (manager == null)
            {
                Warn(stage.Label + " 离散姿态真实组件干涉", "SOLIDWORKS 未提供干涉管理器。");
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
                    if (interference == null)
                    {
                        continue;
                    }

                    double volume = interference.Volume * 1000000000.0;
                    Array participants = interference.Components as Array;
                    List<string> names = new List<string>();
                    bool hasReference = false;
                    bool hasPower = false;
                    bool hasModule = false;
                    if (participants != null)
                    {
                        foreach (object participant in participants)
                        {
                            Component2 component = participant as Component2;
                            if (component == null)
                            {
                                continue;
                            }

                            string stem = Path.GetFileNameWithoutExtension(component.GetPathName());
                            names.Add(stem);
                            hasReference = hasReference || IsReferenceComponent(stem);
                            hasPower = hasPower || stem.StartsWith("ReservedPower", StringComparison.OrdinalIgnoreCase);
                            hasModule = hasModule || string.Equals(stem, ModuleEnvelope, StringComparison.OrdinalIgnoreCase);
                        }
                    }

                    string detail = (names.Count == 0 ? "<无法解析参与组件>" :
                        string.Join(" <-> ", names.ToArray())) +
                        "；重叠体积=" + Format(volume) + " mm^3";

                    if (Math.Abs(volume) <= NegligibleInterferenceVolume)
                    {
                        contacts++;
                        Note(stage.Label + " 接触/数值容差候选：" + detail);
                    }
                    else if (hasPower && hasModule)
                    {
                        intentionalPowerPackaging++;
                        Warn(stage.Label + " 已知模块深度/电源包络冲突",
                            detail + "；中央局部净深 60 mm、母线区 73 mm，不是物理零件碰撞结论。");
                    }
                    else if (hasPower)
                    {
                        powerKeepoutFailures++;
                        Fail(stage.Label + " 电源保留区与真实零件冲突", detail);
                    }
                    else if (hasReference)
                    {
                        referenceOverlaps++;
                        Warn(stage.Label + " 参考体重叠", detail + "；参考体不属于产品真实组件。");
                    }
                    else
                    {
                        realInterferences++;
                        Fail(stage.Label + " 真实组件非零体积干涉", detail);
                    }
                }
            }

            int classified = realInterferences + powerKeepoutFailures + intentionalPowerPackaging +
                referenceOverlaps + contacts;
            Check(apiTotal == classified, stage.Label + " 干涉结果全部分类",
                "API=" + apiTotal + "，真实=" + realInterferences + "，电源违规=" +
                powerKeepoutFailures + "，已知包装=" + intentionalPowerPackaging +
                "，参考体=" + referenceOverlaps + "，接触=" + contacts);
            if (realInterferences == 0 && powerKeepoutFailures == 0)
            {
                Pass(stage.Label + " 离散姿态未检出已建模真实组件体积干涉",
                    "内框、4 mm 腿、外颊、枢轴及 8 个实体隔柱按真实组件检查；概念锁止包络、参考体和已知电源包装冲突另行列出。");
            }
        }
        catch (Exception exception)
        {
            Warn(stage.Label + " 离散姿态干涉检查未完成",
                exception.GetType().Name + ": " + exception.Message);
        }
        finally
        {
            if (manager != null)
            {
                try
                {
                    manager.Done();
                }
                catch (COMException)
                {
                    // SOLIDWORKS may already have closed the interference pane.
                }
            }
        }
    }

    private PartSnapshot GetPart(string stem)
    {
        PartSnapshot cached;
        if (partCache.TryGetValue(stem, out cached))
        {
            return cached;
        }

        string path = PartPath(stem);
        if (!File.Exists(path) || IsLockFile(path))
        {
            Fail("原生 V0.5 零件存在：" + stem, path);
            partCache.Add(stem, null);
            return null;
        }

        try
        {
            ModelDoc2 document = OpenNative(path, swDocumentTypes_e.swDocPART);
            PartDoc part = document as PartDoc;
            if (part == null)
            {
                throw new InvalidDataException("文件未作为 SLDPRT 打开。");
            }

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
                    if (body != null)
                    {
                        snapshot.Bodies.Add(body);
                    }
                }
            }

            if (snapshot.Box == null || snapshot.Box.Length < 6 || snapshot.Bodies.Count == 0)
            {
                throw new InvalidDataException("零件没有可用实体或包络。");
            }

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
            Pass("V0.5 零件可只读打开并含实体：" + stem,
                snapshot.Bodies.Count + " 个实体；" + BoxDescription(snapshot.Box));
            return snapshot;
        }
        catch (Exception exception)
        {
            Fail("V0.5 零件可只读打开并含实体：" + stem, exception.Message);
            partCache.Add(stem, null);
            return null;
        }
    }

    private ModelDoc2 OpenNative(string path, swDocumentTypes_e documentType)
    {
        string expected = Path.GetFullPath(path);
        ModelDoc2 document = application.GetOpenDocumentByName(expected) as ModelDoc2;
        if (document != null)
        {
            if (!ExactPath(document.GetPathName(), expected))
            {
                throw new InvalidOperationException("SOLIDWORKS 返回了同名但路径不同的文档：" + expected);
            }

            return document;
        }

        int errors = 0;
        int openWarnings = 0;
        int options = (int)swOpenDocOptions_e.swOpenDocOptions_Silent |
            (int)swOpenDocOptions_e.swOpenDocOptions_ReadOnly;
        document = application.OpenDoc6(expected, (int)documentType, options, string.Empty,
            ref errors, ref openWarnings) as ModelDoc2;
        if (document == null || errors != 0 || !ExactPath(document.GetPathName(), expected))
        {
            throw new InvalidOperationException("SOLIDWORKS 只读打开失败；errors=" + errors +
                "，warnings=" + openWarnings + "，path=" + expected);
        }

        ownedDocumentTitles.Add(document.GetTitle());
        int relevantWarnings = openWarnings & ~(int)swFileLoadWarning_e.swFileLoadWarning_AlreadyOpen;
        if (relevantWarnings != 0)
        {
            Warn("SOLIDWORKS 打开警告：" + Path.GetFileName(expected),
                "warning bitmask=" + relevantWarnings);
        }

        return document;
    }

    private static List<ComponentSnapshot> ReadTopLevelComponents(AssemblyDoc assembly)
    {
        List<ComponentSnapshot> result = new List<ComponentSnapshot>();
        Array raw = assembly.GetComponents(true) as Array;
        if (raw == null)
        {
            return result;
        }

        foreach (object value in raw)
        {
            Component2 component = value as Component2;
            if (component != null)
            {
                result.Add(Snapshot(component));
            }
        }

        return result;
    }

    private static ComponentSnapshot Snapshot(Component2 component)
    {
        MathTransform transform = component.Transform2;
        Array values = transform == null ? null : transform.ArrayData as Array;
        if (values == null || values.Length < 12)
        {
            throw new InvalidDataException("组件缺少完整变换：" + component.Name2);
        }

        ComponentSnapshot snapshot = new ComponentSnapshot();
        snapshot.Component = component;
        snapshot.Path = component.GetPathName();
        snapshot.Stem = Path.GetFileNameWithoutExtension(snapshot.Path);
        snapshot.Transform = new double[16];
        for (int index = 0; index < Math.Min(16, values.Length); index++)
        {
            snapshot.Transform[index] = Convert.ToDouble(values.GetValue(index), CultureInfo.InvariantCulture);
        }

        if (values.Length < 13)
        {
            snapshot.Transform[12] = 1.0;
        }

        snapshot.X = snapshot.Transform[9] * MillimetresPerMetre;
        snapshot.Y = snapshot.Transform[10] * MillimetresPerMetre;
        snapshot.Z = snapshot.Transform[11] * MillimetresPerMetre;
        return snapshot;
    }

    private static Dictionary<string, List<ComponentSnapshot>> GroupActual(
        List<ComponentSnapshot> components)
    {
        Dictionary<string, List<ComponentSnapshot>> groups =
            new Dictionary<string, List<ComponentSnapshot>>(StringComparer.OrdinalIgnoreCase);
        foreach (ComponentSnapshot component in components)
        {
            List<ComponentSnapshot> list;
            if (!groups.TryGetValue(component.Stem, out list))
            {
                list = new List<ComponentSnapshot>();
                groups.Add(component.Stem, list);
            }

            list.Add(component);
        }

        return groups;
    }

    private static Dictionary<string, List<ExpectedInstance>> GroupExpected(
        List<ExpectedInstance> instances)
    {
        Dictionary<string, List<ExpectedInstance>> groups =
            new Dictionary<string, List<ExpectedInstance>>(StringComparer.OrdinalIgnoreCase);
        foreach (ExpectedInstance instance in instances)
        {
            List<ExpectedInstance> list;
            if (!groups.TryGetValue(instance.Stem, out list))
            {
                list = new List<ExpectedInstance>();
                groups.Add(instance.Stem, list);
            }

            list.Add(instance);
        }

        return groups;
    }

    private static List<ComponentSnapshot> Components(List<ComponentSnapshot> all, string stem)
    {
        List<ComponentSnapshot> result = new List<ComponentSnapshot>();
        foreach (ComponentSnapshot component in all)
        {
            if (string.Equals(component.Stem, stem, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(component);
            }
        }

        return result;
    }

    private static ComponentSnapshot FindBySign(List<ComponentSnapshot> components, int sign)
    {
        foreach (ComponentSnapshot component in components)
        {
            if (component.X * sign > 0.0)
            {
                return component;
            }
        }

        return null;
    }

    private static ComponentSnapshot FindUnique(List<ComponentSnapshot> components, string stem)
    {
        ComponentSnapshot found = null;
        foreach (ComponentSnapshot component in components)
        {
            if (!string.Equals(component.Stem, stem, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (found != null)
            {
                return null;
            }

            found = component;
        }

        return found;
    }

    private static bool OriginsAtAbsoluteX(List<ComponentSnapshot> components, double expected)
    {
        if (components.Count == 0)
        {
            return false;
        }

        foreach (ComponentSnapshot component in components)
        {
            if (!Almost(Math.Abs(component.X), expected))
            {
                return false;
            }
        }

        return true;
    }

    private static string OriginXDescription(List<ComponentSnapshot> components)
    {
        List<string> values = new List<string>();
        foreach (ComponentSnapshot component in components)
        {
            values.Add(Format(component.X));
        }

        return "实际 x=[" + string.Join(", ", values.ToArray()) + "] mm";
    }

    private static List<CylindricalFace> ReadAxisCylinders(PartSnapshot part)
    {
        List<CylindricalFace> result = new List<CylindricalFace>();
        foreach (Body2 body in part.Bodies)
        {
            Array faces = body.GetFaces() as Array;
            if (faces == null)
            {
                continue;
            }

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

                CylindricalFace cylinder = new CylindricalFace();
                cylinder.X = parameters[0] * MillimetresPerMetre;
                cylinder.Y = parameters[1] * MillimetresPerMetre;
                cylinder.Z = parameters[2] * MillimetresPerMetre;
                cylinder.AxisX = parameters[3];
                cylinder.AxisY = parameters[4];
                cylinder.AxisZ = parameters[5];
                cylinder.Diameter = Math.Abs(parameters[6]) * 2.0 * MillimetresPerMetre;
                result.Add(cylinder);
            }
        }

        return result;
    }

    private static bool HasAxisCylinder(PartSnapshot part, int axis, double y, double z)
    {
        foreach (CylindricalFace cylinder in part.Cylinders)
        {
            double direction = axis == 0 ? cylinder.AxisX : axis == 1 ? cylinder.AxisY : cylinder.AxisZ;
            if (Math.Abs(direction) >= 0.99 && Almost(cylinder.Y, y) && Almost(cylinder.Z, z))
            {
                return true;
            }
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
               string.Equals(stem, IndexPin, StringComparison.OrdinalIgnoreCase) ||
               stem.StartsWith("ReservedPower", StringComparison.OrdinalIgnoreCase) ||
               stem.StartsWith("DesktopReferenceSurface_", StringComparison.OrdinalIgnoreCase);
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
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0,
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
            1.0, 0.0, 0.0,
            0.0, sine, cosine,
            0.0, -cosine, sine,
            origin.X / MillimetresPerMetre, origin.Y / MillimetresPerMetre,
            origin.Z / MillimetresPerMetre,
            1.0, 0.0, 0.0, 0.0
        };
    }

    private static double[] DeployedLegTransform(double angleDegrees, double sign)
    {
        double detent = DetentAngle(angleDegrees);
        double relative = DegreesToRadians(angleDegrees - detent);
        double sine = Math.Sin(relative);
        double cosine = Math.Cos(relative);
        Point pivot = CasePointToDesk(sign * LegPlaneX, HingeCaseY, HingeCaseZ, angleDegrees);
        double originY = pivot.Y - (HingeLocalY * sine + HingeLocalZ * -cosine);
        double originZ = pivot.Z - (HingeLocalY * cosine + HingeLocalZ * sine);
        return new double[]
        {
            1.0, 0.0, 0.0,
            0.0, sine, cosine,
            0.0, -cosine, sine,
            pivot.X / MillimetresPerMetre, originY / MillimetresPerMetre,
            originZ / MillimetresPerMetre,
            1.0, 0.0, 0.0, 0.0
        };
    }

    private static Point CasePointToDesk(double x, double y, double z, double angleDegrees)
    {
        double angle = DegreesToRadians(angleDegrees);
        double sine = Math.Sin(angle);
        double cosine = Math.Cos(angle);
        Point point = new Point();
        point.X = x;
        point.Y = (y - ShellContactY) * sine + (ShellContactZ - z) * cosine;
        point.Z = (y - ShellContactY) * cosine + (z - ShellContactZ) * sine;
        return point;
    }

    private static Point TransformPoint(double[] transform, double x, double y, double z)
    {
        Point point = new Point();
        point.X = x * transform[0] + y * transform[3] + z * transform[6] +
            transform[9] * MillimetresPerMetre;
        point.Y = x * transform[1] + y * transform[4] + z * transform[7] +
            transform[10] * MillimetresPerMetre;
        point.Z = x * transform[2] + y * transform[5] + z * transform[8] +
            transform[11] * MillimetresPerMetre;
        return point;
    }

    private static double DetentAngle(double angleDegrees)
    {
        double angle = DegreesToRadians(angleDegrees);
        double pivotHeight = 81.0 * Math.Sin(angle) + 58.0 * Math.Cos(angle);
        return angleDegrees + RadiansToDegrees(Math.Asin(pivotHeight / LegLength));
    }

    private static double SupportDistance(double angleDegrees)
    {
        double angle = DegreesToRadians(angleDegrees);
        double pivotHeight = 81.0 * Math.Sin(angle) + 58.0 * Math.Cos(angle);
        return 81.0 * Math.Cos(angle) - 58.0 * Math.Sin(angle) +
            Math.Sqrt(LegLength * LegLength - pivotHeight * pivotHeight);
    }

    private static bool TransformMatches(double[] actual, double[] expected)
    {
        if (actual == null || expected == null || actual.Length < 13 || expected.Length < 13)
        {
            return false;
        }

        for (int index = 0; index < 9; index++)
        {
            if (Math.Abs(actual[index] - expected[index]) > MatrixTolerance)
            {
                return false;
            }
        }

        for (int index = 9; index < 12; index++)
        {
            if (Math.Abs(actual[index] - expected[index]) * MillimetresPerMetre > PositionTolerance)
            {
                return false;
            }
        }

        return Math.Abs(actual[12] - expected[12]) <= MatrixTolerance;
    }

    private static double TransformMetric(double[] actual, double[] expected)
    {
        if (actual == null || expected == null || actual.Length < 12 || expected.Length < 12)
        {
            return double.PositiveInfinity;
        }

        double metric = 0.0;
        for (int index = 0; index < 9; index++)
        {
            metric += Math.Abs(actual[index] - expected[index]) * 1000.0;
        }

        for (int index = 9; index < 12; index++)
        {
            metric += Math.Abs(actual[index] - expected[index]) * MillimetresPerMetre;
        }

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
        {
            for (int second = first + 1; second < points.Length; second++)
            {
                double dy = points[first].Y - points[second].Y;
                double dz = points[first].Z - points[second].Z;
                maximum = Math.Max(maximum, Math.Sqrt(dy * dy + dz * dz));
            }
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
        if (string.IsNullOrEmpty(temporaryDirectory) || !Directory.Exists(temporaryDirectory))
        {
            return;
        }

        string full = Path.GetFullPath(temporaryDirectory);
        string temporaryRoot = Path.GetFullPath(Path.GetTempPath());
        if (!full.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(full).StartsWith("Rack4ModulesV05StepImport-", StringComparison.Ordinal))
        {
            Warn(stage.Label + " STEP 临时目录清理", "拒绝删除未验证路径：" + full);
            return;
        }

        try
        {
            Directory.Delete(full, true);
        }
        catch (IOException exception)
        {
            Warn(stage.Label + " STEP 临时目录清理", exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            Warn(stage.Label + " STEP 临时目录清理", exception.Message);
        }
    }

    private string PartPath(string stem)
    {
        return Path.Combine(root, "cad", "parts", stem + ".SLDPRT");
    }

    private static bool ExactPath(string actual, string expected)
    {
        if (string.IsNullOrEmpty(actual) || string.IsNullOrEmpty(expected))
        {
            return false;
        }

        return string.Equals(Path.GetFullPath(actual), Path.GetFullPath(expected),
            StringComparison.OrdinalIgnoreCase);
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

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static double RadiansToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }

    private static string SideLabel(int sign)
    {
        return sign < 0 ? "左侧" : "右侧";
    }

    private static string ExpectedCoordinates(StageSpec stage, ExpectedInstance instance)
    {
        if (!stage.AngleDegrees.HasValue)
        {
            return "(" + Format(instance.X) + "," + Format(instance.Y) + "," + Format(instance.Z) + ") mm";
        }

        return instance.Mode + " case(" + Format(instance.X) + "," + Format(instance.Y) + "," +
            Format(instance.Z) + ") mm";
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
        if (condition)
        {
            Pass(title, detail);
        }
        else
        {
            Fail(title, detail);
        }
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

    private enum TransformMode
    {
        RigidCase,
        Kickstand,
        WorldIdentity
    }

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
            Label = label;
            Stem = stem;
            ExpectedCount = expectedCount;
            IncludesLid = includesLid;
            IncludesClearance = includesClearance;
            AngleDegrees = angleDegrees;
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
}
