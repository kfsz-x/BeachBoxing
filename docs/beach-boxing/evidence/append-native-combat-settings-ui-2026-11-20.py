"""Add the five level1-only pause Home nodes without aliasing level0 IDs."""
import copy,json,pathlib,runpy
ROOT=pathlib.Path(__file__).resolve().parents[3]
ns=runpy.run_path(str(pathlib.Path(__file__).with_name('extract-native-remaining-ui-2026-11-20.py')))
result=ns['result'];prior_nodes=ns['nodes'];prior_evidence=ns['evidence']
old_paths={ns['path'](g):g for g in ns['gos']}
level={o.path_id:o for o in ns['objs'] if o.assets_file.name=='level1'}
gos={};rects={};cg={}
for o in level.values():
 if o.type.name=='GameObject':
  d=o.read();gos[o.path_id]=d
  for c in d.m_Component:cg[c.component.path_id]=o.path_id
 elif o.type.name in ('Transform','RectTransform'):rects[o.read().m_GameObject.path_id]=o.read()
parents={g:cg.get(r.m_Father.path_id,0) for g,r in rects.items()}
children={g:[cg[p.path_id] for p in r.m_Children] for g,r in rects.items()}
selected=set()
def select(g):
 selected.add(g)
 for c in children[g]:select(c)
select(517)
assert len(selected)==5
# run_path returns the function globals as this namespace; switch only hierarchy context.
scope=ns['visit'].__globals__
scope.update(level=level,gos=gos,rects=rects,component_go=cg,parents=parents,children=children,
             chosen=selected,nodes=[],evidence={})
ns['visit'](517)
new=scope['nodes'];extra_evidence=scope['evidence']
mapping={g:1000000+g for g in selected}
for go in gos:
 name=ns['path'](go)
 if go not in mapping and name in old_paths:mapping[go]=old_paths[name]
for n in new:
 source_id=n['sourceId'];n['sourceAsset']='level1';n['sourceGameObjectId']=source_id
 n['sourceId']=mapping[source_id]
 n['parentId']=1190 if source_id==517 else mapping[n['parentId']]
 if source_id==517:n['active']=False # Training context starts without the fight-only action.
 for key in ('pointerAudioId','clickAudioId'):
  if n.get(key):n[key]=mapping[n[key]]
 for key in ('button','slider','input','scroll','scrollbar'):
  if not n.get(key):continue
  for field in ('targetId','fillId','handleId','viewportId','textId','placeholderId','verticalBarId','horizontalBarId','contentId'):
   if n[key].get(field):n[key][field]=mapping[n[key][field]]
result['nodes']=prior_nodes+new
result['sprites']=list(scope['sprites'].values())
prior_evidence.update({'level1:'+k:v for k,v in extra_evidence.items()})
result['componentEvidence']=prior_evidence
result['fightContext']={'homeId':1000517,'sourceAsset':'level1','sourceGameObjectId':517,
 'homeCallback':'BoxingController.GoBackToWorkout -> 0.18s scene fade -> Workout beach',
 'hiddenNameIds':[458,205,547],
 'music':{'targetId':653,'trainingPosition':{'x':0,'y':-122.43225860595703,'z':0},'fightPosition':{'x':0,'y':-159.49998474121094,'z':0}},
 'sfx':{'targetId':171,'trainingPosition':{'x':0,'y':-26.5,'z':0},'fightPosition':{'x':0,'y':-33.067745208740234,'z':0}},
 'sourcePauseAnimator':{'training':{'asset':'level0','id':3002,'updateMode':0},'fight':{'asset':'level1','id':1924,'updateMode':2}},
 'combatSourceEvidence':'.superpowers/sdd/2026-11-20-remaining-native-parity/evidence/native-combat-presentation-2026-11-20.json.pauseComparison'}
result['sourceEvidence']['newNodeCount']=len(result['nodes'])-result['sourceEvidence']['baseNodeCount']
result['sourceEvidence']['combatAdditionalNodes']=5
result['sourceEvidence']['combatCompositeIdOffset']=1000000
assert len({x['sourceId'] for x in result['nodes']})==len(result['nodes'])
seen={result['canvasId']}
for n in result['nodes']:
 assert n['parentId'] in seen,(n['sourceId'],n['parentId'])
 seen.add(n['sourceId'])
ns['OUT'].write_text(json.dumps(result,ensure_ascii=False,indent=2,allow_nan=False),encoding='utf-8')
print('merged with level1 Home nodes:',len(result['nodes']),'sprites',len(result['sprites']))
