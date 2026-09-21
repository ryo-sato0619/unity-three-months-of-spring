using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ThreeMonthsOfSpring.EditorTools
{
    /// <summary>
    /// 同梱フォントから TextMeshPro のフォントアセットを生成する。
    ///
    /// 実行時に OS のフォントを読む方式（<see cref="JapaneseFontProvider"/>）は
    /// 開発機では問題ないが、Android や WebGL では日本語が出ない。
    /// 配布ビルドのために、ライセンスの明確なフォントを同梱してアセット化する。
    ///
    /// 生成モードは Dynamic。CJK は字数が多く、全グリフを焼き込むと
    /// アトラスが現実的な大きさに収まらないため、必要になった文字だけを
    /// 実行時にアトラスへ足す。元の otf はビルドに含まれる。
    /// </summary>
    public static class FontAssetTool
    {
        private const string SourceFontPath = "Assets/Fonts/NotoSansJP-Regular.otf";
        private const string OutputPath = "Assets/Resources/Fonts/NotoSansJP SDF.asset";

        [MenuItem("Tools/三か月の春/4. 日本語フォントアセットを生成する")]
        public static void CreateJapaneseFontAsset()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (font == null)
            {
                Debug.LogError($"[フォント] 元のフォントが見つかりません: {SourceFontPath}");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath)!);

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath) != null)
            {
                AssetDatabase.DeleteAsset(OutputPath);
            }

            FontEngine.InitializeFontEngine();

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 1024,
                atlasHeight: 1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            if (fontAsset == null)
            {
                Debug.LogError($"[フォント] アセットの生成に失敗しました: {SourceFontPath}");
                return;
            }

            fontAsset.name = Path.GetFileNameWithoutExtension(OutputPath);
            AssetDatabase.CreateAsset(fontAsset, OutputPath);

            // アトラスとマテリアルはフォントアセットの子として保存する。
            // 別ファイルにすると参照が切れやすい。
            if (fontAsset.atlasTextures is { Length: > 0 } && fontAsset.atlasTextures[0] != null)
            {
                fontAsset.atlasTextures[0].name = fontAsset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Atlas Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[フォント] 生成しました: {OutputPath}\n" +
                $"  元フォント: {font.name}\n" +
                $"  生成モード: Dynamic（必要な文字を実行時にアトラスへ追加）");
        }
    }
}
