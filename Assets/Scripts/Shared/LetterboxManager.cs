using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameCreate3
{
    /// <summary>
    /// 全局 16:9 letterbox：把所有全屏相机锁进居中 16:9 视口，并在 16:9 之外铺"可自定义纹理"的边条。
    ///
    /// 设计目标：16:10（或任意非 16:9）屏幕下，画面与 16:9 完全一致——不裁剪、不变形、排布关系不变，
    /// 多出来的空间用上下（或左右）边条填充。
    ///
    /// 部署：由 <see cref="RuntimeInitializeOnLoadMethod"/> 自动生成，**不需要在任何场景/prefab 里挂载**，
    /// 因此不改动任何序列化文件、不依赖脚本 GUID 绑定。
    ///
    /// 自定义边条纹理：把一张图片放到 <c>Assets/Resources/LetterboxBar.(png|jpg)</c> 即可（命名必须是 LetterboxBar）。
    /// 没有该资源时边条为纯黑。
    /// </summary>
    public sealed class LetterboxManager : MonoBehaviour
    {
        private const string BarTextureResource = "LetterboxBar";
        private const int BarSortingOrder = 30000; // 高于一切游戏内 Canvas
        private const int ConvertedUiSortingBase = 1000; // 转相机空间后抬到世界精灵之上
        private static readonly Rect FullRect = new Rect(0f, 0f, 1f, 1f);

        // 这些 Overlay Canvas 保持全屏，不收进 16:9 box：
        // 鼠标光标要能走到黑边里；letterbox 边条是我们自己的。
        private static readonly string[] KeepFullScreenNameContains = { "Cursor", "Letterbox" };

        private static LetterboxManager instance;

        private readonly Dictionary<Camera, Rect> designRects = new Dictionary<Camera, Rect>();
        private Canvas barCanvas;
        private RawImage barTop;
        private RawImage barBottom;
        private RawImage barLeft;
        private RawImage barRight;
        private Vector2Int lastScreen = new Vector2Int(-1, -1);
        private int convertScanCountdown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            var go = new GameObject("[LetterboxManager]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<LetterboxManager>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            convertScanCountdown = 0; // 新场景立即重扫，把 UI 收进 box
        }

        private void LateUpdate()
        {
            var box = AspectLetterbox.Get16x9Box();
            ApplyCameras(box);

            var screen = new Vector2Int(Screen.width, Screen.height);
            if (screen != lastScreen)
            {
                lastScreen = screen;
                EnsureBars();
                LayoutBars(box);
            }

            ConvertOverlayCanvasesThrottled();
        }

        /// <summary>
        /// 把游戏内的 Screen Space-Overlay Canvas 收进 16:9 box（改为 Screen Space-Camera，挂到已 letterbox 的主相机）。
        /// 这样 UI 按 1920×1080 设计像素级渲染在 box 内，与 dream 相机的 16:9 取景精确对齐——和原版 16:9 一模一样，外面是黑边。
        /// 幂等：转换后 renderMode 不再是 Overlay，重扫自动跳过。
        /// </summary>
        private void ConvertOverlayCanvasesThrottled()
        {
            if (convertScanCountdown-- > 0)
            {
                return;
            }

            convertScanCountdown = 10;

            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            var canvases = FindObjectsOfType<Canvas>(false);
            for (var i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas == null || canvas == barCanvas)
                {
                    continue;
                }

                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    continue;
                }

                if (NameKeepsFullScreen(canvas.gameObject.name))
                {
                    continue;
                }

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                canvas.sortingOrder += ConvertedUiSortingBase;
            }
        }

        private static bool NameKeepsFullScreen(string canvasName)
        {
            for (var i = 0; i < KeepFullScreenNameContains.Length; i++)
            {
                if (canvasName.IndexOf(KeepFullScreenNameContains[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyCameras(Rect box)
        {
            var cams = Camera.allCameras;
            for (var i = 0; i < cams.Length; i++)
            {
                var cam = cams[i];
                if (cam == null || cam.targetTexture != null)
                {
                    continue; // 渲染到 RenderTexture 的相机不动
                }

                if (cam.GetComponent<LetterboxIgnore>() != null)
                {
                    continue; // 交由其它系统管理
                }

                if (!designRects.TryGetValue(cam, out var design))
                {
                    // 只纳管"本就全屏"的相机；本身带子视口（分屏等）的相机交给它的所有者去 letterbox。
                    if (!Approximately(cam.rect, FullRect))
                    {
                        continue;
                    }

                    design = FullRect;
                    designRects[cam] = design;
                }

                cam.rect = AspectLetterbox.Compose(design, box);
            }
        }

        private void EnsureBars()
        {
            if (barCanvas != null)
            {
                return;
            }

            var canvasGo = new GameObject("LetterboxBars");
            canvasGo.transform.SetParent(transform, false);
            barCanvas = canvasGo.AddComponent<Canvas>();
            barCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            barCanvas.sortingOrder = BarSortingOrder;

            var tex = Resources.Load<Texture>(BarTextureResource);
            barTop = CreateBar("Top", tex);
            barBottom = CreateBar("Bottom", tex);
            barLeft = CreateBar("Left", tex);
            barRight = CreateBar("Right", tex);
        }

        private RawImage CreateBar(string barName, Texture tex)
        {
            var go = new GameObject(barName, typeof(RectTransform));
            go.transform.SetParent(barCanvas.transform, false);
            var img = go.AddComponent<RawImage>();
            img.raycastTarget = false;
            if (tex != null)
            {
                img.texture = tex;
                img.color = Color.white;
            }
            else
            {
                img.texture = null;
                img.color = Color.black;
            }

            return img;
        }

        private void LayoutBars(Rect box)
        {
            if (barCanvas == null)
            {
                return;
            }

            // 边条覆盖 16:9 box 之外的四周区域（退化为 0 尺寸时自动隐藏）。
            SetBar(barBottom, 0f, 0f, 1f, box.yMin);
            SetBar(barTop, 0f, box.yMax, 1f, 1f);
            SetBar(barLeft, 0f, 0f, box.xMin, 1f);
            SetBar(barRight, box.xMax, 0f, 1f, 1f);
        }

        private static void SetBar(RawImage img, float xMin, float yMin, float xMax, float yMax)
        {
            if (img == null)
            {
                return;
            }

            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            img.enabled = (xMax - xMin) > 0.0001f && (yMax - yMin) > 0.0001f;
        }

        private static bool Approximately(Rect a, Rect b)
        {
            return Mathf.Abs(a.x - b.x) < 0.001f
                && Mathf.Abs(a.y - b.y) < 0.001f
                && Mathf.Abs(a.width - b.width) < 0.001f
                && Mathf.Abs(a.height - b.height) < 0.001f;
        }
    }
}
