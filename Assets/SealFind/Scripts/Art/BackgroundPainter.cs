using UnityEngine;

namespace SealFind
{
    /// <summary>Malt den Hintergrund (Himmel, Meer, Strand/Eis/Steg …) einer Welt in eine Textur.</summary>
    public static class BackgroundPainter
    {
        public const int Width = 1024;
        public const int Height = 576;

        public static float ShoreLine(WorldDefinition w, float x) =>
            w.ShoreY + Mathf.Sin(x * 0.7f) * 0.18f + Mathf.Sin(x * 1.9f + 1f) * 0.08f;

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777216f;
            }
        }

        public static Texture2D Paint(WorldDefinition w, Rect bounds, int seed)
        {
            var px = new Color32[Width * Height];
            bool planks = w.Id == "harbor";
            float vignette = w.Night ? 0.35f : 0.14f;

            for (int y = 0; y < Height; y++)
            {
                float wy = bounds.yMin + (y + 0.5f) / Height * bounds.height;
                for (int x = 0; x < Width; x++)
                {
                    float wx = bounds.xMin + (x + 0.5f) / Width * bounds.width;
                    float shore = ShoreLine(w, wx);
                    Color c;

                    if (wy >= w.HorizonY)
                    {
                        c = Color.Lerp(w.SkyBottom, w.SkyTop, Mathf.InverseLerp(w.HorizonY, bounds.yMax, wy));
                        if (w.Night && Hash(x, y, seed) > 0.9975f) c = Color.Lerp(c, Color.white, 0.6f + Hash(y, x, seed) * 0.4f);
                    }
                    else if (wy >= shore)
                    {
                        c = w.WaterColorAt(wy);
                        float wave = Mathf.Sin(wx * 1.3f + Mathf.Sin(wy * 3.1f) * 2f + wy * 9f);
                        if (wave > 0.94f) c = Color.Lerp(c, w.Foam, 0.18f);
                        if (w.Underwater)
                        {
                            float ray = Mathf.Sin(wx * 0.45f + wy * 0.18f);
                            if (ray > 0.75f) c = Color.Lerp(c, Color.white, 0.07f * Mathf.InverseLerp(w.ShoreY, bounds.yMax, wy));
                        }
                        else if (w.HorizonY - wy < 0.12f)
                        {
                            c = Color.Lerp(c, w.SkyBottom, 0.35f);
                        }
                        // Schaumkante am Ufer
                        float foamD = wy - shore;
                        if (w.Underwater) { if (foamD < 0.25f) c = Color.Lerp(c, w.GroundTop, 0.4f); }
                        else if (foamD < 0.16f) c = Color.Lerp(c, w.Foam, 0.85f);
                        else if (foamD < 0.32f && Mathf.Sin(wx * 4f) > 0.2f) c = Color.Lerp(c, w.Foam, 0.35f);
                    }
                    else
                    {
                        c = w.GroundColorAt(wy);
                        float n = Hash(x, y, seed) - 0.5f;
                        c = Shade(c, 1f + n * 0.06f);
                        if (Hash(x / 3, y / 3, seed + 7) > 0.985f) c = Shade(c, 0.85f);
                        if (!w.Underwater && shore - wy < 0.45f) c = Shade(c, 0.88f); // nasser Sand
                        if (planks)
                        {
                            float row = (wy + 9f) / 0.9f;
                            int r = Mathf.FloorToInt(row);
                            float fr = row - r;
                            if (fr < 0.06f) c = Shade(c, 0.7f);
                            float seam = (wx + 16f + (r % 2) * 1.7f) / 3.4f;
                            if (seam - Mathf.Floor(seam) < 0.012f) c = Shade(c, 0.75f);
                        }
                    }

                    float vx = (x / (float)Width - 0.5f) * 2f, vy = (y / (float)Height - 0.5f) * 2f;
                    c = Shade(c, 1f - vignette * (vx * vx * 0.5f + vy * vy * 0.5f));
                    px[y * Width + x] = c;
                }
            }

            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                name = "Background_" + w.Id,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);
    }
}
