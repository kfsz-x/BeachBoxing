"""Read shipped Unity objects; preserve peripheral UI and callback evidence."""
import hashlib, json, pathlib, re, struct
from collections import Counter, defaultdict
import UnityPy

ROOT = pathlib.Path(__file__).resolve().parents[3]
SRC = ROOT / '.superpowers/sdd/2026-11-18-beach-boxing-scene-parity/source/data.unity3d'
OUT = pathlib.Path(__file__).with_name('peripheral-native-audit-2026-11-20.json')
env = UnityPy.load(str(SRC))
objs = list(env.objects)
scripts = {o.path_id:o.read().m_ClassName for o in objs if o.type.name == 'MonoScript'}
idx = {(o.assets_file.name,o.path_id):o for o in objs}
inventory=defaultdict(Counter)
for o in objs:
    if o.type.name=='MonoBehaviour':
        d=o.read_typetree(check_read=False)
        inventory[o.assets_file.name][scripts.get(d['m_Script']['m_PathID'],'?')]+=1
gos, trans, comps, monos, rects = {}, {}, {}, {}, {}
for o in objs:
    f=o.assets_file.name
    if f != 'level0': continue
    if o.type.name == 'GameObject':
        d=o.read(); gos[o.path_id]={'name':d.m_Name,'active':bool(d.m_IsActive),'components':[p.component.path_id for p in d.m_Component]}
    elif o.type.name in ('Transform','RectTransform'):
        d=o.read(); trans[o.path_id]=(d.m_GameObject.path_id,d.m_Father.path_id)
        if o.type.name=='RectTransform':
            rects[d.m_GameObject.path_id]={k:{axis:getattr(getattr(d,k),axis) for axis in ('x','y')} for k in ('m_AnchorMin','m_AnchorMax','m_AnchoredPosition','m_SizeDelta','m_Pivot')}
    elif o.type.name == 'MonoBehaviour':
        d=o.read_typetree(check_read=False); monos[o.path_id]=(d['m_GameObject']['m_PathID'], scripts.get(d['m_Script']['m_PathID'],'?'))
    comps[o.path_id]=o
parents={go:trans.get(pa,(0,0))[0] for go,pa in trans.values()}
def path(go):
    if not go:return ''
    return (path(parents.get(go,0))+'/' if parents.get(go,0) else '')+gos.get(go,{'name':str(go)})['name']
def text_at(r,p):
    n=struct.unpack_from('<i',r,p)[0]
    if not 0<=n<10000 or p+4+n>len(r): raise ValueError()
    v=r[p+4:p+4+n].decode('utf-8'); return v,(p+4+n+3)&~3
def pptr(r,p):
    return dict(zip(('file_id','path_id'),struct.unpack_from('<iq',r,p)))
def callback(r,p):
    target=pptr(r,p); p+=12
    cls,p=text_at(r,p); method,p=text_at(r,p)
    if not cls.endswith(('Assembly-CSharp','UnityEngine','UnityEngine.CoreModule','UnityEngine.AudioModule')) or not method: raise ValueError()
    mode=struct.unpack_from('<i',r,p)[0];p+=4
    arg_obj=pptr(r,p);p+=12;arg_type,p=text_at(r,p)
    iv,fv=struct.unpack_from('<if',r,p);p+=8;sv,p=text_at(r,p); bv=bool(r[p]);p=(p+4)&~3
    state=struct.unpack_from('<i',r,p)[0]
    if not (0<=mode<=6 and 0<=state<=2):raise ValueError()
    return {'raw_offset':p,'target':target,'target_type':cls,'method':method,'mode':mode,'int_argument':iv,'float_argument':fv,'string_argument':sv,'bool_argument':bv,'object_argument':arg_obj,'object_argument_type':arg_type,'call_state':state}
def strings(r):
    out=[]
    for p in range(28,len(r)-4,4):
        try:
            v,q=text_at(r,p)
            if v and all(c.isprintable() or c in '\n\t' for c in v):out.append({'offset':p,'value':v})
        except (ValueError,UnicodeError,struct.error):pass
    return out
dump_path=ROOT/'.superpowers/sdd/2026-11-20-peripherals-interactions/native-dump/dump.cs'
dump=dump_path.read_text(encoding='utf-8-sig') if dump_path.exists() else ''
def decoded_fields(cls,r):
    m=re.search(r'public class '+re.escape(cls)+r' : MonoBehaviour[^\n]*\n\{(.*?)\n\s*// Methods',dump,re.S)
    if not m:return None
    fields=re.findall(r'^\s*public ([\w\[\].]+) (\w+);',m[1],re.M);p=32;out={}
    def read(t):
        nonlocal p
        if t.endswith('[]'):
            count=struct.unpack_from('<i',r,p)[0];p+=4
            if not 0<=count<10000:raise ValueError('bad count')
            return [read(t[:-2]) for _ in range(count)]
        if t=='string':
            v,p=text_at(r,p);return v
        if t in ['int','float','bool','LayerMask']:
            v=struct.unpack_from({'int':'<i','LayerMask':'<i','float':'<f','bool':'<?'}[t],r,p)[0];p+=4;return v
        if t in ['Color','Vector2','Vector3','Quaternion']:
            n={'Color':4,'Vector2':2,'Vector3':3,'Quaternion':4}[t];v=list(struct.unpack_from('<'+'f'*n,r,p));p+=4*n;return v
        v=pptr(r,p);p+=12;return v
    try:
        for t,name in fields:
            q=p;v=read(t);out[name]={'raw_offset':q,'value':v}
        return {'fields':out,'consumed':p,'bytes':len(r),'complete':p==len(r)}
    except Exception as ex:return {'fields':out,'consumed':p,'bytes':len(r),'complete':False,'error':str(ex)}
