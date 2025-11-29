using UnityEngine;

namespace _Project.Scripts.Audio
{
    public interface IAudioService
    {
        void Play(AudioEvent audioEvent);
        void PlayAt(AudioEvent audioEvent, Vector3 position);
        void PlayOneShot(AudioEvent audioEvent);
        void Stop(AudioEvent audioEvent);
        void StopAllByCategory(AudioCategory category);
        void SetCategoryVolume(AudioCategory category, float volume); // 0..1
        float GetCategoryVolume(AudioCategory category);
    }
}