using UnityEngine;

namespace _Project.Scripts.Audio
{
    public interface IAudioService
    {
        void Play(AudioEvent audioEvent);
        void PlayAt(AudioEvent audioEvent, Vector3 position);
        void PlayOneShot(AudioEvent audioEvent);
        void PlayOneShot(AudioEvent audioEvent, Vector3 position);
        void Stop(AudioEvent audioEvent);
        void StopAllByCategory(AudioCategory category);

        void SetCategoryVolume(AudioCategory category, float volume);
        float GetCategoryVolume(AudioCategory category);

        // NEW: временное заглушение категории (не влияет на сохранённую громкость)
        void SetCategoryMuted(AudioCategory category, bool muted);
        bool GetCategoryMuted(AudioCategory category);
    }
}