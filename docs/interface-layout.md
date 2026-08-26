# Rack4Modules 后置可换接口面板设计说明

## 1. 设计目的与适用范围

本项目采用三排 3U、每排 104HP 的 Eurorack 机箱。后置接口的目标是让常用音频、MIDI 和 USB 连接从机箱背部引出，同时保留 VESA 支架安装能力、机箱结构强度和未来电源方案的调整空间。

本文仅定义机械接口、候选连接器、安装包络、可换面板和制造复核方法。本文不定义电源电压、供电能力、MIDI 转换电路、USB 主机或设备模式、音频平衡驱动电路，也不宣称尚未设计的接口已经具有实际电气功能。

除非另有说明，文中尺寸均为毫米；候选孔位仅用于 CAD 方案和采购前评估，不能直接作为最终加工依据。

## 2. 同类产品调研

| 同类产品 | 已公开的相关配置 | 可借鉴的设计思路 |
| --- | --- | --- |
| Intellijel 7U Performance Case Gen-2 | 8 个 6.35 mm TRS 接口，3 个 5 针 DIN MIDI 接口，USB-C MIDI，摇臂电源开关，5.5 × 2.5 mm DC 插座，100 × 100 mm M4 VESA 安装孔 | 大型便携式机箱采用完整音频、DIN MIDI、USB 和 VESA 组合；音频插座按相邻接口成组使用 |
| Befaco 7U Case | 6 个 6.35 mm TRS 接口，3 个 5 针 DIN MIDI 接口，USB-B Device，2 个 USB-A Host/充电接口，防破坏电源按钮，DC 插座，VESA 100 安装 | 接口后方设置独立 I/O 电路板；USB Host、USB Device 和充电口必须区分功能 |
| Intellijel Palette 104HP | 4 个 6.35 mm TRS 接口，2 个 3.5 mm Type-A MIDI 接口，USB MIDI，电源开关和 DC 插座 | 在空间受限时使用 3.5 mm TRS MIDI；音频插座通过内部排针根据所连接模块决定功能 |
| Make Noise 7U CV Bus Case | 6.35 mm 外部音频输入和线路/耳机输出，凹陷式侧置电源开关与电源入口，可带线盖合的保护盖 | 电源入口应避免运输碰撞；线路输出和耳机输出需要相应电路，不能由插座本身实现 |

参考产品大多包含 1U 区域，但本项目仍保持三排标准 3U，不因参考其后置接口方案而增加 1U 排。

## 3. 接口数量与初始配置

### 3.1 推荐机械容量

| 接口类别 | 机械位置数量 | 初始用途 | 状态说明 |
| --- | ---: | --- | --- |
| 6.35 mm TRS 音频接口 | 8 | 四对可配置音频连接 | 可先装 6 个，剩余 2 个位置使用堵片或空白插板 |
| 5 针 DIN MIDI 接口 | 3 | MIDI IN、MIDI OUT、MIDI THRU | 仅表示接口位置；具体方向、电路和隔离方式后续确定 |
| USB D 型通用位置 | 2 | 1 个 USB-C，1 个可替换 USB-A/USB-B | 可使用标准 D 型直通件，也可先安装堵板 |
| 3.5 mm TRS 接口 | 2 | AUX 1、AUX 2 | 暂不固定为耳机、音频、时钟或 TRS MIDI |
| 独立电源面板 | 1 | 空白电源盖板 | 当前不加工 DC 插座、IEC 入口、开关或保险丝孔 |

6.35 mm 接口统一优先采用 TRS 插座。TRS 插座可以容纳相同规格的 TS 插头，因此无需另外为 TS 和 TRS 设计不同直径的安装孔。每两个相邻接口形成一对，但是否为平衡立体声、非平衡立体声、输入、输出或 Send/Return，取决于后续内部电路或模块连接。

### 3.2 接口命名

在电气方案冻结前，推荐采用下列中性标识：

```text
AUDIO 1  AUDIO 2  AUDIO 3  AUDIO 4
AUDIO 5  AUDIO 6  AUDIO 7  AUDIO 8

MIDI IN  MIDI OUT  MIDI THRU
USB 1    USB 2     AUX 1    AUX 2

POWER OPTION / BLANK COVER
```

