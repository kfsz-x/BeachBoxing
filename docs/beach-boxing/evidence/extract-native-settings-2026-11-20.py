"""Read-only authored settings audit plus exact WASM function slices."""
import json, re, pathlib, struct, hashlib
import UnityPy
ROOT=pathlib.Path(__file__).resolve().parents[3]
DUMP=ROOT/'.superpowers/sdd/2026-11-20-peripherals-interactions/native-dump'
script=json.loads((DUMP/'script.json').read_text(encoding='utf-8-sig'))
strings={int(x['address'],16):x['value'] for x in json.loads((DUMP/'stringliteral.json').read_text())}
element=[]
with (DUMP/'original.wat').open(encoding='utf-8') as f:
 for line in f:
  if line.startswith('  (elem '):
   element=[int(x) for x in line.split(' func ',1)[1].rstrip(')\n').split()];break
methods={}
for m in script['ScriptMethod']:
 if 0<m['Address']<=len(element):methods.setdefault(element[m['Address']-1],m['Name'])
targets=['KeybindsLoader$$InitializeKeybinds','KeybindsManager$$FixedUpdate','NPCSpawner$$CheckProfanity','NPCSpawner$$','ThirdPersonCharacterControllerSingleplayer$$']
selected={element[m['Address']-1]:m['Name'] for m in script['ScriptMethod'] if any(m['Name'].startswith(t) for t in targets) and 0<m['Address']<=len(element)}
current=None; blocks={}; lines={}; scanned_function=None; key_references={}
key_addresses={a:v for a,v in strings.items() if re.fullmatch(r'Keybind[0-8]|Sensitivity',v)}
with (DUMP/'original.wat').open(encoding='utf-8') as f:
 for i,line in enumerate(f,1):
  m=re.match(r'  \(func \(;(\d+);\)',line)
  if m:
   scanned_function=int(m[1]);current=scanned_function if scanned_function in selected else None
  c=re.search(r'i32.const (\d+)',line)
  if c and int(c[1]) in key_addresses:
   key_references.setdefault(key_addresses[int(c[1])],{}).setdefault(methods.get(scanned_function,str(scanned_function)),[]).append(i)
  if current is not None:
   if current not in blocks:blocks[current]=[];lines[current]=i
   line=re.sub(r'call (\d+)',lambda m:m[0]+' (; '+methods.get(int(m[1]),'?')+' ;)',line)
   line=re.sub(r'i32.const (-?\d+)',lambda m:m[0]+(' (; '+strings[int(m[1])].replace('\n','\\n')+' ;)' if int(m[1]) in strings else ''),line)
   blocks[current].append(line)
out={}
for fn,block in blocks.items():
 name=selected[fn];p=DUMP/(name.replace('$$','-').replace('.','_')+'.wat')
 p.write_text(';; '+name+' func='+str(fn)+' original.wat:'+str(lines[fn])+'\n'+''.join(block),encoding='utf-8')
 out[name]={'func':fn,'original_wat_line':lines[fn],'path':str(p.relative_to(ROOT)),'lines':len(block)}
