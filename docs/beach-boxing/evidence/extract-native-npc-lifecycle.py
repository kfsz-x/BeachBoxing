"""Derive the editor bind payload from the native NPC-only UnityFS records."""
from pathlib import Path
import json
import struct

ROOT = Path(__file__).resolve().parents[3]
HERE = ROOT / 'docs/beach-boxing/evidence'
PLAN = ROOT / '.superpowers/sdd/2026-11-20-remaining-native-parity'
source_file = HERE / 'extract-native-remaining-ui-2026-11-20.py'
# Use the same complete type-tree decoder as the UI extraction.
__file__ = str(source_file)
exec(compile(source_file.read_text(encoding='utf-8').split('\nmove=decode')[0], str(source_file), 'exec'))
motion = json.loads((HERE / 'native-motion-npc-2026-11-20.json').read_text(encoding='utf-8'))

shared = 'sharedassets0.assets'
actor = next(a for a in motion['actors'] if a['class'] == 'NPC')
muscle_manager = next(c for n in actor['nodes'] for c in n['components'] if c.get('class') == 'MuscleManager')
transforms = {n['transformId']: n for n in actor['nodes']}
components = {c['id']: n for n in actor['nodes'] for c in n['components']}
def path_occurrences(transform_id):
    result = []
    while transform_id != 2031:
        t = idx[shared, transform_id].read()
        parent_id = t.m_Father.path_id
        parent = idx[shared, parent_id].read()
        name = idx[shared, t.m_GameObject.path_id].read().m_Name
        siblings = [r.path_id for r in parent.m_Children
                    if idx[shared, idx[shared, r.path_id].read().m_GameObject.path_id].read().m_Name == name]
        result.append(siblings.index(transform_id))
        transform_id = parent_id
    return result[::-1]

muscles = []
for group, field in [('Chest', 'torsoMuscles'), ('Arms', 'armMuscles'), ('Back', 'backMuscles'), ('Legs', 'legMuscles')]:
    for ref in muscle_manager['decoded']['fields'][field]['value']:
        component_id = ref['path_id']
        raw = idx[shared, component_id].get_raw_data()
        file_id, start_id = struct.unpack_from('<iq', raw, 32)
        assert len(raw) == 84 and file_id == 0 and not any(raw[44:])
        end_node = components[component_id]
        start_node = transforms[start_id]
        def pose(node):
            return {field: node['local' + field.title()] for field in ('position', 'rotation', 'scale')}
        muscles.append(dict(group=group, target=end_node['path'][4:], componentId=component_id,
                            startTransformId=start_id, occurrences=path_occurrences(end_node['transformId']),
                            start=pose(start_node), end=pose(end_node)))
assert len(muscles) == 42

spawner = next(c for c in motion['components'] if c['class'] == 'NPCSpawner')
data = {k: spawner['fields'][k]['value'] for k in ('fakeNames', 'adjectives', 'nouns')}
data['markers'] = [dict(id=r['transformId'], field=r['field'],
                        sourcePath=r['ref']['path'], position=dict(zip('xyz', r['worldPosition'])))
                   for r in motion['npcSpawnerReferencedTransforms'] if r['field'] != 'workoutStations']
data['muscles'] = muscles
data['source'] = motion['source']

# The moved player Arms is distinct from 4955, the extra NPC Arms.
station_data = list(motion['npcStationConfigs'])
arms_path = 'LEVEL0/LVL1/WorkoutArea (1)/MachineHolder/ArmsMachineInteractable'
arms_go = next(i for i in gos if path(i) == arms_path)
arms_object = next(level[c.component.path_id] for c in gos[arms_go].m_Component
                   if level[c.component.path_id].type.name == "MonoBehaviour"
                   and decode(level[c.component.path_id])[0] == "ChestPressMachine")
arms_sheet = decode(arms_object)[1]
arms_config = {}
for f in ('playerPos', 'playerExitPos', 'pivotObj', 'holder'):
    go = goid(arms_sheet[f], arms_object)
    arms_config[f] = {'value': {'path': path(go)} if go else {}}
for f in ('idleAnim', 'moveAnim', 'interactable', 'workoutType'):
    arms_config[f] = {'value': bool(arms_sheet[f]) if f == 'interactable' else arms_sheet[f]}
