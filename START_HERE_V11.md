# 从这里开始：Rack4Modules V11 Mechanical Release Candidate

## 1. 只看最终作品

在 SOLIDWORKS 2025 中打开：

`C:\Users\LENOVO\Desktop\Rack4Modules\cad\assemblies\Rack4Modules_ExteriorIdentityShowcase_V11_MechanicalRelease.SLDASM`

这是当前唯一推荐的最终展示入口。视图中：

- 左侧是拆出的 Phase Halo 背板和局部加强片展示副本；
- 中间是模块面相对桌面约 60°、由双后腿与双前支点支撑的完整机箱；
- 右侧是拆下并保持可观察状态的深盖。

左侧两件是展示复制件，不属于产品 BOM。需要判断真实产品质量、配合或干涉时，不要使用这个总装，
改用下表对应的产品状态。

## 2. 六个总装分别看什么

| 任务 | 打开文件 |
| --- | --- |
| 看模块安装、导轨和箱内结构 | `cad\assemblies\Rack4Modules_OpenCase_V11_MechanicalRelease.SLDASM` |
| 看盖是否能完整闭合及四点锁定 | `cad\assemblies\Rack4Modules_TransportClosed_V11_MechanicalRelease.SLDASM` |
| 看 85 mm 模块、电源及母线空间量规 | `cad\assemblies\Rack4Modules_ClearanceCheck_V11_MechanicalRelease.SLDASM` |
| 看真实 60°四点桌面支撑 | `cad\assemblies\Rack4Modules_DesktopTilt60_V11_MechanicalRelease.SLDASM` |
| 看机箱和拆下盖子的产品展示 | `cad\assemblies\Rack4Modules_ShowcaseTilt60_LidOff_V11_MechanicalRelease.SLDASM` |
| 看最终品牌外观同屏展示 | `cad\assemblies\Rack4Modules_ExteriorIdentityShowcase_V11_MechanicalRelease.SLDASM` |

请不要只复制 `.SLDASM`；所有总装都依赖同一工作区中的 `cad\parts`。如果 SOLIDWORKS 提示寻找
引用，先确认整个 `Rack4Modules` 目录仍保持原结构，不要手动把旧版同名零件替换进去。

## 3. 第一次检查的推荐顺序

1. 打开最终展示总装，先确认盖子、背板图案、顶部提手、接口布局和四点支撑外观。
2. 打开 `OpenCase`，确认三排 3U、每排六个导轨界面中的一对上下轨，以及 M3 螺纹条均存在。
3. 打开 `TransportClosed`，确认盖、四组锁扣包络、盖侧加强片、EPDM 预压件和金属硬止挡同时存在。
4. 打开 `DesktopTilt60`，从侧视图观察 262 mm 后腿与 124 mm 前连杆，确认机箱由四个真实脚垫接触桌面。
5. 最后打开 `ClearanceCheck`。这个总装故意加入三个实体空间量规，因此不能把它的质量当作产品质量。

如只做观察，旋转、缩放和切换标准视图即可；不要修改配合、删除组件、另存覆盖或让 SOLIDWORKS
自动更新旧版 V03–V10 原生文件。

## 4. 现在这版解决了什么

### 导轨不再是薄装饰条

每根 V11 导轨是 542 mm 长的 6061-T6 生产意图闭口结构：模块可见长度 528.32 mm，截面高 10 mm、
总深 22 mm，包含 12 mm 前部实体区和 10 mm 闭口脊，闭口壁厚 1.5 mm。每个端部同时由 M3 定位点和
M4 结构点连接至侧框，两轴相距 8 mm；M4 进入可更换 7075 嵌件。连续 M3 螺纹条是独立的
528.32 × 6 × 2 mm AISI 304 零件，每条有 104 个 M3-0.5 位置。

单根“导轨 + 螺纹条”原生 CAD 质量为 0.249760 kg；六轨、六条螺纹条、12 个嵌件和 24 个端部螺钉
合计 1.556887 kg。该数值来自 SOLIDWORKS 材料属性，不是实物称重，也不能替代挤型截面、直线度、
螺纹拉拔和满载振动试验。

### 支撑机构有独立的承力与锁定路径

后腿以 10 mm 保留式主轴和独立 10 mm 实体止挡在双剪切结构中工作；M6 件只做状态锁定。外侧颊板
全范围保持 4 mm，没有为减重开大面积薄弱区。前防倾连杆同样使用双剪切 U 形支架、保留式 8 mm 主轴、
独立 8 mm 扫掠硬止挡和 M6 位置锁；正常工作反力不依赖锁定螺钉。

