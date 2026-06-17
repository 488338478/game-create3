using UnityEngine;

namespace GameCreate3
{
    /// <summary>
    /// 16:9 letterbox 计算工具。把"居中 16:9 区域"表达成相机 viewport（归一化）矩形：
    /// 屏幕比 16:9 更高（16:10 等）→ 上下留边；更宽（21:9 等）→ 左右留边。
    /// </summary>
    public static class AspectLetterbox
    {
        public const float TargetAspect = 16f / 9f;

        /// <summary>当前屏幕下，居中 16:9 区域的归一化 viewport（x,y,w,h ∈ [0,1]）。</summary>
        public static Rect Get16x9Box()
        {
            var h = Mathf.Max(1, Screen.height);
            var current = Screen.width / (float)h;
            if (current <= 0.0001f)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            if (current > TargetAspect)
            {
                // 更宽 → 左右留边（pillarbox）
                var w = TargetAspect / current;
                return new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }

            // 更高 → 上下留边（letterbox）
            var bh = current / TargetAspect;
            return new Rect(0f, (1f - bh) * 0.5f, 1f, bh);
        }

        /// <summary>把 box 嵌进 baseRect 的子矩形（用于分屏相机叠加 letterbox）。</summary>
        public static Rect Compose(Rect baseRect, Rect box)
        {
            return new Rect(
                baseRect.x + box.x * baseRect.width,
                baseRect.y + box.y * baseRect.height,
                box.width * baseRect.width,
                box.height * baseRect.height);
        }
    }
}
