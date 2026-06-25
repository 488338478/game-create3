using System.Threading.Tasks;
using UnityEngine;

namespace GameCreate3.StoryPlayer
{
    public interface IAudioService
    {
        bool IsBgmPlaying { get; }

        Task PlayBgmAsync(AudioClip clip, float volume = 1f, bool loop = true, bool fadeIn = true);
        Task CrossFadeBgmAsync(AudioClip newClip, float volume = 1f, bool loop = true);
        Task StopBgmAsync(bool fadeOut = true);
        void StopBgm();
        void PlayBgm(string clipName, float volume = 1f, bool loop = true);

        void PlaySfx(AudioClip clip, float volume = 1f);
        void PlaySfx(string clipName, float volume = 1f);

        void PlayVoiceOver(AudioClip clip);
        void StopVoiceOver();

        void ApplyPageAudioConfig(StoryAudioConfig config);
    }
}
