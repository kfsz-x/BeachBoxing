# Original pause / settings / rename / keybind audit (2026-11-20)

Read-only shipped-data/WASM audit. No Unity Editor calls and no runtime behavior claim. Reproduce with `C:/Python314/python.exe docs/beach-boxing/evidence/extract-native-settings-2026-11-20.py`.

## Sources

- Original `data.unity3d`, SHA256 `3b109d92fcf4ced42ddf76c238e92d38a755d006d7413ea21bedc742aaf6cf67`.
- Prior complete object/button/label audit `peripheral-native-audit-2026-11-20.json`; this supplement retains the 135-node GO80 subtree.
- `native-dump/dump.cs` serialized metadata: KeybindsLoader193037, KeybindsManager193055, NPCSpawner193423, PlayerManagerSingleplayer193513, PlayerPrefSettings193663; TMP_InputField154583, Selectable183831, Slider184168.
- Individual annotated WAT files in `.superpowers/sdd/2026-11-20-peripherals-interactions/native-dump/`; newly resolved function slices retain `original.wat` line numbers in their first line and `settings-function-map.json`.
- AssetRipper export at `.superpowers/sdd/2026-11-20-peripherals-interactions/trainer-source-export/ExportedProject`. Its scene hierarchy IDs are intact, but MonoBehaviour custom fields are stripped there; the supplement decodes original raw payloads rather than trusting the empty YAML components.

## Original UI identity

| Role | level0 GameObject | Component / note |
|---|---:|---|
| Pause root | 80 | `SingleplayerPlayer/Canvas/MenuHolder/PAUSE MENU`, inactive |
| Settings tab | 1190 | `MENU`, active |
| Keybinds tab | 498 | KeybindsManager4703, inactive |
| Music slider | 166 | Slider4586, value Text593/TMP4416 |
| SFX slider | 705 | Slider4794, value Text733/TMP4438 |
| Name input | 458 | TMP_InputField4689; Text235/TMP4359; Placeholder983/TMP4472 |
| Close | 1089 | Animator3002 `Off`; PlayerManager5001.ExitMenu |
| Open keybinds | 857 | settings1190 false, keys498 true; legacy null target skipped |
| Return settings | 683 | settings1190 true, keys498 false; legacy null target skipped |
| Legacy Join Room | 757 | inactive child; no authored live entry in audited subtree |

All 135 original rects, Image references/colors, TMP strings and 14 Button callback lists are in `native-settings-2026-11-20.json`. This includes inactive legacy Join Room data, which should remain hidden. Source canvas55 uses reference800×600, width match0. Native 1920×1080 restoration should preserve this authored coordinate conversion instead of re-laying out its children.

## Volume and sensitivity

- Both sliders: **0..2**, left-to-right, continuous, authored0. First-run `FirstTimeInGame != 1` writes MusicVolume=1, SFXVolume=1, Sensitivity=1 and then guard1. Source `PlayerPrefSettings-Start.wat:43` onward.
- `OpenSettings` loads PlayerPrefs into both labels and sliders. Labels use format `0.0`. Slider callback is `UpdateSettings`, which schedules `ActuallyUpdateSettings` after0.03s; that stores **both** slider floats, updates both labels and applies the mixer. Source `PlayerPrefSettings-UpdateSettings.wat:17`, `ActuallyUpdateSettings.wat:24` onward.
- Exact dB curve: `v <= 1 ? -80 + 80 * Clamp01(v) : 20 * Clamp01(v - 1)`. Values0,0.5,1,1.5,2 map to−80,−40,0,10,20dB. Source `PlayerPrefSettings-MapVolumeToDecibels.wat:3`.
- The runtime calls mixer names `MusicVolume` and `SFXVolume`. Current imported `Main.mixer:18` instead exposes AssetRipper placeholders `mposwoH` (Music volume GUID eedc35...) and `tuRmmTJ` (SFX volume GUID306933...). This is caused by name-hash-only shipped mixer data. Rebind the exposed names or use those actual imported parameter names; do not silently call SetFloat on a missing parameter.
- **No sensitivity control** exists in this subtree and PlayerPrefSettings contains only two sliders. Complete original.wat address scan finds Sensitivity only twice, both in Start func50100 (initialization). Adding a sensitivity slider is a new behavior decision, not restoration.

## Rename

- Single-line, Standard/Normal input, CharacterValidation.None, **characterLimit0 (unlimited)**. No regex. End-edit has one persistent call: PlayerManager5001.TryChangeName. Submit/value-changed have no persistent calls. Source component4689 raw offset424 (limit),428 (EndEdit count),432 (target),492 (`TryChangeName`),584 (Submit count). No separate confirm button exists.
- `TryChangeName` delegates to NPCSpawner.CheckProfanity. The original293-entry profanity array is preserved in JSON `name_filter`; source NPCSpawner component4671.
- For the filter only: lowercase, remove ASCII spaces, substitute `0→o`, `4→a`, `1→i`, `5→s`, `3→e`, `z→s`, then substring-match every authored forbidden word. Match restores saved Name into the input and triggers profanityAlert `On`. Otherwise ChangeName saves the **original** input text to key `Name` and updates nameText. Sources `NPCSpawner-CheckProfanity.wat:68`,131,178,208; `PlayerManagerSingleplayer-ChangeName.wat:17`.
- There is no explicit nonempty/trim/length guard in these bodies. No PlayerPrefs.Save in ChangeName. First-start name generation uses GeneratedName guard and adjective+noun, then Start copies saved Name into the HUD/input (`PlayerManagerSingleplayer-Start.wat:313`).
- Caret blink0.85, width1, focus-select-all=true, reset-on-deactivation=true, restore-original-on-Escape=true. Raw input settings and colors are in JSON.

