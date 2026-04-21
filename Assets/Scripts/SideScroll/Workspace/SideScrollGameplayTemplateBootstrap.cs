using UnityEngine;
using System.Reflection;

namespace GameCreate3
{
    public sealed class SideScrollGameplayTemplateBootstrap : MonoBehaviour
    {
        private void Start()
        {
            InvokeStatic("GameCreate3.SideScrollTemplateSceneAutoBuilder", "BuildGameplayTemplate");
        }

        private static void InvokeStatic(string typeName, string methodName)
        {
            var type = typeof(SideScrollGameplayTemplateBootstrap).Assembly.GetType(typeName);
            var method = type?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            method?.Invoke(null, null);
        }
    }
}
