using System.Collections;
using UnityEngine;

namespace ThreeMonthsOfSpring
{
    /// <summary>
    /// BGM の再生とクロスフェード。
    ///
    /// 曲は <c>Assets/Resources/Bgm/&lt;キー&gt;</c> から読む。ink の <c>#bgm:</c> タグの値がキー。
    /// ファイルが無ければ何もせず静かに続行する（素材が未配置でもゲームは動く）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioDirector : MonoBehaviour
    {
        private const string MutePrefsKey = "tms.bgm.muted";
        private const string ResourceFolder = "Bgm/";

        [SerializeField] private AudioSource sourceA;
        [SerializeField] private AudioSource sourceB;
        [SerializeField] private float fadeSeconds = 1.2f;
        [SerializeField] private float volume = 0.55f;

        private bool usingA = true;
        private string currentKey;
        private Coroutine fadeRoutine;

        /// <summary>ミュート状態。PlayerPrefs に保存される。</summary>
        public bool Muted
        {
            get => PlayerPrefs.GetInt(MutePrefsKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(MutePrefsKey, value ? 1 : 0);
                PlayerPrefs.Save();
                ApplyMute();
            }
        }

        private AudioSource Active => usingA ? sourceA : sourceB;
        private AudioSource Idle => usingA ? sourceB : sourceA;

        private void Awake()
        {
            foreach (AudioSource source in new[] { sourceA, sourceB })
            {
                source.loop = true;
                source.playOnAwake = false;
                source.volume = 0f;
            }

            ApplyMute();
        }

        /// <summary>
        /// 指定キーの BGM に切り替える。同じ曲が既に鳴っていれば何もしない。
        /// キーが空なら停止する。
        /// </summary>
        public void Play(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Stop();
                return;
            }

            if (key == currentKey && Active.isPlaying)
            {
                return;
            }

            var clip = Resources.Load<AudioClip>(ResourceFolder + key);
            if (clip == null)
            {
                // 素材が未配置。鳴らすものが無いので、鳴っている曲はそのまま続ける。
                Debug.Log($"[BGM] 未配置のためスキップします: Resources/{ResourceFolder}{key}");
                currentKey = key;
                return;
            }

            currentKey = key;
            CrossFadeTo(clip);
        }

        public void Stop()
        {
            currentKey = null;
            CrossFadeTo(null);
        }

        /// <summary>現在の BGM キー。セーブに含めて、ロード時に鳴らし直すために使う。</summary>
        public string CurrentKey => currentKey;

        private void CrossFadeTo(AudioClip clip)
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            fadeRoutine = StartCoroutine(CrossFade(clip));
        }

        private IEnumerator CrossFade(AudioClip clip)
        {
            AudioSource from = Active;
            AudioSource to = Idle;

            if (clip != null)
            {
                to.clip = clip;
                to.volume = 0f;
                to.Play();
            }

            float target = Muted ? 0f : volume;
            float fromStart = from.volume;
            float elapsed = 0f;

            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeSeconds);

                from.volume = Mathf.Lerp(fromStart, 0f, t);
                if (clip != null)
                {
                    to.volume = Mathf.Lerp(0f, target, t);
                }

                yield return null;
            }

            from.Stop();
            from.clip = null;
            from.volume = 0f;

            if (clip != null)
            {
                to.volume = target;
                usingA = !usingA;
            }

            fadeRoutine = null;
        }

        private void ApplyMute()
        {
            float target = Muted ? 0f : volume;

            // フェード中は CrossFade 側が音量を握っているので触らない。
            if (fadeRoutine != null)
            {
                return;
            }

            Active.volume = Active.isPlaying ? target : 0f;
            Idle.volume = 0f;
        }
    }
}
