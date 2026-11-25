# 原版模块对照与本轮修正

> 用户最新约束：仅以1920×1080横屏为开发和验收目标，不再安排其它分辨率适配。此前横竖屏证据作为历史保留。原UI内部坐标比例保留，实际目标视口固定1920×1080。

## 结论与范围

当前不能称为“所有功能模块已经完整一比一”。此前的核心闭环验收没有覆盖商店、签到、完整成长、实际步行可达性等内容。本轮针对用户提出的四项进度、两项外围入口、碰撞及器械交互补齐实现，并对仍存在的差异单独列账。测试通过证明相应断言成立，不代表全部画面逐像素或全部动画逐帧相同。

继续沿用现有 Unity 2021.3.14f1c1、Built-in、Linear、GameFrame 和 vivo SDK。地图、器械、人物、六位教练、外围 UI、声音和特效均预摆在 BeachBoxing.unity；没有增加运行时场景或美术资源加载。用户手动打包与手机验收。

## 本轮恢复内容

| 模块 | 原包依据 | 当前实现和验收边界 |
|---|---|---|
| 四项属性条 | WorkingOutManager5006、四个 Image/满格对象 | 原橙色填充、满格覆盖、前后两层原 SDF 文字。超过999使用两位小数的小写k。使用完整原 Sprite 画布，修正旧切图丢失透明边距造成的竖条和缩放错误。 |
| 手动等级 | 原 PrestigeLVL、11个阈值、LoadStats/LvlUp WASM | 四项全满才显示入口；确认一次升一级，保留所有属性/币/星/对手进度。显示下一等级预览，防止关闭期间重复确认跳级。 |
| 体型和头顶信息 | 42组原 Muscle 起止姿势、原等级/段位数组 | 两套玩家骨架共84处肌肉绑定；各部位独立插值，恢复原等级缩放和相机高度。名字使用原44×44词库生成并保存，等级和称号由存档驱动。改名设置页仍未恢复。 |
| 教练商店 | TrainerMenu4755、六个原 prefab、原价格/加成 | Cody/Dion/Nova/Zara/Serena/Yui，星星购买、金币升级、独立装备和四项训练加成；余额不足不扣款。购买不会自动装备。原生加成在升级后保持旧装备缓存，重装备/重启刷新，保留原版行为。 |
| 教练表现 | 原 prefab依赖、Trainer.Update/UpdateTrainerLVL | 六个原模型/骨骼/控制器预摆；仅切换装备对象，恢复等级、缩放、跟随、器械 TrainerPos 和加油动作。没有移植原教练 Rigidbody/碰撞/声音，地形高差等表现仍属于下表差异。 |
| 六档签到 | DailyRewards5008、ClaimReward/Start WASM | Cody、3000金币、60星、10000金币、100星、Zara。按本地日期变化推进一档，不补缺席天数，不重置成七日循环。同一奖励只领取一次，保存领取状态；打开日历按原版清除本次提醒。 |
| 外围入口及菜单 | 原UI树、Button回调、原字体/图片/曲线 | 360个原场景节点、41张完整原切图、27个按钮、7组横竖屏绑定和3条原动画。没有人为增加“Collect from Daily Rewards”文字的直达跳转。恢复竖屏MenuHolder原1.5倍缩放，并保留原代码在同一场景旋回横屏后不复位的行为。 |
| 教程入口门槛 | 原 TurnOffIfTutorial.Start，CurOpponent<=0 | 商店和签到在首次获胜后显示。新档只看到菜单入口是原版条件，不能因此判定两个模块没有接入。 |
| 碰撞 | 原Box/Sphere/Capsule字段及图层矩阵 | 对照50个既有世界碰撞体，新增4面原拳赛外墙。修正错误的尺寸2、偏移中心及绳索形状；保留实体地面，移动查询忽略NPC8和自身，并采用实际缩放后的角色胶囊。 |
| 擂台入口 | 原 OnTriggerEnter FightMenu | 从出生点正常移动即可进入选人，不要求E。关闭后在触发器内部停留不会立即重开，离开再进入才重开。 |
| 四台器械 | 原 feet+up查询、layer6、SphereCollider和GlowRing | 恢复真实检测范围，靠近才亮圈；桌面绿色Interact/E，手机原手掌按钮按下进入。使用原站位、出口、动作、器械Pivot和相机。正常步行测试没有传送绕过碰撞。 |
| 训练加速广告 | 原TryDoubleAutoTrain、120秒回调及vivo SDK | 预留vivo广告位字段；只有真实完成回调才获得120秒加速，改变原自动训练速度目标，不把力量奖励乘2。用户已选择暂不配置ID；缺配置、取消和失败不发奖励。修正SDK销毁后迟到回调的字典访问异常。 |
| 界面音效 | 原AudioSource/EventTrigger、原音频样本 | 预摆原点击/返回/弹出声音，绑定12个按下事件和6个领取事件，接原SFX Mixer。原随机音高组件尚未恢复。 |
| 存档及暂停 | 现有v1 JSON、原新增键语义 | 原属性和钱包字段保留，新字段向后兼容。诊断使用独立存储；暂停/菜单释放触点并阻止底层动作，关闭中的菜单不再接受发奖/扣款事件。极端Int32档也不会训练成负数或部分发奖。 |

