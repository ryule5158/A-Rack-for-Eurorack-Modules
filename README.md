# Rack4Modules：Rymovia 3×104HP 便携 Eurorack 机箱

当前设计入口是 **V11（V0.11）Mechanical Release Candidate**。它是一个可继续交给工程图、
DFM 和实体样机阶段的完整 CAD 候选版，不是已经量产放行、通过商业认证或完成实体可靠性试验的产品。

第一次打开项目，请从 [START_HERE_V11.md](START_HERE_V11.md) 开始。只想看最终外观时，在
SOLIDWORKS 2025 中打开：

`cad/assemblies/Rack4Modules_ExteriorIdentityShowcase_V11_MechanicalRelease.SLDASM`

这个展示总装把拆下的 Phase Halo 背板展示副本、60°工作姿态机箱和拆下的盖子放在同一视图中。
左侧额外背板及其加强片只是展示副本，不属于产品 BOM，也不能把该总装的质量当作整机质量。

## 设计边界

| 项目 | V11 当前定义 |
| --- | --- |
| 格式 | 3 排 × 3U × 104HP，共 312HP；无 1U |
| 模块网格 | 每排可见宽度 528.32 mm；1HP = 5.08 mm |
| 机身外廓 | 548 × 420 × 110 mm，不含拆装盖与外部凸出件 |
| 模块安装 | 六根 542 mm 自制闭口结构导轨；每根配一条 528.32 mm AISI 304 M3 螺纹条 |
| 桌面姿态 | 机箱模块面相对桌面约 60°，双后腿与双前防倾支撑形成四点支撑 |
| 运输 | 82 mm 深回边盖、四点锁扣、EPDM 预压与独立金属硬止挡 |
| 搬运 | 顶部单提手，两点 M4 安装并通过独立扩散件把载荷送入上边框 |
| VESA | VESA 100 四孔，通过背板、独立加强片和一体式 6061 载荷框传至侧框 |
| 顶部接口顺序 | 95 mm 适配器预留｜3×五针 DIN + USB-C 同排｜中央提手｜2×4 个 6.35 mm TRS |
| 电源 | 只保留空间包络；电源拓扑、PCB、孔位、接地、保险及热设计均未冻结 |

## 当前总装入口

| 用途 | SOLIDWORKS 原生总装 |
| --- | --- |
| 最终展示：背板、60°机箱和拆下盖子同屏 | `cad/assemblies/Rack4Modules_ExteriorIdentityShowcase_V11_MechanicalRelease.SLDASM` |
| 产品展示：60°机箱与拆下盖子 | `cad/assemblies/Rack4Modules_ShowcaseTilt60_LidOff_V11_MechanicalRelease.SLDASM` |
| 正常开箱与模块安装 | `cad/assemblies/Rack4Modules_OpenCase_V11_MechanicalRelease.SLDASM` |
| 合盖运输状态 | `cad/assemblies/Rack4Modules_TransportClosed_V11_MechanicalRelease.SLDASM` |
| 模块、电源母线和电源空间量规 | `cad/assemblies/Rack4Modules_ClearanceCheck_V11_MechanicalRelease.SLDASM` |
| 正式 60°四点桌面支撑状态 | `cad/assemblies/Rack4Modules_DesktopTilt60_V11_MechanicalRelease.SLDASM` |

请保留整个 `Rack4Modules` 文件夹；总装依赖 `cad/parts` 中的原生零件，不能只复制一个
`.SLDASM` 文件。

## V11 机械改进摘要

### 模块导轨

- 六根导轨是本项目的自制恒截面闭口结构，不再把一根过薄轨道或单颗端螺钉当作唯一承力路径。
- 单根结构长度 542 mm、模块可见长度 528.32 mm、截面总高 10 mm、总深 22 mm；前部 12 mm
  实体承载区与后部 10 mm 闭口脊组合，闭口壁厚 1.5 mm。
- 每端使用一个 M3 定位连接和一个 M4 结构连接，两个轴线相距 8 mm；M4 连接进入可更换的
  7075 端部嵌件，M3 进入导轨前部实体区。六轨共 12 个 M3、12 个 M4 端部连接。
- 每根导轨带螺纹条的原生 CAD 质量为 0.249760 kg；含六根导轨、六条螺纹条、12 个端嵌件和
  24 个端部螺钉的完整子系统为 1.556887 kg。它们是材料属性计算值，不是实物称重。

### 折叠支撑

- 后支撑为 262 mm、8 × 26 mm 的 7075-T6 实体臂，采用全尺寸 4 mm 外颊板、双剪切支撑、
  带套筒主轴、独立硬止挡和位置锁定件。锁定件不承担正常工作止挡反力。
