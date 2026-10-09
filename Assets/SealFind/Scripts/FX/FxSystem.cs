using System.Collections.Generic;
using UnityEngine;

namespace SealFind
{
    /// <summary>
    /// Leichtgewichtiges Partikelsystem mit Sprite-Pool: Konfetti, Funken, Spritzer, Zzz …
    /// Bewusst ohne Unitys ParticleSystem, damit alles im Code nachvollziehbar bleibt.
    /// </summary>
    public sealed class FxSystem : MonoBehaviour
    {
        public const int SortingOrder = 30000;

        sealed class Particle
        {
            public Transform T;
            public SpriteRenderer R;
            public Vector3 Velocity;
            public float Life, Age, Gravity, Spin, Drag;
            public float StartScale, EndScale;
            public Color Color;
            public bool Active;
        }

        readonly List<Particle> _particles = new List<Particle>();
        ArtLibrary _art;

        static readonly Color[] Party =
        {
            new Color(1f, 0.36f, 0.45f), new Color(1f, 0.8f, 0.2f), new Color(0.3f, 0.75f, 1f),
            new Color(0.45f, 0.9f, 0.45f), new Color(0.8f, 0.5f, 1f), Color.white,
        };

        public void Init(ArtLibrary art) => _art = art;

        Particle Spawn(Sprite sprite, Vector2 pos, Color color, float scale, float life)
        {
            Particle p = null;
            foreach (var q in _particles)
            {
                if (!q.Active) { p = q; break; }
            }
            if (p == null)
            {
                var go = new GameObject("Fx");
                go.transform.SetParent(transform, false);
                p = new Particle { T = go.transform, R = go.AddComponent<SpriteRenderer>() };
                p.R.sortingOrder = SortingOrder;
                _particles.Add(p);
            }
            p.Active = true;
            p.T.gameObject.SetActive(true);
            p.R.sprite = sprite;
            p.R.color = color;
            p.Color = color;
            p.T.position = new Vector3(pos.x, pos.y, 0f);
            p.T.rotation = Quaternion.identity;
            p.StartScale = scale;
            p.EndScale = scale * 0.2f;
            p.T.localScale = Vector3.one * scale;
            p.Life = life;
            p.Age = 0f;
            p.Velocity = Vector3.zero;
            p.Gravity = 0f;
            p.Spin = 0f;
            p.Drag = 0f;
            return p;
        }

        public void Burst(Vector2 pos, int count, float speed = 3.5f, float size = 0.2f, Color[] colors = null)
        {
            colors = colors ?? Party;
            for (int i = 0; i < count; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                var sprite = i % 3 == 0 ? _art.Star : _art.Circle;
                var p = Spawn(sprite, pos, colors[i % colors.Length], size * Random.Range(0.6f, 1.2f), Random.Range(0.45f, 0.8f));
                p.Velocity = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * speed * Random.Range(0.5f, 1f);
                p.Gravity = -6f;
                p.Drag = 2.5f;
                p.Spin = Random.Range(-360f, 360f);
            }
        }

        public void Sparkle(Vector2 pos, float radius, Color color)
        {
            var p = Spawn(_art.Sparkle, pos + Random.insideUnitCircle * radius, color, Random.Range(0.15f, 0.3f), 0.7f);
            p.Spin = 90f;
            p.EndScale = 0f;
        }

        public void Splash(Vector2 pos, Color water)
        {
            var light = Color.Lerp(water, Color.white, 0.6f);
            for (int i = 0; i < 10; i++)
            {
                var p = Spawn(_art.Circle, pos + new Vector2(Random.Range(-0.3f, 0.3f), 0f), light, Random.Range(0.06f, 0.12f), Random.Range(0.4f, 0.7f));
                p.Velocity = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(2f, 4f), 0f);
                p.Gravity = -12f;
            }
        }

        public void Zzz(Vector2 pos)
        {
            var p = Spawn(_art.ZIcon, pos, new Color(1f, 1f, 1f, 0.9f), 0.12f, 1.8f);
            p.Velocity = new Vector3(0.25f, 0.55f, 0f);
            p.EndScale = 0.3f;
        }

        public void MissMark(Vector2 pos)
        {
            var p = Spawn(_art.Cross, pos, new Color(1f, 0.3f, 0.3f), 0.45f, 0.6f);
            p.EndScale = 0.3f;
        }

        public void Pulse(Vector2 pos, Color color, float radius, float life = 0.9f)
        {
            // Ring-Sprite: Radius 0.56 Einheiten bei Skalierung 1
            var p = Spawn(_art.Ring, pos, color, radius * 0.3f / 0.56f, life);
            p.EndScale = radius / 0.56f;
        }

        public void Confetti(Camera cam, int count = 80)
        {
            float h = cam.orthographicSize, w = h * cam.aspect;
            var c = (Vector2)cam.transform.position;
            for (int i = 0; i < count; i++)
            {
                var pos = c + new Vector2(Random.Range(-w, w), h + Random.Range(0.2f, 2.5f));
                var p = Spawn(_art.Square, pos, Party[i % Party.Length], Random.Range(0.12f, 0.22f) * h / 6f, Random.Range(2.2f, 3.4f));
                p.Velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-3f, -1f), 0f) * h / 6f;
                p.Gravity = -1.5f * h / 6f;
                p.Spin = Random.Range(-540f, 540f);
                p.EndScale = p.StartScale;
            }
        }

        public void Clear()
        {
            foreach (var p in _particles)
            {
                p.Active = false;
                p.T.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var p in _particles)
            {
                if (!p.Active) continue;
                p.Age += dt;
                float k = p.Age / p.Life;
                if (k >= 1f)
                {
                    p.Active = false;
                    p.T.gameObject.SetActive(false);
                    continue;
                }
                p.Velocity += Vector3.up * p.Gravity * dt;
                p.Velocity *= Mathf.Max(0f, 1f - p.Drag * dt);
                p.T.position += p.Velocity * dt;
                p.T.Rotate(0f, 0f, p.Spin * dt);
                p.T.localScale = Vector3.one * Mathf.Lerp(p.StartScale, p.EndScale, k);
                var c = p.Color;
                c.a *= k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
                p.R.color = c;
            }
        }
    }
}
