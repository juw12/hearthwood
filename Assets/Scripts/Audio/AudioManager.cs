using System.Collections.Generic;
using UnityEngine;

namespace Hearthwood.Audio
{
    /// <summary>
    /// Singleton audio router. Owns dedicated SFX pool and music source. Scene-scoped
    /// (no DontDestroyOnLoad) — the AudioManager is rebuilt per scene so AudioMixer
    /// routing can change with scene context.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Pools")]
        [SerializeField, Range(2, 32), Tooltip("Number of pooled AudioSources for SFX.")]
        private int _sfxPoolSize = 12;

        [Header("Defaults")]
        [SerializeField, Range(0f, 1f)]
        private float _masterVolume = 1f;

        [SerializeField, Range(0f, 1f)]
        private float _sfxVolume = 1f;

        [SerializeField, Range(0f, 1f)]
        private float _musicVolume = 1f;

        private readonly List<AudioSource> _sfxPool = new();
        private AudioSource _musicSource;

        public float MasterVolume { get => _masterVolume; set { _masterVolume = Mathf.Clamp01(value); ApplyVolumes(); } }
        public float SfxVolume { get => _sfxVolume; set { _sfxVolume = Mathf.Clamp01(value); ApplyVolumes(); } }
        public float MusicVolume { get => _musicVolume; set { _musicVolume = Mathf.Clamp01(value); ApplyVolumes(); } }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            BuildPool();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BuildPool()
        {
            for (int i = 0; i < _sfxPoolSize; i++)
            {
                var go = new GameObject($"SFX_{i:00}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _sfxPool.Add(src);
            }
            var musicGo = new GameObject("Music");
            musicGo.transform.SetParent(transform);
            _musicSource = musicGo.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;
        }

        /// <summary>Play a 3D-positioned one-shot SFX. Returns the AudioSource used (for tracking).</summary>
        public AudioSource PlaySFX(AudioClip clip, Vector3 position = default, float volumeScale = 1f)
        {
            if (clip == null) return null;
            var src = GetFreeSource();
            if (src == null) return null;
            src.transform.position = position;
            src.spatialBlend = 1f;
            src.clip = clip;
            src.volume = _masterVolume * _sfxVolume * Mathf.Clamp01(volumeScale);
            src.Play();
            return src;
        }

        /// <summary>Play a non-spatial SFX (UI clicks, dialogue beeps).</summary>
        public void PlaySFX2D(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            var src = GetFreeSource();
            if (src == null) return;
            src.spatialBlend = 0f;
            src.clip = clip;
            src.volume = _masterVolume * _sfxVolume * Mathf.Clamp01(volumeScale);
            src.PlayOneShot(clip, src.volume);
        }

        /// <summary>Play looping music, replacing the current track.</summary>
        public void PlayMusic(AudioClip clip, float fadeSeconds = 0.5f)
        {
            if (_musicSource == null) return;
            if (clip == null) { _musicSource.Stop(); return; }
            _musicSource.clip = clip;
            _musicSource.volume = _masterVolume * _musicVolume;
            _musicSource.Play();
        }

        private AudioSource GetFreeSource()
        {
            for (int i = 0; i < _sfxPool.Count; i++) if (!_sfxPool[i].isPlaying) return _sfxPool[i];
            // All busy — recycle the oldest by stopping and returning index 0.
            if (_sfxPool.Count == 0) return null;
            _sfxPool[0].Stop();
            return _sfxPool[0];
        }

        private void ApplyVolumes()
        {
            if (_musicSource != null && _musicSource.isPlaying)
                _musicSource.volume = _masterVolume * _musicVolume;
        }
    }
}
