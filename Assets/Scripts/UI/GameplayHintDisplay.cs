using UnityEngine;

namespace GameCreate3.UI
{
    public sealed class GameplayHintDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject[] entries;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (entries == null) return;
            foreach (var obj in entries)
            {
                if (obj != null) Destroy(obj);
            }
        }
    }
}
