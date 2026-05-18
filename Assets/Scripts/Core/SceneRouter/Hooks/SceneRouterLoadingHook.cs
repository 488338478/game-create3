using UnityEngine;
using GameCreate3.UI;

namespace GameCreate3.Core.SceneRouting
{
    /// <summary>
    /// 切场景时自动开关 UIControlSystem 的 loading 页面。
    /// 仅当 <see cref="SceneRouteContext.UseLoading"/> 为 true 时生效（由 catalog 配）。
    /// 挂在任意场景的 GameObject 上即可；推荐挂在 DontDestroyOnLoad 的 SceneEssentials 子节点。
    /// </summary>
    public sealed class SceneRouterLoadingHook : MonoBehaviour
    {
        [SerializeField] private string loadingPageId = UIPageIds.Loading;

        private void OnEnable()
        {
            SceneRouter.OnBeforeChange += HandleBefore;
            SceneRouter.OnAfterChange += HandleAfter;
        }

        private void OnDisable()
        {
            SceneRouter.OnBeforeChange -= HandleBefore;
            SceneRouter.OnAfterChange -= HandleAfter;
        }

        private void HandleBefore(SceneRouteContext ctx)
        {
            if (!ctx.UseLoading) return;
            UIControlSystem.Instance?.OpenPage(loadingPageId);
        }

        private void HandleAfter(SceneRouteContext ctx)
        {
            if (!ctx.UseLoading) return;
            UIControlSystem.Instance?.ClosePage(loadingPageId);
        }
    }
}
