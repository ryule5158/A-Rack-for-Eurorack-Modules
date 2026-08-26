# 从这里开始：Rack4Modules V0.10 Rymovia Phase Halo 背板版

## 1. 只看最终总览

在 SOLIDWORKS 2025 中打开：

`C:\Users\LENOVO\Desktop\Rack4Modules\cad\assemblies\Rack4Modules_ExteriorIdentityShowcase_V10_RymoviaPhaseHalo.SLDASM`

这是本轮推荐的最终展示窗口：左侧是背板外表面展示副本，中间是 60°倾角工作姿态的完整箱体，右侧
是拆下的保护盖。背板副本是同一 V10 零件的展示复制件，只为让用户在一个镜头里确认背面图案；它不应
进入生产 BOM，也不应把质量重复计入产品。

如果需要只看真实产品装配，打开：

`cad\assemblies\Rack4Modules_ShowcaseTilt60_LidOff_V10_RymoviaPhaseHaloRear.SLDASM`

## 2. 本轮新增内容

- `BackPanel_V10_5052_RymoviaPhaseHalo.SLDPRT`：独立新背板，未修改 V07 源文件。
- 五个 `Rack4Modules_*_V10_RymoviaPhaseHaloRear.SLDASM`：由 V09 复制后只替换背板引用。
- `Rack4Modules_ExteriorIdentityShowcase_V10_RymoviaPhaseHalo.SLDASM`：背板 / 箱体 / 盖子三件同屏展示。
- `logo\rymovia-phase-halo-rear-v10-production-lowcontrast.svg`：生产意图矢量图。

## 3. A 方案的设计逻辑

三层断开的圆角轨迹围绕中央 VESA 区域展开，对应三排 3U，并呼应 Rymovia 的克制、精密和安静品牌语言。
它与盖子上的 Time Grid 有意互补：不重复 Logo，不做满版装饰，不将背板画成假电路板。

## 4. 机械留白与制造边界

背板外形仍为 `548 × 420 mm`；主体是 `1.5 mm 5052-H32`，中央 `160 × 160 mm` 叠层区域约 `2.0 mm`。
图案遮罩采用四边 `16 mm`、中央 `180 × 180 mm`、四个后脚 `R12` 三类安全区，保证 VESA 支架接触面、
脚垫和未来边缘紧固区域不被图案占用。背板没有增加新的孔，也没有改变电源或接口预留。

PNG 贴花只是 CAD 预览；SVG 是量产沟通母版。供应商仍须根据阳极/粉末表面选择激光浅蚀或单色丝印，
并制作颜色、附着、耐磨和清洁剂兼容性样片。

## 5. 质量与验证边界

V10 背板质量读回为 `0.959206 kg`；五个产品装配的替换前后质量保持不变：开箱 `5.267439 kg`、合盖运输
`6.737140 kg`。这些是 CAD 材料属性，不是实物称重。构建器还核对了新背板引用为 1、旧 V07 背板为 0、
背板实际变换未改变，以及 166 个受保护 CAD 源文件的元数据/哈希未改变。

这不等于商业可靠性认证。最终供应商锁扣 STEP、折弯/表面处理公差、VESA 拉脱、满载振动/跌落、脚架循环、
运输和量产工艺仍需样机、工装和测试完成后才能确认。

## 6. 版本边界

V10 是独立派生版本；V07/V09 仍保留。当前工作树中若有旧 V07/V04 文件显示未暂存修改，不要用 `git add .`
把它们混入 V10 提交；只暂存本轮新增的 V10 文件、图案、文档和构建器源代码。
