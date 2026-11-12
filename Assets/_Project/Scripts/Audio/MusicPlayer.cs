using System.Collections;
using UnityEngine;

namespace _Project.Scripts.Audio
{
    public class MusicPlayer
    {
        private readonly AudioSource _source;
        private Coroutine _fadeRoutine;
        private readonly MonoBehaviour _runner;

        public AudioEvent CurrentEvent { get; private set; }

        public MusicPlayer(AudioSource source, MonoBehaviour runner)
        {
            _source = source;
            _runner = runner;
        }

        public void Play(AudioEvent audioEvent, float fadeTime = 0.5f)
        {
            if (audioEvent == null || audioEvent.Clip == null)
                return;

            if (_source.clip == audioEvent.Clip && _source.isPlaying)
                return;

            CurrentEvent = audioEvent;

            if (_fadeRoutine != null)
                _runner.StopCoroutine(_fadeRoutine);

            _fadeRoutine = _runner.StartCoroutine(FadeTo(audioEvent, fadeTime));
        }

        public void Stop(float fadeTime = 0.3f)
        {
            if (!_source.isPlaying)
            {
                CurrentEvent = null;
                return;
            }

            if (_fadeRoutine != null)
                _runner.StopCoroutine(_fadeRoutine);

            _fadeRoutine = _runner.StartCoroutine(FadeOutAndStop(fadeTime));
        }

        private IEnumerator FadeTo(AudioEvent evt, float time)
        {
            float startVol = _source.volume;
            // fade out
            for (float t = 0; t < time; t += Time.unscaledDeltaTime)
            {
                _source.volume = Mathf.Lerp(startVol, 0, t / time);
                yield return null;
            }
            _source.volume = 0;
            _source.clip = evt.Clip;
            _source.loop = evt.Loop || true; // музыка обычно должна лупиться
            _source.pitch = evt.Pitch;
            _source.Play();
            float targetVol = evt.Volume;
            // fade in
            for (float t = 0; t < time; t += Time.unscaledDeltaTime)
            {
                _source.volume = Mathf.Lerp(0, targetVol, t / time);
                yield return null;
            }
            _source.volume = targetVol;
            _fadeRoutine = null;
        }

        private IEnumerator FadeOutAndStop(float time)
        {
            float startVol = _source.volume;
            for (float t = 0; t < time; t += Time.unscaledDeltaTime)
            {
                _source.volume = Mathf.Lerp(startVol, 0, t / time);
                yield return null;
            }
            _source.volume = 0;
            _source.Stop();
            CurrentEvent = null;
            _fadeRoutine = null;
        }
    }
}