MIDI IN、OUT、THRU 为位置规划标签，不表示接口已经完成收发、隔离或转发电路。USB 1、USB 2 在电路未确定前不得标注 HOST、DEVICE、PD、CHARGE 或 AUDIO。

## 4. 后板与可换插板结构

### 4.1 结构分层

后置接口采用以下独立零件：

1. 承力后壳：承担机箱结构、VESA 支架载荷和可换插板安装。
2. 模拟音频插板：安装 8 个 6.35 mm TRS 接口。
3. 数字/MIDI 插板：安装 3 个 DIN、2 个 USB D 型位置和 2 个 3.5 mm 接口。
4. 独立电源盖板：当前为空白件，未来按电源型号更换。
5. 可选后置浅盒：在需要保持完整模块净深时，为连接器尾部和线束提供外部容积。

承力后壳只定义插板窗口、安装螺孔和 VESA 加强结构，不直接固化连接器专用孔型。后续更换接口或电源时，仅需重新制作相应的小插板。

### 4.2 插板参考包络

以下尺寸为布局起点，不是最终加工图：

| 区域 | 建议外形 | 紧固方式 | 说明 |
| --- | --- | --- | --- |
| 模拟音频插板 | 约 160 × 80 | 6 个 M3 螺钉 | 8 个音频孔采用 2 × 4 排列 |
| 数字/MIDI 插板 | 约 160 × 105 | 6 个 M3 螺钉 | DIN、USB 和 AUX 分排布置 |
| 电源空白盖板 | 约 90 × 55 | 至少 4 个 M3 螺钉 | 当前只保留盖板与固定孔 |
| VESA 中央禁布区 | 不小于 140 × 140 | 100 × 100 M4 孔型 | 预留支架压板、垫片、螺钉和结构加强区域 |

所有插板外形、窗口尺寸和螺孔位置应作为 SolidWorks 全局变量或配置参数，而不是写死在单一零件草图中。

### 4.3 后板分区原则

- VESA 安装区位于后板中央，并使用独立加强件或加强筋形成清晰的载荷传递路径。
- 模拟音频插板和数字/MIDI 插板分别布置在 VESA 区域两侧。
- 电源空白盖板布置在靠近未来电源预留空间的一角，并尽量远离模拟音频插板。
- 各插板窗口与 VESA 加强区之间保留完整材料带，不得开贯穿全宽的长条窗口。
- 接口、螺母、尾部线束和插头不得侵入 VESA 支架底板扫掠范围。
- 安装 VESA 支架后，仍需能够插拔音频线、DIN MIDI 线和 USB 线。
- 如果后板为桌面支撑接触面，所有外露插座应采用局部凹陷或受防护边框保护。

## 5. 候选连接器与参考孔位

### 5.1 6.35 mm TRS：轻量螺纹套筒方案

候选型号：Cliff S4 系列 6.35 mm TRS 插座。

- 厂家给出的面板孔径：Ø11.20。
- 推荐相邻孔中心距：25–28。
- 8 个位置建议采用 2 × 4 排列。
- 两排之间建议保留 25–30 的中心距，最终取决于插头外壳和内部插座本体宽度。
- 采购前确认具体 S4 型号的极数、接线形式、是否带切换触点、面板允许厚度和防转要求。
- 该方案比全部使用锁紧式 D 型金属插座更有利于减重和高密度布置。

