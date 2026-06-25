using UnityEngine;
using GameCreate3.UI;

namespace GameCreate3.Core
{
    [DefaultExecutionOrder(-200)]
    public sealed class DontDestroyOnLoadCleaner : MonoBehaviour
    {
        private void Awake()
        {
            if (UIControlSystem.Instance != null && UIControlSystem.Instance.gameObject.scene != gameObject.scene)
            {
                DestroyImmediate(UIControlSystem.Instance.gameObject);
            }

            Destroy(gameObject);
        }
    }
}
