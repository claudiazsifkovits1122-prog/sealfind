using UnityEngine;

namespace SealFind
{
    /// <summary>
    /// Eine versteckte Robbe in der Szene: Animationen (atmen, blinzeln, abtauchen, schwimmen),
    /// Trefferprüfung und die kleinen Charakter-Momente ("Hihi!", Zzz, Wackeln beim Tipp).
    /// </summary>
    public sealed class Seal : MonoBehaviour
    {
        enum PeekState { Up, GoingDown, Down, GoingUp }

        public SealSpecies Species { get; private set; }
        public SealPlacement Placement { get; private set; }
        public bool IsFound { get; private set; }
        public bool IsTarget => Placement.IsTarget;
        public bool IsBonus => Placement.IsBonus;

        SealRig _rig;
        Transform _visual;
        SpriteRenderer _body, _face, _acc, _cover, _marker;
        ArtLibrary _art;
        FxSystem _fx;
        AudioManager _audio;
        WorldDefinition _world;

        float _scale;
        float _t, _phase;
        float _blinkIn, _blinkLeft, _happyLeft;
        float _zTimer, _sparkTimer;
        float _wiggle;
        float _hop;
        bool _sleeping;

        PeekState _peek = PeekState.Up;
        float _peekTimer;
        float _visibility = 1f;
        float _coverLine;

        Vector2 _home;
        float _movePhase;
        bool _tricked;
        bool _busy;

        public float Visibility => _visibility;

        public Vector2 HeadWorld => _visual.TransformPoint(_rig.HeadLocal);

        public Vector2 CenterWorld => Placement.HasWaterCover
            ? HeadWorld
            : (Vector2)_visual.TransformPoint((_rig.HeadLocal + _rig.BodyLocal) * 0.5f);

        public float WorldRadius => 0.7f * _scale;

        public void Init(SealSpecies species, SealPlacement placement, WorldDefinition world, ArtLibrary art, FxSystem fx, AudioManager audio)
        {
            Species = species;
            Placement = placement;
            _world = world;
            _art = art;
            _fx = fx;
            _audio = audio;
            _rig = SealRig.For(species.IsBaby);
            _scale = placement.Scale;
            _home = placement.Position;
            _sleeping = placement.Sleeping;
            _phase = Random.Range(0f, 10f);
            _movePhase = Random.Range(0f, Mathf.PI * 2f);
            _blinkIn = Random.Range(1f, 5f);
            _zTimer = Random.Range(0.5f, 2.5f);
            _peekTimer = Random.Range(0.5f, 3f);

            transform.position = placement.Position;
            transform.localScale = new Vector3(placement.Flip ? -_scale : _scale, _scale, 1f);

            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);

            var env = world.EnvironmentColorAt(placement.Position.y);
            Color tint = Color.Lerp(species.BodyColor, env, placement.Camouflage);
            tint *= world.Ambient;
            tint.a = 1f;

            _body = MakeRenderer("Body", _visual, art.SealBody(species.IsBaby), tint);
            _face = MakeRenderer("Face", _visual, art.SealFaceSprite(species.IsBaby, _sleeping ? SealFace.Blink : SealFace.Open), world.Ambient);
            var accSprite = art.SealAccessory(species.Accessory, species.IsBaby);
            if (accSprite != null) _acc = MakeRenderer("Accessory", _visual, accSprite, world.Ambient);

            _coverLine = _rig.HeadLocal.y - _rig.HeadRadiusLocal * 0.55f;
            if (placement.HasWaterCover)
            {
                var water = world.WaterColorAt(placement.Position.y + _coverLine * _scale);
                water.a = 1f;
                _cover = MakeRenderer("WaterCover", transform, art.WaterCover, water);
                _cover.transform.localPosition = new Vector3(0f, _coverLine, 0f);
                _cover.transform.localScale = new Vector3(1.05f, placement.Style == HidingStyle.Peek ? 1.6f : 1.1f, 1f);
            }
            UpdateSorting();
        }

        static SpriteRenderer MakeRenderer(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
            return r;
        }

        void UpdateSorting()
        {
            int order = SceneBuilder.SortFor(transform.position.y);
            _body.sortingOrder = order;
            _face.sortingOrder = order + 1;
            if (_acc) _acc.sortingOrder = order + 2;
            if (_cover) _cover.sortingOrder = order + 3;
        }

        // ------------------------------------------------------------------ Trefferprüfung

