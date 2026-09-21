using UnityEngine;

namespace ThreeMonthsOfSpring
{
    /// <summary>
    /// プレイヤーが設定画面で変える値。すべて PlayerPrefs に保存され、次回起動でも残る。
    ///
    /// 既定値をここに集めているのは、シーンをコードから生成している以上
    /// Inspector が設定の置き場所として機能しないため。
    /// </summary>
    public static class GameSettings
    {
        private const string TypingKey = "tms.settings.typing";
        private const string AutoKey = "tms.settings.auto";
        private const string VolumeKey = "tms.settings.volume";

        // --- 文字送り速度（1秒あたりの文字数） ---
        public const float TypingSpeedMin = 10f;
        public const float TypingSpeedMax = 140f;
        public const float TypingSpeedDefault = 45f;

        /// <summary>1秒あたりに表示する文字数。大きいほど速い。</summary>
        public static float TypingSpeed
        {
            get => Mathf.Clamp(
                PlayerPrefs.GetFloat(TypingKey, TypingSpeedDefault), TypingSpeedMin, TypingSpeedMax);
            set
            {
                PlayerPrefs.SetFloat(TypingKey, Mathf.Clamp(value, TypingSpeedMin, TypingSpeedMax));
                PlayerPrefs.Save();
            }
        }

        // --- オート再生の速さ（0 = ゆっくり、1 = 速い） ---
        public const float AutoSpeedDefault = 0.5f;

        /// <summary>0〜1。待ち時間そのものではなく「速さ」で持つ。設定画面で直感的に扱えるため。</summary>
        public static float AutoSpeed
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(AutoKey, AutoSpeedDefault));
            set
            {
                PlayerPrefs.SetFloat(AutoKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        /// <summary>行の長さに関わらず必ず取る間。</summary>
        public static float AutoBaseSeconds => Mathf.Lerp(2.2f, 0.35f, AutoSpeed);

        /// <summary>1文字あたりに加算する待ち時間。長い行ほど長く表示される。</summary>
        public static float AutoPerCharacterSeconds => Mathf.Lerp(0.10f, 0.02f, AutoSpeed);

        // --- BGM 音量 ---
        public const float BgmVolumeDefault = 0.55f;

        public static float BgmVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, BgmVolumeDefault));
            set
            {
                PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        /// <summary>すべて既定値に戻す。</summary>
        public static void ResetToDefaults()
        {
            TypingSpeed = TypingSpeedDefault;
            AutoSpeed = AutoSpeedDefault;
            BgmVolume = BgmVolumeDefault;
        }
    }
}
