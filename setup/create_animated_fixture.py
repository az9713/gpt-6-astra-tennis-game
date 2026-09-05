"""Create a deterministic two-bone Blender fixture for Unity import verification."""
from pathlib import Path
from math import radians
import bpy

root = Path(__file__).resolve().parents[1]
source = root / "SourceAssets" / "Preflight"
target = root / "TennisGame" / "Assets" / "Preflight"
source.mkdir(parents=True, exist_ok=True)
target.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)

def material(name, color):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*color, 1)
    return m

orange = material("FixtureOrange", (0.95, 0.25, 0.035))
cyan = material("FixtureCyan", (0.02, 0.8, 0.9))
white = material("FixtureWhite", (0.86, 0.9, 0.93))
parts = []
def box(name, pos, scale, mat, bone):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mat)
    group = ob.vertex_groups.new(name=bone)
    group.add(list(range(len(ob.data.vertices))), 1, "REPLACE")
    parts.append(ob)

# Character faces Blender -Y. Height is exactly two metres.
box("Body", (0, 0, 1), (0.6, 0.4, 0.8), orange, "Root")
box("Head", (0, 0, 1.7), (0.7, 0.55, 0.6), orange, "Root")
box("EyeForward", (0, -0.3, 1.75), (0.35, 0.08, 0.16), cyan, "Root")
box("LeftLeg", (-0.18, 0, 0.3), (0.22, 0.35, 0.6), white, "Root")
box("RightLeg", (0.18, 0, 0.3), (0.22, 0.35, 0.6), white, "Root")
box("SwingArm", (0.7, 0, 1.25), (0.8, 0.18, 0.18), white, "Swing")
box("RacketHead", (1.15, 0, 1.25), (0.24, 0.12, 0.48), cyan, "Swing")
bpy.ops.object.select_all(action="DESELECT")
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
mesh = bpy.context.object
mesh.name = "RiggedFixtureMesh"
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

arm = bpy.data.armatures.new("FixtureArmature")
rig = bpy.data.objects.new("FixtureRig", arm)
bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
mesh.select_set(False)
rig.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
base = arm.edit_bones.new("Root")
base.head, base.tail = (0, 0, 0), (0, 0, 1)
swing = arm.edit_bones.new("Swing")
swing.head, swing.tail = (0.3, 0, 1.25), (1.15, 0, 1.25)
swing.parent = base
bpy.ops.object.mode_set(mode="OBJECT")
modifier = mesh.modifiers.new("FixtureSkin", "ARMATURE")
modifier.object = rig
mesh.parent = rig
pose = rig.pose.bones["Swing"]
pose.rotation_mode = "XYZ"
for frame, angle in [(1, -40), (16, 55), (31, -40), (46, 55), (61, -40)]:
    pose.rotation_euler = (0, radians(angle), 0)
    pose.keyframe_insert(data_path="rotation_euler", frame=frame)
rig.animation_data.action.name = "TennisSwingCheck"
bpy.context.scene.render.fps = 30
bpy.context.scene.frame_start = 1
bpy.context.scene.frame_end = 61
bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(source / "AnimatedRobot.blend"))
bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(
    filepath=str(target / "AnimatedRobot.fbx"), use_selection=True,
    object_types={"MESH", "ARMATURE"}, add_leaf_bones=False,
    axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_UNITS", bake_anim=True,
    bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
    bake_anim_simplify_factor=0,
)
print("ANIMATED_FIXTURE_EXPORTED", target / "AnimatedRobot.fbx")
