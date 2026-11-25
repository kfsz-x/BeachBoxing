# 2026-11-20 当前模块验收入口

当前范围、修正内容、仍缺功能和最新证据见[模块对照审计](module-parity-audit-2026-11-20.md)与`reference/peripheral-verification-2026-11-20.json`。下面2026-11-19记录保留为历史专项，不代表整个游戏已经一比一复刻。

# Beach Boxing 编辑器验收记录

## 光照、字体、转场专项更新（当前结果）

2026-11-19，本轮在现有 Unity 2021.3.14f1c1、Built-in、Linear 工程中完成三项专项修正。没有构建、转换 vivo 包、提交或推送。下文第一轮记录保留为历史，不代表当前实现和测试数量。

### 光照与沙地

原包空间 LightProbes 的位置、四面体、烘焙系数全部为空，也没有 lightmap 数组。撤回上一轮“缺少完整空间探针体积”的诊断。两区使用原版运行时 27 个 SH 系数，已通过原包实际 WebGL uniform 交叉验证。

恢复原生成天空反射 Cubemap：128×128、六面、八级 mip、65,616 个原始 BC1 字节，保持 sRGB 和原 HDR 解码。工程以 Custom reflection 绑定原生成结果，避免不同 Unity 版本重新卷积。纹理字段 m_ColorSpace=1 表示 sRGB，首次实现的误读已通过失败回归纠正。棋盘地垫 Prototype_Global 已按原实际 GLES3 forward program 恢复三平面、法线、太阳、SH、NHxRoughness 与反射，替换旧无光照近似。

远景沙地偏绿的根因是格式语义：原纹理为无 Alpha 的 BC1 RGB；PNG 导出后默认进入 RGBA ASTC，Terrain 将不透明 Alpha 当作光滑度 1，覆盖原 layer 的 smoothness 0。GPU 分量图显示错误的清晰天空反射；仅 AlphaSource None 仍保留 RGBA ASTC，不能解决。现将这张 64×64 小贴图保存为 RGB24 sRGB，保持原像素；两张预摆 Terrain 使用原 BRDF3 的显式 forward 实现，覆盖原单 layer、无 normal map、非 instanced 条件。三块固定沙地区域与原包同机位截图的平均绝对 RGB 差为每通道约 1.3–3.2/255；不代表整图验收。

### 字体与时序

57 个既有预摆文字改为继承现有 Text 的 OriginalSdfText，保留事件、HUD 字段和手调布局。使用原图集、96 字符、小数 advance/bearing/rect、原 face metrics、普通/描边映射和 GLES3 SDF 公式。原 mesh padding 普通 1.25、描边约 3.515，不再误用 atlas padding。另修复热重载后字形 Dictionary 丢失导致文字消失，以及父级缩放动画未刷新 SDF 顶点缩放；均完成实际 RED→GREEN。败北标题和按钮进一步按原白字祖先缩放校正字号/行距，并预摆3个原黑/棕/绿阴影副字；其余字体与Rookie保持。末轮按同视口白字边界校正3处标签位置，阴影相对偏移保持原值，结构与字段引用不变。

倒计时恢复原 XYZ 曲线/切线、FadeCircle、AllAxis_Ridged、颜色及 FIGHT 横幅，4.5 秒结束后开放输入。独立预摆 Canvas 使用原 800×600、matchWidth=0。开场相机按 EntranceCameraAnim 的9条原三次曲线恢复225°环绕、下降和推进，4.5秒交还战斗机位；倒数隐藏菜单/计量条，结算保留原菜单及战斗按钮画面。真实倒地先设 timeScale 0.1，再按原 EnemyManager.Update 每帧 Lerp(timeScale,1,Time.deltaTime) 恢复。原败北子节点名称与 clip 路径不匹配，只有根动画实际生效，现按实际绑定修正。恢复原开场/胜败音量与事件点、2 scaled 秒胜利粒子、0.166667 秒淡黑、0.18 秒切换和 0.5 秒淡入。暂停冻结时钟，重赛/返场恢复速度 1。

败北白字边界专项：900×620 固定视口下，标题四边与原包完全一致；TRY AGAIN 和 RETURN HOME 四边误差最多1像素。原Rookie边界也在1像素内。这些只是对应标签的已测结果，不扩展为所有UI通过。

### 当前验收

| 检查 | 本轮结果 |
| --- | --- |
| EditMode | 44/44，通过；job 1f787e0030b64e43865c799d8942d983。 |
| PlayMode | 22/22，通过；job e3eae0c0364e40b981e2404d7c8c02d2。 |
| 连续迁移 | Transition、Font 连续 Apply 两次，均为 2,460 个 Transform、60 个原 SDF Text（含3个原版阴影副字），没有重复节点。 |
| 连续表现 | 900×620 原包与工程连续状态截图。工程自然时钟记录胜败恢复与返场，未快进表现时间；对象数量不变、进度恢复。倒地起因由诊断设置 HP 为 0，正常战斗数值另由流程测试覆盖。 |
| 独立复核 | 修复字体缩放缓存；最终时间线、引用迁移、沙地 Shader/材质审查无阻断问题。 |