官方资料：[Cliff S4 系列数据表](https://www.cliffuk.co.uk/products/jacksockets/S4.pdf)。

### 5.2 6.35 mm TRS：重载锁紧备选方案

候选型号：Neutrik NJ3FP6C。

- 连接器正面包络约 31 × 26。
- 采用标准 D 型面板安装方式。
- 中央开孔通常采用约 Ø24。
- 需要额外的两处安装孔和足够的法兰间距。
- 具有插头锁紧功能，但体积和质量高于轻量螺纹套筒方案。

除非用户明确要求舞台运输时带锁紧插座，否则不建议 8 个音频位置全部使用 D 型重载金属连接器。

官方资料：[Neutrik NJ3FP6C](https://www.neutrik.com/en/product/nj3fp6c)。

### 5.3 5 针 DIN：MIDI IN、OUT、THRU

MIDI Association 要求 MIDI 设备使用 180° 排列的 5 针 DIN 面板母座，并区分 MIDI IN、MIDI OUT 及可选 MIDI THRU。

候选型号：REAN NYS325。

- 主体候选开孔：约 Ø15。
- 两固定耳候选中心距：22.2。
- 正面圆形包络：约 Ø19。
- 固定耳孔径按照实际连接器图纸确定；若采用 M3 紧固件，应单独核对孔径、头部和螺母空间。
- 相邻 DIN 位置建议采用 32–35 的中心距，以容纳插头外壳和插拔手指空间。
- 必须确认连接器为 5 针、180°、面板母座，而不是 240° DIN、Mini-DIN 或仅形状相近的其他连接器。
- MIDI THRU 是否真实转发、MIDI IN 是否光电隔离以及屏蔽层如何接地，均属于后续电气设计。

官方资料：

- [MIDI Association：5 Pin DIN Electrical Specs](https://midi.org/5-pin-din-electrical-specs)。
- [MIDI Association：MIDI 1.0 Electrical Specification Update](https://www.midi.org/wp-content/uploads/wpforo/default_attachments/1709416667-ca33-MIDI-10-Electrical-Specification-Update.pdf)。
- [REAN Product Guide](https://www.rean-connectors.com/media/16320/download/REAN%20Product%20Guide%20-%20202305%20-%20EN%20Version.pdf?v=4)。

### 5.4 USB：统一 D 型可换孔位

USB 位置优先采用标准 Neutrik D 型直通件，以便在同一面板位置更换 USB-C、USB-A、USB-B 或封堵件。

候选型号：

- Neutrik NAUSBC-5G：USB-C D 型直通件。
- Neutrik NAUSB3：USB-A/USB-B D 型直通件，内部组件支持翻转改变正面接口类型。

参考安装尺寸：

- 中央开孔标称约 Ø24；部分厂家图纸采用不小于 Ø23.8。
- 两安装孔直径通常不小于 Ø3.2，具体以连接器图纸和 M3 紧固方式为准。
- 两安装孔的 X/Y 坐标差参考 19/24，形成标准 D 型对角安装孔型。
- 单个连接器正面法兰约 26 × 31。
- 相邻 D 型位置建议采用 38–40 的中心距。
- 需要预留内部直通接头、短线插头和弯线空间。

USB-C 外形不自动代表 USB-PD、USB 3.x、USB Audio 或 MIDI；USB-A 外形也不自动代表 Host 或供电。最终能力由所选连接器、内部线束、控制器和供电方案共同决定。

官方资料：

- [Neutrik NAUSBC-5G](https://www.neutrik.com/en/product/nausbc-5g)。
- [Neutrik NAUSB3](https://www.neutrik.com/en/product/nausb3)。
- [Neutrik 官方 D 型孔位示例](https://www.neutrik.com/media/20226/download/2D%20Model%20NE8FDPS-1-TOP.pdf?v=1)。

### 5.5 3.5 mm TRS：可配置 AUX 接口

候选型号：Lumberg 1502 04。

- 接口形式：3.5 mm 三极 TRS、螺纹前装、焊片连接。
- 候选安装孔：约 Ø6.2。
- 相邻位置建议采用 14–16 的中心距。
- 最终复核螺纹长度、面板厚度、垫圈、螺母和焊片朝向。
- 默认标记为 AUX 1/2，不预设耳机驱动、TRS MIDI、时钟或音频方向。
- 如未来定义为 TRS MIDI，应标注采用 Type-A 还是其他接线方式，并通过电气原理图确认。

官方资料：

- [Lumberg 1502 04 数据表](https://downloads.lumberg.com/datenblaetter/en/1502_04.pdf)。
- [MIDI Association：TRS MIDI 接口规范说明](https://midi.org/specification-for-trs-adapters-adopted-and-released)。

### 5.6 电源入口：当前保持空白

电源方案尚未确定，因此当前只设计独立空白电源盖板、安装螺孔、内部电源预留空间和电缆可能的通行路径。

不应预先固化以下任何一种孔型：

- 5.5 × 2.1 DC 圆孔。
- 5.5 × 2.5 DC 圆孔。
- 带锁圆形 DC 接头。
- XLR4 电源接头。
- IEC 市电入口。
- 摇臂开关、圆形防破坏按钮或保险丝座。

即使均采用外置电源适配器，不同产品仍可能选择 2.1 或 2.5 的中心针规格，连接器面板固定方式和开关外形也可能完全不同。若未来引入市电 IEC 入口，必须重新审查保护接地、爬电距离、触电防护和相关认证要求。

官方参考：[Intellijel AC/DC Power Bricks](https://intellijel.com/shop/power/ac-dc-power-bricks/)。

## 6. VESA 安装与结构避让

VESA 安装孔采用 100 × 100、M4 的四孔布局。后板中央建议保留至少 140 × 140 的完整禁布区，用于：

- VESA 支架压板和可能超出 100 × 100 孔距的外形。
- M4 螺钉、平垫圈、防松件及装配工具。
- 内侧加强板、铆螺母或螺柱。
- VESA 支架倾斜、旋转或快拆机构的扫掠范围。
- 插拔线缆时的手指和弯线空间。

禁止在 VESA 四孔围成的区域内布置连接器专用孔、长通风槽、接口面板窗口或未经结构复核的大面积减重孔。VESA 强度不能只依据后板厚度推断，后续应结合加强板、边框连接、整机质量和支架额定载荷进行评估。

Intellijel 和 Befaco 均公开使用 VESA 100 安装；Befaco 手册建议支架承载能力高于 12 kg。该数值仅作为同类产品参考，不直接等同于本项目已经通过承载试验。

## 7. 模块净深与连接器侵占

### 7.1 同类产品的实际局部深度

Intellijel 7U Performance Case Gen-2 官方数据：

- 普通区域最大模块深度：73。
- 电源板上方模块深度：50–62。
- 音频/MIDI 电路板上方模块深度：43–62。

Befaco 7U Case 官方数据：

- 普通区域模块深度：70。
- 电源板区域模块深度：53。

这说明后置接口、电路板和电源都会导致局部模块安装深度小于机箱的最大标称深度，不能仅按后壳外形估算全部区域都能安装深模块。

### 7.2 本项目的两种处理方式

如果模块后方有效净深目标为 85，必须显式选择以下策略之一：

1. **局部限深方案**：允许接口区域局部降至约 50–60，并在 SolidWorks 中建立独立的 `IO_KEEP_OUT` 包络实体和模块深度分区图。
2. **外置浅盒方案**：在 VESA 两侧设置约 25–35 深的后置浅盒，将连接器尾部和线束外移，尽量维持内部 85 净深。

后置浅盒深度不是连接器本体深度的简单复制，还需包含：

- 连接器本体和固定螺母。
- USB 直通件内部插头。
- MIDI DIN 焊片和绝缘套。
- 6.35 mm TRS 焊片、接线或背板 PCB。
- 至少约 20–25 的局部弯线和维修余量。

最终净深应按照最不利的连接器和插头组合进行干涉检查。如果不能维持完整 85 净深，必须在装配图和说明文件中标出局部限制，不得将局部限深描述为全机通用净深。

## 8. 轻量化与制造要求

- 承力后壳推荐从 2.0 厚的 5052/5754 铝板方案开始评估。
- 可换接口插板推荐使用 1.5–2.0 厚铝板。
- 每个插板窗口四周建议保留至少 12–15 的材料带。
- 插板窗口内角应采用圆角，避免直角切口形成应力集中；圆角大小由加工方式和结构复核确定。
- 接口板与承力壳优先使用 M3 螺钉配压铆螺母、铆螺母或独立内框螺母。
- 不应在 1.5–2.0 的薄铝板上依赖反复拆装的直接攻丝。
- 插座周边要复核螺母扳手空间、插头外壳外径和防转措施。
- 模拟音频走线应远离未来 DC/DC 开关电源区域和 USB 高速线束。
- 金属插座外壳是否与机箱连接属于电气接地策略，不能仅因机械安装就假定已经正确接地。
- 通风、提手、脚架、盖锁和 VESA 加强结构不应与接口窗口使用同一关键载荷路径。

## 9. 生产前复核与验收清单

在输出激光切割图、折弯图或 CNC 加工文件之前，依次完成以下复核：

1. 确定每一种连接器的实际品牌、完整型号和安装方向。
2. 从厂家官网下载与采购型号一致的最新 2D、DXF、STEP 或正式数据表。
3. 逐项复核开孔直径、孔位公差、固定孔中心距、固定螺钉规格和面板允许厚度。
4. 逐项复核连接器本体深度、螺母外径、焊片伸出、内部插头和弯线半径。
5. 导入真实连接器三维模型，对 8 个音频口、3 个 DIN、2 个 USB 和 2 个 AUX 分别进行装配干涉检查。
6. 装入真实 VESA 100 支架底板模型，检查压板、螺钉、手指操作和线缆扫掠范围。
7. 对普通区域和接口区域分别测量模块可用深度，核对是否满足 85 净深目标或已经明确标示局部限深。
8. 检查电源盖板当前是否仍为空白，且内部电源预留空间没有被 I/O 线束或 VESA 加强件占用。
9. 使用 1:1 打印模板或少量试制插板进行实际插座、插头、螺钉和线束试装。
10. 完成试装后，才冻结插板专用孔位；未采购或未确定的连接器继续使用空白插板或堵片。

## 10. 功能边界声明

当前设计能够表达的是：

- 8 个 6.35 mm TRS 机械安装位置。
- 3 个 5 针 DIN MIDI 机械安装位置。
- 2 个 USB D 型通用机械安装位置。
- 2 个 3.5 mm TRS 机械安装位置。
- 独立、可更换的空白电源盖板。
- VESA 100 安装和接口结构避让。
- 模块净深、线束空间和可制造性的机械评估。

在电路设计、器件选型和实际测试完成前，不得宣称：

- 已具备平衡线路输入或输出。
- 已具备耳机放大和音量控制。
- 已具备 MIDI 光隔离、收发、Thru 或 USB-MIDI。
- 已具备 USB Host、USB Device、USB Hub、USB Audio、USB-PD 或充电能力。
- 已兼容 Intellijel/Befaco 的内部排针电气定义。
- 已完成电源设计、VESA 承载试验或整机运输可靠性验证。

## 11. 官方参考资料

- [Intellijel 7U Performance Case Gen-2 产品页](https://intellijel.com/shop/cases/7u/7u-performance-case-gen-2/)
- [Intellijel 7U Performance Case Gen-2 官方手册](https://intellijel.com/downloads/manuals/7u-performance-case-gen-2_manual_2026.04.13.pdf)
- [Intellijel 4U Palette Case 官方手册](https://intellijel.com/downloads/manuals/4u-palette-eurorack-case_manual_2020.11.30.pdf)
- [Intellijel Audio Jacks Board v2](https://intellijel.com/shop/cases/7u-audio-jacks-board-v2/)
- [Intellijel AC/DC Power Bricks](https://intellijel.com/shop/power/ac-dc-power-bricks/)
- [Befaco 7U Case 产品页](https://www.befaco.org/befaco_7u_case/)
- [Befaco 7U Case 官方手册](https://www.befaco.org/docs/7U_CASE/7U_Case_User_Manual.pdf)
- [Make Noise 7U Metal CV Bus Case](https://www.makenoisemusic.com/retired/retired-cases/7u-metal-cv-bus-case/)
- [Make Noise 4-Zone CV Bus Case 官方手册](https://www.makenoisemusic.com/wp-content/uploads/2024/07/4zone-cv-bus-case-manual-1.pdf)
- [MIDI Association：5 Pin DIN Electrical Specs](https://midi.org/5-pin-din-electrical-specs)
- [MIDI Association：TRS MIDI 接口规范说明](https://midi.org/specification-for-trs-adapters-adopted-and-released)
- [REAN Product Guide](https://www.rean-connectors.com/media/16320/download/REAN%20Product%20Guide%20-%20202305%20-%20EN%20Version.pdf?v=4)
- [Cliff S4 系列数据表](https://www.cliffuk.co.uk/products/jacksockets/S4.pdf)
- [Lumberg 1502 04 数据表](https://downloads.lumberg.com/datenblaetter/en/1502_04.pdf)
- [Neutrik NJ3FP6C](https://www.neutrik.com/en/product/nj3fp6c)
- [Neutrik NAUSBC-5G](https://www.neutrik.com/en/product/nausbc-5g)
- [Neutrik NAUSB3](https://www.neutrik.com/en/product/nausb3)
- [Neutrik 官方 D 型孔位示例](https://www.neutrik.com/media/20226/download/2D%20Model%20NE8FDPS-1-TOP.pdf?v=1)
