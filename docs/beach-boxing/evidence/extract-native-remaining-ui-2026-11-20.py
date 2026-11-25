"""Extract source UI into existing editor DTO schema; stage new art outside Assets.

No Unity Editor or product file writes. DummyDll is used only for exact serialized
field definitions; UnityPy checks complete component byte consumption.
"""
import copy, hashlib, json, pathlib, re, struct, subprocess, wave
from collections import Counter
from PIL import Image, ImageChops
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator

ROOT=pathlib.Path(__file__).resolve().parents[3]
PLAN=ROOT/'.superpowers/sdd/2026-11-20-remaining-native-parity'
STAGE=PLAN/'workspace/ui-art-staging'
STAGE.mkdir(parents=True,exist_ok=True)
SRC=ROOT/'.superpowers/sdd/2026-11-18-beach-boxing-scene-parity/source/data.unity3d'
BASE=ROOT/'Assets/BeachBoxing/OriginalArt/UI/NativePeripheralUi.json'
OUT=ROOT/'docs/beach-boxing/evidence/native-remaining-ui-2026-11-20.json'
old=json.loads(BASE.read_text(encoding='utf-8'))
feedback=json.loads((OUT.parent/'native-feedback-2026-11-20.json').read_text(encoding='utf-8'))
env=UnityPy.load(str(SRC));objs=list(env.objects)
idx={(o.assets_file.name,o.path_id):o for o in objs}
level={o.path_id:o for o in objs if o.assets_file.name=='level0'}
gen=TypeTreeGenerator('2022.3.34f1')
gen.load_local_dll_folder(str(ROOT/'.superpowers/sdd/2026-11-20-peripherals-interactions/native-dump/DummyDll'))
cache={};scripts={};evidence={};unsupported=Counter()
for o in objs:
 if o.type.name=='MonoScript':scripts[o.path_id]=o.read()
def decode(o):
 key=(o.assets_file.name,o.path_id)
 if key in cache:return cache[key]
 if o.type.name=='MonoBehaviour':
  header=o.read_typetree(check_read=False);s=scripts[header['m_Script']['m_PathID']]
  name=(s.m_Namespace+'.' if s.m_Namespace else '')+s.m_ClassName
  try:d=o.read_typetree(gen.get_nodes_up(s.m_AssemblyName,name),check_read=True)
  except Exception as ex:
   if s.m_ClassName in ('Image','TextMeshProUGUI','Button','Slider','TMP_InputField','Scrollbar','ScrollRect','RectMask2D','Mask','ChangeTransformOnPortrait','EventTrigger','LayoutElement'):raise RuntimeError((o.path_id,name,len(o.get_raw_data()))) from ex
   d={**header,'rawHex':o.get_raw_data().hex(),'bodyDecodeError':str(ex)}
  # Generator's native MonoBehaviour header omits editor-hideflags; body is
  # correctly aligned. Use original source header rather than its script PPtr.
  for k in ('m_GameObject','m_Enabled','m_Script','m_Name'):d[k]=header[k]
  cls=s.m_ClassName
 else:d=o.read_typetree();cls=o.type.name
 cache[key]=(cls,d);return cls,d
def pointer(p,origin):
 pid=p.get('m_PathID',0)
 if not pid:return None
 fid=p.get('m_FileID',0)
 name=origin.assets_file.name if not fid else origin.assets_file.externals[fid-1].path.replace('\\','/').rsplit('/',1)[-1]
 # Unity default resources not carried in this UnityFS are intentionally rejected.
 return idx.get((name,pid))
gos={};rects={};parents={};children={};component_go={}
for o in level.values():
 if o.type.name=='GameObject':gos[o.path_id]=o.read()
 elif o.type.name in ('RectTransform','Transform'):
  d=o.read();go=d.m_GameObject.path_id;rects[go]=d
  component_go[o.path_id]=go
for go,g in gos.items():
 for c in g.m_Component:component_go[c.component.path_id]=go
for go,r in rects.items():
 parents[go]=component_go.get(r.m_Father.path_id,0)
 children[go]=[component_go[c.path_id] for c in r.m_Children]
