using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ThreeMonthsOfSpring.EditorTools
{
    /// <summary>
    /// 配布用のビルドをまとめて行う。
    ///
    /// コマンドラインから叩けるようにしてあるのは、ビルド設定を手作業ではなく
    /// コードとして残すため。設定を変えたい場合はこのファイルを直す。
    /// 出力先は既定でデスクトップ。環境変数 TMS_BUILD_DIR があればそちらを使う。
    /// </summary>
    public static class BuildTool
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string ProductName = "three-months-of-spring";
        private const string ApplicationIdentifier = "com.ryosato0619.threemonthsofspring";

        private static string OutputRoot
        {
            get
            {
                string fromEnvironment = Environment.GetEnvironmentVariable("TMS_BUILD_DIR");
                if (!string.IsNullOrEmpty(fromEnvironment))
                {
                    return fromEnvironment;
                }

                return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            }
        }

        [MenuItem("Tools/三か月の春/ビルド/Windows")]
        public static void BuildWindows()
        {
            string directory = Path.Combine(OutputRoot, "三か月の春_Windows");
            Directory.CreateDirectory(directory);

            Run(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(directory, ProductName + ".exe"),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            });
        }

        [MenuItem("Tools/三か月の春/ビルド/Android (APK)")]
        public static void BuildAndroid()
        {
            PlayerSettings.SetApplicationIdentifier(
                UnityEditor.Build.NamedBuildTarget.Android, ApplicationIdentifier);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(
                UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

            // 端末に直接入れて試す用なので、AAB ではなく APK を出す。
            EditorUserBuildSettings.buildAppBundle = false;

            // 縦持ちの端末で遊ぶ想定ではないため横固定にする。
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            Directory.CreateDirectory(OutputRoot);

            Run(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(OutputRoot, "三か月の春.apk"),
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            });
        }

        [MenuItem("Tools/三か月の春/ビルド/WebGL")]
        public static void BuildWebGL()
        {
            string directory = Path.Combine(OutputRoot, "三か月の春_WebGL");
            Directory.CreateDirectory(directory);

            // 圧縮を切っておくと、サーバ側の設定に関係なく配信できる。
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

            Run(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = directory,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            });
        }

        private static void Run(BuildPlayerOptions options)
        {
            PlayerSettings.companyName = "ryo-sato0619";
            PlayerSettings.productName = ProductName;

            Debug.Log($"[Build] 開始: {options.target} -> {options.locationPathName}");

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log(
                    $"[Build] 成功: {options.target} / " +
                    $"{summary.totalSize / 1048576f:0.0} MB / {summary.totalTime.TotalSeconds:0} 秒\n" +
                    $"出力: {options.locationPathName}");
                return;
            }

            Debug.LogError($"[Build] 失敗: {options.target} / 結果 {summary.result}");
            foreach (BuildStep step in report.steps)
            {
                foreach (BuildStepMessage message in step.messages)
                {
                    if (message.type == LogType.Error || message.type == LogType.Exception)
                    {
                        Debug.LogError($"[Build] {step.name}: {message.content}");
                    }
                }
            }
        }
    }
}
