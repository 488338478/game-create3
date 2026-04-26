using UnityEngine;

namespace GameCreate3.DualWorld
{
    public enum DualWorldScreenMode
    {
        RealityOnly,
        SplitDreamFocus,
        SplitRealityFocus
    }

    public sealed class DualWorldScreenLayout : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform realityPanel;

        public void Initialize(Camera camera, RectTransform panel)
        {
            worldCamera = camera;
            realityPanel = panel;
            Apply(DualWorldScreenMode.RealityOnly);
        }

        public void Apply(DualWorldScreenMode mode)
        {
            if (worldCamera != null)
            {
                worldCamera.enabled = true;
                worldCamera.rect = mode == DualWorldScreenMode.RealityOnly
                    ? new Rect(0f, 0f, 1f, 1f)
                    : new Rect(0f, 0f, 0.5f, 1f);
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

            Stretch(realityPanel, new Vector2(0.5f, 0f), Vector2.one, new Vector2(20f, 20f), new Vector2(-20f, -20f));
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
