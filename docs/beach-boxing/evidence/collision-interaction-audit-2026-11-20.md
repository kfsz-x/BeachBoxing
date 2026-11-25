# Collision and interaction audit — 2026-11-20

Read-only product audit. Native UnityPy serialization, native IL2CPP metadata v31, shipped WASM disassembly, and current saved scene were inspected. No Unity Editor, browser or server was used. JSON alongside this document contains complete collider records, hashes, metadata fields and native WABT disassembly.

## Confirmed causes

1. **All 40 current BoxCollider sizes are wrong.** They all serialize `m_Size: {x: 2,y: 2,z: 2}`. Native level0 contains35 BoxColliders and level1 contains9. Current scene has35 Workout and5 Combat boxes;4 native Combat InvisWalls boxes outside LEVEL0 were omitted by world-root import. Cube colliders predominantly use `(1,1,1)`. Native ring rope Sprite colliders use `(16.0130978,31.0947933,.19999997)`, with center `(-5.3065534,10.4209499,.00000572)`. Centers were preserved; size was not. This doubles all floor/platform and ring-base boxes and collapses rope barriers into different shapes. Restore source-specific sizes; do not halve every box blindly.
2. **Machine interaction radius is less than half native.** Current `BeachGameFlow.LocateInteraction` lines158–170 uses distance from player root to station root `<1.6`. Native PlayerManagerSingleplayer.FixedUpdate method20342 / function50114 performs overlap sphere radius1 native (=.356 normalized), center `feet.position + Vector3.up`; feet transform1953 is localY .06099796, so normalized query offset is `.3777153` above root. Target native spheres radius15 with machine worldScale .64645 give normalized radius3.45204, effective query distance about3.80804, with exact vertical geometry. Native mask is Interactable layer6. Native chooses first valid enabled Interactable from results, then activates its `interactableObject` child GlowRing.
3. **Machine proximity glow is never managed by the replacement flow.** Native base Interactable bool + PPtr directly identify Chest/Arms/Back/Legs GlowRing objects681/317/231/275, initially inactive; proximity toggles them on/off. Current Stage has only machinePivot toggling for exercise; no GlowRing selection/toggling. Keep source GlowRing transform/materials, and toggle selection rather than making every glow always visible.
4. **Ring entry should be automatic.** Native glow GO379/transform1583/collider3051 has tag FightMenu and enabled trigger CapsuleCollider. PlayerManagerSingleplayer.OnTriggerEnter method20346, token0x0600013f, tableSlot5007, WASM function50112, file offsets0xe7be1c–0xe7be49: CompareTag(FightMenu); if !menuOpen, set inFightMenu=true and fightMenu.SetActive(true). There is no E condition. Current `BeachGameFlow.Interact` lines173–180 requires an explicit interaction. Native ring effective capsule radius1.771545 normalized; native player capsule radius .267 also contributes to overlap.

## Exact blocking floor mapping

World positions already match native within rounding. Sizes differ.

| Native collider ID | Current component / scene line | Object | Native normalized AABB extents | Current extents |
|---|---|---|---|---|
|level0:2977|546546278 /43684|Boxing Area/Cube (3)|(14.738462,.181039,8.605154)|(29.476923,.362078,17.210307)|
|level0:2951|738789925 /59494|Boxing Area/Cube (1)|(14.341083,.181039,8.243076)|(28.682165,.362078,16.486152)|
|level0:2959|1034038000 /83535|Boxing Area/Cube (2)|(13.943700,.181039,7.880997)|(27.887398,.362078,15.761994)|

All three center approximately `(.49816,-3.99/-3.91/-3.81,-45.22262)`. Lowest native floor front is z=-36.61747; current front is z=-28.01231. Native top is y=-3.80787; current top is y=-3.62683. This changes the physical approach well before the ring glow at z=-39.30239. Current platform overlap can create starting-overlap casts; correcting only movement code would preserve wrong collision geometry.

