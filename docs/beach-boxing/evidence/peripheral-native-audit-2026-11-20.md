# Native peripheral audit — 2026-11-20

Read-only original-package audit. Product code/scenes were not changed. This report describes shipped static logic and authored assets; runtime verification remains a separate integration check.

## Evidence and repeatability

- **P01** `data.unity3d`: SHA256 `3b109d92fcf4ced42ddf76c238e92d38a755d006d7413ea21bedc742aaf6cf67`. Durable source under `.superpowers/sdd/2026-11-18-beach-boxing-scene-parity/source/`.
- **P02** `global-metadata.dat`: SHA256 `d9f63038225abc1111b8447725789358e96408bfd55a6acd60852cde3f3a2f3b`. Metadata version 31. REA parent evidence `ev_94b3d50db2815be6ea4d4b7abe6df9268df7674924f3ee5be917e33cc7d060e2`; portable export `peripheral-metadata-rea-2026-11-20.json`.
- **P03** Original WASM `D:/Desktop/Beach_Boxing_Simulator_100%原版包体/Build/a340f7614095398c51fd34d9096a725b.wasm.unityweb`: already raw WASM, 20,395,056 bytes, SHA256 `d4c8f849b087c734d5aed39f9eee79880e8c50c7cf280a882fe501541011ffce`.
- **P04** Il2CppDumper 6.7.46 + WABT 1.0.42 outputs under `.superpowers/sdd/2026-11-20-peripherals-interactions/native-dump/`. `script.json` SHA256 `321b7f1e92b8c1cfb12fa096bcc2f9bcfe072918c5f4aa941e5142d82b480882`; `dump.cs` SHA256 `7a3db8bd51b18449758791a5313a66636a398b982c020d310c4b0b6d50fb53b1`. Dumper completed all files before its expected redirected-input ReadKey exception.
- **P05** `peripheral-native-audit-2026-11-20.json`: 1,409 level0 GameObjects, all rect geometry, 52 Button persistent-call lists, 211 TMP string lists, Image sprites/colors/raw payloads, complete public field decoding for nine custom scene components, six Trainer prefabs, peripheral animation controllers/clips, and all-asset MonoBehaviour counts. Reproduce with `C:/Python314/python.exe docs/beach-boxing/evidence/extract-peripheral-native-audit.py`.
- WASM `script.json` method Address is an **element-table slot**, not a function index. Slot4856 -> func49860 is Daily ClaimReward. `peripheral-function-map.json` and individual annotated `.wat` files preserve the resolved mapping. Shared native bodies can have multiple managed aliases; annotations on generic/shared utility calls are informational.

## Reachable peripheral modules

| Module | Authored entry / target | Recovered behavior |
|---|---|---|
| Pause/settings/name/keybinds | `Canvas/TopRight/MenuButton`, TryToOpenMenu(0); panel GO80 | Music/SFX sliders, name input/change, keyboard rebind UI. Menu controls disable movement and stop Poki gameplay; boxing menus additionally pause time. |
| Personal trainers (shop) | `Canvas/TopRight/TrainerButton`, TryToOpenMenu(1); panel GO602, TrainerMenu4755 | Six selections, ownership, purchases with Stars, upgrades with Gold, equip/follower, four training bonuses, insufficient-funds animation. |
| Daily rewards | `Canvas/TopRight/Daily Rewards Button`, TryToOpenMenu(2); panel GO204, DailyRewards5008 | Six calendar-day unlocks, claims, claimed/claimable objects, top-right exclamation, reward-collected alert. |
| Level progression | `PrestigeButtonHolderHolder/.../PrestigeBTN (1)` -> activate GO679 and EnterMenu; native WorkingOutManager5006 | Ten titles/caps, four full indicators, level-ready button, level panel. See HUD specialist for exact fill/level formulas. |
| Auto training / x2 | Workout UI Toggle and `AutoTrain/ON/DoubleAutoTrain/X2 Button` | Auto training toggle plus rewarded-ad doubling for 120 seconds. |
| Tutorial/guidance | Tutorial5007; workout station transform1420, fightZone1583 | Tutorial line, movement prompts, workout/fight progression hints. |
| Currency/feedback | PlayerManager5001, WorkingOutManager5006; five UIParticle prefabs | Gold/Stars displays; gain particles and pickup SFX; workout shake, sweat, face/material changes; trainer follows/cheers. |

