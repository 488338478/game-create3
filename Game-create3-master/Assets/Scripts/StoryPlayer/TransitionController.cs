using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.StoryPlayer
{
    public sealed class TransitionController : MonoBehaviour, ITransitionController
    {
        [Header("Components")]
        [SerializeField] private Canvas transitionCanvas;
        [SerializeField] private RawImage transitionImage;
        [SerializeField] private Material transitionMaterial;

        [Header("Settings")]
        [SerializeField] private int defaultResolution = 1080;
        [SerializeField] private bool useUnscaledTime = true;

        private bool isTransitioning;
        private CancellationTokenSource transitionCts;
        private RenderTexture renderTexture;
        private Material runtimeMaterial;
        private float currentProgress;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Initialize()
        {
            if (transitionCanvas == null)
            {
                transitionCanvas = GetComponent<Canvas>();
            }

            if (transitionCanvas != null)
            {
                transitionCanvas.sortingOrder = 1000;
                transitionCanvas.gameObject.SetActive(false);
            }

            CreateRenderTexture();
            CreateRuntimeMaterial();
        }

        private void Cleanup()
        {
            transitionCts?.Cancel();
            transitionCts?.Dispose();
            transitionCts = null;

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }

            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
                runtimeMaterial = null;
            }
        }

        private void CreateRenderTexture()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }

            var width = Screen.width;
            var height = Screen.height;
            renderTexture = new RenderTexture(width, height, 24);
            renderTexture.Create();

            if (transitionImage != null)
            {
                transitionImage.texture = renderTexture;
            }
        }

        private void CreateRuntimeMaterial()
        {
            if (transitionMaterial != null)
            {
                runtimeMaterial = new Material(transitionMaterial);
            }
            else
            {
                runtimeMaterial = new Material(Shader.Find("UI/Default"));
            }

            if (transitionImage != null)
            {
                transitionImage.material = runtimeMaterial;
            }
        }

        public async Task PlayTransitionAsync(StoryTransitionType transitionType, float duration, bool isIn)
        {
            if (transitionType == StoryTransitionType.None || duration <= 0)
            {
                return;
            }

            transitionCts?.Cancel();
            transitionCts?.Dispose();
            transitionCts = new CancellationTokenSource();

            isTransitioning = true;
            currentProgress = 0f;

            try
            {
                EnableTransitionCanvas();
                SetupTransitionMaterial(transitionType, isIn);

                await AnimateTransitionAsync(duration, transitionCts.Token);

                if (!isIn)
                {
                    DisableTransitionCanvas();
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[TransitionController] Transition cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TransitionController] Transition error: {ex.Message}");
            }
            finally
            {
                isTransitioning = false;
            }
        }

        public void SkipCurrentTransition()
        {
            if (!isTransitioning)
            {
                return;
            }

            transitionCts?.Cancel();
            currentProgress = 1f;

            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetFloat("_Progress", 1f);
            }

            DisableTransitionCanvas();
            isTransitioning = false;
        }

        private void EnableTransitionCanvas()
        {
            if (transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(true);
            }

            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetFloat("_Progress", 0f);
            }
        }

        private void DisableTransitionCanvas()
        {
            if (transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(false);
            }
        }

        private void SetupTransitionMaterial(StoryTransitionType transitionType, bool isIn)
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            var shaderName = GetShaderNameForTransition(transitionType);
            var shader = Shader.Find(shaderName);

            if (shader != null)
            {
                runtimeMaterial.shader = shader;
            }

            runtimeMaterial.SetFloat("_Direction", isIn ? 0f : 1f);
            runtimeMaterial.SetFloat("_Progress", 0f);
            runtimeMaterial.SetColor("_Color", Color.black);
        }

        private string GetShaderNameForTransition(StoryTransitionType transitionType)
        {
            return transitionType switch
            {
                StoryTransitionType.Fade => "Transitions/Fade",
                StoryTransitionType.CrossFade => "Transitions/CrossFade",
                StoryTransitionType.SlideLeft => "Transitions/Slide",
                StoryTransitionType.SlideRight => "Transitions/Slide",
                StoryTransitionType.SlideUp => "Transitions/Slide",
                StoryTransitionType.SlideDown => "Transitions/Slide",
                StoryTransitionType.Scale => "Transitions/Scale",
                _ => "UI/Default"
            };
        }

        private async Task AnimateTransitionAsync(float duration, CancellationToken ct)
        {
            var elapsed = 0f;

            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                currentProgress = Mathf.Clamp01(elapsed / duration);

                if (runtimeMaterial != null)
                {
                    runtimeMaterial.SetFloat("_Progress", currentProgress);
                }

                await Task.Yield();
            }

            currentProgress = 1f;

            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetFloat("_Progress", 1f);
            }
        }

        public void CaptureScreen(Action<RenderTexture> onCaptureComplete)
        {
            if (renderTexture == null)
            {
                CreateRenderTexture();
            }

            ScreenCapture.CaptureScreenshotIntoRenderTexture(renderTexture);
            onCaptureComplete?.Invoke(renderTexture);
        }

        public void SetTransitionColor(Color color)
        {
            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetColor("_Color", color);
            }
        }

        public void SetTransitionTexture(Texture texture)
        {
            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetTexture("_TransitionTex", texture);
            }
        }
    }
}
