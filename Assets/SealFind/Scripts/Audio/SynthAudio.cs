using System;
using UnityEngine;

namespace SealFind
{
    public enum Sfx { Plop, Found, FoundRare, Miss, Star, Click, Psst, Win, Lose, Tick, Hint, Whoosh, Giggle, NewSpecies }

    /// <summary>
    /// Erzeugt alle Soundeffekte per Synthese zur Laufzeit – keine Audiodateien nötig.
    /// Später einfach durch echte Clips ersetzen (z.B. über ein ScriptableObject mit AudioClips).
    /// </summary>
    public static class SynthAudio
    {
        const int Rate = 44100;

        public static AudioClip Create(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Plop: return Build("Plop", 0.16f, Plop);
                case Sfx.Found: return Build("Found", 0.32f, (t, i) => Plop(t, i) * 0.6f + Note(t, 0.05f, 880f, 0.12f) + Note(t, 0.13f, 1318f, 0.18f));
                case Sfx.FoundRare: return Build("FoundRare", 0.9f, (t, i) => Arp(t, new[] { 523f, 659f, 784f, 1047f, 1319f }, 0.07f, 0.5f));
                case Sfx.Miss: return Build("Miss", 0.25f, (t, i) => Square(t, Mathf.Lerp(190f, 110f, t / 0.25f)) * Env(t, 0.005f, 0.22f) * 0.25f);
                case Sfx.Star: return Build("Star", 0.6f, (t, i) => Bell(t, 1320f, 0.55f) + Bell(t, 1980f, 0.35f) * 0.3f);
                case Sfx.Click: return Build("Click", 0.05f, (t, i) => Mathf.Sin(t * 2f * Mathf.PI * 1400f) * Env(t, 0.001f, 0.045f) * 0.4f);
                case Sfx.Psst: return Psst();
                case Sfx.Win: return Build("Win", 1.4f, (t, i) => Arp(t, new[] { 523f, 659f, 784f, 1047f }, 0.11f, 0.9f) + (t > 0.45f ? Chord(t - 0.45f) : 0f));
                case Sfx.Lose: return Build("Lose", 0.8f, (t, i) => Note(t, 0f, 392f, 0.3f) + Note(t, 0.25f, 330f, 0.3f) + Note(t, 0.5f, 262f, 0.3f));
                case Sfx.Tick: return Build("Tick", 0.06f, (t, i) => Mathf.Sin(t * 2f * Mathf.PI * 950f) * Env(t, 0.001f, 0.055f) * 0.35f);
                case Sfx.Hint: return Build("Hint", 0.7f, (t, i) => Mathf.Sin(2f * Mathf.PI * (600f * t + 900f * t * t)) * Env(t, 0.02f, 0.65f) * 0.25f);
                case Sfx.Whoosh: return Noise("Whoosh", 0.35f, 0.08f, 0.3f);
                case Sfx.Giggle: return Build("Giggle", 0.45f, (t, i) =>
                {
                    float f = 1050f + Mathf.Sin(t * 2f * Mathf.PI * 18f) * 120f;
                    float gate = Mathf.Sin(t * 2f * Mathf.PI * 7f) > 0f ? 1f : 0f;
                    return Mathf.Sin(2f * Mathf.PI * f * t) * gate * Env(t, 0.01f, 0.43f) * 0.25f;
                });
                case Sfx.NewSpecies: return Build("NewSpecies", 1.0f, (t, i) => Arp(t, new[] { 784f, 988f, 1175f, 1568f }, 0.09f, 0.6f) * 0.9f);
                default: return Build("Silence", 0.05f, (t, i) => 0f);
            }
        }

        /// <summary>Endlos-Loop für Meeresrauschen (Lautstärke-Hüllkurve startet und endet leise → nahtlos).</summary>
        public static AudioClip Waves(bool underwater)
        {
            float len = 8f;
            int n = (int)(len * Rate);
            var data = new float[n];
            var rng = new SeededRandom(1234);
            float lp = 0f, brown = 0f;
            float cutoff = underwater ? 0.012f : 0.05f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                brown = Mathf.Clamp(brown + (rng.Value * 2f - 1f) * 0.05f, -1f, 1f);
                lp += cutoff * (brown - lp);
                float swell = Mathf.Pow(Mathf.Sin(t / len * Mathf.PI * 2f), 2f) * 0.8f + 0.2f;
                data[i] = lp * swell * (underwater ? 1.4f : 0.9f);
            }
            var clip = AudioClip.Create(underwater ? "AmbientUnderwater" : "AmbientWaves", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ------------------------------------------------------------------ Bausteine

        static AudioClip Build(string name, float seconds, Func<float, int, float> f)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate, i), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Env(float t, float attack, float release)
        {
            if (t < attack) return t / attack;
            float r = (t - attack) / release;
            return r >= 1f ? 0f : Mathf.Pow(1f - r, 2f);
        }

        static float Plop(float t, int i)
        {
            float f = Mathf.Lerp(700f, 160f, Mathf.Clamp01(t / 0.12f));
            return Mathf.Sin(2f * Mathf.PI * f * t) * Env(t, 0.004f, 0.14f) * 0.6f;
        }

        static float Square(float t, float f) => Mathf.Sin(2f * Mathf.PI * f * t) > 0f ? 1f : -1f;

        static float Note(float t, float start, float f, float len)
        {
            float lt = t - start;
            if (lt < 0f) return 0f;
            float tri = Mathf.PingPong(lt * f * 2f, 1f) * 2f - 1f;
            return (tri * 0.5f + Mathf.Sin(2f * Mathf.PI * f * lt) * 0.5f) * Env(lt, 0.005f, len) * 0.3f;
        }

        static float Bell(float t, float f, float len) =>
            (Mathf.Sin(2f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * f * 2.7f * t)) * Mathf.Exp(-t / (len * 0.35f)) * 0.3f;

        static float Arp(float t, float[] notes, float step, float tail)
        {
            float s = 0f;
            for (int k = 0; k < notes.Length; k++) s += Note(t, k * step, notes[k], k == notes.Length - 1 ? tail : step * 2.5f);
            return s;
        }

        static float Chord(float t) => (Note(t, 0f, 523f, 0.9f) + Note(t, 0f, 659f, 0.9f) + Note(t, 0f, 784f, 0.9f)) * 0.6f;

        static AudioClip Noise(string name, float seconds, float cutoff, float gain)
        {
            var rng = new SeededRandom(name.GetHashCode());
            float lp = 0f;
            return Build(name, seconds, (t, i) =>
            {
                lp += cutoff * ((rng.Value * 2f - 1f) - lp);
                float env = Mathf.Sin(Mathf.Clamp01(t / seconds) * Mathf.PI);
                return lp * env * gain * 4f;
            });
        }

        static AudioClip Psst()
        {
            var rng = new SeededRandom(99);
            float prev = 0f;
            return Build("Psst", 0.5f, (t, i) =>
            {
                float white = rng.Value * 2f - 1f;
                float hp = white - prev; // grober Hochpass → "sss"
                prev = white;
                float env = t < 0.04f ? t / 0.04f : t < 0.32f ? 1f : Mathf.Max(0f, 1f - (t - 0.32f) / 0.08f);
                float pop = t > 0.42f && t < 0.45f ? Mathf.Sin(t * 2f * Mathf.PI * 2200f) * 0.4f : 0f;
                return hp * env * 0.18f + pop;
            });
        }
    }
}
