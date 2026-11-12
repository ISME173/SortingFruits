using UnityEngine;

namespace _Project.Scripts.Audio
{
    [CreateAssetMenu(fileName = "AudioEvent", menuName = "Audio/Audio Event")]
    public class AudioEvent : ScriptableObject
    {
        [SerializeField] private AudioClip _clip;
        [SerializeField] private AudioCategory _category;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;
        [SerializeField, Range(-3f, 3f)] private float _pitch = 1f;
        [SerializeField] private bool _loop;
        [SerializeField] private bool _useSpatialBlend;
        [SerializeField, Range(0, 1)] private float _spatialBlend;

        public AudioClip Clip => _clip;
        public AudioCategory Category => _category;
        public float Volume => _volume;
        public float Pitch => _pitch;
        public bool Loop => _loop;
        public bool UseSpatialBlend => _useSpatialBlend;
        public float SpatialBlend => _spatialBlend;
    }
}