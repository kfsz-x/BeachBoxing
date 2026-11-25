# 原版剩余模块恢复 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** 补齐原对手路线、设置、重量、教程和表现，并在1920×1080保存场景中完成验证。

**Architecture:** 独立原规则及预绑定表现组件接入现有Flow，Editor显式恢复原节点和模型。Root独占编辑器与共享流程，先完成源证据，再逐项实现和复核。

**Tech Stack:** Unity2021.3.14f1c1 / Built-in / Linear / Unity MCP / UnityPy / Il2CppDumper6.7.46 / 原WASM / GameFrame / vivo SDK。

**Spec:** docs/superpowers/specs/2026-11-20-remaining-native-parity-design.md

## Global Constraints

- 唯一开发和验收目标1920×1080横屏。
- 复用原美术，所有场景、模型、特效和UI在Editor预摆；运行时不加载、Instantiate或重建美术/UI。
- 保留用户现有框架、手调和旧进度；独立测试档，结束按原文恢复用户档。
- Root独占Unity、BeachGameFlow、BeachStage、BeachHud和场景；独立模块不得覆盖共享接口。
- 原包未用方法/空实例不推导新系统；未确认数据继续取证，不用经验数字伪装原值。
- 用户已授权连续推进；不逐项停下来确认。不提交、不推送、不重置、不打包。
- 广告位留空的既有决定继续有效，不以模拟完成回调发奖励。

## Review Focus

- 末尾COMING SOON不能作为第46个可战斗条目，最后一场胜利及旧档不越界。
- 重绑、输入名字、暂停与失焦不触发底层移动、攻击或菜单串联。
- 重量不参与源训练速度/收益时不能人为添加难度；手机和桌面原脉冲路径分别验证。
- 教程阈值51、关闭菜单/训练退出/重进/首次胜利只推进正确阶段，提示不反复刷。
- 深度投射在原模型和相机中工作；资源相同及无Shader错误不能代替画面验证。

## Task 1: 原包证据与基线

