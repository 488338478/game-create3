using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace GameCreate3.EditorTools
{
    public static class CreateSkipOverlayPrefab
    {
        [MenuItem("Tools/Create Skip Overlay Prefab")]
        public static void Create()
        {
            // Root: Canvas
            var root = new GameObject("SkipOverlay");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            root.AddComponent<GraphicRaycaster>();

            // Persistent hint: bottom-right "ESC to Skip"
            var hint = CreateText(root.transform, "HintText", "ESC to Skip",
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 20), new Vector2(200, 40), TextAlignmentOptions.BottomRight);
            var hintCG = hint.AddComponent<CanvasGroup>();
            hintCG.alpha = 0.6f;

            // Confirm panel (hidden by default)
            var panel = new GameObject("ConfirmPanel");
            panel.transform.SetParent(root.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.5f);
            panel.SetActive(false);

            // System message text: top-center
            var msg = CreateText(root.transform, "SystemMessage", "",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(600, 50), TextAlignmentOptions.Center);
            msg.SetActive(false);

            // Attach script
            var overlay = root.AddComponent<GameCreate3.UI.UISkipOverlay>();
            var so = new SerializedObject(overlay);
            so.FindProperty("persistentHint").objectReferenceValue = hint;
            so.FindProperty("confirmPanel").objectReferenceValue = panel;
            so.FindProperty("systemMessageText").objectReferenceValue = msg.GetComponent<TMP_Text>();
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save prefab
            var path = "Assets/Prefabs/UI/System/SkipOverlay.prefab";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(
                System.IO.Path.Combine(Application.dataPath, "..", path)));
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            Debug.Log($"[SkipOverlay] Prefab created at {path}");
        }

        private static GameObject CreateText(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            rect.pivot = new Vector2(0.5f, 0.5f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return go;
        }
    }
}
