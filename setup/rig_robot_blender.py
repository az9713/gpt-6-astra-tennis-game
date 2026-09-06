"""Import the generated robot, build a proportion-matched skeleton and author tennis clips."""
import bpy, math, pathlib, json
from mathutils import Vector, Quaternion
ROOT=pathlib.Path(__file__).resolve().parents[1]
OUT=ROOT/'TennisGame/Assets/Prototype/Models';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(ROOT/'SourceAssets/Robot/meshy-robot.glb'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in meshes:
    bpy.context.view_layer.objects.active=o;o.select_set(True)
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);o.select_set(False)
coords=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
minimum=Vector(tuple(min(v[i] for v in coords) for i in range(3)));maximum=Vector(tuple(max(v[i] for v in coords) for i in range(3)))
center=Vector(((minimum.x+maximum.x)/2,(minimum.y+maximum.y)/2,minimum.z));scale=1.8/(maximum.z-minimum.z)
for o in meshes:
    for v in o.data.vertices:v.co=(o.matrix_world@v.co-center)*scale
    o.matrix_world.identity()
    for poly in o.data.polygons:poly.use_smooth=True
    o.name='RobotMesh'
for image in bpy.data.images:
    if image.type=='IMAGE' and image.size[0]>32:
        name=image.name.lower()
        if 'base' in name or 'color' in name or 'albedo' in name:
            image.filepath_raw=str(OUT/'robot-basecolor.png');image.file_format='PNG';image.save()
if not (OUT/'robot-basecolor.png').exists():
    # Locate the image actually connected to the base-color input.
    for material in bpy.data.materials:
        if not material.use_nodes:continue
        for node in material.node_tree.nodes:
            if node.type=='BSDF_PRINCIPLED' and node.inputs['Base Color'].is_linked:
                source=node.inputs['Base Color'].links[0].from_node
                if source.type=='TEX_IMAGE':source.image.filepath_raw=str(OUT/'robot-basecolor.png');source.image.file_format='PNG';source.image.save()
arm=bpy.data.armatures.new('RoboSkeleton');rig=bpy.data.objects.new('RoboRig',arm);bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
spec={'Root':((0,0,0),(0,0,.18),None),'Hips':((0,0,.70),(0,0,.85),'Root'),
      'Spine':((0,0,.85),(0,0,1.045),'Hips'),'Head':((0,0,1.045),(0,0,1.63),'Spine')}
for side,sign in [('L',1),('R',-1)]:
    spec['UpperArm.'+side]=((sign*.245,0,1.005),(sign*.38,.035,.83),'Spine')
    spec['Forearm.'+side]=((sign*.38,.035,.83),(sign*.51,0,.66),'UpperArm.'+side)
    spec['Hand.'+side]=((sign*.51,0,.66),(sign*.575,-.015,.56),'Forearm.'+side)
    spec['Thigh.'+side]=((sign*.145,0,.73),(sign*.205,-.045,.43),'Hips')
    spec['Shin.'+side]=((sign*.205,-.045,.43),(sign*.255,0,.16),'Thigh.'+side)
    spec['Foot.'+side]=((sign*.255,0,.16),(sign*.255,-.15,.10),'Shin.'+side)
for name,(head,tail,parent) in spec.items():
    b=arm.edit_bones.new(name);b.head=head;b.tail=tail
    if parent:b.parent=arm.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
def distance(p,a,b):
    a,b=Vector(a),Vector(b);v=b-a;t=max(0,min(1,(p-a).dot(v)/v.length_squared));return (p-a-v*t).length
for o in meshes:
    groups={name:o.vertex_groups.new(name=name) for name in spec}
    for vertex in o.data.vertices:
        p=vertex.co
        if p.z>1.09 or (p.z>1.015 and abs(p.x)<.285):candidates=['Head']
        elif p.z>.68 and abs(p.x)<.20:candidates=['Spine','Hips']
        elif p.z<.20:candidates=['Foot.'+('L' if p.x>0 else 'R')]
        else:candidates=[k for k in spec if k not in ['Root','Head'] and ('.' not in k or k.endswith('.L' if p.x>0 else '.R'))]
        ranked=sorted((distance(p,spec[k][0],spec[k][1]),k) for k in candidates)
        weights=[(ranked[0][1],1.0)]
        if len(ranked)>1 and ranked[1][0]-ranked[0][0]<.05:
            blend=.5*(1-(ranked[1][0]-ranked[0][0])/.05);weights=[(ranked[0][1],1-blend),(ranked[1][1],blend)]
        for k,w in weights:groups[k].add([vertex.index],w,'REPLACE')
    modifier=o.modifiers.new('Robo armature','ARMATURE');modifier.object=rig;o.parent=rig
# Smooth neighboring weights only around articulations. The head and shoes stay rigid.
for mesh in meshes:
    adjacency=[set() for _ in mesh.data.vertices]
    for edge in mesh.data.edges:
        a,b=edge.vertices;adjacency[a].add(b);adjacency[b].add(a)
    values=[{g.group:g.weight for g in v.groups} for v in mesh.data.vertices]
    # UV seams duplicate positions. They must receive identical weights to stay closed.
    seams={}
    for v in mesh.data.vertices:seams.setdefault(tuple(round(c,5) for c in v.co),[]).append(v.index)
    for iteration in range(3):
        updated=[]
        for v in mesh.data.vertices:
            current=values[v.index]
            if v.co.z>1.09 or v.co.z<.18 or not adjacency[v.index]:updated.append(current);continue
            weights={k:w*.65 for k,w in current.items()}
            for neighbor in adjacency[v.index]:
                for k,w in values[neighbor].items():weights[k]=weights.get(k,0)+.35*w/len(adjacency[v.index])
            weights=dict(sorted(weights.items(),key=lambda pair:-pair[1])[:4]);total=sum(weights.values())
            updated.append({k:w/total for k,w in weights.items()})
        for indices in seams.values():
            if len(indices)<2:continue
            shared={}
            for index in indices:
                for k,w in updated[index].items():shared[k]=shared.get(k,0)+w/len(indices)
            shared=dict(sorted(shared.items(),key=lambda pair:-pair[1])[:4]);total=sum(shared.values());shared={k:w/total for k,w in shared.items()}
            for index in indices:updated[index]=shared.copy()
        values=updated
    for v,weights in zip(mesh.data.vertices,values):
        for group in mesh.vertex_groups:group.remove([v.index])
        for k,w in weights.items():mesh.vertex_groups[k].add([v.index],w,'REPLACE')
import sys
sys.path.insert(0,str(ROOT/'setup'))
from tennis_motion import author
motion=author(rig)
(ROOT/'Evidence/Motion').mkdir(parents=True,exist_ok=True)
(ROOT/'Evidence/Motion/blender-motion.json').write_text(json.dumps(motion,indent=2))
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'SourceAssets/Robot/RoboPlayer.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'RoboPlayer.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},
    apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,
    bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,
    path_mode='COPY',embed_textures=False)
report={'height':1.8,'bones':len(spec),'vertices':sum(len(o.data.vertices) for o in meshes),'actions':[a.name for a in bpy.data.actions],
        'method':'Bent-joint skeleton, smoothed joint weights, analytical IK authoring, six coordinated clips and contact markers.'}
(ROOT/'Evidence/Prototype/blender-rig.json').write_text(json.dumps(report,indent=2));print('ROBO_RIG '+json.dumps(report))
