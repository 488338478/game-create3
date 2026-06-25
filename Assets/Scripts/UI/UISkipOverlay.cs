using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using TMPro;
using GameCreate3.Core.SceneRouting;
using GameCreate3.StoryPlayer;

namespace GameCreate3.UI
{
    public sealed class UISkipOverlay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject persistentHint;
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private TMP_Text systemMessageText;

        [Header("Settings")]
        [SerializeField] private string skipRouteId = "main_menu";
        [SerializeField] private float messageDuration = 2f;
        [SerializeField] private string confirmMessage = "再按一次 ESC 跳过";

        private enum State { Idle, WaitingConfirm }

        private State state = State.Idle;
        private Coroutine hideMessageRoutine;
        private VideoPlayer[] pausedVideos;

        private void Start()
        {
            if (persistentHint != null) persistentHint.SetActive(true);
            if (confirmPanel != null) confirmPanel.SetActive(false);
        }

        private void Update()
        {
            switch (state)
            {
                case State.Idle:
                    if (Input.GetKeyDown(KeyCode.Escape))
                        EnterConfirm();
                    break;

                case State.WaitingConfirm:
                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        ExecuteSkip();
                    }
                    else if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Escape))
                    {
                        ExitConfirm();
                    }
                    break;
            }
        }

        private void EnterConfirm()
        {
            state = State.WaitingConfirm;
            Time.timeScale = 0f;
            PauseAllVideos();
            PauseStoryPlayer();

            if (confirmPanel != null) confirmPanel.SetActive(true);
            ShowSystemMessage(confirmMessage);
        }

        private void ExitConfirm()
        {
            state = State.Idle;
            Time.timeScale = 1f;
            ResumeAllVideos();
            ResumeStoryPlayer();

            if (confirmPanel != null) confirmPanel.SetActive(false);
            HideSystemMessage();
        }

        private void ExecuteSkip()
        {
            Time.timeScale = 1f;
            DestroyStoryPlayer();
            SceneRouter.Go(skipRouteId);
        }

        private void ShowSystemMessage(string text)
        {
            if (systemMessageText == null) return;

            if (hideMessageRoutine != null)
                StopCoroutine(hideMessageRoutine);

            systemMessageText.text = text;
            systemMessageText.gameObject.SetActive(true);
        }

        private void HideSystemMessage()
        {
            if (systemMessageText == null) return;

            if (hideMessageRoutine != null)
                StopCoroutine(hideMessageRoutine);

            hideMessageRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(messageDuration);
            systemMessageText.gameObject.SetActive(false);
            hideMessageRoutine = null;
        }

        private void PauseAllVideos()
        {
            var all = FindObjectsOfType<VideoPlayer>();
            var count = 0;
            foreach (var vp in all)
            {
                if (vp.isPlaying) count++;
            }
            pausedVideos = new VideoPlayer[count];
            var idx = 0;
            foreach (var vp in all)
            {
                if (vp.isPlaying)
                {
                    vp.Pause();
                    pausedVideos[idx++] = vp;
                }
            }
        }

        private void ResumeAllVideos()
        {
            if (pausedVideos == null) return;
            foreach (var vp in pausedVideos)
            {
                if (vp != null)
                    vp.Play();
            }
            pausedVideos = null;
        }

        private void PauseStoryPlayer()
        {
            if (StoryPlayerService.IsPlaying)
                StoryPlayerService.Player?.Pause();
        }

        private void ResumeStoryPlayer()
        {
            StoryPlayerService.Player?.Resume();
        }

        private void DestroyStoryPlayer()
        {
            var player = StoryPlayerService.Player;
            if (player == null) return;

            player.Stop();
            var rig = player.transform.root.gameObject;
            Destroy(rig);
        }

        private void OnDisable()
        {
            if (state == State.WaitingConfirm)
            {
                Time.timeScale = 1f;
                ResumeAllVideos();
                ResumeStoryPlayer();
                state = State.Idle;
            }
        }
    }
}
