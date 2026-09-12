using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace CrystalRush.Editor
{
    public static class CrystalRushBuild
    {
        [MenuItem("Crystal Rush/Build Windows Test Player")]
        public static void Build()
        {
            const string scene = CrystalRushSetup.ArenaScenePath;
            if (!File.Exists(scene)) CrystalRushSetup.CreateArena();
            Directory.CreateDirectory("Builds/CrystalRush");
            PlayerSettings.productName = "Crystal Rush";
            PlayerSettings.defaultIsFullScreen = false;
            PlayerSettings.fullScreenMode = UnityEngine.FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 800;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scene }, locationPathName = "Builds/CrystalRush/CrystalRush.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Crystal Rush build failed: " + report.summary.result);
            UnityEngine.Debug.Log("CRYSTAL_RUSH_BUILD_OK");
        }
    }
}