(DUMP/'settings-function-map.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
audit=json.loads((ROOT/'docs/beach-boxing/evidence/peripheral-native-audit-2026-11-20.json').read_text())
objects={x['id']:x for x in audit['objects']}
root=objects[80]['path']; selected_objects=[x for x in audit['objects'] if x['path']==root or x['path'].startswith(root+'/')]
ids={x['id'] for x in selected_objects}
result={'scope':'Static shipped authored data and resolved WASM. No runtime claim.','source':audit['source'],'panel_root':root,'objects':selected_objects,'buttons':[x for x in audit['buttons'] if x['game_object_id'] in ids],'labels':[x for x in audit['labels'] if x['game_object_id'] in ids],'images':[x for x in audit['images'] if x['game_object_id'] in ids],'custom_components':[x for x in audit['custom_components'] if x['class'] in ('PlayerPrefSettings','KeybindsManager','KeybindsLoader')],'functions':out}
env=UnityPy.load(str(ROOT/'.superpowers/sdd/2026-11-18-beach-boxing-scene-parity/source/data.unity3d'))
allobjs=list(env.objects)
level={o.path_id:o for o in allobjs if o.assets_file.name=='level0'}
def val(r,p,fmt):return struct.unpack_from('<'+fmt,r,p)[0]
def text(r,p):
 n=val(r,p,'i');return r[p+4:p+4+n].decode(),(p+4+n+3)&~3
def ptr(r,p):return {'file_id':val(r,p,'i'),'path_id':val(r,p+4,'q')}
result['controls']=[]
for cid,go in [(4586,166),(4794,705),(4689,458)]:
 r=level[cid].get_raw_data();row={'component_id':cid,'game_object_id':go,'raw_hex':r.hex(),'raw_bytes':len(r),'navigation':val(r,32,'i'),'interactable':bool(r[292]),'target_graphic':ptr(r,296)}
 if cid in (4586,4794):
  row.update(type='Slider',fill_rect=ptr(r,308),handle_rect=ptr(r,320),direction=val(r,332,'i'),min_value=val(r,336,'f'),max_value=val(r,340,'f'),whole_numbers=bool(r[344]),value=val(r,348,'f'),event_start_offset=352,event_callback_target=ptr(r,356),event_method=text(r,404)[0])
 else:
  row.update(type='TMP_InputField',viewport=ptr(r,308),text_component=ptr(r,320),placeholder=ptr(r,332),scroll_sensitivity=val(r,380,'f'),content_type=val(r,384,'i'),input_type=val(r,388,'i'),asterisk_char=val(r,392,'i'),keyboard_type=val(r,396,'i'),line_type=val(r,400,'i'),hide_mobile_input=bool(r[404]),hide_soft_keyboard=bool(r[408]),character_validation=val(r,412,'i'),regex=text(r,416)[0],global_point_size=val(r,420,'f'),character_limit=val(r,424,'i'),on_end_edit_count=val(r,428,'i'),on_end_edit_target=ptr(r,432),on_end_edit_method=text(r,492)[0],on_submit_count=val(r,584,'i'),on_value_changed_count=val(r,604,'i'),caret_color=list(struct.unpack_from('<ffff',r,612)),custom_caret_color=bool(r[628]),selection_color=list(struct.unpack_from('<ffff',r,632)),initial_text=text(r,648)[0],caret_blink_rate=val(r,652,'f'),caret_width=val(r,656,'i'),read_only=bool(r[660]),rich_text=bool(r[664]),on_focus_select_all=bool(r[680]),reset_on_deactivation=bool(r[684]),restore_original_text_on_escape=bool(r[688]),rich_text_editing=bool(r[692]))
 result['controls'].append(row)
# NPCSpawner public serialized field sequence (dump.cs:193423); preserve authored filter.
scripts={o.path_id:o.read().m_ClassName for o in allobjs if o.type.name=='MonoScript'}
for o in level.values():
 if o.type.name!='MonoBehaviour':continue
 d=o.read_typetree(check_read=False)
 if scripts.get(d['m_Script']['m_PathID'])!='NPCSpawner':continue
 r=o.get_raw_data();p=32
 def string_array():
  global p
  count=val(r,p,'i');p+=4;values=[]
  for _ in range(count):v,p=text(r,p);values.append(v)
  return values
 fake=string_array();p+=24
 count=val(r,p,'i');p+=4+count*12+12
 adjectives=string_array();nouns=string_array();start=p;profanity=string_array()
 result['name_filter']={'component_id':o.path_id,'game_object_id':d['m_GameObject']['m_PathID'],'profanity_array_offset':start,'profanity_words':profanity,'count':len(profanity),'normalization':'lowercase; remove ASCII spaces; replace 0/o 4/a 1/i 5/s 3/e z/s; substring match; reject restores saved Name and triggers profanityAlert.On','source_function':out['NPCSpawner$$CheckProfanity']}
result['raw_control_layout_basis']='dump.cs serialized field order (Selectable:183831, Slider:184168, TMP_InputField:154583), exact raw offsets, event strings and object references cross-checked against authored hierarchy. InputField 712 bytes, Slider496 bytes.'
result['name_filter']['normalization_order']=[{'operation':'ToLower'}, {'operation':'Replace','from':' ','to':''}, {'operation':'Replace','from':'0','to':'o'}, {'operation':'Replace','from':'4','to':'a'}, {'operation':'Replace','from':'1','to':'i'}, {'operation':'Replace','from':'5','to':'s'}, {'operation':'Replace','from':'3','to':'e'}, {'operation':'Replace','from':'z','to':'s'}]
result['complete_wasm_key_reference_scan']=key_references
result['key_capture_invoke_slots']=[m for m in script['ScriptMethod'] if m['Address'] in (4683,4684,4685,4686,1104,1142,710)]
result['rules']={
 'volume':{'defaults':{'SFXVolume':1,'MusicVolume':1},'first_run_guard':'FirstTimeInGame != 1','slider_range':[0,2],'display_format':'0.0','map_db':'v <= 1 ? -80 + 80*Clamp01(v) : 20*Clamp01(v-1)','changed':'UpdateSettings Invoke ActuallyUpdateSettings after 0.03 seconds; stores BOTH slider values and updates BOTH labels and mixer parameters','open':'OpenSettings loads PlayerPrefs float values into both labels/sliders','source':'PlayerPrefSettings-{Start,OpenSettings,UpdateSettings,ActuallyUpdateSettings,MapVolumeToDecibels}.wat'},
 'sensitivity':{'default':1,'source_usage_boundary':'Entire original.wat scanned for i32.const 2803520: only two occurrences, both PlayerPrefSettings.Start func50100. No other reference found. No authored sensitivity control or PlayerPrefSettings sensitivity field.'},
 'keybinds':{'defaults':['W','A','S','D','Space','Escape','M','I','E'],'labels':['Forward','Left','Backward','Right','Jump','Menu','Shop','Locker','Interact'],'runtime_roles':['Forward in BoxingController/ThirdPerson move','Left in BoxingController/ThirdPerson move','Backward in BoxingController/ThirdPerson move','Right in BoxingController/ThirdPerson move','World Jump in ThirdPerson; ExitWorkout in WorkingOutManager (OR hardcoded Escape); NOT training pulse or fight block','Open/close menu in PlayerManager.Update','Legacy Shop UI; no consumer beyond initializer in complete WASM reference scan','Legacy Locker UI; no consumer beyond initializer in complete WASM reference scan','Interact down/up in PlayerManager.Update'],'keys':['Keybind'+str(i) for i in range(9)],'first_run_guard':'FirstTimeStarted != 1; initialized after Invoke(InitializeKeybinds,0.1)','buttons':[358,641,1105,1212,844,920,439,809,1183],'value_labels':[118,838,1192,241,1188,110,583,1152,822],'picking_backgrounds':[35,1041,240,254,459,447,763,450,773],'pick':'ChangeKey sets keyIndex, activates selected BG, clears selected text to one ASCII space, sets picking true','capture':'FixedUpdate when picking: iterate Enum.GetValues(KeyCode), Input.GetKey (held state, NOT GetKeyDown), set picking false; set label Enum.ToString; disable BG; PlayerPrefs.SetString(Keybind+index,key.ToString); PlayerPrefs.Save. No conflict/duplicate filter or Escape cancellation in this body. KeyCode includes mouse buttons.','hidden':'Original behavior: KeybindsManager is on inactive-by-default KEYBINDS GO498; its FixedUpdate is suppressed by GameObject activity. No OnDisable/reset method in dump.cs. Picking state persists on hiding until binding capture. Preserve this state when restoring source parity.','restore_defaults':'No reset/default Button among complete level0 52-Button inventory; no Reset method in KeybindsManager/Loader metadata; initial defaults only guarded InitializeKeybinds. Do not implement a new defaults control.'},
 'name':{'key':'Name','commit_event':'TMP_InputField.onEndEdit -> PlayerManagerSingleplayer.TryChangeName -> NPCSpawner.CheckProfanity -> ChangeName','limit':0,'empty_or_whitespace':'No explicit emptiness/trim/length check in TryChangeName/CheckProfanity/ChangeName; original value is saved, normalization only used for profanity test','validation':'None; Standard, SingleLine, Normal input','invalid':'Restore saved Name to input text; profanityAlert Animator trigger On; no write','valid':'SetString(Name,inputName.text); update nameText; no PlayerPrefs.Save call in ChangeName itself','generated_name':'PlayerManagerSingleplayer.Start checks GeneratedName and generates adjective+noun on first start; subsequently copies saved Name into HUD and input'},
 'menu':{'pause_root':80,'settings_tab':1190,'keybinds_tab':498,'name_input':458,'close_button':1089,'to_keybinds':857,'to_settings':683,'inactive_legacy_join_room':757,'enter':'TryToOpenMenu(0) rejects inFightMenu or menuOpen; activates pause menu then EnterMenu; disables movement, zeroes velocity, walking=false grounded=true, loads settings. TimeScale0 only when boxing flag true.','exit':'ExitMenu sets Animator Off for pause/trainer/daily; menuOpen false; movement true; boxing restores TimeScale1; commercial ad/audio callbacks follow. Native close animation0.166667s and open0.333333s.','menu_key':'PlayerManager.Update reads Keybind5; if not workout and AudioListener not paused, open/close menus. Input focus is not explicitly checked in recovered body.'}
}
result['rules']['keybinds']['runtime_roles'][4]='World Jump in ThirdPerson; ExitWorkout in WorkingOutManager (OR hardcoded Escape); NOT training pulse or fight block'
result['hardcoded_gameplay_inputs']={
 'workout_exit':{'input':'Keybind4 GetKeyDown OR Escape (KeyCode27)','source':'WorkingOutManager-Update.wat:155-174'},
 'workout_training_pulse':{'input':'hardcoded lowercase e via GetKeyDownString, independently of remapped Keybind4/8','source':'WorkingOutManager-Update.wat:795-798'},
 'fight_block':{'input':'hardcoded Space (KeyCode32) down/up; also legacy Fire2 and mobile flags; independent of Keybind4','sources':['BoxingController-Update.wat:261-294','BoxingController-Update.wat:338-371','BoxingController-PressBlock.wat:1','BoxingController-ReleaseBlock.wat:1']},
 'fight_punch':{'input':'legacy Fire1 plus mobile punch flag; no authored rebind row','source':'BoxingController-Update.wat:411-432'}
}
result['mixer_import_warning']={'path':'Assets/BeachBoxing/OriginalArt/AudioMixerController/Main.mixer','music_exposed':'mposwoH','sfx_exposed':'tuRmmTJ','original_runtime_names':['MusicVolume','SFXVolume'],'basis':'AssetRipper generates placeholder exposed names because shipped runtime AudioMixer stores name hashes only. Native controller522 exposed indices2,4 correspond Music/SFX; original code names are recoverable in WASM.'}
result['uncertainties']=['No live runtime claim; authored data + compiled WASM only.','Exact caught-key ordering follows Enum.GetValues(KeyCode); recoverable code has exception-unwind wrappers, whose table-slot metadata resolves GetKey/SetActive/SetString/Save.','First initialization ordering (Awake reads missing prefs before Start defaults) and editor-specific input event timing require live validation.','Restore-defaults UI or sensitivity slider would be a new product decision, not source parity.']
(ROOT/'docs/beach-boxing/evidence/native-settings-2026-11-20.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print('objects',len(ids),'buttons',len(result['buttons']),'functions',len(out))
for x in result['buttons']:print(x['game_object_id'],x['path'],[(c['method'],c['int_argument'],c['bool_argument'],c['target']['path_id']) for c in x['calls']])
for x in result['custom_components']:print(x['class'],x['component_id'],x['decoded_public_fields'])
