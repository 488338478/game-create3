using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GameCreate3.StoryPlayer;
using StoryPlayerComp = GameCreate3.StoryPlayer.StoryPlayer;

namespace GameCreate3.EditorTools
{
    /// <summary>
    /// StoryPlayer 模块 prefab 一键生成。
    ///
    /// 菜单：
    ///   GameCreate3 → StoryPlayer → Generate Rig Prefab                  兼容旧测试 Bootstrap 模式（保留）
    ///   GameCreate3 → StoryPlayer → Generate Production Rig Prefab       生产用 Rig（无 Bootstrap，由 StoryRigBootstrap 引导）
    ///   GameCreate3 → StoryPlayer → Generate Trigger Prefabs             生产用 4 个触发器 prefab
    ///   GameCreate3 → StoryPlayer → Generate Atom Prefabs                Tier 1 UI 原子件
    /// </summary>
    public static class StoryPlayerEditorTools
    {
        private const string PrefabRoot = "Assets/Prefabs/StoryPlayer";
        private const string TriggersRoot = PrefabRoot + "/Triggers";
        private const string AtomsRoot = PrefabRoot + "/Atoms";
        private const string ResourcesPrefabRoot = "Assets/Resources/Prefabs";
        private const string RigPrefabPath = PrefabRoot + "/StoryPlayerRig.prefab";
        private const string RigPrefabResourcesPath = ResourcesPrefabRoot + "/StoryPlayerRig.prefab";
        private const string ProductionRigPrefabPath = PrefabRoot + "/StoryPlayerProductionRig.prefab";
        private const string ChineseFontAssetPath = "Assets/chinese/OTF/WenYuanSerifSC-Bold SDF.asset";

        // ------------------------------------------------------------
        // Rig prefab —— 旧 Bootstrap 模式封装
        // ------------------------------------------------------------

        [MenuItem("GameCreate3/StoryPlayer/Generate Rig Prefab")]
        public static void GenerateRigPrefab()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[StoryPlayerEditorTools] Cannot generate prefab while in Play mode.");
                return;
            }

            EnsureFolder(PrefabRoot);
            EnsureFolder(ResourcesPrefabRoot);

            // 仍用 inactive + AddComponent 套路：StoryPlayerTestBootstrap.Awake 在 Instantiate 时再触发，
            // 自搭整套 UI。这是 Tier 0 fallback —— 给 StoryPlayerService.EnsureRig 做兜底。
            var temp = new GameObject("StoryPlayerRig");
            temp.SetActive(false);
#pragma warning disable 0618
            temp.AddComponent<StoryPlayerTestBootstrap>();
#pragma warning restore 0618

            PrefabUtility.SaveAsPrefabAsset(temp, RigPrefabPath);
            Object.DestroyImmediate(temp);

