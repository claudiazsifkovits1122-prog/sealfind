using System;
using System.Collections.Generic;
using UnityEngine;

namespace SealFind
{
    public enum Ease { Linear, InQuad, OutQuad, InOutQuad, OutCubic, OutBack, InBack, OutElastic, OutBounce }

    /// <summary>
    /// Minimaler Tween-Runner (ohne externe Pakete wie DOTween).
    /// Tweens hängen an einem "Owner"-Objekt und sterben automatisch mit ihm.
    /// </summary>
    public sealed class Tweener : MonoBehaviour
    {
        sealed class Tween
        {
            public UnityEngine.Object Owner;
            public bool HasOwner;
            public float Duration, Time, Delay;
            public Ease Ease;
            public Action<float> Step;
            public Action Done;
            public bool Unscaled;
            public bool Dead;
        }

        static Tweener _instance;
        readonly List<Tween> _tweens = new List<Tween>();
        readonly List<Tween> _pending = new List<Tween>();

        static Tweener Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[Tweener]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<Tweener>();
                }
                return _instance;
            }
        }

        public static void Run(UnityEngine.Object owner, float duration, Action<float> step, Ease ease = Ease.OutQuad,
            Action done = null, float delay = 0f, bool unscaled = false)
        {
            Instance._pending.Add(new Tween
            {
                Owner = owner, HasOwner = owner != null, Duration = Mathf.Max(0.0001f, duration),
                Step = step, Ease = ease, Done = done, Delay = delay, Unscaled = unscaled,
            });
        }

        public static void Kill(UnityEngine.Object owner)
        {
            if (_instance == null || owner == null) return;
            foreach (var t in _instance._tweens) if (t.Owner == owner) t.Dead = true;
            foreach (var t in _instance._pending) if (t.Owner == owner) t.Dead = true;
        }

        public static void Delay(UnityEngine.Object owner, float seconds, Action action, bool unscaled = false) =>
            Run(owner, 0.0001f, _ => { }, Ease.Linear, action, seconds, unscaled);

        // ------------------------------------------------------------------ Helfer

        public static void Scale(Transform t, Vector3 from, Vector3 to, float duration, Ease ease = Ease.OutBack, Action done = null, float delay = 0f, bool unscaled = false)
        {
            t.localScale = from;
            Run(t, duration, k => { if (t) t.localScale = Vector3.LerpUnclamped(from, to, k); }, ease, done, delay, unscaled);
        }

        public static void Move(Transform t, Vector3 from, Vector3 to, float duration, Ease ease = Ease.OutQuad, Action done = null, float delay = 0f, bool unscaled = false)
        {
            Run(t, duration, k => { if (t) t.localPosition = Vector3.LerpUnclamped(from, to, k); }, ease, done, delay, unscaled);
        }

        public static void Fade(CanvasGroup g, float from, float to, float duration, Action done = null, float delay = 0f)
        {
            g.alpha = from;
            Run(g, duration, k => { if (g) g.alpha = Mathf.LerpUnclamped(from, to, k); }, Ease.OutQuad, done, delay, true);
        }

        /// <summary>Kurzes "Aufpoppen" (z.B. Treffer-Feedback).</summary>
        public static void Punch(Transform t, float amount = 0.25f, float duration = 0.35f, bool unscaled = false)
        {
            var baseScale = t.localScale;
            Run(t, duration, k =>
            {
                if (!t) return;
                float s = 1f + Mathf.Sin(k * Mathf.PI) * amount * (1f - k * 0.5f);
                t.localScale = baseScale * s;
            }, Ease.Linear, () => { if (t) t.localScale = baseScale; }, 0f, unscaled);
        }

        public static void Shake(Transform t, float strength, float duration, bool unscaled = true)
        {
            var basePos = t.localPosition;
            Run(t, duration, k =>
            {
                if (!t) return;
                float s = strength * (1f - k);
                t.localPosition = basePos + new Vector3(Mathf.Sin(k * 60f) * s, Mathf.Cos(k * 47f) * s * 0.5f, 0f);
            }, Ease.Linear, () => { if (t) t.localPosition = basePos; }, 0f, unscaled);
        }

        // ------------------------------------------------------------------ Kurven

        public static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.OutBack:
                {
                    const float c1 = 1.70158f, c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                }
                case Ease.InBack:
                {
                    const float c1 = 1.70158f, c3 = c1 + 1f;
                    return c3 * t * t * t - c1 * t * t;
                }
                case Ease.OutElastic:
                {
                    if (t <= 0f || t >= 1f) return t;
                    const float c4 = 2f * Mathf.PI / 3f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
                }
                case Ease.OutBounce:
                {
                    const float n1 = 7.5625f, d1 = 2.75f;
                    if (t < 1f / d1) return n1 * t * t;
                    if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
                    if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
                    t -= 2.625f / d1;
                    return n1 * t * t + 0.984375f;
                }
                default: return t;
            }
        }

        void Update()
        {
            if (_pending.Count > 0)
            {
                _tweens.AddRange(_pending);
                _pending.Clear();
            }

            for (int i = 0; i < _tweens.Count; i++)
            {
                var t = _tweens[i];
                if (t.Dead || (t.HasOwner && t.Owner == null))
                {
                    t.Dead = true;
                    continue;
                }
                float dt = t.Unscaled ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime;
                if (t.Delay > 0f)
                {
                    t.Delay -= dt;
                    continue;
                }
                t.Time += dt;
                float k = Mathf.Clamp01(t.Time / t.Duration);
                try
                {
                    t.Step(Evaluate(t.Ease, k));
                    if (k >= 1f)
                    {
                        t.Dead = true;
                        t.Done?.Invoke();
                    }
                }
                catch (Exception e)
                {
                    t.Dead = true;
                    Debug.LogException(e);
                }
            }
            _tweens.RemoveAll(t => t.Dead);
        }
    }
}
