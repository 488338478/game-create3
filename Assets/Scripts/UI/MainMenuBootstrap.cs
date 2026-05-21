using UnityEngine;

namespace GameCreate3.UI
{
    /// <summary>
    /// 主菜单场景启动时自动打开 main_menu 页面。
    /// 挂在 SceneEssentials 或任意常驻物体上。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class MainMenuBootstrap : MonoBehaviour
    {
        [SerializeField] private bool openMainMenuOnStart = true;

        private void Start()
        {
            if (!openMainMenuOnStart)
            {
                return;
            }

            if (UIControlSystem.Instance == null)
            {
                Debug.LogError(
                    "[MainMenuBootstrap] UIControlSystem 不存在。请运行菜单 GameCreate3/Setup MainMenu Scene。");
                return;
            }

            // UIControlSystem.StartupPageId 也会打开；这里再调一次确保主菜单可见。
            UIControlSystem.Instance.TryOpenStartupPage();
            UIControlSystem.Instance.OpenPage(UIPageIds.MainMenu);
        }
    }
}