            FlipPrefabActive(RigPrefabPath, true);
            CopyAsset(RigPrefabPath, RigPrefabResourcesPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StoryPlayerEditorTools] Saved " + RigPrefabPath + " (mirrored to Resources/Prefabs/).");
        }

        // ------------------------------------------------------------
        // Production Rig prefab —— 生产用，不含废弃 Bootstrap，由 StoryRigBootstrap 引导
        // ------------------------------------------------------------

        [MenuItem("GameCreate3/StoryPlayer/Generate Production Rig Prefab")]
        public static void GenerateProductionRigPrefab()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[StoryPlayerEditorTools] Cannot generate prefab while in Play mode.");
                return;
            }

            EnsureFolder(PrefabRoot);
            EnsureFolder(ResourcesPrefabRoot);

            // -------- Root + Canvas --------
            var root = new GameObject("StoryPlayerRig");
            root.SetActive(false);

            var canvasGO = new GameObject("StoryCanvas", typeof(RectTransform));
            canvasGO.transform.SetParent(root.transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem 由场景的 SceneEssentials.prefab 提供，Rig 不自带（手册 §1, §6.1）

            // -------- Background --------
            var bgImage = CreateFullScreenImage(canvasGO.transform, "BackgroundImage", Color.black);

            // -------- TextContainer + Speaker + Content --------
            var textContainer = new GameObject("TextContainer", typeof(RectTransform));
            textContainer.transform.SetParent(canvasGO.transform, false);
            var textRect = textContainer.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0.3f);
            textRect.offsetMin = new Vector2(50f, 20f);
            textRect.offsetMax = new Vector2(-50f, -20f);

            var speakerGO = new GameObject("SpeakerLabel", typeof(RectTransform));
            speakerGO.transform.SetParent(textContainer.transform, false);
            var speakerLabel = speakerGO.AddComponent<TextMeshProUGUI>();
            speakerLabel.fontSize = 24;
            speakerLabel.color = Color.white;
            speakerLabel.alignment = TextAlignmentOptions.Left;
            var speakerRect = speakerGO.GetComponent<RectTransform>();
            speakerRect.anchorMin = new Vector2(0f, 1f);
            speakerRect.anchorMax = new Vector2(1f, 1f);
            speakerRect.pivot = new Vector2(0.5f, 1f);
            speakerRect.sizeDelta = new Vector2(0f, 40f);

            var contentGO = new GameObject("ContentLabel", typeof(RectTransform));
            contentGO.transform.SetParent(textContainer.transform, false);
            var contentLabel = contentGO.AddComponent<TextMeshProUGUI>();
            contentLabel.fontSize = 32;
            contentLabel.color = Color.white;
            contentLabel.alignment = TextAlignmentOptions.TopLeft;
            AssignChineseFont(speakerLabel);
            AssignChineseFont(contentLabel);
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = new Vector2(0f, -50f);

            // -------- FadeOverlay --------
            var fadeGO = new GameObject("FadeOverlay", typeof(RectTransform));
            fadeGO.transform.SetParent(canvasGO.transform, false);
            var fadeImage = fadeGO.AddComponent<Image>();
            fadeImage.color = Color.black;
            fadeImage.raycastTarget = false;
            var fadeGroup = fadeGO.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            var fadeRect = fadeGO.GetComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;

            // -------- InputBlocker (click to advance) --------
            // StoryInputController 挂在这里：它实现了 IPointerClickHandler/Down/Up，必须放在被 raycast 命中的 UI 上才能收到鼠标事件。
            var blockerGO = new GameObject("InputBlocker", typeof(RectTransform));
            blockerGO.transform.SetParent(canvasGO.transform, false);
            var blockerImage = blockerGO.AddComponent<Image>();
            blockerImage.color = new Color(0f, 0f, 0f, 0f);
            blockerImage.raycastTarget = true;
            var input = blockerGO.AddComponent<StoryInputController>();
            var blockerRect = blockerGO.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            // -------- StoryPlayerSystem (logic components) --------
            var sysGO = new GameObject("StoryPlayerSystem");
            sysGO.transform.SetParent(root.transform, false);
            var player = sysGO.AddComponent<StoryPlayerComp>();
            var renderer = sysGO.AddComponent<StoryPageRenderer>();
            var transition = sysGO.AddComponent<SimpleTransitionController>();
            var audio = sysGO.AddComponent<StoryAudioAdapter>();
            var evt = sysGO.AddComponent<StoryEventSystem>();
            var flow = sysGO.AddComponent<StoryFlowBridge>();

            // -------- Wire private SerializeFields --------
            SetField(renderer, "backgroundImage", bgImage);
            SetField(renderer, "speakerLabel", speakerLabel);
            SetField(renderer, "contentLabel", contentLabel);
            SetField(renderer, "textContainer", textRect);
            SetField(renderer, "fadeOverlay", fadeGroup);

            // 不接 transitionCanvas —— 它会被 SimpleTransitionController.Awake 主动 SetActive(false)，
            // 我们没有独立 transition canvas，淡入淡出靠 fadeOverlay 的 CanvasGroup.alpha。
            SetField(transition, "canvasGroup", fadeGroup);
            SetField(transition, "transitionImage", fadeImage);
            SetField(transition, "transitionRect", fadeRect);

            // -------- StoryRigBootstrap on root --------
            var bootstrap = root.AddComponent<StoryRigBootstrap>();
            SetField(bootstrap, "storyPlayer", player);
            SetField(bootstrap, "pageRenderer", renderer);
            SetField(bootstrap, "transitionController", transition);
            SetField(bootstrap, "inputController", input);
            SetField(bootstrap, "flowBridge", flow);
            SetField(bootstrap, "canvasRoot", canvasGO);

            // -------- Save --------
            PrefabUtility.SaveAsPrefabAsset(root, ProductionRigPrefabPath);
            Object.DestroyImmediate(root);

            FlipPrefabActive(ProductionRigPrefabPath, true);
            AssetDatabase.SaveAssets();
            CopyAsset(ProductionRigPrefabPath, RigPrefabResourcesPath);
            FlipPrefabActive(RigPrefabResourcesPath, true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StoryPlayerEditorTools] Saved production rig to " + ProductionRigPrefabPath +
                      " and mirrored to " + RigPrefabResourcesPath);
        }

        private static Image CreateFullScreenImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static void SetField(Object target, string name, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(name);
            if (prop == null)
            {
                Debug.LogWarning("[StoryPlayerEditorTools] Field not found: " + target.GetType().Name + "." + name);
                return;
            }
            prop.objectReferenceValue = value as Object;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignChineseFont(TMP_Text label)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontAssetPath);
            if (font == null)
            {
                Debug.LogWarning("[StoryPlayerEditorTools] Chinese TMP font not found: " + ChineseFontAssetPath);
                return;
            }

            label.font = font;
        }

        // ------------------------------------------------------------
        // Trigger prefabs —— 生产用，关卡里直接拖
        // ------------------------------------------------------------

        [MenuItem("GameCreate3/StoryPlayer/Generate Trigger Prefabs")]
        public static void GenerateTriggerPrefabs()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[StoryPlayerEditorTools] Cannot generate prefab while in Play mode.");
                return;
            }

            EnsureFolder(TriggersRoot);

            BuildTriggerZonePrefab(TriggersRoot + "/StoryTriggerZone.prefab");
            BuildInteractableTriggerPrefab(TriggersRoot + "/StoryInteractable.prefab");
            BuildEventTriggerPrefab(TriggersRoot + "/StoryWorkspaceEventTrigger.prefab");
            BuildAutoPlayPrefab(TriggersRoot + "/StoryAutoPlay.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StoryPlayerEditorTools] Generated 4 trigger prefabs under " + TriggersRoot);
        }

        private static void BuildTriggerZonePrefab(string path)
        {
            var go = new GameObject("StoryTriggerZone");
            go.SetActive(false);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(3f, 3f);
            int layer = LayerMask.NameToLayer("Trigger");
            if (layer >= 0) go.layer = layer;
            go.AddComponent<StoryTriggerZone>();
            SaveActivePrefab(go, path);
        }

        private static void BuildInteractableTriggerPrefab(string path)
        {
            var go = new GameObject("StoryInteractable");
            go.SetActive(false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.5f, 0.7f, 0.9f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.8f, 1.2f);
            int layer = LayerMask.NameToLayer("Interactable");
            if (layer >= 0) go.layer = layer;
            go.AddComponent<StoryInteractable>();
            SaveActivePrefab(go, path);
        }

        private static void BuildEventTriggerPrefab(string path)
        {
            var go = new GameObject("StoryWorkspaceEventTrigger");
            go.SetActive(false);
            go.AddComponent<StoryWorkspaceEventTrigger>();
            SaveActivePrefab(go, path);
        }

        private static void BuildAutoPlayPrefab(string path)
        {
            var go = new GameObject("StoryAutoPlay");
            go.SetActive(false);
            go.AddComponent<StoryAutoPlay>();
            SaveActivePrefab(go, path);
        }

        // ------------------------------------------------------------
        // Atom prefabs —— Tier 1 UI 原子件
        // ------------------------------------------------------------

        [MenuItem("GameCreate3/StoryPlayer/Generate Atom Prefabs")]
        public static void GenerateAtomPrefabs()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[StoryPlayerEditorTools] Cannot generate prefab while in Play mode.");
                return;
            }

            EnsureFolder(AtomsRoot);

            BuildFullScreenImageAtom(AtomsRoot + "/Background.prefab", "Background", new Color(0.05f, 0.06f, 0.08f, 1f));
            BuildFadeOverlayAtom(AtomsRoot + "/FadeOverlay.prefab");
            BuildInputBlockerAtom(AtomsRoot + "/InputBlocker.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StoryPlayerEditorTools] Generated atom prefabs under " + AtomsRoot);
        }

        private static void BuildFullScreenImageAtom(string path, string name, Color color)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            SaveActivePrefab(go, path);
        }

        private static void BuildFadeOverlayAtom(string path)
        {
            var go = new GameObject("FadeOverlay");
            go.SetActive(false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            SaveActivePrefab(go, path);
        }

        private static void BuildInputBlockerAtom(string path)
        {
            var go = new GameObject("InputBlocker");
            go.SetActive(false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f); // 透明但仍接收点击
            img.raycastTarget = true;
            go.AddComponent<Button>();
            SaveActivePrefab(go, path);
        }

        // ------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------

        private static void SaveActivePrefab(GameObject temp, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            FlipPrefabActive(path, true);
        }

        private static void FlipPrefabActive(string path, bool active)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) return;
            var so = new SerializedObject(asset);
            var prop = so.FindProperty("m_IsActive");
            if (prop != null)
            {
                prop.boolValue = active;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(asset);
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
    }
}
