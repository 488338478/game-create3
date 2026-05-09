using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GameCreate3.StoryPlayer;

namespace GameCreate3.EditorTools
{
    /// <summary>
    /// StoryPlayer 模块 prefab 一键生成。
    ///
    /// 菜单：
    ///   GameCreate3 → StoryPlayer → Generate Rig Prefab          兼容旧测试 Bootstrap 模式（保留）
    ///   GameCreate3 → StoryPlayer → Generate Trigger Prefabs     生产用 4 个触发器 prefab
    ///   GameCreate3 → StoryPlayer → Generate Atom Prefabs        Tier 1 UI 原子件
    /// </summary>
    public static class StoryPlayerEditorTools
    {
        private const string PrefabRoot = "Assets/Prefabs/StoryPlayer";
        private const string TriggersRoot = PrefabRoot + "/Triggers";
        private const string AtomsRoot = PrefabRoot + "/Atoms";
        private const string ResourcesPrefabRoot = "Assets/Resources/Prefabs";
        private const string RigPrefabPath = PrefabRoot + "/StoryPlayerRig.prefab";
        private const string RigPrefabResourcesPath = ResourcesPrefabRoot + "/StoryPlayerRig.prefab";

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
