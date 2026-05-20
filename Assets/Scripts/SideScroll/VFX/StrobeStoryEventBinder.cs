using System.Collections.Generic;
using GameCreate3.StoryPlayer;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameCreate3.SideScroll.VFX
{
    /// <summary>
    /// 将 StoryEventSystem 的静态全局事件 OnPostProcessEffectRequested 桥接到 StrobeEffectController。
    ///
    /// StoryEventSystem 由 StoryPlayer 动态 AddComponent 生成，所以本组件不依赖跟它同 GameObject。
    /// 推荐挂在 [StrobeController] 这种持久 GameObject 上（开启 DontDestroyOnLoad）。
    ///
    /// EventData 字符串格式：
    ///   "PresetName"                          — 用 Resources/VFX/Strobe/PresetName.asset 预设
    ///   "PresetName?intensity=0.5"            — 预设 + 覆盖参数
    ///   "?duration=1&intensity=0.8&noise=0.5" — 完全内联（无预设）
    ///   "stop"                                — 立即停止当前效果
    ///   "fadeout=0.5"                         — 渐隐停止
    /// </summary>
    public sealed class StrobeStoryEventBinder : MonoBehaviour
    {
        [Tooltip("挂在持久 GameObject 上时建议勾选 — 避免重复订阅。")]
        [SerializeField] private bool unsubscribeOnDisable = true;

        private bool isSubscribed;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (unsubscribeOnDisable) Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (isSubscribed) return;
            Debug.Log("[StrobeStoryEventBinder] 订阅 StoryEventSystem.OnPostProcessEffectRequested。");
            StoryEventSystem.OnPostProcessEffectRequested += HandleEffect;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed) return;
            Debug.Log("[StrobeStoryEventBinder] 取消订阅 StoryEventSystem.OnPostProcessEffectRequested。");
            StoryEventSystem.OnPostProcessEffectRequested -= HandleEffect;
            isSubscribed = false;
        }

        private void HandleEffect(string eventData)
        {
            if (string.IsNullOrEmpty(eventData)) return;

            var controller = StrobeEffectController.Instance;
            if (controller == null)
            {
                Debug.LogWarning("[StrobeStoryEventBinder] 场景内无 StrobeEffectController。");
                return;
            }

            if (eventData.Equals("stop", System.StringComparison.OrdinalIgnoreCase))
            {
                controller.Stop();
                return;
            }

            if (eventData.StartsWith("fadeout=", System.StringComparison.OrdinalIgnoreCase))
            {
                if (float.TryParse(eventData.Substring(8), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d))
                    controller.FadeOut(d);
                else
                    controller.Stop();
                return;
            }

            string presetName;
            Dictionary<string, string> overrides = null;
            int qIdx = eventData.IndexOf('?');

            if (qIdx >= 0)
            {
                presetName = eventData.Substring(0, qIdx);
                overrides  = ParseQuery(eventData.Substring(qIdx + 1));
            }
            else
            {
                presetName = eventData;
            }

            StrobeConfig preset = null;
            if (!string.IsNullOrEmpty(presetName))
            {
                preset = Resources.Load<StrobeConfig>($"VFX/Strobe/{presetName}");
                if (preset == null)
                {
                    Debug.LogWarning($"[StrobeStoryEventBinder] 预设未找到：Resources/VFX/Strobe/{presetName}.asset");
                }
            }

            if (preset == null && (overrides == null || overrides.Count == 0)) return;

            controller.Play(preset, overrides);
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var dict = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(query)) return dict;
            foreach (var part in query.Split('&'))
            {
                int eq = part.IndexOf('=');
                if (eq <= 0) continue;
                dict[part.Substring(0, eq).Trim()] = part.Substring(eq + 1).Trim();
            }
            return dict;
        }
    }
}
