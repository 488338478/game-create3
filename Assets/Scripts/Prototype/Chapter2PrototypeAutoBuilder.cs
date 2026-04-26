using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3
{
    public static class Chapter2PrototypeAutoBuilder
    {
        private static bool hasBuilt;

        public static void BuildIfNeeded()
        {
            if (hasBuilt)
            {
                return;
            }

            var coordinator = Object.FindObjectOfType<PrototypeDemoCoordinator>();
            if (coordinator != null)
            {
                hasBuilt = true;
                return;
            }

            BuildPrototype();
            hasBuilt = true;
        }

        private static void BuildPrototype()
        {
            var bootstrap = Object.FindObjectOfType<PrototypeRuntimeBootstrap>();
            if (bootstrap == null)
            {
                return;
            }

            var coordinator = bootstrap.gameObject.AddComponent<PrototypeDemoCoordinator>();

            Debug.Log("[Chapter2PrototypeAutoBuilder] 第二章原型已自动构建。");
        }

        public static void Reset()
        {
            hasBuilt = false;
        }
    }
}
