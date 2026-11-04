using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AshenRoad.EditorTools
{
    /// <summary>Command-line builds for Linux, Windows and macOS. Used by build_linux.sh, build_windows.sh and build_mac.sh.</summary>
    public static class BuildScript
    {
        /// <summary>Reads "-buildFolder <path>" from the command line so a new build can sit beside an old one.</summary>
        static string OutputPath(string fallback)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-buildFolder") return args[i + 1];
            return fallback;
        }

        [MenuItem("Ashen Road/Build Linux Player")]
        public static void BuildLinux() => Build(BuildTarget.StandaloneLinux64, OutputPath("Builds/Linux") + "/AshenRoad.x86_64");

        [MenuItem("Ashen Road/Build Windows Player")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, OutputPath("Builds/Windows") + "/AshenRoad.exe");

        [MenuItem("Ashen Road/Build macOS Player")]
        public static void BuildMac()
        {
            // one app for both Apple Silicon and Intel Macs
            EditorUserBuildSettings.SetPlatformSettings("OSXUniversal", "Architecture", "x64ARM64");
            Build(BuildTarget.StandaloneOSX, OutputPath("Builds/macOS") + "/AshenRoad.app");
        }

        static void Build(BuildTarget target, string location)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
            {
                Debug.LogError($"Ashen Road build: {target} support is not installed. Add it in Unity Hub > Installs > Add modules.");
                if (Application.isBatchMode) EditorApplication.Exit(2);
                return;
            }
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = location,
                target = target,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"Ashen Road build ({target}): {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} errors");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
