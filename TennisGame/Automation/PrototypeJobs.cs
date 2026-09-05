using UnityEditor;
using UnityEngine;
using RoboOpen;

public static class PrototypeJobs
{
    public static string Prepare(){PrototypeSetup.Prepare();return "Prototype scene and rules checks completed.";}
    public static string BuildWindows()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return "Not queued: Editor busy.";
        EditorApplication.delayCall+=PrototypeSetup.BuildWindows;return "Windows prototype build queued.";
    }
    public static string PlayDemo()
    {
        var game=Object.FindAnyObjectByType<RoboOpen.TennisGame>();game.autoplay=true;game.BeginMatch();return "Autoplay started through normal match simulation.";
    }
    public static string InputChecks()
    {
        var g=Object.FindAnyObjectByType<RoboOpen.TennisGame>();g.ReturnToMenu();
        var test=g.gameObject.AddComponent<PrototypeInputChecks>();test.game=g;test.output=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Evidence/Prototype"));
        g.StartCoroutine(test.Run());return "Input checks running.";
    }
    public static string Receipt()=>JsonUtility.ToJson(Object.FindAnyObjectByType<RoboOpen.TennisGame>().Receipt());
    public static string Status()
    {
        var g=Object.FindAnyObjectByType<RoboOpen.TennisGame>();var c=Object.FindAnyObjectByType<Canvas>();
        return "screen="+Screen.width+"x"+Screen.height+" cameraAspect="+g.gameCamera.aspect+" canvas="+((RectTransform)c.transform).rect+" phase="+g.phase+" paused="+g.paused+" ball="+g.ball.position+" player="+g.player.transform.position+" cpu="+g.cpu.transform.position+" aim="+g.aimX+" receipt="+JsonUtility.ToJson(g.Receipt());
    }
    public static string Capture()
    {
        var game=Object.FindAnyObjectByType<RoboOpen.TennisGame>();game.Capture(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Evidence/Prototype/editor-prototype.png")));return "Captured.";
    }
}
