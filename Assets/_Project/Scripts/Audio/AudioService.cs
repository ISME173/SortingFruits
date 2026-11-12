using _Project.Scripts.Saves;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace _Project.Scripts.Audio
{
    public class AudioService : IAudioService
    {
        private readonly Dictionary<AudioCategory, float> _categoryLinearVolumes = new();
        private readonly Dictionary<AudioCategory, AudioMixerGroup> _categoryGroups;
        private readonly Dictionary<AudioCategory, string> _categoryVolumeParams;
        private readonly List<PooledAudioSource> _pool = new();
        private readonly Transform _root;
        private readonly int _initialPoolSize;
        private readonly MusicPlayer _musicPlayer;
        private readonly ISaves _saves;
        private readonly AudioMixer _mixer;

        private const string MusicVolumeKey = "music_volume";
        private const string SfxVolumeKey = "sfx_volume";
        private const string UiVolumeKey = "ui_volume";

        private readonly Dictionary<AudioCategory, bool> _categoryMuted = new();

        // Новое: отслеживание валидности параметров микшера для категории
        private readonly Dictionary<AudioCategory, bool> _mixerParamValid = new();

        public AudioService(
            MonoBehaviour runner,
            ISaves saves,
            AudioMixer mixer,
            Dictionary<AudioCategory, AudioMixerGroup> categoryGroups,
            Dictionary<AudioCategory, string> categoryVolumeParams,
            int initialPoolSize = 10)
        {
            _saves = saves;
            _mixer = mixer;
            _categoryGroups = categoryGroups;
            _categoryVolumeParams = categoryVolumeParams;
            _initialPoolSize = initialPoolSize;

            _root = new GameObject("[AudioService]").transform;
            Object.DontDestroyOnLoad(_root.gameObject);

            // Проверка конфигурации микшера
            ValidateMixerConfig();

            InitCategory(AudioCategory.Music, MusicVolumeKey, 0.8f);
            InitCategory(AudioCategory.Sfx, SfxVolumeKey, 1f);
            InitCategory(AudioCategory.Ui, UiVolumeKey, 1f);

            _categoryMuted[AudioCategory.Music] = false;
            _categoryMuted[AudioCategory.Sfx] = false;
            _categoryMuted[AudioCategory.Ui] = false;

            // Music source
            var musicGO = new GameObject("MusicSource");
            musicGO.transform.SetParent(_root);
            var musicSource = musicGO.AddComponent<AudioSource>();
            musicSource.loop = true;
            if (_categoryGroups.TryGetValue(AudioCategory.Music, out var musicGroup))
                musicSource.outputAudioMixerGroup = musicGroup;

            _musicPlayer = new MusicPlayer(musicSource, runner);

            // SFX pool
            for (int i = 0; i < _initialPoolSize; i++)
                CreateNewPoolSource();

            // Безопасность для WebGL/браузеров: убеждаемся, что слушатель не приглушен
#if UNITY_WEBGL && !UNITY_EDITOR
            AudioListener.volume = 1f;
#endif
        }

        private void ValidateMixerConfig()
        {
            if (_mixer == null)
            {
                Debug.LogWarning("[AudioService] AudioMixer не задан и категории не будут обрабатываться.");
                // Все категории считаем как «параметр невалиден», будем использовать фоллбек громкости источника
                _mixerParamValid[AudioCategory.Music] = false;
                _mixerParamValid[AudioCategory.Sfx] = false;
                _mixerParamValid[AudioCategory.Ui] = false;
                return;
            }

            foreach (var kv in _categoryVolumeParams)
            {
                bool hasParam = _mixer.GetFloat(kv.Value, out _);
                if (!hasParam)
                {
                    Debug.LogWarning($"[AudioService] В AudioMixer не найден Exposed параметр '{kv.Value}' для категории {kv.Key}. Будет использован фоллбек громкости источника.");
                }
                _mixerParamValid[kv.Key] = hasParam;
            }

            foreach (var kv in _categoryGroups)
            {
                if (kv.Value == null)
                    Debug.LogWarning($"[AudioService] Не настроен AudioMixerGroup для категории {kv.Key}. Проверьте связи в микшере и группах на сцене.");
            }
        }

        private void InitCategory(AudioCategory cat, string saveKey, float defaultLinear)
        {
            float linear = _saves.HasKey(saveKey) ? _saves.GetFloat(saveKey, defaultLinear) : defaultLinear;
            _categoryLinearVolumes[cat] = Mathf.Clamp01(linear);
            ApplyMixerVolume(cat);
        }

        private void CreateNewPoolSource()
        {
            var go = new GameObject("PooledAudioSource");
            go.transform.SetParent(_root);
            var src = go.AddComponent<AudioSource>();
            // Безопасный дефолт: 2D для единообразия громкости UI/SFX
            src.spatialBlend = 0f;
            _pool.Add(new PooledAudioSource { Source = src, Busy = false });
        }

        private PooledAudioSource GetFree()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].Busy || !_pool[i].Source.isPlaying)
                {
                    _pool[i].Busy = true;
                    return _pool[i];
                }
            }
            CreateNewPoolSource();
            var created = _pool[_pool.Count - 1];
            created.Busy = true;
            return created;
        }

        public void Play(AudioEvent audioEvent)
        {
            if (audioEvent == null || audioEvent.Clip == null)
                return;

            if (audioEvent.Category == AudioCategory.Music)
            {
                _musicPlayer.Play(audioEvent);
                return;
            }

            if (IsMutedOrZero(audioEvent.Category))
                return;

            var pooled = GetFree();
            pooled.Configure(audioEvent);

            if (_categoryGroups.TryGetValue(audioEvent.Category, out var group))
                pooled.Source.outputAudioMixerGroup = group;

            // Применяем фоллбек громкости источника, если параметр микшера невалиден
            float categoryScalar = GetEffectiveCategoryScalar(audioEvent.Category);
            pooled.Source.volume = audioEvent.Volume * categoryScalar;

            pooled.Source.Play();

            if (!audioEvent.Loop)
                _root.gameObject.AddComponent<AutoRelease>().Init(pooled);
        }

        public void PlayAt(AudioEvent audioEvent, Vector3 position)
        {
            if (audioEvent == null || audioEvent.Clip == null)
                return;

            if (IsMutedOrZero(audioEvent.Category))
                return;

            var pooled = GetFree();
            pooled.Configure(audioEvent, position);

            if (_categoryGroups.TryGetValue(audioEvent.Category, out var group))
                pooled.Source.outputAudioMixerGroup = group;

            // Для позиционных источников также учитываем категорию через фоллбек
            float categoryScalar = GetEffectiveCategoryScalar(audioEvent.Category);
            pooled.Source.volume = audioEvent.Volume * categoryScalar;

            pooled.Source.Play();

            if (!audioEvent.Loop)
                _root.gameObject.AddComponent<AutoRelease>().Init(pooled);
        }

        public void PlayOneShot(AudioEvent audioEvent)
        {
            if (audioEvent == null || audioEvent.Clip == null)
                return;

            if (IsMutedOrZero(audioEvent.Category))
                return;

            var pooled = GetFree();
            pooled.CurrentEvent = audioEvent;
            pooled.Source.pitch = audioEvent.Pitch;
            pooled.Source.spatialBlend = 0;

            if (_categoryGroups.TryGetValue(audioEvent.Category, out var group))
                pooled.Source.outputAudioMixerGroup = group;

            // Фоллбек: множим громкость клипа на категорию
            float categoryScalar = GetEffectiveCategoryScalar(audioEvent.Category);
            pooled.Source.PlayOneShot(audioEvent.Clip, audioEvent.Volume * categoryScalar);

            _root.gameObject.AddComponent<AutoRelease>().Init(pooled, true);
        }

        public void PlayOneShot(AudioEvent audioEvent, Vector3 position)
        {
            if (audioEvent == null || audioEvent.Clip == null)
                return;

            if (IsMutedOrZero(audioEvent.Category))
                return;

            var pooled = GetFree();
            pooled.CurrentEvent = audioEvent;
            pooled.Source.pitch = audioEvent.Pitch;
            pooled.Source.spatialBlend = 0;

            pooled.Source.transform.position = position;

            if (_categoryGroups.TryGetValue(audioEvent.Category, out var group))
                pooled.Source.outputAudioMixerGroup = group;

            // Фоллбек: множим громкость клипа на категорию
            float categoryScalar = GetEffectiveCategoryScalar(audioEvent.Category);
            pooled.Source.PlayOneShot(audioEvent.Clip, audioEvent.Volume * categoryScalar);

            _root.gameObject.AddComponent<AutoRelease>().Init(pooled, true);

            Debug.Log($"Play one shot settings.\nClip: {audioEvent.Clip.name}\nVolume: {pooled.Source.volume}\n Pitch: {pooled.Source.pitch} \n Spatial Blend: {pooled.Source.spatialBlend}\n Position: {position}");
        }

        private bool IsMutedOrZero(AudioCategory category)
        {
            return (_categoryMuted.TryGetValue(category, out var muted) && muted) ||
                   (_categoryLinearVolumes.TryGetValue(category, out var vol) && vol <= 0.0001f);
        }

        public void Stop(AudioEvent audioEvent)
        {
            if (audioEvent == null) return;

            if (audioEvent.Category == AudioCategory.Music)
            {
                if (_musicPlayer.CurrentEvent == audioEvent)
                    _musicPlayer.Stop();
                return;
            }

            for (int i = 0; i < _pool.Count; i++)
            {
                var p = _pool[i];
                if (p.Busy && p.CurrentEvent == audioEvent)
                {
                    p.Source.Stop();
                    p.Busy = false;
                    p.CurrentEvent = null;
                }
            }
        }

        public void StopAllByCategory(AudioCategory category)
        {
            if (category == AudioCategory.Music)
            {
                if (_musicPlayer.CurrentEvent != null)
                    _musicPlayer.Stop();
                return;
            }

            for (int i = 0; i < _pool.Count; i++)
            {
                var p = _pool[i];
                if (p.Busy && p.CurrentEvent != null && p.CurrentEvent.Category == category)
                {
                    p.Source.Stop();
                    p.Busy = false;
                    p.CurrentEvent = null;
                }
            }
        }

        public void SetCategoryVolume(AudioCategory category, float volume)
        {
            volume = Mathf.Clamp01(volume);
            _categoryLinearVolumes[category] = volume;

            string key = category switch
            {
                AudioCategory.Music => MusicVolumeKey,
                AudioCategory.Sfx => SfxVolumeKey,
                AudioCategory.Ui => UiVolumeKey,
                _ => null
            };
            if (key != null)
                _saves.SetFloat(key, volume);

            ApplyMixerVolume(category);
        }

        public float GetCategoryVolume(AudioCategory category) => _categoryLinearVolumes[category];

        public void SetCategoryMuted(AudioCategory category, bool muted)
        {
            _categoryMuted[category] = muted;
            ApplyMixerVolume(category);
        }

        public bool GetCategoryMuted(AudioCategory category)
        {
            return _categoryMuted.TryGetValue(category, out var muted) && muted;
        }

        // Применение громкости через микшер (с учетом mute)
        private void ApplyMixerVolume(AudioCategory category)
        {
            if (_mixer == null) return;
            if (!_categoryVolumeParams.TryGetValue(category, out var paramName))
                return;

            bool muted = _categoryMuted.TryGetValue(category, out var m) && m;
            float baseLinear = _categoryLinearVolumes[category];
            float effectiveLinear = muted ? 0f : baseLinear;

            float dB = LinearToDecibels(effectiveLinear);
            bool ok = _mixer.SetFloat(paramName, dB);
            _mixerParamValid[category] = ok;

            if (!ok)
                Debug.LogWarning($"[AudioService] Не удалось установить значение параметра '{paramName}' (категория {category}). Проверьте, что параметр Exposed и доступен в билде. Будет использован фоллбек громкости источника.");
        }

        // Возвращает линейный коэффициент категории, если параметр микшера недоступен.
        private float GetEffectiveCategoryScalar(AudioCategory category)
        {
            bool muted = _categoryMuted.TryGetValue(category, out var m) && m;
            float baseLinear = _categoryLinearVolumes.TryGetValue(category, out var v) ? v : 1f;
            float effectiveLinear = muted ? 0f : baseLinear;

            // Если параметр микшера валиден — источниковую громкость не трогаем (вернем 1f)
            if (_mixer != null && _mixerParamValid.TryGetValue(category, out var valid) && valid)
                return 1f;

            // Иначе — применим коэффициент категории на уровне источника
            return effectiveLinear;
        }

        private float LinearToDecibels(float linear)
        {
            if (linear <= 0.0001f)
                return -80f; // mute
            return Mathf.Log10(linear) * 20f;
        }

        private class AutoRelease : MonoBehaviour
        {
            private PooledAudioSource _pooled;
            private bool _playOneShot;

            public void Init(PooledAudioSource pooled, bool playOneShot = false)
            {
                _pooled = pooled;
                _playOneShot = playOneShot;
            }

            private void Update()
            {
                if (_pooled == null)
                {
                    Destroy(this);
                    return;
                }

                bool finished = _playOneShot
                    ? !_pooled.Source.isPlaying
                    : (!_pooled.Source.loop && !_pooled.Source.isPlaying);

                if (finished)
                {
                    _pooled.Busy = false;
                    _pooled.CurrentEvent = null;
                    Destroy(this);
                }
            }
        }
    }
}