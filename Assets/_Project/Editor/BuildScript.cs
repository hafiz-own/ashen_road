using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AshenRoad.EditorTools
{
    /// <summary>Command-line builds. Used by build_linux.sh.</summary>
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
        public static void BuildLinux()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath("Builds/Linux") + "/AshenRoad.x86_64",
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"Ashen Road build: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} errors");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
