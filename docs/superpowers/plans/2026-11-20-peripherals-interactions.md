# 原版外围系统与交互修正 Implementation Plan

> 用户最新约束：仅以1920×1080横屏为开发和验收目标，不再安排其它分辨率适配。此前横竖屏证据作为历史保留。原UI内部坐标比例保留，实际目标视口固定1920×1080。

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 补齐四项进度、教练/签到闭环，恢复真实可达的擂台/器械交互，并给出全部实际模块的对照审计。

**Architecture:** 规则/存档与预摆视图分开，沿用 BeachGameFlow/BeachHud/BeachStage、GameFrame 存储和 vivo SDK。原包数据显式归档，Editor 工具一次性修复场景；运行只绑定/切换已有对象。

**Tech Stack:** Unity 2021.3.14f1c1，C#，Built-in Linear，Unity MCP，UnityPy，Il2CppDumper 6.7.46，WABT 1.0.42。

**Spec:** `docs/superpowers/specs/2026-11-20-peripherals-interactions-design.md`

## Global Constraints

- 复用原包素材；场景/UI/特效预摆且可手调。
- 不清空、提交、推送或覆盖用户工作区；不代用户打包。
- 不凭遗留类型名称添加原包未启用系统，不猜经济/签到规则。
- 所有可达性测试从实际出生点正常移动；规则测试不能代替路径和画面验证。

## Review Focus

- 保留已有 v1 用户档与新增状态，购买/领奖失败不得部分扣款或发奖。
- 日期跨天、跳日和重启保持原签到语义，同日/重复点击只能领一次。
- 原 Collider 大小/中心/trigger/layer 逐项恢复，移动不被装饰、NPC或自身体积误挡。
- 关闭/切换外围菜单时停止移动/训练并释放触点，避免后台继续训练或误入擂台。
- 教练装备后的属性显示、实际训练增益、等级/体型必须使用同一状态。

## Task 1：来源审计和失败基线

**Files:** `docs/beach-boxing/evidence/*-audit-2026-11-20.json`，新 PlayMode 路径回归。

- [x] 对原场景9类主要组件、按钮回调、页面和序列化数据建立清单。
- [x] 记录四条进度/教练/签到字段，补方法/WASM 依据和未知项。
- [x] 录制正常移动失败，记录首个阻挡 collider、触发器/提示状态；保存用户档备份。

## Task 2：碰撞还原与原交互

**Files:** 新 `Editor/BeachColliderRestore.cs`、原碰撞配置；修改 `BeachMotor.cs`、`BeachStage.cs`、`BeachGameFlow.cs`；新增碰撞与正常路径测试。

- [x] 先写原 BoxCollider 尺寸/中心和出生到入口可达性失败回归并运行 RED。
- [x] 从原层级映射全部相关 Box/Sphere/Capsule 字段，恢复 layer/trigger/enable；不放大触发范围掩盖错误几何。
- [x] 根据原触发器自动开擂台选人；器械通过原脚部查询与真实交互体积选取，保持 E/按钮/手机路径。
- [x] 预摆原 Interact/键位/手机按钮和激活光圈，核对机器进入、退出、相机与动作。
- [x] 运行正常输入路径、四项器械和旧拳赛回归至 GREEN。

## Task 3：原属性成长规则与 HUD

**Files:** 新 `Runtime/Progression/BeachProgressionRules.cs`、测试；修改 `ProgressStore.cs`、`BeachHud.cs`、`BeachGameFlow.cs`、`BeachStage.cs`；Editor 迁移和原配置。

- [x] 先测分段进度/满条、k格式、升级条件、保留属性、不同等级奖励和教练加成。
- [x] 接入原等级阈值/称号/体型映射，修复固定等级/固定姓名显示和永久禁用肌肉模型。
- [x] 预摆四 FillBar 与满格覆盖、原升级提示/页面，绑定数值和状态。
- [x] 新旧档、阈值两侧、高属性、升级后重新分段及横竖屏验证。

## Task 4：教练与签到规则及持久化

**Files:** 新 `Runtime/Peripheral/BeachPeripheralRules.cs`、EditMode 测试；修改 `ProgressStore.cs`。

- [x] 先测星星购买、金币升级、拥有后装备、实际增益、非法请求和余额保护。
- [x] 先测本地日期初始化/同日/次日/跳日、六档奖励、重复领取与重启。
- [x] 依据原保存键语义扩展现有 JSON；旧档缺字段默认补齐并保留原值。
- [x] 实现无 UI 的原经济/奖励规则并运行 GREEN。

## Task 5：原教练/签到预摆 UI 和流程

**Files:** 新 `Runtime/Peripheral/BeachPeripheralView.cs`、`Editor/BeachPeripheralRestore.cs`、原 UI 数据/资源；修改 `BeachHud.cs`、`BeachGameFlow.cs`，新增 PlayMode 流程测试。

- [x] 从原 UI 树、图片/字体/按钮恢复右侧入口、教练选择/详情/购买/升级/装备、签到卡和红点。
- [x] 绑定原逻辑到明确的运行时方法，关闭/返回释放输入并恢复状态。
- [x] 规则成功后统一保存/刷新/反馈；不足余额、未拥有和已领取显示原状态。
- [x] 验证点击闭环、跨场景/重启、原图横竖屏与对象数量稳定。

## Task 6：广告及其余实际模块差异表

- [x] 对原双倍训练和广告回调、设置/键位/教程、后续对手/段位等逐项标注实际实例、当前状态、依赖和验收证据。
- [x] 复用现有 vivo 接口；只在真实成功回调后发奖励，缺广告位配置记明，不能模拟成真机成功。
- [x] 未实例化的 Locker/Loot/Potion/多人遗留代码与实际游戏缺口分列。

## Task 7：完整回归、保存和交接

- [x] 完整 EditMode/PlayMode，独立复核，失败修复后重跑必要项。159/159 EditMode，1920×1080下36/36 PlayMode，另有17项固定视口专项通过。
- [x] 保存并重开，审计缺脚本/引用/网格/Shader，复核手调对象及新旧存档。360个UI节点及6位预摆教练完整，缺失和Shader错误均为0。
- [x] 归档1920×1080原/复刻页面与正常移动连续证据，恢复用户档和编辑器状态。退出运行，保留目标GameView尺寸，独立诊断存储已清除。
- [x] 更新模块对照清单与验收记录，明确实现、近似、未验证和外部配置项；用户手动打包。见模块审计及peripheral-verification-2026-11-20.json，不宣称全量一比一完成。

## 验收边界和后续排程

本计划完成的是本轮新增模块、可达交互修正和全部实际模块的差异审计，不是整个竞品的全量一比一完成声明。原设置/改名/键位页、后续对手路线、重量难度、完整教程、属性/货币飞行动画及其余装饰表现详见 `docs/beach-boxing/module-parity-audit-2026-11-20.md`，仍未标为复刻完成。

广告位按用户 2026-11-20 的明确答复预留，不阻塞当前交付，不发放模拟广告奖励。构建和手机验收仍由用户手动执行。
