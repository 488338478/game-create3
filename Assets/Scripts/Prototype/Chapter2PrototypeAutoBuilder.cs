using UnityEngine;

namespace GameCreate3
{
    public sealed partial class Chapter2PrototypeAutoBuilder : MonoBehaviour
    {
        public static Chapter2PrototypeAutoBuilder BuildIfNeeded()
        {
            var existing = FindObjectOfType<Chapter2PrototypeAutoBuilder>();
            if (existing != null)
            {
                return existing;
            }

            return new GameObject("Chapter2PrototypeAutoBuilder").AddComponent<Chapter2PrototypeAutoBuilder>();
        }
    }
}
