using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SealFind
{
    /// <summary>
    /// Einheitliche Eingabe für Maus und Touch:
    /// Tippen (= suchen), Ziehen (= Bild verschieben), Pinch/Mausrad (= Zoom).
    /// Ein Tipp zählt nur, wenn der Finger sich kaum bewegt hat – so löst Verschieben keine Fehlklicks aus.
    /// </summary>
    public sealed class PointerInput : MonoBehaviour
    {
        public event Action<Vector2> Tapped;

        public CameraController Cam;
        public bool Enabled;

        Vector2 _start, _last;
        bool _down, _dragging, _overUi, _multi;
        float _pinchDistance;

        float DragThreshold => (Screen.dpi > 0 ? Screen.dpi : 160f) * 0.1f;

        void Awake()
        {
            Input.simulateMouseWithTouches = false;
        }

        void Update()
        {
            if (!Enabled || Cam == null)
            {
                _down = false;
                return;
            }

            if (Input.touchCount > 0) HandleTouches();
            else HandleMouse();
        }

        void HandleTouches()
        {
            if (Input.touchCount >= 2)
            {
                var t0 = Input.GetTouch(0);
                var t1 = Input.GetTouch(1);
                float dist = Vector2.Distance(t0.position, t1.position);
                var mid = (t0.position + t1.position) * 0.5f;
                if (!_multi || t1.phase == TouchPhase.Began)
                {
                    _multi = true;
                    _pinchDistance = dist;
                    _last = mid;
                    Cam.BeginDrag();
                }
                if (_pinchDistance > 1f && dist > 1f) Cam.ZoomAt(mid, _pinchDistance / dist);
                Cam.Pan(mid - _last);
                _pinchDistance = dist;
                _last = mid;
                _dragging = true;
                return;
            }

            var t = Input.GetTouch(0);
            switch (t.phase)
            {
                case TouchPhase.Began:
                    Press(t.position, IsOverUi(t.fingerId));
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (_multi)
                    {
                        // nach Pinch: Referenzpunkt neu setzen, damit es nicht springt
                        _multi = false;
                        _last = t.position;
                    }
                    Move(t.position);
                    break;
                case TouchPhase.Ended:
                    Release(t.position);
                    _multi = false;
                    break;
                case TouchPhase.Canceled:
                    _down = false;
                    _multi = false;
                    Cam.EndDrag();
                    break;
            }
        }

        void HandleMouse()
        {
            _multi = false;
            var pos = (Vector2)Input.mousePosition;
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                Press(pos, IsOverUi(-1));
            else if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
                Move(pos);
            if (Input.GetMouseButtonUp(0))
                Release(pos);
            else if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
            {
                _down = false;
                Cam.EndDrag();
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f && !IsOverUi(-1))
                Cam.ZoomAt(pos, Mathf.Pow(0.88f, scroll));
        }

        void Press(Vector2 pos, bool overUi)
        {
            _down = true;
            _dragging = false;
            _overUi = overUi;
            _start = _last = pos;
            if (!overUi) Cam.BeginDrag();
        }

        void Move(Vector2 pos)
        {
            if (!_down || _overUi) return;
            if (!_dragging && Vector2.Distance(pos, _start) > DragThreshold) _dragging = true;
            if (_dragging) Cam.Pan(pos - _last);
            _last = pos;
        }

        void Release(Vector2 pos)
        {
            if (!_down) return;
            _down = false;
            Cam.EndDrag();
            if (!_overUi && !_dragging) Tapped?.Invoke(pos);
        }

        static bool IsOverUi(int pointerId)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            return pointerId >= 0 ? es.IsPointerOverGameObject(pointerId) : es.IsPointerOverGameObject();
        }
    }
}
