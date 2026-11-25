# 原版重量与教程静态取证（2026-11-20）

来源：原版 `data.unity3d` SHA256 `3b109d92fcf4ced42ddf76c238e92d38a755d006d7413ea21bedc742aaf6cf67`，UnityPy 解码序列化引用；配套 `native-dump/original.wasm`、Il2CppDumper `dump.cs/script.json`、WABT `original.wat`。所有结论为给定原版文件的静态观察，没有运行原版、操作 Unity Editor、清存档或打包。逐字段原始偏移、对象路径、资源 ID、方法来源 SHA256 和 UI rect/image/button 信息见同名 JSON。

## 重量

`WorkingOutManager5006`，`CalculateRecommendedWeight` table slot5090 / WASM func5882：当前部位力量乘精确 float32 `0x1.42381ep-6`（`0.019666699692606926`），加5，除5，银行家舍入后乘5；上限300，最终下限5。按钮显示整数加 `kg`，无空格。

| 操作 | 原版行为 | 来源 |
|---|---|---|
| 进入器材 | 重置进度，当前重量直接设为推荐重量 | EnterWorkout func28170 |
| −5/+5 | 重置进度，重量最低5，无上限；标记用户改过重量 | ChangeCurWeight func50062 |
| 推荐按钮 | 重置进度并设为推荐重量，不设置“手动改重量”标志 | ClickRecommendedButton func50063 |
| 推荐提醒 | 仅当前重量低于推荐时显示 GO513 | UpdateWeightText func9399 / UpdateRecommendedText func17013 |

按钮：GO159 / Button4583 为−5；GO402 / Button4662 为+5；GO910 / Button4893 为推荐；主文字 TMP4322 与4344，推荐数字 TMP4464。原版 `curWeight` 未持久化，每次进入器材重新推荐。

重要边界：完整 `WorkingOutManager.Update` func50065 **没有读取 this.curWeight**，速度和收益都不依赖当前重量。不能添加重量/力量比难度。plates[16] 序列化全零；四组配重 Transform 数组各16项，仅首两项非空。所有 WorkingOutManager 方法中未找到这些配重字段的使用（构造函数给 plates 分配16项除外），没有可复刻的动态加片算法。

## 动作与交互

WorkoutButton(true) func50061 只排队一次输入，false 不做任何操作。EventTrigger4836 挂 GO804：PointerDown→true，PointerUp→false；Button4834 的 onClick 为空。按住不产生连续脉冲。

func50065：E按下或待处理脉冲让速度变3.5（手机5），消费该脉冲，条闪黄、播放下一条 workoutArray 音频并随机 pitch。每帧速度 Lerp 到0.5（doubleAutoTrain时1.3），系数 dt×25（手机dt×16）；进度累加 speed×dt，达到0.99完成；Animator 当前部位+Workout=true、WorkoutSpeed=speed。每个工作帧都会把 autoTraining 设为true。

输入分支不能混淆：func50065 第155–180行的 Keybind4→GetKeyDown 调用 **ExitWorkout**，并列 Escape；其后约795行的加速分支直接加载字面量 `e`→InputUnsafeUtility.GetKeyDownString。`e`不是 GetString fallback。Keybind4 原初始化 Space 的设置证据不改变训练加速键。

速度生命周期：构造函数func50054令私有workoutSpeed=.3f（bits1050253722）。ResetWorkoutBar、EnterWorkout、ExitWorkout、±5、推荐按钮均不重置该变量，也不清history；重置的是进度和Animator的WorkoutSpeed=0。脉冲设置3.5/5后**同帧仍Lerp**，之后累加进度；held不持续提高目标，目标1.3只来自doubleAutoTrain。Update先采旧speed进入history，再处理该帧脉冲和Lerp。

速度历史保留0.5秒（predicate func50053），每0.5秒评估一次：

| 均速 | 表情材质 sharedassets0 | 汗 rateOverTime |
|---|---|---|
| <0.6/空历史 | normal43 | 0 |
| 0.6至<1 | angry45 | 20 |
| 1至<1.5 | struggling46 | 50 |
| 1.5至<2.3 | struggling46 | 90 |
| ≥2.3 | 保持上一状态 | 保持上一状态 |

