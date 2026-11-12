using _Project.Scripts.Audio;
using _Project.Scripts.DI;
using Reflex.Core;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using _Project.Scripts.Saves;

namespace _Project.Scripts.Audio
{
    public class AudioBindingModule : BindingModule
    {
        [Header("Runner для корутин (любой MonoBehaviour в сцене)")]
        [SerializeField] private MonoBehaviour _runnerForCoroutines;

        [Header("Mixer и группы")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;
        [SerializeField] private AudioMixerGroup _uiGroup;

        [Header("Имена параметров громкости (Exposed)")]
        [SerializeField] private string _musicVolumeParam = "MusicVolume";
        [SerializeField] private string _sfxVolumeParam = "SfxVolume";
        [SerializeField] private string _uiVolumeParam = "UiVolume";

        [Header("Размер пула источников SFX/UI")]
        [SerializeField] private int _initialPoolSize = 12;

        public override void Bind(ContainerBuilder containerBuilder)
        {
            var saves = Container.ProjectContainer.Resolve<ISaves>();

            var groups = new Dictionary<AudioCategory, AudioMixerGroup>
            {
                { AudioCategory.Music, _musicGroup },
                { AudioCategory.Sfx, _sfxGroup },
                { AudioCategory.Ui, _uiGroup }
            };

            var paramsMap = new Dictionary<AudioCategory, string>
            {
                { AudioCategory.Music, _musicVolumeParam },
                { AudioCategory.Sfx, _sfxVolumeParam },
                { AudioCategory.Ui, _uiVolumeParam }
            };

            var service = new AudioService(
                _runnerForCoroutines,
                saves,
                _audioMixer,
                groups,
                paramsMap,
                _initialPoolSize);

            containerBuilder.AddSingleton(service, typeof(IAudioService));
        }
    }
}