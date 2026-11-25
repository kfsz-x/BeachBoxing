# 原运动/教练/NPC取证（2026-11-20）

原UnityFS、WASM、Il2CppDumper及持久AssetRipper导出静态取证；没有运行Unity、修改产品、清档或联网。新脚本/47份新WAT在 `.superpowers/sdd/2026-11-20-remaining-native-parity/`。同名JSON包含10个完整脚本解码、六教练/NPC/玩家8棵必要物理变换树、原模型与渲染资源引用、7个NPC器材全部继承字段、14个旧出生点的世界矩阵、原物理/时间设置和50份方法来源（含3份复用只读旧证据）。

## 玩家移动与跳跃

源 `ThirdPersonCharacterControllerSingleplayer5003` / 玩家GO1209 / Rigidbody2943：

| 项目 | 原值/行为 |
|---|---|
| 世界移动速度 | 35；相机相对方向归一化，设置rb.velocity.x/z，保留y |
| 跳跃初速 | 37；直接设置rb.velocity.y，不是AddForce |
| 全局重力 | PhysicsManager `globalgamemanagers:10`：y=−80；Rigidbody.useGravity=true |
| 原固定步长 | TimeManager `globalgamemanagers:8`：.033秒 |
| coyoteTime | .1秒；落地补足，离地按dt扣减；跳键KeyUp会清零 |
| canJump冷却 | 起跳置false，.1秒后TurnOffJustJumped重开；同回调清justJumped |
| 下降额外速度 | 空中且velocity.y<0时，每Update再减.01，无dt乘法 |
| 地面附加项 | groundStick=0、groundAccel=0；不能替换成经验加速/粘地值 |
| 地面检测 | PlayerManager.Update func50116：OverlapSphere(feet.position,.4,groundMask)，有命中则grounded |
| 落地表现 | 空中先置justGrounded=true；重新grounded后播landSFX84/landParticle60，再清标志 |
| 走尘 | grounded且移动时emission5，否则0；不按步数动态生成 |

Update func50091读取配置 `Keybind4`→GetKeyDown，原默认Space。在grounded或剩余coyote>0并且canJump时起跳，播放jumpSFX；先清y再赋37，保留水平速度。手机公开Jump func50090仅设置jumped=true，并在.1秒后StopJump清零；真正跳跃同样由Update门槛执行。没有按住加力曲线。SDK广告标志会阻止该Update；FixedUpdate也在canMove且SDK未阻断时才调用Move。

Keybind4在世界中跳跃、训练中退出；训练加速另为字面量e。必须按模式门控，不能硬编码另一套Jump键。

Armature控制器原过渡：AnyState→`Player_MidAir`条件Grounded=false、Carpet=false、WorkingOut=false，fixed duration=.08，无exitTime；MidAir→`Player_Idle`条件Grounded=true，duration=.1，无exitTime。**没有Jump trigger**。渲染层若每帧强置Grounded=true，将覆盖腾空动作。

玩家物理：rb2943质量1、非kinematic、UseGravity=true、interpolate1、constraints112（全旋转冻结）、collisionDetection3。Capsule3052在GO520 `PlayerCollider`，radius=.5/height=1，子物体localPosition.y=2.5578980446，localScale=(1.5,5.0137600899,1.5)，材质Slippery391。不要只复制height=1而遗漏变换。

现有原场景换算比例 `.356` 下，纯空间换算为：速度12.46、跳初速13.172、重力−28.48、地面球半径.1424、每Update额外下降速度.00356。冷却/固定步长不缩放。这些是数学转换，不是另一份原参数。

## 教练运动与地形

| 教练 | prefab GO | Trainer | Rigidbody | Capsule |
|---|---:|---:|---:|---:|
| Cody | 1190 | 3389 | 3179 | 3221 |
| Dion | 972 | 3584 | 3177 | 3220 |
| Nova | 1417 | 3397 | 3178 | 3222 |
| Zara | 1332 | 3489 | 3181 | 3224 |
| Serena | 1329 | 3541 | 3182 | 3219 |
| Yui | 956 | 3495 | 3176 | 3218 |

