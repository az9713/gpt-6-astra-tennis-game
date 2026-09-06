"""Blender check: joint weights, coincident UV seams, and posed edge stretch."""
import bpy, pathlib, json, math
ROOT=pathlib.Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'SourceAssets/Robot/RoboPlayer.blend'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.data.objects if o.type=='MESH')
groups={}
for v in mesh.data.vertices:groups.setdefault(tuple(round(c,5) for c in v.co),[]).append(v.index)
armLeak=0;maxWeightError=0
for v in mesh.data.vertices:
    maxWeightError=max(maxWeightError,abs(sum(g.weight for g in v.groups)-1))
    if abs(v.co.x)>.40 and .42<v.co.z<.7:
        armLeak+=sum(g.weight>.001 and any(k in mesh.vertex_groups[g.group].name for k in ['Thigh','Shin','Foot']) for g in v.groups)
results=[]
for name,frame in [('Forehand',15),('Backhand',16),('Smash',26),('Serve',43)]:
    rig.animation_data.action=bpy.data.actions[name];bpy.context.scene.frame_set(frame)
    deps=bpy.context.evaluated_depsgraph_get();evaluated=mesh.evaluated_get(deps);posed=evaluated.to_mesh();ratios=[];seam=0
    for indices in groups.values():
        if len(indices)>1:seam=max(seam,max((posed.vertices[i].co-posed.vertices[indices[0]].co).length for i in indices))
    for e in mesh.data.edges:
        a,b=e.vertices;rest=(mesh.data.vertices[a].co-mesh.data.vertices[b].co).length
        if rest>.0001:ratios.append((posed.vertices[a].co-posed.vertices[b].co).length/rest)
    ratios.sort();results.append({'clip':name,'contactFrame':frame-1,'maxCoincidentSeamDistance':seam,'edgeStretchP99':ratios[int(len(ratios)*.99)],'maxEdgeStretch':max(ratios)})
    evaluated.to_mesh_clear()
report={'passed':maxWeightError<1e-5 and armLeak==0 and all(r['maxCoincidentSeamDistance']<.0001 for r in results),'weightSumMaxError':maxWeightError,'handVerticesWithLegInfluence':armLeak,'contactPoses':results,'scope':'Checks contact poses, not an exhaustive artistic deformation certification.'}
path=ROOT/'Evidence/Motion/skin-quality.json';path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(report,indent=2))
print('MOTION_SKIN '+json.dumps(report))
assert report['passed']
