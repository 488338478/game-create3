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
            // 优先用 prefab —— 由 GameCreate3/DualWorld/Generate Workspace Prefab 菜单生成。
            // 没找到 prefab 时回退到老的运行时反射搭建器，保证旧场景仍可跑。
            if (FindObjectOfType<DualWorldWorkspace>() != null) return;

            EnsureSceneEssentials();

            var prefab = Resources.Load<GameObject>("Prefabs/DualWorldWorkspace");
            if (prefab != null)
            {
                Instantiate(prefab);
            }
            else
            {
                Debug.LogWarning("[DualWorldRuntimeBootstrap] Resources/Prefabs/DualWorldWorkspace.prefab not found, falling back to runtime AutoBuilder. Run menu 'GameCreate3/DualWorld/Generate Workspace Prefab' to bake one.");
                DualWorldTestSceneAutoBuilder.BuildIfNeeded();
            }
        }

        private static void EnsureSceneEssentials()
        {
            // EventSystem / Main Camera / CinemachineBrain 是场景全局，不能塞进工作区 prefab。
            // 缺哪个就 Instantiate 一份 SceneEssentials；没 prefab 就让 AutoBuilder 兜底。
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null && Camera.main != null) return;

            var essentials = Resources.Load<GameObject>("Prefabs/SceneEssentials");
            if (essentials != null) Instantiate(essentials);
        }
    }
}
