# 原版反馈/装饰/移动教程取证（2026-11-20）

本轮仅静态取证：UnityPy读取原UnityFS，读取既有AssetRipper导出Controller/Clip YAML，按Il2CppDumper表槽从original.wat重新提取26个方法。新脚本与WAT全部在 `.superpowers/sdd/2026-11-20-remaining-native-parity/`；原包及既有导出未改。完整字段、rawHex、原对象ID、音频配置、Animator挂载、所有曲线关键帧/切线/权重/无限切线、来源SHA256见同名JSON。

## 移动教学与输入分支

ThirdPersonCharacterControllerSingleplayer5003 的 moveTutorial 指向 GO601 `SingleplayerPlayer/Canvas/MobileControls/Move Tutorial`，原 active=true。Move（slot5039 / func28178）在移动向量不等于零时（Vector3零比较使用平方长<1e−10）隐藏GO601；FirstMove=0时写FirstMove=1，并调用首个gameplayStart。没有读取CompletedTutorial；Start func50093也没有按FirstMove隐藏该对象。

WorkingOutManager.Update func50065 有两条独立键盘路径：配置Keybind4加Escape调用ExitWorkout；后方**字面量 e**经InputUnsafeUtility.GetKeyDownString产生训练加速。`e`不是GetString fallback。Keybind4原初始化Space的证据对应退出训练键。

## 装饰和教程原曲线

JSON含level0实际使用的15种UI Controller，以及奖励预制的CoinParticle，共16种Controller/20个Clip；35个Animator挂载。人物、相机、海和发光区的大动画排除。下列六组常用装饰和空教程线均已完整恢复：

| Controller sharedassets0 ID | Clip ID/名称 | 核心曲线 |
|---|---|---|
| 386 Pointing | 342 Pointing | 1秒循环；根anchoredPosition.y：0秒2.3、.5秒−35.2、1秒2.3；x始终0 |
| 375 Fade | 351 gyjjgy | 3秒循环；Image alpha：0秒.48235294，.5秒.78039217，1秒.49019608，其后每.5秒交替至3秒 |
| 383 KeepTrainingTip | 349 fsefsef | 4.6秒循环；根y：0秒311.3、.36666667秒151.5、.56666666秒163.5、4秒163.5、4.6秒311.3；但Delete在4.5秒隐藏父对象 |
| 384 Move Tutorial 1 | 330 Move Tutorial | 1.6833333秒循环；Finger x89；y：0秒−89、1.3333334秒354、1.5666667秒−89，末帧−89；Finger Image alpha同步1→约0→0→1 |
| 371 Arrow | 352 hhhhhhhhhhhhhhhhhhh | 1秒循环；x：−3.2→1.9→−3.2；y=0；Image green：.85574234→.6666667→.85574234，blue：.48584908→0→.48584908，red=1 |
| 381 Image | 318 Arrow | 1秒循环；y：2.7→−4.2→2.7；green：1→.66279554→1，blue：1→0→1，red=1 |
| 388 TutorialLine | 346 awd | Clip长度1秒，非循环，**全部曲线为空**，无事件；不能凭名称新增呼吸/流动动画 |

Arrow表中的三点时间均为0、.5、1。所有曲线原切线与阶跃信息在JSON完整保留，不能把所有关键帧都当线性插值。

AssetRipper导出的 `script_0x..._...` 是剥离属性名。已用精确CRC32恢复全部哈希绑定，零未解项：`0x1222D899=m_Color.a`，`0x969C9947=m_Color.r`，`0xFB417DAC=m_Color.g`，`0x8B2B8923=m_Color.b`；TMP `0x7DCDF575=m_fontColor.r`、`0x1010119E=m_fontColor.g`、`0x607AE511=m_fontColor.b`。原脚本GUID与导出Image/TMP脚本路径也在JSON，直接复制伪属性名不会得到正确绑定。

实际提示Animator：FightTooltip/Pointing3003、WorkoutTooltip/Pointing3015、手机Pointing3032、ExitTooltip/Pointing3038；教程线3005、移动3020、首胜提示3013；Fade3000/3004/3025；对手路线Arrow3037。其余Controller、状态、过渡、默认状态及UI路径亦已留存。MenuClose327在.16666667秒事件调用TurnOff；这是关闭菜单隐藏的实际事件来源。

## 五种奖励飞行

| sharedassets0预制GO | UIParticle | Delete | 原层级 |
|---|---|---|---|
| 1080 ArmsParticle | 3503 | 3544 | 8节点 |
| 1081 LegsParticle | 3504 | 3545 | 8节点 |
| 1082 ChestParticle | 3505 | 3546 | 8节点 |
| 1083 BackParticle | 3506 | 3547 | 8节点 |
| 1084 GemParticle | 3507 | 3548 | 4节点；text/_text均null |

