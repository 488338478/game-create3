using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class DreamColorHintRouter : MonoBehaviour
    {
        private static readonly int GrayscaleAmountId = Shader.PropertyToID("_GrayscaleAmount");
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

        [SerializeField] private SideScrollWorkspaceBase workspace;
        [SerializeField] private global::GameCreate3.DreamColorCollectController dreamCollector;
        [SerializeField] private global::GameCreate3.ColorPuzzleController colorPuzzle;
        [SerializeField] private RealityAlignmentTask mappingTask;
        [SerializeField] private bool disableLegacyPressEInteractions = true;
        [SerializeField] private Shader grayscaleShader;
        [SerializeField] [Range(0f, 1f)] private float mutedGrayscaleAmount = 1f;
        [SerializeField] [Range(0.25f, 1.5f)] private float mutedBrightness = 1f;
        [SerializeField] private float stateTransitionDurationSec = 0.18f;
        [SerializeField] private float pulseDurationSec = 0.42f;
        [SerializeField] private float pulseWhiteLift = 0.18f;
        [SerializeField] private float fallbackPulseSaturation = 0.72f;
        [SerializeField] private float fallbackPulseValue = 1f;

        private sealed class HintTargetState
        {
            public InteractTrigger trigger;
            public SpriteRenderer[] renderers = System.Array.Empty<SpriteRenderer>();
            public Color[] baseColors = System.Array.Empty<Color>();
            public Material[] baseMaterials = System.Array.Empty<Material>();
            public MaterialPropertyBlock[] propertyBlocks = System.Array.Empty<MaterialPropertyBlock>();
            public Coroutine pulseRoutine;
            public Coroutine transitionRoutine;
            public float currentGrayscaleAmount = 0f;
            public float currentFlashAmount = 0f;
            public Color currentFlashColor = Color.white;
        }

        private readonly Dictionary<string, HintTargetState> targetStatesByInteractId = new Dictionary<string, HintTargetState>();
        private readonly Dictionary<string, HashSet<int>> requiredBlocksByInteractId = new Dictionary<string, HashSet<int>>();
        private readonly List<string> configuredInteractIds = new List<string>();
        private readonly List<string> mappedInteractIds = new List<string>();
        private readonly List<ColorSlot> trackedColorSlots = new List<ColorSlot>();
        private readonly List<int> trackedBlockIndices = new List<int>();
        private readonly Dictionary<int, Color> accentColorCache = new Dictionary<int, Color>();
        private bool subscribed;
        private Material runtimeGrayscaleMaterial;
        private static readonly Color[] DefaultAccentPalette =
        {
            new Color(0.9764706f, 0.68235296f, 0.9490196f, 1f),
            new Color(1f, 0.70980394f, 0.7058824f, 1f),
            new Color(1f, 0.8235294f, 0.49411765f, 1f),
            new Color(1f, 0.9137255f, 0.49803922f, 1f),
            new Color(0.52156866f, 0.7882353f, 0.7607843f, 1f),
            new Color(0.5686275f, 0.7411765f, 0.93333334f, 1f),
            new Color(0.5647059f, 0.654902f, 0.9372549f, 1f),
            new Color(0.6784314f, 0.6117647f, 1f, 1f)
        };

        public void Initialize(
            SideScrollWorkspaceBase ownerWorkspace,
            global::GameCreate3.DreamColorCollectController collector,
            global::GameCreate3.ColorPuzzleController puzzle,
            RealityAlignmentTask alignmentTask)
        {
            UnsubscribeCollector();
            workspace = ownerWorkspace != null ? ownerWorkspace : workspace;
            dreamCollector = collector != null ? collector : dreamCollector;
            colorPuzzle = puzzle != null ? puzzle : colorPuzzle;
            mappingTask = alignmentTask != null ? alignmentTask : mappingTask;

            RebuildTargetCache();
            RebuildSlotSubscriptions();
            RebuildInteractBlockRequirements();
            DisableLegacyInteractions();
            SubscribeCollector();
            RefreshMutedState();
        }

        private void OnEnable()
        {
            SubscribeCollector();
        }

        private void OnDisable()
        {
            UnsubscribeCollector();
            UnsubscribeColorSlots();
            RefreshMutedState();
        }

        public void RefreshMutedState()
        {
            StopActivePulses();
            RefreshAllTargetResolvedVisuals(animated: false);
        }

        private void HandleItemCollected(global::GameCreate3.PaletteColorOption option)
        {
            // 新颜色进来，先把上一个颜色的脉冲关掉，所有物体回到褪色/恢复状态
            StopActivePulses();
            RefreshAllTargetResolvedVisuals(animated: false);

            if (!TryResolveBlockIndex(option, out var blockIndex))
            {
                return;
            }

            if (mappingTask == null)
            {
                return;
            }

            mappedInteractIds.Clear();
            mappingTask.GetInteractIdsForBlockIndex(blockIndex, mappedInteractIds);
            if (mappedInteractIds.Count == 0)
            {
                return;
            }

            var accentColor = ResolveAccentColor(option, blockIndex);
            for (var i = 0; i < mappedInteractIds.Count; i++)
            {
                if (!targetStatesByInteractId.TryGetValue(mappedInteractIds[i], out var state) || state == null)
                {
                    continue;
                }

                if (IsInteractTargetFullySolved(mappedInteractIds[i]))
                {
                    StartStateTransition(state, 0f, restoreOriginalMaterialOnComplete: true);
                    continue;
                }

                if (state.pulseRoutine != null)
                {
                    StopCoroutine(state.pulseRoutine);
                    state.pulseRoutine = null;
                }

                if (state.transitionRoutine != null)
                {
                    StopCoroutine(state.transitionRoutine);
                    state.transitionRoutine = null;
                }

                state.pulseRoutine = StartCoroutine(PulseTarget(state, accentColor));
            }
        }

        private void HandleColorSlotStateChanged(ColorSlot slot)
        {
            if (slot == null || !slot.TryGetBlockIndex(out var blockIndex))
            {
                return;
            }

            StopPulseForBlock(blockIndex);
            RefreshTargetsForBlock(blockIndex, animated: true);
        }

        private IEnumerator PulseTarget(HintTargetState state, Color accentColor)
        {
            var liftedColor = Color.Lerp(accentColor, Color.white, pulseWhiteLift);
            liftedColor.a = 1f;

            ApplyMutedVisual(state, mutedGrayscaleAmount, Color.white, 0f);

            var elapsed = 0f;
            while (true)
            {
                elapsed += Time.deltaTime;
                var normalized = (elapsed % pulseDurationSec) / Mathf.Max(0.01f, pulseDurationSec);
                var wave = Mathf.Sin(normalized * Mathf.PI);
                ApplyMutedVisual(state, mutedGrayscaleAmount, liftedColor, wave);

                yield return null;
            }
        }

        private void RebuildTargetCache()
        {
            StopActivePulses();
            targetStatesByInteractId.Clear();
            accentColorCache.Clear();

            if (workspace == null)
            {
                workspace = GetComponentInParent<SideScrollWorkspaceBase>(true);
            }

            if (mappingTask == null && workspace != null)
            {
                mappingTask = workspace.GetComponentInChildren<RealityAlignmentTask>(true);
            }

            if (workspace == null || mappingTask == null)
            {
                return;
            }

            configuredInteractIds.Clear();
            mappingTask.GetConfiguredInteractIds(configuredInteractIds);
            if (configuredInteractIds.Count == 0)
            {
                return;
            }

            var interactTriggers = workspace.GetComponentsInChildren<InteractTrigger>(true);
            for (var i = 0; i < interactTriggers.Length; i++)
            {
                var trigger = interactTriggers[i];
                if (trigger == null || string.IsNullOrWhiteSpace(trigger.InteractId))
                {
                    continue;
                }

                if (!configuredInteractIds.Contains(trigger.InteractId) || targetStatesByInteractId.ContainsKey(trigger.InteractId))
                {
                    continue;
                }

                var renderers = trigger.GetComponentsInChildren<SpriteRenderer>(true);
                if (renderers == null || renderers.Length == 0)
                {
                    continue;
                }

                var state = new HintTargetState
                {
                    trigger = trigger,
                    renderers = renderers,
                    baseColors = new Color[renderers.Length],
                    baseMaterials = new Material[renderers.Length],
                    propertyBlocks = new MaterialPropertyBlock[renderers.Length]
                };

                for (var r = 0; r < renderers.Length; r++)
                {
                    var renderer = renderers[r];
                    var baseColor = renderer != null ? renderer.color : Color.white;
                    state.baseColors[r] = baseColor;
                    state.baseMaterials[r] = renderer != null ? renderer.sharedMaterial : null;
                    state.propertyBlocks[r] = new MaterialPropertyBlock();
                }

                targetStatesByInteractId.Add(trigger.InteractId, state);
            }
        }

        private void RebuildSlotSubscriptions()
        {
            UnsubscribeColorSlots();
            trackedBlockIndices.Clear();

            if (colorPuzzle == null)
            {
                return;
            }

            trackedColorSlots.AddRange(colorPuzzle.GetComponentsInChildren<ColorSlot>(true));
            for (var i = 0; i < trackedColorSlots.Count; i++)
            {
                var slot = trackedColorSlots[i];
                if (slot == null)
                {
                    continue;
                }

                slot.StateChanged -= HandleColorSlotStateChanged;
                slot.StateChanged += HandleColorSlotStateChanged;

                if (slot.TryGetBlockIndex(out var blockIndex) && !trackedBlockIndices.Contains(blockIndex))
                {
                    trackedBlockIndices.Add(blockIndex);
                }
            }
        }

        private void UnsubscribeColorSlots()
        {
            for (var i = 0; i < trackedColorSlots.Count; i++)
            {
                var slot = trackedColorSlots[i];
                if (slot != null)
                {
                    slot.StateChanged -= HandleColorSlotStateChanged;
                }
            }

            trackedColorSlots.Clear();
            trackedBlockIndices.Clear();
        }

        private void RebuildInteractBlockRequirements()
        {
            requiredBlocksByInteractId.Clear();

            if (mappingTask == null)
            {
                return;
            }

            for (var i = 0; i < trackedBlockIndices.Count; i++)
            {
                var blockIndex = trackedBlockIndices[i];
                mappedInteractIds.Clear();
                mappingTask.GetInteractIdsForBlockIndex(blockIndex, mappedInteractIds);

                for (var j = 0; j < mappedInteractIds.Count; j++)
                {
                    var interactId = mappedInteractIds[j];
                    if (string.IsNullOrWhiteSpace(interactId))
                    {
                        continue;
                    }

                    if (!requiredBlocksByInteractId.TryGetValue(interactId, out var blockSet))
                    {
                        blockSet = new HashSet<int>();
                        requiredBlocksByInteractId.Add(interactId, blockSet);
                    }

                    blockSet.Add(blockIndex);
                }
            }
        }

        private void DisableLegacyInteractions()
        {
            if (!disableLegacyPressEInteractions || workspace == null)
            {
                return;
            }

            var interactTriggers = workspace.GetComponentsInChildren<InteractTrigger>(true);
            for (var i = 0; i < interactTriggers.Length; i++)
            {
                var trigger = interactTriggers[i];
                if (trigger != null)
                {
                    trigger.enabled = false;
                }
            }
        }

        private void SubscribeCollector()
        {
            if (subscribed || dreamCollector == null)
            {
                return;
            }

            dreamCollector.ItemCollected -= HandleItemCollected;
            dreamCollector.ItemCollected += HandleItemCollected;
            subscribed = true;
        }

        private void UnsubscribeCollector()
        {
            if (!subscribed || dreamCollector == null)
            {
                subscribed = false;
                return;
            }

            dreamCollector.ItemCollected -= HandleItemCollected;
            subscribed = false;
        }

        private bool TryResolveBlockIndex(global::GameCreate3.PaletteColorOption option, out int blockIndex)
        {
            blockIndex = -1;

            if (colorPuzzle != null && colorPuzzle.TryResolveBlockIndex(option, out blockIndex))
            {
                return true;
            }

            Debug.LogError($"[DreamColorHintRouter] TryResolveBlockIndex 失败：ColorPuzzleController 未找到匹配 variantId={option.variantId} colorId={option.colorId} 的 ColorSlot", this);
            return false;
        }

        private Color ResolveAccentColor(global::GameCreate3.PaletteColorOption option, int blockIndex)
        {
            var spriteId = option.paletteSprite != null ? option.paletteSprite.GetInstanceID() : 0;
            if (spriteId != 0 && accentColorCache.TryGetValue(spriteId, out var cachedColor))
            {
                return cachedColor;
            }

            Color resolvedColor;
            if (IsUsableFallbackColor(option.fallbackColor))
            {
                resolvedColor = option.fallbackColor;
            }
            else if (TrySampleSpriteColor(option.paletteSprite, out var sampledColor))
            {
                resolvedColor = sampledColor;
            }
            else
            {
                resolvedColor = ResolveDefaultAccentColor(option, blockIndex);
            }

            resolvedColor.a = 1f;
            if (spriteId != 0)
            {
                accentColorCache[spriteId] = resolvedColor;
            }

            return resolvedColor;
        }

        private Color ResolveDefaultAccentColor(global::GameCreate3.PaletteColorOption option, int blockIndex)
        {
            var paletteIndex = -1;
            if (option.variantId > 0)
            {
                paletteIndex = option.variantId - 1;
            }
            else if (int.TryParse(option.colorId, out var parsedColorId) && parsedColorId > 0)
            {
                paletteIndex = parsedColorId - 1;
            }
            else if (blockIndex >= 0)
            {
                paletteIndex = blockIndex;
            }

            if (paletteIndex >= 0 && paletteIndex < DefaultAccentPalette.Length)
            {
                return DefaultAccentPalette[paletteIndex];
            }

            var hueIndex = Mathf.Max(0, blockIndex);
            return Color.HSVToRGB(
                Mathf.Repeat(0.08f + hueIndex * 0.137f, 1f),
                fallbackPulseSaturation,
                fallbackPulseValue);
        }

        private static bool IsUsableFallbackColor(Color candidate)
        {
            if (candidate.a <= 0.001f || candidate.maxColorComponent <= 0.001f)
            {
                return false;
            }

            return !(candidate.r >= 0.97f && candidate.g >= 0.97f && candidate.b >= 0.97f);
        }

        private static bool TrySampleSpriteColor(Sprite sprite, out Color sampledColor)
        {
            sampledColor = default;
            if (sprite == null)
            {
                return false;
            }

            var texture = sprite.texture;
            if (texture == null || !texture.isReadable)
            {
                return false;
            }

            var rect = sprite.rect;
            var startX = Mathf.RoundToInt(rect.xMin);
            var endX = Mathf.RoundToInt(rect.xMax);
            var startY = Mathf.RoundToInt(rect.yMin);
            var endY = Mathf.RoundToInt(rect.yMax);
            var stepX = Mathf.Max(1, Mathf.RoundToInt(rect.width / 8f));
            var stepY = Mathf.Max(1, Mathf.RoundToInt(rect.height / 8f));

            var weightedSum = Vector4.zero;
            var alphaWeight = 0f;

            for (var y = startY; y < endY; y += stepY)
            {
                for (var x = startX; x < endX; x += stepX)
                {
                    var pixel = texture.GetPixel(x, y);
                    if (pixel.a <= 0.05f)
                    {
                        continue;
                    }

                    weightedSum += (Vector4)pixel * pixel.a;
                    alphaWeight += pixel.a;
                }
            }

            if (alphaWeight <= 0.001f)
            {
                return false;
            }

            sampledColor = weightedSum / alphaWeight;
            sampledColor.a = 1f;
            return true;
        }

        private bool IsBlockSolved(int blockIndex)
        {
            for (var i = 0; i < trackedColorSlots.Count; i++)
            {
                var slot = trackedColorSlots[i];
                if (slot == null || !slot.IsCorrectColor() || !slot.TryGetBlockIndex(out var slotBlockIndex))
                {
                    continue;
                }

                if (slotBlockIndex == blockIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsInteractTargetFullySolved(string interactId)
        {
            if (string.IsNullOrWhiteSpace(interactId))
            {
                return false;
            }

            if (!requiredBlocksByInteractId.TryGetValue(interactId, out var requiredBlocks) || requiredBlocks == null || requiredBlocks.Count == 0)
            {
                return false;
            }

            foreach (var blockIndex in requiredBlocks)
            {
                if (!IsBlockSolved(blockIndex))
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyBlockSolvedVisual(int blockIndex, bool animated = false)
        {
            ResolveBlockTargets(blockIndex);
            for (var i = 0; i < mappedInteractIds.Count; i++)
            {
                var interactId = mappedInteractIds[i];
                if (!IsInteractTargetFullySolved(interactId))
                {
                    ApplyInteractMutedVisual(interactId, animated);
                    continue;
                }

                if (targetStatesByInteractId.TryGetValue(interactId, out var state) && state != null)
                {
                    if (animated)
                    {
                        StartStateTransition(state, 0f, restoreOriginalMaterialOnComplete: true);
                    }
                    else
                    {
                        RestoreOriginalVisual(state);
                    }
                }
            }
        }

        private void ApplyBlockMutedVisual(int blockIndex, bool animated = false)
        {
            ResolveBlockTargets(blockIndex);
            for (var i = 0; i < mappedInteractIds.Count; i++)
            {
                var interactId = mappedInteractIds[i];
                if (IsInteractTargetFullySolved(interactId))
                {
                    ApplyInteractSolvedVisual(interactId, animated);
                    continue;
                }

                if (targetStatesByInteractId.TryGetValue(interactId, out var state) && state != null)
                {
                    if (animated)
                    {
                        StartStateTransition(state, mutedGrayscaleAmount, restoreOriginalMaterialOnComplete: false);
                    }
                    else
                    {
                        ApplyMutedVisual(state, mutedGrayscaleAmount, Color.white, 0f);
                    }
                }
            }
        }

        private void RefreshTargetsForBlock(int blockIndex, bool animated)
        {
            ResolveBlockTargets(blockIndex);
            for (var i = 0; i < mappedInteractIds.Count; i++)
            {
                var interactId = mappedInteractIds[i];
                if (IsInteractTargetFullySolved(interactId))
                {
                    ApplyInteractSolvedVisual(interactId, animated);
                }
                else
                {
                    ApplyInteractMutedVisual(interactId, animated);
                }
            }
        }

        private void RefreshAllTargetResolvedVisuals(bool animated)
        {
            foreach (var entry in targetStatesByInteractId)
            {
                if (IsInteractTargetFullySolved(entry.Key))
                {
                    ApplyInteractSolvedVisual(entry.Key, animated);
                }
                else
                {
                    ApplyInteractMutedVisual(entry.Key, animated);
                }
            }
        }

        private void ApplyInteractSolvedVisual(string interactId, bool animated)
        {
            if (!targetStatesByInteractId.TryGetValue(interactId, out var state) || state == null)
            {
                return;
            }

            if (animated)
            {
                StartStateTransition(state, 0f, restoreOriginalMaterialOnComplete: true);
            }
            else
            {
                RestoreOriginalVisual(state);
            }
        }

        private void ApplyInteractMutedVisual(string interactId, bool animated)
        {
            if (!targetStatesByInteractId.TryGetValue(interactId, out var state) || state == null)
            {
                return;
            }

            if (animated)
            {
                StartStateTransition(state, mutedGrayscaleAmount, restoreOriginalMaterialOnComplete: false);
            }
            else
            {
                ApplyMutedVisual(state, mutedGrayscaleAmount, Color.white, 0f);
            }
        }

        private void StopPulseForBlock(int blockIndex)
        {
            ResolveBlockTargets(blockIndex);
            for (var i = 0; i < mappedInteractIds.Count; i++)
            {
                if (!targetStatesByInteractId.TryGetValue(mappedInteractIds[i], out var state) || state == null || state.pulseRoutine == null)
                {
                    continue;
                }

                StopCoroutine(state.pulseRoutine);
                state.pulseRoutine = null;

                if (state.transitionRoutine != null)
                {
                    StopCoroutine(state.transitionRoutine);
                    state.transitionRoutine = null;
                }
            }
        }

        private void StartStateTransition(HintTargetState state, float targetGrayscaleAmount, bool restoreOriginalMaterialOnComplete)
        {
            if (state == null)
            {
                return;
            }

            if (state.transitionRoutine != null)
            {
                StopCoroutine(state.transitionRoutine);
                state.transitionRoutine = null;
            }

            if (!isActiveAndEnabled || stateTransitionDurationSec <= 0f)
            {
                if (restoreOriginalMaterialOnComplete && targetGrayscaleAmount <= 0.001f)
                {
                    RestoreOriginalVisual(state);
                }
                else
                {
                    ApplyMutedVisual(state, targetGrayscaleAmount, Color.white, 0f);
                }
                return;
            }

            state.transitionRoutine = StartCoroutine(TransitionState(state, targetGrayscaleAmount, restoreOriginalMaterialOnComplete));
        }

        private IEnumerator TransitionState(HintTargetState state, float targetGrayscaleAmount, bool restoreOriginalMaterialOnComplete)
        {
            var startGrayscaleAmount = state.currentGrayscaleAmount;
            var startFlashAmount = state.currentFlashAmount;
            var startFlashColor = state.currentFlashColor;

            ApplyMutedVisual(state, startGrayscaleAmount, startFlashColor, startFlashAmount);

            var elapsed = 0f;
            while (elapsed < stateTransitionDurationSec)
            {
                elapsed += Time.deltaTime;
                var normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, stateTransitionDurationSec));
                var smooth = Mathf.SmoothStep(0f, 1f, normalized);
                ApplyMutedVisual(
                    state,
                    Mathf.Lerp(startGrayscaleAmount, targetGrayscaleAmount, smooth),
                    startFlashColor,
                    Mathf.Lerp(startFlashAmount, 0f, smooth));

                yield return null;
            }

            if (restoreOriginalMaterialOnComplete && targetGrayscaleAmount <= 0.001f)
            {
                RestoreOriginalVisual(state);
            }
            else
            {
                ApplyMutedVisual(state, targetGrayscaleAmount, Color.white, 0f);
            }

            state.transitionRoutine = null;
        }

        private void ResolveBlockTargets(int blockIndex)
        {
            mappedInteractIds.Clear();
            if (mappingTask != null)
            {
                mappingTask.GetInteractIdsForBlockIndex(blockIndex, mappedInteractIds);
            }
        }

        private void StopActivePulses()
        {
            foreach (var entry in targetStatesByInteractId)
            {
                var state = entry.Value;
                if (state == null)
                {
                    continue;
                }

                if (state.pulseRoutine != null)
                {
                    StopCoroutine(state.pulseRoutine);
                    state.pulseRoutine = null;
                }

                if (state.transitionRoutine != null)
                {
                    StopCoroutine(state.transitionRoutine);
                    state.transitionRoutine = null;
                }
            }
        }

        private void ApplyAllBlocksMutedVisual(bool animated)
        {
            RefreshAllTargetResolvedVisuals(animated);
        }

        private void ApplyMutedVisual(HintTargetState state, float grayscaleAmount, Color flashColor, float flashAmount)
        {
            if (state == null || !EnsureRuntimeGrayscaleMaterial())
            {
                return;
            }

            state.currentGrayscaleAmount = grayscaleAmount;
            state.currentFlashColor = flashColor;
            state.currentFlashAmount = flashAmount;

            var resolvedBrightness = Mathf.Lerp(1f, mutedBrightness, Mathf.Clamp01(grayscaleAmount));
            for (var i = 0; i < state.renderers.Length; i++)
            {
                var renderer = state.renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (renderer.sharedMaterial != runtimeGrayscaleMaterial)
                {
                    renderer.sharedMaterial = runtimeGrayscaleMaterial;
                }

                var block = state.propertyBlocks[i] ?? new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetFloat(GrayscaleAmountId, grayscaleAmount);
                block.SetFloat(BrightnessId, resolvedBrightness);
                block.SetColor(FlashColorId, flashColor);
                block.SetFloat(FlashAmountId, flashAmount);
                renderer.SetPropertyBlock(block);
                state.propertyBlocks[i] = block;
            }
        }

        private void RestoreOriginalVisual(HintTargetState state)
        {
            if (state == null)
            {
                return;
            }

            state.currentGrayscaleAmount = 0f;
            state.currentFlashAmount = 0f;
            state.currentFlashColor = Color.white;

            for (var i = 0; i < state.renderers.Length; i++)
            {
                var renderer = state.renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.sharedMaterial = i < state.baseMaterials.Length ? state.baseMaterials[i] : null;
                renderer.SetPropertyBlock(null);
            }
        }

        private bool EnsureRuntimeGrayscaleMaterial()
        {
            if (runtimeGrayscaleMaterial != null)
            {
                return true;
            }

            if (grayscaleShader == null)
            {
                grayscaleShader = Shader.Find("Game/Sprites/GrayscaleTint");
            }

            if (grayscaleShader == null)
            {
                Debug.LogWarning("[DreamColorHintRouter] Shader 'Game/Sprites/GrayscaleTint' 未找到，无法应用左侧黑白褪色效果。", this);
                return false;
            }

            runtimeGrayscaleMaterial = new Material(grayscaleShader)
            {
                name = "RuntimeDreamMutedGrayscale"
            };
            runtimeGrayscaleMaterial.hideFlags = HideFlags.DontSave;
            return true;
        }

        private void OnDestroy()
        {
            foreach (var entry in targetStatesByInteractId)
            {
                RestoreOriginalVisual(entry.Value);
            }

            if (runtimeGrayscaleMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(runtimeGrayscaleMaterial);
            }
            else
            {
                DestroyImmediate(runtimeGrayscaleMaterial);
            }
        }
    }
}
