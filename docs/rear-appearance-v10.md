# V0.10 背板外观说明：Phase Halo / Structural Echo

## 设计决定

用户从概念板 A 选择了相位/轨道方向。最终名称为 **Rymovia Phase Halo**，工程副标题为
**Structural Echo**。图形由五层同心圆 / 椭圆轨道组成，再拆分为不对称的实线与点线弧段；
轨迹围绕中央 VESA 净区但不闭合，形成有意的“相位间隙”，外围仅保留四个方向短校准标记。

盖子继续使用 Time Grid。两者是一组互补语言：盖子是离散的时间节奏，背板是连续但断裂的相位回廊。
为了避免廉价贴花感，背板不放大 Logo、不用红色、不做 PCB 节点、发光或高反差满版纹理。

## 几何与遮罩

| 项目 | V0.10 定义 |
| --- | --- |
| 背板外形 | 548 × 420 mm |
| 主体 | 1.5 mm 5052-H32 |
| VESA 局部叠层 | 中央 160 × 160 mm，约 2.0 mm 总厚 |
| VESA 孔 | 4 × Ø4.5 mm，中心 ±50 / ±50 mm |
| 图案边缘 | 四边至少 16 mm 无图案 |
| 中央净区 | 180 × 180 mm 无图案 |
| 后脚净区 | 四处各 R12 无图案 |
| A 方案轨道 | 5 层同心圆 / 椭圆、21 个非对称断续弧段，含实线和点线 |

背板图案只附着在 `Z=110 mm` 的完整外平面。它不切削实体、不进入 VESA 孔、不改动内部桥、立柱、
后横梁或脚架。V10 零件由参数化几何重新生成，避免把当前被 SolidWorks 外观保存触碰过的 V07 文件
当作派生源。

## 交付文件

- `logo/rymovia-phase-halo-rear-v10.svg`：可编辑评审母版，含隐藏的 keep-out 图层。
- `logo/rymovia-phase-halo-rear-v10-production-lowcontrast.svg`：推荐供应商沟通稿。
- `logo/rymovia-phase-halo-rear-v10.png`：SolidWorks 透明预览贴花；为视口可读性适度增强笔画。
- `cad/parts/BackPanel_V10_5052_RymoviaPhaseHalo.SLDPRT`：带一个背面贴花的原生零件。
- `exports/BackPanel_V10_5052_RymoviaPhaseHalo.STEP`：不含贴花实体的几何交换文件。

## 生产边界

SVG 不是已经签核的蚀刻深度或丝印油墨配方。供应商需在实际石墨表面做工艺 coupon，确认颜色、线宽、
附着、耐磨、清洁剂相容性和批次一致性，再冻结刀路/网版。图案变更不会替代 VESA 拉脱、板件屈曲、
运输振动、脚架循环或跌落测试。
