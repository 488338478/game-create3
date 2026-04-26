using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCreate3.DualWorld
{
    public sealed class DualWorldRuntimeBootstrap : MonoBehaviour
    {
        public const string SceneName = "DW_Test_Workspace";
        private const string BootstrapObjectName = "_DualWorldRuntimeBootstrap";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.name != SceneName)
            {
                return;
            }

            if (FindObjectOfType<DualWorldRuntimeBootstrap>() != null)
            {
                return;
            }

            var bootstrap = new GameObject(BootstrapObjectName);
            bootstrap.AddComponent<DualWorldRuntimeBootstrap>();
        }

        private void Start()
        {
            DualWorldTestSceneAutoBuilder.BuildIfNeeded();
        }
    }
}
