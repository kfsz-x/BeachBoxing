# Native full roster audit (2026-11-20)

Source: shipped `data.unity3d`; SHA-256 `3b109d92fcf4ced42ddf76c238e92d38a755d006d7413ea21bedc742aaf6cf67`. Static evidence only; no runtime or save mutation.

- `level0:4688 FightMenu`: all serialized fields decode exactly (2932/2932 bytes). Names/portraits/prestige: 46 entries; muscle stat arrays: 50. Index 45 is `COMING SOON`, with zero stats/prestige and no combat prefab.
- `level1:2954 CombatManager`: all fields decode exactly (744/744 bytes). Its 45 `enemies` references establish canonical opponent order. Sharedassets1 root prefab names equal indices 0..44.
- `sharedassets1.assets:37774..37818 EnemyManager`: 45 complete 540/540 byte decodes. JSON `roster` maps canonical indices to prefab IDs, component IDs, sharedassets0 portrait IDs, exact four stats, prestige, all AI fields and derived health/stamina/block capacities.
- Metadata: `native-dump/dump.cs:192701` EnemyManager, `:192887` FightMenu, `:192602` CombatManager. Serialized offsets are recorded per field in JSON components.
- WAT function evidence: `roster-function-map.json` records original.wat line and wasm function number; each selected method has a focused sibling WAT file. Reward and selection rules: `FightMenu-LoadOpponent.wat:42` hides fight at 45, `:96` sets 50*(index+1) gold and 3*(index+1) stars, `:492` clamps next navigation through interactable state; `FightMenu-OnEnable.wat:43` resumes CurOpponent; `BoxingController-Victory.wat:45` grants rewards and `:83` advances only when CombatOpponent==CurOpponent.
- Derived enemy stats: `EnemyManager-LoadMaxHealth.wat:17`, `LoadMaxStamina.wat:17`, `LoadMaxBlock.wat:17`: health=prefab.maxHealth+chest/20, stamina=prefab.maxStamina+legs/20, block=20+back/60; integer division. Back also modifies all three regeneration rates (see JSON formulas).
- Preserve source spelling: `Kyung  ` has two trailing spaces; Ramez rematches occur at both 18 and 44.

Open: visual prefab extraction/import and complete AI scheduling translation. Existing Temp AssetRipper export is absent; shipped bundle remains readable. Full AI tuning is exact and available; do not substitute guessed uniform behavior.
