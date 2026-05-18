using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameCreate3.Core;
using GameCreate3.Core.SceneRouting;

namespace GameCreate3.UI
{
    public sealed class UIResultPageData
    {
        public string title;
        public string stageName;
        public bool hasUnlockedCG;
        public string unlockedCGId;
        public bool hasNextStage;
    }

    public sealed class UIResultPageController : UIPageController
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text stageNameText;
        [SerializeField] private GameObject unlockRoot;
        [SerializeField] private TMP_Text unlockTitle;
        [SerializeField] private Image unlockPreview;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button mainMenuButton;

        private UIResultPageData resultData;

        private void OnEnable()
        {
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(HandleNext);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(HandleMainMenu);
            }
        }

        private void OnDisable()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNext);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(HandleMainMenu);
            }
        }

        protected override void OnOpened(object data)
        {
            resultData = data as UIResultPageData;
            if (resultData == null && data is UISettlementData settlement)
            {
                resultData = new UIResultPageData
                {
                    title = settlement.title,
                    stageName = settlement.message,
                    hasNextStage = settlement.cleared
                };
            }

            Apply(resultData);
        }

        private void Apply(UIResultPageData data)
        {
            if (titleText != null)
            {
                titleText.text = data != null && !string.IsNullOrEmpty(data.title) ? data.title : "完成";
            }

            if (stageNameText != null)
            {
                stageNameText.text = data != null ? data.stageName : string.Empty;
            }

            if (unlockRoot != null)
            {
                unlockRoot.SetActive(data != null && data.hasUnlockedCG);
            }

            if (unlockTitle != null)
            {
                unlockTitle.text = data != null && data.hasUnlockedCG ? $"解锁 CG：{data.unlockedCGId}" : string.Empty;
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(data == null || data.hasNextStage);
            }
        }

        private static void HandleNext()
        {
            SceneRouter.Go("next_stage");
        }

        private static void HandleMainMenu()
        {
            SceneRouter.Go("main_menu");
        }
    }
}
