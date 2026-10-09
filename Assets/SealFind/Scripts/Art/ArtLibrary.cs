using System.Collections.Generic;
using UnityEngine;

namespace SealFind
{
    /// <summary>
    /// Erzeugt und cached alle Sprites prozedural – das Projekt braucht keine einzige Bilddatei.
    /// Wer später echte Illustrationen hat, tauscht einfach die Methoden hier gegen geladene Sprites aus.
    /// </summary>
    public sealed class ArtLibrary
    {
        const float PropPpu = 100f;
        const float IconPpu = 100f;

        readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        static readonly Color Ink = new Color(0.22f, 0.2f, 0.24f);
        static readonly Color SealOutline = new Color(0.32f, 0.32f, 0.35f);
        static readonly Color Eye = new Color(0.07f, 0.07f, 0.1f);

        Sprite Cached(string key, System.Func<Sprite> build)
        {
            if (!_cache.TryGetValue(key, out var s) || s == null)
            {
                s = build();
                _cache[key] = s;
            }
            return s;
        }

        public void Dispose()
        {
            foreach (var s in _cache.Values)
            {
                if (s == null) continue;
                Object.Destroy(s.texture);
                Object.Destroy(s);
            }
            _cache.Clear();
        }

        static float Hash(float x, float y)
        {
            float h = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        static Color Gray(float v, float a = 1f) => new Color(v, v, v, a);

        // ================================================================== Robbe

        public Sprite SealBody(bool baby) => Cached("seal_body_" + baby, () => DrawSealBody(SealRig.For(baby), baby, false));

        /// <summary>Gleiche Silhouette ohne Gesicht in Steinoptik – der Köder.</summary>
        Sprite SealRockSprite(int variant) => Cached("sealrock_" + variant, () => DrawSealBody(variant == 0 ? SealRig.Adult : SealRig.Baby, variant == 1, true));

        public Sprite SealFaceSprite(bool baby, SealFace face) => Cached("seal_face_" + baby + face, () => DrawSealFace(SealRig.For(baby), face));

        public Sprite SealAccessory(Accessory acc, bool baby) =>
            acc == Accessory.None ? null : Cached("seal_acc_" + acc + baby, () => DrawAccessory(SealRig.For(baby), acc));

        static PixelCanvas.Sdf[] SilhouetteShapes(SealRig r) => new[]
        {
            PixelCanvas.SdEllipse(r.Tail1.x, r.Tail1.y, r.TailRadii.x, r.TailRadii.y, 35f),
            PixelCanvas.SdEllipse(r.Tail2.x, r.Tail2.y, r.TailRadii.x, r.TailRadii.y, -20f),
            PixelCanvas.SdEllipse(r.Body.x, r.Body.y, r.BodyRadii.x, r.BodyRadii.y, r.BodyRotation),
            PixelCanvas.SdCircle(r.Head.x, r.Head.y, r.HeadRadius),
            PixelCanvas.SdEllipse(r.Snout.x, r.Snout.y, r.SnoutRadii.x, r.SnoutRadii.y),
        };

        static Sprite DrawSealBody(SealRig r, bool baby, bool asRock)
        {
            var c = new PixelCanvas(SealRig.Width, SealRig.Height);
            float o = 4.5f;
            var outline = asRock ? Gray(0.36f) : SealOutline;
            c.Union(SilhouetteShapes(r), outline, o, outline);

            if (asRock)
            {
                // Steinoptik: Körnung + Flecken, kein Gesicht
                var shapes = SilhouetteShapes(r);
                PixelCanvas.Sdf all = (x, y) =>
                {
                    float d = float.MaxValue;
                    foreach (var s in shapes) d = Mathf.Min(d, s(x, y));
                    return d;
                };
                c.Fill(all, (x, y) =>
                {
                    float g = 0.62f + 0.25f * (y / SealRig.Height) + (Hash(x, y) - 0.5f) * 0.08f;
                    // runde Flecken: pro Rasterzelle ein zufällig versetzter Punkt
                    float cx = Mathf.Floor(x / 12f), cy = Mathf.Floor(y / 12f);
                    float px = cx * 12f + 3f + Hash(cx, cy) * 6f, py = cy * 12f + 3f + Hash(cy, cx) * 6f;
                    if (Hash(cx + 7f, cy) > 0.6f && (x - px) * (x - px) + (y - py) * (y - py) < 7f) g -= 0.12f;
                    return Gray(g);
                }, 0, 0, SealRig.Width, SealRig.Height);
                c.Ellipse(r.Body.x - 20, r.Body.y + 18, 30, 10, Gray(1f, 0.25f), -8f);
                c.Line(r.Body.x + 10, r.Body.y - 5, r.Body.x + 32, r.Body.y - 18, 2.5f, Gray(0.35f, 0.6f));
                return c.ToSprite("SealRock", SealRig.PixelsPerUnit, SealRig.Pivot);
            }

            float bodyBottom = r.Body.y - r.BodyRadii.y;
            c.Ellipse(r.Tail1.x, r.Tail1.y, r.TailRadii.x, r.TailRadii.y, Gray(0.74f), 35f);
            c.Ellipse(r.Tail2.x, r.Tail2.y, r.TailRadii.x, r.TailRadii.y, Gray(0.7f), -20f);
            c.Ellipse(r.Body.x, r.Body.y, r.BodyRadii.x, r.BodyRadii.y,
                (x, y) => Gray(Mathf.Lerp(0.74f, 0.97f, Mathf.Clamp01((y - bodyBottom) / (r.BodyRadii.y * 2f)))), r.BodyRotation);
            // heller Bauch
            c.Ellipse(r.Body.x + 12, r.Body.y - r.BodyRadii.y * 0.35f, r.BodyRadii.x * 0.72f, r.BodyRadii.y * 0.42f, Gray(1f, 0.55f), r.BodyRotation);
            if (baby)
            {
                // flauschiges Fell: weiche Flecken
                for (int i = 0; i < 9; i++)
                {
                    float fx = r.Body.x - r.BodyRadii.x * 0.6f + i * r.BodyRadii.x * 0.15f;
                    c.Circle(fx, r.Body.y + r.BodyRadii.y * 0.55f + Mathf.Sin(i * 1.7f) * 6f, 5f, Gray(1f, 0.35f));
                }
            }
            float headBottom = r.Head.y - r.HeadRadius;
            c.Fill(PixelCanvas.SdCircle(r.Head.x, r.Head.y, r.HeadRadius),
                (x, y) => Gray(Mathf.Lerp(0.82f, 0.99f, Mathf.Clamp01((y - headBottom) / (r.HeadRadius * 2f)))),
                r.Head.x - r.HeadRadius, headBottom, r.Head.x + r.HeadRadius, r.Head.y + r.HeadRadius);
            c.Ellipse(r.Snout.x, r.Snout.y, r.SnoutRadii.x, r.SnoutRadii.y, Gray(1f));
            c.Ellipse(r.Head.x - r.HeadRadius * 0.35f, r.Head.y + r.HeadRadius * 0.5f, r.HeadRadius * 0.35f, r.HeadRadius * 0.18f, Gray(1f, 0.45f), 20f);
            c.Ellipse(r.Flipper.x, r.Flipper.y, r.FlipperRadii.x, r.FlipperRadii.y, Gray(0.72f), -25f, 3.5f, SealOutline);
            return c.ToSprite(baby ? "SealBodyBaby" : "SealBody", SealRig.PixelsPerUnit, SealRig.Pivot);
        }

        static Sprite DrawSealFace(SealRig r, SealFace face)
        {
            var c = new PixelCanvas(SealRig.Width, SealRig.Height);
            float er = r.EyeRadius;

            // Bäckchen
            c.Ellipse(r.Head.x + 6, r.Snout.y + 4, er * 1.3f, er * 0.75f, new Color(1f, 0.5f, 0.58f, 0.45f));

            foreach (var e in new[] { r.EyeBack, r.EyeFront })
            {
                switch (face)
                {
                    case SealFace.Open:
                        c.Ellipse(e.x, e.y, er * 0.85f, er, Eye);
                        c.Circle(e.x - er * 0.3f, e.y + er * 0.38f, er * 0.38f, Color.white);
                        c.Circle(e.x + er * 0.3f, e.y - er * 0.35f, er * 0.16f, Gray(1f, 0.8f));
                        break;
                    case SealFace.Blink:
                        c.Line(e.x - er * 0.9f, e.y - 1f, e.x + er * 0.9f, e.y - 2f, 3.2f, Eye);
                        break;
                    case SealFace.Happy:
                        c.Line(e.x - er * 0.9f, e.y - er * 0.4f, e.x, e.y + er * 0.5f, 3.2f, Eye);
                        c.Line(e.x, e.y + er * 0.5f, e.x + er * 0.9f, e.y - er * 0.4f, 3.2f, Eye);
                        break;
                }
            }

            var n = r.Nose;
            c.Ellipse(n.x, n.y, 8f, 6f, Eye);
            c.Circle(n.x - 2f, n.y + 2f, 2f, Gray(1f, 0.7f));
            c.Line(n.x, n.y - 5f, n.x - 6f, n.y - 10f, 2.4f, Eye);
            c.Line(n.x, n.y - 5f, n.x + 4f, n.y - 10f, 2.4f, Eye);

            var whisker = new Color(0.25f, 0.25f, 0.28f, 0.7f);
            for (int k = -1; k <= 1; k++)
                c.Line(r.Snout.x + 6f, r.Snout.y - 2f + k * 4f, r.Snout.x + 30f, r.Snout.y + k * 9f, 1.6f, whisker);

            return c.ToSprite("SealFace" + face, SealRig.PixelsPerUnit, SealRig.Pivot);
        }

        static Sprite DrawAccessory(SealRig r, Accessory acc)
        {
            var c = new PixelCanvas(SealRig.Width, SealRig.Height);
            float R = r.HeadRadius;
            float er = r.EyeRadius;
            var h = r.Head;

            switch (acc)
            {
                case Accessory.Sunglasses:
                {
                    var lens = new Color(0.08f, 0.08f, 0.13f);
                    c.Line(r.EyeBack.x - er * 1.4f, r.EyeBack.y + er * 0.3f, h.x - R * 0.85f, h.y + 6f, 3.5f, lens);
                    c.Line(r.EyeBack.x + er, r.EyeBack.y + er * 0.5f, r.EyeFront.x - er, r.EyeFront.y + er * 0.5f, 3.5f, lens);
                    foreach (var e in new[] { r.EyeBack, r.EyeFront })
                    {
                        c.Box(e.x, e.y, er * 1.45f, er * 1.15f, er * 0.6f, lens);
                        c.Line(e.x - er * 0.8f, e.y + er * 0.1f, e.x - er * 0.1f, e.y + er * 0.75f, 2.2f, new Color(0.55f, 0.8f, 1f, 0.75f));
                    }
                    break;
                }
                case Accessory.PirateHat:
                {
                    var top = new Vector2(h.x - 4f, h.y + R * 0.68f);
                    var hat = new[]
                    {
                        new Vector2(top.x - R * 1.15f, top.y), new Vector2(top.x + R * 1.15f, top.y + 5f),
                        new Vector2(top.x + R * 0.75f, top.y + R * 0.55f), new Vector2(top.x + R * 0.2f, top.y + R * 0.95f),
                        new Vector2(top.x - R * 0.3f, top.y + R * 0.85f), new Vector2(top.x - R * 0.85f, top.y + R * 0.5f),
                    };
                    c.Poly(hat, new Color(0.13f, 0.12f, 0.15f), 3f, new Color(0.85f, 0.65f, 0.15f));
                    c.Circle(top.x, top.y + R * 0.48f, R * 0.17f, Color.white);
                    c.Line(top.x - R * 0.28f, top.y + R * 0.2f, top.x + R * 0.28f, top.y + R * 0.36f, 3f, Color.white);
                    c.Line(top.x - R * 0.28f, top.y + R * 0.36f, top.x + R * 0.28f, top.y + R * 0.2f, 3f, Color.white);
                    c.Circle(top.x - R * 0.06f, top.y + R * 0.5f, 2.2f, Ink);
                    c.Circle(top.x + R * 0.08f, top.y + R * 0.5f, 2.2f, Ink);
                    break;
                }
                case Accessory.Scarf:
                {
                    var red = new Color(0.86f, 0.2f, 0.22f);
                    var dark = new Color(0.55f, 0.08f, 0.1f);
                    var neck = new Vector2(h.x - R * 0.55f, h.y - R * 0.68f);
                    c.Line(neck.x - R * 0.1f, neck.y - R * 0.05f, neck.x - R * 0.55f, neck.y - R * 0.85f, R * 0.32f, red, 2.5f, dark);
                    c.Line(neck.x - R * 0.4f, neck.y + R * 0.4f, neck.x + R * 0.6f, neck.y - R * 0.2f, R * 0.48f, red, 2.5f, dark);
                    for (int i = 0; i < 3; i++)
                    {
                        float t = 0.2f + i * 0.3f;
                        var p = Vector2.Lerp(new Vector2(neck.x - R * 0.4f, neck.y + R * 0.4f), new Vector2(neck.x + R * 0.6f, neck.y - R * 0.2f), t);
                        c.Line(p.x - 4f, p.y - 8f, p.x + 4f, p.y + 8f, 3f, new Color(1f, 1f, 1f, 0.85f));
                    }
                    break;
                }
                case Accessory.Crown:
                {
                    float b = h.y + R * 0.72f;
                    float x = h.x - 4f;
                    var crown = new[]
                    {
                        new Vector2(x - R * 0.6f, b), new Vector2(x + R * 0.6f, b), new Vector2(x + R * 0.72f, b + R * 0.62f),
                        new Vector2(x + R * 0.35f, b + R * 0.3f), new Vector2(x, b + R * 0.78f),
                        new Vector2(x - R * 0.35f, b + R * 0.3f), new Vector2(x - R * 0.72f, b + R * 0.62f),
                    };
                    c.Poly(crown, new Color(1f, 0.8f, 0.2f), 3f, new Color(0.6f, 0.4f, 0.05f));
                    c.Circle(x, b + R * 0.2f, R * 0.12f, new Color(0.9f, 0.15f, 0.3f));
                    c.Circle(x - R * 0.38f, b + R * 0.14f, R * 0.08f, new Color(0.2f, 0.5f, 1f));
                    c.Circle(x + R * 0.38f, b + R * 0.14f, R * 0.08f, new Color(0.2f, 0.8f, 0.4f));
                    break;
                }
                case Accessory.Snorkel:
                {
                    var mid = (r.EyeBack + r.EyeFront) * 0.5f;
                    float hw = Mathf.Abs(r.EyeFront.x - r.EyeBack.x) * 0.5f + er * 1.7f;
                    c.Line(mid.x - hw, mid.y + 2f, h.x - R * 0.9f, h.y + 4f, 4f, new Color(0.1f, 0.4f, 0.8f));
                    c.Box(mid.x, mid.y, hw, er * 1.6f, er, new Color(0.6f, 0.88f, 1f, 0.45f), 0f, 3f, new Color(0.1f, 0.42f, 0.85f));
                    float tx = h.x - R * 0.62f;
                    c.Line(tx, h.y + R * 0.1f, tx, h.y + R * 1.3f, 7f, new Color(1f, 0.82f, 0.1f), 2f, new Color(0.6f, 0.4f, 0.0f));
                    c.Box(tx, h.y + R * 1.32f, 6f, 4f, 2f, new Color(1f, 0.45f, 0.1f));
                    break;
                }
                case Accessory.PartyHat:
                {
                    var bl = new Vector2(h.x - R * 0.55f, h.y + R * 0.72f);
                    var br = new Vector2(h.x + R * 0.45f, h.y + R * 0.82f);
                    var apex = new Vector2(h.x - R * 0.15f, h.y + R * 1.75f);
                    c.Poly(new[] { bl, br, apex }, new Color(0.95f, 0.3f, 0.65f), 3f, new Color(0.55f, 0.1f, 0.35f));
                    for (int i = 1; i <= 2; i++)
                    {
                        float t = i / 3f;
                        var a = Vector2.Lerp(bl, apex, t);
                        var b = Vector2.Lerp(br, apex, t);
                        c.Line(a.x + 2f, a.y, b.x - 2f, b.y, 5f, new Color(1f, 0.9f, 0.25f));
                    }
                    c.Circle(apex.x, apex.y, 7f, new Color(1f, 0.9f, 0.25f), 2f, new Color(0.7f, 0.5f, 0.05f));
                    break;
                }
            }
            return c.ToSprite("Acc" + acc, SealRig.PixelsPerUnit, SealRig.Pivot);
        }

        /// <summary>Wasseroberfläche, die den unteren Teil einer schwimmenden Robbe verdeckt (wird eingefärbt).</summary>
        public Sprite WaterCover => Cached("water_cover", () =>
        {
            const int w = 128, h = 64;
            var c = new PixelCanvas(w, h);
            float line = 48f;
            c.Fill((x, y) => y - (line + Mathf.Sin(x / w * Mathf.PI * 4f) * 2.5f), (x, y) =>
            {
                float top = line + Mathf.Sin(x / w * Mathf.PI * 4f) * 2.5f;
                float g = top - y < 4f ? 1.18f : 1f;
                float a = Mathf.Clamp01(y / 20f) * Mathf.Clamp01(Mathf.Min(x, w - x) / 24f);
                return new Color(g, g, g, a);
            }, 0, 0, w, h);
            return c.ToSprite("WaterCover", 64f, new Vector2(0.5f, line / h));
        });

        // ================================================================== Deko

        public Sprite Prop(PropKind kind, int variant)
        {
            if (kind == PropKind.SealRock) return SealRockSprite(variant % 2);
            return Cached("prop_" + kind + "_" + variant, () => DrawProp(kind, variant));
        }

        static Sprite DrawProp(PropKind kind, int v)
        {
            var size = PropCatalog.Get(kind).Size;
            int w = Mathf.CeilToInt(size.x * PropPpu), h = Mathf.CeilToInt(size.y * PropPpu);
            var c = new PixelCanvas(w, h);
            float o = 3f;

            switch (kind)
            {
                case PropKind.Rock:
                case PropKind.RockSmall:
                {
                    var rng = new SeededRandom(v * 31 + (kind == PropKind.Rock ? 7 : 13));
                    int n = 10;
                    var pts = new Vector2[n];
                    float cx = w * 0.5f, cy = h * 0.42f;
                    for (int i = 0; i < n; i++)
                    {
                        float a = i / (float)n * Mathf.PI * 2f;
                        float rad = rng.Range(0.82f, 1f);
                        var p = new Vector2(cx + Mathf.Cos(a) * (w * 0.46f - 6f) * rad, cy + Mathf.Sin(a) * (h * 0.44f - 6f) * rad);
                        p.y = Mathf.Max(p.y, 6f);
                        pts[i] = p;
                    }
                    var poly = PixelCanvas.SdPolygon(pts);
                    c.Fill((x, y) => poly(x, y) - 5f, (x, y) =>
                        Gray(Mathf.Lerp(0.58f, 0.9f, y / h) + (Hash(x, y) - 0.5f) * 0.06f), 0, 0, w, h, o, Gray(0.32f));
                    c.Ellipse(w * 0.36f, h * 0.68f, w * 0.14f, h * 0.07f, Gray(1f, 0.3f), 15f);
                    if (kind == PropKind.Rock) c.Line(w * 0.55f, h * 0.5f, w * 0.68f, h * 0.32f, 2.5f, Gray(0.35f, 0.5f));
                    break;
                }
                case PropKind.Umbrella:
                {
                    Color a = v == 0 ? new Color(0.92f, 0.25f, 0.25f) : v == 1 ? new Color(0.2f, 0.5f, 0.92f) : new Color(1f, 0.72f, 0.1f);
                    Color b = v == 2 ? new Color(1f, 0.45f, 0.15f) : Color.white;
                    float cx = w * 0.5f, cy = h * 0.66f;
                    c.Line(cx, 2f, cx, h * 0.8f, 6f, Gray(0.85f), 2f, Gray(0.4f));
                    var dome = PixelCanvas.SdEllipse(cx, cy, w * 0.47f, h * 0.27f);
                    c.Fill((x, y) => Mathf.Max(dome(x, y), cy - y), (x, y) =>
                    {
                        float ang = Mathf.Atan2(y - cy, x - cx);
                        return ((int)(ang / (Mathf.PI / 6f)) % 2 == 0) ? a : b;
                    }, 0, cy - 4, w, h, o, Shade(a, 0.5f));
                    c.Circle(cx, cy + h * 0.27f + 2f, 5f, Gray(0.9f), 2f, Gray(0.4f));
                    break;
                }
                case PropKind.Palm:
                {
                    var top = new Vector2(w * 0.58f, h * 0.84f);
                    for (int i = 0; i < 10; i++)
                    {
                        float t = i / 9f;
                        var p = new Vector2(w * 0.45f + (top.x - w * 0.45f) * t * t, 12f + (top.y - 12f) * t);
                        c.Ellipse(p.x, p.y, 15f - t * 4f, 17f, i % 2 == 0 ? new Color(0.62f, 0.42f, 0.24f) : new Color(0.55f, 0.36f, 0.2f), 0f, 2.5f, new Color(0.35f, 0.22f, 0.1f));
                    }
                    var leaf = new Color(0.25f, 0.68f, 0.3f);
                    foreach (float ang in new[] { 165f, 140f, 110f, 70f, 35f, 10f })
                    {
                        float rad = ang * Mathf.Deg2Rad;
                        var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad) * 0.55f - 0.25f);
                        var mid = top + dir * 52f;
                        c.Ellipse(mid.x, mid.y, 56f, 13f, leaf, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 2.5f, new Color(0.1f, 0.38f, 0.15f));
                    }
                    for (int i = 0; i < 3; i++) c.Circle(top.x - 10f + i * 10f, top.y - 10f - (i % 2) * 4f, 8f, new Color(0.45f, 0.3f, 0.15f), 2f, new Color(0.25f, 0.15f, 0.05f));
                    break;
                }
                case PropKind.Shell:
                {
                    Color col = v == 0 ? new Color(1f, 0.78f, 0.75f) : new Color(1f, 0.92f, 0.78f);
                    var fan = PixelCanvas.SdEllipse(w * 0.5f, 6f, w * 0.45f, h * 0.8f);
                    c.Fill((x, y) => Mathf.Max(fan(x, y), 6f - y), col, 0, 0, w, h, 2f, Shade(col, 0.55f));
                    for (int i = -2; i <= 2; i++) c.Line(w * 0.5f, 7f, w * 0.5f + i * w * 0.17f, h * 0.75f, 1.5f, Shade(col, 0.7f));
                    break;
                }
                case PropKind.Starfish:
                {
                    Color col = v == 0 ? new Color(1f, 0.55f, 0.25f) : new Color(0.95f, 0.4f, 0.55f);
                    var pts = new Vector2[10];
                    for (int i = 0; i < 10; i++)
                    {
                        float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                        float rr = i % 2 == 0 ? w * 0.45f : w * 0.2f;
                        pts[i] = new Vector2(w * 0.5f + Mathf.Cos(a) * rr, h * 0.5f + Mathf.Sin(a) * rr);
                    }
                    var star = PixelCanvas.SdPolygon(pts);
                    c.Fill((x, y) => star(x, y) - 2f, col, 0, 0, w, h, 2f, Shade(col, 0.5f));
                    c.Circle(w * 0.5f, h * 0.5f, 3f, Gray(1f, 0.6f));
                    break;
                }
                case PropKind.Gull:
                {
                    c.Line(30, 2, 30, 14, 2.5f, new Color(1f, 0.6f, 0.2f));
                    c.Line(40, 2, 40, 14, 2.5f, new Color(1f, 0.6f, 0.2f));
                    c.Ellipse(33, 21, 23, 12, Color.white, -8f, 2.5f, Gray(0.4f));
                    c.Ellipse(28, 23, 16, 8, Gray(0.7f), 10f, 2f, Gray(0.4f));
                    c.Circle(53, 33, 9, Color.white, 2.5f, Gray(0.4f));
                    c.Poly(new[] { new Vector2(60, 35), new Vector2(69, 31), new Vector2(60, 29) }, new Color(1f, 0.7f, 0.15f));
                    c.Circle(55, 35, 1.8f, Eye);
                    break;
                }
                case PropKind.GullFlying:
                {
                    var col = Color.white;
                    c.Line(8, 30, 28, 36, 6f, col, 2f, Gray(0.35f));
                    c.Line(28, 36, 45, 20, 6f, col, 2f, Gray(0.35f));
                    c.Line(45, 20, 62, 36, 6f, col, 2f, Gray(0.35f));
                    c.Line(62, 36, 82, 30, 6f, col, 2f, Gray(0.35f));
                    c.Ellipse(45, 20, 8, 5, col);
                    break;
                }
                case PropKind.Boat:
                {
                    Color hull = v == 0 ? new Color(0.78f, 0.25f, 0.2f) : new Color(0.2f, 0.4f, 0.7f);
                    c.Line(w * 0.5f, 55, w * 0.5f, h - 10, 6f, new Color(0.45f, 0.3f, 0.18f));
                    c.Poly(new[] { new Vector2(w * 0.52f, 70), new Vector2(w * 0.52f, h - 18), new Vector2(w * 0.86f, 78) }, Gray(0.98f), 3f, Gray(0.45f));
                    if (v == 1) c.Poly(new[] { new Vector2(w * 0.48f, 70), new Vector2(w * 0.48f, h - 40), new Vector2(w * 0.2f, 76) }, Gray(0.95f), 3f, Gray(0.45f));
                    c.Poly(new[] { new Vector2(w * 0.5f, h - 10), new Vector2(w * 0.5f, h - 28), new Vector2(w * 0.6f, h - 19) }, new Color(1f, 0.85f, 0.2f));
                    c.Poly(new[] { new Vector2(10, 60), new Vector2(w - 10, 60), new Vector2(w - 45, 6), new Vector2(45, 6) }, hull, 3f, Shade(hull, 0.45f));
                    c.Line(28, 44, w - 28, 44, 5f, Gray(1f, 0.85f));
                    for (int i = 0; i < 3; i++) c.Circle(w * 0.35f + i * 34f, 28, 6f, new Color(0.8f, 0.95f, 1f), 2f, Shade(hull, 0.4f));
                    break;
                }
                case PropKind.Buoy:
                {
                    c.Box(w * 0.5f, 10, w * 0.42f, 8f, 4f, Gray(0.3f));
                    c.Box(w * 0.5f, h * 0.42f, w * 0.34f, h * 0.32f, 10f,
                        (x, y) => ((int)(y / 14f) % 2 == 0) ? new Color(0.92f, 0.2f, 0.2f) : Color.white, 0f, o, Gray(0.3f));
                    c.Line(w * 0.5f, h * 0.74f, w * 0.5f, h * 0.88f, 4f, Gray(0.35f));
                    c.Circle(w * 0.5f, h * 0.9f, 7f, new Color(1f, 0.9f, 0.3f), 2f, Gray(0.35f));
                    break;
                }
                case PropKind.Crate:
                {
                    var wood = new Color(0.76f, 0.56f, 0.32f);
                    var dark = new Color(0.42f, 0.28f, 0.14f);
                    c.Box(w * 0.5f, h * 0.5f, w * 0.46f, h * 0.46f, 4f, (x, y) => Shade(wood, 0.92f + Hash(Mathf.Floor(y / 12f), 3f) * 0.12f), 0f, o, dark);
                    for (int i = 1; i < 3; i++) c.Line(8, h * i / 3f, w - 8, h * i / 3f, 2f, Shade(wood, 0.65f));
                    c.Line(10, 10, w - 10, h - 10, 7f, Shade(wood, 0.8f), 2f, dark);
                    break;
                }
                case PropKind.Barrel:
                {
                    var wood = new Color(0.6f, 0.38f, 0.2f);
                    c.Box(w * 0.5f, h * 0.5f, w * 0.44f, h * 0.47f, 22f,
                        (x, y) => Shade(wood, 0.8f + 0.3f * Mathf.Sin(x / w * Mathf.PI)), 0f, o, Shade(wood, 0.4f));
                    c.Line(8, h * 0.25f, w - 8, h * 0.25f, 5f, Gray(0.45f));
                    c.Line(8, h * 0.75f, w - 8, h * 0.75f, 5f, Gray(0.45f));
                    for (int i = 1; i < 4; i++) c.Line(w * i / 4f, 8, w * i / 4f, h - 8, 1.5f, Shade(wood, 0.6f));
                    break;
                }
                case PropKind.IceBlock:
                {
                    var ice = new Color(0.84f, 0.94f, 1f);
                    c.Box(w * 0.5f, h * 0.47f, w * 0.46f, h * 0.43f, 14f,
                        (x, y) => Color.Lerp(new Color(0.7f, 0.85f, 0.97f), ice, y / h), v == 1 ? 4f : 0f, o, new Color(0.5f, 0.72f, 0.9f));
                    c.Line(w * 0.2f, h * 0.7f, w * 0.45f, h * 0.78f, 4f, Gray(1f, 0.85f));
                    c.Line(w * 0.6f, h * 0.35f, w * 0.75f, h * 0.55f, 3f, Gray(1f, 0.6f));
                    break;
                }
                case PropKind.Iceberg:
                {
                    var pts = new[]
                    {
                        new Vector2(8, 8), new Vector2(w - 8, 8), new Vector2(w * 0.82f, h * 0.55f), new Vector2(w * 0.66f, h * 0.62f),
                        new Vector2(w * 0.52f, h - 8), new Vector2(w * 0.38f, h * 0.7f), new Vector2(w * 0.22f, h * 0.75f),
                    };
                    c.Poly(pts, (Color)new Color(0.93f, 0.97f, 1f), o, new Color(0.5f, 0.72f, 0.9f));
                    c.Poly(new[] { new Vector2(w * 0.52f, h - 14), new Vector2(w * 0.6f, 12), new Vector2(w - 14, 12), new Vector2(w * 0.8f, h * 0.53f) }, new Color(0.72f, 0.86f, 0.97f));
                    break;
                }
                case PropKind.Penguin:
                {
                    var body = new Color(0.12f, 0.14f, 0.2f);
                    c.Ellipse(20, 6, 9, 4, new Color(1f, 0.6f, 0.1f));
                    c.Ellipse(40, 6, 9, 4, new Color(1f, 0.6f, 0.1f));
                    c.Ellipse(30, 40, 23, 34, body, 0f, 2f, Gray(0.05f));
                    c.Ellipse(32, 34, 15, 26, Gray(0.98f));
                    c.Ellipse(9, 40, 6, 18, body, 15f);
                    c.Ellipse(51, 40, 6, 18, body, -15f);
                    c.Circle(24, 60, 3.5f, Color.white); c.Circle(25, 60, 1.8f, Eye);
                    c.Circle(38, 60, 3.5f, Color.white); c.Circle(39, 60, 1.8f, Eye);
                    c.Poly(new[] { new Vector2(26, 54), new Vector2(36, 54), new Vector2(31, 47) }, new Color(1f, 0.6f, 0.1f));
                    break;
                }
                case PropKind.Fish:
                {
                    Color col = v == 0 ? new Color(1f, 0.55f, 0.15f) : v == 1 ? new Color(0.25f, 0.6f, 1f) : new Color(1f, 0.85f, 0.2f);
                    c.Poly(new[] { new Vector2(4, h * 0.15f), new Vector2(4, h * 0.85f), new Vector2(w * 0.32f, h * 0.5f) }, Shade(col, 0.8f), 2f, Shade(col, 0.45f));
                    c.Ellipse(w * 0.58f, h * 0.5f, w * 0.34f, h * 0.36f, col, 0f, 2f, Shade(col, 0.45f));
                    c.Line(w * 0.5f, h * 0.2f, w * 0.5f, h * 0.8f, 3f, Gray(1f, 0.6f));
                    c.Circle(w * 0.78f, h * 0.58f, 3.5f, Color.white); c.Circle(w * 0.79f, h * 0.58f, 2f, Eye);
                    break;
                }
                case PropKind.Seaweed:
                {
                    var green = new Color(0.2f, 0.62f, 0.35f);
                    for (int s = 0; s < 3; s++)
                    {
                        float bx = w * (0.25f + s * 0.25f);
                        float len = h * (0.7f + 0.15f * ((s + v) % 2));
                        for (float t = 0; t < 1f; t += 0.02f)
                        {
                            float y = 4f + t * len;
                            float x = bx + Mathf.Sin(t * 9f + s) * 7f;
                            c.Circle(x, y, Mathf.Lerp(7f, 2.5f, t), Shade(green, 0.85f + s * 0.08f));
                        }
                    }
                    break;
                }
                case PropKind.Coral:
                {
                    Color col = v == 0 ? new Color(1f, 0.45f, 0.55f) : new Color(1f, 0.6f, 0.3f);
                    var rng = new SeededRandom(17 + v);
                    for (int i = 0; i < 7; i++)
                    {
                        float bx = w * 0.5f + rng.Range(-15f, 15f);
                        var tip = new Vector2(w * rng.Range(0.12f, 0.88f), h * rng.Range(0.55f, 0.9f));
                        var mid = new Vector2((bx + tip.x) * 0.5f, tip.y * 0.45f);
                        c.Line(bx, 6, mid.x, mid.y, 13f, col, 2f, Shade(col, 0.55f));
                        c.Line(mid.x, mid.y, tip.x, tip.y, 10f, col, 2f, Shade(col, 0.55f));
                        c.Circle(tip.x, tip.y, 7f, Shade(col, 1.12f));
                    }
                    break;
                }
                case PropKind.Cloud:
                {
                    var shapes = new[]
                    {
                        PixelCanvas.SdCircle(w * 0.3f, h * 0.42f, h * 0.3f), PixelCanvas.SdCircle(w * 0.5f, h * 0.55f, h * 0.4f),
                        PixelCanvas.SdCircle(w * 0.7f, h * 0.45f, h * 0.32f), PixelCanvas.SdBox(w * 0.5f, h * 0.25f, w * 0.38f, h * 0.17f, h * 0.15f),
                    };
                    c.Fill((x, y) =>
                    {
                        float d = float.MaxValue;
                        foreach (var s in shapes) d = Mathf.Min(d, s(x, y));
                        return d;
                    }, (x, y) => Color.Lerp(new Color(0.85f, 0.9f, 0.97f, 0.95f), new Color(1f, 1f, 1f, 0.95f), y / h), 0, 0, w, h);
                    break;
                }
                case PropKind.Sun:
                {
                    float cx = w * 0.5f, cy = h * 0.5f;
                    if (v == 0)
                    {
                        for (int i = 0; i < 30; i++)
                            c.Circle(cx, cy, w * 0.48f - i * 1.2f, new Color(1f, 0.95f, 0.6f, 0.02f));
                        c.Circle(cx, cy, w * 0.3f, new Color(1f, 0.88f, 0.3f), 3f, new Color(1f, 0.7f, 0.2f));
                    }
                    else
                    {
                        for (int i = 0; i < 30; i++)
                            c.Circle(cx, cy, w * 0.48f - i * 1.2f, new Color(0.8f, 0.85f, 1f, 0.02f));
                        var moon = PixelCanvas.SdCircle(cx, cy, w * 0.28f);
                        var cut = PixelCanvas.SdCircle(cx + w * 0.14f, cy + w * 0.08f, w * 0.24f);
                        c.Fill((x, y) => Mathf.Max(moon(x, y), -cut(x, y)), new Color(1f, 0.96f, 0.78f), 0, 0, w, h);
                    }
                    break;
                }
                case PropKind.Ball:
                {
                    Color[] cols = { new Color(0.95f, 0.25f, 0.25f), Color.white, new Color(0.2f, 0.5f, 0.95f), new Color(1f, 0.85f, 0.2f) };
                    float cx = w * 0.5f, cy = h * 0.5f;
                    c.Fill(PixelCanvas.SdCircle(cx, cy, w * 0.44f), (x, y) =>
                    {
                        float a = Mathf.Atan2(y - cy, x - cx) + Mathf.PI;
                        return cols[(int)(a / (Mathf.PI / 2f)) % 4];
                    }, 0, 0, w, h, 2f, Gray(0.3f));
                    c.Circle(cx, cy, 5f, Color.white);
                    c.Ellipse(cx - 8f, cy + 9f, 7f, 4f, Gray(1f, 0.5f), 30f);
                    break;
                }
                case PropKind.Towel:
                {
                    Color a = v == 0 ? new Color(0.95f, 0.35f, 0.5f) : new Color(0.25f, 0.7f, 0.65f);
                    var pts = new[] { new Vector2(6, 5), new Vector2(w - 6, 5), new Vector2(w - 22, h - 5), new Vector2(22, h - 5) };
                    c.Fill(PixelCanvas.SdPolygon(pts), (x, y) => ((int)(x / 18f) % 2 == 0) ? a : Color.white, 0, 0, w, h, 2f, Shade(a, 0.5f));
                    break;
                }
                case PropKind.Sandcastle:
                {
                    var sand = new Color(0.9f, 0.76f, 0.48f);
                    var dark = Shade(sand, 0.55f);
                    c.Box(w * 0.5f, h * 0.28f, w * 0.44f, h * 0.24f, 4f, sand, 0f, o, dark);
                    c.Box(w * 0.22f, h * 0.5f, w * 0.13f, h * 0.3f, 3f, sand, 0f, o, dark);
                    c.Box(w * 0.78f, h * 0.5f, w * 0.13f, h * 0.3f, 3f, sand, 0f, o, dark);
                    c.Box(w * 0.5f, h * 0.55f, w * 0.15f, h * 0.22f, 3f, Shade(sand, 1.05f), 0f, o, dark);
                    c.Box(w * 0.5f, h * 0.22f, w * 0.08f, h * 0.12f, 8f, Shade(sand, 0.6f));
                    c.Line(w * 0.5f, h * 0.77f, w * 0.5f, h * 0.97f, 2.5f, Gray(0.3f));
                    c.Poly(new[] { new Vector2(w * 0.5f, h * 0.97f), new Vector2(w * 0.5f, h * 0.85f), new Vector2(w * 0.64f, h * 0.91f) }, new Color(0.95f, 0.25f, 0.25f));
                    break;
                }
                case PropKind.Crab:
                {
                    var red = new Color(0.92f, 0.3f, 0.22f);
                    for (int i = 0; i < 3; i++)
                    {
                        c.Line(w * 0.3f, h * 0.35f, w * 0.08f, 4f + i * 4f, 2.5f, Shade(red, 0.7f));
                        c.Line(w * 0.7f, h * 0.35f, w * 0.92f, 4f + i * 4f, 2.5f, Shade(red, 0.7f));
                    }
                    c.Ellipse(w * 0.5f, h * 0.4f, w * 0.28f, h * 0.25f, red, 0f, 2f, Shade(red, 0.5f));
                    c.Circle(w * 0.12f, h * 0.7f, 6f, red, 2f, Shade(red, 0.5f));
                    c.Circle(w * 0.88f, h * 0.7f, 6f, red, 2f, Shade(red, 0.5f));
                    c.Line(w * 0.42f, h * 0.6f, w * 0.42f, h * 0.85f, 2f, Shade(red, 0.6f));
                    c.Line(w * 0.58f, h * 0.6f, w * 0.58f, h * 0.85f, 2f, Shade(red, 0.6f));
                    c.Circle(w * 0.42f, h * 0.87f, 3f, Color.white); c.Circle(w * 0.42f, h * 0.87f, 1.5f, Eye);
                    c.Circle(w * 0.58f, h * 0.87f, 3f, Color.white); c.Circle(w * 0.58f, h * 0.87f, 1.5f, Eye);
                    break;
                }
                case PropKind.Lantern:
                {
                    float cx = w * 0.5f;
                    for (int i = 0; i < 18; i++) c.Circle(cx, h * 0.84f, 22f - i, new Color(1f, 0.85f, 0.4f, 0.04f));
                    c.Line(cx, 2, cx, h * 0.76f, 6f, Gray(0.2f));
                    c.Box(cx, h * 0.84f, 11f, 15f, 3f, new Color(1f, 0.9f, 0.5f), 0f, 2.5f, Gray(0.15f));
                    c.Poly(new[] { new Vector2(cx - 15, h * 0.92f), new Vector2(cx + 15, h * 0.92f), new Vector2(cx, h - 3) }, Gray(0.2f));
                    break;
                }
                case PropKind.Chest:
                {
                    var wood = new Color(0.58f, 0.36f, 0.18f);
                    var gold = new Color(1f, 0.78f, 0.2f);
                    c.Box(w * 0.5f, h * 0.32f, w * 0.46f, h * 0.3f, 4f, wood, 0f, o, Shade(wood, 0.4f));
                    var lid = PixelCanvas.SdEllipse(w * 0.5f, h * 0.6f, w * 0.46f, h * 0.36f);
                    c.Fill((x, y) => Mathf.Max(lid(x, y), h * 0.6f - y), Shade(wood, 0.85f), 0, h * 0.55f, w, h, o, Shade(wood, 0.4f));
                    c.Line(w * 0.2f, 4, w * 0.2f, h * 0.9f, 6f, gold);
                    c.Line(w * 0.8f, 4, w * 0.8f, h * 0.9f, 6f, gold);
                    c.Box(w * 0.5f, h * 0.55f, 8f, 10f, 2f, gold, 0f, 2f, Shade(gold, 0.5f));
                    break;
                }
                case PropKind.Flag:
                {
                    c.Line(w * 0.15f, 2, w * 0.15f, h - 4, 5f, new Color(0.45f, 0.3f, 0.18f));
                    c.Box(w * 0.55f, h * 0.8f, w * 0.38f, h * 0.15f, 3f, Gray(0.1f));
                    c.Circle(w * 0.55f, h * 0.82f, 9f, Color.white);
                    c.Line(w * 0.45f, h * 0.72f, w * 0.65f, h * 0.76f, 3f, Color.white);
                    break;
                }
                case PropKind.Bollard:
                {
                    c.Box(w * 0.5f, h * 0.4f, w * 0.32f, h * 0.38f, 6f, Gray(0.3f), 0f, 2f, Gray(0.12f));
                    c.Ellipse(w * 0.5f, h * 0.78f, w * 0.44f, h * 0.14f, Gray(0.38f), 0f, 2f, Gray(0.12f));
                    break;
                }
                case PropKind.Snowman:
                {
                    var snow = new Color(0.97f, 0.99f, 1f);
                    var line = new Color(0.55f, 0.72f, 0.88f);
                    c.Circle(w * 0.5f, h * 0.24f, w * 0.42f, snow, 2.5f, line);
                    c.Circle(w * 0.5f, h * 0.57f, w * 0.31f, snow, 2.5f, line);
                    c.Circle(w * 0.5f, h * 0.82f, w * 0.22f, snow, 2.5f, line);
                    c.Circle(w * 0.43f, h * 0.85f, 3f, Eye); c.Circle(w * 0.57f, h * 0.85f, 3f, Eye);
                    c.Poly(new[] { new Vector2(w * 0.5f, h * 0.83f), new Vector2(w * 0.5f, h * 0.78f), new Vector2(w * 0.78f, h * 0.79f) }, new Color(1f, 0.55f, 0.1f));
                    c.Line(w * 0.25f, h * 0.68f, w * 0.75f, h * 0.7f, 8f, new Color(0.2f, 0.55f, 0.85f));
                    break;
                }
                case PropKind.Bubble:
                {
                    c.Ring(w * 0.5f, h * 0.5f, w * 0.4f, 2.5f, new Color(0.85f, 0.97f, 1f, 0.9f));
                    c.Circle(w * 0.5f, h * 0.5f, w * 0.38f, new Color(0.8f, 0.95f, 1f, 0.15f));
                    c.Circle(w * 0.38f, h * 0.62f, 3f, new Color(1f, 1f, 1f, 0.9f));
                    break;
                }
                case PropKind.Anchor:
                {
                    var iron = Gray(0.4f);
                    c.Ring(w * 0.5f, h * 0.88f, 8f, 5f, iron);
                    c.Line(w * 0.5f, h * 0.8f, w * 0.5f, h * 0.12f, 8f, iron);
                    c.Line(w * 0.28f, h * 0.68f, w * 0.72f, h * 0.68f, 7f, iron);
                    var arc = PixelCanvas.SdCircle(w * 0.5f, h * 0.42f, w * 0.38f);
                    c.Fill((x, y) => Mathf.Max(Mathf.Abs(arc(x, y)) - 4f, y - h * 0.38f), iron, 0, 0, w, h);
                    c.Poly(new[] { new Vector2(w * 0.04f, h * 0.48f), new Vector2(w * 0.24f, h * 0.38f), new Vector2(w * 0.08f, h * 0.3f) }, iron);
                    c.Poly(new[] { new Vector2(w * 0.96f, h * 0.48f), new Vector2(w * 0.76f, h * 0.38f), new Vector2(w * 0.92f, h * 0.3f) }, iron);
                    break;
                }
            }
            return c.ToSprite("Prop" + kind + v, PropPpu, new Vector2(0.5f, 0f));
        }