前连杆生产意图为 17-4PH H900，SOLIDWORKS 当前使用 AISI 304 密度代理。生成器要求单件质量处于
0.1720–0.1745 kg，并对折叠至展开路径使用 257 个角度采样点检查槽边、锁孔、主轴和支架孔之间的名义
余量。这个离散几何门禁不能证明材料疲劳、冲击韧性、磨损寿命或公差极限下仍合格。

### 盖子、提手和 VESA 都有明确载荷路径

盖是 1.2 mm 5052-H32 连续结构生产意图模型，保留 82 mm 深回边、前连杆收纳避让、四点锁扣、局部
加强片、EPDM 预压与金属硬止挡。顶部提手只设置一只，通过 128 mm 安装中心和内部扩散件把载荷传入
上边框。VESA 100 不只依赖背板四孔：背板后还有独立 0.5 mm 加强片及一体式 6061 载荷框连接左右侧框。

这些载荷路径在 CAD 中已经连接，但是否能承受规定载荷仍需要实体提手载荷、VESA 拉拔、锁扣循环和
运输试验。

## 5. 顶部接口与电源边界

顶部从左到右为：95 mm 电源适配器区域、三只五针 DIN 与 USB-C 同排区域、中央提手、2×4 个
6.35 mm TRS 音频接口区域。接口板可以独立拆卸，不需要为了改变接口而重做整箱侧板。

电源尚未冻结。`ClearanceCheck` 中的 `210 × 90 × 45 mm` 电源体积和 `500 × 85 × 20 mm` 母线体积
只是空间量规，不是已设计的 PCB，也没有由此批准市电输入、DC/DC 拓扑、接地、保险丝、散热或安装孔。

## 6. 最终数字验证怎样理解

| 状态 | 顶层组件 | 原生质量 | 干涉结果 |
| --- | ---: | ---: | --- |
| OpenCase | 74 | 6.20306772555 kg | 0 |
| TransportClosed | 80 | 7.85012405063 kg | 0 |
| ClearanceCheck | 82 | 21.95348932555 kg | 2 个预期量规交叠 |
| DesktopTilt60 | 74 | 6.20306772555 kg | 0 |
| ShowcaseTilt60_LidOff | 80 | 7.85012405063 kg | 0 |
| ExteriorIdentityShowcase | 82 | 8.80830729677 kg | 0 |

`ClearanceCheck` 的两个交叠是量规之间的已知关系：模块/电源空间 472500 mm³、模块/母线空间
420900 mm³；没有把它们误报为产品实体互撞。`ExteriorIdentityShowcase` 的质量包含左侧展示副本。

六个 STEP 又使用 `LoadFile4` 独立读回，均为 `errors=0`、`warnings=0`，组件数依次为
74 / 80 / 82 / 74 / 80 / 82，干涉结果与原生文件一致。STEP 读回不保留可直接比较的材料密度，
因此 STEP 质量不作为验证项。

`DesktopTilt60` 的原生空箱筛查以 20 N 水平力、200 mm 作用高度和纵向重心 ±30 mm 敏感性为条件，
得到后倾安全系数 2.415862、前倾安全系数 2.439637。它只说明这个假设条件下的名义静态几何/质量属性
筛查通过，不说明任意模块组合、任意桌面或动态演奏都不会倾覆。

## 7. 什么时候才可以叫“生产放行”

现在不能。至少还缺少：

- 供应商钣金、挤型、机加工、焊接、表面处理与装配 DFM；
- 冻结的 2D 工程图、公差链、检具与首件检测报告；
- 材料证书，以及 CAD 中 AISI 304 / NEOPRENE 密度代理到真实生产材料的核对；
- 导轨 M3/M4 扭矩、防松、拉拔、重复装卸与真实 104HP 模块试装；
- 后腿和前连杆静载、单侧偏载、侧载、疲劳、磨损及误操作试验；
- 盖锁循环、提手满载、VESA 拉拔/循环弯曲、运输冲击/跌落/振动；
- 指定桌面上的脚垫摩擦测试；
- 最终模块、电源、母线和线束组合下的温升、噪声、EMC 与电气安全验证。

完整证据见 `reports\v11-mechanical-validation.md`，制造交接见 `docs\manufacturing.md`。旧版入口
`START_HERE_V10.md` 继续保留用于历史追溯，不再作为当前设计入口。
