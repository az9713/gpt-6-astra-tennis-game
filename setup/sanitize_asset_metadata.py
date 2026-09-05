"""Run with Blender --background --python setup/sanitize_asset_metadata.py.

Remove exporter home paths while preserving mesh/animation data. Originals stay
in ignored Evidence/Publication/backups. Publication hygiene, not asset generation.
"""
from pathlib import Path
import bpy, json, re, shutil
from io_scene_fbx import parse_fbx, encode_bin
ROOT=Path(__file__).resolve().parents[1]
BACKUP=ROOT/'Evidence/Publication/backups'
report=[]
home=str(Path.home()).replace('\\','/').lower()
root_norm=str(ROOT).replace('\\','/')

def backup(p):
    dest=BACKUP/p.relative_to(ROOT)
    dest.parent.mkdir(parents=True,exist_ok=True)
    if not dest.exists(): shutil.copy2(p,dest)

def clean_path(raw):
    text=raw.decode('utf-8',errors='strict').replace('\\','/')
    if home not in text.lower(): return raw
    if text.lower().startswith(root_norm.lower()+'/'):
        return ('//'+text[len(root_norm)+1:]).encode()
    return ('//'+text.rsplit('/',1)[-1]).encode()

methods=dict(zip('BCZYILFDRSifdlbc',['bool','char','int8','int16','int32','int64','float32','float64','bytes','string','int32_array','float32_array','float64_array','int64_array','bool_array','byte_array']))
for p in sorted((ROOT/'TennisGame/Assets').rglob('*.fbx')):
    parsed,version=parse_fbx.parse(str(p)); count=[0]
    def convert(n):
        dest=encode_bin.FBXElem(n.id)
        for value,kind in zip(n.props,n.props_type):
            k=chr(kind)
            if k=='S':
                new=clean_path(value)
                if new!=value: count[0]+=1
                value=new
            getattr(dest,'add_'+methods[k])(value)
        dest.elems.extend(convert(child) for child in n.elems)
        return dest
    rewritten=convert(parsed)
    if count[0]:
        backup(p); encode_bin.write(str(p),rewritten,version)
        check,_=parse_fbx.parse(str(p))
        def compare(a,b):
            assert a.id==b.id and a.props_type==b.props_type and len(a.elems)==len(b.elems)
            for x,y,k in zip(a.props,b.props,a.props_type):
                assert (clean_path(x) if chr(k)=='S' else x)==y, 'Unexpected non-path modification'
            for x,y in zip(a.elems,b.elems): compare(x,y)
        compare(parsed,check)
    report.append({'file':p.relative_to(ROOT).as_posix(),'pathStringsCleaned':count[0],'allOtherParsedPropertiesIdentical':True})

for p in sorted((ROOT/'SourceAssets').rglob('*.blend')):
    backup(p); bpy.ops.wm.open_mainfile(filepath=str(p))
    before={'vertices':sum(len(o.data.vertices) for o in bpy.data.objects if o.type=='MESH'),'bones':sum(len(a.bones) for a in bpy.data.armatures),'actions':sorted(a.name for a in bpy.data.actions)}
    bpy.ops.file.make_paths_relative()
    bpy.ops.wm.save_as_mainfile(filepath=str(p),compress=False)
    data=p.read_bytes(); matches=list(re.finditer(rb'[A-Za-z]:[\\/][^\x00\r\n]*',data)); count=0
    for m in reversed(matches):
        raw=m.group()
        try: new=clean_path(raw)
        except UnicodeDecodeError: continue
        if new!=raw:
            assert len(new)<=len(raw)
            data=data[:m.start()]+new+b'\x00'*(len(raw)-len(new))+data[m.end():]; count+=1
    p.write_bytes(data)
    bpy.ops.wm.open_mainfile(filepath=str(p))
    after={'vertices':sum(len(o.data.vertices) for o in bpy.data.objects if o.type=='MESH'),'bones':sum(len(a.bones) for a in bpy.data.armatures),'actions':sorted(a.name for a in bpy.data.actions)}
    assert before==after
    report.append({'file':p.relative_to(ROOT).as_posix(),'fixedWidthPathFieldsCleaned':count,'reopenedInBlender':True,'preserved':after})
(ROOT/'docs/evidence/asset-privacy.json').write_text(json.dumps(report,indent=2)+'\n')
print('ASSET_PRIVACY '+json.dumps(report))
