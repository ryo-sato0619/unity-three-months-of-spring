using System.Collections.Generic;
using UnityEngine;

namespace ThreeMonthsOfSpring
{
    /// <summary>
    /// 背景スプライトの供給元。
    ///
    /// <c>Assets/Resources/Backgrounds/&lt;キー&gt;</c> に画像があればそれを使い、
    /// 無ければ <see cref="BackgroundPalette"/> の色グラデーションで代用する。
    /// 素材が未配置でも成立させつつ、画像を置いた時点で自動的に切り替わる。
    /// </summary>
    public static class BackgroundProvider
    {
        private const string ResourceFolder = "Backgrounds/";

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>キーに対応する背景スプライト。結果はキャッシュされる。</summary>
        public static Sprite Get(string key)
        {
            string safeKey = key ?? string.Empty;

            if (Cache.TryGetValue(safeKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            var texture = Resources.Load<Texture2D>(ResourceFolder + safeKey);
            bool fromImage = texture != null;

            if (!fromImage)
            {
                texture = BackgroundPalette.CreateTexture(safeKey);
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = $"bg_{safeKey}{(fromImage ? string.Empty : "_fallback")}";

            Cache[safeKey] = sprite;
            return sprite;
        }

        /// <summary>そのキーに実画像が存在するか（グラデーション代用かどうかの判定用）。</summary>
        public static bool HasImage(string key)
        {
            return Resources.Load<Texture2D>(ResourceFolder + (key ?? string.Empty)) != null;
        }
    }
}