- 前防倾连杆为 124 mm、8 × 20 mm 的 17-4PH H900 生产意图截面；CAD 使用 AISI 304 作为
  密度代理。双剪切 U 形支架、保留式 8 mm 主轴/止挡、独立 M6 位置锁和 20 mm EPDM 脚垫
  形成左右两个前支点。
- 前连杆设置真实扫掠止挡槽、局部承力岛和圆角中段筋。生成器对单件 CAD 质量实施
  0.1720–0.1745 kg 门禁，并以 257 个角度采样点检查止挡槽周围的名义材料余量；这些都是
  几何/材料属性门禁，不是疲劳或冲击证明。
- 60°原生空箱状态的 20 N、作用高度 200 mm 且纵向重心 ±30 mm 敏感性筛查得到后倾安全系数
  2.415862、前倾安全系数 2.439637。这个计算不包含真实模块分布、桌面摩擦、制造变形或动态载荷。

### 盖、VESA 与外观

- 盖采用 1.2 mm 5052-H32 连续焊接/折弯生产意图结构，具有 82 mm 深回边、前连杆收纳避让、
  四个 31 × 72 mm 锁扣包络、四组盖侧加强和金属限位的 EPDM 预压。
- VESA 100 载荷路径为：外部 M4 → 1.5 mm 背板 → 0.5 mm 独立加强片 → 局部桥 → 一体式
  6061 纵梁/横梁 → 四个侧框 M4 连接。CAD 几何不等于支架额定载荷或拉拔合格。
- 盖面继续使用 Rymovia Time Grid，背板继续使用用户选定的 A 方案 Phase Halo。量产蚀刻、丝印、
  阳极或粉末涂层必须先做颜色、附着、耐磨及清洁剂兼容性样片。

## 最终 CAD 与 STEP 复核快照

| 状态 | 顶层组件 | 原生 CAD 质量 | 实体干涉 |
| --- | ---: | ---: | ---: |
| OpenCase | 74 | 6.20306772555 kg | 0 |
| TransportClosed | 80 | 7.85012405063 kg | 0 |
| ClearanceCheck | 82 | 21.95348932555 kg | 仅 2 个预期量规交叠 |
| DesktopTilt60 | 74 | 6.20306772555 kg | 0 |
| ShowcaseTilt60_LidOff | 80 | 7.85012405063 kg | 0 |
| ExteriorIdentityShowcase | 82 | 8.80830729677 kg | 0 |

`ClearanceCheck` 的高质量包含模块、电源和母线实体量规；两个预期交叠分别为模块包络与
`210 × 90 × 45 mm` 电源空间的 472500 mm³，以及模块包络与 `500 × 85 × 20 mm` 母线空间的
420900 mm³。它们不是产品零件干涉。`ExteriorIdentityShowcase` 含额外背板展示副本，不能用于
产品称重。

六个对应 STEP 均使用 SOLIDWORKS `LoadFile4` 重新读入，`errors=0`、`warnings=0`，顶层组件数依次为
74 / 80 / 82 / 74 / 80 / 82；干涉结果与原生状态一致。STEP 读回通常没有可直接比较的材料密度，
因此这里不使用 STEP 质量来证明原生材料属性正确。

完整的结果和限制见 [V11 机械验证报告](reports/v11-mechanical-validation.md)，制造交接见
[制造说明](docs/manufacturing.md)。

## 放行边界

V11 当前可以作为以下工作的输入：原生 CAD 评审、STEP 交换验证、工程图/公差设计、供应商 DFM、
首件报价和实体样机计划。它不能单独证明承重、寿命、跌落、振动、摩擦、温升或商业可靠性。

在任何生产放行前，至少仍需完成：实体首件与三排真实模块装配、材料证书、关键尺寸检测、紧固件
扭矩与防松、导轨 M3/M4 拉拔、后腿与前连杆静载/偏载/疲劳、运输冲击/跌落/振动、四锁循环、
提手满载、VESA 拉拔与循环弯曲、脚垫摩擦及最终电源/模块组合的温升测试。

## 历史版本

V11 是独立派生的当前入口；旧版文件保留用于追溯，不应覆盖或删除：

- [V10 外观版零基础说明](START_HERE_V10.md)
- `docs/strength-weight-audit-v07.md`
- `reports/layout-v07-validation.md`
- `START_HERE_V06.md`
- `docs/stability-v06.md`
- `reports/layout-v06-validation.md`

历史报告中的 PASS/FAIL 只对应其版本和覆盖条件，不能替代 V11 当前证据或实体试验。