callbacks=[]; labels=[]; custom=[]; images=[]
sprite_objs={o.path_id:o for o in objs if o.type.name=='Sprite'}
sprite_names={i:o.read().m_Name for i,o in sprite_objs.items()}
for cid,(go,cls) in monos.items():
    r=comps[cid].get_raw_data()
    if cls=='Button':
        calls=[]
        for p in range(32,len(r)-40,4):
            try:c=callback(r,p);calls.append(c)
            except (ValueError,UnicodeError,struct.error,IndexError):pass
        callbacks.append({'component_id':cid,'game_object_id':go,'path':path(go),'calls':calls})
    elif cls=='TextMeshProUGUI':
        labels.append({'component_id':cid,'game_object_id':go,'path':path(go),'strings':strings(r)})
    elif cls=='Image':
        refs=[]
        for p in range(32,len(r)-11,4):
            q=pptr(r,p)
            if q['file_id']==2 and q['path_id'] in sprite_names: refs.append({'offset':p,'sprite_id':q['path_id'],'sprite_name':sprite_names[q['path_id']]})
        images.append({'component_id':cid,'game_object_id':go,'path':path(go),'sprite_candidates':refs,'color_rgba':list(struct.unpack_from('<ffff',r,44)),'raw_hex':r.hex()})
    elif cls in {'DailyRewards','TrainerMenu','WorkingOutManager','PlayerManagerSingleplayer','PlayerPrefSettings','Tutorial','GameManager','KeybindsManager','KeybindsLoader','PokiHandler','GotChestAlready','LockerManager','LootManager','Potion'}:
        custom.append({'component_id':cid,'class':cls,'game_object_id':go,'path':path(go),'bytes':len(r),'raw_hex':r.hex(),'strings':strings(r),'decoded_public_fields':decoded_fields(cls,r)})
def ints(cid,start,count):return list(struct.unpack_from('<'+'i'*count,comps[cid].get_raw_data(),start))
anim_assets=[]
trainer_prefabs=[]
for o in objs:
    if o.assets_file.name=='sharedassets0.assets' and o.type.name=='MonoBehaviour':
        d=o.read_typetree(check_read=False)
        if scripts.get(d['m_Script']['m_PathID'])=='Trainer':
            trainer_prefabs.append({'component_id':o.path_id,'game_object_id':d['m_GameObject']['m_PathID'],'decoded_public_fields':decoded_fields('Trainer',o.get_raw_data())})
    if o.assets_file.name=='sharedassets0.assets' and ((o.type.name=='AnimatorController' and o.path_id in (376,377,378,385)) or (o.type.name=='AnimationClip' and o.path_id in (327,328,329,344,345,347,350))):
        anim_assets.append({'file':o.assets_file.name,'path_id':o.path_id,'type':o.type.name,'data':o.read_typetree()})
result={
 'source':{'path':str(SRC),'sha256':hashlib.sha256(SRC.read_bytes()).hexdigest()},
 'scope':'All level0 GameObjects/Transforms/MonoBehaviours; all Button persistent calls and TMP strings. Metadata inventory inspected separately. No runtime claim.',
 'all_asset_monobehaviour_counts':{k:dict(v) for k,v in inventory.items()},
 'peripheral_animation_assets':anim_assets,
 'trainer_prefabs':trainer_prefabs,
 'objects':[{'id':i,'path':path(i),**d,'classes':[monos[c][1] for c in d['components'] if c in monos], 'rect':rects.get(i)} for i,d in gos.items()],
 'buttons':callbacks,'labels':labels,'images':images,'custom_components':custom,
 'trainer_serialized_arrays':{'component_id':4755,'trainerCosts':{'offset':220,'values':ints(4755,224,6)},'defaultChestModifiers':{'offset':524,'values':ints(4755,528,6)},'defaultArmsModifiers':{'offset':552,'values':ints(4755,556,6)},'defaultLegsModifiers':{'offset':580,'values':ints(4755,584,6)},'defaultBackModifiers':{'offset':608,'values':ints(4755,612,6)}},
 'limitations':['Stripped MonoBehaviour type trees prevent direct custom field decoding. Named arrays follow metadata field order plus exact serialized boundaries; verify formula separately.','Persistent callbacks are authored wiring, not proof of runtime branch execution.','Embedded save-key strings establish names only, not default values or update rules.']}
OUT.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(OUT);print('objects',len(gos),'buttons',len(callbacks),'labels',len(labels),'custom',len(custom))
for b in callbacks:
 if any(c['method'] in ('ClaimReward','PurchaseTrainer','SelectTrainer','EquipTrainer','LvlUp','TryToOpenMenu') for c in b['calls']): print(b['path'],[(c['method'],c['int_argument'],c['string_argument'],c['bool_argument']) for c in b['calls']])
print(result['trainer_serialized_arrays'])
