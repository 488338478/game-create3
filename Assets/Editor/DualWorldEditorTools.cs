using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameCreate3;
using GameCreate3.DualWorld;

namespace GameCreate3.EditorTools
{
    /// <summary>
    /// 一键把双世界工作区从"运行时反射搭"转成"持久化 prefab"。
    /// 菜单项：
    ///   GameCreate3 → Generate Default Assets               生成 4 个默认 SO 资产
    ///   GameCreate3 → DualWorld → Generate Workspace Prefab  生成 DualWorldWorkspace.prefab
    /// </summary>
    public static class DualWorldEditorTools
    {
        private const string DefaultsRoot = "Assets/Settings/Defaults";
        private const string DualWorldSettingsRoot = "Assets/Settings/DualWorld";
        private const string PrefabRoot = "Assets/Prefabs/DualWorld";
        private const string SideScrollPrefabRoot = "Assets/Prefabs/SideScroll";
        private const string ResourcesPrefabRoot = "Assets/Resources/Prefabs";

        private const string MoveCfgPath = DefaultsRoot + "/DefaultCharacterMoveConfig.asset";
        private const string JumpCfgPath = DefaultsRoot + "/DefaultCharacterJumpConfig.asset";
        private const string CamCfgPath = DefaultsRoot + "/DefaultCameraConfig.asset";
        private const string AlignmentChatPath = DualWorldSettingsRoot + "/AlignmentChatTask.asset";
        private const string WorkspacePrefabPath = PrefabRoot + "/DualWorldWorkspace.prefab";
        private const string WorkspacePrefabResourcesPath = ResourcesPrefabRoot + "/DualWorldWorkspace.prefab";
        private const string PlayerPrefabPath = SideScrollPrefabRoot + "/SideScrollPlayer.prefab";
        private const string CameraRigPrefabPath = SideScrollPrefabRoot + "/CameraRig.prefab";
        private const string RealityCanvasPrefabPath = PrefabRoot + "/RealityCanvas.prefab";
        private const string DreamWorldPrefabPath = PrefabRoot + "/DreamWorld.prefab";
        private const string ChatTaskPanelPrefabPath = PrefabRoot + "/ChatTaskPanel.prefab";
        private const string ChatBoxPrefabPath = PrefabRoot + "/ChatBox.prefab";
        private const string ChatLogEntryPrefabPath = PrefabRoot + "/Atoms/ChatLogEntry.prefab";

        // Tier 1 atoms（双世界）
        private const string AtomsRoot = PrefabRoot + "/Atoms";
        // Tier 3 logic singletons（双世界）
        private const string LogicRoot = PrefabRoot + "/Logic";
        // Tier 4 assemblies（双世界）
        private const string AssemblyRoot = PrefabRoot + "/Assemblies";

        // ------------------------------------------------------------
        // Default ScriptableObject assets
        // ------------------------------------------------------------

        [MenuItem("GameCreate3/Generate Default Assets")]
        public static void GenerateDefaultAssets()
        {
            EnsureFolder(DefaultsRoot);
            EnsureFolder(DualWorldSettingsRoot);

            CreateOrGet<CharacterMoveConfig>(MoveCfgPath);
            CreateOrGet<CharacterJumpConfig>(JumpCfgPath);
            CreateOrGet<CameraConfig>(CamCfgPath, c =>
            {
                c.followOffset = new Vector3(0f, 1.2f, -10f);
                c.damping = new Vector2(0.2f, 0.2f);
                c.useConfiner = true;
                c.orthographicSize = 5f;
            });
            CreateOrGet<ChatTaskDefinition>(AlignmentChatPath, d =>
            {
                d.taskId = "alignment.right";
                d.title = "排版任务";
                d.description = "把右侧三个模块对齐到目标位。";
                d.initialMessage = "你来看看这版排得行不行？";
                d.failureMessage = "不对，再调一下。";
                d.blockedMessage = "你是不是哪里没看清？要不去走两步，换个角度。";
                d.enhancedMessage = "梦里好像帮你顺过了，再试一次。";
                d.successMessage = "这次可以了。";
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DualWorldEditorTools] Default assets generated under " + DefaultsRoot + " / " + DualWorldSettingsRoot);
        }

        // ------------------------------------------------------------
        // Workspace prefab
        // ------------------------------------------------------------

        [MenuItem("GameCreate3/DualWorld/Generate Workspace Prefab")]
        public static void GenerateWorkspacePrefab()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[DualWorldEditorTools] Cannot generate prefab while in Play mode.");
                return;
            }

