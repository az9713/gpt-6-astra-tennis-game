from pathlib import Path
import bpy

output = Path(__file__).resolve().parent / "verification"
output.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.mesh.primitive_cube_add()
bpy.context.active_object.name = "BlenderUnityPipelineCheck"
bpy.ops.wm.save_as_mainfile(filepath=str(output / "blender_check.blend"))
bpy.ops.export_scene.fbx(filepath=str(output / "blender_check.fbx"), use_selection=True)
assert (output / "blender_check.blend").stat().st_size > 0
assert (output / "blender_check.fbx").stat().st_size > 0
print(f"VERIFIED: Blender {bpy.app.version_string}; scene saved; FBX export successful")
