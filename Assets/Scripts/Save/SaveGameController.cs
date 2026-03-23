using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCreate3
{
    public sealed class SaveGameController : MonoBehaviour
    {
        [SerializeField] private string saveFileName = "save_slot_01.json";
        [SerializeField] private Transform playerTransform;
        [SerializeField] private NarrativeVariableStore variableStore;
        [SerializeField] private ObjectiveTracker objectiveTracker;

        public void Save()
        {
            if (playerTransform == null || variableStore == null)
            {
                Debug.LogWarning("[SaveGameController] Missing player transform or variable store.");
                return;
            }

            var completed = objectiveTracker != null
                ? objectiveTracker.CaptureCompletedObjectives().ToArray()
                : System.Array.Empty<string>();

            var saveData = new GameSaveData
            {
                sceneName = SceneManager.GetActiveScene().name,
                playerPosition = new SerializableVector3(
                    playerTransform.position.x,
                    playerTransform.position.y,
                    playerTransform.position.z),
                variables = variableStore.CaptureSnapshot(),
                completedObjectives = completed
            };

            JsonSaveUtility.Save(saveFileName, saveData);
        }

        public void Load()
        {
            if (!JsonSaveUtility.TryLoad(saveFileName, out GameSaveData saveData))
            {
                Debug.LogWarning("[SaveGameController] No save file found.");
                return;
            }

            if (saveData == null)
            {
                return;
            }

            var activeSceneName = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrWhiteSpace(saveData.sceneName) && saveData.sceneName != activeSceneName)
            {
                Debug.LogWarning($"[SaveGameController] Save scene mismatch. Expected '{activeSceneName}', got '{saveData.sceneName}'.");
            }

            if (variableStore != null && saveData.variables != null)
            {
                variableStore.RestoreSnapshot(saveData.variables);
            }

            if (objectiveTracker != null)
            {
                objectiveTracker.RestoreCompletedObjectives(saveData.completedObjectives);
            }

            if (playerTransform != null)
            {
                playerTransform.position = new Vector3(
                    saveData.playerPosition.x,
                    saveData.playerPosition.y,
                    saveData.playerPosition.z);
            }
        }
    }
}
