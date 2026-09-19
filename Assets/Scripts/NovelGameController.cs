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

        [Header("Story")]
        [SerializeField] private InkFile inkFile;

        [Header("UI - 本編")]
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text chapterLabel;
        [SerializeField] private GameObject speakerPanel;
        [SerializeField] private TMP_Text speakerLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private GameObject continueIndicator;
        [SerializeField] private RectTransform choiceRoot;
        [SerializeField] private Button choiceTemplate;
        [SerializeField] private Button advanceArea;

        [Header("UI - タイトル / リザルト")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private TMP_Text endingListLabel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button clearRecordButton;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField] private Button backToTitleButton;

        [Header("演出")]
        [SerializeField] private float charactersPerSecond = 45f;

        private Story story;
        private Coroutine typingRoutine;
        private bool isTyping;
        private string pendingFullLine;
        private string reachedEndingId;
        private readonly List<Button> spawnedChoices = new List<Button>();

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
            backToTitleButton.onClick.AddListener(ShowTitle);
            clearRecordButton.onClick.AddListener(ClearEndingRecord);
            advanceArea.onClick.AddListener(OnAdvanceRequested);

            ShowTitle();
        }

        private void Update()
        {
            if (!titlePanel.activeSelf && !resultPanel.activeSelf &&
                (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
            {
                OnAdvanceRequested();
            }
        }

        // ------------------------------------------------------------
        //  タイトル / リザルト
        // ------------------------------------------------------------

        private void ShowTitle()
        {
            titlePanel.SetActive(true);
            resultPanel.SetActive(false);
            choiceRoot.gameObject.SetActive(false);
            continueIndicator.SetActive(false);
            speakerPanel.SetActive(false);
            bodyLabel.text = string.Empty;
            chapterLabel.text = string.Empty;
            SetBackground("street_morning");
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

            endingListLabel.text =
                $"エンディング  {collected} / {AllEndings.Length}\n\n{sb}";
        }

        private void ClearEndingRecord()
        {
            foreach ((string id, string _) in AllEndings)
            {
                PlayerPrefs.DeleteKey(EndingPrefsPrefix + id);
            }

            PlayerPrefs.Save();
            RefreshEndingList();
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

        // ------------------------------------------------------------
        //  本編
        // ------------------------------------------------------------

        private void BeginStory()
        {
            if (inkFile == null || !inkFile.isCompiled)
            {
                bodyLabel.text = "ink ファイルが読み込めません。Assets/Ink のコンパイルエラーを確認してください。";
                return;
            }

            story = new Story(inkFile.storyJson);
            reachedEndingId = null;

            titlePanel.SetActive(false);
            resultPanel.SetActive(false);
            ContinueStory();
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
            if (tags == null)
            {
                return;
            }

            // 話者は行ごとに解決する。タグが無い行は地の文とみなす。
            bool sawSpeakerTag = false;

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
                        speakerPanel.SetActive(true);
                        speakerLabel.text = value;
                        break;
                    case "chapter":
                        chapterLabel.text = value;
                        break;
                    case "bg":
                        SetBackground(value);
                        break;
                    case "ending":
                        reachedEndingId = value;
                        break;
                }
            }

            if (!sawSpeakerTag)
            {
                speakerPanel.SetActive(false);
                speakerLabel.text = string.Empty;
            }
        }

        private void SetBackground(string key)
        {
            if (!BackgroundPalette.IsKnown(key))
            {
                Debug.LogWarning($"未定義の背景タグです: '{key}'。BackgroundPalette に追加してください。");
            }

            Texture2D texture = BackgroundPalette.CreateTexture(key);
            Sprite previous = background.sprite;

            background.sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            background.color = Color.white;

            // 直前のスプライトと、その元テクスチャを解放する。
            if (previous != null)
            {
                Texture2D previousTexture = previous.texture;
                Destroy(previous);
                if (previousTexture != null)
                {
                    Destroy(previousTexture);
                }
            }
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

        private void OnAdvanceRequested()
        {
            if (story == null || titlePanel.activeSelf || resultPanel.activeSelf)
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
        //  終了
        // ------------------------------------------------------------

        private void ShowResult()
        {
            continueIndicator.SetActive(false);
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

            resultLabel.text =
                $"{label}\n\n到達エンディング　{collected} / {AllEndings.Length}";
            resultPanel.SetActive(true);
        }
    }
}
