using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3
{
    /// <summary>
    /// 显示/隐藏一组 Renderer。
    /// 用法：把脚本挂到「目标贴图所在节点」或其父节点上，留空 renderers 则自动收集自身 + 子物体所有 Renderer。
    /// 在 InteractTrigger.onInteracted 里直接选 Show()/Hide()/Toggle()/SetVisible(bool) 即可。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpriteVisibilityToggle : MonoBehaviour
    {
        [SerializeField, Tooltip("留空 = Awake 时自动收集自身 + 所有子物体的 Renderer。")]
        private List<Renderer> renderers = new List<Renderer>();

        [SerializeField, Tooltip("Awake 时强制隐藏一次，作为默认初始状态。")]
        private bool hideOnAwake = false;

        private void Awake()
        {
            if (renderers == null || renderers.Count == 0)
            {
                renderers = new List<Renderer>(GetComponentsInChildren<Renderer>(includeInactive: true));
            }
            if (hideOnAwake) SetVisible(false);
        }

        public void Show() => SetVisible(true);
        public void Hide() => SetVisible(false);
        public void Toggle() => SetVisible(!IsVisible());

        public void SetVisible(bool visible)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (r != null) r.enabled = visible;
            }
        }

        private bool IsVisible()
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (r != null) return r.enabled;
            }
            return false;
        }
    }
}