**Files:** evidence/native-full-roster-2026-11-20.json/md、native-settings-2026-11-20.json/md、native-weight-tutorial-2026-11-20.json/md、native-decal-state-2026-11-20.json、native-decal-glsl/*。

**Interfaces:** 输出源对象ID/字段/公式/调用与未用分支的证据；后续模块以这些已解析原值为配置依据。

- [ ] 逐项解析45敌人、46菜单名称、135节点原设置树、重量和教程方法、表现组件和Shader315。
- [ ] 保存当前用户档和源哈希；复用上一轮失败/通过证据，不重新扫描无关系统。
- [ ] 形成缺口及精确实现接口，分别审计原数据和运行证据的覆盖边界。

## Task 2: 深度投射与原视觉反馈

**Files:** OriginalArt/Shader/Tutorial_054_UnlitDynamicDecal.shader；新增Runtime/Presentation和Editor恢复组件、PlayMode渲染回归。

**Interfaces:** 通过原Shader名称和既有材质引用生效，恢复相机DepthTextureMode.Depth。预绑定动画/粒子组件只播放原曲线，不创建对象。

- [ ] 先做渲染反例：有遮挡皮肤时投射局部xz纹理，体积外不得涂色；占位UV Shader应失败。
- [ ] 按原GLSL和pass state恢复投射、混合、裁剪和深度；原Shader315不采mesh UV、不使用_MainTex_ST。
- [ ] 原装饰动画、奖励飞行、音高和跟随行为逐组件恢复，使用源ID和曲线。
- [ ] 回归渲染、原人物脸部/器械/菜单及声音，记录同状态截图。

## Task 3: 完整对手路线

**Files:** Runtime/Combat/OpponentProfile.cs、独立NativeOpponentCatalog组件、Editor/BeachOpponentRosterRestore.cs、原模型/头像配置、EditMode和PlayMode对手测试。

**Interfaces:** 产生45个源顺序OpponentProfile和预绑定模型/头像；Flow取序列化catalog，Stage.UseModel按源名称切换；末尾COMING SOON只展示。

- [ ] 先测原索引与全部属性/奖励映射、45模型/头像绑定、末尾越界和最后一场不重复越界解锁。
- [ ] 将原EnemyManager参数映射到现有战斗规则，恢复影响AI行为的实际字段及方法，不能只扩展名字。
- [ ] 导入所需原依赖，Editor预摆全部敌人，补齐选人/胜利头像和段位路线。
- [ ] 测首场、中间各段位、最后一场、失败重赛和旧档，复核实际模型外观。

## Task 4: 原设置、改名和九键输入

**Files:** 新Runtime/Settings/BeachNativeSettings.cs和View、Editor设置恢复、相关原UI数据；修改BeachInput及Flow集成由Root执行。

**Interfaces:** 设置状态向LocalStore持久化；View发出暂停/关闭/音量/改名/重绑动作；输入层消费九个源默认角色键位，不等待UI时才允许游戏动作。

- [ ] 先测默认键值、音量映射、旧档、姓名校验和重绑保存，等待输入/关闭/取消/失焦的门控。
- [ ] 追加原GO80设置树与实际Slider/InputField/按钮，绑定原Mixer和原文字；不增加恢复默认/灵敏度控件。
- [ ] 接入改名头顶信息和键位提示；名字输入与键位等待期间屏蔽底层行为。
- [ ] 原页面开关、音量0/100、改名、重绑、重启和框架返回路径回归。

## Task 5: 原重量和完整教程

**Files:** 新Runtime/Training/BeachWeightRules.cs、Runtime/Tutorial/BeachTutorialRules.cs及View、Editor恢复及配置；原训练周期/Flow/Stage集成由Root执行。

**Interfaces:** Weight状态返回当前/推荐kg、按钮/配重和tooltip状态；Tutorial消费属性、CurOpponent、交互阶段，输出原线目标和提示。持久化字段追加到旧档。

- [ ] 先测推荐round-even公式、5..300边界、原按钮步长/配重，curWeight不影响原收益的已确认路径。
- [ ] 恢复桌面3.5/手机5脉冲，dt*25/dt*16插值，默认0.5/广告1.3目标，进度>=.99完成。
- [ ] 预摆原重量/提示UI、配重片及颜色表情特效引用，接入真实器械进入/训练/退出。
- [ ] 教程先测Arms51门槛、CompletedTutorial及首胜提示；恢复全部实际原阶段/节点和持久化，不增加原包没有的遮罩流程。
- [ ] 回归手动/自动训练、广告取消/缺ID、菜单遮挡、重进和旧档。

## Task 6: 集成与逐项表现校准

**Files:** BeachGameFlow/BeachStage/BeachHud/BeachInput、BeachBoxing.unity、新模块PlayMode集成测试和同状态截图。

**Interfaces:** 使用前述独立组件，统一输入门控与保存，所有对象预绑定。

- [ ] 原选人、设置、重量、教程菜单和动画同一状态对照1920×1080截图，修正布局/字体/材质/光照/反馈。
- [ ] 测教程新档完整首胜、训练与商店回路、各段位对手、设置和输入、多指释放及对象数量稳定。
- [ ] 场景保存重开，确认无运行时美术/UI创建及引用丢失，完成任务复核和缺陷修正。

## Task 7: 完整验收及交接

**Files:** 更新module-parity-audit、vivo-handoff、场景审计/测试JSON、manifest和计划ledger。

- [ ] 运行适当EditMode/PlayMode全集及渲染检查，逐项记录实际结果，完成独立最终复核。
- [ ] 归档原版与复刻1920×1080同状态截图/连续交互证据，记录无法证明的剩余视觉差异。
- [ ] 恢复用户档、停止诊断进程及Play、保留干净保存场景和1920×1080 GameView。
- [ ] 更新完整差异清单，不把配置预留、静态证据或自动化断言冒充真机/像素完全相同；用户手动打包。
