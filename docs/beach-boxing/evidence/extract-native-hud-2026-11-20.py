"""Read-only native HUD extraction. Output goes to evidence, never Assets."""
import functools
import json
import pathlib
import re
import struct
import UnityPy

SOURCE = '.superpowers/sdd/2026-11-18-beach-boxing-scene-parity/source/data.unity3d'
env = UnityPy.load(SOURCE)
objects = {(o.assets_file.name, o.path_id): o for o in env.objects}
scripts = {o.path_id: o.read().m_Name for o in env.objects
           if o.type.name == 'MonoScript' and o.assets_file.name == 'globalgamemanagers.assets'}
gos = {o.path_id: o.read() for o in env.objects if o.type.name == 'GameObject' and o.assets_file.name == 'level0'}
transforms = {o.path_id: o.read() for o in env.objects
              if o.type.name in ('Transform', 'RectTransform') and o.assets_file.name == 'level0'}
go_transform = {t.m_GameObject.path_id: i for i, t in transforms.items()}

@functools.lru_cache(None)
def hierarchy(go):
    parent = transforms[go_transform[go]].m_Father.path_id
    return (hierarchy(transforms[parent].m_GameObject.path_id) + '/' if parent else '') + gos[go].m_Name

labels = {x['id']: x for x in json.loads(pathlib.Path(
    'docs/beach-boxing/evidence/native-tmp-label-paths-2026-11-19.json').read_text(encoding='utf8'))
    if x['asset'] == 'level0'}
rows = []
sprite_assets = {}
for go, game_object in gos.items():
    path = hierarchy(go)
    if not any(k in path for k in ['/TopLeft/', '/TopRight/', '/InteractButtonHolder', '/Hand Button',
                                   '/PrestigeButtonHolder', '/LVL UP BTN']):
        continue
    row = {'go': go, 'path': path, 'active': game_object.m_IsActive, 'components': []}
    transform = transforms[go_transform[go]]
    if hasattr(transform, 'm_AnchoredPosition'):
        row['rect'] = {k: {a: getattr(getattr(transform, k), a) for a in ('x', 'y')}
                       for k in ['m_AnchorMin', 'm_AnchorMax', 'm_AnchoredPosition', 'm_SizeDelta', 'm_Pivot']}
    for component in game_object.m_Component:
        obj = objects[('level0', component.component.path_id)]
        record = {'id': obj.path_id, 'type': obj.type.name}
        if obj.type.name == 'MonoBehaviour':
            raw = obj.get_raw_data()
            script = scripts.get(struct.unpack_from('<q', raw, 20)[0])
            record['script'] = script
            if script == 'Image':
                file_id, path_id = struct.unpack_from('<iq', raw, 88)
                asset_file = 'sharedassets0.assets' if file_id == 2 else 'level0'
                sprite = objects.get((asset_file, path_id))
                record.update({'spriteFile': asset_file, 'spriteId': path_id,
                               'spriteName': sprite.read().m_Name if sprite else None,
                               'color': struct.unpack_from('<4f', raw, 44),
                               'imageType': struct.unpack_from('<i', raw, 100)[0],
                               'fillMethod': struct.unpack_from('<i', raw, 112)[0],
                               'fillAmount': struct.unpack_from('<f', raw, 116)[0]})
                if sprite:
                    native_sprite = sprite.read()
                    texture = native_sprite.m_RD.texture.read()
                    sprite_assets[f'{asset_file}:{path_id}'] = {
                        'name': native_sprite.m_Name,
                        'textureId': native_sprite.m_RD.texture.path_id,
                        'texture': texture.m_Name,
                        'rect': {a: getattr(native_sprite.m_Rect, a) for a in ['x', 'y', 'width', 'height']},
                        'textureRect': {a: getattr(native_sprite.m_RD.textureRect, a) for a in ['x', 'y', 'width', 'height']},
                        'assetAlreadyImported': pathlib.Path(
                            f'Assets/BeachBoxing/OriginalArt/Resources/BeachOriginal/UI/{native_sprite.m_Name}.asset').exists()}
            elif script == 'TextMeshProUGUI':
                record['text'] = labels.get(obj.path_id, {}).get('text')
            elif script == 'Button':
                record['serializedStrings'] = [x.decode() for x in re.findall(rb'[\x20-\x7e]{4,}', raw[32:])]
        row['components'].append(record)
    rows.append(row)

out = {'source': SOURCE, 'method': 'UnityPy standard objects; stripped MonoBehaviour header and UI.Image raw offsets; no game runtime',
       'imageOffsets': {'spritePPtr': 88, 'imageType': 100, 'fillMethod': 112, 'fillAmount': 116}, 'ui': rows,
       'spriteAssets': sprite_assets}
manager = objects[('level0', 5006)].get_raw_data()
needle = struct.pack('<12i', 11, 0, 1000, 3000, 6000, 10000, 15000, 23000, 33000, 45000, 70000, 100000)
offset = manager.index(needle)
rank_offset = manager.index(struct.pack('<i', 6) + b'Rookie') - 4
rank_count = struct.unpack_from('<i', manager, rank_offset)[0]
assert rank_count == 10
cursor = rank_offset + 4
ranks = []
for _ in range(rank_count):
    length = struct.unpack_from('<i', manager, cursor)[0]
    cursor += 4
    ranks.append(manager[cursor:cursor + length].decode())
    cursor = (cursor + length + 3) & ~3
out['WorkingOutManager'] = {'id': 5006, 'thresholdArrayByteOffset': offset,
    'thresholds': list(struct.unpack_from('<11i', manager, offset + 4)),
    'rankStringArrayByteOffset': rank_offset, 'rankStrings': ranks,
    'note': 'Serialized configuration only; runtime field semantics/formulas require Wasm verification.'}
target = pathlib.Path('docs/beach-boxing/evidence/hud-module-audit-2026-11-20.json')
target.write_text(json.dumps(out, ensure_ascii=False, indent=2), encoding='utf8')
for row in rows:
    images = [r for r in row['components'] if r.get('script') == 'Image']
    if images:
        print(row['go'], row['path'], [(r['id'], r.get('spriteName'), r.get('imageType'), r.get('fillAmount')) for r in images])
