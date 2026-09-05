using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class PreflightSetup
{
    const string ModelPath = "Assets/Preflight/AnimatedRobot.fbx";
    const string ScenePath = "Assets/Preflight/Preflight.unity";
    static string Root => Directory.GetParent(Application.dataPath).Parent.FullName;
    static string Evidence => Path.Combine(Root, "Evidence");

    [MenuItem("Preflight/Prepare and verify scene")]
    public static void Prepare()
    {
        Directory.CreateDirectory(Evidence);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        Require(importer != null, "FBX importer missing");
        importer.animationType = ModelImporterAnimationType.Legacy;
        importer.importAnimation = true;
        importer.isReadable = true;
        importer.globalScale = 1;
        importer.importNormals = ModelImporterNormals.Import;
        importer.SaveAndReimport();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var robot = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        robot.name = "AnimatedBlenderFixture";
        var skin = robot.GetComponentInChildren<SkinnedMeshRenderer>();
        Require(skin != null, "No skinned mesh imported");
        Require(skin.bones.Length >= 2, "Expected at least two bones");
        Require(skin.sharedMesh.normals.Length == skin.sharedMesh.vertexCount, "Normals missing");
        var weights = skin.sharedMesh.boneWeights;
        Require(weights.Length == skin.sharedMesh.vertexCount, "Skin weights missing");
        foreach (var w in weights)
            Require(Mathf.Abs(w.weight0 + w.weight1 + w.weight2 + w.weight3 - 1) < .001f, "Unnormalized skin weight");
        var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToArray();
        Require(clips.Length > 0, "No animation clip imported");
        var clip = clips.OrderByDescending(c => c.length).First();
        Require(clip.length > 1, "Animation duration too short");
        foreach (var renderer in robot.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterials = renderer.sharedMaterials.Select(m =>
            {
                Require(m != null, "Imported material missing");
                var color = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;
                return MakeMaterial(m.name.Replace(" (Instance)", ""), color);
            }).ToArray();
        }
        var animation = robot.GetComponent<Animation>() ?? robot.AddComponent<Animation>();
        animation.AddClip(clip, "Swing");
        animation.clip = clip;
        animation.wrapMode = WrapMode.Loop;
        clip.SampleAnimation(robot, .1f);
        var baked = new Mesh();
        skin.BakeMesh(baked);
        var first = baked.vertices;
        var size = skin.bounds.size;
        Require(size.y > 1.8f && size.y < 2.2f, "Model is not approximately 2 metres tall: " + size);
        clip.SampleAnimation(robot, .5f);
        skin.BakeMesh(baked);
        var second = baked.vertices;
        float delta = first.Zip(second, (a, b) => Vector3.Distance(a, b)).Max();
        Require(delta > .05f, "Animation did not deform the imported skin");
        clip.SampleAnimation(robot, 0);
        UnityEngine.Object.DestroyImmediate(baked);

        var court = GameObject.CreatePrimitive(PrimitiveType.Cube);
        court.name = "PreflightCourt";
        court.transform.position = new Vector3(0, -.1f, 0);
        court.transform.localScale = new Vector3(9, .2f, 7);
        court.GetComponent<Renderer>().sharedMaterial = MakeMaterial("CourtCoral", new Color(.66f, .13f, .10f));
        var surround = GameObject.CreatePrimitive(PrimitiveType.Cube);
        surround.name = "TurquoiseSurround";
        surround.transform.position = new Vector3(0, -.23f, 0);
        surround.transform.localScale = new Vector3(13, .2f, 11);
        surround.GetComponent<Renderer>().sharedMaterial = MakeMaterial("SurroundTeal", new Color(.015f, .45f, .42f));
        var cam = new GameObject("Main Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.transform.position = new Vector3(4, 3.0f, 6);
        cam.transform.LookAt(new Vector3(0, 1, 0));
        cam.backgroundColor = new Color(.12f, .20f, .29f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.fieldOfView = 45;
        cam.nearClipPlane = .1f;
        cam.farClipPlane = 100;
        cam.gameObject.AddComponent<AudioListener>();
        var light = new GameObject("Sun").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(45, -35, 0);
        light.intensity = 2;
        light.shadows = LightShadows.Soft;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.5f, .5f, .5f);
        var runtime = new GameObject("PreflightRuntime").AddComponent<PreflightRuntime>();
        runtime.robot = robot;
        runtime.skin = skin;
        runtime.clip = clip;
        PlayerSettings.companyName = "Local Tennis Project";
        PlayerSettings.productName = "Tennis Preflight";
        PlayerSettings.defaultScreenWidth = 960;
        PlayerSettings.defaultScreenHeight = 600;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        QualitySettings.vSyncCount = 0;
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        File.WriteAllText(Path.Combine(Evidence, "editor-import.json"), JsonUtility.ToJson(new ImportReceipt {
            unityVersion = Application.unityVersion, vertices = skin.sharedMesh.vertexCount,
            bones = skin.bones.Length, clips = clips.Length, clipSeconds = clip.length,
            heightMetres = size.y, sampledVertexDelta = delta, materials = skin.sharedMaterials.Length,
            renderingPipeline = GraphicsSettings.currentRenderPipeline?.GetType().Name ?? "Built-in",
            passed = true
        }, true));
        Debug.Log("PREFLIGHT_IMPORT_PASS: " + delta + " metres animated displacement");
    }

    static Material MakeMaterial(string name, Color color)
    {
        Directory.CreateDirectory("Assets/Preflight/Materials");
        string path = "Assets/Preflight/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", .3f);
        return m;
    }

    [MenuItem("Preflight/Build Windows")]
    public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Windows/TennisPreflight.exe");
    [MenuItem("Preflight/Build WebGL")]
    public static void BuildWebGL() => Build(BuildTarget.WebGL, "WebGL");
    static void Build(BuildTarget target, string relative)
    {
        Directory.CreateDirectory(Evidence);
        string path = Path.Combine(Root, "Builds", relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { ScenePath }, locationPathName = path, target = target,
            // Pipeline's development-only hot-reload preservation pulls unused TLS code
            // into WebGL. Browser players use a normal build and retain our runtime checks.
            options = target == BuildTarget.WebGL ? BuildOptions.None : BuildOptions.Development
        });
        File.WriteAllText(Path.Combine(Evidence, "build-" + target + ".json"), JsonUtility.ToJson(new BuildReceipt {
            target = target.ToString(), result = report.summary.result.ToString(),
            errors = report.summary.totalErrors, warnings = report.summary.totalWarnings,
            bytes = (long)report.summary.totalSize, seconds = report.summary.totalTime.TotalSeconds
        }, true));
        Require(report.summary.result == BuildResult.Succeeded, "Build failed: " + target);
        Debug.Log("PREFLIGHT_BUILD_PASS: " + target);
    }
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    [Serializable] class ImportReceipt {
        public bool passed; public string unityVersion, renderingPipeline;
        public int vertices, bones, clips, materials; public float clipSeconds, heightMetres, sampledVertexDelta;
    }
    [Serializable] class BuildReceipt {
        public string target, result; public int errors, warnings; public long bytes; public double seconds;
    }
}
