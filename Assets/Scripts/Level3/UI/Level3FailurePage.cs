using GameCreate3.Core.SceneRouting;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.Level3
{
    public sealed class Level3FailurePage : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Animation")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private float showDelay = 0.5f;

        private void Awake()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetry);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenu);
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (retryButton != null)
                retryButton.onClick.RemoveListener(OnRetry);
            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveListener(OnMainMenu);
        }

        // --- WorkspaceEventRouter 调用的 public 入口 ---

        public void OnLevelFail()
        {
            Invoke(nameof(ShowPage), showDelay);
        }

        // ---

        private void ShowPage()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        private void OnRetry() => SceneRouter.Reload();
        private void OnMainMenu() => SceneRouter.Go("main_menu");
    }
}
