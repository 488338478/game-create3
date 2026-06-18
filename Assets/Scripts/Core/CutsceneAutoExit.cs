using UnityEngine;
using UnityEngine.Video;
using GameCreate3.Core.SceneRouting;

public class CutsceneAutoExit : MonoBehaviour
{
    [SerializeField] private string nextRouteId;
    [SerializeField] private VideoPlayer videoPlayer;

    private void Start()
    {
        if (!videoPlayer) videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer) videoPlayer.loopPointReached += _ => SceneRouter.Go(nextRouteId);
    }
}