本轮原始来源与回归记录：evidence/lighting-parity-2026-11-19.json、font-parity-2026-11-19.json，reference/presentation-timeline.json。原包序列与 WASM/CRC/曲线见 reference/native-presentation/；全部 3/2/1/FIGHT 补采及实际 GL shader/uniform 见 native-presentation-dense2/。补采实际 elapsed 以 capture.json 为准，文件名目标时刻会受截图耗时影响。当前工程横屏序列见 replica-presentation/、竖屏见 replica-presentation-mobile/；最终审计和 SHA-256 见 polish-verification.json、polish-manifest.json。

本轮三个专项的已知实现差异已按原数据修正。没有把测试通过当作所有画面逐像素、所有动作逐帧相同的证明。姓名、NPC、动画相位与随机 AI 并非全部固定；计划中的全检查点 HUD 4 px、关键点 1%、动作 2 帧阈值仍没有全量通过记录。也未扩展 NPC 随机游走、完整段位路径、外围经济和原生 Rigidbody 调度。vivo 构建、Shader 转换、真机性能、多指、安全区与前后台由用户手动验证。

## 第一轮历史记录（已被上方专项更新修正）

以下数量、近似实现及差异描述为专项开始前的历史状态。其中“缺少空间探针体积”的归因已撤回；字体整数度量和近似转场已由本轮替换。

2026-11-19。环境：Unity 2021.3.14f1c1、Built-in、Linear，现有 GameFrame 与 vivo SDK。原包为用户提供的 Unity 2022.3.34f1 WebGL。当前工程直接修改并保存，没有提交、推送、构建 WebGL 或转换 vivo 包。

## 实施结果

核心闭环与可编辑场景已经接通：自由移动、四项训练、加速与退出、存档、Ramez/Kyle 选人、倒数、出拳、格挡与恢复、胜败、首胜奖励、解锁、重赛、暂停与返场。两张原地图作为同一场景内的训练区与拳赛区预摆，切换时启停现有对象。

场景为 `Assets/BeachBoxing/Scenes/BeachBoxing.unity`。最终层级包含 2,438 个 Transform。训练模型与拳赛模型分别使用对应原骨架、Avatar 和 Animator；两位对手、器械、Terrain、相机、灯光、桌面/手机 UI 和粒子已保存。Editor 恢复工具不会自动运行。

原版资源包括几何、贴图、切图、字体图集、骨骼动画、音频和粒子。棋盘、动态 decal、SDF 文字和引导线的 Shader 采用当前版本兼容实现；这部分不是恢复了原始 Shader 源码。

## 验证与证据

| 检查 | 结果与范围 |
| --- | --- |
| EditMode | 27/27，0 失败；含训练结算、属性阈值、首胜幂等、命中窗/几何、AI 节奏、场景引用与触屏设备状态锁存。 |
| PlayMode | 21/21，0 失败；完整结果见 `reference/final-verification.json`。包含框架正常入口、四项训练、首胜与解锁、触点所有权、竖屏机位、近身换边镜头半径、败北重赛与返场。 |
| 保存重开 | 将 Arms 组移动 X/Z 各 0.1、PUNCH 按钮移动 1 个逻辑像素；保存、关闭、重开后两个位置检查均通过，随后恢复原位并保存。 |
| 静态引用 | 全部活动和非活动层级审计：缺失脚本 0、缺失 Mesh 0、不支持 Shader 0。最终数量与结果见 JSON。 |
| 手机训练 | 432×768 的独立预摆布局；加速增长、手机/桌面 EXIT、切换布局时释放旧触点、唯一活动 EXIT、对象数量恒定均已实测。 |
| 录制 | 训练 240 帧/30 fps：走近、交互、训练、退出。拳赛 360 帧/30 fps：空拳、接近、格挡、追击出拳；最终固定 Unity 随机种子 2468 的录制记录 5 次格挡命中；前轮随机走位还出现过不同次数，体现随机 AI 差异。 |
| 存档保护 | 录制前备份进度。拳赛录制结束先禁用流程，防末拳延后落账，再恢复备份；训练录制结束后另行恢复。交接时为录制前的全零测试档。 |
| 代码复核 | 独立复核发现并修复 Animator 误写与 WebGL 瞬时触点切布局问题；末轮复核结果及回归记录保存在计划执行记录中。 |

回归先失败再通过的重点案例：训练模型切换污染拳赛控制器；竖屏开场裁切；败北独立面板与重赛/返场；触屏设备识别、松手和输入 Reset 后保持布局。编辑器负向资源测试有意请求 `sample/invalid`，其预期异常不等于玩法缺资源。