## 仍未达到原版的实际模块和表现

以下是已确认的差异，不归入本轮完成项，也不能用规则测试代替验收。

| 项目 | 证据和现状 | 后续工作 |
|---|---|---|
| 完整对手路线 | 原FightMenu4688有46个名称条目，末项为COMING SOON；当前只接入Ramez/Kyle。 | 补后续对手数据、模型、卡片、奖励、AI参数和整条段位路线，分别验证。名称清单见native-roster-inventory-2026-11-20.json。 |
| 原暂停/设置/改名/键位页 | 原Pause GO80、PlayerPrefSettings5002、KeybindsManager4703/Loader5004有实际实例。当前游戏内暂停沿用已有恢复/回框架菜单面板；框架自身音量设置不等于原版设置页。 | 恢复原页面、音量/灵敏度/改名/键位逻辑及持久化，核对vivo键盘行为。 |
| 重量调节和器械反馈 | 原WorkingOutManager有curWeight、推荐重量、配重片/材质、表情及肌肉颜色字段。当前训练收益按原等级/教练规则，未完整恢复重量难度及全部反馈。 | 补重量操作、推荐提示、配重片、难度变化、震动/表情/颜色等实际触发路径。 |
| 货币和属性飞行反馈 | 原五种UIParticle以及领取/获胜反馈路径存在。当前状态和奖励已保存，部分使用现有增量显示/胜利粒子。 | 恢复原图标运动、目标计数框、时序和声音同步。 |
| 教程完整流程 | 当前保留已有引导线、提示及原入口门槛，尚未逐条恢复Tutorial5007的所有阶段/遮罩/提示时序。 | 按新档完整录屏逐阶段对照，不把入口门槛等同于完整教程。 |
| 六项装饰动画和随机音高 | 菜单开关与提醒已采样原曲线，但手掌/Interact脉冲、选中人物图、升级/加速相关装饰动画，以及6个ChangePitchPerLoop尚未恢复。 | 从原控制器/曲线恢复，并按1920×1080实际按钮录屏核对。 |
| 教练物理与环境NPC | 教练原Rigidbody/碰撞/声音被剥离；现有环境NPC为预摆表现，不等于原NPCSpawner随机生成、游走、名字/连接提示逻辑。 | 在保持预摆约束下补真实地形跟随和原行为，测试地垫高差、遮挡和动画相位。 |
| 商业插屏与平台差异 | 原Poki有菜单退出/训练定时commercialBreak；当前只增加激励加速适配。 | vivo插屏另需平台配置与真实回调验证，不能用编辑器模拟成功代替。 |
| 全量像素/帧级表现 | 新模块有原树/素材和1920×1080截图，但没有全部状态和动画相位的阈值通过记录。人物脸部纹理清晰度、肌肉外观和环境角色相位仍有可见差异，不能把资源相同等同于画面相同。 | 固定1920×1080，补相同存档、动作相位及镜头的连续对照，逐项修正。 |

