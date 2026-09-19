using TMPro;
using UnityEngine;

namespace ThreeMonthsOfSpring
{
    /// <summary>
    /// OS にインストールされている日本語フォントから TMP のフォントアセットを実行時に生成する。
    ///
    /// フォントファイル自体をリポジトリに含めないための仕組み。TMP の既定フォント
    /// (LiberationSans) には日本語グリフが無く、そのままでは全文字が豆腐になる。
    /// AtlasPopulationMode.DynamicOS で生成されるため、グリフは必要になった時点で
    /// OS のフォントから取り込まれる。
    ///
    /// 配布ビルドを作る場合は、ライセンスの明確なフォント (Noto Sans JP など) を
    /// 同梱して <see cref="Override"/> に差し替えることを推奨する。
    /// </summary>
    public static class JapaneseFontProvider
    {
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