            // 确保依赖资产已存在
            GenerateDefaultAssets();
            EnsureFolder(PrefabRoot);
            EnsureFolder(SideScrollPrefabRoot);
            EnsureFolder(ResourcesPrefabRoot);

            // 清掉当前场景里的旧产物（避免重复 build）
            CleanupExistingArtifacts();

            // 让 AutoBuilder 在编辑器场景里跑一次
            DualWorldTestSceneAutoBuilder.ResetForEditor();
            DualWorldTestSceneAutoBuilder.BuildIfNeeded();

            var root = GameObject.Find("DualWorldRoot");
            if (root == null)
            {
                Debug.LogError("[DualWorldEditorTools] AutoBuilder did not produce DualWorldRoot.");
                return;
            }

            // 把运行时 ScriptableObject.CreateInstance 出来的临时 SO 替换成项目里的持久 .asset
            var moveCfg = AssetDatabase.LoadAssetAtPath<CharacterMoveConfig>(MoveCfgPath);
            var jumpCfg = AssetDatabase.LoadAssetAtPath<CharacterJumpConfig>(JumpCfgPath);
            var camCfg = AssetDatabase.LoadAssetAtPath<CameraConfig>(CamCfgPath);
            var chatTask = AssetDatabase.LoadAssetAtPath<ChatTaskDefinition>(AlignmentChatPath);

            foreach (var m in root.GetComponentsInChildren<CharacterMovementMotor>(true))
                SetSerializedField(m, "config", moveCfg);
            foreach (var j in root.GetComponentsInChildren<CharacterJumpMotor>(true))
                SetSerializedField(j, "config", jumpCfg);
            foreach (var c in root.GetComponentsInChildren<SideScrollCameraController>(true))
                SetSerializedField(c, "defaultConfig", camCfg);
            foreach (var f in root.GetComponentsInChildren<AlignmentSubLevelFlow>(true))
                SetSerializedField(f, "taskDefinition", chatTask);

            // ===== 拆 nested prefab，让顶层 prefab 体积小、内层可复用 =====
            // 用 SaveAsPrefabAssetAndConnect：保存为 prefab 资产 + 把场景实例转成对该 prefab 的引用，
            // 这样后面保存外层 root 时，玩家/相机以"嵌套 prefab 引用"形式存进去而非 baked 副本。

            // 1. SideScrollPlayer —— 玩家自包含子树
            var playerGo = root.transform.Find("SideScrollPlayer")?.gameObject;
            if (playerGo != null)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(playerGo, PlayerPrefabPath, InteractionMode.AutomatedAction);
            }

            // 2. CameraRig + CameraBounds —— 把 Bounds 重 parent 到 Rig 下，再整棵抽出去。
            //    这样 vcam 的 Confiner 引用 Bounds 走 prefab 内部 fileID，跨 prefab 不丢。
            var boundsTransform = root.transform.Find("CameraBounds");
            var cameraRigGo = root.transform.Find("CameraRig")?.gameObject;
            if (cameraRigGo != null && boundsTransform != null)
            {
                boundsTransform.SetParent(cameraRigGo.transform, true); // worldPositionStays
                PrefabUtility.SaveAsPrefabAssetAndConnect(cameraRigGo, CameraRigPrefabPath, InteractionMode.AutomatedAction);
            }

