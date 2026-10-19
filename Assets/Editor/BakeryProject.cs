using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RainyDayBakery;

public static class BakeryProject
{
    [MenuItem("Rainy Day Bakery/Build Windows")]
    public static void BuildWindows()
    {
        var result = BuildPipeline.BuildPlayer(new[] { "Assets/Scenes/Bakery.unity" }, "Builds/Windows/RainyDayBakery.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("Build failed: " + result.summary.result);
    }

    [MenuItem("Rainy Day Bakery/Verify Gameplay Rules")]
    public static void VerifyRules()
    {
        Check(BakeryRules.Quality(0) == "Soft", "Early removal");
        Check(BakeryRules.Quality(5.99f) == "Soft", "Before golden window");
        Check(BakeryRules.Quality(6) == "Golden", "Golden starts at six seconds");
        Check(BakeryRules.Quality(10) == "Golden", "Golden includes ten seconds");
        Check(BakeryRules.Quality(10.01f) == "Toasty", "Late removal");
        Check(BakeryRules.Reward(0, 0, "Golden") == 17, "Perfect order");
        Check(BakeryRules.Reward(1, 0, "Soft") == 8, "Alternative bread still earns shells");
        Check(BakeryRules.Reward(2, 2, "Toasty") == 13, "Toasty bread never fails");
        Debug.Log("Rainy Day Bakery: all 8 gameplay rule checks passed.");
    }
    static void Check(bool condition, string message)
    { if (!condition) throw new System.Exception("Rule check failed: " + message); }
}
