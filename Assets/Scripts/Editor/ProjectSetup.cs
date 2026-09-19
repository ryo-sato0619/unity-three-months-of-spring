using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThreeMonthsOfSpring.EditorTools
{
    /// <summary>
    /// プロジェクトの初期セットアップ。CI やコマンドラインからも呼べるようにしてある。
    /// </summary>
    public static class ProjectSetup
    {
        /// <summary>
        /// TextMeshPro の必須リソース (TMP Settings / シェーダ / 既定フォント) を取り込む。
        ///
        /// これが無いと TMP_Text は一切描画できない。取り込んだ結果は
        /// Assets/TextMesh Pro/ に置かれ、リポジトリにコミットする。
        /// clone しただけで動く状態を保つため。
        /// </summary>
        [MenuItem("Tools/三か月の春/1. TMP 必須リソースを取り込む")]
        public static void ImportTmpEssentials()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0)
            {
                Debug.Log("[Setup] TMP 必須リソースは既に取り込み済みです。");
                return;
            }

            string packagePath = Path.Combine(
                TMPro.EditorUtilities.TMP_EditorUtility.packageFullPath,
                "Package Resources/TMP Essential Resources.unitypackage");

            if (!File.Exists(packagePath))
            {
                Debug.LogError($"[Setup] TMP のリソースパッケージが見つかりません: {packagePath}");
                return;
            }

            Debug.Log($"[Setup] TMP 必須リソースを取り込みます: {packagePath}");
            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh();
        }
    }
}
