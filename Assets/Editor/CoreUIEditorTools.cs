using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GameCreate3.Core;
using GameCreate3.UI;

namespace GameCreate3.EditorTools
{
    /// <summary>
    /// Core / UI 模块 prefab 一键生成。
    /// 菜单：
    ///   GameCreate3/Core/Generate Core Prefabs
    ///   GameCreate3/UI/Generate UI Prefabs
    ///   GameCreate3/Generate Core UI Prefabs
    /// </summary>
    public static class CoreUIEditorTools
    {
        private const string CoreRoot = "Assets/Prefabs/Core";
        private const string UiRoot = "Assets/Prefabs/UI";
        private const string UiSystemRoot = UiRoot + "/System";
        private const string UiPagesRoot = UiRoot + "/Pages";
        private const string UiPopupsRoot = UiRoot + "/Popups";
        private const string UiAtomsRoot = UiRoot + "/Atoms";

        private const string AudioServicePath = CoreRoot + "/AudioService.prefab";
        private const string SaveProgressServicePath = CoreRoot + "/SaveProgressService.prefab";
        private const string GlobalFlowRouterPath = CoreRoot + "/GlobalFlowRouter.prefab";
        private const string UIControlSystemPath = UiSystemRoot + "/UIControlSystem.prefab";
        private const string UISettingsServicePath = UiSystemRoot + "/UISettingsService.prefab";
        private const string CGGalleryPagePath = UiPagesRoot + "/CGGalleryPage.prefab";
        private const string PromptPopupPath = UiPopupsRoot + "/PromptPopup.prefab";
        private const string CGGallerySlotPath = UiAtomsRoot + "/CGGallerySlot.prefab";
        private const string VolumeSliderPath = UiAtomsRoot + "/VolumeSlider.prefab";

        [MenuItem("GameCreate3/Generate Core UI Prefabs")]
        public static void GenerateAll()
        {
            GenerateCorePrefabs();
            GenerateUIPrefabs();
        }

        [MenuItem("GameCreate3/Core/Generate Core Prefabs")]
        public static void GenerateCorePrefabs()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[CoreUIEditorTools] Cannot generate prefab while in Play mode.");
                return;
            }