def path(go):return (path(parents[go])+'/' if parents.get(go) else '')+gos[go].m_Name
def goid(p,origin):
 o=pointer(p,origin)
 if not o:return 0
 if o.type.name=='GameObject':return o.path_id
 return component_go.get(o.path_id,0)
def vec(d):return {k:float(getattr(d,k)) for k in ('x','y','z','w') if hasattr(d,k)}
def event_calls(d):return d.get('m_PersistentCalls',{}).get('m_Calls',[])
def compbase(o,d):return {'componentId':o.path_id,'enabled':bool(d.get('m_Enabled',1))}

move=decode(level[5003])[1]['moveTutorial']['m_PathID']
roots=[80,881,457,372,move,933]
chosen={n['sourceId'] for n in old['nodes']}
def subtree(go):
 chosen.add(go)
 for child in children.get(go,[]):subtree(child)
for go in roots:subtree(go)
for go in list(chosen):
 while go and go!=old['canvasId']:
  chosen.add(go);go=parents.get(go,0)
chosen.discard(old['canvasId'])
# Portrait pose markers may be outside the restored branch.
portraits=copy.deepcopy(old['portraits'])
seen_portrait={x['targetId'] for x in portraits}
for go in list(chosen):
 for c in gos[go].m_Component:
  o=level[c.component.path_id]
  if o.type.name!='MonoBehaviour':continue
  cls,d=decode(o)
  if cls!='ChangeTransformOnPortrait':continue
  p={'targetId':goid(d['myTransform'],o),'portraitId':goid(d['portraitSize'],o),'landscapeId':goid(d['landscapeSize'],o),'applyPhoneScale':bool(d['changeScaleOnPhone']),'phoneScale':d['scaleOnPhone'],'sourceComponentId':o.path_id}
  if p['targetId'] not in seen_portrait:portraits.append(p);seen_portrait.add(p['targetId'])
  for marker in (p['portraitId'],p['landscapeId']):
   while marker and marker!=old['canvasId']:chosen.add(marker);marker=parents.get(marker,0)

sprites={x['sourceId']:copy.deepcopy(x) for x in old['sprites']}
used_paths={x['path'] for x in sprites.values()};sprite_checks=[];staged=[];audio_staged=set()
base_audio_ids={n['audio']['sourceClipId'] for n in old['nodes'] if n.get('audio')}
def full_sprite(s):
 trim=s.image.convert('RGBA');w,h=round(s.m_Rect.width),round(s.m_Rect.height)
 offset=s.m_RD.textureRectOffset
 canvas=Image.new('RGBA',(w,h),(0,0,0,0))
 xy=(round(offset.x),h-round(offset.y)-trim.height)
 assert xy[0]>=0 and xy[1]>=0 and xy[0]+trim.width<=w and xy[1]+trim.height<=h,(s.m_Name,(w,h),trim.size,xy)
 canvas.paste(trim,xy)
 return canvas,trim,offset,xy
for frame in old['sprites']:
 s=idx[('sharedassets0.assets',frame['sourceId'])].read()
 canvas,trim,offset,xy=full_sprite(s)
 current=Image.open(ROOT/frame['path']).convert('RGBA')
 equal=canvas.size==current.size and canvas.tobytes()==current.tobytes()
 sprite_checks.append({'sourceId':frame['sourceId'],'equalExistingPngPixels':equal,'canvas':list(canvas.size),'existing':list(current.size)})
def sprite_path(p,origin):
 obj=pointer(p,origin)
 if not obj:return ''
 sid=obj.path_id
 if sid in sprites:return sprites[sid]['path']
 s=obj.read();filename=re.sub(r'[<>:"/\\|?*]','_',s.m_Name)+'.png'
 target='Assets/BeachBoxing/OriginalArt/UI/NativeSprites/'+filename
 if target in used_paths:filename=str(sid)+'-'+filename;target='Assets/BeachBoxing/OriginalArt/UI/NativeSprites/'+filename
 canvas,trim,offset,xy=full_sprite(s);w,h=canvas.size
 stage=STAGE/filename;canvas.save(stage)
 row={'sourceId':sid,'sourceAsset':obj.assets_file.name,'path':target,'pixelsPerUnit':s.m_PixelsToUnits,'border':vec(s.m_Border),'pivot':vec(s.m_Pivot),'meshType':1,'stagingPath':str(stage.relative_to(ROOT)).replace('\\','/'),'sourceRect':{'width':w,'height':h},'sourceTrimSize':list(trim.size),'sourceTrimOffset':vec(offset),'canvasPasteTopLeft':list(xy),'pngSha256':hashlib.sha256(stage.read_bytes()).hexdigest()}
 sprites[sid]=row;used_paths.add(target);staged.append(row)
 return target
