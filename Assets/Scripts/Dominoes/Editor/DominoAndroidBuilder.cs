#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Dominoes
{
    public static class DominoAndroidBuilder
    {
        [MenuItem("Dominoes/Build Android APK")]
        public static void BuildAndroidAPK()
        {
            string buildDirectory = "Builds/Android";
            if (!Directory.Exists(buildDirectory))
            {
                Directory.CreateDirectory(buildDirectory);
            }

            string apkPath = Path.Combine(buildDirectory, "Dominoes_MVP_Prototype.apk");
            string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log($"[DominoAndroidBuilder] Starting Android APK build to: {apkPath}");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=green>[DominoAndroidBuilder] Build SUCCESS! Size: {summary.totalSize / (1024 * 1024):F1} MB</color>");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"<color=red>[DominoAndroidBuilder] Build FAILED with {summary.totalErrors} errors!</color>");
            }
        }
    }
}
#endif
