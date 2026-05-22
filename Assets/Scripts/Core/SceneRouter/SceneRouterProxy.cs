using UnityEngine;

namespace GameCreate3.Core.SceneRouting
{
    /// <summary>
    /// 把静态 <see cref="SceneRouter"/> 包成实例方法，供 UnityEvent Inspector 拖拽调用。
    /// 挂在场景里任意 GameObject 上即可。
    /// </summary>
    public sealed class SceneRouterProxy : MonoBehaviour
    {
        public void Go(string routeId) => SceneRouter.Go(routeId);
        public void GoScene(string sceneName) => SceneRouter.GoScene(sceneName);
        public void Reload() => SceneRouter.Reload();
    }
}
