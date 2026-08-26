# 从这里开始：Rack4Modules V0.9 Rymovia 外观 / 四点锁盖版

V0.9 是当前展示与继续设计入口。格式仍为 **3 排 3U、每排 104HP、无 1U**，模块安装
内净宽保持 `542 mm`。本版在 V0.7 结构基线和 V0.8 CMF 外观基础上增加：Rymovia 外盖
图案、真实钻孔深盖、左右各两处过中心锁扣安装位、局部加强板、共享 M3 载荷路径、金属
硬止挡与 EPDM 预压垫。

## 1. 只看最终成品

在 SOLIDWORKS 2025 中打开：

`C:\Users\LENOVO\Desktop\Rack4Modules\cad\assemblies\Rack4Modules_ShowcaseTilt60_LidOff_V09_RymoviaSecureLid.SLDASM`

这是最终展示总装。盖子与机箱分开放置，以便同一画面观察外盖图案和三排 104HP 导轨；
盖子没有盖在模块区上并不是装配错误。当前保存视图为后视正投影，FeatureManager 已隐藏。

## 2. 五个 V0.9 总装

| 文件名 | 用途 |
| --- | --- |
| `Rack4Modules_ShowcaseTilt60_LidOff_V09_RymoviaSecureLid.SLDASM` | 最终外观与三排导轨展示 |
| `Rack4Modules_OpenCase_V09_RymoviaSecureLid.SLDASM` | 开箱结构与模块安装区 |
| `Rack4Modules_TransportClosed_V09_RymoviaSecureLid.SLDASM` | 四点锁盖运输状态 |
| `Rack4Modules_ClearanceCheck_V09_RymoviaSecureLid.SLDASM` | 模块与电源预留包络检查 |
| `Rack4Modules_DesktopTilt60_V09_RymoviaSecureLid.SLDASM` | 60°桌面支撑状态 |

对应 STEP 位于 `exports`。不要只复制单个 `.SLDASM`；总装依赖 `cad\parts` 中的零件。

## 3. 外观图案

外盖采用 **Rymovia Time Grid**：22 段断续线组成三组稀疏节奏带，呼应三排 3U 模块，
但不模拟 PCB、霓虹或游戏设备纹理。图案只位于盖外主平面，不进入锁扣、侧边、接口、
脚架或盖内标识区。

- 评审用矢量母版：`logo\rymovia-timegrid-v09.svg`
- 低反差量产意图稿：`logo\rymovia-timegrid-v09-production-lowcontrast.svg`
- SOLIDWORKS 透明预览：`logo\rymovia-timegrid-v09.png`
- 外盖：Time Grid + 单一 Rymovia mark
- 盖内：完整 Rymovia lockup

SOLIDWORKS PNG 与原 SVG 是便于评审的高可见度展示稿；低反差 SVG 把视觉不透明度降为
`28%`，作为量产意图参考，让图案近看才出现。不建议在黑色盖板上使用亮白满强度丝印。最终蚀刻深度、
阳极/粉末层相容性和耐磨性需由表面处理供应商打样确认。

## 4. 四点锁盖的 CAD 定义

- 左右侧各两个锁点，中心位于 `y=±196 mm`，共四点。
- 盖侧为真实 `Ø3.2 mm` M3 孔阵；锁扣本体、`2 mm` 5052 加强板和 `1.2 mm` 盖回边
  采用同轴贯穿紧固。
- 箱侧紧固件贯穿局部侧框加强区、桥板和扣座；下锁点到脚架外颊板名义间隙 `3 mm`。
- 锁扣桥顶部到最低散热孔名义间隙 `58 mm`。
- EPDM 自由厚度 `2.8 mm`、闭合厚度 `2.0 mm`，名义压缩 `0.8 mm / 28.6%`；金属
  hard-stop 控制最终闭合位置，避免只靠橡胶或锁扣行程限位。

当前 CAD 锁扣模型是按 Southco V7-small 类过中心锁扣包络建立的工程占位，不是最终供应商
STEP。选型冻结后仍需用真实供应商模型复核孔距、开启扫掠、手指空间和外凸包络。

## 5. 质量与验证边界

本次 SOLIDWORKS 生成器完成并输出：11 个 V0.9 零件、5 个原生总装及对应 STEP。

| 状态 | CAD 质量 | 说明 |
| --- | ---: | --- |
| 开箱裸机 | `5.267439 kg` | 不含未来电源和模块 |
| 合盖运输态 | `6.737140 kg` | 含 V0.9 深盖和四点锁盖包 |
| 60°展示态 | `6.737140 kg` | 已移除约 1.983 kg 的桌面参考实体 |

这些是材料库和几何得到的 CAD 质量，不是实物称重。生成器已检查锁点数量、装配引用、
实际变换、名义孔位、脚架/散热区间隙、硬止挡、EPDM 压缩量和质量预算；构建日志仍出现
SOLIDWORKS `NeedsRegen (32)` 与组件预载 `128` 警告，因此不能把“构建完成”描述成实体认证。

投产前至少还要完成：最终供应商锁扣 STEP 复核、折弯/表面处理公差链、M3 承压与防松、
四点预紧一致性、锁盖提拉、开合循环、满载振动/跌落、脚架与线缆操作空间，以及实物称重。

## 6. 版本边界

V0.9 新文件可以继续编辑；V0.7 是结构历史基线。当前工作树中部分旧 V0.7/V0.4 CAD
因 SOLIDWORKS 外观保存被标记为修改，在确认几何与 Git 基线一致前不要把它们混入 V0.9
提交。`tools\build_v09` 是本地编译输出，不进入 Git。
