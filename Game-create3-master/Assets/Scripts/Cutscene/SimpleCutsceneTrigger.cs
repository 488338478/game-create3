using UnityEngine;
using UnityEngine.Playables;

namespace GameCreate3
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class SimpleCutsceneTrigger : MonoBehaviour
    {
        [SerializeField] private PlayableDirector director;
        [SerializeField] private SideScrollerPlayerController playerController;
        [SerializeField] private bool oneShot = true;
        [SerializeField] private bool triggerByTag = true;
        [SerializeField] private string targetTag = "Player";

        private bool consumed;

        private void Reset()
        {
            var collider2D = GetComponent<Collider2D>();
            collider2D.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed && oneShot)
            {
                return;
            }

            if (triggerByTag && !other.CompareTag(targetTag))
            {
                return;
            }

            Play();
        }

        public void Play()
        {
            if (director == null)
            {
                Debug.LogWarning("[SimpleCutsceneTrigger] PlayableDirector is missing.");
                return;
            }

            director.stopped -= HandleCutsceneStopped;
            director.stopped += HandleCutsceneStopped;

            if (playerController != null)
            {
                playerController.SetInputLocked(true);
            }

            director.Play();
            consumed = true;
        }

        private void HandleCutsceneStopped(PlayableDirector _)
        {
            if (playerController != null)
            {
                playerController.SetInputLocked(false);
            }
        }
    }
}
