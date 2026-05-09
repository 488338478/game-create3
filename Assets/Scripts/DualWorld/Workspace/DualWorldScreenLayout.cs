using UnityEngine;

namespace GameCreate3.DualWorld
{
    public enum DualWorldScreenMode
    {
        RealityOnly,
        SplitDreamFocus,
        SplitRealityFocus
    }

    /// <summary>
    /// 双世界两屏布局：左半屏放 UI 拼图（reality / 现实侧），右半屏放横板世界（dream / 梦境侧）。
    /// 通过设置 worldCamera.rect 让相机只在右半渲染；通过设置 realityPanel 锚点让 UI 容器贴左半。
    /// </summary>
    public sealed class DualWorldScreenLayout : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform realityPanel;

        public void Initialize(Camera camera, RectTransform panel, DualWorldScreenMode initialMode = DualWorldScreenMode.RealityOnly)
        {
            worldCamera = camera;
            realityPanel = panel;
            Apply(initialMode);
        }

        public void Apply(DualWorldScreenMode mode)
        {
            if (worldCamera != null)
            {
                worldCamera.enabled = true;
                worldCamera.rect = mode == DualWorldScreenMode.RealityOnly
                    ? new Rect(0f, 0f, 1f, 1f)
                    : new Rect(0.5f, 0f, 0.5f, 1f); // RIGHT half = dream/world
            }

            if (realityPanel == null)
            {
                return;
            }

            if (mode == DualWorldScreenMode.RealityOnly)
            {
                Stretch(realityPanel, Vector2.zero, Vector2.one, new Vector2(20f, 20f), new Vector2(-20f, -20f));
                return;
            }

            // LEFT half = reality / UI puzzle，零内边距：避免 viewport 不覆盖区域露出 GBuffer 残留。
            Stretch(realityPanel, Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }
    }
}
