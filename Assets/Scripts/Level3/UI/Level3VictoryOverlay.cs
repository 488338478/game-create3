using System.Collections;
using GameCreate3.Core.SceneRouting;
using TMPro;
using UnityEngine;

namespace GameCreate3.Level3
{
    public sealed class Level3VictoryOverlay : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI celebrationText;
        [SerializeField] private float celebrationDuration = 3f;
        [SerializeField] private string celebrationMessage = "粉丝突破 10 万！\n梦境即将结束...";

        [Header("Transition")]
        [SerializeField] private string targetRouteId = "level3cutscene";

        private void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        // --- WorkspaceEventRouter 调用的 public 入口 ---

        public void OnLevelComplete()
        {
            StartCoroutine(PlayVictorySequence());
        }

        // ---

        private IEnumerator PlayVictorySequence()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);

            if (celebrationText != null)
            {
                celebrationText.text = "";
                for (var i = 0; i < celebrationMessage.Length; i++)
                {
                    celebrationText.text = celebrationMessage.Substring(0, i + 1);
                    yield return new WaitForSeconds(0.05f);
                }
            }

            yield return new WaitForSeconds(celebrationDuration);

            if (!string.IsNullOrEmpty(targetRouteId))
                SceneRouter.Go(targetRouteId);
            else
                SceneRouter.GoScene("Level3Cutscene");
        }
    }
}
