# Original presentation timeline evidence

Source package: `Beach_Boxing_Simulator_100%原版包体`, Unity 2022.3.34f1 WebGL. Target: Unity 2021.3.14f1c1 Built-in. The companion `presentation-timeline.json` records original animation keys, incoming/outgoing tangents, source paths, hashes and events.

## Opening

`CombatManager.Start` (WASM function 49865) and `BoxingController.Start` (49914) both invoke their cutscene exit after **4.5 scaled seconds**. `ExitCutscene` (49903) enables the player Canvas and clears `inCutscene`. `Fight.anim` lasts 4.5 seconds:

Opening CanvasScaler 2948 has reference resolution **800×600** and matchWidthOrHeight **0**. A separate preplaced opening Canvas preserves this behavior instead of inheriting the main HUD's different scale match.

| Target | Enters | Overshoot | Settles | Begins exit | Hidden |
| --- | ---: | ---: | ---: | ---: | ---: |
| 3 | 0.5 | 0.5833333 | 0.6666667 | 1.25 | 1.4166666 |
| 2 | 1.5 | 1.5833334 | 1.6666666 | 2.25 | 2.4166667 |
| 1 | 2.5 | 2.5833333 | 2.6666667 | 3.25 | 3.4166667 |
| FIGHT | 3.5 | 3.5833333 | 3.6666667 | 4.3333335 | 4.5 |

The numbers use `FadeCircle` (scale 3.622306, anchored y=-6.100052) behind `AllAxis_Ridged` (scale 1.4710771). The glow/circle colors progress from orange 3 (1/.215686277/0/1 and 1/.214346379/0/1), through amber 2 (1/.643137276/0/1 and 1/.644330204/0/1), to yellow 1 (.980392158/1/0/1 and .980179906/1/0/1). Number text is original Lilita size 115.78. FIGHT text is `FIGHT!`, size 96.6, with green versions of the same art and a black `FadeCircle` stretched by 11.198575/2.1698387/1.4710771. All these fields come from original level1 MonoBehaviour bytes and RectTransforms, not screenshot estimates.

## Results and time scale

`EnemyManager.Die` (28135) sets `Time.timeScale = 0.1` before calling `BoxingController.Victory` (28150). `BoxingController.Die` (28146) also sets 0.1. Both activate the corresponding result Canvas immediately. **Victory itself does not change time scale**; direct diagnostic `SendMessage("Victory")` therefore differs from a real knockout.

**Correction from native replay and complete WASM call-site analysis:** 0.1 is the initial scale, not a persistent scale. `EnemyManager.Update` (function 49842) checks its dead flag at native offset 312. When true, it runs `Time.timeScale = Mathf.Lerp(Time.timeScale, 1, Time.deltaTime)` and returns. The `deltaTime` wrapper is function 1396 and its actual icall string is `UnityEngine.Time::get_deltaTime()`. Enemy Die writes this flag itself. Player Die calls enemy function 28145, which writes the same flag and clears movement/blocking, so both outcomes recover time scale each frame. The earlier constant-0.1 conclusion and its regression expectation are withdrawn.

The replica computes the frame's scaled seconds using the previous time scale, advances clip time with those seconds, then applies the source Lerp for the next frame. At 60 fps this gives clip times approximately .1570372 at 1 wall second and 2.7443643 at 5 wall seconds, with scales .2309624 and .9430571. Frame ordering between original scripts and Unity's native animation subsystem is not fully observable from static evidence; this is the explicitly chosen ordering.

Combat scene Animators 1928 and 1943 both use updateMode 0 (Normal). Their shared `FIGHT MENU.controller` has one state, speed 1, no active speed parameter. `VictoryScreen.anim` has these **clip-clock**, not real-clock, milestones:

- Root stays zero until 1.3333334, reaches 1.1 at 1.55 and 1 at 1.65.
- `MENU/Title`: re-enters at 1.8, settles at 2.1166666.
- `MENU/MiddleBit`: re-enters at 2.2166667, settles at 2.6166666.
- `MENU/AcceptBTN` and `MENU/HomeButton`: re-enter at 2.7166667, settle at 3.05.
- The clip lasts 3.65. Victory schedules particles at 2 scaled seconds.