TryToOpenMenu rejects while `inFightMenu` or `menuOpen` is true. Entries use the same native menu controller. Shop/daily are authored active systems, not decorative buttons.

## Shop rules: exact native data and execution

TrainerMenu4755 raw payload consumes exactly 636 bytes. Native code: `TrainerMenu-{OnEnable,SelectTrainer,LoadLVLCost,LvlUp,EquipTrainer,PurchaseTrainer}.wat`.

| Index | Name | Purchase Stars | Base chest | Arms | Legs | Back | Icon sprite ID |
|---:|---|---:|---:|---:|---:|---:|---:|
| 0 | Cody | daily grant (-1 sentinel) | 5 | 5 | 5 | 5 | 514 |
| 1 | Dion | 50 | 10 | 10 | 10 | 10 | 515 |
| 2 | Nova | 200 | 30 | 0 | 0 | 30 | 505 |
| 3 | Zara | daily grant (-1 sentinel) | 10 | 40 | 40 | 10 | 438 |
| 4 | Serena | 500 | 50 | 30 | 50 | 20 | 403 |
| 5 | Yui | 900 | 50 | 50 | 50 | 50 | 489 |

- Ownership/level key: `Trainer{index}LVL`; default 0 means locked. Purchase checks Stars, subtracts cost, writes level1. Cost -1 writes level1 directly; SelectTrainer shows `Collect from Daily Rewards` instead of a purchase button for those slots.
- Purchased trainer different from `CurTrainer` shows EQUIP. Equipped trainer shows LVL UP. `CurTrainer` defaults -1; shop initial selection is equipped trainer, otherwise index0.
- Upgrade cost is **8 * current trainer level + 8 Gold**. It subtracts Gold and increments level. Native UI prints this cost with integer format `0`.
- Equip destroys the previous Trainer object, instantiates selected prefab beside the player, sets its target, writes `CurTrainer`, and writes `{Chest,Arms,Legs,Back}Modifier = corresponding base + trainer level`.
- Actual workout gain applies `gain *= 1 + modifier/100`, then converts to int by truncation (`WorkingOutManager-Update.wat`, around lines1210–1360). Trainer level therefore affects gains, not just display.
- Native inconsistency: SelectTrainer displays the base bonus for level<=1, but base+level for level>1; Equip writes base+level even at level1. Upgrade itself updates badge/scale and UI but does not rewrite the four Modifier keys; they refresh when equipping or next startup. Preserve this distinction when deciding parity versus correction.
- Trainer prefab IDs `[1190,972,1417,1332,1329,956]` in sharedassets0. Trainer UpdateTrainerLVL sets badge `LVL {n} Trainer` and uniform scale `1 + n*0.01`; movement/follow/cheer native function preserved separately.

## Daily rules: exact native execution

Native Daily Start func49861; ClaimReward func49860. Public payload consumes exactly308 bytes.

| Day / ClaimReward argument | Reward |
|---|---|
| 1 / 0 | Cody, sets Trainer0LVL=1 via PurchaseTrainer; TrainerToLookAt=1; shop exclamation enabled |
| 2 / 1 | +3000 Gold |
| 3 / 2 | +60 Stars |
| 4 / 3 | +10000 Gold |
| 5 / 4 | +100 Stars |
| 6 / 5 | Zara, sets Trainer3LVL=1 via PurchaseTrainer; shop exclamation enabled |

- Original framework `_GetLocalDate()` uses `new Date().toDateString()` in the browser's local timezone. No server time or UTC API is involved.
- On DailyRewards.Start, if today's string differs from `LastDailyReward` (default empty), increment `DailyRewardIndex` (default0), store today's string, and call PlayerPrefs.Save. Thus first visit unlocks day1; a visit on a different local date advances one step, regardless of elapsed dates or prior claims. This is not a streak reset or elapsed-days calculation.
- Day index i is eligible when `DailyRewardIndex > i`; `Collected{i}==1` shows CLAIMED and disables its button. Eligible unclaimed days show claimable objects and the top-right exclamation. Locked days have disabled buttons.
- ClaimReward triggers `rewardCollected` Animator `On`, saves `Collected{i}=1`, and grants the table reward. Each authored Button also plays AudioSource, hides Claimable, and shows Claimed. The method itself relies on UI gating; it has no duplicate/date guard inside its body.
- Shop and daily functions have no ad request. No ad should be invented as a purchase/claim requirement.