        // ================================================================== UI & Effekte

        /// <summary>Abgerundetes Rechteck mit 9-Slice-Rand für Buttons und Panels.</summary>
        public Sprite RoundedRect => Cached("ui_round", () =>
        {
            var c = new PixelCanvas(64, 64);
            c.Box(32, 32, 32, 32, 24, Color.white);
            return c.ToSprite("RoundedRect", IconPpu, new Vector2(0.5f, 0.5f), new Vector4(26, 26, 26, 26));
        });

        public Sprite Circle => Cached("ui_circle", () =>
        {
            var c = new PixelCanvas(128, 128);
            c.Circle(64, 64, 62, Color.white);
            return c.ToSprite("Circle", IconPpu, new Vector2(0.5f, 0.5f));
        });

        public Sprite SoftCircle => Cached("ui_soft", () =>
        {
            var c = new PixelCanvas(64, 64);
            c.Fill((x, y) => -1f, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32)) / 32f;
                float a = Mathf.Clamp01(1f - d);
                return new Color(1, 1, 1, a * a);
            }, 0, 0, 64, 64);
            return c.ToSprite("SoftCircle", IconPpu, new Vector2(0.5f, 0.5f));
        });

        public Sprite Ring => Cached("ui_ring", () =>
        {
            var c = new PixelCanvas(128, 128);
            c.Ring(64, 64, 56, 10, Color.white);
            return c.ToSprite("Ring", IconPpu, new Vector2(0.5f, 0.5f));
        });

        public Sprite Star => Cached("ui_star", () =>
        {
            var c = new PixelCanvas(128, 128);
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? 60f : 26f;
                pts[i] = new Vector2(64 + Mathf.Cos(a) * r, 60 + Mathf.Sin(a) * r);
            }
            var star = PixelCanvas.SdPolygon(pts);
            c.Fill((x, y) => star(x, y) - 3f, Color.white, 0, 0, 128, 128);
            return c.ToSprite("Star", IconPpu, new Vector2(0.5f, 0.5f));
        });

        public Sprite Sparkle => Cached("ui_sparkle", () =>
        {
            var c = new PixelCanvas(64, 64);
            var pts = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                float r = i % 2 == 0 ? 31f : 7f;
                pts[i] = new Vector2(32 + Mathf.Cos(a) * r, 32 + Mathf.Sin(a) * r);
            }
            c.Poly(pts, Color.white);
            return c.ToSprite("Sparkle", IconPpu, new Vector2(0.5f, 0.5f));
        });

        public Sprite Cross => Cached("ui_cross", () =>
        {
            var c = new PixelCanvas(128, 128);
            c.Line(28, 28, 100, 100, 22, Color.white);
            c.Line(28, 100, 100, 28, 22, Color.white);
            return c.ToSprite("Cross", IconPpu, new Vector2(0.5f, 0.5f));
        });

        public Sprite Check => Cached("ui_check", () =>
        {
            var c = new PixelCanvas(128, 128);
            c.Line(22, 66, 52, 34, 20, Color.white);
            c.Line(52, 34, 108, 98, 20, Color.white);
            return c.ToSprite("Check", IconPpu, new Vector2(0.5f, 0.5f));
        });

        public Sprite Lock => Cached("ui_lock", () =>
        {
            var c = new PixelCanvas(128, 128);
            var shackle = PixelCanvas.SdCircle(64, 70, 30);
            c.Fill((x, y) => Mathf.Max(Mathf.Abs(shackle(x, y)) - 7f, 68f - y), Color.white, 0, 0, 128, 128);
            c.Line(34, 50, 34, 72, 14, Color.white);
            c.Line(94, 50, 94, 72, 14, Color.white);
            c.Box(64, 40, 44, 34, 8, Color.white);
            return c.ToSprite("Lock", IconPpu, new Vector2(0.5f, 0.5f));
        });

        public Sprite Magnifier => Cached("ui_magnifier", () =>
        {
            var c = new PixelCanvas(128, 128);
            c.Ring(54, 74, 32, 12, Color.white);
            c.Line(78, 50, 110, 18, 18, Color.white);
            return c.ToSprite("Magnifier", IconPpu, new Vector2(0.5f, 0.5f));
        });

        public Sprite PauseIcon => Cached("ui_pause", () =>
        {
            var c = new PixelCanvas(128, 128);
            c.Box(44, 64, 13, 42, 6, Color.white);
            c.Box(84, 64, 13, 42, 6, Color.white);
            return c.ToSprite("Pause", IconPpu, new Vector2(0.5f, 0.5f));
        });

        public Sprite BackIcon => Cached("ui_back", () =>
        {
            var c = new PixelCanvas(128, 128);
            c.Line(84, 108, 40, 64, 20, Color.white);
            c.Line(40, 64, 84, 20, 20, Color.white);
            return c.ToSprite("Back", IconPpu, new Vector2(0.5f, 0.5f));
        });

        public Sprite GearIcon => Cached("ui_gear", () =>
        {
            var c = new PixelCanvas(128, 128);
            var body = PixelCanvas.SdCircle(64, 64, 38);
            var hole = PixelCanvas.SdCircle(64, 64, 16);
            var teeth = new PixelCanvas.Sdf[8];
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f;
                float rad = a * Mathf.Deg2Rad;
                teeth[i] = PixelCanvas.SdBox(64 + Mathf.Cos(rad) * 44, 64 + Mathf.Sin(rad) * 44, 11, 9, 3, a);
            }
            c.Fill((x, y) =>
            {
                float d = body(x, y);
                foreach (var t in teeth) d = Mathf.Min(d, t(x, y));
                return Mathf.Max(d, -hole(x, y));
            }, Color.white, 0, 0, 128, 128);
            return c.ToSprite("Gear", IconPpu, new Vector2(0.5f, 0.5f));
        });

        public Sprite ClockIcon => Cached("ui_clock", () =>
        {
            var c = new PixelCanvas(128, 128);
            c.Ring(64, 64, 48, 12, Color.white);
            c.Line(64, 64, 64, 96, 10, Color.white);
            c.Line(64, 64, 88, 52, 10, Color.white);
            return c.ToSprite("Clock", IconPpu, new Vector2(0.5f, 0.5f));
        });

        public Sprite ZIcon => Cached("ui_z", () =>
        {
            var c = new PixelCanvas(64, 64);
            var col = new Color(1, 1, 1, 1);
            c.Line(14, 50, 50, 50, 8, col);
            c.Line(50, 50, 14, 14, 8, col);
            c.Line(14, 14, 50, 14, 8, col);
            return c.ToSprite("Z", IconPpu, new Vector2(0.5f, 0.5f));
        });

        public Sprite Square => Cached("ui_square", () =>
        {
            var c = new PixelCanvas(16, 16);
            c.Box(8, 8, 8, 8, 1, Color.white);
            return c.ToSprite("Square", IconPpu, new Vector2(0.5f, 0.5f));
        });
    }
}
