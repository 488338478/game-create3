using UnityEngine;

namespace GameCreate3.Core.SceneRouting
{
    /// <summary>
    /// SceneRouterHooks.prefab 根节点的活体保留器。
    /// 让整个 Hook 容器 DontDestroyOnLoad，避免切场景时 Hook 订阅丢失。
    /// 同时去重：场景里出现第二份时把自己删了。
    /// 在此处拖入路由表，避免硬编码 Resources 路径。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class SceneRouterHooksRoot : MonoBehaviour
    {
        private static SceneRouterHooksRoot instance;

        [Tooltip("拖入路由表 ScriptableObject。留空则回退到 Resources/SceneRoutes。")]
        [SerializeField] private SceneRouteCatalog catalog;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (catalog != null)
                SceneRouter.SetCatalog(catalog);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
