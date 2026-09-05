using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public static class PreflightCapture
{
    public static string Capture()
    {
        var camera = Camera.main;
        var target = new RenderTexture(960, 600, GraphicsFormat.R8G8B8A8_SRGB, GraphicsFormat.D24_UNorm_S8_UInt);
        var previous = RenderTexture.active;
        var pixels = new Texture2D(960, 600, TextureFormat.RGB24, false);
        try
        {
            var request = new RenderPipeline.StandardRequest { destination = target };
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 960, 600), 0, 0);
            pixels.Apply();
            var output = Path.GetFullPath("../Evidence/editor-urp.png");
            File.WriteAllBytes(output, pixels.EncodeToPNG());
            return output;
        }
        finally
        {
            RenderTexture.active = previous;
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(pixels);
        }
    }
}
