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
            // 出力フォルダ名がそのまま Build/ 以下のファイル名になる。
            // 非 ASCII のままだと配信先によって URL の解釈が揺れるため、英字にする。
            string directory = Path.Combine(OutputRoot, ProductName + "_WebGL");
            Directory.CreateDirectory(directory);

            // Brotli で圧縮し、展開はローダー側の JavaScript に任せる。
            // GitHub Pages のような静的ホスティングは .br に Content-Encoding を
            // 付けられないが、フォールバックを有効にしておけばサーバ設定に
            // 依存せず動く。無圧縮だと 77MB、Brotli なら大幅に小さくなる。
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;

            PlayerSettings.defaultWebScreenWidth = 1920;
            PlayerSettings.defaultWebScreenHeight = 1080;

            // 自作テンプレート。Unity 既定のテンプレートはデスクトップで
            // キャンバスを 1920x1080 に固定するため、小さいウィンドウで見切れる。
            // またセーブの永続化 (autoSyncPersistentDataPath) も既定では無効。
            PlayerSettings.WebGL.template = "PROJECT:Responsive";

            // 実行時のエラーをブラウザのコンソールに出す。配布先での切り分け用。
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

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
