# Beach Boxing：1920×1080 手动打包与设备验收

当前唯一目标是1920×1080横屏。继续使用Unity2021.3.14f1c1、Built-in、Linear、现有GameFrame与vivo SDK。场景、45名对手、人物、六位教练、UI、音效和特效预摆在 `Assets/BeachBoxing/Scenes/BeachBoxing.unity`，运行时切换和播放已绑定对象。编辑器验证不能替代vivo转换与真机验证；本轮由用户手动打包。

## 打包入口

1. 打开 `D:/Desktop/BeachBoxing`。
2. Build Settings 启用顺序保持 `Assets/GameFrame/Init.unity`、`Assets/GameFrame/Game.unity`、`Assets/BeachBoxing/Scenes/BeachBoxing.unity`。
3. 使用现有 `VIVO小游戏 / 转换小游戏`，核对自己的包身份、签名、输出目录与landscape方向。只按1920×1080验收。
4. `BeachBoxing` 根上的 `BeachAutoTraining` 保留激励广告位配置。ID按用户决定暂留空，未配置、取消、失败均不能发放加速。不要用编辑器模拟成功作为广告通过证据。

## 核心流程验收

- 新档进入海滩，移动教学在手机实际移动后收起。引导线先指向原手臂器械，Arms达到51后指向擂台；首次胜利后开放原商店和签到入口。
- 从出生点真实走到四台器械和擂台。器械接近才出现光圈及绿色Interact；擂台进入触发区自动打开选人，无需按E。关闭选人后留在圈内不应立即重开，离圈再进入应重开。
- 四项训练进入原站位、隐藏器械活动Pivot，使用原动作、第一秒相机过渡、表情、肌肉颜色、汗、属性图标飞行和音效；退出使用原出口和即时闲置镜头复位。
- 桌面E或手机每次点击产生速度脉冲。长按不持续获得额外加速。自然速度从0.3向0.5过渡，完成一次按原等级和教练加成发属性。更换重量只重置进度条，保留速度历史。重量至少5kg，源逻辑没有人为添加的300kg上限；推荐重量使用原round-even公式。
- 完整路线为45场，末项COMING SOON只展示，不能开赛。检查首场、各段位交界、最后一场、失败重赛和通关旧档。重赛已战胜对手仍按原版发放奖励；只有打赢当前解锁索引才推进路线。
- 开场3/2/1/FIGHT及相机共用原4.5秒时间。检查出拳、体力、格挡、受伤移动停顿、击退、低血量边框、数字命中提示、胜败慢动作、重赛与返场淡出。保留原手机版镜头取值，不要求人为把双方一直夹在视口内。

## 外围、输入与存档

- 四项属性条、小写k数值、满格覆盖、等级和段位随状态刷新。手动升级只有四项全满才可用；一次确认只能升一级，保留钱包与路线。
- 六位教练：购买、独立装备、金币升级、跟随和器械加油；购买不能自动装备。升级后原版装备缓存需重新装备或重新进入才刷新加成。
- 六档签到按本地日期推进，缺席不补档，同档不重复领取；检查重启持久化和提醒清除。
- 原设置页：Music/SFX的原0..2滑块、改名、九键重绑、隐藏时待选键恢复及返回。海滩设置页不暂停海滩时间，拳赛设置暂停比赛；拳赛RETURN HOME回海滩。框架主菜单入口与游戏内返回是不同路径。
- 手机系统键盘：首次打开、取消、确认、重复点击同一姓名字段再次打开；中文输入需按原字体实际字形覆盖检查。此项需vivo设备验证。
- 多指移动、出拳、格挡各自持有指针。无关指针抬起不得释放别人的动作；滑出按钮本身不取消原版持有。拥有者抬起、打开菜单、失焦或切后台时应清空旧输入，返回后不得自动继续移动/攻击。
- 重启核对属性、币、星、姓名、键位、音量、签到、教练和路线。离线基础循环不依赖服务器；桌面本地环境NPC按原FPS条件启用，手机原规则不生成这三名环境NPC。

## 渲染与性能检查

- 原沙地已保留64px源基底并处理两级已剥离mip的Unity2021导入差异。沙地使用RGB24，避免转换器将不透明Alpha当作光滑度1。不要在转换时自动改回RGBA ASTC。
- 检查地形、脸部深度贴花、原SDF字体、透明光圈、粒子、原反射和声音。相同资源或编辑器编译通过不等于设备画面通过。
- 连续进行训练、菜单、三场拳赛与返场，记录首屏耗时、卡顿、内存、帧率、触点或音效异常。

## 手动调整

直接编辑并保存 `Assets/BeachBoxing/Scenes/BeachBoxing.unity`：

- `Original peripheral canvas`：原属性条、商店、签到、设置、选人、训练和触控节点；组件保留源GameObject ID与引用。
- `Training beach/<Chest|Arms|Back|Legs>/Original equipment`：检测球、GlowRing、Workout pose、ExitPos、TrainerPos。等级会按源规则缩放相应器械Holder，编辑时保留锚点父子关系。
- `Training beach/Original Workout world/LEVEL0/LVL1/SM_FX_Glow_Ring_01`：擂台入口触发器。
- `Arena/Original Combat world/InvisWalls`：原拳赛外墙；`Player spawn`和`Enemy spawn`为已对照的出生锚点。
- `Original trainers`、`Training beach/Original ambient NPCs`：六位教练和三名预摆环境NPC池。
- 根上的 `BeachNativeBeachCamera`、`BeachNativeCombatCamera` 与 `BeachGameFlow.stage`：海滩、训练、比赛镜头与引用；`Gameplay camera base pose`承接基础镜头，子Camera承接原震动。

恢复菜单只用于显式迁移。后续普通预览直接运行保存场景，不要重复执行全量恢复覆盖手调。问题回报请附机型、系统、小游戏环境版本、构建日期、操作步骤、预期/实际、1920×1080录屏和相关日志。