        public bool HitTest(Vector2 worldPos, float tolerance)
        {
            if (IsFound || _busy) return false;
            if (Placement.Style == HidingStyle.Peek && _visibility < 0.4f) return false;

            Vector2 local = _visual.InverseTransformPoint(worldPos);
            float tol = tolerance / _scale;

            if ((local - _rig.HeadLocal).magnitude <= _rig.HeadRadiusLocal + tol) return true;
            if (Placement.HasWaterCover) return false;

            var radii = _rig.BodyRadiiLocal + new Vector2(tol, tol);
            var q = local - _rig.BodyLocal;
            q = new Vector2(q.x / radii.x, q.y / radii.y);
            return q.sqrMagnitude <= 1f;
        }

        // ------------------------------------------------------------------ Ereignisse

        /// <summary>Trickster-Robben hüpfen beim ersten Antippen weg. Gibt true zurück, wenn das passiert ist.</summary>
        public bool TryTrick()
        {
            if (!Placement.Trickster || _tricked) return false;
            _tricked = true;
            _busy = true;
            _happyLeft = 0.9f;
            SetFace(SealFace.Happy);
            _audio?.Play(Sfx.Giggle);

            Vector3 from = transform.position;
            Vector3 to = Placement.AltPosition;
            float height = 1.5f + Vector3.Distance(from, to) * 0.08f;
            Tweener.Run(this, 0.7f, k =>
            {
                var p = Vector3.Lerp(from, to, k);
                p.y += Mathf.Sin(k * Mathf.PI) * height;
                transform.position = p;
                if (k > 0.5f) UpdateSorting();
            }, Ease.InOutQuad, () =>
            {
                _busy = false;
                _home = to;
                UpdateSorting();
                _fx?.Burst(transform.position, 6, 2f, 0.12f);
            });
            return true;
        }

        public void MarkFound()
        {
            if (IsFound) return;
            IsFound = true;
            _sleeping = false;
            _happyLeft = 1.4f;
            SetFace(SealFace.Happy);

            if (Placement.Style == HidingStyle.Peek)
            {
                _peek = PeekState.GoingUp;
                _peekTimer = 0f;
            }

            Tweener.Punch(_visual, 0.3f, 0.4f);
            Tweener.Run(this, 0.45f, k => _hop = Mathf.Sin(k * Mathf.PI) * 0.35f, Ease.Linear, () => _hop = 0f);

            if (Placement.HasWaterCover) _fx?.Splash(HeadWorld, _world.WaterColorAt(transform.position.y));
            ShowMarker(new Color(0.35f, 1f, 0.55f), true);
        }

        /// <summary>Hinweis: Robbe wackelt kurz und taucht ggf. auf.</summary>
        public void Wiggle()
        {
            _wiggle = 1f;
            if (Placement.Style == HidingStyle.Peek && _peek != PeekState.Up)
            {
                _peek = PeekState.GoingUp;
                _peekTimer = 0f;
            }
        }

        /// <summary>Nach Zeitablauf: zeigen, wo die Robbe war.</summary>
        public void Reveal()
        {
            if (IsFound) return;
            _visibility = 1f;
            _peek = PeekState.Up;
            _peekTimer = 999f;
            ShowMarker(new Color(1f, 0.45f, 0.35f), false);
            _wiggle = 1f;
        }

        public void Celebrate(float delay)
        {
            Tweener.Delay(this, delay, () =>
            {
                SetFace(SealFace.Happy);
                _happyLeft = 1f;
                Tweener.Run(this, 0.4f, k => _hop = Mathf.Sin(k * Mathf.PI) * 0.3f, Ease.Linear, () => _hop = 0f);
            });
        }

        void ShowMarker(Color color, bool pop)
        {
            if (_marker == null)
            {
                _marker = MakeRenderer("Marker", transform, _art.Ring, color);
                _marker.sortingOrder = FxSystem.SortingOrder - 10;
            }
            _marker.color = color;
            var center = (Vector2)transform.InverseTransformPoint(CenterWorld);
            _marker.transform.localPosition = new Vector3(center.x, center.y, 0f);
            float radius = Mathf.Max(0.55f, (Placement.HasWaterCover ? 0.45f : 0.75f) * _scale);
            float s = radius / 0.56f / _scale;
            var target = new Vector3(s, s, 1f);
            if (pop) Tweener.Scale(_marker.transform, Vector3.zero, target, 0.35f, Ease.OutBack);
            else _marker.transform.localScale = target;
        }

        void SetFace(SealFace face) => _face.sprite = _art.SealFaceSprite(Species.IsBaby, face);

        // ------------------------------------------------------------------ Animation

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _t += dt;

