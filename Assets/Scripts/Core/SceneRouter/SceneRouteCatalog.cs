using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.Core.SceneRouting
{
    /// <summary>
    /// 路由表配置。一条路由 = 一个语义 ID + 对应的场景名 + 是否走 loading 遮罩。
    /// 默认从 Resources/SceneRoutes 加载，如要自定义可调用 <see cref="SceneRouter.SetCatalog"/>。
    /// </summary>
    [CreateAssetMenu(fileName = "SceneRoutes", menuName = "Game/Core/Scene Route Catalog")]
    public sealed class SceneRouteCatalog : ScriptableObject
    {
        [SerializeField] private List<SceneRoute> routes = new List<SceneRoute>();

        private Dictionary<string, SceneRoute> lookup;

        public IReadOnlyList<SceneRoute> Routes => routes;

        public bool TryGet(string routeId, out SceneRoute route)
        {
            EnsureLookup();
            return lookup.TryGetValue(routeId, out route);
        }

        private void EnsureLookup()
        {
            if (lookup != null) return;
            lookup = new Dictionary<string, SceneRoute>(routes.Count);
            foreach (var r in routes)
            {
                if (string.IsNullOrWhiteSpace(r.routeId)) continue;
                lookup[r.routeId] = r;
            }
        }

        private void OnValidate()
        {
            lookup = null;
        }
    }

    [Serializable]
    public struct SceneRoute
    {
        [Tooltip("语义 ID，调用方写它。如 start_new_game / level1_intro / main_menu。")]
        public string routeId;

        [Tooltip("Unity 场景名（必须在 Build Settings 里）。")]
        public string sceneName;

        [Tooltip("切换期间是否打开 UIControlSystem 的 loading 页面。")]
        public bool useLoading;
    }
}
