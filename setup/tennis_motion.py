"""Blender structural stroke authoring. Imported by rig_robot_blender.py.

Hand/foot targets, explicit bend planes, planted support feet and contact markers.
The animation is baked to the existing deform skeleton for a dependency-free player.
"""
import math
import bpy
from mathutils import Vector, Quaternion, Matrix

CONTACTS={'Forehand':14,'Backhand':15,'Smash':25,'Serve':42}
DURATIONS={'Idle':60,'Run':24,'Forehand':34,'Backhand':36,'Smash':46,'Serve':66}

def author(rig):
    scene=bpy.context.scene;scene.render.fps=30
    for b in rig.pose.bones:b.rotation_mode='QUATERNION'
    controls={}
    for name in ['Wrist.R','Wrist.L','Ankle.R','Ankle.L','ElbowPole.R','ElbowPole.L','KneePole.R','KneePole.L']:
        o=bpy.data.objects.new('CTRL_'+name,None);bpy.context.collection.objects.link(o)
        o.empty_display_type='SPHERE';o.empty_display_size=.045;controls[name]=o
    def refresh():bpy.context.view_layer.update()
    def rotate(name,axis,deg):
        b=rig.pose.bones[name];local=b.bone.matrix_local.to_quaternion().inverted()@Vector(axis)
        b.rotation_quaternion=Quaternion(local,math.radians(deg))
    def aim(b,target):
        refresh();m=b.matrix.copy();direction=Vector(target)-b.head
        if direction.length<1e-6:return
        q=(m.to_3x3()@Vector((0,1,0))).rotation_difference(direction.normalized())
        b.matrix=Matrix.Translation(b.head)@q.to_matrix().to_4x4()@m.to_3x3().to_4x4();refresh()
    def ik(upper,lower,end,target,pole):
        refresh();a=rig.pose.bones[upper];b=rig.pose.bones[lower];c=rig.pose.bones[end]
        origin=a.head.copy();l1=a.length;l2=b.length;delta=Vector(target)-origin
        distance=max(abs(l1-l2)+.002,min(delta.length,l1+l2-.008));axis=delta.normalized()
        bend=Vector(pole)-origin;bend-=axis*bend.dot(axis)
        if bend.length<.0001:bend=Vector((0,-1,0)).cross(axis)
        bend.normalize();along=(l1*l1-l2*l2+distance*distance)/(2*distance)
        elbow=origin+axis*along+bend*math.sqrt(max(0,l1*l1-along*along))
        aim(a,elbow);aim(b,origin+axis*distance)
    def blend(frames,f):
        for i in range(len(frames)-1):
            a,A=frames[i];b,B=frames[i+1]
            if f<=b:
                t=max(0,min(1,(f-a)/(b-a)));t=t*t*(3-2*t)
                return {k:Vector(A[k]).lerp(Vector(B[k]),t) if isinstance(A[k],tuple) else A[k]+(B[k]-A[k])*t for k in A}
        return frames[-1][1].copy()
    # Coordinates are Blender rig space: -Y is forward, +Z is up.
    ready=dict(right=(-.32,-.30,.98),left=(.30,-.28,1.01),shaft=(-.15,-.15,1),hips=0,turn=0,knee=.045,jump=0,lean=0)
    def pose(**kw):return dict(ready,**kw)
    timelines={
      'Forehand':[(0,ready),(8,pose(right=(-.54,.10,1.03),left=(.30,-.31,1.07),turn=-38,hips=-16,knee=.085,shaft=(-.8,.2,.4))),
                  (14,pose(right=(-.53,-.30,1.08),left=(.28,.08,1.06),turn=15,hips=12,knee=.02,shaft=(-.6,-.15,.55))),
                  (23,pose(right=(.14,-.32,1.22),left=(.30,.04,.92),turn=48,hips=24,knee=.04,shaft=(.6,0,.8))),(34,ready)],
      'Backhand':[(0,ready),(8,pose(right=(.10,-.29,1.10),left=(.24,-.30,1.1),turn=38,hips=18,knee=.09,shaft=(.8,.1,.55))),
                  (15,pose(right=(.14,-.40,1.08),left=(.22,-.38,1.06),turn=-12,hips=-8,knee=.025,shaft=(.8,-.05,.55))),
                  (25,pose(right=(-.56,-.18,1.26),left=(.23,-.10,1.03),turn=-45,hips=-20,shaft=(-.6,0,.8))),(36,ready)],
      'Smash':[(0,ready),(13,pose(right=(-.33,.14,1.32),left=(.18,-.16,1.38),turn=-22,hips=-10,knee=.13,shaft=(.1,.7,-.4),lean=-8)),
               (25,pose(right=(-.27,-.14,1.42),left=(.30,-.13,.86),turn=15,hips=10,jump=.21,knee=0,shaft=(-.05,-.2,1),lean=6)),
               (34,pose(right=(-.16,-.35,.88),left=(.30,-.13,.87),turn=36,hips=18,knee=.1,shaft=(.3,-.8,-.4),lean=12)),(46,ready)],
      'Serve':[(0,pose(right=(-.42,-.07,.78),left=(.30,-.15,.84),shaft=(0,-.4,-1))),
               (15,pose(right=(-.43,.06,1.10),left=(.18,-.16,1.39),turn=-23,hips=-13,knee=.04,shaft=(-.2,0,1))),
               (28,pose(right=(-.32,.13,1.28),left=(.17,-.10,1.40),turn=-32,hips=-16,knee=.13,shaft=(0,.8,-.3),lean=-10)),
               (42,pose(right=(-.27,-.13,1.43),left=(.26,-.16,.91),turn=12,hips=10,jump=.22,shaft=(0,-.18,1),lean=5)),
               (53,pose(right=(.06,-.33,.94),left=(.27,-.19,.94),turn=37,hips=18,knee=.11,shaft=(.3,-.7,-.5),lean=12)),(66,ready)]}
    for kind,endframe in DURATIONS.items():
        rig.animation_data_create();action=bpy.data.actions.new(kind);rig.animation_data.action=action;action.use_fake_user=True
        if kind in CONTACTS:action.pose_markers.new('CONTACT').frame=CONTACTS[kind]+1
        for f in range(endframe+1):
            for b in rig.pose.bones:b.location=(0,0,0);b.rotation_quaternion=Quaternion()
            t=f/endframe
            cfg=blend(timelines[kind],f) if kind in timelines else dict(ready)
            if kind=='Idle':cfg['knee']=.038+.012*math.sin(t*math.tau);cfg['right']=tuple(Vector(ready['right'])+Vector((0,0,math.sin(t*math.tau)*.012)))
            if kind=='Run':
                s=math.sin(t*math.tau);cfg.update(knee=.03,jump=abs(s)*.035,right=(-.32,-.19-.12*s,1.04),left=(.30,-.19+.12*s,1.04),turn=s*8,hips=-s*5,lean=7)
            root=rig.pose.bones['Root'];root.location=root.bone.matrix_local.to_quaternion().inverted()@Vector((0,0,cfg['jump']-cfg['knee']))
            rotate('Hips',(0,0,1),cfg['hips']);rotate('Spine',(0,0,1),cfg['turn']-cfg['hips']);rotate('Head',(0,0,1),-cfg['turn']*.72)
            # Small whole-body forward lean, with feet solved back onto the support plane.
            rig.pose.bones['Spine'].rotation_quaternion @= Quaternion((1,0,0),math.radians(cfg['lean']))
            refresh()
            for side,sign in [('R',-1),('L',1)]:
                foot=Vector((sign*.245,-.015,.16+cfg['jump']))
                if kind=='Run':
                    s=math.sin(t*math.tau+(0 if side=='R' else math.pi));foot.y+=s*.17;foot.z+=max(0,s)*.11
                elif kind in timelines:foot.y+=(-.065 if side=='L' else .055)*math.sin(math.pi*t)
                pole=Vector((sign*.25,-.8,.4));ik('Thigh.'+side,'Shin.'+side,'Foot.'+side,foot,pole)
                aim(rig.pose.bones['Foot.'+side],rig.pose.bones['Foot.'+side].head+Vector((0,-1,0)))
                wrist=Vector(cfg['right' if side=='R' else 'left'])
                if wrist.z>1.20:wrist.x=sign*max(abs(wrist.x),.42)
                wrist+=Vector((0,0,cfg['jump']-cfg['knee']*.3))
                elbow=Vector((sign*.8,.05,1.05));ik('UpperArm.'+side,'Forearm.'+side,'Hand.'+side,wrist,elbow)
                shaft=Vector(cfg['shaft']) if side=='R' else Vector((0,-.4,.6))
                aim(rig.pose.bones['Hand.'+side],rig.pose.bones['Hand.'+side].head+shaft)
                for name,position in [('Wrist.'+side,wrist),('Ankle.'+side,foot),('ElbowPole.'+side,elbow),('KneePole.'+side,pole)]:
                    controls[name].location=position
                    controls[name]['purpose']='Baked target guide; motion targets are authored in setup/tennis_motion.py.'
            for b in rig.pose.bones:
                b.keyframe_insert('rotation_quaternion',frame=f+1,group=b.name);b.keyframe_insert('location',frame=f+1,group=b.name)
        track=rig.animation_data.nla_tracks.new();track.name=kind;track.strips.new(kind,1,action);track.mute=True
    rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_set(1);refresh()
    return {'contactsAt30fps':CONTACTS,'frames':DURATIONS,'method':'Analytical two-bone IK, explicit elbow/knee bend planes, planted ankles, torso counter-rotation, contact poses and recovery','controls':list(controls)}