## Native UI assets and transitions

- Entry sprites: `TrainerButton`#409, `DailyRewardsBTN`#509, `Menu Button`#486; exclamation bubble `XButton`#435. Trainer icons named `TrainerIcon 5,6,4,2,3,1` in index order. All exact Image-to-sprite references and panel rects are in P05.
- Pause/shop/daily/level panels share AnimatorController sharedassets0#385 `PAUSE MENU`. MenuOpen#329 lasts0.333333s; MenuClose#327 lasts0.166667s and calls animation event `TurnOff`; MenuIdle#328 is zero-length. Closing goes through Animator `Off`; activating the panel triggers native opening state.
- Daily reward/insufficient-funds alerts use `FreeLookAlert`#378 with TextAlert#344 (2.166667s) and idle#345. Text/transform animation curves are preserved in P05.
- All TMP authored labels, title/currency formatting, button callback arguments, colors, and geometry are recoverable. Static placeholder LVL100 and +15% labels must be replaced by recovered runtime values.

## Ads, settings, and storage

- `WorkingOutManager.TryDoubleAutoTrain` requests Poki rewardedBreak and pauses AudioListener. Completion bool true sets doubleAutoTrain, initial counter121 then immediately decrements to120; one-second Invoke countdown ends at0 and restores the x2 button. False grants no multiplier. This state is not stored in PlayerPrefs.
- Despite the x2 label, the native idle workoutSpeed interpolation chooses target1.3 while doubleAutoTrain is true versus0.5 otherwise (16*deltaTime touch, 25*deltaTime desktop). It does not simply multiply strength gain by2; preserve the actual speed branch.
- `WorkingOutManager.timeBeforeAd` is serialized0. During an active workout it decreases by deltaTime; below0 it resets180 seconds and calls PlayerManager.WatchAd commercialBreak. Menus also use commercialBreak on exit and audio/movement restoration callbacks; these are commercial ads, not currency rewards.
- FirstTimeInGame initializes Sensitivity/MusicVolume/SFXVolume to1 and legacy locker keys. Settings public payload92 bytes consumed exactly. SFX/Music sliders persist float keys and display one decimal. `MapVolumeToDecibels` maps [0,1] linearly to[-80,0], [1,2] linearly to[0,20], clamped within each segment.
- Additional observed progression keys: `Gold`, `Stars`, `ChestStrength`, `ArmsStrength`, `BackStrength`, `LegsStrength`, `PrestigeLVL`, `CompletedTutorial`; name keys `Name`, `GeneratedName`; keyboard keys `Keybind0..8`; menu/ad key `FirstMove`.

## Bounded absence / dormant leftovers

Complete UnityFS MonoBehaviour inventory (all asset files) finds **zero instances** of LockerManager, LootManager, Potion, GotChestAlready, and GameManager, although their names/methods remain in metadata. PlayerManager5001.lockerMan is null; its six teleportPositions are all null. A LockerViewCamera and stale locker keybind label alone do not establish a working inventory module.

The pause prefab contains a Join Room PANEL callback `PlayerManager.JoinNewLobby`. The stripped Assembly-CSharp metadata inventory has PlayerManagerSingleplayer but no PlayerManager type. Treat this as stale multiplayer UI, not evidence to recreate networking. No working multiplayer flow was recovered.

## Remaining limits and integration acceptance

- Static recovery establishes stored data, authored bindings, and native branch logic. It does not prove platform ad success, timing under an actual SDK, or visual appearance after reconstruction.
- Reachable trainer follow/cheer state, menu transitions, daily indicators, settings, key rebind, level/cap transitions, workout x2/countdown, and currency/strength gain effects all need end-to-end checks in the reconstructed build.
- Current repository bounded source check: ProgressStore.PlayerProgress presently stores only four muscles, coins/stars, and unlockedOpponent; no trainer/daily/settings state. BeachHud and BeachGameFlow have core workout/fight/pause flow. Shop/daily require new implementation and persistence, rather than merely reconnecting existing buttons.
- UI geometry/hierarchy and all nine public component payloads passed complete byte-consumption verification. Native formulas above were crosschecked against individual WAT functions and source data; unrecovered runtime behavior is not filled with invented rules.
