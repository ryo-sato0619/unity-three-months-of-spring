using System.Collections.Generic;
using UnityEngine;

namespace ThreeMonthsOfSpring
{
    /// <summary>
    /// 立ち絵の供給元。
    ///
    /// <c>Assets/Resources/Sprites/&lt;キー&gt;</c> から読む。ink の <c>#sprite:</c> タグの値がキー。
    /// ファイルが無ければ null を返し、立ち絵なしで進行する（素材が未配置でもゲームは成立する）。
    ///
    /// 画像は Unity の Sprite として読む。Resources 直下の画像は
    /// <c>CharacterSpriteImporter</c> が自動的に Sprite 設定でインポートする。
    /// </summary>
    public static class CharacterSpriteProvider
    {
        public const string ResourceFolder = "Sprites/";

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        private static Sprite[] allSprites;

        private static Sprite[] AllSprites =>
            allSprites ??= Resources.LoadAll<Sprite>(ResourceFolder.TrimEnd('/'));

        /// <summary>立ち絵を隠すことを表すキー。</summary>
        public static bool IsHideKey(string key)
        {
            return string.IsNullOrEmpty(key) || key == "none" || key == "なし";
        }

        /// <summary>キーに対応する立ち絵。無ければ null。結果はキャッシュされる。</summary>
        public static Sprite Get(string key)
        {
            if (IsHideKey(key))
            {
                return null;
            }

            if (Cache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>(ResourceFolder + key);

            if (sprite == null)
            {
                // Sprite としてインポートされていない場合に備えて、テクスチャからも試す。
                var texture = Resources.Load<Texture2D>(ResourceFolder + key);
                if (texture != null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0f),
                        100f);
                    sprite.name = key;
                }
            }

            // 目的の表情が無ければ、同じキャラクターの別の表情で代用する。
            // 表情を1枚ずつ足していく途中でも、立ち絵が消えたり現れたりしないようにするため。
            if (sprite == null)
            {
                sprite = FindSubstitute(key);
            }

            if (sprite != null)
            {
                Cache[key] = sprite;
            }

            return sprite;
        }

        /// <summary>
        /// "haruka_troubled" が無いときに "haruka_smile" などで代用する。
        /// キーの "_" より前をキャラクター名とみなす。
        /// </summary>
        private static Sprite FindSubstitute(string key)
        {
            int separator = key.IndexOf('_');
            string character = separator > 0 ? key.Substring(0, separator) : key;

            foreach (Sprite candidate in AllSprites)
            {
                if (candidate != null && candidate.name.StartsWith(character))
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>そのキーの立ち絵が、代用ではなく実ファイルとして存在するか。</summary>
        public static bool ExistsExactly(string key)
        {
            if (IsHideKey(key))
            {
                return false;
            }

            return Resources.Load<Sprite>(ResourceFolder + key) != null
                || Resources.Load<Texture2D>(ResourceFolder + key) != null;
        }

        /// <summary>そのキーで何かしら表示できるか（代用を含む）。</summary>
        public static bool Exists(string key)
        {
            return !IsHideKey(key) && Get(key) != null;
        }
    }
}