全部Delete：turnOffAfterTime=true、destroy=true、time=1.5、randomTime=false。JSON还保留所有预制RectTransform、位置/缩放/旋转、Sprite/颜色、文字及组件ID。

UIParticle.ctor func50071：私有firstStopTime初值精确float32为−.15000000596046448。FixedUpdate func50072：有target才推进 `firstStopTime += Time.deltaTime`；大于0后 `position=Vector3.Lerp(position,target.position,dt*firstStopTime*12)`，使用世界坐标和clamped Lerp。没有距离到达判定、计分、销毁逻辑；speed字段未用；也没有OnEnable重置时钟。使用预摆池时需重置−.15秒，才能匹配原来每次Instantiate的初始时序。

WorkingOutManager.SpawnParticle func28164：两个整数Random.Range(−70,70)，加到particleSpawn的世界x/y；Instantiate当前部位预制，旋转=parentCanvas.rotation；设置目标部位HUD、两份文字`+收益`，再SetParent(parentCanvas，默认保持世界坐标)。最后在自身Invoke("GainCoin",1)，但整份dump.cs中没有任何GainCoin方法；本证据不支持据此新增金币。

五类预制共用Controller372 / Clip CoinParticle：根缩放0→1.1（.16666667秒）→1（.28333333秒）；Image缩放.4保持至1秒后在1.1666666秒缩至0；TextHolder缩放.69947保持至.38333333秒后在.5833333秒缩至0；Fade于.9166667秒后放大，1秒到2，在1.1666666秒缩零，alpha于1.0833334至1.1666666秒淡出。CollectCoinSFX对象 `m_IsActive` 在.9166667秒开启，源曲线具阶跃切线；不能只用总飞行时间替代。

四部位根AudioSource播CheckpointReached、volume1；CollectCoinSFX子节点播CollectCoin、volume.2；Gem子节点播CollectGem、volume.26。音频均PlayOnAwake，子节点激活时发出收集声音，不等于金额更新。

## 六类辅助组件

范围sharedassets0+level0，逐字段解码全部闭合：AudioPlay4、ChangePitchPerLoop35、Delete11、LookAtPlayer7、PlayAudioArray1、UIParticle5，共63；另含CameraFollow1和移动控制器5003供引用追溯。

- Delete.OnEnable func49859：CancelInvoke(TurnOff)，延时=time，randomTime时加Random.Range(−.9,.9)，再Invoke。TurnOff func49858依destroy决定Destroy还是SetActive(false)。KeepTrainingTip4652为4.5秒隐藏，五奖励预制为1.5秒销毁；六个场景菜单中其余Delete均turnOffAfterTime=false，由动画事件驱动。
- ChangePitchPerLoop.OnEnable func49881：audio空时GetComponent；changePitchAtStart时 `Random.Range(minPitch,maxPitch)`。FixedUpdate func49880：justStopPlaying且audio未播时随机pitch；dontLoop=false才Play；该分支将justStopPlaying置false，其他帧置true。dontLoop=true时也可能每隔一次FixedUpdate重随机，不能简化成“播完只随机一次”。
- PlayAudioArray.PlayAudio func17020：audios[index]先随机pitch再Play，index循环。训练组件4825顺序2992、2989、2983、2980、2991，五者均原Clip355 `Fantasy click sound 10`。字段命名反转：maxPitch=.8、minPitch=1.2；原调用实参为Random.Range(1.2,.8)，不改写成自行设计的pitch范围。
- AudioPlay.PlayAudio1..4 func49924..49921直接调用对应AudioSource.Play，不另加pitch逻辑。
- LookAtPlayer.Start/Update：cam为空时取Camera.main.transform，已有cam时LookAt(cam.position)，包含垂直角度，不是仅绕y轴的billboard。
- UIParticle见上一节。

额外CameraFollow.Update func49887只做 `Lerp(worldPosition,target.worldPosition,dt*accel)`；序列化camShake/mainCamera字段在该方法未读。KeepTrainingTip的TurnOffIfPhone4653在Start设置active=(OnPhone!=1)。

## 覆盖边界

未运行原版或Unity，本报告不声称运行观测。Controller/Clip曲线为原导出资源，参数哈希已与Unity属性名CRC32吻合；所有重现实现仍需宿主Unity实测。GainCoin字符串调用无匹配方法，不推断货币收益。16种Controller/20Clip是level0 UI与五奖励预制的真实挂载范围，不是整个战斗场景所有动画的穷举。
