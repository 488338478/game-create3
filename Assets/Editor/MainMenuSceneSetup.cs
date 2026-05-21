using GameCreate3.Core;
using GameCreate3.Core.SceneRouting;
using GameCreate3.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameCreate3.EditorTools
{
    /// <summary>
    /// 一键把当前场景配置成可运行的主菜单场景（解决「一片空白」）。
    /// </summary>
    public static class MainMenuSceneSetup
    {
        private const string SceneEssentialsName = "SceneEssentials";
        private const string SceneRouterHooksPath = "Assets/Prefabs/Core/SceneRouterHooks.prefab";
        private const string SaveProgressServicePath = "Assets/Prefabs/Core/SaveProgressService.prefab";
        private const string UIControlSystemPath = "Assets/Prefabs/UI/System/UIControlSystem.prefab";
        private const string MainMenuPagePath = "Assets/Prefabs/UI/Pages/MainMenuPage.prefab";
        private const string LevelSelectPagePath = "Assets/Prefabs/UI/System/LevelSelectPage.prefab";

        [MenuItem("GameCreate3/Setup MainMenu Scene (Fix Blank Screen)")]
        public static void SetupCurrentScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[MainMenuSceneSetup] 请在非 Play 模式下运行。");
                return;
            }

            var essentials = FindOrCreate(SceneEssentialsName);

            // These prefabs call DontDestroyOnLoad in Awake, so Unity requires
            // them to live at the scene root instead of under SceneEssentials.
            var hooks = InstantiateRootPrefab(SceneRouterHooksPath);
            var save = InstantiateRootPrefab(SaveProgressServicePath);
            var uiRoot = InstantiateRootPrefab(UIControlSystemPath);

            if (uiRoot == null)
            {
                Debug.LogError("[MainMenuSceneSetup] 无法加载 UIControlSystem 预制体。");
                return;
            }

            var control = uiRoot.GetComponent<UIControlSystem>();
            if (control == null)
            {
                Debug.LogError("[MainMenuSceneSetup] UIControlSystem 组件缺失。");
                return;
            }

            var menuRoot = uiRoot.transform.Find("MenuRoot");
            if (menuRoot == null)
            {
                Debug.LogError("[MainMenuSceneSetup] 找不到 MenuRoot。");
                return;
            }

            var mainMenuPage = InstantiateUiPage(MainMenuPagePath, menuRoot);
            var levelSelectPage = InstantiateUiPage(LevelSelectPagePath, menuRoot);
            FixLevelRouteIds(levelSelectPage);

            RegisterScenePage(control, mainMenuPage);
            RegisterScenePage(control, levelSelectPage);
            SetStartupPage(control, UIPageIds.MainMenu);

            if (essentials.GetComponent<MainMenuBootstrap>() == null)
            {
                essentials.AddComponent<MainMenuBootstrap>();
            }

            EnsureCamera();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = essentials;

            Debug.Log(
                "[MainMenuSceneSetup] 完成。请保存场景，并把该场景加入 Build Settings 且放在第一位。然后 Play 测试。");
        }

        private static GameObject FindOrCreate(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
            {
                return existing;
            }

            return new GameObject(name);
        }

        private static GameObject InstantiateRootPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[MainMenuSceneSetup] 找不到预制体: {path}");
                return null;
            }

            var instanceName = prefab.name;
            var existing = GameObject.Find(instanceName);
            if (existing != null)
            {
                existing.transform.SetParent(null, true);
                return existing;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = instanceName;
            instance.transform.SetParent(null, true);
            return instance;
        }

        private static UIPageController InstantiateUiPage(string path, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[MainMenuSceneSetup] 找不到页面预制体: {path}");
                return null;
            }

            var page = prefab.GetComponent<UIPageController>();
            if (page == null)
            {
                Debug.LogWarning($"[MainMenuSceneSetup] 预制体缺少 UIPageController: {path}");
                return null;
            }

            var existing = FindPageInChildren(parent, page.PageId);
            if (existing != null)
            {
                NormalizeUiPageRoot(existing.gameObject);
                return existing;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = prefab.name;
            NormalizeUiPageRoot(instance);
            return instance.GetComponent<UIPageController>();
        }

        private static UIPageController FindPageInChildren(Transform parent, string pageId)
        {
            var pages = parent.GetComponentsInChildren<UIPageController>(true);
            for (var i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null && pages[i].PageId == pageId)
                {
                    return pages[i];
                }
            }

            return null;
        }

        private static void NormalizeUiPageRoot(GameObject pageRoot)
        {
            var rect = pageRoot.GetComponent<RectTransform>();
            if (rect == null)
            {
                var transform = pageRoot.transform;
                var parent = transform.parent;
                var sibling = transform.GetSiblingIndex();
                var localPosition = transform.localPosition;
                var localRotation = transform.localRotation;
                var localScale = transform.localScale;

                Object.DestroyImmediate(transform);
                rect = pageRoot.AddComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.SetSiblingIndex(sibling);
                rect.localPosition = localPosition;
                rect.localRotation = localRotation;
                rect.localScale = localScale;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localPosition = Vector3.zero;

            StretchChild(pageRoot.transform, "BackgroundRoot");
        }

        private static void StretchChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child == null || child is not RectTransform rect)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void FixLevelRouteIds(UIPageController levelSelectPage)
        {
            if (levelSelectPage == null || levelSelectPage is not UILevelSelectPageController controller)
            {
                return;
            }

            var so = new SerializedObject(controller);
            var levels = so.FindProperty("levels");
            if (levels == null)
            {
                return;
            }

            for (var i = 0; i < levels.arraySize; i++)
            {
                var routeId = levels.GetArrayElementAtIndex(i).FindPropertyRelative("routeId");
                if (routeId == null)
                {
                    continue;
                }

                routeId.stringValue = routeId.stringValue.Trim();
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(levelSelectPage);
        }

        private static void SetStartupPage(UIControlSystem control, string pageId)
        {
            var so = new SerializedObject(control);
            var startup = so.FindProperty("startupPageId");
            if (startup != null)
            {
                startup.stringValue = pageId;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RegisterScenePage(UIControlSystem control, UIPageController page)
        {
            if (page == null)
            {
                return;
            }

            var so = new SerializedObject(control);
            var scenePages = so.FindProperty("scenePages");
            for (var i = 0; i < scenePages.arraySize; i++)
            {
                if (scenePages.GetArrayElementAtIndex(i).objectReferenceValue == page)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }

            scenePages.InsertArrayElementAtIndex(scenePages.arraySize);
            scenePages.GetArrayElementAtIndex(scenePages.arraySize - 1).objectReferenceValue = page;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
            cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        }
    }
}
