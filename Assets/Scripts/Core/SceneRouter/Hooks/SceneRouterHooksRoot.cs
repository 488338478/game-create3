using UnityEngine;

namespace GameCreate3.Core.SceneRouting
{
    /// <summary>
    /// SceneRouterHooks.prefab 根节点的活体保留器。
    /// 让整个 Hook 容器 DontDestroyOnLoad，避免切场景时 Hook 订阅丢失。
    /// 同时去重：场景里出现第二份时把自己删了。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class SceneRouterHooksRoot : MonoBehaviour
    {
        private static SceneRouterHooksRoot instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
