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
    spec['UpperArm.'+side]=((sign*.245,0,1.005),(sign*.38,0,.83),'Spine')
    spec['Forearm.'+side]=((sign*.38,0,.83),(sign*.51,0,.66),'UpperArm.'+side)
    spec['Hand.'+side]=((sign*.51,0,.66),(sign*.575,-.015,.56),'Forearm.'+side)
    spec['Thigh.'+side]=((sign*.145,0,.73),(sign*.205,0,.43),'Hips')
    spec['Shin.'+side]=((sign*.205,0,.43),(sign*.255,0,.16),'Thigh.'+side)
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
        elif p.z>.69 and abs(p.x)<.255:candidates=['Spine','Hips']
        elif p.z>.53 and abs(p.x)>.29:candidates=[k for k in spec if k.endswith('.L' if p.x>0 else '.R') and any(part in k for part in ['Arm','Forearm','Hand'])]
        else:candidates=[k for k in spec if k.endswith('.L' if p.x>0 else '.R') and any(part in k for part in ['Thigh','Shin','Foot'])]+['Hips']
        ranked=sorted((distance(p,spec[k][0],spec[k][1]),k) for k in candidates)
        weights=[(ranked[0][1],1.0)]
        if len(ranked)>1 and ranked[1][0]-ranked[0][0]<.05:
            blend=.5*(1-(ranked[1][0]-ranked[0][0])/.05);weights=[(ranked[0][1],1-blend),(ranked[1][1],blend)]
        for k,w in weights:groups[k].add([vertex.index],w,'REPLACE')
    modifier=o.modifiers.new('Robo armature','ARMATURE');modifier.object=rig;o.parent=rig
scene=bpy.context.scene;scene.render.fps=30
def angle(name,axis,degrees):
    bone=rig.pose.bones[name]
    localaxis=bone.bone.matrix_local.to_quaternion().inverted()@Vector(axis)
    bone.rotation_quaternion=Quaternion(localaxis,math.radians(degrees))
def pose(kind,t):
    for b in rig.pose.bones:b.rotation_mode='QUATERNION';b.rotation_quaternion=Quaternion();b.location=(0,0,0)
    if kind=='Idle':
        angle('Head',(0,0,1),math.sin(t*math.tau)*2);rig.pose.bones['Root'].location.z=math.sin(t*math.tau)*.008
        angle('UpperArm.R',(1,0,0),-8);angle('UpperArm.L',(1,0,0),-8)
    elif kind=='Run':
        s=math.sin(t*math.tau)
        for side,sign in [('L',1),('R',-1)]:
            angle('Thigh.'+side,(1,0,0),s*sign*27);angle('Shin.'+side,(1,0,0),max(0,-s*sign)*30)
            angle('UpperArm.'+side,(1,0,0),-s*sign*19-8)
        rig.pose.bones['Root'].location.z=abs(s)*.025
    else:
        s=math.sin(t*math.pi)
        if kind=='Serve':
            angle('UpperArm.R',(0,1,0),s*-145);angle('Forearm.R',(1,0,0),s*-70)
            angle('UpperArm.L',(0,1,0),s*100);angle('Spine',(1,0,0),s*10)
        elif kind=='Forehand':
            angle('UpperArm.R',(1,0,0),s*-67);angle('Forearm.R',(0,0,1),s*-62)
            angle('Spine',(0,0,1),math.sin(t*math.tau)*-19);angle('Head',(0,0,1),math.sin(t*math.tau)*10)
        elif kind=='Backhand':
            angle('UpperArm.R',(1,0,0),s*-60);angle('Forearm.R',(0,0,1),s*55)
            angle('Spine',(0,0,1),math.sin(t*math.tau)*22)
for kind,duration in [('Idle',2.0),('Run',.66),('Serve',.75),('Forehand',.55),('Backhand',.55)]:
    rig.animation_data_create();action=bpy.data.actions.new(kind);rig.animation_data.action=action;action.use_fake_user=True
    count=round(duration*30)
    for frame in range(count+1):
        pose(kind,frame/count)
        for b in rig.pose.bones:b.keyframe_insert('rotation_quaternion',frame=frame+1,group=b.name);b.keyframe_insert('location',frame=frame+1,group=b.name)
    track=rig.animation_data.nla_tracks.new();track.name=kind;strip=track.strips.new(kind,1,action);track.mute=True
rig.animation_data.action=None;pose('Idle',0);scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'SourceAssets/Robot/RoboPlayer.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'RoboPlayer.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},
    apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,
    bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,
    path_mode='COPY',embed_textures=False)
report={'height':1.8,'bones':len(spec),'vertices':sum(len(o.data.vertices) for o in meshes),'actions':[a.name for a in bpy.data.actions],
        'method':'Custom Blender skeleton and spatial skinning fitted to toy proportions; five authored clips.'}
(ROOT/'Evidence/Prototype/blender-rig.json').write_text(json.dumps(report,indent=2));print('ROBO_RIG '+json.dumps(report))
