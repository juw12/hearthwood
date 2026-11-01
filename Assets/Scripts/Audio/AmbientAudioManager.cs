using System.Collections;
using UnityEngine;

namespace Hearthwood.Audio
{
    /// <summary>
    /// Plays ambient soundscapes (wind, crickets, distant forge clang) for a scene.
    /// Crossfades between ambient layers when the scene or time-of-day changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AmbientAudioManager : MonoBehaviour
    {
        [System.Serializable]
        public struct AmbientLayer
        {
            [Tooltip("Layer id used by SetActiveLayer.")]
            public string Id;
            public AudioClip Clip;
            [Range(0f, 1f)] public float Volume;
        }

        [Header("Layers")]
        [SerializeField, Tooltip("Ambient layers available. Pick by id at runtime.")]
        private AmbientLayer[] _layers = System.Array.Empty<AmbientLayer>();

        [SerializeField, Tooltip("Initial active layer id. Empty = none.")]
        private string _initialLayerId = "";

        [SerializeField, Range(0f, 5f), Tooltip("Default crossfade duration.")]
        private float _defaultCrossfadeSeconds = 1.5f;

        private AudioSource _a;
        private AudioSource _b;
        private string _activeId;
        private Coroutine _fade;

        private void Awake()
        {
            _a = CreateSource("AmbientA");
            _b = CreateSource("AmbientB");
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(_initialLayerId)) SetActiveLayer(_initialLayerId, 0f);
        }

        private AudioSource CreateSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var s = go.AddComponent<AudioSource>();
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            return s;
        }

        /// <summary>Crossfade to the named layer over N seconds. Pass empty id to fade to silence.</summary>
        public void SetActiveLayer(string id, float crossfade = -1f)
        {
            if (crossfade < 0f) crossfade = _defaultCrossfadeSeconds;
            AmbientLayer? found = null;
            foreach (var l in _layers) if (l.Id == id) { found = l; break; }
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(CrossfadeRoutine(found, crossfade));
            _activeId = id ?? "";
        }

        private IEnumerator CrossfadeRoutine(AmbientLayer? target, float duration)
        {
            // Identify the "outgoing" source — the one currently louder.
            AudioSource outgoing = _a.volume > _b.volume ? _a : _b;
            AudioSource incoming = outgoing == _a ? _b : _a;
            // Configure incoming.
            if (target.HasValue && target.Value.Clip != null)
            {
                incoming.clip = target.Value.Clip;
                incoming.volume = 0f;
                incoming.Play();
            }
            float startOut = outgoing.volume;
            float targetIn = target.HasValue ? Mathf.Clamp01(target.Value.Volume) : 0f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = duration <= 0f ? 1f : t / duration;
                outgoing.volume = Mathf.Lerp(startOut, 0f, k);
                incoming.volume = Mathf.Lerp(0f, targetIn, k);
                yield return null;
            }
            outgoing.Stop();
            outgoing.volume = 0f;
            incoming.volume = targetIn;
            _fade = null;
        }
    }
}