Original AudioPlay bindings: opening MonoBehaviour 2949 → AudioSource 1918 (`RaceStartSFX`, volume .03400000185, event .11666667); victory 3130 → 1915 (`Success 7`, volume .2619999945, event 1.3166667); defeat 2921 → 1910 (`Debuff Downgrade 37`, volume .4250000119, same result event). The Stage's repeated countdown sound and immediate victory sound/particles have been replaced with clock-triggered entry points. Defeat's original audio is recovered as PCM `OriginalArt/Resources/BeachOriginal/Audio/Defeat.wav` (SHA256 `8b3603f9b37757422ff8313868f7b60e400afb904be27c07bff3bbb151a9a70a`) and bound by migration to appended sound slot 6.

The implementation must keep result clip time and global time scale separate. Browser observation remains a useful cross-check, especially a **real enemy knockout**, rather than invoking Victory alone. No artificial result delay should be added before the original root curve starts.

Sharing the controller does **not** mean every child curve binds in both layouts. Native Defeat hierarchy has `MENU/Titled`, `MENU/AcceptBTNd`, and `MENU/BallsButton`; their CRCs do not match the clip's `MENU/Title`, `MENU/AcceptBTN`, or `MENU/HomeButton`. There is no MiddleBit. Thus only the root curve animates Defeat: title/buttons arrive together with the root, which settles at 1.65 scaled seconds (roughly 3.8 wall seconds under the recovering scale). This explains the complete Defeat screenshot at 5 seconds. Native Victory matches root/title/MiddleBit/AcceptBTN; its HomeButton path is absent too. Migration now reproduces these actual bindings rather than forcing every curve onto both screens.

Raw native clips, controllers, hierarchy/CRC mappings and recovery evidence are preserved under `native-presentation/`. The opening clip's four bindings all match, all source children are active/enabled, and native streamed key times agree with the exported 4.5-second clip. Its controller node's `m_Duration=1` is observed across every inspected controller and is **not** evidence by itself for changing playback speed. **Resolved by the main thread's dense replay** in `native-presentation-dense2/`: 3/2/1/FIGHT all appear in the source. Nominal screenshot filenames hid capture drift; `capture.json` actual elapsed times are authoritative. No animation-speed adjustment was needed.

## Opening camera

The native scene's Animator 1937 is on `CombatManager/CameraPivot` (Transform 1129). Its CameraPivot controller plays `sharedassets1.assets` clip 94, `EntranceCameraAnim`, with speed 1, no parameters/behaviours/events. The clip lasts 4.5833335 seconds; the source's cutscene exit switches away at 4.5 seconds. Its three native bindings are child `Camera` position (CRC 1018227507), root Euler rotation (CRC 0), and child Camera Euler rotation.

Original CombatManager position (-41.6091232, 9.4101515, -77.4336090) plus CameraPivot local position (59.9091225, -4.2501516, 71.4636078) gives pivot world position (18.2999992, 5.15999985, -5.97000122). Initial child Camera position is (0, 26.3, -32.3), with pitch 45° and FOV 60. The current original CombatWorld normalization maps that first pose exactly to the replica's former static offset (1.5308, 8.74336, -11.87972).

Nine scalar native streamed cubic tracks are retained in `presentation-timeline.json` under `introCamera`; their key coefficients evaluate as `((a*dt+b)*dt+c)*dt+d`, without approximating tangents. Pivot yaw follows the source cubic from 0 to 225° at 2.5 seconds and holds. Camera local height drops 26.3 → 15.5 at 2.5 seconds → 9.2 at 4.35; local z moves -32.3 → -28.3 → 1.6. Child pitch drops from 45° to 14.105° at 4.35. Native clip and hierarchy are preserved as `EntranceCameraAnim-native.json` and `camera-hierarchy-1937.json`.

Migration serializes these tracks against the existing camera and original combat-world transform. Runtime composes root orbit with child position/rotation, then applies the world's existing normalization. GameFlow samples the intro after the ordinary Stage framing only while the fight countdown is positive; live fight framing remains unchanged. No new camera or runtime visual object is created.

