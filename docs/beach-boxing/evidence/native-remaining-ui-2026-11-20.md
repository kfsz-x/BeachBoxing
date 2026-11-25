# Remaining native UI extraction (2026-11-20)

`native-remaining-ui-2026-11-20.json` is a **merged editor import source**: original360 nodes plus280 newly covered nodes,640 total. Product Assets/scenes/runtime code are untouched. New assets are staged under `.superpowers/sdd/2026-11-20-remaining-native-parity/workspace/ui-art-staging/`.

Reproduce with `C:/Python314/python.exe docs/beach-boxing/evidence/extract-native-remaining-ui-2026-11-20.py`. It reads the original UnityFS data and existing DummyDll field metadata, with UnityPy TypeTreeGenerator for Unity2022.3.34f1. All supported UI component bodies pass `check_read=True` (complete serialized byte consumption). Native MonoBehaviour header identity comes from the original type tree, because the generated body tree omits the editor-hideflags header field.

| Requested root | Source GO | Complete descendants including root |
|---|---:|---:|
| PAUSE MENU | 80 | 135 |
| WORKING OUT | 881 | 69 |
| FIGHT MENU | 457 | 71 |
| KeepTrainingTip | 372 | 6 |
| Move Tutorial | 601 | 3 |
| ProfanityDetected | 933 | 4 |
| Fight-only Home (level1 composite IDs) | 1000517 | 5 |

Move Tutorial601 is recovered from ThirdPersonCharacterControllerSingleplayer component5003.`moveTutorial`, rather than a name guess. Necessary canvas ancestors and portrait pose markers are included. Nodes are ordered by original RectTransform child order, parent before children; `sourceSiblingIndex` preserves the absolute original index. Existing360 nodes have **zero differing pre-existing DTO fields**.

## Schema

Existing `canvasId`, canvas settings, `nodes`, `sprites`, `portraits`, `animations` and `unsupported` retain the current NativePeripheralUi/BeachPeripheralRestore shape. Original art paths remain under `Assets/BeachBoxing/OriginalArt/UI/NativeSprites/` or `NativeAudio/`, ready for the main thread to copy staged files. `sourceEvidence.stagedAssets` maps each destination to its stagingPath and SHA256.

Existing Image/Label/Selectable/Audio fields are retained. Labels use actual TMP **m_fontColor**, **m_fontAsset**, fractional **m_fontSize** and authored min/max sizes. `sourceGraphicColor` separately preserves TMP's inherited Graphic color; it must not replace m_fontColor. Source material ID, fontStyle, richText, margins and overflow are also retained. Fonts used by202 text nodes are3373 (plain,144 nodes) and3372 (outline,58 nodes). Input component's global font3375 is distinct from its actual child Text label font; use the child LabelData for rendering.

New node fields for importer extension:

- `slider`: SelectableData fields + `fillId`, `handleId`, `direction`, `minValue`, `maxValue`, `wholeNumbers`, `value`, `persistentCalls`. Two sliders, both0..2; source component IDs4586/4794.
- `input`: SelectableData fields + `viewportId`, `textId`, `placeholderId`, `verticalBarId`, content/input/keyboard/line enum values, character validation/limit, text, caret/selection colors, blink/width, focus/Escape/deactivation flags, scroll sensitivity, global point size/font ID, and exact `endEditCalls`, `submitCalls`, `valueChangedCalls`. Includes name4689 and inactive legacy JoinRoom4907.
- `rectMask`: componentId/enabled + exact `padding` and `softness` (two input text-area masks).
- `animator`: source componentId/enabled + sourceControllerId.
- `layoutElement`: source componentId/enabled + complete sourceData.
- `eventTriggers`: exact native pointer events/callback payloads. Existing `pointerAudioId` and `clickAudioId` are also populated and dependency-checked.
- ButtonData now adds the exact authored `persistentCalls`; it does not authorize connecting missing legacy targets. Null targets are preserved as evidence.

Counts:264 Images,202 labels,50 Buttons,13 AudioSources,2 Sliders,2 InputFields,2 ScrollRects,2 Scrollbars,2 Masks,2 RectMasks,24 Animators.

## Sprite reconstruction

64 sprite records reuse41 current PNGs and add23 PNGs. New PNGs restore the complete source `m_Rect` canvas, including transparent margins. UnityPy's packed sprite extraction handles atlas rotation; trim is pasted at `(textureRectOffset.x, rect.height - textureRectOffset.y - trim.height)`. PPU, normalized pivot and border remain source values; meshType1 matches the existing import pattern.

The same reconstruction method was applied read-only to all41 existing NativeSprites and compared against their current PNG pixels: **41/41 exact matches**. New staged PNG dimensions, trim dimensions, offsets, canvas paste positions and hashes are recorded on SpriteData and stagedAssets. Example source405 restores100×100 from60×72 at(20,14);483 restores256×256 from69×239 at(182,8). Cropping those margins would visibly change native placement/scaling.

One new WAV is needed: AudioClip355, `native-ui-355.wav` (used by five native sources). It is staged once and referenced by all sources. Source mixer-group IDs and exact volume/pitch/loop/playOnAwake values are preserved.

## Animations and original controllers

The existing `animations` DTO list is unchanged so the current importer remains readable. The complete native feedback evidence is reused in these extra properties:

- `sourceAnimatorBindings`: exact bindings for included nodes.
- `sourceControllers`: complete source controller YAML documents, indexed by controller name.
- `sourceClips`: complete native clip curves/settings/events from native-feedback evidence.
- `hashedAttributeMap`: recovered Image/TMP field hashes, including m_fontColor and m_fontSize.

The main thread can consume flattened native-ui-motion-2026-11-20.json and BeachRemainingUiRestore.Clip(id)/MotionBindings() for the remaining animator bindings. Do not feed TMP/Image/RectTransform float tracks through the old Transform-only ImportClip fallback: original classID114 colors/font-size and classID224 anchored-position tracks require their appropriate restored targets. Full source tracks are retained without guessing a runtime remapping.

## Validation and bounded limitation

`sourceEvidence.verification` records unique IDs, parent-before-child order, component reference closure, zero old DTO differences and41/41 sprite equality. All referenced slider/input/scroll/selectable/audio targets are included. All nonzero source Image sprite pointers resolve to an art path.

Custom gameplay component FightMenu4688 could not be read by the generated public serialized body tree (`read_int_array out of bounds`). Its exact2932 raw bytes and original header are preserved in componentEvidence; this does **not** affect its71-node UI extraction or any supported UI component decoding. Existing custom behavior classes remain listed as unsupported/separately handled rather than silently instantiated.

No Unity Editor calls, runtime art loading, build, save clearing or product edits were performed.

Task4 supplement: source933 and combat-only Home were added after the initial631 extraction. Combat Home carries original level1 IDs under a1000000 offset; the source remains in fightContext and sourceAsset fields. Music/SFX poses and combat PauseAnimator1924 Unscaled2 are separately recorded. Level0 default-motion source bindings now total25 (old24 plus933).
