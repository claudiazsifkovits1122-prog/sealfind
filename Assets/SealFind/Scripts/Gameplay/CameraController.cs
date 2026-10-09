using UnityEngine;

namespace SealFind
{
    /// <summary>
    /// Orthografische Kamera mit Pan, Pinch-/Mausrad-Zoom, Trägheit und sanften Kamerafahrten.
    /// Die Kamera zeigt nie etwas außerhalb des Bildes ("cover"-Verhalten wie bei einem Hintergrundbild).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraController : MonoBehaviour
    {
        public Camera Cam { get; private set; }

        Rect _bounds = new Rect(-16f, -9f, 32f, 18f);
        float _minSize = 2.6f;
        float _maxSize = 9f;
        Vector2 _velocity;
        bool _dragging;
        bool _autoMoving;

        /// <summary>Menü-Hintergrund: Kamera schwebt langsam hin und her.</summary>
        public bool AttractMode { get; set; }
        float _attractT;

        public float ZoomFactor => _maxSize / Cam.orthographicSize;

        void Awake()
        {
            Cam = GetComponent<Camera>();
            Cam.orthographic = true;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color(0.1f, 0.12f, 0.16f);
        }

        public void Setup(Rect bounds)
        {
            _bounds = bounds;
            RecalculateLimits();
            Cam.orthographicSize = _maxSize;
            transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
            _velocity = Vector2.zero;
        }

        void RecalculateLimits()
        {
            float aspect = Mathf.Max(0.1f, Cam.aspect);
            _maxSize = Mathf.Min(_bounds.height * 0.5f, _bounds.width * 0.5f / aspect);
            _minSize = Mathf.Min(_maxSize, 2.6f);
        }

        public Vector2 ScreenToWorld(Vector2 screen) => Cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));

        /// <summary>Wie viele Welteinheiten ein Bildschirmpixel gerade sind.</summary>
        public float WorldPerPixel => Cam.orthographicSize * 2f / Mathf.Max(1, Screen.height);

        public void BeginDrag()
        {
            _dragging = true;
            _autoMoving = false;
            Tweener.Kill(this);
            _velocity = Vector2.zero;
        }

        public void Pan(Vector2 screenDelta)
        {
            var delta = -screenDelta * WorldPerPixel;
            transform.position += new Vector3(delta.x, delta.y, 0f);
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.001f);
            _velocity = Vector2.Lerp(_velocity, delta / dt, 0.4f);
            Clamp();
        }

        public void EndDrag() => _dragging = false;

        public void ZoomAt(Vector2 screenPos, float factor)
        {
            _autoMoving = false;
            Tweener.Kill(this);
            var before = ScreenToWorld(screenPos);
            Cam.orthographicSize = Mathf.Clamp(Cam.orthographicSize * factor, _minSize, _maxSize);
            var after = ScreenToWorld(screenPos);
            transform.position += (Vector3)(before - after);
            Clamp();
        }

        public void FocusOn(Vector2 world, float size, float duration = 0.8f)
        {
            Tweener.Kill(this);
            _autoMoving = true;
            _velocity = Vector2.zero;
            Vector3 startPos = transform.position;
            float startSize = Cam.orthographicSize;
            float targetSize = Mathf.Clamp(size, _minSize, _maxSize);
            Tweener.Run(this, duration, k =>
            {
                Cam.orthographicSize = Mathf.Lerp(startSize, targetSize, k);
                var p = Vector3.Lerp(startPos, new Vector3(world.x, world.y, -10f), k);
                transform.position = p;
                Clamp();
            }, Ease.InOutQuad, () => _autoMoving = false, 0f, true);
        }

        public void ZoomOutFull(float duration = 0.8f) => FocusOn(_bounds.center, _maxSize, duration);

        void LateUpdate()
        {
            float before = _maxSize;
            RecalculateLimits(); // Bildschirm gedreht / Fenstergröße geändert
            if (!Mathf.Approximately(before, _maxSize))
                Cam.orthographicSize = Mathf.Clamp(Cam.orthographicSize, _minSize, _maxSize);

            float dt = Time.unscaledDeltaTime;
            if (AttractMode)
            {
                _attractT += dt;
                Cam.orthographicSize = Mathf.Lerp(Cam.orthographicSize, _maxSize * 0.82f, dt);
                var target = new Vector3(_bounds.center.x + Mathf.Sin(_attractT * 0.07f) * 6f, _bounds.center.y - 1f, -10f);
                transform.position = Vector3.Lerp(transform.position, target, dt * 0.5f);
            }
            else if (!_dragging && !_autoMoving && _velocity.sqrMagnitude > 0.0001f)
            {
                transform.position += (Vector3)(_velocity * dt);
                _velocity *= Mathf.Exp(-6f * dt);
            }
            Clamp();
        }

        void Clamp()
        {
            float h = Cam.orthographicSize, w = h * Cam.aspect;
            var p = transform.position;
            p.x = _bounds.width <= w * 2f ? _bounds.center.x : Mathf.Clamp(p.x, _bounds.xMin + w, _bounds.xMax - w);
            p.y = _bounds.height <= h * 2f ? _bounds.center.y : Mathf.Clamp(p.y, _bounds.yMin + h, _bounds.yMax - h);
            p.z = -10f;
            transform.position = p;
        }
    }
}