## 对照文件

以下均位于 `docs/beach-boxing/reference`。视频为画面录制，没有音轨；音频以场景绑定和触发路径核对，未完成波形级对拍。

| 状态 | 原包 | 当前工程 |
| --- | --- | --- |
| 桌面出生 900×620 | `original-extra/spawn.png` | `replica-final-desktop.png` |
| 附件比例 1136×512 | `original-extra/spawn_wide.png` | `replica-wide-spawn.png` |
| 手机出生 432×768 | `mobile-01.png` | `replica-mobile-spawn.png` |
| 四项训练 | `original-extra/Chest.png`、`Arms.png`、`Back.png`、`Legs.png` | `replica-workout-Chest.png`、`Arms.png`、`Back.png`、`Legs.png` |
| 手机训练 | `original-extra/Arms-mobile.png` | `replica-mobile-workout-final.png` |
| 选人 | `original-extra/fight-menu.png` | `replica-selection.png`；手机另见 `replica-picker-mobile.png`、`replica-picker-kyle-mobile.png` |
| 倒数与战斗 | `desktop-09.png`、`desktop-11.png`、`original-extra/combat-12s.png` | `replica-countdown.png`、`replica-fight-live.png`、`replica-mobile-fight.png` |
| 胜利与败北 | `original-extra/Victory.png`、`original-extra/Defeat.png` | `replica-victory.png`、`replica-defeat.png` |
| 连续画面 | `original-arms-visual.mp4`、`original-combat-visual.mp4`、`original-mobile-training-visual.mp4` | `editor-training-walkthrough.mp4`、`editor-combat-walkthrough.mp4` |

四项训练并排图为 `comparison-workouts.png`；出生叠图/差图为 `overlay-spawn.png`、`difference-spawn.png`。它们用于找差异，不是视觉验收通过证明。角色随机姓名、NPC、动画相位、相机适配和存档数值不完全一致的样本，不能用整图像素误差判定通过。

`replica-picker-desktop-1.png` 等早期选人专项图直接切换 UI 状态，背景还是训练机位；仅用于布局核对。最终 `replica-selection.png` 从预摆入口位置进入选人。追加原包胜败图调用其现有 `Victory`/`Die` 方法，用于页面观察，不作为自然战胜/战败或数值规则的证据。`Defeat.png` 等待 10 秒，避免把慢动作倒地误当作最终败北页。

原包字段和来源记录：`workout-cameras.json`、`combat-cameras.json`、`original-ambient.json`、`combat-button-source.json`、`original-scene-data.zip`。战斗按钮圆底实际是 256×255 的 `Handle_Ridged`，已经替换早期错误的 32×32 Circle。新增文件的 SHA-256 记录见 `manifest-final.json`。

## 尚未通过的视觉项目

1. 没有把全部对照点统一到相同存档、随机种子、角色位置与动画帧。计划中的 HUD 4 px、关键点 1%、动作首帧 2 帧容差，不能宣称全部通过。
2. 源场景没有 lightmap 数组，现恢复两区各 27 个环境光 SH 系数；完整空间光探针体积尚未合并。地垫、人物和环境仍可见明暗差异。
3. SDF 使用原图集和 96 字符度量，但当前文字组件的整数度量、字距、粗体、描边与 TMP 原生组合不完全相同。姓名、标题和部分数字仍有差异。
4. 对战镜头使用原桌面/手机偏移，旋转期间按局部偏移沿弧线平滑，修复旧版沿弦穿入近身区；绕双方中点和距离的适配仍非原生完整相机算法。现有竖屏测试覆盖开场角色中心，换边测试覆盖镜头半径，不能证明移动中每个肢体、计量条或更窄安全区始终可见。
5. 判定、移动与 AI 已恢复核心原生参数；随机序列、重复低体力回调和 Unity 物理边界没有逐种子对齐。当前使用解析胶囊/球判定和移动扫掠，不能等同完整原生 Rigidbody/Trigger 调度。
6. 倒数光晕、FIGHT 过渡、败北慢动作/面板动画、部分反馈箭头、粒子相位、音频与镜头震动尚未逐帧对齐。现有兼容渲染和转场仅保证核心循环可用。
7. NPC 复用原模型和训练动作；随机游走、AFK、姓名和进阶体型未恢复。选人段位栏仅保留两名核心对手节点，原版五节点布局与箭头样式仍有差异。
8. vivo 构建、Shader 转换、手机性能、真多指、安全区与前后台恢复由用户手动验证。本轮没有真机或打包通过结论。

这是一份可继续手调和真机验收的编辑器交接结果，不能称为“完美复刻已验收”。手机步骤见 [vivo 交接清单](vivo-handoff.md)。

最终手机拳赛图是录制末尾暂停后的 432×768 定点取景，用于检查当前 UI 与手机机位，不是与原包同步的战斗帧。
