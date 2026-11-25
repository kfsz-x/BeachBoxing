"""Flatten exact source clips/controller defaults for editor-only clip restoration."""
import json,math,pathlib,re
ROOT=pathlib.Path(__file__).resolve().parents[3]
EVIDENCE=ROOT/'docs/beach-boxing/evidence'
ui=json.loads((EVIDENCE/'native-remaining-ui-2026-11-20.json').read_text(encoding='utf-8'))
clips=[];guid_clips={};by_id={}
def finite(v):
 f=float(v);return f if math.isfinite(f) else 0.0
def key(k,axis=None):
 def part(field):
  v=k.get(field,0)
  return v[axis] if axis else v
 a,b=part('inSlope'),part('outSlope')
 return {'time':k['time'],'value':finite(part('value')),'inSlope':finite(a),'outSlope':finite(b),'inConstant':not math.isfinite(float(a)),'outConstant':not math.isfinite(float(b)),'weightedMode':k.get('weightedMode',0),'inWeight':finite(part('inWeight')),'outWeight':finite(part('outWeight'))}
def curve(path,prop,kind,keys,sourceprop,sourceclass):return {'path':path or '', 'property':prop,'targetKind':kind,'sourceProperty':sourceprop,'sourceClassId':sourceclass,'discrete':prop=='m_IsActive','keys':keys}
for name,src in ui['sourceClips'].items():
 cid=src['originalIds'][0]['id'];curves=[]
 for group,prop,axes in [('rotationCurves','m_LocalRotation','xyzw'),('eulerCurves','localEulerAnglesRaw','xyz'),('positionCurves','m_LocalPosition','xyz'),('scaleCurves','m_LocalScale','xyz')]:
  for c in src.get(group,[]):
   for axis in axes:curves.append(curve(c.get('path'),prop+'.'+axis,'Transform',[key(k,axis) for k in c['curve']['m_Curve']],prop+'.'+axis,4))
 for c in src.get('floatCurves',[]):
  raw=c['attribute'];prop=raw
  m=re.search(r'0x([0-9a-fA-F]{8})',raw)
  if m:prop=ui['hashedAttributeMap']['0x'+m[1].lower()]
  sourceprop=prop;cls=c['classID']
  if cls==1:kind='GameObject'
  elif cls==224:kind='RectTransform'
  elif cls==4:kind='Transform'
  elif prop.startswith('m_fontColor'):
   kind='Graphic';prop=prop.replace('m_fontColor','m_Color',1)
  elif prop=='m_fontSize':kind='OriginalSdfText';prop='nativePointSize'
  elif prop.startswith('m_Color'):kind='Graphic'
  elif prop=='m_FillAmount':kind='Image'
  elif prop=='m_Alpha':kind='CanvasGroup'
  else:raise ValueError((name,cls,raw))
  curves.append(curve(c.get('path'),prop,kind,[key(k) for k in c['curve']['m_Curve']],sourceprop,cls))
 result={'sourceId':cid,'name':name,'frameRate':src['sampleRate'],'duration':src['settings']['m_StopTime']-src['settings']['m_StartTime'],'loop':bool(src['settings']['m_LoopTime']),'wrapMode':src.get('wrapMode',0),'curves':curves,'events':[{**e,'stringParameter':e.get('data') or ''} for e in src.get('events',[])],'source':src['source']}
 clips.append(result);by_id[cid]=result
 meta=ROOT/(src['source']['path']+'.meta')
 guid=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8'),re.M)[1];guid_clips[guid]=cid
bindings=[]
nodes={n['sourceId']:n for n in ui['nodes']}
for binding in ui['sourceAnimatorBindings']:
 controller=ui['sourceControllers'][binding['controllerName']]
 docs={x['fileID']:next(iter(x['data'].values())) for x in controller['documents']}
 ctl=next(next(iter(x['data'].values())) for x in controller['documents'] if x['classID']==91)
 machine=docs[ctl['m_AnimatorLayers'][0]['m_StateMachine']['fileID']]
 default_state=machine['m_DefaultState']['fileID'];states=[]
 for child in machine['m_ChildStates']:
  sid=child['m_State']['fileID'];state=docs[sid];motion=state['m_Motion'];cid=guid_clips.get(motion.get('guid',''),0)
  states.append({'name':state['m_Name'],'clipId':cid,'isDefault':sid==default_state,'speed':state['m_Speed'],'loop':by_id[cid]['loop'] if cid else False})
 default=next(x for x in states if x['isDefault'])
 row={'sourceAnimatorId':binding['animatorId'],'targetId':binding['go'],'sourcePath':binding['path'],'controllerId':binding['controllerId'],'controllerName':binding['controllerName'],'defaultClipId':default['clipId'],'loop':default['loop'],'updateMode':nodes[binding['go']]['animator']['updateMode'],'enabled':binding['enabled'],'states':states}
 bindings.append(row)
out={'source':'native-feedback original clip/controller evidence + exact native-remaining-ui Animator updateMode','clips':clips,'bindings':bindings}
(EVIDENCE/'native-ui-motion-2026-11-20.json').write_text(json.dumps(out,ensure_ascii=False,indent=2,allow_nan=False),encoding='utf-8')
lines=['# Native UI default motion bindings','', '| GO | Animator | Controller | Default clip ID / name | Loop | UpdateMode |','|---:|---:|---|---|---|---:|']
for b in bindings:lines.append('| '+str(b['targetId'])+' | '+str(b['sourceAnimatorId'])+' | '+b['controllerName']+' | '+str(b['defaultClipId'])+' / '+by_id.get(b['defaultClipId'],{}).get('name','none')+' | '+str(b['loop'])+' | '+str(b['updateMode'])+' |')
(EVIDENCE/'native-ui-motion-2026-11-20.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print('clips',len(clips),'bindings',len(bindings));print([(b['targetId'],b['defaultClipId'],b['loop'],b['updateMode']) for b in bindings])
