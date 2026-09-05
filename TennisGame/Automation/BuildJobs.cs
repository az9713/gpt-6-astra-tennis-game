using UnityEditor;
public static class PreflightBuildJobs
{
    public static string QueueWindows()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return "Not queued: wait for Editor compilation/import to finish, then retry.";
        EditorApplication.delayCall += PreflightSetup.BuildWindows;
        return "Windows build queued; inspect Evidence/build-StandaloneWindows64.json for completion.";
    }
    public static string QueueWebGL()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return "Not queued: wait for Editor compilation/import to finish, then retry.";
        EditorApplication.delayCall += PreflightSetup.BuildWebGL;
        return "WebGL build queued; inspect Evidence/build-WebGL.json for completion.";
    }
}
