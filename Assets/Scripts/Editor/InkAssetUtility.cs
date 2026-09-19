using Ink.UnityIntegration;
using UnityEditor;
using UnityEngine;

namespace ThreeMonthsOfSpring.EditorTools
{
    /// <summary>
    /// ink アセットの読み込みヘルパ。
    ///
    /// ink パッケージは「どの .ink がマスターファイルか」を InkIncludeGraph が
    /// <c>EditorApplication.delayCall</c> で判定している。この delayCall は
    /// <c>-batchmode -quit</c> では走らないため、バッチ実行やCIでは .ink が
    /// 未コンパイルのままになる。エディタで開けば解決するが、それでは
    /// コマンドラインから検証できないので、ここで明示的に解決する。
    /// </summary>
    public static class InkAssetUtility
    {
        /// <summary>
        /// コンパイル済みの InkFile を返す。未コンパイルなら、マスターファイルとして
        /// 扱うよう importer を設定して再インポートしてから返す。
        /// </summary>
        public static InkFile LoadCompiled(string assetPath)
        {
            var inkFile = AssetDatabase.LoadAssetAtPath<InkFile>(assetPath);
            if (inkFile == null)
            {
                Debug.LogError($"[Ink] アセットが見つかりません: {assetPath}");
                return null;
            }

            if (inkFile.isCompiled || inkFile.hasErrors || inkFile.hasUnhandledCompileErrors)
            {
                return inkFile;
            }

            if (AssetImporter.GetAtPath(assetPath) is not InkImporter importer)
            {
                Debug.LogError($"[Ink] InkImporter が割り当てられていません: {assetPath}");
                return inkFile;
            }

            Debug.Log($"[Ink] 未コンパイルのため、マスターファイルとして再インポートします: {assetPath}");

            var so = new SerializedObject(importer);
            so.FindProperty("isMasterFile").boolValue = true;
            so.FindProperty("compileAsMasterFileOverride").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<InkFile>(assetPath);
        }
    }
}