def selectable(o,d):
 row=compbase(o,d);colors=d['m_Colors'];state=d['m_SpriteState']
 row.update(navigation=d['m_Navigation']['m_Mode'],transition=d['m_Transition'],interactable=bool(d['m_Interactable']),targetId=goid(d['m_TargetGraphic'],o),multiplier=colors['m_ColorMultiplier'],fadeDuration=colors['m_FadeDuration'])
 for k in ('normal','highlighted','pressed','selected','disabled'):row[k]=colors['m_'+k.title()+'Color']
 for k in ('highlighted','pressed','selected','disabled'):row[k+'Sprite']=sprite_path(state['m_'+k.title()+'Sprite'],o)
 return row
def audio(o,d):
 row=compbase(o,d);clip=pointer(d['m_audioClip'],o)
 cid=clip.path_id if clip else 0
 target='Assets/BeachBoxing/OriginalArt/UI/NativeAudio/native-ui-'+str(cid)+'.wav' if cid else ''
 if cid and cid not in base_audio_ids and cid not in audio_staged:
  samples=clip.read().samples
  assert len(samples)==1,(cid,len(samples))
  encoded=next(iter(samples.values()));filename='native-ui-'+str(cid)+'.wav';p=STAGE/filename
  source_meta={'sourceSha256':hashlib.sha256(encoded).hexdigest()}
  if encoded[:4]==b'RIFF':p.write_bytes(encoded);source_meta.update(sourceContainer='WAV',sourceCodec='PCM')
  elif encoded[4:8]==b'ftyp':
   raw_dir=STAGE.parent/'ui-audio-source';raw_dir.mkdir(parents=True,exist_ok=True)
   raw=raw_dir/('native-ui-'+str(cid)+'.original.m4a');raw.write_bytes(encoded)
   if not p.exists() or p.read_bytes()[:4]!=b'RIFF':
    ffmpeg=ROOT/'.superpowers/tools/imageio-ffmpeg-0.6.0/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
    assert ffmpeg.exists(),'Decode original AAC to PCM WAV with the verified local ffmpeg before re-running.'
    subprocess.run([str(ffmpeg),'-hide_banner','-loglevel','error','-y','-i',str(raw),'-vn','-c:a','pcm_s16le','-map_metadata','-1','-bitexact',str(p)],check=True)
   source_meta.update(sourceContainer='MP4',sourceCodec='AAC',sourcePath=str(raw.relative_to(ROOT)).replace('\\','/'),decoder='ffmpeg7.1 pcm_s16le; native source sample rate/channels retained')
  else:raise ValueError(('Unrecognized source audio container',cid,encoded[:16].hex()))
  with wave.open(str(p),'rb') as decoded:
   assert decoded.getsampwidth()==2 and decoded.getnframes()>0
   source_meta.update(decodedCodec='PCM16',channels=decoded.getnchannels(),sampleRate=decoded.getframerate(),frames=decoded.getnframes())
  staged.append({'type':'AudioClip','sourceId':cid,'path':target,'stagingPath':str(p.relative_to(ROOT)).replace('\\','/'),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),**source_meta})
  audio_staged.add(cid)
 row.update(sourceClipId=cid,clipPath=target,playOnAwake=bool(d['m_PlayOnAwake']),loop=bool(d['Loop']),volume=d['m_Volume'],pitch=d['m_Pitch'],spatialBlend=d['panLevelCustomCurve']['m_Curve'][0]['value'],sourceMixerGroupId=d['OutputAudioMixerGroup']['m_PathID'])
 return row
