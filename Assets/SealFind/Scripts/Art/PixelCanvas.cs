using System;
using UnityEngine;

namespace SealFind
{
    /// <summary>
    /// Mini-Zeichenfläche für prozedurale Sprites. Zeichnet Formen über Signed Distance Functions,
    /// dadurch bekommen alle Kanten automatisch Anti-Aliasing. Koordinaten in Pixeln, y zeigt nach oben.
    /// </summary>
    public sealed class PixelCanvas
    {
        public delegate float Sdf(float x, float y);

        public readonly int Width;
        public readonly int Height;
        readonly Color[] _px;

        public PixelCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            _px = new Color[width * height];
        }

        public void Blend(int x, int y, Color c, float coverage)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
            float a = c.a * coverage;
            if (a <= 0.001f) return;
            int i = y * Width + x;
            var d = _px[i];
            float outA = a + d.a * (1f - a);
            if (outA <= 0f) return;
            float k = d.a * (1f - a);
            _px[i] = new Color(
                (c.r * a + d.r * k) / outA,
                (c.g * a + d.g * k) / outA,
                (c.b * a + d.b * k) / outA,
                outA);
        }

        /// <summary>Füllt eine Form. Optional mit Umrandung (wird zuerst und etwas größer gezeichnet).</summary>
        public void Fill(Sdf sdf, Func<float, float, Color> color, float x0, float y0, float x1, float y1,
            float outline = 0f, Color outlineColor = default)
        {
            float pad = outline + 2f;
            int ix0 = Mathf.Max(0, Mathf.FloorToInt(x0 - pad));
            int iy0 = Mathf.Max(0, Mathf.FloorToInt(y0 - pad));
            int ix1 = Mathf.Min(Width - 1, Mathf.CeilToInt(x1 + pad));
            int iy1 = Mathf.Min(Height - 1, Mathf.CeilToInt(y1 + pad));

            for (int y = iy0; y <= iy1; y++)
            for (int x = ix0; x <= ix1; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float d = sdf(px, py);
                if (outline > 0f)
                {
                    float co = Mathf.Clamp01(0.5f - (d - outline));
                    if (co > 0f) Blend(x, y, outlineColor, co);
                }
                float ci = Mathf.Clamp01(0.5f - d);
                if (ci > 0f) Blend(x, y, color(px, py), ci);
            }
        }

        public void Fill(Sdf sdf, Color color, float x0, float y0, float x1, float y1, float outline = 0f, Color outlineColor = default)
            => Fill(sdf, (x, y) => color, x0, y0, x1, y1, outline, outlineColor);

        // ------------------------------------------------------------------ Bequeme Formen

        public void Ellipse(float cx, float cy, float rx, float ry, Color c, float rotDeg = 0f, float outline = 0f, Color oc = default)
        {
            float m = Mathf.Max(rx, ry);
            Fill(SdEllipse(cx, cy, rx, ry, rotDeg), c, cx - m, cy - m, cx + m, cy + m, outline, oc);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Func<float, float, Color> c, float rotDeg = 0f, float outline = 0f, Color oc = default)
        {
            float m = Mathf.Max(rx, ry);
            Fill(SdEllipse(cx, cy, rx, ry, rotDeg), c, cx - m, cy - m, cx + m, cy + m, outline, oc);
        }

        public void Circle(float cx, float cy, float r, Color c, float outline = 0f, Color oc = default)
            => Fill(SdCircle(cx, cy, r), c, cx - r, cy - r, cx + r, cy + r, outline, oc);

        public void Box(float cx, float cy, float hw, float hh, float radius, Color c, float rotDeg = 0f, float outline = 0f, Color oc = default)
        {
            float m = Mathf.Sqrt(hw * hw + hh * hh);
            Fill(SdBox(cx, cy, hw, hh, radius, rotDeg), c, cx - m, cy - m, cx + m, cy + m, outline, oc);
        }

        public void Box(float cx, float cy, float hw, float hh, float radius, Func<float, float, Color> c, float rotDeg = 0f, float outline = 0f, Color oc = default)
        {
            float m = Mathf.Sqrt(hw * hw + hh * hh);
            Fill(SdBox(cx, cy, hw, hh, radius, rotDeg), c, cx - m, cy - m, cx + m, cy + m, outline, oc);
        }

        public void Line(float ax, float ay, float bx, float by, float width, Color c, float outline = 0f, Color oc = default)
        {
            float r = width * 0.5f;
            Fill(SdSegment(ax, ay, bx, by, r), c,
                Mathf.Min(ax, bx) - r, Mathf.Min(ay, by) - r, Mathf.Max(ax, bx) + r, Mathf.Max(ay, by) + r, outline, oc);
        }

        public void Poly(Vector2[] pts, Color c, float outline = 0f, Color oc = default)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var p in pts)
            {
                x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y);
                x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y);
            }
            Fill(SdPolygon(pts), c, x0, y0, x1, y1, outline, oc);
        }

        public void Ring(float cx, float cy, float r, float width, Color c)
        {
            float hw = width * 0.5f;
            Fill((x, y) => Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) - r) - hw, c,
                cx - r - hw, cy - r - hw, cx + r + hw, cy + r + hw);
        }

        /// <summary>Vereinigung mehrerer Formen als eine Silhouette mit gemeinsamer Umrandung.</summary>
        public void Union(Sdf[] shapes, Color fill, float outline, Color oc)
        {
            Fill((x, y) =>
            {
                float d = float.MaxValue;
                foreach (var s in shapes) d = Mathf.Min(d, s(x, y));
                return d;
            }, fill, 0, 0, Width, Height, outline, oc);
        }

        // ------------------------------------------------------------------ SDFs

        public static Sdf SdCircle(float cx, float cy, float r) =>
            (x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

        public static Sdf SdEllipse(float cx, float cy, float rx, float ry, float rotDeg = 0f)
        {
            float rad = -rotDeg * Mathf.Deg2Rad;
            float cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
            float k = Mathf.Min(rx, ry);
            return (x, y) =>
            {
                float dx = x - cx, dy = y - cy;
                float lx = dx * cs - dy * sn, ly = dx * sn + dy * cs;
                float qx = lx / rx, qy = ly / ry;
                return (Mathf.Sqrt(qx * qx + qy * qy) - 1f) * k;
            };
        }

        public static Sdf SdBox(float cx, float cy, float hw, float hh, float radius, float rotDeg = 0f)
        {
            float rad = -rotDeg * Mathf.Deg2Rad;
            float cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
            return (x, y) =>
            {
                float dx = x - cx, dy = y - cy;
                float lx = Mathf.Abs(dx * cs - dy * sn), ly = Mathf.Abs(dx * sn + dy * cs);
                float qx = lx - hw + radius, qy = ly - hh + radius;
                float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
            };
        }

        public static Sdf SdSegment(float ax, float ay, float bx, float by, float r)
        {
            var a = new Vector2(ax, ay);
            var ba = new Vector2(bx - ax, by - ay);
            float len2 = Mathf.Max(ba.sqrMagnitude, 1e-5f);
            return (x, y) =>
            {
                var pa = new Vector2(x - ax, y - ay);
                float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / len2);
                return (pa - ba * h).magnitude - r;
            };
        }

        public static Sdf SdPolygon(Vector2[] v)
        {
            return (x, y) =>
            {
                var p = new Vector2(x, y);
                float d = (p - v[0]).sqrMagnitude;
                float s = 1f;
                for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
                {
                    var e = v[j] - v[i];
                    var w = p - v[i];
                    var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Mathf.Max(e.sqrMagnitude, 1e-5f));
                    d = Mathf.Min(d, b.sqrMagnitude);
                    bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                    if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
                }
                return s * Mathf.Sqrt(d);
            };
        }

        // ------------------------------------------------------------------ Ausgabe

        public Color GetPixel(int x, int y) => _px[y * Width + x];

#if SEALFIND_ART_DUMP
        /// <summary>Nur für Werkzeuge außerhalb von Unity: fängt fertige Zeichnungen ab (z.B. als PNG-Vorschau).</summary>
        public static Action<PixelCanvas, string> DebugBake;
#endif

        public Texture2D ToTexture(string name, FilterMode filter = FilterMode.Bilinear)
        {
#if SEALFIND_ART_DUMP
            DebugBake?.Invoke(this, name);
#endif
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp,
            };
            // Ränder transparenter Pixel mit Nachbarfarbe füllen verhindert dunkle Säume beim Filtern.
            var outPx = new Color[_px.Length];
            for (int i = 0; i < _px.Length; i++)
            {
                var c = _px[i];
                if (c.a <= 0f) c = new Color(1f, 1f, 1f, 0f);
                outPx[i] = c;
            }
            tex.SetPixels(outPx);
            tex.Apply(false, true);
            return tex;
        }

        public Sprite ToSprite(string name, float pixelsPerUnit, Vector2 pivot, Vector4 border = default)
        {
            var tex = ToTexture(name);
            var sprite = Sprite.Create(tex, new Rect(0, 0, Width, Height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            return sprite;
        }
    }
}
