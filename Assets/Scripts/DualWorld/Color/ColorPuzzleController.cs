using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCreate3
{
    public sealed class ColorPuzzleController : MonoBehaviour
    {
        private readonly string[] defaultRejectLines =
        {
            "写的什么玩意，审美呢？",
            "211就这水平？",
            "你是老太太吗，年轻点！"
        };

        private readonly string[] defaultApproveLines =
        {
            "还得是高材生，可以可以。"
        };

        [SerializeField] private CanvasGroup interactionGroup;
        [SerializeField] private Text hintLabel;
        [SerializeField] private List<ColorSlot> colorSlots = new List<ColorSlot>();
        [SerializeField] private CanvasGroup dreamPaletteGroup;
        [SerializeField] private ColorPaletteSwatch currentPaletteSwatch;
        [SerializeField] private bool forceFirstFailure = true;
        [SerializeField] private float disabledAlpha = 0.42f;
        [SerializeField] private string initialHintLine = "在左侧梦境中通过跳跃拾取颜色，请在右侧将颜色正确地填充。";
        [SerializeField] private string dreamPaletteHintLine = "接到哪种 ID 的颜色，右侧色板就切到哪一版，再点击对应组件替换成同 ID 的样式。";
        [SerializeField] private string successSummaryFormat = "这版顺眼多了，{0} / {1} 个组件都换对了。";
        [SerializeField] private List<string> rejectLines = new List<string>();
        [SerializeField] private List<string> approveLines = new List<string>();

        private bool dreamPaletteEnabled;
        private bool interactable;
        private bool forcedFailurePending = true;
        private int rejectLineIndex;
        private int approveLineIndex;
        private bool hasCurrentPalette;
        private PaletteColorOption currentPaletteOption;

        public bool IsInteractable => interactable;

        public event Action<ColorSubmitResult> OnSubmitAttempted;
        public event Action<ColorSlot> SlotStateChanged;

        /// <summary>供 Flow 层（ChatBox 管道）调用，触发一次提交评估</summary>
        public void Submit()
        {
            if (!interactable) return;
            var result = TrySubmit();
            OnSubmitAttempted?.Invoke(result);
        }

        public void Initialize(
            CanvasGroup canvasGroup,
            Text hint,
            List<ColorSlot> slots)
        {
            interactionGroup = canvasGroup;
            hintLabel = hint;
            colorSlots = slots ?? new List<ColorSlot>();

            foreach (var slot in colorSlots)
            {
                slot.SetDreamPaletteEnabled(false);
                slot.SetInteractable(true);
                slot.Clicked -= HandleColorSlotClicked;
                slot.Clicked += HandleColorSlotClicked;
                slot.StateChanged -= HandleColorSlotStateChanged;
                slot.StateChanged += HandleColorSlotStateChanged;
            }
        }

        public void RuntimeConfigure(
            CanvasGroup canvasGroup,
            Text hint,
            List<ColorSlot> slots)
        {
            Initialize(canvasGroup, hint, slots);
        }

        private void Awake()
        {
            if (colorSlots.Count == 0)
                colorSlots.AddRange(GetComponentsInChildren<ColorSlot>(true));

            if (currentPaletteSwatch == null)
                currentPaletteSwatch = GetComponentInChildren<ColorPaletteSwatch>(true);

            if (dreamPaletteGroup == null && currentPaletteSwatch != null)
                dreamPaletteGroup = currentPaletteSwatch.GetComponentInParent<CanvasGroup>();

            EnsureFeedbackLineFallbacks();
            forcedFailurePending = forceFirstFailure;
            AutoConfigureRuntimeUi();
        }

        private void OnEnable()
        {
            BindRuntimeEvents();
            SetDreamPaletteEnabled(false);
            SetInteractable(true);
        }

        private void OnDisable()
        {
            UnbindRuntimeEvents();
        }

        public void SetDreamPaletteEnabled(bool enabled)
        {
            dreamPaletteEnabled = enabled;

            foreach (var slot in colorSlots)
                slot.SetDreamPaletteEnabled(enabled);

            RefreshDreamPaletteArea();

            if (hintLabel != null)
                hintLabel.text = enabled ? dreamPaletteHintLine : initialHintLine;
        }

        public void SetDreamPaletteColors(IReadOnlyList<PaletteColorOption> options)
        {
            if (options == null || options.Count == 0)
            {
                ClearCurrentPalette();
                return;
            }
            SetCurrentPalette(options[options.Count - 1]);
        }

        public void SetCurrentPalette(PaletteColorOption option)
        {
            currentPaletteOption = option;
            hasCurrentPalette = option.IsValid || option.paletteSprite != null;
            RefreshDreamPaletteArea();
        }

        public void ClearCurrentPalette()
        {
            currentPaletteOption = default;
            hasCurrentPalette = false;
            RefreshDreamPaletteArea();
        }

        public void FlashTargetsForOption(PaletteColorOption option)
        {
            if (!option.IsValid)
            {
                return;
            }

            // 先停掉所有色槽的旧脉冲
            for (var i = 0; i < colorSlots.Count; i++)
            {
                colorSlots[i]?.StopApplyTargetPulses();
            }

            for (var i = 0; i < colorSlots.Count; i++)
            {
                var slot = colorSlots[i];
                if (slot != null && slot.MatchesOption(option))
                {
                    slot.PlayHintPulse(option);
                }
            }
        }

        public bool TryResolveBlockIndex(PaletteColorOption option, out int blockIndex)
        {
            blockIndex = -1;
            if (!option.IsValid) return false;

            for (var i = 0; i < colorSlots.Count; i++)
            {
                var slot = colorSlots[i];
                if (slot == null || !slot.MatchesOption(option)) continue;
                if (slot.TryGetBlockIndex(out blockIndex)) return true;
            }
            return false;
        }

        public bool IsOptionSolved(PaletteColorOption option)
        {
            if (!option.IsValid)
            {
                return false;
            }

            for (var i = 0; i < colorSlots.Count; i++)
            {
                var slot = colorSlots[i];
                if (slot == null || !slot.MatchesOption(option) || !slot.IsCorrectColor())
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        public bool HasAnyAppliedColor()
        {
            for (var i = 0; i < colorSlots.Count; i++)
            {
                var slot = colorSlots[i];
                if (slot != null && slot.HasAppliedOption)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAutoFillOption(PaletteColorOption option)
        {
            if (!option.IsValid && option.paletteSprite == null)
            {
                return false;
            }

            for (var i = 0; i < colorSlots.Count; i++)
            {
                var slot = colorSlots[i];
                if (slot == null || !slot.MatchesOption(option) || slot.IsCorrectColor())
                {
                    continue;
                }

                slot.ApplyPaletteColor(option);
                return true;
            }

            return false;
        }

        public void SetInteractable(bool enabled)
        {
            interactable = enabled;

            if (interactionGroup != null)
            {
                interactionGroup.interactable = enabled;
                interactionGroup.blocksRaycasts = enabled;
                interactionGroup.alpha = enabled ? 1f : disabledAlpha;
            }

            foreach (var slot in colorSlots)
                slot.SetInteractable(enabled);

            RefreshDreamPaletteArea();
        }

        public ColorSubmitResult TrySubmit()
        {
            var totalCount = colorSlots.Count;
            var correctCount = 0;

            foreach (var slot in colorSlots)
                if (slot.IsCorrectColor()) correctCount++;

            if (!dreamPaletteEnabled && forcedFailurePending)
            {
                forcedFailurePending = false;
                return new ColorSubmitResult(false, correctCount, totalCount, NextRejectLine());
            }

            var success = correctCount >= totalCount;
            return new ColorSubmitResult(success, correctCount, totalCount,
                success ? NextApproveLine() : NextRejectLine());
        }

        public void ResetPuzzle()
        {
            forcedFailurePending = forceFirstFailure;
            rejectLineIndex = 0;
            approveLineIndex = 0;
            ClearCurrentPalette();
            SetDreamPaletteEnabled(false);
            SetInteractable(true);

            foreach (var slot in colorSlots)
                slot.ResetSlot();

            RefreshDreamPaletteArea();
        }

        private string NextRejectLine()
        {
            var line = rejectLines[rejectLineIndex];
            rejectLineIndex = (rejectLineIndex + 1) % rejectLines.Count;
            return line;
        }

        private string NextApproveLine()
        {
            var line = approveLines[approveLineIndex];
            approveLineIndex = (approveLineIndex + 1) % approveLines.Count;
            return line;
        }

        private void HandleColorSlotClicked(ColorSlot slot)
        {
            if (!dreamPaletteEnabled)
            {
                return;
            }
            if (slot == null)
            {
                return;
            }
            if (!hasCurrentPalette)
            {
                return;
            }
            slot.ApplyPaletteColor(currentPaletteOption);
        }

        private void HandleColorSlotStateChanged(ColorSlot slot)
        {
            SlotStateChanged?.Invoke(slot);
        }

        private void RefreshDreamPaletteArea()
        {
            if (dreamPaletteGroup != null)
            {
                dreamPaletteGroup.gameObject.SetActive(dreamPaletteEnabled);
                dreamPaletteGroup.alpha = dreamPaletteEnabled ? 1f : 0f;
                dreamPaletteGroup.interactable = false;
                dreamPaletteGroup.blocksRaycasts = false;
            }

            if (currentPaletteSwatch != null)
            {
                currentPaletteSwatch.Configure(0, currentPaletteOption, hasCurrentPalette, false);
                currentPaletteSwatch.SetSelected(false);
                currentPaletteSwatch.SetVisible(dreamPaletteEnabled);
            }
        }

        private void EnsureFeedbackLineFallbacks()
        {
            if (rejectLines == null) rejectLines = new List<string>();
            if (approveLines == null) approveLines = new List<string>();
            if (rejectLines.Count == 0) rejectLines.AddRange(defaultRejectLines);
            if (approveLines.Count == 0) approveLines.AddRange(defaultApproveLines);
        }

        private void BindRuntimeEvents()
        {
            foreach (var slot in colorSlots)
            {
                if (slot == null) continue;
                slot.Clicked -= HandleColorSlotClicked;
                slot.Clicked += HandleColorSlotClicked;
                slot.StateChanged -= HandleColorSlotStateChanged;
                slot.StateChanged += HandleColorSlotStateChanged;
            }
        }

        private void UnbindRuntimeEvents()
        {
            foreach (var slot in colorSlots)
            {
                if (slot != null)
                {
                    slot.Clicked -= HandleColorSlotClicked;
                    slot.StateChanged -= HandleColorSlotStateChanged;
                }
            }
        }

        private void AutoConfigureRuntimeUi()
        {
            var uiRoot = ResolveUiRoot();
            if (uiRoot == null) return;

            if (interactionGroup == null)
            {
                interactionGroup = uiRoot.GetComponent<CanvasGroup>();
                if (interactionGroup == null)
                    interactionGroup = uiRoot.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private Transform ResolveUiRoot()
        {
            var inputBox = FindDescendant(transform, "InputBox");
            if (inputBox != null)
            {
                var inputCanvas = inputBox.GetComponentInParent<Canvas>();
                if (inputCanvas != null) return inputCanvas.transform;
            }

            for (var i = 0; i < colorSlots.Count; i++)
            {
                if (colorSlots[i] == null) continue;
                var slotCanvas = colorSlots[i].GetComponentInParent<Canvas>();
                if (slotCanvas != null) return slotCanvas.transform;
            }

            if (currentPaletteSwatch != null)
            {
                var swatchCanvas = currentPaletteSwatch.GetComponentInParent<Canvas>();
                if (swatchCanvas != null) return swatchCanvas.transform;
            }

            return null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;

            for (var i = 0; i < root.childCount; i++)
            {
                var match = FindDescendant(root.GetChild(i), name);
                if (match != null) return match;
            }
            return null;
        }
    }

    public readonly struct ColorSubmitResult
    {
        public ColorSubmitResult(bool success, int correctCount, int totalCount, string feedbackLine)
        {
            this.success = success;
            this.correctCount = correctCount;
            this.totalCount = totalCount;
            this.feedbackLine = feedbackLine;
        }

        public bool success { get; }
        public int correctCount { get; }
        public int totalCount { get; }
        public string feedbackLine { get; }
    }
}
