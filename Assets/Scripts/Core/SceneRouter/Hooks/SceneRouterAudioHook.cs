using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.Core.SceneRouting
{
    /// <summary>
    /// 切场景时的 BGM 处理：
    /// - 切之前淡出当前 BGM
    /// - 切之后，若 <see cref="routeBgm"/> 表里配置了 ToRouteId 对应的 bgmId，自动 PlayBGM
    ///
    /// 想跳过自动播放某条路由，把 bgmId 留空。
    /// </summary>
    public sealed class SceneRouterAudioHook : MonoBehaviour
    {
        [Serializable]
        public struct RouteBgm
        {
            public string routeId;
            public string bgmId;
            public bool loop;
            [Range(0f, 1f)] public float volumeScale;
        }

        [Tooltip("routeId → BGM 映射。同一 routeId 后写的覆盖前面的。")]
        [SerializeField] private List<RouteBgm> routeBgm = new List<RouteBgm>();

        [Tooltip("切场景前是否淡出 BGM")]
        [SerializeField] private bool fadeOutOnLeave = true;

        private Dictionary<string, RouteBgm> lookup;

        private void OnEnable()
        {
            RebuildLookup();
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
            if (!fadeOutOnLeave) return;
            GameAudioService.Instance?.FadeOut(GameAudioChannel.Bgm);
        }

        private void HandleAfter(SceneRouteContext ctx)
        {
            if (lookup == null || string.IsNullOrEmpty(ctx.ToRouteId)) return;
            if (!lookup.TryGetValue(ctx.ToRouteId, out var entry)) return;
            if (string.IsNullOrEmpty(entry.bgmId)) return;

            var vol = entry.volumeScale <= 0f ? 1f : entry.volumeScale;
            GameAudioService.Instance?.PlayBGM(entry.bgmId, entry.loop, vol);
        }

        private void RebuildLookup()
        {
            lookup = new Dictionary<string, RouteBgm>(routeBgm.Count);
            foreach (var r in routeBgm)
            {
                if (string.IsNullOrEmpty(r.routeId)) continue;
                lookup[r.routeId] = r;
            }
        }

        private void OnValidate()
        {
            lookup = null;
        }
    }
}
