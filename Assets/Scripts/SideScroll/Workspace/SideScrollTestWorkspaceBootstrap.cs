using UnityEngine;
using System.Reflection;

namespace GameCreate3
{
    public sealed class SideScrollTestWorkspaceBootstrap : MonoBehaviour
    {
        private void Start()
        {
            InvokeStatic("GameCreate3.SideScrollTestWorkspaceAutoBuilder", "BuildIfNeeded");
        }

        private static void InvokeStatic(string typeName, string methodName)
        {
            var type = typeof(SideScrollTestWorkspaceBootstrap).Assembly.GetType(typeName);
            var method = type?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            method?.Invoke(null, null);
        }
    }
}
