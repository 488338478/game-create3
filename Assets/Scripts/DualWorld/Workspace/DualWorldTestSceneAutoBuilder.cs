using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCreate3.DualWorld
{
    public static class DualWorldTestSceneAutoBuilder
    {
        private static bool hasBuilt;

        public static void BuildIfNeeded()
        {
            EnsureEventSystem();

            if (hasBuilt || Object.FindObjectOfType<DualWorldWorkspace>() != null)
            {
                hasBuilt = true;
                return;
            }

            Build();
            hasBuilt = true;
        }

        /// <summary>
        /// Editor-only hook：把"已建过"标志清掉，让 Editor 工具能多次重新生成 prefab。
        /// 不要在运行时调用。
        /// </summary>
        public static void ResetForEditor() => hasBuilt = false;

        private static void EnsureEventSystem()
        {
            // 没有 EventSystem 时，UI 的 IDragHandler / Button.onClick / GraphicRaycaster 全部不会触发。
            // 自动搭建场景必须自带，否则即便挂了 GraphicRaycaster 也是死的。
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            // StandaloneInputModule 直接读 legacy Input —— 项目 activeInputHandler = Both，无需 UIActions 资源。
            // 不用 InputSystemUIInputModule：运行时 AddComponent 不会自动绑默认 actions，会导致鼠标事件丢失。
            es.AddComponent<StandaloneInputModule>();
        }

        private static void Build()
        {
            var root = new GameObject("DualWorldRoot");
            // Build inactive so SideScrollWorkspaceBase.Awake/Initialize/Start defers until refs are wired
            // (otherwise ScanSceneObjects would run before triggers/players exist and they'd never bind).
            root.SetActive(false);
            var workspace = root.AddComponent<DualWorldWorkspace>();

            var flowGo = new GameObject("LevelInGameFlow");
            flowGo.transform.SetParent(root.transform, false);
            var flowController = flowGo.AddComponent<LevelInGameFlowController>();

            var realityRoot = new GameObject("RealityRoot");
            realityRoot.transform.SetParent(root.transform, false);
            var realityCanvas = BuildRealityCanvas(realityRoot.transform, out var alignmentTask, out var realityPanel);

            var dreamRoot = new GameObject("DreamRoot");
            dreamRoot.transform.SetParent(root.transform, false);
            var pushTarget = BuildDreamScene(dreamRoot.transform, out var pathOpener, out var exitTriggerGo);

            // Persistent UI canvas — must NOT live under realityRoot (which gets toggled by AlignmentSubLevelFlow).
            var persistentUiGo = new GameObject("PersistentUI");
            persistentUiGo.transform.SetParent(root.transform, false);
            var persistentCanvas = persistentUiGo.AddComponent<Canvas>();
            persistentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            persistentCanvas.sortingOrder = 10;
            ConfigureCanvasScaler(persistentUiGo.AddComponent<CanvasScaler>());
            persistentUiGo.AddComponent<GraphicRaycaster>();

            var (chatController, chatPanel) = BuildChatPanel(persistentCanvas.transform, workspace);
            var taskDef = BuildAlignmentTaskDefinition();

            var subLevelGo = new GameObject("AlignmentSubLevel");
            subLevelGo.transform.SetParent(flowGo.transform, false);
            var alignmentFlow = subLevelGo.AddComponent<AlignmentSubLevelFlow>();
            ApplyAlignmentFlowFields(alignmentFlow, alignmentTask, realityRoot, pushTarget, dreamRoot, taskDef);

            var bridgesGo = new GameObject("CrossWorldBridges");
            bridgesGo.transform.SetParent(root.transform, false);
            var enhancer = bridgesGo.AddComponent<DreamToRealityEnhancer>();
            var repair = bridgesGo.AddComponent<RealityToDreamRepair>();
            ApplyBridgeFields(enhancer, workspace, alignmentTask);
            ApplyBridgeFields(repair, workspace, pathOpener);

            ApplyFlowControllerSubLevels(flowController, new List<BaseSubLevelFlow> { alignmentFlow });
            ApplyWorkspaceFields(workspace, flowController, chatController);

            // Real SideScroll player + Cinemachine camera so DreamTaskUnlocked / DreamTraversalActive are playable.
            var playerSpawnPos = new Vector3(-5f, -1.8f, 0f);
            var player = BuildSideScrollPlayer(root.transform, playerSpawnPos);
            var confiner = BuildCameraBounds(root.transform);
            var cameraController = BuildCameraRig(root.transform, player.transform, confiner);
            ApplyWorkspaceCharacterFields(workspace, player.GetComponent<SideScrollCharacterControllerBase>(), cameraController);

            // 双屏布局：UI 拼图 → 左半屏，横版世界 → 右半屏（通过 Camera.rect 裁剪）。
            var screenLayout = root.AddComponent<DualWorldScreenLayout>();
            screenLayout.Initialize(Camera.main, realityPanel, DualWorldScreenMode.SplitDreamFocus);

            // 调试按钮 —— 移到屏幕底部正中（不挡左侧拼图也不挡右侧玩家路径）。
            BuildDebugButton(persistentCanvas.transform, "DEBUG: 模拟梦境完成",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-120f, 30f), alignmentFlow.OnDreamComplete);
            BuildDebugButton(persistentCanvas.transform, "DEBUG: 模拟走到出口",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(120f, 30f), alignmentFlow.OnTraversalReachedExit);

            // Activate now — Awake → Initialize (resolve refs, scan triggers, bind workspace) → Start → Enter → flow begins.
            root.SetActive(true);

            Debug.Log("[DualWorldTestSceneAutoBuilder] Dual world built: 左 UI 拼图 + 右横板。键盘 A/D/Space 控制玩家，鼠标拖拽左侧方块。");
        }

        private static void BuildDebugButton(Transform canvasTransform, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(text);
            go.transform.SetParent(canvasTransform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(220f, 40f);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.4f, 0.2f, 0.2f, 0.9f);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }

        private static Canvas BuildRealityCanvas(Transform parent, out RealityAlignmentTask alignmentTask, out RectTransform realityPanel)
        {
            var canvasGo = new GameObject("RealityCanvas");
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ConfigureCanvasScaler(canvasGo.AddComponent<CanvasScaler>());
            canvasGo.AddComponent<GraphicRaycaster>();

            // RealityPanel 会被 DualWorldScreenLayout 锚定到屏幕左半。
            // 这里给它一个深色底板，既圈住拼图区域，也和右侧横板形成视觉分屏。
            var panelGo = new GameObject("RealityPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            realityPanel = panelGo.AddComponent<RectTransform>();
            realityPanel.anchorMin = Vector2.zero;
            realityPanel.anchorMax = new Vector2(0.5f, 1f);
            realityPanel.offsetMin = new Vector2(20f, 20f);
            realityPanel.offsetMax = new Vector2(-20f, -20f);
            var panelBg = panelGo.AddComponent<Image>();
            panelBg.color = new Color(0.10f, 0.12f, 0.18f, 0.96f);

            var panelTitleGo = new GameObject("PanelTitle");
            panelTitleGo.transform.SetParent(panelGo.transform, false);
            var panelTitleRect = panelTitleGo.AddComponent<RectTransform>();
            panelTitleRect.anchorMin = new Vector2(0f, 1f);
            panelTitleRect.anchorMax = new Vector2(1f, 1f);
            panelTitleRect.pivot = new Vector2(0.5f, 1f);
            panelTitleRect.sizeDelta = new Vector2(0f, 36f);
            panelTitleRect.anchoredPosition = new Vector2(0f, -10f);
            var panelTitle = panelTitleGo.AddComponent<Text>();
            panelTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panelTitle.text = "现实 · 排版任务（鼠标拖拽 → 提交）";
            panelTitle.alignment = TextAnchor.MiddleCenter;
            panelTitle.color = new Color(0.9f, 0.9f, 1f);
            panelTitle.fontSize = 18;

            // AlignmentTask 现在挂在 RealityPanel 内，居中显示。
            var taskGo = new GameObject("AlignmentTask");
            taskGo.transform.SetParent(panelGo.transform, false);
            var taskRect = taskGo.AddComponent<RectTransform>();
            taskRect.anchorMin = new Vector2(0.5f, 0.5f);
            taskRect.anchorMax = new Vector2(0.5f, 0.5f);
            taskRect.sizeDelta = new Vector2(420f, 320f);
            taskRect.anchoredPosition = Vector2.zero;

            var taskGroup = taskGo.AddComponent<CanvasGroup>();
            alignmentTask = taskGo.AddComponent<RealityAlignmentTask>();

            var blocks = new List<DraggableAlignmentBlock>();
            var targets = new List<RectTransform>();

            for (var i = 0; i < 3; i++)
            {
                var target = CreateUiBlock(taskGo.transform, $"Target_{i}", new Vector2(-120f + i * 120f, 60f), new Color(0.8f, 0.8f, 0.4f, 0.4f));
                targets.Add(target);

                var block = CreateUiBlock(taskGo.transform, $"Block_{i}", new Vector2(-120f + i * 120f, -80f), new Color(0.85f, 0.5f, 0.4f));
                var draggable = block.gameObject.AddComponent<DraggableAlignmentBlock>();
                blocks.Add(draggable);
            }

            // A 方案：所有 block 共享同一组候选 targets。
            foreach (var draggable in blocks)
            {
                ApplyDraggableFields(draggable, targets);
            }

            var submitGo = new GameObject("SubmitButton");
            submitGo.transform.SetParent(taskGo.transform, false);
            var submitRect = submitGo.AddComponent<RectTransform>();
            submitRect.sizeDelta = new Vector2(160f, 50f);
            submitRect.anchoredPosition = new Vector2(0f, -130f);
            var submitImage = submitGo.AddComponent<Image>();
            submitImage.color = new Color(0.3f, 0.4f, 0.6f);
            var submit = submitGo.AddComponent<Button>();
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(submitGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<Text>();
            label.text = "提交";
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            ApplyAlignmentTaskFields(alignmentTask, taskGroup, submit, blocks, targets);
            return canvas;
        }

        private static RectTransform CreateUiBlock(Transform parent, string name, Vector2 anchoredPosition, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80f, 80f);
            rect.anchoredPosition = anchoredPosition;
            var image = go.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private static DreamPushTarget BuildDreamScene(Transform parent, out DreamPathOpener pathOpener, out GameObject exitTriggerGo)
        {
            var ground = CreateWorldQuad(parent, "Ground", new Vector3(0f, -3.4f, 0f), new Vector3(20f, 1f, 1f), new Color(0.25f, 0.3f, 0.35f), addBox: true);
            ground.layer = GetLayerOrDefault("Ground", 0);

            var pushable = CreateWorldQuad(parent, "PushableBlock", new Vector3(-3f, -2f, 0f), new Vector3(1f, 1f, 1f), new Color(0.85f, 0.55f, 0.3f), addBox: true);
            pushable.AddComponent<DreamPushable>();
            var pushBody = pushable.AddComponent<Rigidbody2D>();
            pushBody.gravityScale = 3f;
            pushBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            pushBody.interpolation = RigidbodyInterpolation2D.Interpolate;

            var targetGo = new GameObject("DreamPushTarget");
            targetGo.transform.SetParent(parent, false);
            targetGo.transform.position = new Vector3(3f, -2f, 0f);
            var targetCollider = targetGo.AddComponent<BoxCollider2D>();
            targetCollider.isTrigger = true;
            targetCollider.size = new Vector2(2f, 2f);
            var targetVisual = targetGo.AddComponent<SpriteRenderer>();
            targetVisual.sprite = BuildSquareSprite();
            targetVisual.color = new Color(0.4f, 0.85f, 0.6f, 0.4f);
            var pushTarget = targetGo.AddComponent<DreamPushTarget>();

            var blockedPath = CreateWorldQuad(parent, "BlockedPath", new Vector3(7f, -2f, 0f), new Vector3(1f, 2f, 1f), new Color(0.4f, 0.4f, 0.4f), addBox: true);
            var openPath = CreateWorldQuad(parent, "OpenPath", new Vector3(7f, -2.4f, 0f), new Vector3(2f, 0.2f, 1f), new Color(0.6f, 0.85f, 1f), addBox: false);
            openPath.SetActive(false);

            var pathGo = new GameObject("DreamPathOpener");
            pathGo.transform.SetParent(parent, false);
            pathOpener = pathGo.AddComponent<DreamPathOpener>();
            ApplyPathOpenerFields(pathOpener, blockedPath, openPath);

            exitTriggerGo = new GameObject("ExitTrigger");
            exitTriggerGo.transform.SetParent(parent, false);
            exitTriggerGo.transform.position = new Vector3(9f, -2f, 0f);
            exitTriggerGo.layer = GetLayerOrDefault("Trigger", 0);
            var exitCollider = exitTriggerGo.AddComponent<BoxCollider2D>();
            exitCollider.isTrigger = true;
            exitCollider.size = new Vector2(1.5f, 3f);
            var exitTrigger = exitTriggerGo.AddComponent<WorkspaceEventTriggerZone>();
            ApplyExitTriggerFields(exitTrigger, "alignment_exit");

            return pushTarget;
        }

        private static (ChatTaskController, ChatTaskPanelUI) BuildChatPanel(Transform canvasTransform, DualWorldWorkspace workspace)
        {
            var panelGo = new GameObject("ChatTaskPanel");
            panelGo.transform.SetParent(canvasTransform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            // 顶部正中跨左右两屏 —— 系统级反馈，不属于任何单一世界。
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -20f);
            panelRect.sizeDelta = new Vector2(640f, 110f);

            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.09f, 0.12f, 0.85f);
            var canvasGroup = panelGo.AddComponent<CanvasGroup>();

            var accentGo = new GameObject("Accent");
            accentGo.transform.SetParent(panelGo.transform, false);
            var accentRect = accentGo.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.sizeDelta = new Vector2(6f, 0f);
            accentRect.anchoredPosition = Vector2.zero;
            var accentImage = accentGo.AddComponent<Image>();

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(20f, -10f);
            titleRect.sizeDelta = new Vector2(-30f, 30f);
            var title = titleGo.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.color = Color.white;
            title.fontSize = 18;

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(panelGo.transform, false);
            var bodyRect = bodyGo.AddComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(20f, 10f);
            bodyRect.offsetMax = new Vector2(-10f, -45f);
            var body = bodyGo.AddComponent<Text>();
            body.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            body.color = new Color(0.95f, 0.95f, 0.95f);
            body.fontSize = 14;

            var panelUi = panelGo.AddComponent<ChatTaskPanelUI>();
            ApplyChatPanelFields(panelUi, canvasGroup, title, body, accentImage);

            var controllerGo = new GameObject("ChatTaskController");
            controllerGo.transform.SetParent(canvasTransform, false);
            var controller = controllerGo.AddComponent<ChatTaskController>();
            ApplyChatControllerFields(controller, panelUi, workspace);
            return (controller, panelUi);
        }

        private static ChatTaskDefinition BuildAlignmentTaskDefinition()
        {
            var def = ScriptableObject.CreateInstance<ChatTaskDefinition>();
            def.taskId = "alignment.right";
            def.title = "排版任务";
            def.failureMessages.Add(new ChatTaskDefinition.NpcChatMessage { text = "不对，再调一下。" });
            def.successMessages.Add(new ChatTaskDefinition.NpcChatMessage { text = "这次可以了。" });
            return def;
        }

        private static GameObject CreateWorldQuad(Transform parent, string name, Vector3 position, Vector3 scale, Color color, bool addBox)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = BuildSquareSprite();
            renderer.color = color;
            if (addBox)
            {
                go.AddComponent<BoxCollider2D>();
            }
            return go;
        }

        private static Sprite cachedSquareSprite;

        private static Sprite BuildSquareSprite()
        {
            if (cachedSquareSprite != null)
            {
                return cachedSquareSprite;
            }

            var tex = new Texture2D(2, 2);
            var pixels = new[] { Color.white, Color.white, Color.white, Color.white };
            tex.SetPixels(pixels);
            tex.Apply();
            cachedSquareSprite = Sprite.Create(tex, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f, 0u, SpriteMeshType.FullRect);
            return cachedSquareSprite;
        }

        private static void ApplyDraggableFields(DraggableAlignmentBlock block, IList<RectTransform> targets)
        {
            var type = typeof(DraggableAlignmentBlock);
            var field = type.GetField("targets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null) return;
            var list = new List<RectTransform>(targets != null ? targets.Count : 0);
            if (targets != null)
            {
                for (var i = 0; i < targets.Count; i++)
                {
                    if (targets[i] != null) list.Add(targets[i]);
                }
            }
            field.SetValue(block, list);
        }

        private static void ApplyAlignmentTaskFields(RealityAlignmentTask task, CanvasGroup group, Button submit, List<DraggableAlignmentBlock> blocks, List<RectTransform> targets)
        {
            // submit 按钮已从 RealityAlignmentTask 移除，改由 ChatBoxUI 持有并通过 SubmitRequested 事件外抛。
            // builder 生成的 submit 按钮目前是孤儿 —— 测试场景需手动把它移到 ChatBox 下，或用 prefab 流程。
            _ = submit;
            var t = typeof(RealityAlignmentTask);
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            t.GetField("interactionGroup", F)?.SetValue(task, group);
            t.GetField("blocks", F)?.SetValue(task, blocks);
            t.GetField("targetRects", F)?.SetValue(task, targets);
        }

        private static void ApplyAlignmentFlowFields(AlignmentSubLevelFlow flow, RealityAlignmentTask task, GameObject realityRoot, DreamPushTarget push, GameObject dreamRoot, ChatTaskDefinition def)
        {
            var t = typeof(AlignmentSubLevelFlow);
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            t.GetField("realityTask", F)?.SetValue(flow, task);
            t.GetField("realityRoot", F)?.SetValue(flow, realityRoot);
            t.GetField("pushTarget", F)?.SetValue(flow, push);
            t.GetField("dreamRoot", F)?.SetValue(flow, dreamRoot);
            t.GetField("taskDefinition", F)?.SetValue(flow, def);
            typeof(BaseSubLevelFlow).GetField("subLevelId", F)?.SetValue(flow, "alignment");
        }

        private static void ApplyBridgeFields(DreamToRealityEnhancer bridge, DualWorldWorkspace ws, RealityAlignmentTask task)
        {
            var t = typeof(DreamToRealityEnhancer);
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            t.GetField("workspace", F)?.SetValue(bridge, ws);
            t.GetField("alignmentTask", F)?.SetValue(bridge, task);
        }

        private static void ApplyBridgeFields(RealityToDreamRepair bridge, DualWorldWorkspace ws, DreamPathOpener opener)
        {
            var t = typeof(RealityToDreamRepair);
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            t.GetField("workspace", F)?.SetValue(bridge, ws);
            t.GetField("pathOpener", F)?.SetValue(bridge, opener);
        }

        private static void ApplyPathOpenerFields(DreamPathOpener opener, GameObject blocked, GameObject open)
        {
            var t = typeof(DreamPathOpener);
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            t.GetField("blockedPath", F)?.SetValue(opener, blocked);
            t.GetField("openPath", F)?.SetValue(opener, open);
        }

        private static void ApplyExitTriggerFields(WorkspaceEventTriggerZone trigger, string id)
        {
            var t = typeof(WorkspaceEventTriggerZone);
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            t.GetField("eventId", F)?.SetValue(trigger, id);
        }

        private static void ApplyChatPanelFields(ChatTaskPanelUI panel, CanvasGroup group, Text title, Text body, Image accent)
        {
            // ChatTaskPanelUI 已重构为 log 模式（ScrollRect + entryPrefab），builder 无法在运行时合成 ScrollRect/EntryPrefab。
            // 老字段（titleLabel/bodyLabel/accentBar）不再存在；这里只兜底挂 canvasGroup + titleHeader 让面板不至于完全裸。
            // 完整 log 体验请走 scene-authored ChatBox prefab，不要依赖 test builder。
            _ = body; _ = accent;
            var t = typeof(ChatTaskPanelUI);
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            t.GetField("canvasGroup", F)?.SetValue(panel, group);
            t.GetField("titleHeader", F)?.SetValue(panel, title);
            Debug.LogWarning("[DualWorldTestSceneAutoBuilder] ChatTaskPanelUI 已升级为 log 模式；builder 生成的面板仅占位，请改用 ChatBox prefab。");
        }

        private static void ApplyChatControllerFields(ChatTaskController controller, ChatTaskPanelUI panel, DualWorldWorkspace ws)
        {
            _ = panel;  // 不再持 panel 引用，运行时通过 FindObjectOfType<ChatBoxUI> 查
            var t = typeof(ChatTaskController);
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            t.GetField("workspace", F)?.SetValue(controller, ws);
        }

        private static void ApplyFlowControllerSubLevels(LevelInGameFlowController controller, List<BaseSubLevelFlow> subs)
        {
            var t = typeof(LevelInGameFlowController);
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            t.GetField("subLevels", F)?.SetValue(controller, subs);
        }

        private static void ApplyWorkspaceFields(DualWorldWorkspace ws, LevelInGameFlowController flow, ChatTaskController chat)
        {
            var t = typeof(DualWorldWorkspace);
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            t.GetField("flowController", F)?.SetValue(ws, flow);
            t.GetField("chatTaskController", F)?.SetValue(ws, chat);
        }

        private static void ApplyWorkspaceCharacterFields(DualWorldWorkspace ws, SideScrollCharacterControllerBase player, SideScrollCameraController camera)
        {
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            // playerController / cameraController live on the SideScrollWorkspaceBase ancestor.
            var baseType = typeof(SideScrollWorkspaceBase);
            baseType.GetField("playerController", F)?.SetValue(ws, player);
            baseType.GetField("cameraController", F)?.SetValue(ws, camera);
            baseType.GetField("groundMask", F)?.SetValue(ws, (LayerMask)LayerMask.GetMask("Ground"));
        }

        private static GameObject BuildSideScrollPlayer(Transform parent, Vector3 position)
        {
            var player = new GameObject("SideScrollPlayer");
            player.transform.SetParent(parent, false);
            player.transform.position = position;
            player.layer = GetLayerOrDefault("Player", 0);

            var renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = BuildSquareSprite();
            renderer.color = new Color(0.85f, 0.55f, 0.3f);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(0.8f, 1.4f);

            var capsule = player.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.8f, 1.4f);
            capsule.direction = CapsuleDirection2D.Vertical;

            var interactBox = player.AddComponent<BoxCollider2D>();
            interactBox.size = new Vector2(1.0f, 1.6f);
            interactBox.isTrigger = true;

            var body = player.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 3f;
            // Interpolate：让 Rigidbody2D 在两次 FixedUpdate 之间做位置插值，
            // 否则高 FPS 下渲染只看到 50Hz 物理位置，会出现"走两步顿一下"的视觉断帧。
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            player.AddComponent<SideScrollCharacterControllerBase>();
            player.AddComponent<CharacterInputProxy>();
            player.AddComponent<CharacterGroundDetector>();
            player.AddComponent<CharacterMovementMotor>();
            player.AddComponent<CharacterJumpMotor>();
            player.AddComponent<SideScrollInteractionDetector>();

            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(player.transform, false);
            groundCheck.transform.localPosition = new Vector3(0f, -0.75f, 0f);

            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(CharacterGroundDetector).GetField("groundCheckPoint", F)?.SetValue(player.GetComponent<CharacterGroundDetector>(), groundCheck.transform);
            typeof(CharacterGroundDetector).GetField("groundMask", F)?.SetValue(player.GetComponent<CharacterGroundDetector>(), (LayerMask)LayerMask.GetMask("Ground"));
            typeof(SideScrollInteractionDetector).GetField("interactableMask", F)?.SetValue(player.GetComponent<SideScrollInteractionDetector>(), (LayerMask)LayerMask.GetMask("Interactable"));
            typeof(SideScrollInteractionDetector).GetField("detectCollider", F)?.SetValue(player.GetComponent<SideScrollInteractionDetector>(), interactBox);

            // controller.ApplyConfigs would NRE here: root is inactive, so the controller's Awake hasn't run
            // and its serialized motor refs are still null. Apply configs to the motors directly — they don't need Awake.
            player.GetComponent<CharacterMovementMotor>().SetConfig(ScriptableObject.CreateInstance<CharacterMoveConfig>());
            player.GetComponent<CharacterJumpMotor>().SetConfig(ScriptableObject.CreateInstance<CharacterJumpConfig>());
            player.GetComponent<CharacterGroundDetector>().SetGroundMask((LayerMask)LayerMask.GetMask("Ground"));
            return player;
        }

        private static Collider2D BuildCameraBounds(Transform parent)
        {
            var bounds = new GameObject("CameraBounds");
            bounds.transform.SetParent(parent, false);
            bounds.transform.position = new Vector3(2f, 0f, 0f);
            var collider2D = bounds.AddComponent<PolygonCollider2D>();
            collider2D.SetPath(0, CreateRectPath(new Vector2(22f, 12f)));
            collider2D.isTrigger = true;
            return collider2D;
        }

        private static Vector2[] CreateRectPath(Vector2 size)
        {
            var half = size * 0.5f;
            return new[]
            {
                new Vector2(-half.x, -half.y),
                new Vector2(-half.x, half.y),
                new Vector2(half.x, half.y),
                new Vector2(half.x, -half.y)
            };
        }

        private static SideScrollCameraController BuildCameraRig(Transform parent, Transform followTarget, Collider2D confinerShape)
        {
            var rig = new GameObject("CameraRig");
            rig.transform.SetParent(parent, false);

            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                cameraObject.AddComponent<AudioListener>();
                mainCamera = cameraObject.AddComponent<Camera>();
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 5f;
            }

            // 右半屏天空底色（无 skybox 时避免黑屏 / 残影），同时和左侧深色面板形成对比。
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.65f, 0.78f, 0.88f);

            var brain = mainCamera.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = mainCamera.gameObject.AddComponent<CinemachineBrain>();
            }

            var vcamGo = new GameObject("CM_VCam");
            vcamGo.transform.SetParent(rig.transform, false);
            var vcam = vcamGo.AddComponent<CinemachineVirtualCamera>();
            vcam.m_Lens.Orthographic = true;
            vcam.Follow = followTarget;
            vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
            var confiner = vcamGo.AddComponent<CinemachineConfiner2D>();
            confiner.m_BoundingShape2D = confinerShape;
            vcamGo.AddComponent<CinemachineAxisLock2D>();

            var controller = rig.AddComponent<SideScrollCameraController>();
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(SideScrollCameraController).GetField("virtualCamera", F)?.SetValue(controller, vcam);
            typeof(SideScrollCameraController).GetField("confiner2D", F)?.SetValue(controller, confiner);

            var camConfig = ScriptableObject.CreateInstance<CameraConfig>();
            camConfig.followOffset = new Vector3(0f, 1.2f, -10f);
            camConfig.damping = new Vector2(0.2f, 0.2f);
            camConfig.orthographicSize = 5f;
            controller.ApplyCameraConfig(camConfig);
            return controller;
        }

        private static int GetLayerOrDefault(string layerName, int fallback)
        {
            var layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? layer : fallback;
        }

        private static void ConfigureCanvasScaler(CanvasScaler scaler)
        {
            if (scaler == null)
            {
                return;
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
        }
    }
}
