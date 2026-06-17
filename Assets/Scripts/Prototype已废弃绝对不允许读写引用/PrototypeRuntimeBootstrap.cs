using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;

namespace GameCreate3
{
    public sealed class PrototypeRuntimeBootstrap : MonoBehaviour
    {
        private const string BootstrapObjectName = "_PrototypeRuntimeBootstrap";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || (activeScene.name != "SampleScene" && activeScene.name != "Chapter2Prototype"))
            {
                return;
            }

            if (FindObjectOfType<PrototypeRuntimeBootstrap>() != null)
            {
                return;
            }

            var bootstrap = new GameObject(BootstrapObjectName);
            bootstrap.AddComponent<PrototypeRuntimeBootstrap>();
        }

        private void Start()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "Chapter2Prototype")
            {
                var type = typeof(PrototypeRuntimeBootstrap).Assembly.GetType("GameCreate3.Chapter2PrototypeAutoBuilder");
                var method = type?.GetMethod("BuildIfNeeded", BindingFlags.Public | BindingFlags.Static);
                method?.Invoke(null, null);
                return;
            }

            Debug.LogWarning("[PrototypeRuntimeBootstrap] SampleScene 的旧运行时原型已废弃。请使用 Assets/Scenes/Chapter2Prototype.unity。");
        }
    }
}
