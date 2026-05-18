using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCreate3.Core.SceneRouting
{
    /// <summary>
    /// 全局场景切换门面。任何代码任何地方：<c>SceneRouter.Go("level1_intro")</c>。
    ///
    /// 设计原则：
    /// - 唯一负责"切场景"这件事。其他模块（UI、剧情、调试）只对它说话。
    /// - 不感知 UI 流程／语义节点；不维护返回栈。需要那些上层概念时再做独立模块。
    /// - 通过 <see cref="OnBeforeChange"/> / <see cref="OnAfterChange"/> 让存档、音频、loading
    ///   等系统挂钩，无硬依赖。
    ///
    /// Catalog 默认从 Resources/SceneRoutes 加载；可调用 <see cref="SetCatalog"/> 替换。
    /// </summary>
    public static class SceneRouter
    {
        private const string DefaultCatalogResourcePath = "SceneRoutes";

        private static SceneRouteCatalog catalog;
        private static SceneRouterRunner runner;
        private static bool isTransitioning;

        public static string CurrentRouteId { get; private set; } = string.Empty;
        public static object LastPayload { get; private set; }
        public static bool IsTransitioning => isTransitioning;

        public static event Action<SceneRouteContext> OnBeforeChange;
        public static event Action<SceneRouteContext> OnAfterChange;

        // ------------------------------------------------------------
        // 配置
        // ------------------------------------------------------------

        public static void SetCatalog(SceneRouteCatalog newCatalog)
        {
            catalog = newCatalog;
        }

        // ------------------------------------------------------------
        // API：按 routeId 切（推荐）
        // ------------------------------------------------------------

        public static void Go(string routeId, object payload = null)
        {
            if (!TryResolveScene(routeId, out var route)) return;
            BeginLoad(routeId, route.sceneName, route.useLoading, payload);
        }

        public static Task GoAsync(string routeId, object payload = null)
        {
            if (!TryResolveScene(routeId, out var route))
            {
                return Task.CompletedTask;
            }
            return BeginLoadAsync(routeId, route.sceneName, route.useLoading, payload);
        }

        // ------------------------------------------------------------
        // API：按场景名直切（用于调试 / 不在 catalog 里的临时场景）
        // ------------------------------------------------------------

        public static void GoScene(string sceneName, object payload = null)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneRouter] GoScene called with empty sceneName.");
                return;
            }
            BeginLoad(string.Empty, sceneName, false, payload);
        }

        public static Task GoSceneAsync(string sceneName, object payload = null)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneRouter] GoSceneAsync called with empty sceneName.");
                return Task.CompletedTask;
            }
            return BeginLoadAsync(string.Empty, sceneName, false, payload);
        }

        // ------------------------------------------------------------
        // API：重载当前场景
        // ------------------------------------------------------------

        public static void Reload()
        {
            var currentName = SceneManager.GetActiveScene().name;
            BeginLoad(CurrentRouteId, currentName, false, LastPayload);
        }

        public static Task ReloadAsync()
        {
            var currentName = SceneManager.GetActiveScene().name;
            return BeginLoadAsync(CurrentRouteId, currentName, false, LastPayload);
        }

        // ------------------------------------------------------------
        // 内部：解析、加载
        // ------------------------------------------------------------

        private static bool TryResolveScene(string routeId, out SceneRoute route)
        {
            if (string.IsNullOrWhiteSpace(routeId))
            {
                Debug.LogError("[SceneRouter] Go called with empty routeId.");
                route = default;
                return false;
            }

            EnsureCatalogLoaded();

            if (catalog == null)
            {
                Debug.LogError("[SceneRouter] No catalog assigned and Resources/SceneRoutes not found.");
                route = default;
                return false;
            }

            if (!catalog.TryGet(routeId, out route))
            {
                Debug.LogError($"[SceneRouter] Route '{routeId}' not found in catalog.");
                return false;
            }

            if (string.IsNullOrEmpty(route.sceneName))
            {
                Debug.LogError($"[SceneRouter] Route '{routeId}' has empty sceneName.");
                return false;
            }

            return true;
        }

        private static void BeginLoad(string routeId, string sceneName, bool useLoading, object payload)
        {
            if (isTransitioning)
            {
                Debug.LogWarning($"[SceneRouter] Already transitioning; ignored request to '{sceneName}'.");
                return;
            }

            EnsureRunner();
            runner.StartCoroutine(LoadRoutine(routeId, sceneName, useLoading, payload));
        }

        private static Task BeginLoadAsync(string routeId, string sceneName, bool useLoading, object payload)
        {
            if (isTransitioning)
            {
                Debug.LogWarning($"[SceneRouter] Already transitioning; ignored async request to '{sceneName}'.");
                return Task.CompletedTask;
            }

            EnsureRunner();
            var tcs = new TaskCompletionSource<bool>();
            runner.StartCoroutine(LoadRoutineAwaitable(routeId, sceneName, useLoading, payload, tcs));
            return tcs.Task;
        }

        private static IEnumerator LoadRoutine(string routeId, string sceneName, bool useLoading, object payload)
        {
            isTransitioning = true;
            var fromScene = SceneManager.GetActiveScene().name;
            var ctx = new SceneRouteContext(fromScene, routeId, sceneName, payload, useLoading);

            try
            {
                OnBeforeChange?.Invoke(ctx);
            }
            catch (Exception ex) { Debug.LogException(ex); }

            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogError($"[SceneRouter] LoadSceneAsync returned null for '{sceneName}'. Is it in Build Settings?");
                isTransitioning = false;
                yield break;
            }
            while (!op.isDone) yield return null;

            CurrentRouteId = routeId ?? string.Empty;
            LastPayload = payload;
            isTransitioning = false;

            try
            {
                OnAfterChange?.Invoke(ctx);
            }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        private static IEnumerator LoadRoutineAwaitable(string routeId, string sceneName, bool useLoading, object payload, TaskCompletionSource<bool> tcs)
        {
            yield return LoadRoutine(routeId, sceneName, useLoading, payload);
            tcs.TrySetResult(true);
        }

        private static void EnsureCatalogLoaded()
        {
            if (catalog != null) return;
            catalog = Resources.Load<SceneRouteCatalog>(DefaultCatalogResourcePath);
        }

        private static void EnsureRunner()
        {
            if (runner != null) return;
            var go = new GameObject("[SceneRouterRunner]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<SceneRouterRunner>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            catalog = null;
            runner = null;
            isTransitioning = false;
            CurrentRouteId = string.Empty;
            LastPayload = null;
            OnBeforeChange = null;
            OnAfterChange = null;
        }

        private sealed class SceneRouterRunner : MonoBehaviour { }
    }
}