UpdateMuscleColour func28168：`t=Clamp01(abs(Clamp01(fill)-.5)*2)`，当前部位用 `Color.Lerp(redColor,skinColor,t)`；该部位肌肉 multiplier≥1 时不改色。skin=(1,.6196078658,.4941176474,0)，red=(1,.1764705926,.1764705926,0)。因此中途最红，起止还原肤色。ResetWorkoutBar func5883 重置进度、触发 Restart[部位]Workout、WorkoutSpeed=0，四组肌肉材质回肤色。

完成 func28165：震动 intensity=.08 / duration=.14；部位 Strength 加收益；轮播 workoutParticles 索引0、1、2（虽然数组有4项）；刷新数据、重置并更新推荐。tier1..10 基础收益 `[11,13,15,20,24,28,33,38,43,48]`，乘 `(1+部位Modifier/100f)` 后截断。curWeight 不参与。

## 教程阶段与提示

`Tutorial5007`：

| 条件 | 原版效果 | 方法 |
|---|---|---|
| CompletedTutorial=0 | 连线点1→玩家头部 Transform1444；点0→Arms器材 Transform1420；ArmsStrength≥51 时存 CompletedTutorial=1 | Update func50074 |
| CompletedTutorial=1 | 点0换为擂台发光区 Transform1583；点1仍跟玩家头 | 同上 |
| Start时 CurOpponent>0 | KeepTrainingHint!=1 时显示 GO372并存1；关 TutorialLine GO199；停用 Tutorial5007 | Start func50075 |
| 首对手且 Arms训练，力量≤49 | 关引导线，开 WorkoutTooltip GO3 | CalculateRecommendedWeight func5882 |
| 首对手且 Arms训练，力量≥50 | 关 WorkoutTooltip；0.2秒后开 ExitTooltip GO314 | 同上 / ExitTooltip func50055 |
| 退出训练且 CurOpponent=0 | 重开引导线；关训练UI父级GO881，未直接清两个子提示 activeSelf | ExitWorkout func28169 |
| 首对手打开对战菜单 | 开 FightTooltip | FightMenu.OnEnable func49834 |

**50和51是原版不同门槛，需分别保留。** 反复刷新推荐也会重复触发 Arms 提示副作用。

TurnOffIfTutorial.Start func50076：CurOpponent≤0 时隐藏自身。三个挂载对象为 Daily Rewards GO510、AutoTrain GO643、TrainerButton GO855。以首个对手状态为门槛，不是 CompletedTutorial。

KeepTrainingTip GO372 文字为 “Keep working out to defeat stronger opponents!”。Delete4652 序列化 turnOffAfterTime=true / destroy=false / time=4.5 / randomTime=false；OnEnable func49859 取消旧 TurnOff 后重新排4.5秒；TurnOff func49858 仅隐藏对象。另有 TurnOffIfPhone4653；Animator3013 对应 sharedassets0 Controller383 `KeepTrainingTip`。

原版 Tutorial 没有屏幕遮罩字段；WorkoutTooltip/ExitTooltip 是 pointing 图片层级。JSON 保留了完整局部UI证据；不能凭教程概念新增遮罩。

## 场景接口建议与未解项

重量模块接收进入器材、±5、推荐、单次训练脉冲、逐帧dt、完成重置；阶段层负责实际器材、动作与相机。ChestPressMachine.GetCameraPosition func49872：`Vector3.Lerp(newCameraPos,newCameraPosLvl10,(Clamp(level,1,10)-1)/9f)`，应直接引用原站点配置。

教程层读取/保留 CompletedTutorial 和 KeepTrainingHint；线指向场景中的原 Arms 站与擂台区，不用替代目标。首胜提示在原版是 Tutorial.Start 检查，并非 Update实时监听。

未解：本次没有运行原版；未追踪移动教学的隐藏逻辑与各教程动画曲线。给定 AssetRipper 临时目录在该子任务环境中不存在，已直接解析原 UnityFS。无动态配重算法证据。
