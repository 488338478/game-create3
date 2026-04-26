using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class DreamPathOpener : MonoBehaviour
    {
        [SerializeField] private GameObject blockedPath;
        [SerializeField] private GameObject openPath;
        [SerializeField] private SpriteRenderer pathRenderer;
        [SerializeField] private Color openColor = new Color(0.6f, 0.85f, 1f, 1f);

        public void OpenPath()
        {
            if (blockedPath != null) blockedPath.SetActive(false);
            if (openPath != null) openPath.SetActive(true);
            if (pathRenderer != null) pathRenderer.color = openColor;
        }

        public void Reset()
        {
            if (blockedPath != null) blockedPath.SetActive(true);
            if (openPath != null) openPath.SetActive(false);
        }
    }
}