Machine mapping: Chest native collider3044→current463458832 (scene33871), Arms3045→924939620 (74056), Back3048→1669943091 (122426), Legs3049→373920229 (29293). Their SphereCollider radii are preserved. Ring trigger3051→1480927321 (109353) is also geometrically preserved.

## Native layers and movement

Native TagManager: Ground3, Interactable6, Player7, NPC8. PhysicsManager pathID10: gravity `(0,-80,0)`, contactOffset .01, Player7 collision mask0xfffffeff excludes only NPC8; Ground3 collides. Current DefaultRaycastLayers query includes NPC8 and excludes IgnoreRaycast2. Thus a hypothesis that native ignores all Ground3 boxes is false. Preserve relevant source layers and masks; do not disable floor colliders as a workaround.

Native player capsule3052, GO520/transform1647: radius .5, height1, transformed scale `(1.5,5.01376,1.5)`; actual native radius .75, worldHeight5.01376. Normalized radius .267. Current custom motor capsule constants match this basic geometry. Native controller uses dynamic Rigidbody2943, gravity and contacts; custom casts plus GroundPlayer are an approximation. If a route remains blocked after source box restoration, log first capsulecast hit collider, point, normal and distance. Terrain-wide `bounds.max.y` in BeachMotor.Translate lines31–37 cannot classify local slope height; a first ground hit may be mistaken for a wall. This remains a candidate, not a verified blocker in this read-only audit.

## Smallest justified patch

- Restore all40 existing world BoxCollider sizes from native level0/level1 records, retaining per-object center, enabled state, trigger flag and layer. Match full native hierarchy and sibling identity; moved Terrain/Cube is a deliberate current hierarchy exception. Correct original world prefab data too if future placement uses it.
- Replace hardcoded machine radius with native collider overlap geometry at normalized native query center/radius. Toggle exactly one GlowRing; hide on exercise/menu/leave. Keep native E/touch workout action.
- Detect ring player-capsule/entry-trigger overlap and open opponent menu on entry. No machine Interact button for ring. Track leave/re-enter to prevent immediate reopening after cancel while standing inside.
- Re-run physical route regression before considering motor changes.

## Meaningful regression

Start at saved beach spawn using Home/EnterBeach. Drive normal input through BeachStage.WalkPlayer / real joystick or keyboard each frame; never SetPosition/Teleport to a target. Record trajectory, any stationary-input interval, first cast obstacle, selected machine/glow, HUD button and phase.

1. Walk from spawn to Arms, then leave its range: near native overlap boundary only Arms glow + green Interact E should appear; E/touch starts exercise; exit returns native exit marker; glow/button disappear outside.
2. Repeat Chest/Back/Legs via walkable routes, including platform edges. Verify collision boundaries against source sizes and no false glow from remote station roots.
3. Walk from spawn down center route to ring, crossing native floor edge. Without E or touch interaction, opponent selection must open only after player capsule overlaps native FightMenu capsule.
4. Cancel selection, walk outside trigger, re-enter: opens once again. Merely cancelling while inside should not loop-open.
5. Approach actual ring barriers and decor: actor must still collide; NPC8 bodies should not become movement walls if native mask is applied.

Existing tests that teleport directly to stations/ring prove state transitions but cannot establish route accessibility.

## Native interaction button evidence

`nativeInteractButtonHierarchy` in JSON preserves HolderGO417/T3876, ButtonGO1057/T4222 and descendants with exact serialized rects and raw MonoBehaviour bytes. Holder center `(-15.014736,-130.399994)`, scale1.2, size196.4004; Button size70x70 and scale2.80572, initially inactive. Main Image4944, Button4943, EventTrigger4942, Animator3033. Portrait targetT3675 center `(-15.014735,-367)`, landscapeT4065 center `(-15.014735,-130.4)`, bothscale1.2. Main TMP4502 and extraTMP4522 supplied; native font profile already identifies main source path in `Assets/BeachBoxing/OriginalFont/LilitaOneNativeUiProfiles.json:87`. Current `BeachOriginalHudRestore.cs` has no interaction-button restoration; inspect/restore this existing bound HUD element to match the native green button and E hint. This is a separate presentation mismatch from proximity detection.