def label(o,d):
 row=compbase(o,d)
 row.update(text=d['m_text'],fontId=d['m_fontAsset']['m_PathID'],pointSize=d['m_fontSize'],color=d['m_fontColor'],raycast=bool(d['m_RaycastTarget']),maskable=bool(d['m_Maskable']),horizontal=d['m_HorizontalAlignment'],vertical=d['m_VerticalAlignment'],wordWrap=bool(d['m_enableWordWrapping']),autoSize=bool(d['m_enableAutoSizing']),minSize=d['m_fontSizeMin'],maxSize=d['m_fontSizeMax'],spacing=d['m_characterSpacing'],sourceGraphicColor=d['m_Color'],sourceFontMaterialId=d['m_sharedMaterial']['m_PathID'],fontStyle=d['m_fontStyle'],richText=bool(d['m_isRichText']),margin=d['m_margin'],overflowMode=d['m_overflowMode'])
 return row
oldnodes={x['sourceId']:x for x in old['nodes']}
nodes=[]
def visit(go):
 if go in chosen:
  r=rects[go]
  assert hasattr(r,'m_AnchorMin'),(go,path(go),'non-Rect UI ancestor')
  row={'sourceId':go,'parentId':parents[go],'name':gos[go].m_Name,'active':bool(gos[go].m_IsActive),'anchorMin':vec(r.m_AnchorMin),'anchorMax':vec(r.m_AnchorMax),'pivot':vec(r.m_Pivot),'sizeDelta':vec(r.m_SizeDelta),'anchoredPosition':{**vec(r.m_AnchoredPosition),'z':r.m_LocalPosition.z},'rotation':vec(r.m_LocalRotation),'scale':vec(r.m_LocalScale),'sourceSiblingIndex':children[parents[go]].index(go),'sourcePath':path(go)}
  for c in gos[go].m_Component:
   o=level[c.component.path_id]
   if o.type.name in ('RectTransform','Transform','CanvasRenderer'):continue
   cls,d=decode(o);evidence[str(o.path_id)]={'class':cls,'go':go,'bytes':len(o.get_raw_data()),'data':d}
   if cls=='Image':
    row['image']={**compbase(o,d),'spritePath':sprite_path(d['m_Sprite'],o),'color':d['m_Color'],'raycast':bool(d['m_RaycastTarget']),'maskable':bool(d['m_Maskable']),'imageType':d['m_Type'],'preserveAspect':bool(d['m_PreserveAspect']),'fillCenter':bool(d['m_FillCenter']),'fillMethod':d['m_FillMethod'],'fillAmount':d['m_FillAmount'],'fillClockwise':bool(d['m_FillClockwise']),'fillOrigin':d['m_FillOrigin'],'useSpriteMesh':bool(d['m_UseSpriteMesh']),'pixelsPerUnitMultiplier':d['m_PixelsPerUnitMultiplier'],'sourceSpriteId':d['m_Sprite']['m_PathID']}
   elif cls=='TextMeshProUGUI':row['label']=label(o,d)
   elif cls=='Button':row['button']=selectable(o,d);row['button']['persistentCalls']=event_calls(d['m_OnClick'])
   elif cls=='Mask':row['mask']={**compbase(o,d),'showGraphic':bool(d['m_ShowMaskGraphic'])}
   elif cls=='RectMask2D':row['rectMask']={**compbase(o,d),'padding':d['m_Padding'],'softness':d['m_Softness']}
   elif cls=='Scrollbar':
    row['scrollbar']={**selectable(o,d),'handleId':goid(d['m_HandleRect'],o),'direction':d['m_Direction'],'value':d['m_Value'],'size':d['m_Size'],'steps':d['m_NumberOfSteps']}
   elif cls=='ScrollRect':
    row['scroll']={**compbase(o,d),'contentId':goid(d['m_Content'],o),'viewportId':goid(d['m_Viewport'],o),'horizontalBarId':goid(d['m_HorizontalScrollbar'],o),'verticalBarId':goid(d['m_VerticalScrollbar'],o)}
    for k,source in {'horizontal':'m_Horizontal','vertical':'m_Vertical','movementType':'m_MovementType','elasticity':'m_Elasticity','inertia':'m_Inertia','decelerationRate':'m_DecelerationRate','sensitivity':'m_ScrollSensitivity','horizontalVisibility':'m_HorizontalScrollbarVisibility','verticalVisibility':'m_VerticalScrollbarVisibility','horizontalSpacing':'m_HorizontalScrollbarSpacing','verticalSpacing':'m_VerticalScrollbarSpacing'}.items():row['scroll'][k]=d[source]
    for k in ('horizontal','vertical','inertia'):row['scroll'][k]=bool(row['scroll'][k])
   elif cls=='Slider':
    row['slider']={**selectable(o,d),'fillId':goid(d['m_FillRect'],o),'handleId':goid(d['m_HandleRect'],o),'direction':d['m_Direction'],'minValue':d['m_MinValue'],'maxValue':d['m_MaxValue'],'wholeNumbers':bool(d['m_WholeNumbers']),'value':d['m_Value'],'persistentCalls':event_calls(d['m_OnValueChanged'])}
   elif cls=='TMP_InputField':
    inp=selectable(o,d)
    for k,source in {'viewportId':'m_TextViewport','textId':'m_TextComponent','placeholderId':'m_Placeholder','verticalBarId':'m_VerticalScrollbar'}.items():inp[k]=goid(d[source],o)
    for k,source in {'contentType':'m_ContentType','inputType':'m_InputType','asteriskChar':'m_AsteriskChar','keyboardType':'m_KeyboardType','lineType':'m_LineType','hideMobileInput':'m_HideMobileInput','hideSoftKeyboard':'m_HideSoftKeyboard','characterValidation':'m_CharacterValidation','regexValue':'m_RegexValue','pointSize':'m_GlobalPointSize','characterLimit':'m_CharacterLimit','caretColor':'m_CaretColor','customCaretColor':'m_CustomCaretColor','selectionColor':'m_SelectionColor','text':'m_Text','caretBlinkRate':'m_CaretBlinkRate','caretWidth':'m_CaretWidth','readOnly':'m_ReadOnly','richText':'m_RichText','selectAllOnFocus':'m_OnFocusSelectAll','resetOnDeactivation':'m_ResetOnDeActivation','restoreOriginalTextOnEscape':'m_RestoreOriginalTextOnEscape','richTextEditingAllowed':'m_isRichTextEditingAllowed','lineLimit':'m_LineLimit','scrollSensitivity':'m_ScrollSensitivity'}.items():inp[k]=d[source]
    inp['endEditCalls']=event_calls(d['m_OnEndEdit']);inp['submitCalls']=event_calls(d['m_OnSubmit']);inp['valueChangedCalls']=event_calls(d['m_OnValueChanged']);inp['fontId']=d['m_GlobalFontAsset']['m_PathID'];row['input']=inp
    for k in ('hideMobileInput','hideSoftKeyboard','customCaretColor','readOnly','richText','selectAllOnFocus','resetOnDeactivation','restoreOriginalTextOnEscape','richTextEditingAllowed'):inp[k]=bool(inp[k])
   elif cls=='AudioSource':row['audio']=audio(o,d)
   elif cls=='EventTrigger':
    row['eventTriggers']=d['m_Delegates']
    for ev in d['m_Delegates']:
     if ev['eventID']!=2:continue
     for call in event_calls(ev['callback']):
      if call['m_MethodName']=='Play':row['pointerAudioId']=goid(call['m_Target'],o)
   elif cls=='Animator':row['animator']={**compbase(o,d),'sourceControllerId':d['m_Controller']['m_PathID'],'updateMode':d['m_UpdateMode']}
   elif cls=='LayoutElement':row['layoutElement']={**compbase(o,d),'sourceData':d}
   else:unsupported[cls]+=1
  if row.get('button'):
   for call in row['button']['persistentCalls']:
    if call['m_MethodName']=='Play':row['clickAudioId']=goid(call['m_Target'],o)
  nodes.append(row)
 for child in children.get(go,[]):visit(child)
