using System.IO;
using Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameCreate3.EditorTools
{
    /// <summary>
    /// 生成一个"场景全局必需品"的 prefab：
    ///   - Main Camera（含 CinemachineBrain + AudioListener）
    ///   - EventSystem（含 StandaloneInputModule）
    ///
    /// 任何用 DualWorld / StoryPlayer 的新场景，**先拖这个 prefab**，
    /// 再拖工作区 prefab —— 一步到位。
    ///
    /// 菜单项：GameCreate3 → Generate Scene Essentials Prefab
    /// </summary>
    public static class SceneEssentialsEditorTools
    {
        private const string PrefabRoot = "Assets/Prefabs";
        private const string ResourcesPrefabRoot = "Assets/Resources/Prefabs";
        private const string PrefabPath = PrefabRoot + "/SceneEssentials.prefab";
        private const string PrefabResourcesPath = ResourcesPrefabRoot + "/SceneEssentials.prefab";

        [MenuItem("GameCreate3/Generate Scene Essentials Prefab")]
        public static void Generate()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[SceneEssentialsEditorTools] Cannot generate prefab while in Play mode.");
                return;
            }

            EnsureFolder(PrefabRoot);
            EnsureFolder(ResourcesPrefabRoot);

            var root = new GameObject("SceneEssentials");
            root.SetActive(false); // 防 Awake 在编辑场景里乱跑

            // Main Camera（注意 tag 必须是 MainCamera 否则 Camera.main 是 null）
            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            cam.transform.SetParent(root.transform, false);
            var camera = cam.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cam.AddComponent<AudioListener>();
            cam.AddComponent<CinemachineBrain>();

            // EventSystem
            var es = new GameObject("EventSystem");
            es.transform.SetParent(root.transform, false);
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            // 把 m_IsActive 改回 true（参见 StoryPlayer 工具同款套路）
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefabAsset != null)
            {
                var so = new SerializedObject(prefabAsset);
                var prop = so.FindProperty("m_IsActive");
                if (prop != null)
                {
                    prop.boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorUtility.SetDirty(prefabAsset);
            }

            CopyAsset(PrefabPath, PrefabResourcesPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneEssentialsEditorTools] Saved " + PrefabPath);
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
