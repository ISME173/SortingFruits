using UnityEngine;

namespace _Project.Scripts.Audio
{
    public class PooledAudioSource
    {
        public AudioSource Source;
        public AudioEvent CurrentEvent;
        public bool Busy;

        public void Configure(AudioEvent audioEvent, Vector3? position = null)
        {
            CurrentEvent = audioEvent;
            Source.clip = audioEvent.Clip;
            Source.loop = audioEvent.Loop;
            Source.pitch = audioEvent.Pitch;
            Source.volume = audioEvent.Volume;
            if (audioEvent.UseSpatialBlend)
            {
                Source.spatialBlend = audioEvent.SpatialBlend;
                if (position.HasValue)
                    Source.transform.position = position.Value;
            }
            else
            {
                Source.spatialBlend = 0;
            }
        }
    }
}