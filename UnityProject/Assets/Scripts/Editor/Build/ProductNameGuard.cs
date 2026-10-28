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

    static ProductNameGuard()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                Restore();
        };
        EditorApplication.delayCall += Restore;
    }

    // Deferred so it runs after the scenario tooling finishes its own post-build steps.
    public void OnPostprocessBuild(BuildReport report)
    {
        EditorApplication.delayCall += Restore;
    }

    private static void Restore()
    {
        if (PlayerSettings.productName == ProductName + ScenarioSuffix)
            PlayerSettings.productName = ProductName;
    }
}
