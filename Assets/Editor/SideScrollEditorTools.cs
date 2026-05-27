using System.IO;
using UnityEditor;
using UnityEngine;
using GameCreate3;

namespace GameCreate3.EditorTools
{
    /// <summary>
    /// SideScroll 模块 prefab 一键生成。
    ///
    /// 菜单：
    ///   GameCreate3 → SideScroll → Generate Atom Prefabs       Tier 1：触发器、交互物等单组件件
    ///   GameCreate3 → SideScroll → Generate Workspace Shells   Tier 3：Story / Gameplay 工作区壳
    ///
    /// 依赖：玩家 prefab、镜头组 prefab 由 DualWorld 工具同时生成（共用），不在这里重复。
    /// </summary>
    public static class SideScrollEditorTools
    {
        private const string PrefabRoot = "Assets/Prefabs/SideScroll";
        private const string AtomsRoot = PrefabRoot + "/Atoms";
        private const string ShellsRoot = PrefabRoot + "/Shells";
        private const string ResourcesPrefabRoot = "Assets/Resources/Prefabs";

        // ------------------------------------------------------------
        // Tier 1 atoms
        // ------------------------------------------------------------

        [MenuItem("GameCreate3/SideScroll/Generate Atom Prefabs")]
        public static void GenerateAtoms()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[SideScrollEditorTools] Cannot generate prefab while in Play mode.");
                return;
            }

            EnsureFolder(AtomsRoot);

            BuildInteractable<InteractTrigger>(AtomsRoot + "/InteractTrigger.prefab", "InteractTrigger",
                new Color(0.35f, 0.8f, 0.7f), trigger: true);
            BuildInteractable<PickupObject>(AtomsRoot + "/PickupObject.prefab", "PickupObject",
                new Color(0.95f, 0.85f, 0.4f), trigger: true);
            BuildInteractable<PushableObject>(AtomsRoot + "/PushableObject.prefab", "PushableObject",
                new Color(0.85f, 0.55f, 0.3f), trigger: false, withRigidbody: true);
            BuildInteractable<ExitPoint>(AtomsRoot + "/ExitPoint.prefab", "ExitPoint",
                new Color(0.6f, 0.85f, 1f, 0.7f), trigger: true);

            BuildTriggerZone<WorkspaceEventTriggerZone>(AtomsRoot + "/WorkspaceEventTriggerZone.prefab", "WorkspaceEventTriggerZone");
            BuildTriggerZone<DialogueTriggerZone>(AtomsRoot + "/DialogueTriggerZone.prefab", "DialogueTriggerZone");
            BuildTriggerZone<GoalTriggerZone>(AtomsRoot + "/GoalTriggerZone.prefab", "GoalTriggerZone");
            BuildTriggerZone<ConditionTriggerZone>(AtomsRoot + "/ConditionTriggerZone.prefab", "ConditionTriggerZone");
            BuildTriggerZone<CameraTriggerZone>(AtomsRoot + "/CameraTriggerZone.prefab", "CameraTriggerZone");

            BuildGroundAtom(AtomsRoot + "/GroundBlock.prefab");
            BuildCameraBoundsAtom(AtomsRoot + "/CameraBounds.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SideScrollEditorTools] Generated atom prefabs under " + AtomsRoot);
        }

        // ------------------------------------------------------------
        // Tier 3 workspace shells
        // ------------------------------------------------------------

        [MenuItem("GameCreate3/SideScroll/Generate Workspace Shells")]
        public static void GenerateWorkspaceShells()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[SideScrollEditorTools] Cannot generate prefab while in Play mode.");
                return;
            }

            EnsureFolder(ShellsRoot);

            BuildShell<SideScrollStoryWorkspace>(ShellsRoot + "/SideScrollStoryWorkspace.prefab", "StoryWorkspace");
            BuildShell<SideScrollGameplayWorkspace>(ShellsRoot + "/SideScrollGameplayWorkspace.prefab", "GameplayWorkspace");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SideScrollEditorTools] Generated workspace shell prefabs under " + ShellsRoot);
        }

        // ------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------

        private static void BuildInteractable<T>(string path, string name, Color color, bool trigger, bool withRigidbody = false)
            where T : MonoBehaviour
        {
            var go = new GameObject(name);
            go.SetActive(false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = color;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = trigger;
            box.size = new Vector2(0.8f, 0.8f);
            if (withRigidbody)
            {
                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 3f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
            int layer = LayerMask.NameToLayer("Interactable");
            if (layer >= 0) go.layer = layer;
            go.AddComponent<T>();
            SaveActivePrefab(go, path);
        }

        private static void BuildTriggerZone<T>(string path, string name) where T : TriggerZoneBase
        {
            var go = new GameObject(name);
            go.SetActive(false);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(2f, 3f);
            int layer = LayerMask.NameToLayer("Trigger");
            if (layer >= 0) go.layer = layer;
            go.AddComponent<T>();
            SaveActivePrefab(go, path);
        }

        private static void BuildGroundAtom(string path)
        {
            var go = new GameObject("GroundBlock");
            go.SetActive(false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.25f, 0.3f, 0.35f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(4f, 1f);
            int layer = LayerMask.NameToLayer("Ground");
            if (layer >= 0) go.layer = layer;
            SaveActivePrefab(go, path);
        }

        private static void BuildCameraBoundsAtom(string path)
        {
            var go = new GameObject("CameraBounds");
            go.SetActive(false);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(20f, 12f);
            box.isTrigger = true;
            SaveActivePrefab(go, path);
        }

        private static void BuildShell<T>(string path, string name) where T : SideScrollWorkspaceBase
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.AddComponent<T>();
            SaveActivePrefab(go, path);
        }

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

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
