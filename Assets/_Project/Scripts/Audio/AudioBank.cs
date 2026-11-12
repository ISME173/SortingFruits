using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Audio
{
    [CreateAssetMenu(fileName = "AudioBank", menuName = "Audio/Audio Bank")]
    public class AudioBank : ScriptableObject
    {
        [SerializeField] private List<AudioEvent> _events = new List<AudioEvent>();

        public IReadOnlyList<AudioEvent> Events => _events;

        public AudioEvent GetByClip(AudioClip clip)
        {
            for (int i = 0; i < _events.Count; i++)
                if (_events[i].Clip == clip) return _events[i];
            return null;
        }
    }
}