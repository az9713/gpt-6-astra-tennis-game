using UnityEditor;
public static class RefreshPrototype
{
    public static string Run(){AssetDatabase.Refresh();return "Asset refresh requested.";}
}
