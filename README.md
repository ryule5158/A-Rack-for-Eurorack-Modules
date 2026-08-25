# Rack4Modules

面向现场演出、录音和携带的 **9U × 104HP Eurorack 模块合成器箱体**。

## 固定需求

- 三排标准 3U，每排 104HP，共 312HP，不设置 1U。
- 6 根 104HP 前导轨，连续 M3 模块安装螺纹条。
- 可拆卸深盖、四点盖锁、折平提手、双档折叠脚架。
- 中央 VESA 100 × 100 mm、M4 加强安装位。
- 左右可换后置音频、MIDI、USB 接口面板。
- 独立空白电源入口板和电源/母线禁入体积，不冻结电源电路或 PCB 孔位。
- 轻量化铝板壳体、局部加强、被动通风，无风扇。

## 初版设计参数

| 项目 | 目标 |
| --- | --- |
| 机身外廓 | 548 × 420 × 110 mm |
| 深盖净空 | 70 mm |
| 闭合参考深度 | 约 182 mm |
| 常规模块有效深度 | ≥ 85 mm |
| 局部 I/O / 电源区域 | ≥ 60 mm，必须标识禁入包络 |
| 主壳 / 侧板 / 深盖 | 2.0 / 3.0 / 1.5 mm 铝合金 |
| VESA 设计工作载荷 | 15 kg；静载验证目标 30 kg |

参数源见 [design/parameters.json](design/parameters.json)。

## 目录

- `design/parameters.json`：参数化尺寸、孔位和预留空间。
- `docs/requirements.md`：机械边界、接口边界和验收条件。
- `docs/reference-products.md`：官方同类产品与取舍依据。
- `tools/`：SolidWorks 2025 自动建模源码。
- `cad/parts/`：SolidWorks 零件。
- `cad/assemblies/`：SolidWorks 装配体。
- `exports/`：STEP 等交换文件。
- `reports/`：建模与几何校核输出。

## 当前边界

后置 TRS、DIN 和 USB 仅表示机械接口预留；它们不自动代表平衡音频、耳机放大、MIDI、USB Host/PD 或充电功能。具体导轨、锁扣、插座、支架、脚架、电源和紧固件料号确定后，仍需按原厂图纸复核孔位、净空、承重及电气安全。