## 原包没有启用的遗留类型

对UnityFS所有资产的MonoBehaviour实例清点中，没有LockerManager、LootManager、Potion、GotChestAlready、GameManager实例；lockerMan为空、6个teleportPositions为空。JoinRoom遗留按钮还指向原包不存在的PlayerManager类型。这些不应直接当成已运行的背包、抽奖、药水、传送或多人功能来复刻。

AutoTrain区域没有Toggle组件，ChangeAutoTrain也没有场景按钮回调；原Update在训练中设autoTraining=true。因此没有凭未使用的方法添加一个原页面不存在的开关。

## 证据和验收入口

- 原包来源、9个主要组件、52组按钮和全部UI字段：`evidence/peripheral-native-audit-2026-11-20.md/json`。
- 碰撞、触发器和原器械检测公式：`evidence/collision-interaction-audit-2026-11-20.md/json`；修复前实际受阻路径：`evidence/ring-walk-before-2026-11-20.txt`；修复后逐帧正常触控路径：`evidence/ring-walk-after-2026-11-20.csv`。
- 等级、收益、体型和姓名来源：`evidence/native-progression-rules-2026-11-20.json`、`native-muscle-morph-2026-11-20.json`及`Assets/BeachBoxing/OriginalArt/World/NativePlayerNames.json`。
- 六个教练依赖的来源和哈希：`evidence/native-trainer-assets-2026-11-20.json`。
- 当前原版1920×1080截图与视口记录：`reference/native-peripherals-1920/`。对应复刻画面为`reference/replica-peripheral-hud-1920-2026-11-20.png`、`replica-trainer-1920-2026-11-20.png`和`replica-daily-1920-2026-11-20.png`。复刻截图采用独立诊断进度；背景NPC随机相位未固定。较早其它尺寸截图只作历史保留。
- 最新测试、保存重开审计、对象数量与数据恢复结果：`reference/peripheral-verification-2026-11-20.json`。本轮没有构建或真机通过声明。

最新结果：EditMode 159/159，1920×1080下完整PlayMode 36/36；另有17项固定视口专项通过。新增画面检查连续改变999/1000/4240/3520/3000并反复开关教练菜单，检查实际帧缓冲内白色字形像素，两次运行通过。诊断中曾在编辑器失焦后截图得到部分旧文字画面，切回Game视图即恢复；没有据此添加未经根因证实的产品补丁，也不宣称已修复一个正常流程可复现的字体缺陷。

保存重开后：3510个Transform、7933个组件、360个原UI节点、6位教练；缺脚本、缺Mesh、缺Renderer材质、Shader错误及不支持Shader均为0。用户原存档按备份原文恢复，独立诊断档已清除，退出运行，GameView保持1920×1080。

## 手调位置

打开`Assets/BeachBoxing/Scenes/BeachBoxing.unity`，在`BeachBoxing`根下调整：

- `Original peripheral canvas`：原进度条、右侧入口、商店、签到、等级页、Interact/手掌和自动训练加速；`BeachPeripheralView.nodes`保留原GameObject ID与引用。
- `Original trainers`：六位原教练。运行时只启用已装备对象。
- `Training beach/<Chest|Arms|Back|Legs>/Original equipment`：原检测球、GlowRing、Workout pose、ExitPos、TrainerPos。
- `Training beach/Original Workout world/LEVEL0/LVL1/SM_FX_Glow_Ring_01`：擂台入口触发器。
- `Arena/Original Combat world/InvisWalls`：补回的四面原外墙。

Editor迁移工具都需显式执行，不会启动时重建场景。后续手调后，不要为普通运行重复执行全量恢复工具。vivo广告位在根对象的BeachAutoTraining组件中预留，用户提供ID后再做设备验证。
