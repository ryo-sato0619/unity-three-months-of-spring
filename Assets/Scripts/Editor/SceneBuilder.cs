using System.IO;
using Ink.UnityIntegration;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThreeMonthsOfSpring.EditorTools
{
    /// <summary>
    /// ノベルゲームのシーンをコードから組み立てる。
    ///
    /// Inspector 上の手作業を無くし、シーンの内容を差分として読める形で
    /// リポジトリに残すのが目的。UI を変えたいときはこのスクリプトを直して
    /// メニューから再生成する。
    /// </summary>
    public static class SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string InkPath = "Assets/Ink/ThreeMonthsOfSpring.ink";

        private static readonly Color PanelColor = new Color(0.04f, 0.05f, 0.08f, 0.78f);
        private static readonly Color AccentColor = new Color(0.86f, 0.62f, 0.42f, 0.95f);
        private static readonly Color ChoiceIdle = new Color(0.10f, 0.12f, 0.17f, 0.92f);
        private static readonly Color TextColor = new Color(0.96f, 0.96f, 0.94f, 1f);
        private static readonly Color OverlayColor = new Color(0.02f, 0.03f, 0.05f, 0.95f);

        [MenuItem("Tools/三か月の春/2. シーンを生成する")]
        public static void BuildScene()
        {
            PlayerSettings.companyName = "ryo-sato0619";
            PlayerSettings.productName = "three-months-of-spring";

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateEventSystem();

            Canvas canvas = CreateCanvas();
            var controller = canvas.gameObject.AddComponent<NovelGameController>();
            var so = new SerializedObject(controller);

            AudioDirector audio = CreateAudio(canvas.transform);

            // --- 描画順 = ヒエラルキー順。後に作ったものほど手前に来る。 ---
            CreateBackground(canvas.transform, out RectTransform backgroundArea, out Image background);
            Button advanceArea = CreateAdvanceArea(canvas.transform);
            Image characterSprite = CreateCharacterSprite(canvas.transform);
            TextMeshProUGUI chapterLabel = CreateChapterLabel(canvas.transform);

            BuildMessageWindow(
                canvas.transform,
                out GameObject speakerPanel,
                out TextMeshProUGUI speakerLabel,
                out TextMeshProUGUI bodyLabel,
                out GameObject continueIndicator);

            BuildControlBar(
                canvas.transform,
                out GameObject controlBar,
                out Button barLog,
                out Button barSave,
                out Button barLoad,
                out Button barBgm,
                out TextMeshProUGUI barBgmLabel,
                out Button barTitle);

            BuildChoiceArea(canvas.transform, out RectTransform choiceRoot, out Button choiceTemplate);

            BuildResultPanel(
                canvas.transform,
                out GameObject resultPanel,
                out TextMeshProUGUI resultLabel,
                out Button backToTitleButton);

            BuildTitlePanel(
                canvas.transform,
                out GameObject titlePanel,
                out TextMeshProUGUI endingListLabel,
                out Button startButton,
                out Button titleLoadButton,
                out Button titleCreditsButton,
                out Button clearRecordButton);

            // ログ・クレジット・スロットはタイトルより手前。
            // タイトル画面からロードとクレジットを開けるようにするため。
            BuildScrollPanel(
                canvas.transform, "Log", "ログ", TextAlignmentOptions.TopLeft,
                out GameObject logPanel,
                out ScrollRect logScrollRect,
                out TextMeshProUGUI logText,
                out Button logCloseButton);

            BuildScrollPanel(
                canvas.transform, "Credits", "クレジット", TextAlignmentOptions.Top,
                out GameObject creditsPanel,
                out ScrollRect creditsScrollRect,
                out TextMeshProUGUI creditsLabel,
                out Button creditsCloseButton);

            BuildSlotPanel(
                canvas.transform,
                out GameObject slotPanel,
                out TextMeshProUGUI slotPanelTitle,
                out Button[] slotButtons,
                out TextMeshProUGUI[] slotLabels,
                out Button slotCloseButton);

            // 確認ダイアログは最前面。スロット画面の上にも出るため。
            BuildConfirmPanel(
                canvas.transform,
                out GameObject confirmPanel,
                out TextMeshProUGUI confirmLabel,
                out Button confirmYesButton,
                out Button confirmNoButton);

            // --- 参照の配線 ---
            Assign(so, "inkFile", LoadInkFile());
            Assign(so, "backgroundArea", backgroundArea);
            Assign(so, "background", background);
            Assign(so, "characterSprite", characterSprite);
            Assign(so, "chapterLabel", chapterLabel);
            Assign(so, "speakerPanel", speakerPanel);
            Assign(so, "speakerLabel", speakerLabel);
            Assign(so, "bodyLabel", bodyLabel);
            Assign(so, "continueIndicator", continueIndicator);
            Assign(so, "choiceRoot", choiceRoot);
            Assign(so, "choiceTemplate", choiceTemplate);
            Assign(so, "advanceArea", advanceArea);

            Assign(so, "controlBar", controlBar);
            Assign(so, "barLogButton", barLog);
            Assign(so, "barSaveButton", barSave);
            Assign(so, "barLoadButton", barLoad);
            Assign(so, "barBgmButton", barBgm);
            Assign(so, "barBgmLabel", barBgmLabel);
            Assign(so, "barTitleButton", barTitle);

            Assign(so, "logPanel", logPanel);
            Assign(so, "logScrollRect", logScrollRect);
            Assign(so, "logText", logText);
            Assign(so, "logCloseButton", logCloseButton);

            Assign(so, "creditsPanel", creditsPanel);
            Assign(so, "creditsScrollRect", creditsScrollRect);
            Assign(so, "creditsLabel", creditsLabel);
            Assign(so, "creditsCloseButton", creditsCloseButton);
            Assign(so, "titleCreditsButton", titleCreditsButton);

            Assign(so, "slotPanel", slotPanel);
            Assign(so, "slotPanelTitle", slotPanelTitle);
            AssignArray(so, "slotButtons", slotButtons);
            AssignArray(so, "slotLabels", slotLabels);
            Assign(so, "slotCloseButton", slotCloseButton);

            Assign(so, "confirmPanel", confirmPanel);
            Assign(so, "confirmLabel", confirmLabel);
            Assign(so, "confirmYesButton", confirmYesButton);
            Assign(so, "confirmNoButton", confirmNoButton);

            Assign(so, "titlePanel", titlePanel);
            Assign(so, "endingListLabel", endingListLabel);
            Assign(so, "startButton", startButton);
            Assign(so, "titleLoadButton", titleLoadButton);
            Assign(so, "clearRecordButton", clearRecordButton);
            Assign(so, "resultPanel", resultPanel);
            Assign(so, "resultLabel", resultLabel);
            Assign(so, "backToTitleButton", backToTitleButton);

            Assign(so, "audioDirector", audio);

            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();

            Debug.Log($"[SceneBuilder] シーンを生成しました: {ScenePath}");
        }

        private static InkFile LoadInkFile()
        {
            InkFile inkFile = InkAssetUtility.LoadCompiled(InkPath);
            if (inkFile == null)
            {
                Debug.LogError($"[SceneBuilder] ink ファイルが読み込めません: {InkPath}");
            }
            else if (inkFile.hasErrors)
            {
                Debug.LogError($"[SceneBuilder] ink のコンパイルエラーがあります: {InkPath}");
            }
            else if (!inkFile.isCompiled)
            {
                Debug.LogError($"[SceneBuilder] ink が未コンパイルのままです: {InkPath}");
            }

            return inkFile;
        }

        private static void Assign(SerializedObject so, string property, Object value)
        {
            SerializedProperty p = so.FindProperty(property);
            if (p == null)
            {
                Debug.LogError($"[SceneBuilder] フィールドが見つかりません: {property}");
                return;
            }

            p.objectReferenceValue = value;
        }

        private static void AssignArray(SerializedObject so, string property, Object[] values)
        {
            SerializedProperty p = so.FindProperty(property);
            if (p == null)
            {
                Debug.LogError($"[SceneBuilder] フィールドが見つかりません: {property}");
                return;
            }

            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        // ------------------------------------------------------------
        //  土台
        // ------------------------------------------------------------

        private static void CreateCamera()
        {
            // AudioListener が無いと BGM が一切鳴らないので必ず付ける。
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            Camera camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
        }

        private static void CreateEventSystem()
        {
            // Active Input Handling が旧 Input Manager のため StandaloneInputModule を使う。
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static AudioDirector CreateAudio(Transform parent)
        {
            var go = new GameObject("Audio", typeof(AudioDirector), typeof(AudioSource), typeof(AudioSource));
            go.transform.SetParent(parent, false);

            AudioSource[] sources = go.GetComponents<AudioSource>();
            var director = go.GetComponent<AudioDirector>();

            var so = new SerializedObject(director);
            so.FindProperty("sourceA").objectReferenceValue = sources[0];
            so.FindProperty("sourceB").objectReferenceValue = sources[1];
            so.ApplyModifiedPropertiesWithoutUndo();

            return director;
        }

        // ------------------------------------------------------------
        //  各パーツ
        // ------------------------------------------------------------

        private static void CreateBackground(Transform parent, out RectTransform area, out Image image)
        {
            // 画面からはみ出した分を隠すための外枠。写真を縦横比を保ったまま敷き詰めるのに使う。
            GameObject areaGo = NewUI("BackgroundArea", parent);
            StretchFull(areaGo);
            areaGo.AddComponent<RectMask2D>();
            area = (RectTransform)areaGo.transform;

            GameObject go = NewUI("Background", areaGo.transform);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(1920f, 1080f);

            image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            image.raycastTarget = false;
        }

        private static Button CreateAdvanceArea(Transform parent)
        {
            GameObject go = NewUI("AdvanceArea", parent);
            StretchFull(go);

            // 透明だがクリックは拾う。選択肢より手前に来ないよう、階層の早い位置に置く。
            Image image = go.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);

            Button button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            return button;
        }

        /// <summary>
        /// 立ち絵。メッセージウィンドウより奥（階層の手前側）に置くことで、
        /// 足元がウィンドウに隠れる一般的なノベルゲームの見え方になる。
        /// </summary>
        private static Image CreateCharacterSprite(Transform parent)
        {
            GameObject go = NewUI("CharacterSprite", parent);
            var rt = (RectTransform)go.transform;

            // 画面のやや右寄り、下端基準。
            rt.anchorMin = new Vector2(0.72f, 0f);
            rt.anchorMax = new Vector2(0.72f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 150f);
            rt.sizeDelta = new Vector2(820f, 1000f);

            Image image = go.AddComponent<Image>();
            image.raycastTarget = false;
            // 縦横比を保ったまま枠内に収める。差し替えた立ち絵の比率が違っても歪まない。
            image.preserveAspect = true;
            go.SetActive(false);

            return image;
        }

        private static TextMeshProUGUI CreateChapterLabel(Transform parent)
        {
            GameObject go = NewUI("ChapterLabel", parent);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(48f, -36f);
            rt.sizeDelta = new Vector2(1100f, 54f);

            TextMeshProUGUI text = AddText(go, string.Empty, 30f, TextAlignmentOptions.Left);
            text.color = new Color(1f, 1f, 1f, 0.72f);
            text.raycastTarget = false;
            return text;
        }

        private static void BuildMessageWindow(
            Transform parent,
            out GameObject speakerPanel,
            out TextMeshProUGUI speakerLabel,
            out TextMeshProUGUI bodyLabel,
            out GameObject continueIndicator)
        {
            GameObject window = NewUI("MessageWindow", parent);
            var rt = (RectTransform)window.transform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(90f, 60f);
            rt.offsetMax = new Vector2(-90f, 60f + 330f);

            Image panel = window.AddComponent<Image>();
            panel.color = PanelColor;
            panel.raycastTarget = false;

            GameObject body = NewUI("BodyLabel", window.transform);
            var bodyRt = (RectTransform)body.transform;
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(48f, 40f);
            bodyRt.offsetMax = new Vector2(-48f, -40f);
            bodyLabel = AddText(body, string.Empty, 38f, TextAlignmentOptions.TopLeft);
            bodyLabel.lineSpacing = 12f;

            speakerPanel = NewUI("SpeakerPanel", window.transform);
            var speakerRt = (RectTransform)speakerPanel.transform;
            speakerRt.anchorMin = new Vector2(0f, 1f);
            speakerRt.anchorMax = new Vector2(0f, 1f);
            speakerRt.pivot = new Vector2(0f, 0f);
            speakerRt.anchoredPosition = new Vector2(32f, 6f);
            speakerRt.sizeDelta = new Vector2(420f, 60f);

            Image speakerBg = speakerPanel.AddComponent<Image>();
            speakerBg.color = AccentColor;
            speakerBg.raycastTarget = false;

            GameObject speakerTextGo = NewUI("SpeakerLabel", speakerPanel.transform);
            StretchFull(speakerTextGo);
            speakerLabel = AddText(speakerTextGo, string.Empty, 30f, TextAlignmentOptions.Center);
            speakerLabel.color = new Color(0.08f, 0.07f, 0.06f, 1f);

            continueIndicator = NewUI("ContinueIndicator", window.transform);
            var indicatorRt = (RectTransform)continueIndicator.transform;
            indicatorRt.anchorMin = new Vector2(1f, 0f);
            indicatorRt.anchorMax = new Vector2(1f, 0f);
            indicatorRt.pivot = new Vector2(1f, 0f);
            indicatorRt.anchoredPosition = new Vector2(-32f, 16f);
            indicatorRt.sizeDelta = new Vector2(48f, 40f);
            TextMeshProUGUI indicatorText =
                AddText(continueIndicator, "▼", 30f, TextAlignmentOptions.Center);
            indicatorText.color = AccentColor;
        }

        private static void BuildControlBar(
            Transform parent,
            out GameObject bar,
            out Button logButton,
            out Button saveButton,
            out Button loadButton,
            out Button bgmButton,
            out TextMeshProUGUI bgmLabel,
            out Button titleButton)
        {
            bar = NewUI("ControlBar", parent);
            var rt = (RectTransform)bar.transform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            // メッセージウィンドウ (高さ330 + 下余白60) のすぐ上。
            rt.anchoredPosition = new Vector2(-90f, 60f + 330f + 12f);
            rt.sizeDelta = new Vector2(0f, 56f);

            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var fitter = bar.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            logButton = CreateBarButton(bar.transform, "BarLog", "ログ", 120f, out _);
            saveButton = CreateBarButton(bar.transform, "BarSave", "セーブ", 140f, out _);
            loadButton = CreateBarButton(bar.transform, "BarLoad", "ロード", 140f, out _);
            bgmButton = CreateBarButton(bar.transform, "BarBgm", "BGM ON", 160f, out bgmLabel);
            titleButton = CreateBarButton(bar.transform, "BarTitle", "タイトル", 150f, out _);
        }

        private static Button CreateBarButton(
            Transform parent, string name, string caption, float width, out TextMeshProUGUI label)
        {
            GameObject go = NewUI(name, parent);

            Image image = go.AddComponent<Image>();
            image.color = ChoiceIdle;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var element = go.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = 56f;

            GameObject labelGo = NewUI("Label", go.transform);
            StretchFull(labelGo);
            label = AddText(labelGo, caption, 24f, TextAlignmentOptions.Center);
            label.raycastTarget = false;

            return button;
        }

        private static void BuildChoiceArea(Transform parent, out RectTransform choiceRoot, out Button choiceTemplate)
        {
            GameObject root = NewUI("ChoiceRoot", parent);
            choiceRoot = (RectTransform)root.transform;
            choiceRoot.anchorMin = new Vector2(0.5f, 0.5f);
            choiceRoot.anchorMax = new Vector2(0.5f, 0.5f);
            choiceRoot.pivot = new Vector2(0.5f, 0.5f);
            choiceRoot.anchoredPosition = new Vector2(0f, 90f);
            choiceRoot.sizeDelta = new Vector2(1240f, 0f);

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = root.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject template = NewUI("ChoiceTemplate", root.transform);
            Image image = template.AddComponent<Image>();
            image.color = ChoiceIdle;

            choiceTemplate = template.AddComponent<Button>();
            ColorBlock colors = choiceTemplate.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.15f, 0.95f, 1f);
            colors.pressedColor = new Color(0.75f, 0.7f, 0.6f, 1f);
            choiceTemplate.colors = colors;
            choiceTemplate.targetGraphic = image;

            var element = template.AddComponent<LayoutElement>();
            element.minHeight = 86f;

            GameObject labelGo = NewUI("Label", template.transform);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(32f, 10f);
            labelRt.offsetMax = new Vector2(-32f, -10f);
            TextMeshProUGUI label = AddText(labelGo, "選択肢", 32f, TextAlignmentOptions.Center);
            label.raycastTarget = false;
        }

        // ------------------------------------------------------------
        //  バックログ
        // ------------------------------------------------------------

        /// <summary>
        /// 見出し + スクロール可能なテキスト + 閉じるボタン、という構成のパネル。
        /// ログ画面とクレジット画面で共用する。
        /// </summary>
        private static void BuildScrollPanel(
            Transform parent,
            string name,
            string heading,
            TextAlignmentOptions alignment,
            out GameObject panel,
            out ScrollRect scrollRect,
            out TextMeshProUGUI bodyText,
            out Button closeButton)
        {
            panel = NewUI(name, parent);
            StretchFull(panel);
            Image bg = panel.AddComponent<Image>();
            bg.color = OverlayColor;

            CreatePanelHeading(panel.transform, heading);

            GameObject scrollGo = NewUI("ScrollView", panel.transform);
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(140f, 140f);
            scrollRt.offsetMax = new Vector2(-140f, -150f);

            scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 45f;

            GameObject viewportGo = NewUI("Viewport", scrollGo.transform);
            StretchFull(viewportGo);
            viewportGo.AddComponent<RectMask2D>();
            var viewportRt = (RectTransform)viewportGo.transform;

            GameObject contentGo = NewUI("Content", viewportGo.transform);
            var contentRt = (RectTransform)contentGo.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = new Vector2(0f, 0f);
            contentRt.offsetMax = new Vector2(0f, 0f);

            bodyText = AddText(contentGo, string.Empty, 28f, alignment);
            bodyText.lineSpacing = 6f;

            // テキストの高さに合わせて Content が伸びることで、スクロール範囲が決まる。
            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;

            closeButton = CreateLabeledButton(
                panel.transform, name + "CloseButton", "閉じる", new Vector2(0f, 0f), new Vector2(360f, 76f));
            var closeRt = (RectTransform)closeButton.transform;
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 40f);
        }

        // ------------------------------------------------------------
        //  セーブ / ロード
        // ------------------------------------------------------------

        private static void BuildSlotPanel(
            Transform parent,
            out GameObject panel,
            out TextMeshProUGUI heading,
            out Button[] slotButtons,
            out TextMeshProUGUI[] slotLabels,
            out Button closeButton)
        {
            panel = NewUI("SlotPanel", parent);
            StretchFull(panel);
            Image bg = panel.AddComponent<Image>();
            bg.color = OverlayColor;

            heading = CreatePanelHeading(panel.transform, "セーブ");

            slotButtons = new Button[SaveSystem.SlotCount];
            slotLabels = new TextMeshProUGUI[SaveSystem.SlotCount];

            const float slotHeight = 130f;
            const float spacing = 26f;
            float totalHeight = SaveSystem.SlotCount * slotHeight + (SaveSystem.SlotCount - 1) * spacing;
            float top = totalHeight * 0.5f - slotHeight * 0.5f;

            for (int i = 0; i < SaveSystem.SlotCount; i++)
            {
                float y = top - i * (slotHeight + spacing);
                Button button = CreateLabeledButton(
                    panel.transform,
                    $"Slot{i}",
                    string.Empty,
                    new Vector2(0f, y + 40f),
                    new Vector2(1000f, slotHeight));

                slotButtons[i] = button;
                slotLabels[i] = button.GetComponentInChildren<TextMeshProUGUI>();
                slotLabels[i].fontSize = 30f;
            }

            closeButton = CreateLabeledButton(
                panel.transform, "SlotCloseButton", "閉じる", new Vector2(0f, 0f), new Vector2(360f, 76f));
            var closeRt = (RectTransform)closeButton.transform;
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 40f);
        }

        // ------------------------------------------------------------
        //  確認ダイアログ
        // ------------------------------------------------------------

        private static void BuildConfirmPanel(
            Transform parent,
            out GameObject panel,
            out TextMeshProUGUI label,
            out Button yesButton,
            out Button noButton)
        {
            panel = NewUI("ConfirmPanel", parent);
            StretchFull(panel);
            Image dim = panel.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.7f);

            GameObject box = NewUI("Box", panel.transform);
            var boxRt = (RectTransform)box.transform;
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.anchoredPosition = Vector2.zero;
            boxRt.sizeDelta = new Vector2(920f, 400f);
            Image boxBg = box.AddComponent<Image>();
            boxBg.color = new Color(0.08f, 0.09f, 0.13f, 1f);

            GameObject labelGo = NewUI("ConfirmLabel", box.transform);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot = new Vector2(0.5f, 1f);
            labelRt.anchoredPosition = new Vector2(0f, -56f);
            labelRt.sizeDelta = new Vector2(-80f, 180f);
            label = AddText(labelGo, string.Empty, 32f, TextAlignmentOptions.Center);

            yesButton = CreateLabeledButton(
                box.transform, "ConfirmYes", "はい", new Vector2(-190f, -120f), new Vector2(320f, 84f));
            noButton = CreateLabeledButton(
                box.transform, "ConfirmNo", "いいえ", new Vector2(190f, -120f), new Vector2(320f, 84f));
        }

        // ------------------------------------------------------------
        //  タイトル / リザルト
        // ------------------------------------------------------------

        private static void BuildTitlePanel(
            Transform parent,
            out GameObject titlePanel,
            out TextMeshProUGUI endingListLabel,
            out Button startButton,
            out Button loadButton,
            out Button creditsButton,
            out Button clearRecordButton)
        {
            titlePanel = NewUI("TitlePanel", parent);
            StretchFull(titlePanel);
            Image bg = titlePanel.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.04f, 0.07f, 0.94f);

            GameObject titleGo = NewUI("Title", titlePanel.transform);
            var titleRt = (RectTransform)titleGo.transform;
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -120f);
            titleRt.sizeDelta = new Vector2(1200f, 130f);
            TextMeshProUGUI title = AddText(titleGo, "三か月の春", 88f, TextAlignmentOptions.Center);
            title.color = AccentColor;

            GameObject subtitleGo = NewUI("Subtitle", titlePanel.transform);
            var subtitleRt = (RectTransform)subtitleGo.transform;
            subtitleRt.anchorMin = new Vector2(0.5f, 1f);
            subtitleRt.anchorMax = new Vector2(0.5f, 1f);
            subtitleRt.pivot = new Vector2(0.5f, 1f);
            subtitleRt.anchoredPosition = new Vector2(0f, -252f);
            subtitleRt.sizeDelta = new Vector2(1200f, 50f);
            TextMeshProUGUI subtitle =
                AddText(subtitleGo, "Three Months of Spring", 28f, TextAlignmentOptions.Center);
            subtitle.color = new Color(1f, 1f, 1f, 0.5f);

            GameObject listGo = NewUI("EndingList", titlePanel.transform);
            var listRt = (RectTransform)listGo.transform;
            listRt.anchorMin = new Vector2(0.5f, 0.5f);
            listRt.anchorMax = new Vector2(0.5f, 0.5f);
            listRt.pivot = new Vector2(0.5f, 0.5f);
            listRt.anchoredPosition = new Vector2(0f, 30f);
            listRt.sizeDelta = new Vector2(900f, 280f);
            endingListLabel = AddText(listGo, string.Empty, 28f, TextAlignmentOptions.Top);
            endingListLabel.color = new Color(1f, 1f, 1f, 0.85f);

            startButton = CreateLabeledButton(
                titlePanel.transform, "StartButton", "はじめから", new Vector2(0f, 360f), new Vector2(420f, 92f));
            loadButton = CreateLabeledButton(
                titlePanel.transform, "TitleLoadButton", "つづきから", new Vector2(0f, 252f), new Vector2(420f, 92f));
            creditsButton = CreateLabeledButton(
                titlePanel.transform, "TitleCreditsButton", "クレジット", new Vector2(0f, 162f), new Vector2(420f, 64f));
            clearRecordButton = CreateLabeledButton(
                titlePanel.transform, "ClearRecordButton", "エンディング記録を消す", new Vector2(0f, 84f), new Vector2(420f, 64f));

            // ボタン群はリストの下に並べる。アンカー基準を画面下側に変えておく。
            foreach (Button button in new[] { startButton, loadButton, creditsButton, clearRecordButton })
            {
                var rt = (RectTransform)button.transform;
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
            }
        }

        private static void BuildResultPanel(
            Transform parent,
            out GameObject resultPanel,
            out TextMeshProUGUI resultLabel,
            out Button backToTitleButton)
        {
            resultPanel = NewUI("ResultPanel", parent);
            StretchFull(resultPanel);
            Image bg = resultPanel.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.02f, 0.04f, 0.9f);

            GameObject labelGo = NewUI("ResultLabel", resultPanel.transform);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = new Vector2(0.5f, 0.5f);
            labelRt.anchorMax = new Vector2(0.5f, 0.5f);
            labelRt.pivot = new Vector2(0.5f, 0.5f);
            labelRt.anchoredPosition = new Vector2(0f, 60f);
            labelRt.sizeDelta = new Vector2(1300f, 300f);
            resultLabel = AddText(labelGo, string.Empty, 46f, TextAlignmentOptions.Center);
            resultLabel.color = AccentColor;

            backToTitleButton = CreateLabeledButton(
                resultPanel.transform, "BackToTitleButton", "タイトルへ", new Vector2(0f, -170f), new Vector2(420f, 92f));
        }

        // ------------------------------------------------------------
        //  ヘルパ
        // ------------------------------------------------------------

        private static TextMeshProUGUI CreatePanelHeading(Transform parent, string caption)
        {
            GameObject go = NewUI("Heading", parent);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -50f);
            rt.sizeDelta = new Vector2(1000f, 70f);

            TextMeshProUGUI text = AddText(go, caption, 46f, TextAlignmentOptions.Center);
            text.color = AccentColor;
            return text;
        }

        private static Button CreateLabeledButton(
            Transform parent, string name, string caption, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = NewUI(name, parent);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;

            Image image = go.AddComponent<Image>();
            image.color = ChoiceIdle;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

            GameObject labelGo = NewUI("Label", go.transform);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(24f, 8f);
            labelRt.offsetMax = new Vector2(-24f, -8f);
            TextMeshProUGUI label = AddText(labelGo, caption, 30f, TextAlignmentOptions.Center);
            label.raycastTarget = false;

            return button;
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(GameObject go)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI AddText(GameObject go, string content, float size, TextAlignmentOptions alignment)
        {
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = TextColor;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }
    }
}