            // Atmen
            float breath = Mathf.Sin(_t * 2.2f + _phase) * 0.018f;
            _visual.localScale = new Vector3(1f + breath, 1f - breath, 1f);

            // Gesicht: blinzeln, glücklich, schlafen
            if (_happyLeft > 0f)
            {
                _happyLeft -= dt;
                if (_happyLeft <= 0f) SetFace(_sleeping ? SealFace.Blink : SealFace.Open);
            }
            else if (!_sleeping)
            {
                if (_blinkLeft > 0f)
                {
                    _blinkLeft -= dt;
                    if (_blinkLeft <= 0f) SetFace(SealFace.Open);
                }
                else if ((_blinkIn -= dt) <= 0f)
                {
                    SetFace(SealFace.Blink);
                    _blinkLeft = 0.12f;
                    _blinkIn = Random.Range(2f, 6f);
                }
            }
            else if ((_zTimer -= dt) <= 0f)
            {
                _zTimer = Random.Range(1.8f, 3f);
                _fx?.Zzz(HeadWorld + new Vector2(0.15f, 0.25f) * _scale);
            }

            if (Species.Golden && (_sparkTimer -= dt) <= 0f)
            {
                _sparkTimer = Random.Range(0.4f, 1.1f);
                _fx?.Sparkle(CenterWorld, 0.5f * _scale, new Color(1f, 0.95f, 0.6f));
            }

            UpdatePeek(dt);
            UpdateMoving(dt);

            // Wackeln (Hinweis) + Hüpfer
            float rot = 0f;
            if (_wiggle > 0f)
            {
                _wiggle -= dt / 0.9f;
                rot = Mathf.Sin(_t * 30f) * 9f * Mathf.Max(0f, _wiggle);
            }
            _visual.localRotation = Quaternion.Euler(0f, 0f, rot);

            float hidden = _coverLine - (_rig.HeadLocal.y + _rig.HeadRadiusLocal) - 0.06f;
            float y = Mathf.Lerp(hidden, 0f, _visibility) + _hop / _scale;
            if (Placement.HasWaterCover && !IsFound) y += Mathf.Sin(_t * 1.6f + _phase) * 0.03f;
            _visual.localPosition = new Vector3(0f, y, 0f);
        }

        void UpdatePeek(float dt)
        {
            if (Placement.Style != HidingStyle.Peek) return;
            const float transition = 0.35f;
            _peekTimer -= dt;
            switch (_peek)
            {
                case PeekState.Up:
                    _visibility = 1f;
                    if (!IsFound && _peekTimer <= 0f)
                    {
                        _peek = PeekState.GoingDown;
                        _peekTimer = transition;
                    }
                    break;
                case PeekState.GoingDown:
                    _visibility = Mathf.Clamp01(_peekTimer / transition);
                    if (_peekTimer <= 0f)
                    {
                        _peek = PeekState.Down;
                        _peekTimer = Random.Range(1.8f, 3.5f);
                    }
                    break;
                case PeekState.Down:
                    _visibility = 0f;
                    if (_peekTimer <= 0f)
                    {
                        _peek = PeekState.GoingUp;
                        _peekTimer = transition;
                        if (_body.isVisible)
                        {
                            _audio?.Play(Sfx.Plop, 0.35f, 1.2f);
                            _fx?.Splash(HeadWorld + Vector2.up * 0.3f * _scale, _world.WaterColorAt(transform.position.y));
                        }
                    }
                    break;
                case PeekState.GoingUp:
                    _visibility = 1f - Mathf.Clamp01(_peekTimer / transition);
                    if (_peekTimer <= 0f)
                    {
                        _peek = PeekState.Up;
                        _peekTimer = Random.Range(2.2f, 3.4f);
                    }
                    break;
            }
        }

        void UpdateMoving(float dt)
        {
            if (Placement.Style != HidingStyle.Moving || IsFound || _busy || Placement.MoveRange <= 0f) return;
            const float speed = 0.9f;
            _movePhase += dt * speed / Placement.MoveRange;
            float x = _home.x + Mathf.Sin(_movePhase) * Placement.MoveRange;
            float dir = Mathf.Cos(_movePhase);
            float bob = _world.Underwater ? Mathf.Sin(_t * 1.3f + _phase) * 0.12f : 0f;
            transform.position = new Vector3(x, _home.y + bob, 0f);
            var s = transform.localScale;
            s.x = dir >= 0f ? _scale : -_scale;
            transform.localScale = s;
        }
    }
}
