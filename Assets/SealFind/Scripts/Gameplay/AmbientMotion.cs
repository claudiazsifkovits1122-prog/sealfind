using UnityEngine;

namespace SealFind
{
    /// <summary>Kleine Lebendigkeit für Deko: Boote schaukeln, Wolken ziehen, Möwen fliegen, Fische schwimmen.</summary>
    public sealed class AmbientMotion : MonoBehaviour
    {
        PropMotion _mode;
        Vector3 _home;
        Vector3 _baseScale;
        Rect _bounds;
        float _phase, _speed, _range, _t;

        public void Init(PropMotion mode, Rect bounds, Rect zone)
        {
            _mode = mode;
            _home = transform.position;
            _baseScale = transform.localScale;
            _bounds = bounds;
            _phase = Random.Range(0f, 10f);
            switch (mode)
            {
                case PropMotion.Drift: _speed = Random.Range(0.12f, 0.3f); break;
                case PropMotion.Fly: _speed = Random.Range(1.2f, 2.0f) * (Random.value < 0.5f ? -1f : 1f); break;
                case PropMotion.Swim:
                    _range = Random.Range(1f, 3f);
                    _speed = Random.Range(0.25f, 0.5f);
                    break;
                case PropMotion.Rise:
                    _speed = Random.Range(0.4f, 0.8f);
                    _range = zone.yMax;
                    break;
            }
            if (mode == PropMotion.Rise) _bounds = zone;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _t += dt;
            var p = transform.position;

            switch (_mode)
            {
                case PropMotion.Bob:
                    p.y = _home.y + Mathf.Sin(_t * 1.2f + _phase) * 0.05f;
                    transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_t * 0.9f + _phase) * 2f);
                    break;
                case PropMotion.Sway:
                    transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_t * 0.8f + _phase) * 2.5f);
                    break;
                case PropMotion.Drift:
                    p.x += _speed * dt;
                    if (p.x > _bounds.xMax + 2.5f) p.x = _bounds.xMin - 2.5f;
                    break;
                case PropMotion.Fly:
                {
                    p.x += _speed * dt;
                    p.y = _home.y + Mathf.Sin(_t * 1.7f + _phase) * 0.3f;
                    if (p.x > _bounds.xMax + 1.5f) p.x = _bounds.xMin - 1.5f;
                    if (p.x < _bounds.xMin - 1.5f) p.x = _bounds.xMax + 1.5f;
                    float flap = 1f + Mathf.Sin(_t * 11f + _phase) * 0.18f;
                    transform.localScale = new Vector3(Mathf.Abs(_baseScale.x) * Mathf.Sign(_speed), _baseScale.y * flap, 1f);
                    break;
                }
                case PropMotion.Swim:
                {
                    float a = _t * _speed + _phase;
                    p.x = _home.x + Mathf.Sin(a) * _range;
                    p.y = _home.y + Mathf.Sin(_t * 2f + _phase) * 0.06f;
                    float dir = Mathf.Cos(a) >= 0f ? 1f : -1f;
                    transform.localScale = new Vector3(Mathf.Abs(_baseScale.x) * dir, _baseScale.y, 1f);
                    break;
                }
                case PropMotion.Rise:
                    p.y += _speed * dt;
                    p.x = _home.x + Mathf.Sin(_t * 2f + _phase) * 0.12f;
                    if (p.y > _range) p.y = _bounds.yMin;
                    break;
            }
            transform.position = p;
        }
    }
}
