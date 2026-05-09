using System.Collections;
using UnityEngine;

namespace GameCreate3.StoryPlayer
{
    /// <summary>
    /// 场景加载完后立刻播剧情（开场用）。可设延时。
    /// 用法：拖 prefab 进场景，sequence 字段挂剧情 asset。
    /// </summary>
    public sealed class StoryAutoPlay : MonoBehaviour
    {
        [SerializeField] private StorySequence sequence;
        [Tooltip("场景加载到剧情开始前的延时（秒）")]
        [SerializeField] private float delaySec = 0f;

        private IEnumerator Start()
        {
            if (sequence == null) yield break;
            if (delaySec > 0f) yield return new WaitForSeconds(delaySec);
            StoryPlayerService.Play(sequence);
        }
    }
}
