using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Multiplayer Play Mode renames the product to "<name> (Client)" for its scenario build and never restores it; this puts the project's name back.
[InitializeOnLoad]
public class ProductNameGuard : IPostprocessBuildWithReport
{
    private const string ProductName = "ft_transcendence";
    private const string ScenarioSuffix = " (Client)";

    public int callbackOrder => int.MaxValue;

    private static void Restore()
    {
        if (PlayerSettings.productName == ProductName + ScenarioSuffix)
            PlayerSettings.productName = ProductName;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            Restore();
    }

    // Deferred so it runs after the scenario tooling finishes its own post-build steps.
    public void OnPostprocessBuild(BuildReport report)
    {
        EditorApplication.delayCall += Restore;
    }

    static ProductNameGuard()
    {
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        EditorApplication.delayCall += Restore;
    }
}
