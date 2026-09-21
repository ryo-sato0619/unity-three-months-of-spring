using TMPro;
using UnityEngine;

namespace ThreeMonthsOfSpring
{
    /// <summary>
    /// 日本語が表示できる TMP フォントアセットを供給する。
    ///
    /// TMP の既定フォント (LiberationSans) には日本語グリフが無く、
    /// そのままでは全文字が豆腐になるため、次の順で解決する。
    ///
    ///   1. <see cref="Override"/>（コードから明示指定された場合）
    ///   2. Resources/Fonts の同梱フォント（Noto Sans JP）
    ///   3. OS にインストールされているフォント
    ///
    /// 2 を先に見るのは、Android や WebGL では 3 が使えないため。
    /// 開発機では 3 でも足りるが、配布ビルドでは同梱フォントが無いと日本語が出ない。
    /// </summary>
    public static class JapaneseFontProvider
    {
        /// <summary>同梱フォントの置き場所（Resources からの相対パス、拡張子なし）。</summary>
        private const string BundledFontResourcePath = "Fonts/NotoSansJP SDF";

        /// <summary>明示的に使いたいフォントアセットがある場合に設定する。</summary>
        public static TMP_FontAsset Override { get; set; }

        /// <summary>試行するフォントファミリ名。上から順に、最初に見つかったものを使う。</summary>
        private static readonly string[] CandidateFamilies =
        {
            // Windows
            "Yu Gothic UI",
            "Yu Gothic",
            "Meiryo",
            "MS Gothic",
            // macOS
            "Hiragino Sans",
            "Hiragino Kaku Gothic ProN",
            // Linux / 手動インストール
            "Noto Sans JP",
            "Noto Sans CJK JP",
            "IPAGothic",
        };

        private static readonly string[] CandidateStyles = { "Regular", "Medium", "Book", "" };

        private static TMP_FontAsset cached;
        private static bool resolved;

        /// <summary>
        /// 日本語が表示できるフォントアセットを返す。見つからない場合は null。
        /// 一度解決した結果はキャッシュされる。
        /// </summary>
        public static TMP_FontAsset Get()
        {
            if (Override != null)
            {
                return Override;
            }

            if (resolved)
            {
                return cached;
            }

            resolved = true;

            // 同梱フォントを優先する。どの環境でも同じ見た目になり、
            // OS のフォントが使えない Android / WebGL でも成立するため。
            var bundled = Resources.Load<TMP_FontAsset>(BundledFontResourcePath);
            if (bundled != null)
            {
                cached = bundled;
                return cached;
            }

            Debug.Log(
                $"[フォント] 同梱フォントが見つからないため OS のフォントを探します: " +
                $"Resources/{BundledFontResourcePath}");

            foreach (string family in CandidateFamilies)
            {
                foreach (string style in CandidateStyles)
                {
                    TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(family, style);
                    if (asset == null)
                    {
                        continue;
                    }

                    asset.name = $"JP Dynamic ({family})";
                    // 生成直後のアセットがシーン遷移や GC で消えないようにする。
                    Object.DontDestroyOnLoad(asset);
                    cached = asset;
                    return cached;
                }
            }

            Debug.LogError(
                "日本語フォントが見つかりませんでした。OS に日本語フォントが無いか、" +
                "フォント名が候補リストに含まれていません。" +
                "JapaneseFontProvider.Override に手動でフォントアセットを設定してください。");
            return null;
        }

        /// <summary>指定した TMP_Text に日本語フォントを適用する。</summary>
        public static void Apply(TMP_Text target)
        {
            if (target == null)
            {
                return;
            }

            TMP_FontAsset font = Get();
            if (font != null)
            {
                target.font = font;
            }
        }
    }
}
