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

        [MenuItem("Tools/三か月の春/2. シーンを生成する")]
        public static void BuildScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateEventSystem();

            Canvas canvas = CreateCanvas();
            var controller = canvas.gameObject.AddComponent<NovelGameController>();
            var so = new SerializedObject(controller);

            // --- 描画順 = ヒエラルキー順。先に作ったものほど奥に来る。 ---
            Image background = CreateBackground(canvas.transform);
            Button advanceArea = CreateAdvanceArea(canvas.transform);
            TextMeshProUGUI chapterLabel = CreateChapterLabel(canvas.transform);

            BuildMessageWindow(
                canvas.transform,
                out GameObject speakerPanel,
                out TextMeshProUGUI speakerLabel,
                out TextMeshProUGUI bodyLabel,
                out GameObject continueIndicator);

            BuildChoiceArea(canvas.transform, out RectTransform choiceRoot, out Button choiceTemplate);

            BuildTitlePanel(
                canvas.transform,
                out GameObject titlePanel,
                out TextMeshProUGUI endingListLabel,
                out Button startButton,
                out Button clearRecordButton);

            BuildResultPanel(
                canvas.transform,
                out GameObject resultPanel,
                out TextMeshProUGUI resultLabel,
                out Button backToTitleButton);

            // --- 参照の配線 ---
            so.FindProperty("inkFile").objectReferenceValue = LoadInkFile();
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("chapterLabel").objectReferenceValue = chapterLabel;
            so.FindProperty("speakerPanel").objectReferenceValue = speakerPanel;
            so.FindProperty("speakerLabel").objectReferenceValue = speakerLabel;
            so.FindProperty("bodyLabel").objectReferenceValue = bodyLabel;
            so.FindProperty("continueIndicator").objectReferenceValue = continueIndicator;
            so.FindProperty("choiceRoot").objectReferenceValue = choiceRoot;
            so.FindProperty("choiceTemplate").objectReferenceValue = choiceTemplate;
            so.FindProperty("advanceArea").objectReferenceValue = advanceArea;
            so.FindProperty("titlePanel").objectReferenceValue = titlePanel;
            so.FindProperty("endingListLabel").objectReferenceValue = endingListLabel;
            so.FindProperty("startButton").objectReferenceValue = startButton;
            so.FindProperty("clearRecordButton").objectReferenceValue = clearRecordButton;
            so.FindProperty("resultPanel").objectReferenceValue = resultPanel;
            so.FindProperty("resultLabel").objectReferenceValue = resultLabel;
            so.FindProperty("backToTitleButton").objectReferenceValue = backToTitleButton;
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

        // ------------------------------------------------------------
        //  土台
        // ------------------------------------------------------------

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera));
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

        // ------------------------------------------------------------
        //  各パーツ
        // ------------------------------------------------------------

        private static Image CreateBackground(Transform parent)
        {
            GameObject go = NewUI("Background", parent);
            StretchFull(go);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            image.raycastTarget = false;
            return image;
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

            // 本文
            GameObject body = NewUI("BodyLabel", window.transform);
            var bodyRt = (RectTransform)body.transform;
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(48f, 40f);
            bodyRt.offsetMax = new Vector2(-48f, -40f);
            bodyLabel = AddText(body, string.Empty, 38f, TextAlignmentOptions.TopLeft);
            bodyLabel.lineSpacing = 12f;

            // 話者名
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

            // 次へ進めることを示す ▼
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

            // 実行時に複製される雛形。Awake で非アクティブにされる。
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

        private static void BuildTitlePanel(
            Transform parent,
            out GameObject titlePanel,
            out TextMeshProUGUI endingListLabel,
            out Button startButton,
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
            titleRt.anchoredPosition = new Vector2(0f, -140f);
            titleRt.sizeDelta = new Vector2(1200f, 130f);
            TextMeshProUGUI title = AddText(titleGo, "三か月の春", 88f, TextAlignmentOptions.Center);
            title.color = AccentColor;

            GameObject subtitleGo = NewUI("Subtitle", titlePanel.transform);
            var subtitleRt = (RectTransform)subtitleGo.transform;
            subtitleRt.anchorMin = new Vector2(0.5f, 1f);
            subtitleRt.anchorMax = new Vector2(0.5f, 1f);
            subtitleRt.pivot = new Vector2(0.5f, 1f);
            subtitleRt.anchoredPosition = new Vector2(0f, -272f);
            subtitleRt.sizeDelta = new Vector2(1200f, 50f);
            TextMeshProUGUI subtitle =
                AddText(subtitleGo, "Three Months of Spring", 28f, TextAlignmentOptions.Center);
            subtitle.color = new Color(1f, 1f, 1f, 0.5f);

            GameObject listGo = NewUI("EndingList", titlePanel.transform);
            var listRt = (RectTransform)listGo.transform;
            listRt.anchorMin = new Vector2(0.5f, 0.5f);
            listRt.anchorMax = new Vector2(0.5f, 0.5f);
            listRt.pivot = new Vector2(0.5f, 0.5f);
            listRt.anchoredPosition = new Vector2(0f, -30f);
            listRt.sizeDelta = new Vector2(900f, 330f);
            endingListLabel = AddText(listGo, string.Empty, 28f, TextAlignmentOptions.Top);
            endingListLabel.color = new Color(1f, 1f, 1f, 0.85f);

            startButton = CreateLabeledButton(
                titlePanel.transform, "StartButton", "はじめから", new Vector2(0f, 170f), new Vector2(420f, 92f));
            clearRecordButton = CreateLabeledButton(
                titlePanel.transform, "ClearRecordButton", "エンディング記録を消す", new Vector2(0f, 64f), new Vector2(420f, 64f));
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
            StretchFull(labelGo);
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
