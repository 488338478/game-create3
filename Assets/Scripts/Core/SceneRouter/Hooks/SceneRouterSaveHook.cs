using UnityEngine;

namespace GameCreate3.Core.SceneRouting
{
    /// <summary>
    /// 切场景前自动调 GameSaveProgressService.Save()。
    /// 默认对所有切换生效；调试场景可在 <see cref="excludeRouteIds"/> 里排除。
    /// </summary>
    public sealed class SceneRouterSaveHook : MonoBehaviour
    {
        [Tooltip("这些 routeId 切换前不存档（调试 / sandbox 用）")]
        [SerializeField] private string[] excludeRouteIds;

        private void OnEnable()
        {
            SceneRouter.OnBeforeChange += HandleBefore;
        }

        private void OnDisable()
        {
            SceneRouter.OnBeforeChange -= HandleBefore;
        }

        private void HandleBefore(SceneRouteContext ctx)
        {
            if (IsExcluded(ctx.ToRouteId)) return;
            GameSaveProgressService.Instance?.Save();
        }

        private bool IsExcluded(string routeId)
        {
            if (excludeRouteIds == null || string.IsNullOrEmpty(routeId)) return false;
            for (int i = 0; i < excludeRouteIds.Length; i++)
            {
                if (excludeRouteIds[i] == routeId) return true;
            }
            return false;
        }
    }
}
