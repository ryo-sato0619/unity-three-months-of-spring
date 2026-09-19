using System.Collections.Generic;
using UnityEngine;

namespace ThreeMonthsOfSpring
{
    /// <summary>
    /// ink の <c>#bg:</c> タグを背景のグラデーションに対応づける。
    ///
    /// 背景画像をまだ用意していないため、場面ごとの時間帯・空気感を色だけで表現している。
    /// 実際の背景画像を入れる場合は <see cref="NovelGameController"/> の背景差し替え処理を
    /// Sprite の読み込みに置き換えればよい。
    /// </summary>
    public static class BackgroundPalette
    {
        private readonly struct Gradient
        {
            public readonly Color Top;
            public readonly Color Bottom;

            public Gradient(string top, string bottom)
            {
                Top = Parse(top);
                Bottom = Parse(bottom);
            }

            private static Color Parse(string hex)
            {
                return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
            }
        }

        private static readonly Gradient Fallback = new Gradient("#2B2F38", "#151821");

        private static readonly Dictionary<string, Gradient> Gradients = new Dictionary<string, Gradient>
        {
            // 早朝の通勤路。澄んだ空気、冷たい光。
            ["street_morning"] = new Gradient("#A9C7E8", "#E6E1D6"),
            // 老舗のオフィス。蛍光灯と古い家具のくすんだ白。
            ["office_day"] = new Gradient("#D8D6CE", "#AFACA2"),
            // 夕方のフロア。窓から差し込む斜光。
            ["office_evening"] = new Gradient("#E0A96D", "#7A5C55"),
            // 残業の時間帯。モニターの光だけが残る。
            ["office_night"] = new Gradient("#2E3647", "#141821"),
            // 昼の定食屋。暖色の照明と木のカウンター。
            ["restaurant_noon"] = new Gradient("#E8C89A", "#B08355"),
            // 居酒屋。赤提灯とざわめき。
            ["izakaya_night"] = new Gradient("#8C4A3C", "#2A1A18"),
            // 会議室。感情の無い青白い壁。
            ["meeting_room"] = new Gradient("#BFC7D1", "#8A939F"),
            // 昼の公園。秋晴れ。
            ["park_noon"] = new Gradient("#9FD0F0", "#BBD4A0"),
            // 夕暮れの帰り道。
            ["street_evening"] = new Gradient("#F0A878", "#5C4A5E"),
            // 夜道。
            ["street_night"] = new Gradient("#1E2740", "#0B0E16"),
        };

        /// <summary>タグ名に対応する縦グラデーションのテクスチャを生成して返す。</summary>
        public static Texture2D CreateTexture(string key)
        {
            Gradient g = key != null && Gradients.TryGetValue(key, out Gradient found) ? found : Fallback;

            const int height = 64;
            var texture = new Texture2D(1, height, TextureFormat.RGBA32, false)
            {
                name = $"bg_{key}",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };

            for (int y = 0; y < height; y++)
            {
                // y=0 が下端。
                float t = y / (float)(height - 1);
                texture.SetPixel(0, y, Color.Lerp(g.Bottom, g.Top, t));
            }

            texture.Apply();
            return texture;
        }

        /// <summary>タグ名が既知かどうか。未知のタグはログで気付けるようにする。</summary>
        public static bool IsKnown(string key)
        {
            return key != null && Gradients.ContainsKey(key);
        }
    }
}
