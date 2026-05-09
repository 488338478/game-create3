using System;
using UnityEngine;

namespace GameCreate3.StoryPlayer
{
    /// <summary>
    /// 剧情播放静态门面。
    /// 任何代码任何位置：<c>StoryPlayerService.Play(sequence)</c>。
    /// 第一次调用时按下面顺序找一个 StoryPlayer：
    ///   1. 场景里已有的（兼容旧 <see cref="StoryPlayerTestBootstrap"/> 模式）
    ///   2. 从 Resources/Prefabs/StoryPlayerRig 加载并 DontDestroyOnLoad
    /// </summary>
    public static class StoryPlayerService
    {
        private const string RigResourcePath = "Prefabs/StoryPlayerRig";

        private static StoryPlayer player;

        public static bool IsPlaying => player != null && player.IsPlaying;
        public static StoryPlayer Player => EnsureRig() ? player : null;

        public static void Play(StorySequence sequence, Action onComplete = null)
        {
            if (sequence == null)
            {
                Debug.LogError("[StoryPlayerService] Cannot play null sequence.");
                return;
            }
            if (!EnsureRig())
            {
                Debug.LogError("[StoryPlayerService] Failed to acquire StoryPlayer rig. " +
                    "Place StoryPlayerRig.prefab under Assets/Resources/Prefabs/ (run menu " +
                    "'GameCreate3/StoryPlayer/Generate Rig Prefab') or have a scene with StoryPlayerTestBootstrap.");
                return;
            }

            if (onComplete != null)
            {
                Action handler = null;
                handler = () =>
                {
                    player.OnSequenceCompleted -= handler;
                    onComplete();
                };
                player.OnSequenceCompleted += handler;
            }

            player.Play(sequence);
        }

        public static void Stop() => player?.Stop();

        public static void Skip() => player?.SkipSequence();

        private static bool EnsureRig()
        {
            if (player != null) return true;

            // 1. 场景里已有 —— 兼容老 Bootstrap 模式 / 手挂模式
            player = UnityEngine.Object.FindObjectOfType<StoryPlayer>();
            if (player != null) return true;

            // 2. 没有则从 Resources 实例化全局 rig
            var prefab = Resources.Load<GameObject>(RigResourcePath);
            if (prefab == null)
            {
                return false;
            }
            var instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = prefab.name; // 去掉 (Clone) 后缀
            UnityEngine.Object.DontDestroyOnLoad(instance);
            player = instance.GetComponentInChildren<StoryPlayer>(true);
            return player != null;
        }

        /// <summary>
        /// Domain Reload 关闭时（Editor "Enter Play Mode Options"），
        /// 静态字段会跨 Play 周期残留。这里在 Play 开始前强制清空。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            player = null;
        }
    }
}