            // 3. RealityCanvas —— 左屏 UI 子树（拖块 + 提交按钮 + 目标位）
            //    RealityAlignmentTask 内部所有引用（blocks/targets/submitButton/CanvasGroup）都在子树内 ✓
            //    Flow 和 Bridge 对它的引用变成跨 prefab 引用，Unity 能保留。
            var realityCanvasGo = root.transform.Find("RealityRoot/RealityCanvas")?.gameObject;
            if (realityCanvasGo != null)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(realityCanvasGo, RealityCanvasPrefabPath, InteractionMode.AutomatedAction);
            }

            // 4. DreamWorld —— 右屏梦境地形（地面 + 推方块 + 舒适区 + 路径 + 出口）
            //    DreamPathOpener 引用 BlockedPath/OpenPath 都在子树内 ✓
            var dreamWorldGo = root.transform.Find("DreamRoot")?.gameObject;
            if (dreamWorldGo != null)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(dreamWorldGo, DreamWorldPrefabPath, InteractionMode.AutomatedAction);
            }

            // 5. ChatTaskPanel —— 持久 UI 上的聊天面板子树（不含 Controller）
            //    ChatTaskPanelUI 内部引用 Title/Body/Accent/CanvasGroup 都在子树内 ✓
            //    Controller 留在外层 prefab，它对 panel 的引用变跨 prefab 引用。
            var chatPanelGo = root.transform.Find("PersistentUI/ChatTaskPanel")?.gameObject;
            if (chatPanelGo != null)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(chatPanelGo, ChatTaskPanelPrefabPath, InteractionMode.AutomatedAction);
            }

            // 6. Tier 3 逻辑单例 —— 抽出 Flow / 两个 Bridge / ChatController 为独立 prefab。
            //    注意：这些组件的 SerializeField 引用（如 Flow.realityTask）当前指向工作区里嵌套的
            //    RealityCanvas / DreamWorld 实例，存为 prefab 时变成"跨 prefab 引用"。Unity 可保留，
            //    但只在嵌套于本 DualWorldWorkspace.prefab 上下文中有效。要做真正"独立可拖"的逻辑 prefab，
            //    需要进一步把这些引用改成运行时 Initialize 注入。
            EnsureFolder(LogicRoot);
            ExtractToNested(root.transform.Find("LevelInGameFlow/AlignmentSubLevel")?.gameObject,
                LogicRoot + "/AlignmentSubLevelFlow.prefab");
            ExtractToNested(root.transform.Find("LevelInGameFlow")?.gameObject,
                LogicRoot + "/LevelInGameFlowController.prefab");
            ExtractToNested(root.transform.Find("CrossWorldBridges/DreamToRealityEnhancer")?.gameObject,
                LogicRoot + "/DreamToRealityEnhancer.prefab");
            ExtractToNested(root.transform.Find("CrossWorldBridges/RealityToDreamRepair")?.gameObject,
                LogicRoot + "/RealityToDreamRepair.prefab");
            ExtractToNested(root.transform.Find("PersistentUI/ChatTaskController")?.gameObject,
                LogicRoot + "/ChatTaskController.prefab");

            // 7. 顶层工作区 prefab —— 嵌套引用上面所有
            PrefabUtility.SaveAsPrefabAsset(root, WorkspacePrefabPath);
            CopyAsset(WorkspacePrefabPath, WorkspacePrefabResourcesPath);

            // 清理本次 build 残留的场景物体（Main Camera / EventSystem 是 AutoBuilder 顺带创的，
            // 它们不属于工作区，存进当前场景反而污染编辑场景）
            Object.DestroyImmediate(root);
            CleanupExistingArtifacts();
            EditorSceneManagerSafeMarkDirty();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DualWorldEditorTools] Saved " + WorkspacePrefabPath + " (mirrored to Resources/Prefabs/).");
        }

        // ------------------------------------------------------------
        // Atoms（Tier 1 单组件可复用件）—— 独立菜单一键生成
        // ------------------------------------------------------------

        // ------------------------------------------------------------
        // ChatBox（scene 根驻留的右屏聊天框 prefab + 日志条目 atom）
        // ------------------------------------------------------------

        [MenuItem("GameCreate3/DualWorld/Generate ChatBox Prefabs")]
        public static void GenerateChatBoxPrefabs()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[DualWorldEditorTools] Cannot generate prefab while in Play mode.");
                return;
            }

            EnsureFolder(PrefabRoot);
            EnsureFolder(AtomsRoot);

            // 1) ChatLogEntry atom（先建，ChatBox 要把它指给 ChatTaskPanelUI.entryPrefab）
            var entryAsset = BuildChatLogEntryPrefab(ChatLogEntryPrefabPath);

            // 2) ChatBox 主 prefab
            BuildChatBoxPrefab(ChatBoxPrefabPath, entryAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DualWorldEditorTools] Generated ChatBox.prefab + ChatLogEntry.prefab under " + PrefabRoot);
        }

        // ChatLogEntry：水平 layout，NpcRoot 在左、PlayerRoot 在右，二选一激活。
        // 每个 root 内：Avatar + Bubble(MoodAccent + Body) + HighlightGlow。
        private static GameObject BuildChatLogEntryPrefab(string path)
        {
            var root = new GameObject("ChatLogEntry");
            root.SetActive(false);
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(0f, 80f);

            var rootLayout = root.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.spacing = 4f;
            var rootFitter = root.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            rootFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            var npcRoot = BuildEntrySide(root.transform, "NpcRoot", isPlayer: false,
                bubbleColor: new Color(0.18f, 0.20f, 0.28f, 0.9f),
                bodyColor: new Color(0.95f, 0.95f, 0.95f),
                out var npcBody, out var npcAvatar, out var npcAccent, out var npcGlow);

            var playerRoot = BuildEntrySide(root.transform, "PlayerRoot", isPlayer: true,
                bubbleColor: new Color(0.42f, 0.55f, 0.85f, 0.9f),
                bodyColor: Color.white,
                out var playerBody, out var playerAvatar, out var playerAccent, out var playerGlow);

            // View 组件 —— 默认绑 NPC 侧字段（最常见），渲染时按 speaker 在 NpcRoot/PlayerRoot 二选一激活。
            var view = root.AddComponent<ChatLogEntryView>();
            SetPrivateField(view, "bodyLabel", npcBody);
            SetPrivateField(view, "avatarImage", npcAvatar);
            SetPrivateField(view, "moodAccent", npcAccent);
            SetPrivateField(view, "highlightGlow", npcGlow);
            SetPrivateField(view, "npcRoot", npcRoot);
            SetPrivateField(view, "playerRoot", playerRoot);

            // 默认两侧都 active；运行时 Bind 会按 speaker 二选一
            npcRoot.SetActive(true);
            playerRoot.SetActive(false);

            SaveActivePrefab(root, path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static GameObject BuildEntrySide(Transform parent, string name, bool isPlayer,
            Color bubbleColor, Color bodyColor,
            out UnityEngine.UI.Text bodyOut, out UnityEngine.UI.Image avatarOut,
            out UnityEngine.UI.Image accentOut, out GameObject glowOut)
        {
            var side = new GameObject(name);
            side.transform.SetParent(parent, false);
            var sideRect = side.AddComponent<RectTransform>();
            var layout = side.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.childAlignment = isPlayer ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.spacing = 6f;
            var sideFitter = side.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            sideFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            sideFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            _ = sideRect;

            // Avatar（占位 Image，运行时 Bind 时填 sprite；无 sprite 时由 Bind 自动 disable）
            var avatarGo = new GameObject("Avatar");
            avatarGo.transform.SetParent(side.transform, false);
            var avatarLE = avatarGo.AddComponent<UnityEngine.UI.LayoutElement>();
            avatarLE.preferredWidth = 48f;
            avatarLE.preferredHeight = 48f;
            avatarOut = avatarGo.AddComponent<UnityEngine.UI.Image>();
            avatarOut.color = Color.white;

            // Bubble（VerticalLayout：Accent 条 + Body 文本）
            var bubble = new GameObject("Bubble");
            bubble.transform.SetParent(side.transform, false);
            var bubbleImg = bubble.AddComponent<UnityEngine.UI.Image>();
            bubbleImg.color = bubbleColor;
            var bubbleLayout = bubble.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            bubbleLayout.padding = new RectOffset(8, 8, 6, 6);
            bubbleLayout.spacing = 4f;
            bubbleLayout.childForceExpandWidth = true;
            bubbleLayout.childForceExpandHeight = false;
            bubbleLayout.childControlWidth = true;
            bubbleLayout.childControlHeight = true;
            var bubbleFitter = bubble.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            bubbleFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            bubbleFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            var bubbleLE = bubble.AddComponent<UnityEngine.UI.LayoutElement>();
            bubbleLE.preferredWidth = 280f;
            bubbleLE.flexibleWidth = 0f;

            var accentGo = new GameObject("MoodAccent");
            accentGo.transform.SetParent(bubble.transform, false);
            var accentLE = accentGo.AddComponent<UnityEngine.UI.LayoutElement>();
            accentLE.preferredHeight = 3f;
            accentOut = accentGo.AddComponent<UnityEngine.UI.Image>();
            accentOut.color = Color.gray;

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(bubble.transform, false);
            bodyOut = bodyGo.AddComponent<UnityEngine.UI.Text>();
            bodyOut.color = bodyColor;
            bodyOut.fontSize = 14;
            bodyOut.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bodyOut.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyOut.verticalOverflow = VerticalWrapMode.Overflow;

            // HighlightGlow（默认 inactive；Bind 时 SetActive(entry.highlight)）
            glowOut = new GameObject("HighlightGlow");
            glowOut.transform.SetParent(bubble.transform, false);
            var glowImg = glowOut.AddComponent<UnityEngine.UI.Image>();
            glowImg.color = new Color(1f, 0.95f, 0.5f, 0.5f);
            var glowLE = glowOut.AddComponent<UnityEngine.UI.LayoutElement>();
            glowLE.preferredHeight = 2f;
            glowOut.SetActive(false);

            // Player 侧把 Avatar 拍到 Bubble 后面 —— 自然成"气泡在左，头像在右"。
            if (isPlayer)
            {
                avatarGo.transform.SetSiblingIndex(1);
            }

            return side;
        }

        // ChatBox：右屏整个聊天框 —— Panel（背景 Image）+ TitleHeader + ScrollRect + Submit。
        private static void BuildChatBoxPrefab(string path, GameObject entryPrefabAsset)
        {
            var root = new GameObject("ChatBox");
            root.SetActive(false);
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(447f, 847f);    // 对齐 Level2.unity 现有 ChatBox 尺寸
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            var rootImg = root.AddComponent<UnityEngine.UI.Image>();
            rootImg.color = new Color(0.78f, 0.88f, 0.95f, 1f);  // 蓝色底，配合截图风格

            // —— LogPanel（持 ChatTaskPanelUI）
            var logPanelGo = new GameObject("LogPanel");
            logPanelGo.transform.SetParent(root.transform, false);
            var logPanelRect = logPanelGo.AddComponent<RectTransform>();
            logPanelRect.anchorMin = new Vector2(0f, 0f);
            logPanelRect.anchorMax = new Vector2(1f, 1f);
            logPanelRect.offsetMin = new Vector2(12f, 80f);     // 留出底部给 SubmitButton
            logPanelRect.offsetMax = new Vector2(-12f, -12f);
            var logCanvasGroup = logPanelGo.AddComponent<CanvasGroup>();

            // TitleHeader
            var titleGo = new GameObject("TitleHeader");
            titleGo.transform.SetParent(logPanelGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(0f, 28f);
            titleRect.anchoredPosition = new Vector2(0f, 0f);
            var titleText = titleGo.AddComponent<UnityEngine.UI.Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.color = new Color(0.15f, 0.2f, 0.3f);
            titleText.fontSize = 18;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = "";

            // ScrollRect（占 Title 以下整块）
            var scrollGo = new GameObject("ScrollRect");
            scrollGo.transform.SetParent(logPanelGo.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(0f, 0f);
            scrollRect.offsetMax = new Vector2(0f, -32f);       // 给 TitleHeader 让位
            scrollGo.AddComponent<UnityEngine.UI.Image>().color = new Color(1f, 1f, 1f, 0.2f);
            var scroll = scrollGo.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;

            // Viewport
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGo.AddComponent<UnityEngine.UI.Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewportGo.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;

            // Content
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);
            var contentLayout = contentGo.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(6, 6, 6, 6);
            contentLayout.spacing = 6f;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            var contentFitter = contentGo.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            contentFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            // ChatTaskPanelUI 组件 + 引用拼线
            var panelUi = logPanelGo.AddComponent<ChatTaskPanelUI>();
            SetPrivateField(panelUi, "canvasGroup", logCanvasGroup);
            SetPrivateField(panelUi, "titleHeader", titleText);
            SetPrivateField(panelUi, "scrollRect", scroll);
            SetPrivateField(panelUi, "contentRoot", contentRect);
            // entryPrefab：把 atom prefab 资产指过来（运行时 Instantiate 它）
            if (entryPrefabAsset != null)
            {
                var entryViewOnAsset = entryPrefabAsset.GetComponent<ChatLogEntryView>();
                SetPrivateField(panelUi, "entryPrefab", entryViewOnAsset);
            }

            // —— SubmitButton（贴底）
            var btnGo = new GameObject("SubmitButton");
            btnGo.transform.SetParent(root.transform, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0f);
            btnRect.anchorMax = new Vector2(0.5f, 0f);
            btnRect.pivot = new Vector2(0.5f, 0f);
            btnRect.sizeDelta = new Vector2(200f, 56f);
            btnRect.anchoredPosition = new Vector2(0f, 12f);
            var btnImg = btnGo.AddComponent<UnityEngine.UI.Image>();
            btnImg.color = new Color(0.3f, 0.45f, 0.7f);
            var btn = btnGo.AddComponent<UnityEngine.UI.Button>();

            var btnLabelGo = new GameObject("Label");
            btnLabelGo.transform.SetParent(btnGo.transform, false);
            var btnLabelRect = btnLabelGo.AddComponent<RectTransform>();
            btnLabelRect.anchorMin = Vector2.zero;
            btnLabelRect.anchorMax = Vector2.one;
            btnLabelRect.offsetMin = Vector2.zero;
            btnLabelRect.offsetMax = Vector2.zero;
            var btnLabel = btnLabelGo.AddComponent<UnityEngine.UI.Text>();
            btnLabel.text = "提交";
            btnLabel.alignment = TextAnchor.MiddleCenter;
            btnLabel.color = Color.white;
            btnLabel.fontSize = 18;
            btnLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // ChatBoxUI 组件 + 引用拼线
            var box = root.AddComponent<ChatBoxUI>();
            SetPrivateField(box, "logPanel", panelUi);
            SetPrivateField(box, "submitButton", btn);

            SaveActivePrefab(root, path);
        }

        // 反射设私有 SerializeField（避免每次写 new SerializedObject 长链）
        private static void SetPrivateField(Component target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning($"[DualWorldEditorTools] Field '{fieldName}' not found on {target.GetType().Name}");
            }
        }

        [MenuItem("GameCreate3/DualWorld/Generate Atom Prefabs")]
        public static void GenerateAtomPrefabs()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[DualWorldEditorTools] Cannot generate prefab while in Play mode.");
                return;
            }

            EnsureFolder(AtomsRoot);

            BuildUiAtomBlock(AtomsRoot + "/DraggableBlock.prefab", "DraggableBlock", new Color(0.85f, 0.5f, 0.4f),
                go => go.AddComponent<DraggableAlignmentBlock>());
            BuildUiAtomBlock(AtomsRoot + "/AlignmentTarget.prefab", "AlignmentTarget", new Color(0.8f, 0.8f, 0.4f, 0.4f), null);
            BuildSubmitButtonAtom(AtomsRoot + "/SubmitButton.prefab");
            BuildPushableBlockAtom(AtomsRoot + "/PushableBlock.prefab");
            BuildDreamPushTargetAtom(AtomsRoot + "/DreamPushTarget.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DualWorldEditorTools] Generated 5 atom prefabs under " + AtomsRoot);
        }

        private static void BuildUiAtomBlock(string path, string name, Color color, System.Action<GameObject> extra)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80f, 80f);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = color;
            img.raycastTarget = true;
            extra?.Invoke(go);
            SaveActivePrefab(go, path);
        }

        private static void BuildSubmitButtonAtom(string path)
        {
            var go = new GameObject("SubmitButton");
            go.SetActive(false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160f, 50f);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.3f, 0.4f, 0.6f);
            go.AddComponent<UnityEngine.UI.Button>();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<UnityEngine.UI.Text>();
            label.text = "提交";
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            SaveActivePrefab(go, path);
        }

        private static void BuildPushableBlockAtom(string path)
        {
            var go = new GameObject("PushableBlock");
            go.SetActive(false);
            go.transform.localScale = Vector3.one;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.85f, 0.55f, 0.3f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            go.AddComponent<DreamPushable>();
            SaveActivePrefab(go, path);
        }

        private static void BuildDreamPushTargetAtom(string path)
        {
            var go = new GameObject("DreamPushTarget");
            go.SetActive(false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.4f, 0.85f, 0.6f, 0.4f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(2f, 2f);
            box.isTrigger = true;
            go.AddComponent<DreamPushTarget>();
            SaveActivePrefab(go, path);
        }

        // 把 inactive 的临时 GameObject 保存为 active=true 的 prefab
        private static void SaveActivePrefab(GameObject temp, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset != null)
            {
                var so = new SerializedObject(asset);
                var prop = so.FindProperty("m_IsActive");
                if (prop != null) { prop.boolValue = true; so.ApplyModifiedPropertiesWithoutUndo(); }
                EditorUtility.SetDirty(asset);
            }
        }

        // 把场景里的 GameObject 抽成 prefab + 把场景实例转成对该 prefab 的引用
        private static void ExtractToNested(GameObject sceneObj, string prefabPath)
        {
            if (sceneObj == null) return;
            PrefabUtility.SaveAsPrefabAssetAndConnect(sceneObj, prefabPath, InteractionMode.AutomatedAction);
        }

        // ------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------

        private static void CleanupExistingArtifacts()
        {
            DestroyByName("DualWorldRoot");
            DestroyByName("EventSystem");
            DestroyByName("Main Camera"); // AutoBuilder 在没有主相机时会创一个；保留它会让编辑场景多出无主相机
        }

        private static void DestroyByName(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) Object.DestroyImmediate(existing);
        }

        private static T CreateOrGet<T>(string path, System.Action<T> init = null) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                if (init != null) { init(existing); EditorUtility.SetDirty(existing); }
                return existing;
            }
            var so = ScriptableObject.CreateInstance<T>();
            init?.Invoke(so);
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void CopyAsset(string from, string to)
        {
            AssetDatabase.DeleteAsset(to);
            AssetDatabase.CopyAsset(from, to);
        }

        private static void SetSerializedField(Component target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedProperties();
            }
        }

        private static void EditorSceneManagerSafeMarkDirty()
        {
            // 编辑器场景被 cleanup 改过；标记 dirty 让 Unity 提示用户保存。
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