## Keybinds

| Index | Action | Default | Button | Value label | Listening BG |
|---:|---|---|---:|---:|---:|
| 0 | Forward | W | 358 | 118 | 35 |
| 1 | Left | A | 641 | 838 | 1041 |
| 2 | Backward | S | 1105 | 1192 | 240 |
| 3 | Right | D | 1212 | 241 | 254 |
| 4 | Jump | Space | 844 | 1188 | 459 |
| 5 | Menu | Escape | 920 | 110 | 447 |
| 6 | Shop | M | 439 | 583 | 763 |
| 7 | Locker | I | 809 | 1152 | 450 |
| 8 | Interact | E | 1183 | 822 | 773 |

- Storage `Keybind0`..`Keybind8`. InitializeKeybinds runs after0.1s and only writes defaults when FirstTimeStarted !=1. `KeybindsLoader-InitializeKeybinds.wat:47` onward.
- ChangeKey(index) sets keyIndex, shows selected listening BG, clears selected text to one ASCII space, sets picking=true (`KeybindsManager-ChangeKey.wat:14`).
- FixedUpdate checks picking, iterates Enum.GetValues(KeyCode), and uses **Input.GetKey held state** (table-slot4683 at line373), then picking=false, label=Enum.ToString, BG false, SetString and Save (slots4684/4685/4686). It has no duplicate/conflict filter, no special Escape cancel, and KeyCode includes mouse keys. Read-only function slice has850+ lines because exception invoke wrappers are retained, not omitted.
- KeybindsManager lives on GO498. Hiding it prevents FixedUpdate via GameObject inactivity, but the class has no OnDisable/reset method; an unfinished capture retains picking state for the next activation. **This is original behavior and should be preserved for source parity.** Hidden picking must not consume input until its GameObject is active again.
- **No restore-defaults button** among the complete52-Button level0 inventory or this135-node subtree; no reset method exists in KeybindsManager/Loader metadata. Defaults are first-run-only.
- Complete compiled reference scan per KeybindN is retained in JSON. Indices0..3 drive BoxingController/ThirdPerson movement. **Index4 is world Jump / workout EXIT**, not the training pulse: `WorkingOutManager-Update.wat:155`..174 reads Keybind4 with GetKeyDown, ORs hardcoded Escape27, then calls ExitWorkout. The training pulse uses hardcoded lowercase `e` through GetKeyDownString at line795..798, independently of Keybind4/8. Index5 opens/closes menus; index8 sends interact-down/up. **Indices6/7 only appear in first-run initialization**, so Shop/Locker are authored legacy labels without a compiled singleplayer consumer. Restore their rows and persistence without inventing actions.
- BoxingController reads remapped Keybind0..3 only for movement. **Fight blocking is hardcoded KeyCode.Space32**, not Keybind4: `BoxingController-Update.wat:261`..294 GetKeyDown32 enters Blocking;338..371 GetKeyUp32 exits. It also checks legacy `Fire2` and mobile PressBlock/ReleaseBlock flags. Thus rebinding Jump must not silently rebind fight blocking. Punching uses legacy `Fire1` plus mobile PressPunch, with no authored rebind row.

## Menu and input gate

TryToOpenMenu(0) rejects inFightMenu/menuOpen, activates GO80 then EnterMenu. EnterMenu disables movement, zeroes velocity, Walking=false/Grounded=true, and OpenSettings loads current values. It sets timeScale0 only while boxing; workout beach NPCs otherwise continue. ExitMenu triggers pause/trainer/daily `Off`, clears menuOpen, enables movement, restores boxing timeScale1 and enters commercial-ad/audio callbacks. Sources `PlayerManagerSingleplayer-{TryToOpenMenu,EnterMenu,ExitMenu}.wat`.

Close/open animation uses existing sharedassets0 controller385 and clips327/329:0.166667s close,0.333333s open. Name typing and rebinding must block reconstructed gameplay input through the existing flow gate; original recovered menu key body itself does not explicitly test InputField focus (`PlayerManagerSingleplayer-Update.wat:208`).

## Importer reuse recommendation

Extend the existing **editor-only** `BeachPeripheralRestore` source schema and `NativePeripheralUi.json` with the GO80 descendants and shared MenuHolder ancestor. Reuse its Image, OriginalSdfText, Selectable, masks, ScrollRect/Scrollbar, audio, sprite and animation paths. Add SliderData (fill/handle/range/value) and InputData (text/placeholder/viewport plus recovered settings), then author ordinary `UnityEngine.UI.InputField` using existing OriginalSdfText since this project restores TMP glyphs through a Text subclass. Serialize its input/slider references into the scene controller. No runtime AssetDatabase, Resources or texture loading is needed.

Before authoring callbacks, distinguish source-persistent presentation audio from reconstructed gameplay callbacks: RemoveAllListeners does not remove persistent listeners. Tabs swap1190/498. Preserve native pending-picking state while hiding its input consumer with GO498; don't introduce cancellation semantics, sensitivity controls or a default-reset button. Preserve fixed1920×1080 scene UI and imported art, retain original Name/volume/key storage independently of progression, and leave inactive Join Room unreachable.

## Remaining limits

No live first-run lifecycle, focus, keyboard release timing or audio gain observation was performed here. Native serialized values and compiled branches are established; editor-native UI parity and input safety still require the main thread's Unity validation. Face shader investigation is owned by the main thread and is outside this supplement.
