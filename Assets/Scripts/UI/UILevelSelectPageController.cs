using System;
using System.Collections.Generic;
using GameCreate3.Core;
using GameCreate3.Core.SceneRouting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.UI
{
    public sealed class UILevelSelectPageController : UIPageController
    {
        [Serializable]
        private sealed class LevelRouteEntry
        {
            public string routeId;
            public string displayName;
            public bool interactable = true;
            public Sprite selectedSprite;
            public Sprite unselectedSprite;
            public Sprite lockedSprite;
        }

        [Header("Navigation")]
        [SerializeField] private Button backButton;

        [Header("Carousel Level Select")]
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button selectedLevelButton;
        [SerializeField] private Image selectedLevelPreviewImage;
        [SerializeField] private TMP_Text selectedLevelNameText;
        [SerializeField] private TMP_Text selectedLevelStateText;
        [SerializeField] private bool wrapAround = true;
        [SerializeField] private bool useSaveUnlocks;
        [SerializeField] private int defaultUnlockedMaxLevelIndex = 0;

        [Header("Dynamic Level Buttons")]
        [SerializeField] private Transform levelButtonContainer;
        [SerializeField] private Button levelButtonPrefab;
        [SerializeField] private bool rebuildOnOpen = true;
        [SerializeField] private List<LevelRouteEntry> levels = new List<LevelRouteEntry>();

        private readonly List<Button> spawnedButtons = new List<Button>();
        private int currentIndex;

        private void OnEnable()
        {
            Add(backButton, HandleBack);
            Add(previousButton, HandlePrevious);
            Add(nextButton, HandleNext);
            Add(selectedLevelButton, HandleSelectedLevel);
        }

        private void OnDisable()
        {
            Remove(backButton, HandleBack);
            Remove(previousButton, HandlePrevious);
            Remove(nextButton, HandleNext);
            Remove(selectedLevelButton, HandleSelectedLevel);
            ClearSpawnedButtons();
        }

        protected override void OnOpened(object data)
        {
            currentIndex = ResolveInitialIndex();
            RefreshSelectedLevelView();

            if (rebuildOnOpen)
            {
                RebuildLevelButtons();
            }
        }

        [ContextMenu("Rebuild Level Buttons")]
        public void RebuildLevelButtons()
        {
            ClearSpawnedButtons();
            if (levelButtonContainer == null || levelButtonPrefab == null)
            {
                return;
            }

            for (var i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                if (level == null || string.IsNullOrWhiteSpace(level.routeId))
                {
                    continue;
                }

                var routeId = level.routeId;
                var button = Instantiate(levelButtonPrefab, levelButtonContainer);
                button.name = $"LevelButton_{i + 1}_{routeId}";
                button.gameObject.SetActive(true);
                button.interactable = IsLevelSelectable(i);

                var label = string.IsNullOrWhiteSpace(level.displayName) ? routeId : level.displayName;
                ApplyButtonLabel(button, label);
                ApplyButtonSprite(button, ResolveLevelSprite(i, i == currentIndex));
                var levelIndex = i;
                button.onClick.AddListener(() => HandleLevelSelected(levelIndex));
                spawnedButtons.Add(button);
            }
        }

        private void HandleBack()
        {
            UIControlSystem.Instance?.OpenPage(UIPageIds.MainMenu);
        }

        private void HandlePrevious()
        {
            MoveSelection(-1);
        }

        private void HandleNext()
        {
            MoveSelection(1);
        }

        private void HandleSelectedLevel()
        {
            HandleLevelSelected(currentIndex);
        }

        private void MoveSelection(int delta)
        {
            if (levels.Count == 0)
            {
                return;
            }

            var nextIndex = currentIndex + delta;
            if (wrapAround)
            {
                nextIndex = (nextIndex % levels.Count + levels.Count) % levels.Count;
            }
            else
            {
                nextIndex = Mathf.Clamp(nextIndex, 0, levels.Count - 1);
            }

            currentIndex = nextIndex;
            RefreshSelectedLevelView();
        }

        private void HandleLevelSelected(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= levels.Count || !IsLevelSelectable(levelIndex))
            {
                return;
            }

            var routeId = levels[levelIndex].routeId;
            if (string.IsNullOrWhiteSpace(routeId))
            {
                return;
            }

            MarkProgress(routeId, levelIndex);
            SceneRouter.Go(routeId);
        }

        private void RefreshSelectedLevelView()
        {
            if (levels.Count == 0)
            {
                ApplySelectedLevelText("暂无关卡");
                ApplySelectedLevelPreview(null);
                SetSelectedLevelInteractable(false);
                return;
            }

            currentIndex = Mathf.Clamp(currentIndex, 0, levels.Count - 1);
            var level = levels[currentIndex];
            var displayName = level != null && !string.IsNullOrWhiteSpace(level.displayName)
                ? level.displayName
                : level != null ? level.routeId : string.Empty;
            var selectable = IsLevelSelectable(currentIndex);

            ApplySelectedLevelText(displayName);
            ApplySelectedLevelPreview(ResolveLevelSprite(currentIndex, true));
            SetSelectedLevelInteractable(selectable);
            RefreshSpawnedButtonSprites();

            if (selectedLevelStateText != null)
            {
                selectedLevelStateText.text = selectable ? "点击进入" : "未解锁";
            }

            if (!wrapAround)
            {
                if (previousButton != null)
                {
                    previousButton.interactable = currentIndex > 0;
                }

                if (nextButton != null)
                {
                    nextButton.interactable = currentIndex < levels.Count - 1;
                }
            }
            else
            {
                if (previousButton != null)
                {
                    previousButton.interactable = levels.Count > 1;
                }

                if (nextButton != null)
                {
                    nextButton.interactable = levels.Count > 1;
                }
            }
        }

        private int ResolveInitialIndex()
        {
            var save = GameSaveProgressService.Instance;
            var lastRouteId = save != null ? save.GetProgress(UIProgressKeys.LastRouteId, string.Empty) : string.Empty;
            if (!string.IsNullOrWhiteSpace(lastRouteId))
            {
                for (var i = 0; i < levels.Count; i++)
                {
                    if (levels[i] != null && levels[i].routeId == lastRouteId)
                    {
                        return i;
                    }
                }
            }

            return Mathf.Clamp(GetUnlockedMaxLevelIndex(), 0, Mathf.Max(0, levels.Count - 1));
        }

        private bool IsLevelSelectable(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= levels.Count)
            {
                return false;
            }

            var level = levels[levelIndex];
            if (level == null || string.IsNullOrWhiteSpace(level.routeId) || !level.interactable)
            {
                return false;
            }

            return !useSaveUnlocks || levelIndex <= GetUnlockedMaxLevelIndex();
        }

        private int GetUnlockedMaxLevelIndex()
        {
            if (!useSaveUnlocks)
            {
                return levels.Count - 1;
            }

            var save = GameSaveProgressService.Instance;
            if (save == null)
            {
                return defaultUnlockedMaxLevelIndex;
            }

            var raw = save.GetProgress(UIProgressKeys.UnlockedMaxLevelIndex, defaultUnlockedMaxLevelIndex.ToString());
            return int.TryParse(raw, out var index) ? index : defaultUnlockedMaxLevelIndex;
        }

        private static void MarkProgress(string routeId, int levelIndex)
        {
            var save = GameSaveProgressService.Instance;
            if (save == null)
            {
                return;
            }

            save.SetProgress(UIProgressKeys.HasProgress, true.ToString());
            save.SetProgress(UIProgressKeys.LastRouteId, routeId);

            var rawUnlocked = save.GetProgress(UIProgressKeys.UnlockedMaxLevelIndex, "0");
            var unlockedIndex = int.TryParse(rawUnlocked, out var parsed) ? parsed : 0;
            save.SetProgress(UIProgressKeys.UnlockedMaxLevelIndex, Mathf.Max(unlockedIndex, levelIndex).ToString());
            save.Save();
        }

        private void ApplySelectedLevelText(string label)
        {
            if (selectedLevelNameText != null)
            {
                selectedLevelNameText.text = label;
            }

            ApplyButtonLabel(selectedLevelButton, label);
        }

        private void ApplySelectedLevelPreview(Sprite sprite)
        {
            if (selectedLevelPreviewImage == null)
            {
                return;
            }

            selectedLevelPreviewImage.sprite = sprite;
            selectedLevelPreviewImage.enabled = sprite != null;
            selectedLevelPreviewImage.preserveAspect = true;
        }

        private void SetSelectedLevelInteractable(bool interactable)
        {
            if (selectedLevelButton != null)
            {
                selectedLevelButton.interactable = interactable;
            }
        }

        private Sprite ResolveLevelSprite(int levelIndex, bool selected)
        {
            if (levelIndex < 0 || levelIndex >= levels.Count)
            {
                return null;
            }

            var level = levels[levelIndex];
            if (level == null)
            {
                return null;
            }

            if (!IsLevelSelectable(levelIndex) && level.lockedSprite != null)
            {
                return level.lockedSprite;
            }

            if (selected && level.selectedSprite != null)
            {
                return level.selectedSprite;
            }

            return level.unselectedSprite != null ? level.unselectedSprite : level.selectedSprite;
        }

        private void RefreshSpawnedButtonSprites()
        {
            for (var i = 0; i < spawnedButtons.Count; i++)
            {
                ApplyButtonSprite(spawnedButtons[i], ResolveLevelSprite(i, i == currentIndex));
            }
        }

        private static void ApplyButtonSprite(Button button, Sprite sprite)
        {
            if (button == null || sprite == null)
            {
                return;
            }

            var image = button.targetGraphic as Image;
            if (image == null)
            {
                image = button.GetComponent<Image>();
            }

            if (image != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
            }
        }

        private void ClearSpawnedButtons()
        {
            for (var i = 0; i < spawnedButtons.Count; i++)
            {
                var button = spawnedButtons[i];
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                Destroy(button.gameObject);
            }

            spawnedButtons.Clear();
        }

        private static void ApplyButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            var tmpLabel = button.GetComponentInChildren<TMP_Text>(true);
            if (tmpLabel != null)
            {
                tmpLabel.text = label;
                return;
            }

            var uiLabel = button.GetComponentInChildren<Text>(true);
            if (uiLabel != null)
            {
                uiLabel.text = label;
            }
        }

        private static void Add(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void Remove(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }
    }
}
