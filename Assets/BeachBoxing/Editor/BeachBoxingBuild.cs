using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BeachBoxing.Editor
{
    internal static class BeachBoxingBuild
    {
        [MenuItem("Tools/Beach Boxing/Build WebGL Smoke")]
        public static void BuildWebGl()
        {
            const string entry = "Assets/GameFrame/Init.unity";
            const string play = "Assets/BeachBoxing/Game.unity";
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length != 2 || scenes[0] != entry || scenes[1] != play)
                throw new InvalidOperationException("Enable Init as index 0 and Assets/BeachBoxing/Game.unity as index 1 in Build Settings.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                throw new InvalidOperationException("Select WebGL in Build Settings before running the smoke build.");
            var compression = PlayerSettings.WebGL.compressionFormat;
            try
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                var report = BuildPipeline.BuildPlayer(scenes, "Builds/BeachBoxingWebGL", BuildTarget.WebGL, BuildOptions.None);
                if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
                    throw new InvalidOperationException($"Beach Boxing WebGL build: {report.summary.result}.");
                Debug.Log($"Beach Boxing WebGL smoke build succeeded ({report.summary.totalSize:N0} bytes).");
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = compression;
                AssetDatabase.SaveAssets();
            }
        }
    }
}
