using System.Collections.Generic;
using UnityEngine;

namespace SealFind
{
    /// <summary>Spielt Soundeffekte über einen kleinen Pool an AudioSources und kümmert sich um Umgebungsgeräusche.</summary>
    public sealed class AudioManager : MonoBehaviour
    {
        const int Voices = 10;

        readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();
        readonly List<AudioSource> _sources = new List<AudioSource>();
        AudioSource _ambient;
        AudioClip _waves, _underwater;
        int _next;

        public bool SoundOn { get; set; } = true;

        bool _ambienceOn = true;
        public bool AmbienceOn
        {
            get => _ambienceOn;
            set
            {
                _ambienceOn = value;
                if (_ambient) _ambient.mute = !value;
            }
        }

        void Awake()
        {
            for (int i = 0; i < Voices; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                _sources.Add(s);
            }
            _ambient = gameObject.AddComponent<AudioSource>();
            _ambient.loop = true;
            _ambient.playOnAwake = false;
            _ambient.volume = 0f;
        }

        AudioClip Clip(Sfx sfx)
        {
            if (!_clips.TryGetValue(sfx, out var c) || c == null)
            {
                c = SynthAudio.Create(sfx);
                _clips[sfx] = c;
            }
            return c;
        }

        /// <summary>Clips vorab erzeugen, damit beim ersten Fund nichts ruckelt.</summary>
        public void Warmup()
        {
            foreach (Sfx s in System.Enum.GetValues(typeof(Sfx))) Clip(s);
        }

        public void Play(Sfx sfx, float volume = 1f, float pitch = 1f, float pitchJitter = 0.04f)
        {
            if (!SoundOn) return;
            var src = _sources[_next];
            _next = (_next + 1) % _sources.Count;
            src.pitch = pitch * (1f + Random.Range(-pitchJitter, pitchJitter));
            src.volume = volume;
            src.clip = Clip(sfx);
            src.Play();
        }

        public void SetAmbience(WorldDefinition world)
        {
            if (world == null)
            {
                FadeAmbient(0f);
                return;
            }
            var clip = world.Underwater
                ? (_underwater ? _underwater : (_underwater = SynthAudio.Waves(true)))
                : (_waves ? _waves : (_waves = SynthAudio.Waves(false)));
            if (_ambient.clip != clip)
            {
                _ambient.clip = clip;
                _ambient.Play();
            }
            _ambient.mute = !_ambienceOn;
            FadeAmbient(world.Night ? 0.35f : 0.45f);
        }

        void FadeAmbient(float target)
        {
            float start = _ambient.volume;
            Tweener.Run(_ambient, 1.2f, k => _ambient.volume = Mathf.Lerp(start, target, k), Ease.InOutQuad, null, 0f, true);
        }
    }

    public static class Haptics
    {
        public static bool Enabled = true;

        public static void Buzz()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (Enabled) Handheld.Vibrate();
#endif
        }
    }
}
