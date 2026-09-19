using UnityEditor;
using UnityEngine;

namespace ThreeMonthsOfSpring.EditorTools
{
    /// <summary>
    /// Resources 以下の画像に適切なインポート設定を自動で当てる。
    ///
    /// 立ち絵は Sprite として、かつ「足元が原点」になるようピボットを下中央に置く。
    /// これをやっておかないと、身長や構図の違う立ち絵を差し替えるたびに
    /// 位置がずれて手で直すことになる。
    /// </summary>
    public sealed class TextureImportRules : AssetPostprocessor
    {
        private const string SpritesPath = "Assets/Resources/Sprites/";
        private const string BackgroundsPath = "Assets/Resources/Backgrounds/";

        private void OnPreprocessTexture()
        {
            var importer = (TextureImporter)assetImporter;

            if (assetPath.StartsWith(SpritesPath))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;

                // 足元を基準にする。立ち絵の縦横比が変わっても接地位置が動かない。
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                importer.SetTextureSettings(settings);
            }
            else if (assetPath.StartsWith(BackgroundsPath))
            {
                // 背景は画面いっぱいに一度だけ描くので、ミップマップは不要。
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
            }
        }
    }
}