visit(old['canvasId'])
assert len(nodes)==len(chosen),(len(nodes),len(chosen),chosen-{x['sourceId'] for x in nodes})
result={k:copy.deepcopy(v) for k,v in old.items() if k not in ('nodes','sprites','portraits')}
result.update(source='Original data.unity3d + exact DummyDll serialized types; includes old 360 nodes and remaining native UI',nodes=nodes,sprites=list(sprites.values()),portraits=portraits)
result['unsupported']=sorted(unsupported)
result['sourceEvidence']={'source':str(SRC.relative_to(ROOT)),'sha256':hashlib.sha256(SRC.read_bytes()).hexdigest(),'unityVersion':'2022.3.34f1','roots':roots,'moveTutorialReference':{'componentId':5003,'field':'moveTutorial','go':move},'baseNodeCount':len(old['nodes']),'newNodeCount':len(nodes)-len(old['nodes']),'reusedSourceIds':sorted(set(oldnodes)&chosen),'componentReadMethod':'UnityPy.read_typetree with TypeTreeGenerator from existing DummyDll; check_read=True requires complete serialized byte consumption; original native MonoBehaviour header preserved separately.','stagedAssets':staged,'unsupportedComponents':dict(unsupported)}
result['componentEvidence']=evidence
result['sourceEvidence']['spriteReconstructionPixelChecks']=sprite_checks
result['sourceEvidence']['spriteReconstructionMethod']='UnityPy source Sprite image preserves packed transform; restore full m_Rect RGBA transparent canvas and paste trimmed sprite at (textureRectOffset.x, rect.height - textureRectOffset.y - trim.height); preserve source PPU/pivot/border and old NativeSprites meshType1.'
result['sourceAnimatorBindings']=[x for x in feedback['uiAnimatorBindings'] if x['asset']=='level0' and x['go'] in chosen]
controller_names={x['controllerName'] for x in result['sourceAnimatorBindings']}
result['sourceControllers']={k:v for k,v in feedback['uiControllers'].items() if k in controller_names}
result['sourceClips']=feedback['uiClips']
result['hashedAttributeMap']=feedback['hashedAttributeMap']
# Validate dependency closure and unchanged values of all old DTO fields.
node_ids={n['sourceId'] for n in nodes};ordered={old['canvasId']};diffs=[]
def compare(a,b,p):
 if isinstance(a,dict):
  for k,v in a.items():
   if k not in b:diffs.append(p+'.'+k+' missing')
   else:compare(v,b[k],p+'.'+k)
 elif a!=b:diffs.append(p)
