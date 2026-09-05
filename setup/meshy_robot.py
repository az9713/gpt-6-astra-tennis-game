"""Budgeted Meshy asset workflow. Never print credentials or signed download URLs."""
import argparse, base64, json, pathlib, urllib.request, datetime
ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / 'SourceAssets/Robot'
PRIVATE = ROOT / 'Evidence/Prototype'
LEDGER = OUT / 'meshy-ledger.json'
def save(path, obj):
    path.write_text(json.dumps(obj, indent=2), encoding='utf-8')
def api(path, body=None):
    values = {}
    for line in (ROOT/'.env').read_text(encoding='utf-8-sig').splitlines():
        if '=' in line and not line.lstrip().startswith('#'):
            k,v=line.split('=',1); values[k.strip()]=v.strip().strip('\"\'')
    key=values['MESHY_API_KEY']
    req=urllib.request.Request('https://api.meshy.ai/openapi/'+path,
        data=None if body is None else json.dumps(body).encode(),
        headers={'Authorization':'Bearer '+key,'Content-Type':'application/json'})
    with urllib.request.urlopen(req, timeout=120) as r: return json.load(r)
def download(url, path):
    with urllib.request.urlopen(url,timeout=120) as r: path.write_bytes(r.read())
def main():
    p=argparse.ArgumentParser();p.add_argument('action',choices=['balance','generate','poll','rig','poll-rig']);a=p.parse_args().action
    OUT.mkdir(parents=True,exist_ok=True); PRIVATE.mkdir(parents=True,exist_ok=True)
    ledger=json.loads(LEDGER.read_text()) if LEDGER.exists() else {'cap':400,'tasks':[]}
    if a=='balance':
        b=api('v1/balance'); save(PRIVATE/'meshy-balance.json',b);print(json.dumps(b));return
    kind='rigging' if a in ('rig','poll-rig') else 'image-to-3d'
    previous=next((x for x in ledger['tasks'] if x['kind']==kind),None)
    if a in ('generate','rig'):
        if previous: print('Existing task retained; use poll. '+json.dumps(previous));return
        reserve=40 if a=='generate' else 5
        if sum(t['reserved'] for t in ledger['tasks'])+reserve>ledger['cap']:raise RuntimeError('Budget cap exceeded')
        if a=='generate':
            body={'image_url':'data:image/png;base64,'+base64.b64encode((OUT/'robot-reference.png').read_bytes()).decode(),
                'ai_model':'meshy-6','should_texture':True,'enable_pbr':True,'should_remesh':True,
                'target_polycount':12000,'pose_mode':'a-pose','texture_resolution':'2k',
                'image_enhancement':False,'remove_lighting':True,'target_formats':['glb','fbx']}
        else:
            source=next(t for t in ledger['tasks'] if t['kind']=='image-to-3d')
            if source.get('status')!='SUCCEEDED':raise RuntimeError('Model not completed')
            body={'input_task_id':source['id'],'height_meters':1.8}
        task={'kind':kind,'reserved':reserve,'created':datetime.datetime.now(datetime.timezone.utc).isoformat(),'status':'SUBMITTING'}
        ledger['tasks'].append(task);save(LEDGER,ledger)
        result=api('v1/'+kind,body);task['id']=result['result'];task['status']='PENDING';save(LEDGER,ledger);print(json.dumps(task));return
    if not previous or 'id' not in previous:raise RuntimeError('No identified task; inspect submission before retrying')
    result=api('v1/'+kind+'/'+previous['id']);save(PRIVATE/(kind+'-response.json'),result)
    previous['status']=result['status'];previous['progress']=result.get('progress');previous['consumed_credits']=result.get('consumed_credits');save(LEDGER,ledger)
    print(json.dumps(previous))
    if result['status']=='FAILED':print(json.dumps(result.get('task_error',{})));return
    if result['status']=='SUCCEEDED':
        if kind=='image-to-3d':
            for ext in ('glb','fbx'):
                path=OUT/('meshy-robot.'+ext)
                if not path.exists():download(result['model_urls'][ext],path)
            if not (OUT/'meshy-preview.png').exists():download(result['thumbnail_url'],OUT/'meshy-preview.png')
        else:
            print('Rig completed; result keys: '+', '.join(result.get('result',{}).keys()))
            for k,v in result.get('result',{}).items():
                if isinstance(v,str) and k in ('rigged_character_fbx_url','rigged_character_glb_url'):
                    path=OUT/('rigged-robot.'+('fbx' if 'fbx' in k else 'glb'))
                    if not path.exists():download(v,path)
        print('Local asset download complete.')
if __name__=='__main__':main()
