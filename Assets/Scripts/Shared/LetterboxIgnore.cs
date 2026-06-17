using UnityEngine;

namespace GameCreate3
{
    /// <summary>
    /// 挂在"由其它系统自行管理 camera.rect"的相机上，<see cref="LetterboxManager"/> 会跳过它，
    /// 避免争抢 viewport。普通全屏相机不需要挂。
    /// </summary>
    public sealed class LetterboxIgnore : MonoBehaviour
    {
    }
}
