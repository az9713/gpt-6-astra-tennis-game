using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class PreflightRuntime : MonoBehaviour
{
    public GameObject robot;
    public SkinnedMeshRenderer skin;
    public AnimationClip clip;
    Animation animator;
    bool paused;
    string status = "Checking animation...";
    float displacement;
    int interactions;
    [Serializable] class Receipt {
        public bool passed; public string platform, graphicsDevice, status;
        public float animatedVertexDelta; public int interactions;
    }
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void PreflightReport(string json);
#endif
    IEnumerator Start()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
        animator = robot.GetComponent<Animation>();
        animator.cullingType = AnimationCullingType.AlwaysAnimate;
        skin.updateWhenOffscreen = true;
        animator.AddClip(clip, "Swing");
        animator.wrapMode = WrapMode.Loop;
        animator.Play("Swing");
        yield return new WaitForSeconds(.12f);
        var mesh = new Mesh();
        skin.BakeMesh(mesh);
        var first = mesh.vertices;
        yield return new WaitForSeconds(.3f);
        skin.BakeMesh(mesh);
        displacement = first.Zip(mesh.vertices, (a, b) => Vector3.Distance(a, b)).Max();
        Destroy(mesh);
        status = displacement > .05f ? "PASS: animated Blender model running" : "FAIL: skin did not move";
        Publish();
        if (Application.platform != RuntimePlatform.WebGLPlayer && !Application.isEditor)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--preflight-output");
            if (index >= 0 && index + 1 < args.Length)
            {
                string output = args[index + 1];
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "windows-runtime.json"), JsonUtility.ToJson(GetReceipt(), true));
                yield return new WaitForSeconds(1);
                Capture(Path.Combine(output, "windows-runtime.png"));
                yield return new WaitForSeconds(2);
            }
            if (args.Contains("--preflight-autoquit")) Application.Quit(displacement > .05f ? 0 : 1);
        }
    }
    Receipt GetReceipt() => new Receipt { passed = displacement > .05f, platform = Application.platform.ToString(),
        graphicsDevice = SystemInfo.graphicsDeviceName, animatedVertexDelta = displacement, status = status, interactions = interactions };
    void Capture(string path)
    {
        var target = new RenderTexture(960, 600, GraphicsFormat.R8G8B8A8_SRGB, GraphicsFormat.D24_UNorm_S8_UInt);
        var previous = RenderTexture.active;
        var pixels = new Texture2D(960, 600, TextureFormat.RGB24, false);
        try
        {
            RenderPipeline.SubmitRenderRequest(Camera.main, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 960, 600), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            target.Release();
            Destroy(target);
            Destroy(pixels);
        }
    }
    void Publish()
    {
        string json = JsonUtility.ToJson(GetReceipt());
        Debug.Log("PREFLIGHT_RUNTIME " + json);
#if UNITY_WEBGL && !UNITY_EDITOR
        PreflightReport(json);
#endif
    }
    void OnGUI()
    {
        GUI.skin.label.fontSize = 20;
        GUI.skin.button.fontSize = 18;
        GUI.Box(new Rect(16, 16, 620, 112), "");
        GUI.Label(new Rect(30, 25, 580, 30), "TENNIS / BLENDER → UNITY PRE-FLIGHT");
        GUI.Label(new Rect(30, 63, 580, 30), status);
        GUI.Label(new Rect(30, 94, 580, 25), "Two-bone rig · imported materials · looping racket swing");
        if (GUI.Button(new Rect(20, Screen.height - 64, 220, 44), paused ? "Resume animation" : "Pause animation"))
        {
            paused = !paused;
            animator["Swing"].speed = paused ? 0 : 1;
            interactions++;
            status = paused ? "PASS: animation paused" : "PASS: animation resumed";
            Publish();
        }
        if (GUI.Button(new Rect(255, Screen.height - 64, 180, 44), "Rotate model"))
        {
            robot.transform.Rotate(0, 45, 0);
            interactions++;
            Publish();
        }
    }
}
