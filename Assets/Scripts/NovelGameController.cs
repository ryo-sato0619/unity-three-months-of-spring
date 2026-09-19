using System;
using System.Collections;
using System.Collections.Generic;
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
            "<align=center><color=#E0A96D>背景写真</color></align>\n\n" +
            "Unsplash (unsplash.com) / Unsplash License\n\n" +
            "Mylène Larnaud ／ Petr ／ kate.sade ／ JC Gellidon ／\n" +
            "Ryunosuke Kikuno ／ Pema G. Lama ／ Benjamin Child ／\n" +
            "Jelena Kostic ／ Weichao Deng ／ Lutz Stallknecht\n\n\n" +
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

        [Header("UI - タイトル / リザルト")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private TMP_Text endingListLabel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button titleLoadButton;
        [SerializeField] private Button clearRecordButton;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField] private Button backToTitleButton;

        [Header("音")]
        [SerializeField] private AudioDirector audioDirector;

        [Header("演出")]
        [SerializeField] private float charactersPerSecond = 45f;

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

        private bool slotPanelIsSaveMode;
        private Vector2Int lastScreenSize;
        private Action pendingConfirmAction;

        // ------------------------------------------------------------
        //  初期化
        // ------------------------------------------------------------

        private void Awake()
        {
            // フォントアセットは OS のフォントから実行時に生成するため、
            // シーンに焼き込まれていない。ここで全ラベルにまとめて適用する。
            foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true))
            {
                JapaneseFontProvider.Apply(label);
            }

            choiceTemplate.gameObject.SetActive(false);

            startButton.onClick.AddListener(BeginStory);
            titleLoadButton.onClick.AddListener(() => OpenSlotPanel(false));
            clearRecordButton.onClick.AddListener(RequestClearEndingRecord);
            backToTitleButton.onClick.AddListener(ShowTitle);
            advanceArea.onClick.AddListener(OnAdvanceRequested);

            barLogButton.onClick.AddListener(OpenLog);
            barSaveButton.onClick.AddListener(() => OpenSlotPanel(true));
            barLoadButton.onClick.AddListener(() => OpenSlotPanel(false));
            barBgmButton.onClick.AddListener(ToggleBgm);
            barTitleButton.onClick.AddListener(RequestReturnToTitle);

            logCloseButton.onClick.AddListener(() => logPanel.SetActive(false));
            titleCreditsButton.onClick.AddListener(OpenCredits);
            creditsCloseButton.onClick.AddListener(() => creditsPanel.SetActive(false));
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
            ShowTitle();
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
                || creditsPanel.activeSelf;
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
            story = null;
            backlog.Clear();
            reachedEndingId = null;

            titlePanel.SetActive(true);
            resultPanel.SetActive(false);
            logPanel.SetActive(false);
            slotPanel.SetActive(false);
            confirmPanel.SetActive(false);
            creditsPanel.SetActive(false);
            controlBar.SetActive(false);
            choiceRoot.gameObject.SetActive(false);
            continueIndicator.SetActive(false);
            speakerPanel.SetActive(false);

            bodyLabel.text = string.Empty;
            chapterLabel.text = string.Empty;
            currentChapter = string.Empty;
            currentSpeaker = string.Empty;
            currentText = string.Empty;

            SetBackground("street_morning");
            audioDirector.Play("title");
            RefreshEndingList();
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
            OpenConfirm("エンディングの到達記録をすべて消します。\nよろしいですか？", () =>
            {
                foreach ((string id, string _) in AllEndings)
                {
                    PlayerPrefs.DeleteKey(EndingPrefsPrefix + id);
                }

                PlayerPrefs.Save();
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
                StartTyping(line);
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

            currentBackground = key;
            background.sprite = BackgroundProvider.Get(key);
            background.color = Color.white;
            FitBackground();
        }

        /// <summary>
        /// 背景を画面いっぱいに「切れてもいいので隙間なく」収める（cover）。
        /// 単純に引き伸ばすと写真の縦横比が崩れるため。
        /// </summary>
        private void FitBackground()
        {
            Sprite sprite = background.sprite;
            if (sprite == null || backgroundArea == null)
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

            Vector2 size = spriteAspect > areaAspect
                ? new Vector2(area.y * spriteAspect, area.y)
                : new Vector2(area.x, area.x / spriteAspect);

            background.rectTransform.sizeDelta = size;
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

            float interval = charactersPerSecond > 0f ? 1f / charactersPerSecond : 0f;

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
                inkState = story.state.ToJson(),
                log = new List<LogEntry>(backlog),
            };

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