            EnsureFolder(CoreRoot);
            BuildAudioService();
            BuildSingleComponentPrefab<GameSaveProgressService>("SaveProgressService", SaveProgressServicePath);
            BuildSingleComponentPrefab<GlobalFlowRouter>("GlobalFlowRouter", GlobalFlowRouterPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CoreUIEditorTools] Generated Core prefabs under " + CoreRoot);
        }

        [MenuItem("GameCreate3/UI/Generate UI Prefabs")]
        public static void GenerateUIPrefabs()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[CoreUIEditorTools] Cannot generate prefab while in Play mode.");
                return;
            }

            EnsureFolder(UiSystemRoot);
            EnsureFolder(UiPagesRoot);
            EnsureFolder(UiPopupsRoot);
            EnsureFolder(UiAtomsRoot);

            var cgGallerySlot = BuildCGGallerySlot();
            BuildVolumeSlider();
            var promptPopup = BuildPromptPopup();
            var cgGalleryPage = BuildCGGalleryPage(cgGallerySlot);
            BuildUISettingsService();
            BuildUIControlSystem(cgGalleryPage, promptPopup);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CoreUIEditorTools] Generated UI prefabs under " + UiRoot);
        }

        private static void BuildAudioService()
        {
            var go = new GameObject("AudioService");
            go.SetActive(false);
            var service = go.AddComponent<GameAudioService>();

            var bgm = CreateAudioSourceChild(go.transform, "Core_BGM", true);
            var ambient = CreateAudioSourceChild(go.transform, "Core_Ambient", true);
            var sfx = CreateAudioSourceChild(go.transform, "Core_SFX", false);
            var ui = CreateAudioSourceChild(go.transform, "Core_UI", false);
            var voice = CreateAudioSourceChild(go.transform, "Core_Voice", false);

            var so = new SerializedObject(service);
            SetObject(so, "bgmSource", bgm);
            SetObject(so, "ambientSource", ambient);
            SetObject(so, "sfxSource", sfx);
            SetObject(so, "uiSource", ui);
            SetObject(so, "voiceSource", voice);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveActivePrefab(go, AudioServicePath);
        }

        private static AudioSource CreateAudioSourceChild(Transform parent, string name, bool loop)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            return source;
        }

        private static void BuildSingleComponentPrefab<T>(string name, string path) where T : Component
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.AddComponent<T>();
            SaveActivePrefab(go, path);
        }

        private static void BuildUIControlSystem(UIPageController cgPage, UIPageController prompt)
        {
            var go = new GameObject("UIControlSystem");
            go.SetActive(false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<GraphicRaycaster>();
            var control = go.AddComponent<UIControlSystem>();

            var mainRoot = CreateStretchRect("MainRoot", go.transform);
            var hudRoot = CreateStretchRect("HudRoot", go.transform);
            var menuRoot = CreateStretchRect("MenuRoot", go.transform);
            var popupRoot = CreateStretchRect("PopupRoot", go.transform);
            var overlayRoot = CreateStretchRect("OverlayRoot", go.transform);
            var hudGroup = hudRoot.gameObject.AddComponent<CanvasGroup>();

            var so = new SerializedObject(control);
            SetObject(so, "mainRoot", mainRoot);
            SetObject(so, "hudRoot", hudRoot);
            SetObject(so, "menuRoot", menuRoot);
            SetObject(so, "popupRoot", popupRoot);
            SetObject(so, "overlayRoot", overlayRoot);
            SetObject(so, "hudGroup", hudGroup);

            var pages = so.FindProperty("pagePrefabs");
            pages.arraySize = cgPage != null ? 1 : 0;
            if (cgPage != null)
            {
                var entry = pages.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("pageId").stringValue = UIPageIds.CGGallery;
                entry.FindPropertyRelative("prefab").objectReferenceValue = cgPage;
                entry.FindPropertyRelative("layer").enumValueIndex = (int)UIPageLayer.Menu;
                entry.FindPropertyRelative("closePeersOnOpen").boolValue = true;
            }

            var popups = so.FindProperty("popupPrefabs");
            popups.arraySize = prompt != null ? 2 : 0;
            if (prompt != null)
            {
                SetPopupEntry(popups.GetArrayElementAtIndex(0), UIPageIds.ConfirmPopup, prompt);
                SetPopupEntry(popups.GetArrayElementAtIndex(1), UIPageIds.SkipPrompt, prompt);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            SaveActivePrefab(go, UIControlSystemPath);
        }

        private static void SetPopupEntry(SerializedProperty entry, string id, UIPageController prefab)
        {
            entry.FindPropertyRelative("popupId").stringValue = id;
            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        }

        private static void BuildUISettingsService()
        {
            BuildSingleComponentPrefab<UISettingsService>("UISettingsService", UISettingsServicePath);
        }

        private static UIPageController BuildCGGalleryPage(UICGGallerySlotView slotPrefab)
        {
            var go = CreateUiPanel("CGGalleryPage", new Vector2(900f, 620f), new Color(0.08f, 0.09f, 0.11f, 0.94f));
            var group = go.AddComponent<CanvasGroup>();
            var page = go.AddComponent<UICGGalleryPageController>();
            ConfigurePage(page, UIPageIds.CGGallery, UIPageLayer.Menu, true, group, null);

            var title = CreateTMPText("Title", go.transform, "CG Gallery", 28, TextAlignmentOptions.Center);
            SetAnchors(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -72f), new Vector2(0f, -20f));

            var content = CreateStretchRect("Content", go.transform);
            SetAnchors(content, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(32f, 32f), new Vector2(-32f, -92f));
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(160f, 130f);
            grid.spacing = new Vector2(18f, 18f);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            var so = new SerializedObject(page);
            SetObject(so, "slotContainer", content);
            SetObject(so, "slotPrefab", slotPrefab);
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = SaveActivePrefab(go, CGGalleryPagePath);
            return asset != null ? asset.GetComponent<UIPageController>() : null;
        }

        private static UIPageController BuildPromptPopup()
        {
            var go = CreateUiPanel("PromptPopup", new Vector2(520f, 300f), new Color(0.09f, 0.1f, 0.12f, 0.97f));
            var group = go.AddComponent<CanvasGroup>();
            var popup = go.AddComponent<UIPromptPopup>();

            var title = CreateTMPText("Title", go.transform, "提示", 26, TextAlignmentOptions.Center);
            SetAnchors(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -68f), new Vector2(-28f, -24f));

            var message = CreateTMPText("Message", go.transform, "确认继续？", 20, TextAlignmentOptions.Center);
            SetAnchors(message.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(36f, 96f), new Vector2(-36f, -86f));

            var cancelButton = CreateButton("CancelButton", go.transform, "取消", new Color(0.22f, 0.24f, 0.28f, 1f));
            SetAnchors(cancelButton.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-176f, 28f), new Vector2(-28f, 76f));

            var confirmButton = CreateButton("ConfirmButton", go.transform, "确定", new Color(0.24f, 0.42f, 0.68f, 1f));
            SetAnchors(confirmButton.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(28f, 28f), new Vector2(176f, 76f));

            ConfigurePage(popup, UIPageIds.ConfirmPopup, UIPageLayer.Popup, false, group, confirmButton.Button.gameObject);

            var so = new SerializedObject(popup);
            SetObject(so, "titleLabel", title);
            SetObject(so, "messageLabel", message);
            SetObject(so, "confirmLabel", confirmButton.Label);
            SetObject(so, "cancelLabel", cancelButton.Label);
            SetObject(so, "confirmButton", confirmButton.Button);
            SetObject(so, "cancelButton", cancelButton.Button);
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = SaveActivePrefab(go, PromptPopupPath);
            return asset != null ? asset.GetComponent<UIPageController>() : null;
        }

        private static UICGGallerySlotView BuildCGGallerySlot()
        {
            var go = CreateUiPanel("CGGallerySlot", new Vector2(160f, 130f), new Color(0.13f, 0.14f, 0.16f, 1f));
            var rootImage = go.GetComponent<Image>();
            var button = go.AddComponent<Button>();
            button.targetGraphic = rootImage;
            var view = go.AddComponent<UICGGallerySlotView>();

            var thumbnail = new GameObject("Thumbnail");
            thumbnail.transform.SetParent(go.transform, false);
            var thumbRect = thumbnail.AddComponent<RectTransform>();
            SetAnchors(thumbRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 34f), new Vector2(-10f, -10f));
            thumbnail.AddComponent<CanvasRenderer>();
            var thumbnailImage = thumbnail.AddComponent<Image>();
            thumbnailImage.color = new Color(0.22f, 0.23f, 0.26f, 1f);

            var title = CreateTMPText("Title", go.transform, "???", 16, TextAlignmentOptions.Center);
            SetAnchors(title.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(8f, 6f), new Vector2(-8f, 30f));

            var locked = new GameObject("LockedOverlay");
            locked.transform.SetParent(go.transform, false);
            var lockedRect = locked.AddComponent<RectTransform>();
            SetAnchors(lockedRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            locked.AddComponent<CanvasRenderer>();
            var lockedImage = locked.AddComponent<Image>();
            lockedImage.color = new Color(0f, 0f, 0f, 0.55f);
            var lockedGroup = locked.AddComponent<CanvasGroup>();

            var lockLabel = CreateTMPText("LockLabel", locked.transform, "LOCKED", 14, TextAlignmentOptions.Center);
            SetAnchors(lockLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var so = new SerializedObject(view);
            SetObject(so, "titleLabel", title);
            SetObject(so, "thumbnailImage", thumbnailImage);
            SetObject(so, "lockedGroup", lockedGroup);
            SetObject(so, "button", button);
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = SaveActivePrefab(go, CGGallerySlotPath);
            return asset != null ? asset.GetComponent<UICGGallerySlotView>() : null;
        }

        private static void BuildVolumeSlider()
        {
            var go = new GameObject("VolumeSlider");
            go.SetActive(false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260f, 24f);
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            var background = CreateSliderPart("Background", go.transform, new Color(0.16f, 0.17f, 0.2f, 1f));
            SetAnchors(background.Rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var fillArea = CreateStretchRect("Fill Area", go.transform);
            SetAnchors(fillArea, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
            var fill = CreateSliderPart("Fill", fillArea, new Color(0.24f, 0.52f, 0.72f, 1f));
            SetAnchors(fill.Rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var handleArea = CreateStretchRect("Handle Slide Area", go.transform);
            SetAnchors(handleArea, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
            var handle = CreateSliderPart("Handle", handleArea, new Color(0.92f, 0.94f, 0.96f, 1f));
            handle.Rect.sizeDelta = new Vector2(22f, 22f);

            slider.fillRect = fill.Rect;
            slider.handleRect = handle.Rect;
            slider.targetGraphic = handle.Image;

            var binder = go.AddComponent<UIVolumeSliderBinder>();
            var so = new SerializedObject(binder);
            so.FindProperty("channel").enumValueIndex = (int)UIVolumeChannel.Master;
            SetObject(so, "slider", slider);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveActivePrefab(go, VolumeSliderPath);
        }

        private static GameObject CreateUiPanel(string name, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            go.AddComponent<CanvasRenderer>();
            var image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private static RectTransform CreateStretchRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TextMeshProUGUI CreateTMPText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200f, 40f);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        private static ButtonParts CreateButton(string name, Transform parent, string text, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(148f, 48f);
            go.AddComponent<CanvasRenderer>();
            var image = go.AddComponent<Image>();
            image.color = color;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var label = CreateTMPText("Label", go.transform, text, 18, TextAlignmentOptions.Center);
            SetAnchors(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return new ButtonParts(rect, button, label);
        }

        private static SliderPart CreateSliderPart(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            go.AddComponent<CanvasRenderer>();
            var image = go.AddComponent<Image>();
            image.color = color;
            return new SliderPart(rect, image);
        }

        private static void ConfigurePage(UIPageController page, string id, UIPageLayer layer, bool closePeers, CanvasGroup group, GameObject firstSelected)
        {
            var so = new SerializedObject(page);
            so.FindProperty("pageId").stringValue = id;
            so.FindProperty("layer").enumValueIndex = (int)layer;
            so.FindProperty("closePeersOnOpen").boolValue = closePeers;
            so.FindProperty("hideOnAwake").boolValue = true;
            SetObject(so, "canvasGroup", group);
            SetObject(so, "firstSelected", firstSelected);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetObject(SerializedObject so, string propertyName, Object value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static GameObject SaveActivePrefab(GameObject temp, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset != null)
            {
                var so = new SerializedObject(asset);
                var prop = so.FindProperty("m_IsActive");
                if (prop != null)
                {
                    prop.boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorUtility.SetDirty(asset);
            }

            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private readonly struct ButtonParts
        {
            public readonly RectTransform Rect;
            public readonly Button Button;
            public readonly TextMeshProUGUI Label;

            public ButtonParts(RectTransform rect, Button button, TextMeshProUGUI label)
            {
                Rect = rect;
                Button = button;
                Label = label;
            }
        }

        private readonly struct SliderPart
        {
            public readonly RectTransform Rect;
            public readonly Image Image;

            public SliderPart(RectTransform rect, Image image)
            {
                Rect = rect;
                Image = image;
            }
        }
    }
}
