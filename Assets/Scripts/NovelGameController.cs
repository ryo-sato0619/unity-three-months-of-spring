using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Ink.Runtime;
using Ink.UnityIntegration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeMonthsOfSpring
{
    /// <summary>
    /// ink のストーリーを読み進めるノベルゲーム本体。
    ///
    /// シーンの組み立ては <c>Editor/SceneBuilder.cs</c> が行うため、
    /// Inspector で手作業の配線をする必要はない。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NovelGameController : MonoBehaviour
    {
        /// <summary>全エンディングの一覧。タイトル画面の回収率表示に使う。</summary>
        public static readonly (string Id, string Label)[] AllEndings =
        {
            ("true_spring", "TRUE END ／ 三か月の春"),
            ("normal_each", "NORMAL END ／ それぞれの春"),
            ("normal_passed", "NORMAL END ／ 春は過ぎて"),
            ("bad_unseen", "BAD END ／ 評価されないまま"),
            ("bad_line", "BAD END ／ 越えた一線"),
        };

        private const string EndingPrefsPrefix = "tms.ending.";

        /// <summary>
        /// ゲーム内クレジット。BGM は CC BY 4.0 なので、作品そのものに表示する義務がある。
        /// リポジトリ側の表記は CREDITS.md。
        /// </summary>
        private const string CreditsText =
            "<align=center><color=#E0A96D>音楽</color></align>\n\n" +
            "\"Bittersweet\", \"Disquiet\", \"Morning\",\n" +
            "\"Stay the Course\", \"Immersed\", \"Inspired\"\n" +
            "Kevin MacLeod (incompetech.com)\n" +
            "Licensed under Creative Commons: By Attribution 4.0 License\n" +
            "http://creativecommons.org/licenses/by/4.0/\n\n\n" +
            "<align=center><color=#E0A96D>立ち絵</color></align>\n\n" +
            "羽田春香の立ち絵は画像生成AIで生成したものです。\n\n\n" +
            "<align=center><color=#E0A96D>背景写真</color></align>\n\n" +
            "Unsplash (unsplash.com) / Unsplash License\n\n" +
            "Mylène Larnaud ／ Petr ／ kate.sade ／ JC Gellidon ／\n" +
            "Ryunosuke Kikuno ／ Pema G. Lama ／ Benjamin Child ／\n" +
            "Jelena Kostic ／ Weichao Deng ／ Lutz Stallknecht\n\n\n" +
            "<align=center><color=#E0A96D>フォント</color></align>\n\n" +
            "Noto Sans JP — Google / Noto CJK\n" +
            "Licensed under the SIL Open Font License 1.1\n\n\n" +
            "<align=center><color=#E0A96D>ソフトウェア</color></align>\n\n" +
            "ink / ink-unity-integration — inkle Ltd. (MIT License)\n" +
            "TextMeshPro — Unity Technologies\n\n\n" +
            "<align=center><color=#6B7280>登場する人物・企業・団体はすべて架空です。</color></align>";

        /// <summary>
        /// バックログの上限。TMP は1つのテキストで扱える文字数に上限があるため、
        /// 際限なく貯めると表示が欠ける。古いものから捨てる。
        /// </summary>
        private const int MaxLogEntries = 200;

        [Header("Story")]
        [SerializeField] private InkFile inkFile;

        [Header("UI - 本編")]
        [SerializeField] private RectTransform backgroundArea;
        [SerializeField] private Image background;
        [SerializeField] private Image backgroundNext;
        [SerializeField] private Image characterSprite;
        [SerializeField] private float spriteFadeSeconds = 0.25f;
        [SerializeField] private TMP_Text chapterLabel;
        [SerializeField] private GameObject speakerPanel;
        [SerializeField] private TMP_Text speakerLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private GameObject continueIndicator;
        [SerializeField] private RectTransform choiceRoot;
        [SerializeField] private Button choiceTemplate;
        [SerializeField] private Button advanceArea;

        [Header("UI - 操作バー")]
        [SerializeField] private GameObject controlBar;
        [SerializeField] private Button barLogButton;
        [SerializeField] private Button barSaveButton;
        [SerializeField] private Button barLoadButton;
        [SerializeField] private Button barAutoButton;
        [SerializeField] private TMP_Text barAutoLabel;
        [SerializeField] private Button barSkipButton;
        [SerializeField] private TMP_Text barSkipLabel;
        [SerializeField] private Button barBgmButton;
        [SerializeField] private TMP_Text barBgmLabel;
        [SerializeField] private Button barTitleButton;

        [Header("UI - バックログ")]
        [SerializeField] private GameObject logPanel;
        [SerializeField] private ScrollRect logScrollRect;
        [SerializeField] private TMP_Text logText;
        [SerializeField] private Button logCloseButton;

        [Header("UI - クレジット")]
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private ScrollRect creditsScrollRect;
        [SerializeField] private TMP_Text creditsLabel;
        [SerializeField] private Button creditsCloseButton;
        [SerializeField] private Button titleCreditsButton;

        [Header("UI - セーブ / ロード")]
        [SerializeField] private GameObject slotPanel;
        [SerializeField] private TMP_Text slotPanelTitle;
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private TMP_Text[] slotLabels;
        [SerializeField] private Button slotCloseButton;

        [Header("UI - 確認ダイアログ")]
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private TMP_Text confirmLabel;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        [Header("UI - エンディング一覧")]
        [SerializeField] private GameObject endingsPanel;
        [SerializeField] private ScrollRect endingsScrollRect;
        [SerializeField] private TMP_Text endingListLabel;
        [SerializeField] private Button endingsCloseButton;
        [SerializeField] private Button clearRecordButton;
        [SerializeField] private Button titleEndingsButton;

        [Header("UI - タイトル / リザルト")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button titleLoadButton;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField] private Button backToTitleButton;

        [Header("音")]
        [SerializeField] private AudioDirector audioDirector;

        [Header("UI - 設定")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button settingsCloseButton;
        [SerializeField] private Button settingsResetButton;
        [SerializeField] private Slider typingSpeedSlider;
        [SerializeField] private TMP_Text typingSpeedValue;
        [SerializeField] private Slider autoSpeedSlider;
        [SerializeField] private TMP_Text autoSpeedValue;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text volumeValue;
        [SerializeField] private Button titleSettingsButton;
        [SerializeField] private Button barSettingsButton;

        [Header("UI - 終了")]
        [SerializeField] private Button titleQuitButton;
        [SerializeField] private Button barQuitButton;

        [Header("演出")]
        /// <summary>背景を切り替えるときのクロスフェード時間。</summary>
        [SerializeField] private float backgroundFadeSeconds = 0.45f;

        /// <summary>スキップ時の1行あたりの間隔。設定画面には出さない内部の値。</summary>
        [SerializeField] private float skipIntervalSeconds = 0.035f;

        private Story story;
        private Coroutine typingRoutine;
        private bool isTyping;
        private string pendingFullLine;
        private string reachedEndingId;

        private readonly List<Button> spawnedChoices = new List<Button>();
        private readonly List<LogEntry> backlog = new List<LogEntry>();

        // いま画面に出ている内容。セーブして復元するために保持する。
        private string currentChapter = string.Empty;
        private string currentSpeaker = string.Empty;
        private string currentText = string.Empty;
        private string currentBackground = "street_morning";
        private string currentSprite = string.Empty;
        private Coroutine spriteFadeRoutine;
        private Coroutine backgroundFadeRoutine;

        private bool autoPlay;
        private bool skipping;
        private Coroutine autoRoutine;
        private Coroutine skipRoutine;

        /// <summary>直前に表示した行が、既に読んだことのある行だったか。スキップの停止判定に使う。</summary>
        private bool lastLineWasRead;

        private bool slotPanelIsSaveMode;
        private Vector2Int lastScreenSize;
        private Action pendingConfirmAction;

        // ------------------------------------------------------------
        //  初期化
        // ------------------------------------------------------------

        private void Awake()
        {
            // シーンとスクリプトが噛み合っていないと、この先で NullReferenceException が
            // 連鎖して原因が分からなくなる。先に確認して、直し方を示して止める。
            if (!ValidateReferences())
            {
                // 以降の処理はすべて参照に依存するので、ここで止める。
                enabled = false;
                return;
            }

            // フォントアセットは OS のフォントから実行時に生成するため、
            // シーンに焼き込まれていない。ここで全ラベルにまとめて適用する。
            foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true))
            {
                JapaneseFontProvider.Apply(label);
            }

            ApplyTitleScrim();
            choiceTemplate.gameObject.SetActive(false);

            startButton.onClick.AddListener(BeginStory);
            titleLoadButton.onClick.AddListener(() => OpenSlotPanel(false));
            clearRecordButton.onClick.AddListener(RequestClearEndingRecord);
            backToTitleButton.onClick.AddListener(ShowTitle);
            advanceArea.onClick.AddListener(OnAdvanceRequested);

            barLogButton.onClick.AddListener(OpenLog);
            barSaveButton.onClick.AddListener(() => OpenSlotPanel(true));
            barLoadButton.onClick.AddListener(() => OpenSlotPanel(false));
            barAutoButton.onClick.AddListener(ToggleAuto);
            barSkipButton.onClick.AddListener(ToggleSkip);
            barBgmButton.onClick.AddListener(ToggleBgm);
            barTitleButton.onClick.AddListener(RequestReturnToTitle);

            titleSettingsButton.onClick.AddListener(OpenSettings);
            barSettingsButton.onClick.AddListener(OpenSettings);
            settingsCloseButton.onClick.AddListener(() => settingsPanel.SetActive(false));
            settingsResetButton.onClick.AddListener(ResetSettings);

            titleQuitButton.onClick.AddListener(RequestQuit);
            barQuitButton.onClick.AddListener(RequestQuit);

            SetUpSettingsSliders();

            logCloseButton.onClick.AddListener(() => logPanel.SetActive(false));
            titleCreditsButton.onClick.AddListener(OpenCredits);
            creditsCloseButton.onClick.AddListener(() => creditsPanel.SetActive(false));
            titleEndingsButton.onClick.AddListener(OpenEndings);
            endingsCloseButton.onClick.AddListener(() => endingsPanel.SetActive(false));
            slotCloseButton.onClick.AddListener(() => slotPanel.SetActive(false));

            for (int i = 0; i < slotButtons.Length; i++)
            {
                int slot = i;
                slotButtons[i].onClick.AddListener(() => OnSlotClicked(slot));
            }

            confirmNoButton.onClick.AddListener(CloseConfirm);
            confirmYesButton.onClick.AddListener(() =>
            {
                Action action = pendingConfirmAction;
                CloseConfirm();
                action?.Invoke();
            });

            RefreshBgmLabel();
            RefreshAutoLabel();
            RefreshSkipLabel();
            ShowTitle();
        }

        private void OnApplicationQuit()
        {
            ReadHistory.Flush();
        }

        /// <summary>
        /// タイトル画面の暗幕を、一様な色から上下だけ濃いグラデーションに差し替える。
        /// テクスチャを実行時に作るので、シーンには色の指定だけが入っている。
        /// </summary>
        private void ApplyTitleScrim()
        {
            var scrim = titlePanel.GetComponent<Image>();
            if (scrim == null)
            {
                return;
            }

            Texture2D texture = BackgroundPalette.CreateTitleScrim();
            scrim.sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            scrim.color = Color.white;
        }

        /// <summary>
        /// Inspector で割り当てられるべき参照がすべて埋まっているか確認する。
        ///
        /// シーンは SceneBuilder が生成するので、スクリプトに項目を足したあと
        /// シーンを作り直し忘れると未割り当てのまま実行されてしまう。
        /// その場合に何が足りないのかと、どう直すのかを一度にログへ出す。
        /// </summary>
        private bool ValidateReferences()
        {
            var missing = new List<string>();

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            foreach (FieldInfo field in GetType().GetFields(flags))
            {
                if (field.GetCustomAttribute<SerializeField>() == null)
                {
                    continue;
                }

                if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                {
                    if (field.GetValue(this) as UnityEngine.Object == null)
                    {
                        missing.Add(field.Name);
                    }
                }
                else if (field.FieldType.IsArray &&
                         typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType.GetElementType()))
                {
                    var array = field.GetValue(this) as Array;
                    if (array == null || array.Length == 0)
                    {
                        missing.Add(field.Name);
                        continue;
                    }

                    foreach (object element in array)
                    {
                        if (element as UnityEngine.Object == null)
                        {
                            missing.Add(field.Name);
                            break;
                        }
                    }
                }
            }

            if (missing.Count == 0)
            {
                return true;
            }

            Debug.LogError(
                "シーンの参照が設定されていません: " + string.Join(", ", missing) + "\n" +
                "スクリプトを変更したあとシーンを作り直していない可能性があります。\n" +
                "再生を停止して [Tools > 三か月の春 > 2. シーンを生成する] を実行してください。");
            return false;
        }

        private void Update()
        {
            if (!IsModalOpen() && story != null &&
                (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
            {
                OnAdvanceRequested();
            }

            // 画面サイズが変わったら背景の収まりを計算し直す。
            var size = new Vector2Int(Screen.width, Screen.height);
            if (size != lastScreenSize)
            {
                lastScreenSize = size;
                FitBackground();
            }
        }

        /// <summary>本文のクリック送りを止めるべき状態か。</summary>
        private bool IsModalOpen()
        {
            return titlePanel.activeSelf
                || resultPanel.activeSelf
                || logPanel.activeSelf
                || slotPanel.activeSelf
                || confirmPanel.activeSelf
                || creditsPanel.activeSelf
                || endingsPanel.activeSelf
                || settingsPanel.activeSelf;
        }

        private void OpenEndings()
        {
            RefreshEndingList();
            endingsPanel.SetActive(true);

            Canvas.ForceUpdateCanvases();
            endingsScrollRect.verticalNormalizedPosition = 1f;
        }

        private void OpenCredits()
        {
            creditsLabel.text = CreditsText;
            creditsPanel.SetActive(true);

            Canvas.ForceUpdateCanvases();
            creditsScrollRect.verticalNormalizedPosition = 1f;
        }

        // ------------------------------------------------------------
        //  タイトル / リザルト
        // ------------------------------------------------------------

        private void ShowTitle()
        {
            StopAutoAndSkip();
            ReadHistory.Flush();

            story = null;
            backlog.Clear();
            reachedEndingId = null;

            titlePanel.SetActive(true);
            resultPanel.SetActive(false);
            logPanel.SetActive(false);
            slotPanel.SetActive(false);
            confirmPanel.SetActive(false);
            creditsPanel.SetActive(false);
            endingsPanel.SetActive(false);
            settingsPanel.SetActive(false);
            controlBar.SetActive(false);
            choiceRoot.gameObject.SetActive(false);
            continueIndicator.SetActive(false);
            speakerPanel.SetActive(false);

            bodyLabel.text = string.Empty;
            chapterLabel.text = string.Empty;
            currentChapter = string.Empty;
            currentSpeaker = string.Empty;
            currentText = string.Empty;
            currentSprite = string.Empty;
            ApplyCharacterSpriteInstantly();

            SetBackground("street_morning");
            audioDirector.Play("title");
        }

        private void RefreshEndingList()
        {
            var sb = new StringBuilder();
            int collected = 0;

            foreach ((string id, string label) in AllEndings)
            {
                bool seen = PlayerPrefs.GetInt(EndingPrefsPrefix + id, 0) == 1;
                if (seen)
                {
                    collected++;
                    sb.AppendLine($"<color=#FFD27F>■</color>  {label}");
                }
                else
                {
                    sb.AppendLine("<color=#6B7280>□　？？？？？？？？？？</color>");
                }
            }

            endingListLabel.text = $"エンディング  {collected} / {AllEndings.Length}\n\n{sb}";
        }

        private void RequestClearEndingRecord()
        {
            OpenConfirm(
                "エンディングの到達記録と既読記録をすべて消します。\n" +
                "既読を消すと、スキップは最初から効かなくなります。",
                () =>
                {
                    foreach ((string id, string _) in AllEndings)
                    {
                        PlayerPrefs.DeleteKey(EndingPrefsPrefix + id);
                    }

                    PlayerPrefs.Save();
                    ReadHistory.Clear();
                    RefreshEndingList();
                });
        }

        private void RecordEnding(string endingId)
        {
            if (string.IsNullOrEmpty(endingId))
            {
                return;
            }

            PlayerPrefs.SetInt(EndingPrefsPrefix + endingId, 1);
            PlayerPrefs.Save();
        }

        private void RequestReturnToTitle()
        {
            OpenConfirm("タイトルに戻ります。\nセーブしていない進行は失われます。", ShowTitle);
        }

        // ------------------------------------------------------------
        //  本編
        // ------------------------------------------------------------

        private void BeginStory()
        {
            if (!TryCreateStory())
            {
                return;
            }

            backlog.Clear();
            reachedEndingId = null;

            titlePanel.SetActive(false);
            resultPanel.SetActive(false);
            controlBar.SetActive(true);
            ContinueStory();
        }

        private bool TryCreateStory()
        {
            if (inkFile == null || !inkFile.isCompiled)
            {
                bodyLabel.text = "ink ファイルが読み込めません。Assets/Ink のコンパイルエラーを確認してください。";
                return false;
            }

            story = new Story(inkFile.storyJson);
            return true;
        }

        private void ContinueStory()
        {
            ClearChoices();

            while (story.canContinue)
            {
                string line = story.Continue().Trim();
                ApplyTags(story.currentTags);

                if (line.Length == 0)
                {
                    // 空行はタグだけを運ぶ行なので表示せず読み飛ばす。
                    continue;
                }

                currentText = line;
                AppendLog(LogEntry.KindLine, currentSpeaker, line);

                lastLineWasRead = ReadHistory.IsRead(line);
                ReadHistory.Mark(line);

                if (skipping)
                {
                    // スキップ中は文字送りをしない。
                    ShowLineInstantly(line);
                    continueIndicator.SetActive(true);
                }
                else
                {
                    StartTyping(line);
                }

                return;
            }

            if (story.currentChoices.Count > 0)
            {
                ShowChoices();
            }
            else
            {
                ShowResult();
            }
        }

        private void ApplyTags(IReadOnlyList<string> tags)
        {
            // 話者は行ごとに解決する。タグが無い行は地の文とみなす。
            bool sawSpeakerTag = false;

            if (tags != null)
            {
                foreach (string tag in tags)
                {
                    int separator = tag.IndexOf(':');
                    if (separator < 0)
                    {
                        continue;
                    }

                    string key = tag.Substring(0, separator).Trim();
                    string value = tag.Substring(separator + 1).Trim();

                    switch (key)
                    {
                        case "name":
                            sawSpeakerTag = true;
                            currentSpeaker = value;
                            break;
                        case "chapter":
                            SetChapter(value);
                            break;
                        case "bg":
                            SetBackground(value);
                            break;
                        case "bgm":
                            audioDirector.Play(value);
                            break;
                        case "sprite":
                            SetCharacterSprite(value);
                            break;
                        case "ending":
                            reachedEndingId = value;
                            break;
                    }
                }
            }

            if (!sawSpeakerTag)
            {
                currentSpeaker = string.Empty;
            }

            ApplySpeakerToUi();
        }

        private void ApplySpeakerToUi()
        {
            bool hasSpeaker = !string.IsNullOrEmpty(currentSpeaker);
            speakerPanel.SetActive(hasSpeaker);
            speakerLabel.text = hasSpeaker ? currentSpeaker : string.Empty;
        }

        private void SetChapter(string chapter)
        {
            if (currentChapter == chapter)
            {
                return;
            }

            currentChapter = chapter;
            chapterLabel.text = chapter;
            AppendLog(LogEntry.KindChapter, null, chapter);
        }

        private void SetBackground(string key)
        {
            if (!BackgroundPalette.IsKnown(key) && !BackgroundProvider.HasImage(key))
            {
                Debug.LogWarning($"未定義の背景タグです: '{key}'。BackgroundPalette か Resources/Backgrounds に追加してください。");
            }

            Sprite sprite = BackgroundProvider.Get(key);
            bool first = background.sprite == null;
            bool changed = currentBackground != key;
            currentBackground = key;

            // 初回とスキップ中は即時。それ以外は前の背景から溶け込ませる。
            if (first || skipping || !changed || backgroundFadeSeconds <= 0f)
            {
                ApplyBackgroundInstantly(sprite);
                return;
            }

            if (backgroundFadeRoutine != null)
            {
                StopCoroutine(backgroundFadeRoutine);
                backgroundFadeRoutine = null;
            }

            backgroundFadeRoutine = StartCoroutine(CrossFadeBackground(sprite));
        }

        private void ApplyBackgroundInstantly(Sprite sprite)
        {
            if (backgroundFadeRoutine != null)
            {
                StopCoroutine(backgroundFadeRoutine);
                backgroundFadeRoutine = null;
            }

            background.sprite = sprite;
            background.color = Color.white;
            backgroundNext.gameObject.SetActive(false);
            FitBackground();
        }

        /// <summary>
        /// 上に重ねたもう1枚を不透明にしていき、終わったら下に焼き付ける。
        /// 1枚の画像の色を触るだけだと一度暗転してしまうので、2枚でつなぐ。
        /// </summary>
        private IEnumerator CrossFadeBackground(Sprite sprite)
        {
            backgroundNext.sprite = sprite;
            backgroundNext.gameObject.SetActive(true);
            FitBackground();

            var color = Color.white;
            float elapsed = 0f;

            while (elapsed < backgroundFadeSeconds)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Clamp01(elapsed / backgroundFadeSeconds);
                backgroundNext.color = color;
                yield return null;
            }

            background.sprite = sprite;
            background.color = Color.white;
            backgroundNext.gameObject.SetActive(false);
            backgroundNext.color = Color.white;
            FitBackground();

            backgroundFadeRoutine = null;
        }

        /// <summary>
        /// 立ち絵を切り替える。キーが "none" または空なら隠す。
        /// 素材が未配置の場合も静かに隠すだけで、進行は止めない。
        /// </summary>
        private void SetCharacterSprite(string key)
        {
            if (currentSprite == key)
            {
                return;
            }

            currentSprite = key ?? string.Empty;
            Sprite sprite = CharacterSpriteProvider.Get(currentSprite);

            if (sprite == null && !CharacterSpriteProvider.IsHideKey(currentSprite))
            {
                Debug.Log($"[立ち絵] 未配置のためスキップします: Resources/{CharacterSpriteProvider.ResourceFolder}{currentSprite}");
            }

            ApplyCharacterSprite(sprite, animate: true);
        }

        /// <summary>ロード時など、フェードせずに即座に反映したい場合に使う。</summary>
        private void ApplyCharacterSpriteInstantly()
        {
            ApplyCharacterSprite(CharacterSpriteProvider.Get(currentSprite), animate: false);
        }

        private void ApplyCharacterSprite(Sprite sprite, bool animate)
        {
            if (spriteFadeRoutine != null)
            {
                StopCoroutine(spriteFadeRoutine);
                spriteFadeRoutine = null;
            }

            if (sprite == null)
            {
                if (animate && characterSprite.gameObject.activeSelf)
                {
                    spriteFadeRoutine = StartCoroutine(FadeOutCharacter());
                }
                else
                {
                    characterSprite.gameObject.SetActive(false);
                }

                return;
            }

            characterSprite.sprite = sprite;
            characterSprite.gameObject.SetActive(true);

            if (animate)
            {
                spriteFadeRoutine = StartCoroutine(FadeInCharacter());
            }
            else
            {
                characterSprite.color = Color.white;
            }
        }

        private IEnumerator FadeInCharacter()
        {
            float elapsed = 0f;
            Color c = characterSprite.color;
            c.a = 0f;
            characterSprite.color = c;

            while (elapsed < spriteFadeSeconds)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Clamp01(elapsed / spriteFadeSeconds);
                characterSprite.color = c;
                yield return null;
            }

            characterSprite.color = Color.white;
            spriteFadeRoutine = null;
        }

        private IEnumerator FadeOutCharacter()
        {
            float elapsed = 0f;
            Color c = characterSprite.color;
            float from = c.a;

            while (elapsed < spriteFadeSeconds)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(from, 0f, Mathf.Clamp01(elapsed / spriteFadeSeconds));
                characterSprite.color = c;
                yield return null;
            }

            characterSprite.gameObject.SetActive(false);
            characterSprite.color = Color.white;
            spriteFadeRoutine = null;
        }

        /// <summary>
        /// 背景を画面いっぱいに「切れてもいいので隙間なく」収める（cover）。
        /// 単純に引き伸ばすと写真の縦横比が崩れるため。
        /// </summary>
        private void FitBackground()
        {
            FitToCover(background);
            FitToCover(backgroundNext);
        }

        private void FitToCover(Image image)
        {
            if (image == null || backgroundArea == null)
            {
                return;
            }

            Sprite sprite = image.sprite;
            if (sprite == null)
            {
                return;
            }

            Vector2 area = backgroundArea.rect.size;
            if (area.x <= 0f || area.y <= 0f)
            {
                return;
            }

            float spriteAspect = sprite.rect.width / sprite.rect.height;
            float areaAspect = area.x / area.y;

            image.rectTransform.sizeDelta = spriteAspect > areaAspect
                ? new Vector2(area.y * spriteAspect, area.y)
                : new Vector2(area.x, area.x / spriteAspect);
        }

        // ------------------------------------------------------------
        //  文字送り
        // ------------------------------------------------------------

        private void StartTyping(string line)
        {
            if (typingRoutine != null)
            {
                StopCoroutine(typingRoutine);
            }

            pendingFullLine = line;
            typingRoutine = StartCoroutine(TypeLine(line));
        }

        private IEnumerator TypeLine(string line)
        {
            isTyping = true;
            continueIndicator.SetActive(false);

            bodyLabel.text = line;
            bodyLabel.maxVisibleCharacters = 0;

            // TMP に文字数を確定させてから 1 文字ずつ開示する。
            bodyLabel.ForceMeshUpdate();
            int total = bodyLabel.textInfo.characterCount;

            float speed = GameSettings.TypingSpeed;
            float interval = speed > 0f ? 1f / speed : 0f;

            for (int visible = 1; visible <= total; visible++)
            {
                bodyLabel.maxVisibleCharacters = visible;
                if (interval > 0f)
                {
                    yield return new WaitForSeconds(interval);
                }
            }

            FinishTyping();
        }

        private void FinishTyping()
        {
            if (typingRoutine != null)
            {
                StopCoroutine(typingRoutine);
                typingRoutine = null;
            }

            bodyLabel.text = pendingFullLine;
            bodyLabel.maxVisibleCharacters = int.MaxValue;
            isTyping = false;
            continueIndicator.SetActive(true);
        }

        /// <summary>文字送りをせずに、指定した行をそのまま表示する（ロード時の復元用）。</summary>
        private void ShowLineInstantly(string line)
        {
            if (typingRoutine != null)
            {
                StopCoroutine(typingRoutine);
                typingRoutine = null;
            }

            pendingFullLine = line;
            bodyLabel.text = line;
            bodyLabel.maxVisibleCharacters = int.MaxValue;
            isTyping = false;
        }

        private void OnAdvanceRequested()
        {
            if (story == null || IsModalOpen())
            {
                return;
            }

            // 選択肢の表示中はクリックで先に進めない。
            if (choiceRoot.gameObject.activeSelf)
            {
                return;
            }

            // 手動で送ったらスキップは解除する。押した位置で止まってほしいはずなので。
            // オートは継続させる（クリックは「今の行を早送り」の意味に留める）。
            if (skipping)
            {
                SetSkip(false);
            }

            if (isTyping)
            {
                FinishTyping();
                return;
            }

            ContinueStory();
        }

        // ------------------------------------------------------------
        //  選択肢
        // ------------------------------------------------------------

        private void ShowChoices()
        {
            continueIndicator.SetActive(false);
            choiceRoot.gameObject.SetActive(true);

            for (int i = 0; i < story.currentChoices.Count; i++)
            {
                Choice choice = story.currentChoices[i];
                Button button = Instantiate(choiceTemplate, choiceRoot);
                button.gameObject.SetActive(true);
                button.name = $"Choice_{i}";

                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                JapaneseFontProvider.Apply(label);
                label.text = choice.text.Trim();

                int index = choice.index;
                button.onClick.AddListener(() => OnChoiceSelected(index));

                spawnedChoices.Add(button);
            }
        }

        private void OnChoiceSelected(int index)
        {
            string chosen = index >= 0 && index < story.currentChoices.Count
                ? story.currentChoices[index].text.Trim()
                : string.Empty;

            AppendLog(LogEntry.KindChoice, null, chosen);

            story.ChooseChoiceIndex(index);
            ClearChoices();
            ContinueStory();
        }

        private void ClearChoices()
        {
            foreach (Button button in spawnedChoices)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            spawnedChoices.Clear();
            choiceRoot.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------
        //  バックログ
        // ------------------------------------------------------------

        private void AppendLog(int kind, string speaker, string text)
        {
            backlog.Add(new LogEntry { kind = kind, speaker = speaker ?? string.Empty, text = text });

            while (backlog.Count > MaxLogEntries)
            {
                backlog.RemoveAt(0);
            }
        }

        private void OpenLog()
        {
            logText.text = BuildLogText();
            logPanel.SetActive(true);

            // レイアウトが確定してからでないと、末尾までスクロールできない。
            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 0f;
        }

        private string BuildLogText()
        {
            if (backlog.Count == 0)
            {
                return "<color=#6B7280>まだ記録がありません。</color>";
            }

            var sb = new StringBuilder();

            foreach (LogEntry entry in backlog)
            {
                switch (entry.kind)
                {
                    case LogEntry.KindChapter:
                        sb.AppendLine();
                        sb.AppendLine($"<align=center><color=#E0A96D>──　{entry.text}　──</color></align>");
                        sb.AppendLine();
                        break;

                    case LogEntry.KindChoice:
                        sb.AppendLine($"<color=#9FD0F0>▶　{entry.text}</color>");
                        sb.AppendLine();
                        break;

                    default:
                        if (!string.IsNullOrEmpty(entry.speaker))
                        {
                            sb.AppendLine($"<color=#E0A96D>{entry.speaker}</color>");
                        }

                        sb.AppendLine(entry.text);
                        sb.AppendLine();
                        break;
                }
            }

            return sb.ToString();
        }

        // ------------------------------------------------------------
        //  セーブ / ロード
        // ------------------------------------------------------------

        private void OpenSlotPanel(bool saveMode)
        {
            slotPanelIsSaveMode = saveMode;
            slotPanelTitle.text = saveMode ? "セーブ" : "ロード";

            for (int i = 0; i < slotButtons.Length; i++)
            {
                slotLabels[i].text = SaveSystem.Describe(i);

                // ロード時は、空きスロットを押せないようにする。
                slotButtons[i].interactable = saveMode || SaveSystem.Load(i) != null;
            }

            slotPanel.SetActive(true);
        }

        private void OnSlotClicked(int slot)
        {
            if (slotPanelIsSaveMode)
            {
                if (SaveSystem.Exists(slot))
                {
                    OpenConfirm($"スロット {slot + 1} は使用中です。\n上書きしますか？", () => WriteSave(slot));
                }
                else
                {
                    WriteSave(slot);
                }
            }
            else
            {
                ReadSave(slot);
            }
        }

        private void WriteSave(int slot)
        {
            if (story == null)
            {
                return;
            }

            var data = new SaveData
            {
                chapter = currentChapter,
                speaker = currentSpeaker,
                text = currentText,
                background = currentBackground,
                bgm = audioDirector.CurrentKey,
                sprite = currentSprite,
                inkState = story.state.ToJson(),
                log = new List<LogEntry>(backlog),
            };

            ReadHistory.Flush();

            if (SaveSystem.Save(slot, data))
            {
                slotPanel.SetActive(false);
            }
            else
            {
                slotLabels[slot].text = $"スロット {slot + 1}　-　保存に失敗しました";
            }
        }

        private void ReadSave(int slot)
        {
            SaveData data = SaveSystem.Load(slot);
            if (data == null)
            {
                slotLabels[slot].text = $"スロット {slot + 1}　-　読み込めません";
                return;
            }

            if (!TryCreateStory())
            {
                return;
            }

            try
            {
                story.state.LoadJson(data.inkState);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] 進行状態の復元に失敗しました: {e.Message}");
                slotLabels[slot].text = $"スロット {slot + 1}　-　データが壊れています";
                story = null;
                return;
            }

            StopAutoAndSkip();

            // ログを引き継ぐ。
            backlog.Clear();
            if (data.log != null)
            {
                backlog.AddRange(data.log);
            }

            reachedEndingId = null;

            // 画面を復元する。
            currentChapter = data.chapter ?? string.Empty;
            currentSpeaker = data.speaker ?? string.Empty;
            currentText = data.text ?? string.Empty;
            chapterLabel.text = currentChapter;
            ApplySpeakerToUi();
            SetBackground(string.IsNullOrEmpty(data.background) ? "office_day" : data.background);
            audioDirector.Play(data.bgm);

            // 立ち絵はフェードなしで即座に戻す。ロード直後にふわっと出ると違和感があるため。
            currentSprite = data.sprite ?? string.Empty;
            ApplyCharacterSpriteInstantly();

            ShowLineInstantly(currentText);

            titlePanel.SetActive(false);
            resultPanel.SetActive(false);
            slotPanel.SetActive(false);
            logPanel.SetActive(false);
            controlBar.SetActive(true);
            ClearChoices();

            // 選択肢の直前でセーブされていた場合は、選択肢を出し直す。
            if (!story.canContinue && story.currentChoices.Count > 0)
            {
                ShowChoices();
            }
            else if (!story.canContinue && story.currentChoices.Count == 0)
            {
                ShowResult();
            }
            else
            {
                continueIndicator.SetActive(true);
            }
        }

        // ------------------------------------------------------------
        //  確認ダイアログ
        // ------------------------------------------------------------

        private void OpenConfirm(string message, Action onYes)
        {
            confirmLabel.text = message;
            pendingConfirmAction = onYes;
            confirmPanel.SetActive(true);
        }

        private void CloseConfirm()
        {
            pendingConfirmAction = null;
            confirmPanel.SetActive(false);
        }

        // ------------------------------------------------------------
        //  BGM
        // ------------------------------------------------------------

        private void ToggleBgm()
        {
            audioDirector.Muted = !audioDirector.Muted;
            RefreshBgmLabel();
            RefreshSettingsValues();
        }

        // ------------------------------------------------------------
        //  設定
        // ------------------------------------------------------------

        private void SetUpSettingsSliders()
        {
            typingSpeedSlider.minValue = GameSettings.TypingSpeedMin;
            typingSpeedSlider.maxValue = GameSettings.TypingSpeedMax;
            typingSpeedSlider.wholeNumbers = true;
            typingSpeedSlider.onValueChanged.AddListener(value =>
            {
                GameSettings.TypingSpeed = value;
                RefreshSettingsValues();
            });

            autoSpeedSlider.minValue = 0f;
            autoSpeedSlider.maxValue = 1f;
            autoSpeedSlider.onValueChanged.AddListener(value =>
            {
                GameSettings.AutoSpeed = value;
                RefreshSettingsValues();
            });

            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.onValueChanged.AddListener(value =>
            {
                GameSettings.BgmVolume = value;
                // 鳴っている曲に即座に反映する。調整結果をその場で聞けるように。
                audioDirector.ApplyVolume();
                RefreshSettingsValues();
            });

            PullSettingsIntoSliders();
        }

        /// <summary>保存された設定値をスライダーに反映する。onValueChanged は呼ばせない。</summary>
        private void PullSettingsIntoSliders()
        {
            typingSpeedSlider.SetValueWithoutNotify(GameSettings.TypingSpeed);
            autoSpeedSlider.SetValueWithoutNotify(GameSettings.AutoSpeed);
            volumeSlider.SetValueWithoutNotify(GameSettings.BgmVolume);
            RefreshSettingsValues();
        }

        private void RefreshSettingsValues()
        {
            typingSpeedValue.text = $"{GameSettings.TypingSpeed:0} 文字/秒";

            // 待ち時間そのものを出す。「0.7」より「1行あたり約1.4秒」のほうが伝わる。
            float sample = GameSettings.AutoBaseSeconds + 30f * GameSettings.AutoPerCharacterSeconds;
            autoSpeedValue.text = $"1行あたり約 {sample:0.0} 秒";

            volumeValue.text = audioDirector.Muted
                ? "ミュート中"
                : $"{GameSettings.BgmVolume * 100f:0} %";
        }

        private void OpenSettings()
        {
            PullSettingsIntoSliders();
            settingsPanel.SetActive(true);
        }

        private void ResetSettings()
        {
            GameSettings.ResetToDefaults();
            audioDirector.ApplyVolume();
            PullSettingsIntoSliders();
        }

        // ------------------------------------------------------------
        //  終了
        // ------------------------------------------------------------

        private void RequestQuit()
        {
            // 進行中かどうかで注意書きを変える。失うものが違うため。
            string message = story != null
                ? "ゲームを終了します。\nセーブしていない進行は失われます。"
                : "ゲームを終了します。";

            OpenConfirm(message, QuitGame);
        }

        private void QuitGame()
        {
            ReadHistory.Flush();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------
        //  オート再生 / 既読スキップ
        // ------------------------------------------------------------

        private void ToggleAuto()
        {
            SetAuto(!autoPlay);
        }

        private void SetAuto(bool enable)
        {
            autoPlay = enable;
            RefreshAutoLabel();

            if (autoRoutine != null)
            {
                StopCoroutine(autoRoutine);
                autoRoutine = null;
            }

            if (autoPlay)
            {
                // オートとスキップは同時に動かさない。速いほうが勝つと操作が読めなくなる。
                SetSkip(false);
                autoRoutine = StartCoroutine(AutoRoutine());
            }
        }

        private void RefreshAutoLabel()
        {
            barAutoLabel.text = autoPlay ? "オート ON" : "オート";
        }

        private IEnumerator AutoRoutine()
        {
            while (autoPlay)
            {
                if (story == null || IsModalOpen() || choiceRoot.gameObject.activeSelf || isTyping)
                {
                    yield return null;
                    continue;
                }

                // 行の長さに応じて待つ。短い相槌で長く待たされないように。
                float wait = GameSettings.AutoBaseSeconds
                    + (currentText?.Length ?? 0) * GameSettings.AutoPerCharacterSeconds;
                float elapsed = 0f;
                while (elapsed < wait)
                {
                    if (!autoPlay || IsModalOpen() || choiceRoot.gameObject.activeSelf)
                    {
                        break;
                    }

                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (!autoPlay || IsModalOpen() || choiceRoot.gameObject.activeSelf || isTyping)
                {
                    continue;
                }

                ContinueStory();
            }

            autoRoutine = null;
        }

        private void ToggleSkip()
        {
            SetSkip(!skipping);
        }

        private void SetSkip(bool enable)
        {
            skipping = enable;
            RefreshSkipLabel();

            if (skipRoutine != null)
            {
                StopCoroutine(skipRoutine);
                skipRoutine = null;
            }

            if (skipping)
            {
                SetAuto(false);
                skipRoutine = StartCoroutine(SkipRoutine());
            }
        }

        private void RefreshSkipLabel()
        {
            barSkipLabel.text = skipping ? "スキップ中" : "スキップ";
        }

        /// <summary>
        /// 既読の行だけを送る。未読に当たったら、その行を表示して止まる。
        ///
        /// ink は「次の行」を消費せずに覗けないので、1行進めてから既読か判定している。
        /// 結果として必ず未読の1行目で止まる形になり、これは望ましい挙動でもある。
        /// </summary>
        private IEnumerator SkipRoutine()
        {
            while (skipping)
            {
                if (story == null || IsModalOpen() || choiceRoot.gameObject.activeSelf)
                {
                    break;
                }

                if (!story.canContinue && story.currentChoices.Count == 0)
                {
                    break;
                }

                ContinueStory();

                // 未読に到達した、選択肢が出た、物語が終わった、のいずれかで停止。
                if (!lastLineWasRead || choiceRoot.gameObject.activeSelf || IsModalOpen())
                {
                    break;
                }

                yield return new WaitForSeconds(skipIntervalSeconds);
            }

            skipRoutine = null;
            if (skipping)
            {
                skipping = false;
                RefreshSkipLabel();
            }
        }

        /// <summary>タイトルへ戻る、ロードするなど、進行が切り替わるときに両方止める。</summary>
        private void StopAutoAndSkip()
        {
            SetAuto(false);
            SetSkip(false);
        }

        private void RefreshBgmLabel()
        {
            barBgmLabel.text = audioDirector.Muted ? "BGM OFF" : "BGM ON";
        }

        // ------------------------------------------------------------
        //  終了
        // ------------------------------------------------------------

        private void ShowResult()
        {
            StopAutoAndSkip();
            ReadHistory.Flush();

            continueIndicator.SetActive(false);
            controlBar.SetActive(false);
            RecordEnding(reachedEndingId);

            string label = "エンディング";
            foreach ((string id, string text) in AllEndings)
            {
                if (id == reachedEndingId)
                {
                    label = text;
                    break;
                }
            }

            int collected = 0;
            foreach ((string id, string _) in AllEndings)
            {
                if (PlayerPrefs.GetInt(EndingPrefsPrefix + id, 0) == 1)
                {
                    collected++;
                }
            }

            resultLabel.text = $"{label}\n\n到達エンディング　{collected} / {AllEndings.Length}";
            resultPanel.SetActive(true);
        }
    }
}