station_data.append(dict(id=arms_object.path_id, path=arms_path, sourcePublicFields={'fields': arms_config}))
player = {4697: 'Chest', 4877: 'Back', 4918: 'Legs', arms_object.path_id: 'Arms'}
data['stations'] = []
for station in station_data:
    f = {k:v['value'] for k,v in station['sourcePublicFields']['fields'].items()}
    data['stations'].append(dict(id=station['id'], sourcePath=station['path'], idle=f['idleAnim'], move=f['moveAnim'],
                       playerMuscle=player.get(station['id'], ''), npcCandidate=station['id'] != arms_object.path_id,
                       available=f['interactable'], **{n:f[n].get('path', '') for n in ('playerPos','playerExitPos','pivotObj','holder')}))

def source_rect(asset, tid):
    r = idx[asset,tid].read()
    return dict(name=idx[asset,r.m_GameObject.path_id].read().m_Name, id=tid,
                anchorsMin=vec(r.m_AnchorMin), anchorsMax=vec(r.m_AnchorMax),
                anchoredPosition={**vec(r.m_AnchoredPosition), 'z':r.m_LocalPosition.z},
                pivot=vec(r.m_Pivot), size=vec(r.m_SizeDelta),
                rotation=vec(r.m_LocalRotation), scale=vec(r.m_LocalScale))

def source_label(asset, cid):
    c = idx[asset,cid]
    d = decode(c)[1]
    go = idx[asset,d['m_GameObject']['m_PathID']].read()
    rect = source_rect(asset, go.m_Component[0].component.path_id)
    return dict(rect=rect, componentId=cid, text=d['m_text'], fontId=d['m_fontAsset']['m_PathID'],
                materialId=d['m_sharedMaterial']['m_PathID'], color=d['m_fontColor'],
                pointSize=d['m_fontSize'], autoSize=d['m_enableAutoSizing'],
                minSize=d['m_fontSizeMin'], maxSize=d['m_fontSizeMax'], margin=d['m_margin'],
                spacing=d['m_characterSpacing'])
data['titleCanvas'] = source_rect(shared,3328)
data['titleLabels'] = [source_label(shared,c) for c in (3415,3478,3455)]
data['alerts'] = []
for tid, aid, image_id in [
    (4086,3028,4924), (3996,3022,4826)]:
    candidates = motion['npcAlerts']['labels']
    own = 'DisconnectedAlert' if tid == 3996 else 'ConnectedAlert'
    label_ids = [r['component_id'] for r in candidates if f'/{own}/' in r['path']]
    assert len(label_ids)==2
    image = decode(level[image_id])[1]
    image_go = gos[image['m_GameObject']['m_PathID']]
    sprite = next(x for x in old['sprites'] if x['sourceId'] == image['m_Sprite']['m_PathID'])
    data['alerts'].append(dict(rect=source_rect('level0',tid), animatorId=aid,
        image=source_rect('level0',image_go.m_Component[0].component.path_id), imageColor=image['m_Color'],
        spritePath=sprite['path'], labels=[source_label('level0', c) for c in label_ids]))
    data['alerts'][-1]['labels'].sort(key=lambda row: children[component_go[tid]].index(component_go[row['rect']['id']]))
data['audio'] = []
for i in (3192,3191,3202):
    d = decode(idx[shared,i])[1]
    go = idx[shared,d['m_GameObject']['m_PathID']].read()
    rolloff = d['rolloffCustomCurve']['m_Curve']
    data['audio'].append(dict(componentId=i, name=go.m_Name, volume=d['m_Volume'], pitch=d['m_Pitch'],
        clipId=d['m_audioClip']['m_PathID'], mixerGroupId=d['OutputAudioMixerGroup']['m_PathID'],
        spatial=d['panLevelCustomCurve']['m_Curve'][0]['value'], maxDistance=d['MaxDistance'],
        minDistance=d['MinDistance'], rolloff=d['rolloffMode'], customRolloff=rolloff))
out = PLAN / 'workspace/npc-lifecycle-source.json'
out.write_text(json.dumps(data, indent=2, ensure_ascii=False)+'\n',encoding='utf8')
print(out, 'player Arms script:', arms_object.path_id)