## Fades

Original `fffff.anim` fades alpha 0 to 1 by 0.16666667 seconds and holds to 0.93333334. `FadeIn_.anim` fades alpha 1 to 0 over 0.5 seconds. `ActuallyGoBackToWorkout` (49909) and `ActuallyFightAgain` (49906) activate fade-out and invoke the loader after 0.18 scaled seconds. `GoBackToWorkout` (49910) restores global time scale to 1 before this transition.

## Implementation plan and ownership

Use a pure presentation clock and serialized per-axis AnimationCurves sampled onto existing scene transforms. An explicit Editor migration preplaces the original circle/glow/banner group and full-screen fade, binds existing result pieces, and copies only missing original sprite dependencies. Runtime creates no visual objects. Curve normalization preserves each existing result piece's resting scene scale.

1. Add runnable failing EditMode tests for opening unlock, independent result clock/time scale, pause preservation, 0.18-second transitions and home reset; main thread runs RED. Add source-event regression tests for one countdown sound, scaled result sound and delayed victory particles.
2. Implement the pure clock and serialized view; connect GameFlow transitions and time-scale restoration. Replace Stage's immediate/repeated presentation effects with explicitly timed entry points, preserving combat-impact feedback and all lighting behavior.
3. Main thread changes FightController's countdown initial value to 4.5 and runs the explicit migration, then focused EditMode and existing PlayMode suites.
4. Main thread verifies original/replica opening, actual knockout, result entry, retry/home and pause-resume. Main thread owns Editor, HUD, Stage and scene changes. No build or commit.

One alternative was a hand-authored replacement animation; it cannot preserve original tangents. Another was importing legacy Animators and their MonoBehaviour animation events wholesale; their missing components and changed result paths make that fragile. Sampling source curves onto bound targets retains original motion with a small, inspectable runtime surface.

## Verification recorded by the implementation agent

- First five clock tests: compiled/runnable RED (all expected assertion failures, class absent); main thread independently confirmed the same five failures in Editor job `ebdf886ff4cf4cfdb811ccc7760e1411`.
- Three event tests: compiled/runnable RED (three expected missing-event assertions), then GREEN.
- Fresh standalone Mono/csc compilation of all current BeachBoxing Runtime C# files and the migration helper: no errors. Uses the target Unity installation's managed references and existing UnityCore/UI assemblies; output goes only to the external reference temp directory.
- All eight focused NUnit tests pass against that freshly compiled actual Runtime assembly.
- Scene migration, native-font migration, complete Editor suites and browser/Editor visual comparison remain the main thread's responsibility. The implementation agent did not operate Unity Editor, save a scene, build or commit.

Integration follow-up: the main thread observed `MissingComponentException` at `openingCanvas.renderMode`. Unity's missing-component wrapper can compare null through Unity's operator while remaining non-null to C# `??`; all Unity-object `??` and `?.` uses in this migration were replaced with explicit Unity truth checks. Reapplying result bindings now reads each previously bound target's rest scale from the serialized curve's final values, rather than absorbing a preview's intermediate transform scale. Existing zero axes are repaired independently. Runtime remains absolute curve sampling with a reset result clock, so repeated results do not multiply prior scales. Fresh standalone compilation and all eight clock/event tests pass after this follow-up; main thread should verify two consecutive Apply calls and interrupted-result → Home/Retry → next-result in Editor.

Recovery correction verification: replaced the constant-scale expectation with source-derived 60 fps recovery values and added pause-during-recovery coverage. Both new expectations failed against the old implementation (`expected .1570372`, `actual .09999994`), then all **nine** focused tests passed after per-frame recovery. Fresh full Runtime and migration-helper compilation passed. Scene Apply and updated full Editor/Play suites are still owned by the main thread.

Opening camera verification: two focused native-cubic tests first failed because the sampler was absent, then passed against its production implementation (nonlinear yaw, position-segment changes and boundary clamping). Fresh full Runtime and migration-helper compilation passed; all **eleven** focused tests pass. Main thread still owns scene Apply and visual orbit comparison.
