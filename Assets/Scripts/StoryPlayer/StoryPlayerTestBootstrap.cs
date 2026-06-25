using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace GameCreate3.StoryPlayer
{
    /// <summary>
    /// [DEPRECATED] 测试场景一键自搭播放器（Awake 时 new 出 Canvas + 7 个剧情组件 + EventSystem + 主相机）。
    /// 生产关卡请改用 <see cref="StoryPlayerService"/> 静态门面 + 触发器 prefab：
    ///   - StoryTriggerZone：玩家走入触发器
    ///   - StoryInteractable：按交互键触发
    ///   - StoryWorkspaceEventTrigger：监听工作区事件
    ///   - StoryAutoPlay：场景加载完立即播放
    /// 本组件保留仅为兼容旧测试场景。
    /// </summary>
    [System.Obsolete("Production code: use StoryPlayerService + Trigger prefabs. This bootstrap is for legacy test scenes only.", false)]
    public sealed class StoryPlayerTestBootstrap : MonoBehaviour
    {
        [Header("Test Sequence")]
        [SerializeField] private StorySequence testSequence;
        [SerializeField] private TMP_FontAsset chineseFontAsset;

        [Header("UI References (Auto-created if null)")]
        [SerializeField] private Canvas storyCanvas;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text speakerLabel;
        [SerializeField] private TMP_Text contentLabel;
        [SerializeField] private CanvasGroup fadeOverlay;

        private StoryPlayer storyPlayer;
        private StoryPageRenderer pageRenderer;
        private SimpleTransitionController transitionController;
        private StoryInputController inputController;
        private StoryAudioAdapter audioAdapter;
        private StoryEventSystem eventSystem;
        private StoryFlowBridge flowBridge;
        private TMP_Text testGuideLabel;
        private TMP_Text runtimeStateLabel;
        private TMP_FontAsset runtimeChineseFontAsset;

        private void Awake()
        {
            SetupScene();
        }

        private void Start()
        {
            if (testSequence != null)
            {
                storyPlayer.Play(testSequence);
            }
            else
            {
                Debug.Log("[StoryPlayerTestBootstrap] No test sequence assigned, using generated test data.");
                testSequence = StoryTestDataGenerator.CreateTestSequence();
                storyPlayer.Play(testSequence);
            }
        }

        private void SetupScene()
        {
            // Create Camera if not exists
            if (Camera.main == null)
            {
                var cameraObj = new GameObject("Main Camera");
                cameraObj.tag = "MainCamera";
                var camera = cameraObj.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5;
                camera.transform.position = new Vector3(0, 0, -10);
                cameraObj.AddComponent<AudioListener>();
            }

            // Create Canvas
            if (storyCanvas == null)
            {
                var canvasObj = new GameObject("StoryCanvas");
                storyCanvas = canvasObj.AddComponent<Canvas>();
                storyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                storyCanvas.sortingOrder = 0;

                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                // 按宽度匹配：16:10 等更高屏幕下横向布局与 16:9 像素一致，多出的高度仅作留白。
                scaler.matchWidthOrHeight = 0f;

                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                storyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                storyCanvas.sortingOrder = 0;
            }

            var canvasRect = storyCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.localScale = Vector3.one;
                canvasRect.anchorMin = Vector2.zero;
                canvasRect.anchorMax = Vector2.one;
                canvasRect.offsetMin = Vector2.zero;
                canvasRect.offsetMax = Vector2.zero;
            }

            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }

            // Create Background Image
            if (backgroundImage == null)
            {
                var bgObj = new GameObject("BackgroundImage", typeof(RectTransform));
                bgObj.transform.SetParent(storyCanvas.transform, false);
                backgroundImage = bgObj.AddComponent<Image>();
                backgroundImage.color = Color.black;

                var rect = bgObj.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            // Create Text Container
            var textContainerObj = new GameObject("TextContainer", typeof(RectTransform));
            textContainerObj.transform.SetParent(storyCanvas.transform, false);
            var textContainer = textContainerObj.GetComponent<RectTransform>();
            textContainer.anchorMin = new Vector2(0, 0);
            textContainer.anchorMax = new Vector2(1, 0.3f);
            textContainer.offsetMin = new Vector2(50, 20);
            textContainer.offsetMax = new Vector2(-50, -20);

            // Create Speaker Label
            if (speakerLabel == null)
            {
                var speakerObj = new GameObject("SpeakerLabel", typeof(RectTransform));
                speakerObj.transform.SetParent(textContainer, false);
                speakerLabel = speakerObj.AddComponent<TextMeshProUGUI>();
                speakerLabel.fontSize = 24;
                speakerLabel.color = Color.white;
                speakerLabel.alignment = TextAlignmentOptions.Left;

                var speakerRect = speakerObj.GetComponent<RectTransform>();
                speakerRect.anchorMin = new Vector2(0, 1);
                speakerRect.anchorMax = new Vector2(1, 1);
                speakerRect.pivot = new Vector2(0.5f, 1);
                speakerRect.sizeDelta = new Vector2(0, 40);
                speakerRect.anchoredPosition = new Vector2(0, 0);
            }

            // Create Content Label
            if (contentLabel == null)
            {
                var contentObj = new GameObject("ContentLabel", typeof(RectTransform));
                contentObj.transform.SetParent(textContainer, false);
                contentLabel = contentObj.AddComponent<TextMeshProUGUI>();
                contentLabel.fontSize = 32;
                contentLabel.color = Color.white;
                contentLabel.alignment = TextAlignmentOptions.TopLeft;

                var contentRect = contentObj.GetComponent<RectTransform>();
                contentRect.anchorMin = Vector2.zero;
                contentRect.anchorMax = Vector2.one;
                contentRect.offsetMin = new Vector2(0, 0);
                contentRect.offsetMax = new Vector2(0, -50);
            }

            runtimeChineseFontAsset = ResolveChineseFontAsset();
            if (runtimeChineseFontAsset != null)
            {
                speakerLabel.font = runtimeChineseFontAsset;
                contentLabel.font = runtimeChineseFontAsset;
            }

            // Create Fade Overlay
            if (fadeOverlay == null)
            {
                var fadeObj = new GameObject("FadeOverlay", typeof(RectTransform));
                fadeObj.transform.SetParent(storyCanvas.transform, false);
                var fadeImage = fadeObj.AddComponent<Image>();
                fadeImage.color = Color.black;
                fadeOverlay = fadeObj.AddComponent<CanvasGroup>();
                fadeOverlay.alpha = 0;

                var fadeRect = fadeObj.GetComponent<RectTransform>();
                fadeRect.anchorMin = Vector2.zero;
                fadeRect.anchorMax = Vector2.one;
                fadeRect.offsetMin = Vector2.zero;
                fadeRect.offsetMax = Vector2.zero;
            }

            // Create Input Blocker
            var inputBlockerObj = new GameObject("InputBlocker", typeof(RectTransform));
            inputBlockerObj.transform.SetParent(storyCanvas.transform, false);
            var blockerImage = inputBlockerObj.AddComponent<Image>();
            blockerImage.color = Color.clear;
            var blockerButton = inputBlockerObj.AddComponent<Button>();
            blockerButton.transition = Selectable.Transition.None;

            var blockerRect = inputBlockerObj.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            // Create StoryPlayer GameObject and Components
            var storyPlayerObj = new GameObject("StoryPlayerSystem");

            // Add StoryPlayer
            storyPlayer = storyPlayerObj.AddComponent<StoryPlayer>();

            // Add StoryPageRenderer
            pageRenderer = storyPlayerObj.AddComponent<StoryPageRenderer>();
            // Use reflection to set private fields
            SetPrivateField(pageRenderer, "backgroundImage", backgroundImage);
            SetPrivateField(pageRenderer, "speakerLabel", speakerLabel);
            SetPrivateField(pageRenderer, "contentLabel", contentLabel);
            SetPrivateField(pageRenderer, "fadeOverlay", fadeOverlay);

            // Add TransitionController
            transitionController = storyPlayerObj.AddComponent<SimpleTransitionController>();
            SetPrivateField(transitionController, "transitionCanvas", storyCanvas);
            SetPrivateField(transitionController, "canvasGroup", fadeOverlay);
            SetPrivateField(transitionController, "transitionRect", fadeOverlay.GetComponent<RectTransform>());

            // Add InputController
            inputController = storyPlayerObj.AddComponent<StoryInputController>();

            // Add AudioAdapter
            audioAdapter = storyPlayerObj.AddComponent<StoryAudioAdapter>();

            // Add EventSystem
            eventSystem = storyPlayerObj.AddComponent<StoryEventSystem>();

            // Add FlowBridge
            flowBridge = storyPlayerObj.AddComponent<StoryFlowBridge>();

            // Initialize StoryPlayer
            storyPlayer.Initialize(pageRenderer, transitionController, audioAdapter, eventSystem);

            // Bind Input
            inputController.Initialize(storyPlayer, pageRenderer);
            inputController.OnNextPageRequested += () => storyPlayer.NextPage();
            inputController.OnSkipSequenceRequested += () => storyPlayer.SkipSequence();

            // Bind FlowBridge
            flowBridge.BindStoryPlayer(storyPlayer);

            storyPlayer.OnPageChanged += index => Debug.Log($"[StoryPlayerTestBootstrap] Page changed: {index}");
            storyPlayer.OnSequenceCompleted += () => Debug.Log("[StoryPlayerTestBootstrap] Sequence completed.");
            storyPlayer.OnSequenceSkipped += () => Debug.Log("[StoryPlayerTestBootstrap] Sequence skip requested.");
            storyPlayer.OnPageEvent += evt => Debug.Log($"[StoryPlayerTestBootstrap] Event: {evt.EventType} | {evt.EventData}");
            storyPlayer.OnStateChanged += HandleStoryStateChanged;
            storyPlayer.OnPageChanged += HandlePageChanged;
            storyPlayer.OnSequenceCompleted += () => UpdateRuntimeStateLabel("Flow ended: Completed");
            storyPlayer.OnSequenceSkipped += () => UpdateRuntimeStateLabel("Flow ended: Skipped");

            CreateTestGuidePanel();
            UpdateRuntimeStateLabel("Waiting to start...");

            Debug.Log("[StoryPlayerTestBootstrap] Scene setup complete!");
        }

        private void HandleStoryStateChanged(StoryPlayerState state)
        {
            UpdateRuntimeStateLabel($"State changed: {state}");
        }

        private void HandlePageChanged(int pageIndex)
        {
            var pageText = pageIndex switch
            {
                0 => "Page 1: Auto advance (no input)",
                1 => "Page 2: Manual advance / typewriter fast-forward / hold 1.5s to skip",
                2 => "Page 3: Event page (auto sound + variable set)",
                3 => "Page 4: Final check (click complete or Esc/hold skip)",
                _ => $"Page {pageIndex + 1}"
            };

            UpdateRuntimeStateLabel(pageText);
        }

        private void CreateTestGuidePanel()
        {
            if (storyCanvas == null)
            {
                return;
            }

            var panelObj = new GameObject("TestGuidePanel", typeof(RectTransform), typeof(Image));
            panelObj.transform.SetParent(storyCanvas.transform, false);
            var panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(20f, -20f);
            panelRect.sizeDelta = new Vector2(720f, 240f);

            var panelImage = panelObj.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.65f);

            testGuideLabel = CreatePanelText(
                panelObj.transform,
                "Test Guide:\n" +
                "1) Page 1 auto-advances (no input)\n" +
                "2) Page 2 click to advance; click during typewriter = fast-forward\n" +
                "3) Hold ~1.5s or press Esc = trigger Skip\n" +
                "4) Page 3 auto-triggers events (check Console)\n" +
                "5) Page 4 click to complete flow",
                18,
                new Vector2(10f, -10f),
                new Vector2(-10f, -90f));

            runtimeStateLabel = CreatePanelText(
                panelObj.transform,
                "State: Not Started",
                22,
                new Vector2(10f, -155f),
                new Vector2(-10f, -10f));
            runtimeStateLabel.color = new Color(1f, 0.93f, 0.35f, 1f);
        }

        private TMP_Text CreatePanelText(Transform parent, string text, int fontSize, Vector2 offsetMin, Vector2 offsetMax)
        {
            var textObj = new GameObject("Label", typeof(RectTransform));
            textObj.transform.SetParent(parent, false);
            var label = textObj.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.TopLeft;

            if (runtimeChineseFontAsset != null)
            {
                label.font = runtimeChineseFontAsset;
            }

            var rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return label;
        }

        private void UpdateRuntimeStateLabel(string text)
        {
            if (runtimeStateLabel != null)
            {
                runtimeStateLabel.text = $"Current State: {text}";
            }
        }

        private TMP_FontAsset CreateChineseFontAsset()
        {
            var preferredFonts = new List<string>
            {
                "Microsoft YaHei",
                "Microsoft JhengHei",
                "SimHei",
                "SimSun",
                "Arial Unicode MS"
            };

            foreach (var fontName in preferredFonts)
            {
                try
                {
                    var osFont = Font.CreateDynamicFontFromOSFont(fontName, 32);
                    if (osFont == null)
                    {
                        continue;
                    }

                    var fontAsset = TMP_FontAsset.CreateFontAsset(osFont);

                    if (fontAsset != null)
                    {
                        Debug.Log($"[StoryPlayerTestBootstrap] Using runtime Chinese font: {fontName}");
                        return fontAsset;
                    }
                }
                catch
                {
                    // Try next font.
                }
            }

            Debug.LogWarning("[StoryPlayerTestBootstrap] No system Chinese font asset was created. Please assign a TMP font manually.");
            return null;
        }

        private TMP_FontAsset ResolveChineseFontAsset()
        {
            if (chineseFontAsset == null)
            {
                return CreateChineseFontAsset();
            }

            if (chineseFontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic)
            {
                return chineseFontAsset;
            }

            if (chineseFontAsset.sourceFontFile != null)
            {
                var dynamicFromSource = TMP_FontAsset.CreateFontAsset(chineseFontAsset.sourceFontFile);
                if (dynamicFromSource != null)
                {
                    Debug.Log("[StoryPlayerTestBootstrap] Converted assigned font to runtime dynamic TMP font.");
                    return dynamicFromSource;
                }
            }

            Debug.LogWarning("[StoryPlayerTestBootstrap] Assigned font is static and has no source font file. Missing Chinese glyphs may appear as squares.");
            return chineseFontAsset;
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
    }
}