均为sharedassets0，root Layer8；prefab根默认位置(7.4200000763,3.2400000095,44.9599990845)，默认scale1，但原EquipTrainer func50078实际在玩家位置、identity旋转实例化，并绑定targ=player。不要将预制默认位置误当运行出生点。

六者原Rigidbody均mass1/drag0/angularDrag.05/UseGravity=true/非kinematic/interpolate0/collisionDetection0/**constraints122**（冻结物理X/Z及所有旋转，Y自由）。Capsule radius=.5，height=5.8154392242，centerY=−.3549976349，非trigger，启用，Slippery391 static/dynamicFriction=0、bounce=0。

Trainer.Update func50085没有Raycast、Terrain.SampleHeight、向玩家y插值或高度snap。它只计算平面方向、手动移动transform的水平位置；高度由动态Rigidbody和地形碰撞决定。这一“地形跟随”解释是代码+物理配置推断，未运行原版。

- 非训练：取消旧TPToWorkoutSpot；平面距玩家<7停止Walking；≥7直接朝向玩家，`position += forward*20*dt`，Walking=true。无追赶加速或平滑旋转。
- 训练：改向机器trainerPos；距该点<1停止Walking并面向玩家。tryCheer初始true时播放一次：Random.RangeInt(0,4)==0用Cheer，否则Cheer2；2..8秒后TryCheer重新允许。距≥1时仍以20*dt移动。
- OnEnable置Grounded=true，UpdateTrainerLVL func17016读取Trainer{index}LVL，文字`LVL {n} Trainer`，root scale=`1+n*.01`。保留人物子层级的原旋转/位置偏移。
- WorkingOutManager.EnterWorkout设workingOut=true、workoutTarg=machine.trainerPos，并排TPToWorkoutSpot(1秒)；Exit清workingOut。给定Trainer方法清单里没有TPToWorkoutSpot实现，不能凭这个字符串补造瞬移。

## NPC资产与碰撞

原prefab为sharedassets0 GO1400 `NPC`，脚本3527，Rigidbody3180，Capsule3223，Animator3206，MuscleManager3435，名字TMP3415、称号3478、等级3455。源可直接复用持久导出 `Assets/GameObject/NPC.prefab`；JSON保留模型层级、Mesh/材质/bone引用及肌肉配置。

NPC root Layer8/Tag NPC，原rb质量1、UseGravity=true、非kinematic、constraints112（冻结全旋转）、interpolate0、collisionDetection0。Capsule radius=.5、height=9.4722118378、centerY=4.7521505356、非trigger，材质null。根runtime缩放由随机等级设置，不能只缩模型、遗漏碰撞体。

物理层：Player7、NPC8、Ground3；原矩阵仅禁Player7↔NPC8和NPC8↔NPC8。教练也Layer8，因此源教练/NPC不会物理推挤玩家或彼此。地形和普通场景碰撞仍启用。

## 生成数量、生命周期与提示

NPCSpawner4671挂GO416；npcSpawnPoint Transform1597世界位置为(14,10.8999996185,72)，fightArena Transform1583。7个器材脚本4955、4877、4793、4697、4791、4598、4918；旧出生点14项的源ID、完整世界矩阵及7台器材playerPos/exitPos/idle/move/pivot/holder等字段均在JSON。

Awake func49789先存SpawnedNPCs=0。Update func49788：

1. OnPhone=1或timesFailed>2时直接跳过，手机不生成NPC。
2. 桌面记录(time,1/dt)，保留最近3秒（predicate func50126），每3秒检查一次。
3. 均FPS≥40且有样本、SpawnedNPCs!=1时，Connected提示触发On，存SpawnedNPCs=1，连续调用SpawnOldNPC三次。初始目标是**3个**，没有额外maxCount字段。
4. 均FPS<40或空样本则timesFailed++、DestroyAllNPCs。该方法找Tag NPC，每个NPC触发Disconnected提示、ExitWorkoutStation、Destroy。**不清SpawnedNPCs**；超过2次失败后停止检查。cock序列化false；若true会先强制targetFrameRate30，与40阈值有冲突，不能将此支路当默认。

SpawnOldNPC func12025每次独立随机选14点之一（允许重叠），oldNPC=true，yaw整数0..359。SpawnNewNPC func49787在1597位置、identity旋转生成普通新NPC，不触发Connected提示。

NPC.Start func49800给每个NPC安排随机整数30..299秒LeaveGame。NPC到擂台区平面距离<3时，还会另排1..2秒后的LeaveGame。LeaveGame func49792先释放器材，3..9秒后向master排SpawnNewNPC，然后Destroy自己；原方法没有取消重叠计时器。

ConnectedAlert Animator3028、Disconnected3022，文字分别是`Connected to Server!`、`Disconnected from Server!`，由本地条件驱动；FreeLookAlert378控制器及两条TextAlert曲线已一并留存，不代表真实联网。

## 名字、等级、行动与跳跃

名字全部本地生成：Random.RangeInt(1,4)，结果1用fakeNames73项之一，结果2/3用44形容词+空格+44名词。完整数组保留在Spawner解码中；不新增玩家列表或在线强度。

LoadSize func28127：等级均匀随机1..10，WorkoutSpeed=.5；根scale依次1、1.15、1.3、1.45、1.6、1.7、1.8、1.9、2、2.1。称号1..5依次Rookie/Amateur/Semi Pro/Rising Star/Pro Boxer，**6..10全部Division Threat**。四部位肌肉独立Random.Range(.1,1)。这些是外观参数，不是NPC战斗数值或训练收益。

Start初始行动抽整数1..4：1设goesAfk并在5..9秒后AFK；2设goesAfk并在10..19秒后Decide；3/4若oldNPC则0..0.222秒后FindStation、再.1秒后Interact，否则立即Decide。AFK仅将私有afk置true，13..89秒后清；**FixedUpdate不读取afk**，不能添加原版没有的暂停游走机制。

Decide func12026：goesAfk时RangeInt(1,2)==1恒成立，另排1..2秒AFK。抽RangeInt(0,5)==0去擂台（20%），否则去空器材（80%）；FindStation从nonnull且interactable的7器材中均匀选。没有空器材时原方法递归Decide，没有经验重试周期。

FixedUpdate func49797：到器材平面距<3停止Walking，.1..0.3秒后Interact，清goingToWorkoutStation；否则 `position += forward*25*fixedDeltaTime`。去擂台同速、同3阈值，到达安排离场。方向由选择目标时LookAtTargetY直接设置，不存在独立随机航点漫游或每帧寻路。

RandomTryJump首次在float.2..3秒，之后每整数1..3秒设置tryJump。只在实际行走分支消费：Jump func16987令rb.velocity.y=33、Grounded=false，.7秒后回Grounded=true（没有碰地测试）；末尾清任何遗留tryJump。以.356空间比例换算NPC水平速度8.9、跳初速11.748，仍属转换值。

器材交互：原InteractWithWorkoutStation func49794占用interactable=false、隐藏pivot、瞬移到playerPos，清各部位idle后开当前idle，.2秒后开moveAnim，WorkingOut=true；训练时间整数10..89秒，退出后再float.3..1秒Decide。机器holder按NPC等级缩放。ExitWorkoutStation func16985恢复pivot/interactable、瞬移exitPos、清idle/move/WorkingOut、速度零、旋转identity、holder回scale1。占用/退出必须在池复用时正确释放。

## 网络遗留和池化边界

给定dump.cs与stringliteral.json均无JoinRoom方法或字面量；NPC/NPCSpawner路径是Unity本地Instantiate/Invoke/Random/PlayerPrefs。Connected提示是本地装饰，不能据此新增网络服务。TPToWorkoutSpot、GainCoin等无法对应方法的字符串调用已分别标注，不补造实现。

用户要求预摆池时应保留初始3个、生成门槛、离场/补位时间及全部Start私有状态重置；原版没有池容量字段。“3槽池”是适配用户约束的实现选择，不应标成源序列化容量。尚无原版实时运行验证；高度表现需用原碰撞树在宿主Unity验证。