for n in nodes:
 assert n['parentId'] in ordered,(n['sourceId'],'parent appears after child')
 ordered.add(n['sourceId'])
 for k in ('pointerAudioId','clickAudioId'):assert not n.get(k) or n[k] in node_ids,(n['sourceId'],k)
 for k,fields in {'button':['targetId'],'slider':['targetId','fillId','handleId'],'input':['targetId','viewportId','textId','placeholderId','verticalBarId'],'scroll':['contentId','viewportId','horizontalBarId','verticalBarId'],'scrollbar':['targetId','handleId']}.items():
  for f in fields:
   if n.get(k):assert not n[k].get(f) or n[k][f] in node_ids,(n['sourceId'],k,f,n[k][f])
 if n.get('image'):assert n['image']['spritePath'] or not n['image']['sourceSpriteId'],n['sourceId']
 if n['sourceId'] in oldnodes:compare(oldnodes[n['sourceId']],n,str(n['sourceId']))
assert not diffs,diffs
assert all(c['equalExistingPngPixels'] for c in sprite_checks)
result['sourceEvidence']['verification']={'uniqueSourceIds':len(node_ids)==len(nodes),'parentsBeforeChildren':True,'componentReferenceClosure':True,'existingDtoFieldDifferences':diffs,'existingSpritePixelMatches':sum(c['equalExistingPngPixels'] for c in sprite_checks),'existingSpritePixelChecks':len(sprite_checks),'typedUiCompleteRead':True,'customRawFallbacks':[{'componentId':int(k),'class':v['class'],'go':v['go'],'error':v['data']['bodyDecodeError']} for k,v in evidence.items() if 'bodyDecodeError' in v['data']]}
OUT.write_text(json.dumps(result,ensure_ascii=False,indent=2,allow_nan=False),encoding='utf-8')
print('roots',[(g,path(g)) for g in roots]);print('nodes',len(nodes),'new',len(nodes)-len(oldnodes),'sprites',len(sprites),'staged',len(staged));print('unsupported',dict(unsupported));print('output',OUT)
