"""Package the local Windows player while excluding private debug paths and symbols."""
from pathlib import Path
import hashlib,json,zipfile
ROOT=Path(__file__).resolve().parents[1]
build=ROOT/'Builds/RoboOpen-Windows'
out=ROOT/'Evidence/Motion/RoboOpen-Windows-v0.2.0.zip'
out.parent.mkdir(parents=True,exist_ok=True)
home=str(Path.home());count=0
with zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED,compresslevel=7) as archive:
    for p in sorted(build.rglob('*')):
        if not p.is_file() or p.suffix.lower() in ['.pdb','.log'] or any('donotship' in x.lower() for x in p.parts):continue
        data=p.read_bytes()
        if p.name=='Assembly-CSharp.dll':
            marker=data.index(b'RSDS');start=marker+24;end=data.index(b'\0',start)
            assert data[start:end].endswith(b'.pdb')
            replacement=b'Assembly-CSharp.pdb';assert len(replacement)<=end-start
            data=data[:start]+replacement+b'\0'*(end-start-len(replacement))+data[end:]
        for path in [home,home.replace('\\','/')]:
            for encoding in ['utf-8','utf-16-le']:
                assert path.encode(encoding).lower() not in data.lower(), 'Private path in '+p.name
        archive.writestr('Builds/RoboOpen-Windows/'+p.relative_to(build).as_posix(),data);count+=1
    for name in ['PLAY_ROBO_OPEN.cmd','VIEW_ROBOT_MOTION.cmd','README.md','MOTION-UPGRADE.md']:
        archive.write(ROOT/name,name)
    archive.writestr('START_HERE.txt','ROBO OPEN v0.2\n\nPLAY_ROBO_OPEN.cmd starts the tennis game.\nVIEW_ROBOT_MOTION.cmd opens the four-stroke viewer.\nIn the viewer, B toggles bone guides and Escape closes it.\n\nIn the game: WASD moves; Space swings. A suitable high ball automatically selects smash.\nKeep the entire extracted folder together. Unity and Blender are not needed to play.\n')
with zipfile.ZipFile(out) as archive:assert archive.testzip() is None
report={'archive':out.name,'bytes':out.stat().st_size,'sha256':hashlib.sha256(out.read_bytes()).hexdigest(),'runtimeFiles':count,'archiveCrcVerified':True,'privateHomePathScan':'passed','codeViewPdbPath':'filename only','debugSymbolsExcluded':True}
target=ROOT/'docs/evidence/motion';target.mkdir(parents=True,exist_ok=True)
(target/'release.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report))
