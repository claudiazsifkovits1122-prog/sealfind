using System;
using System.Collections.Generic;
using SealFind;
using UnityEngine;

/// <summary>
/// Software-Renderer, der ein Wimmelbild genauso zusammensetzt wie SceneBuilder/Seal in Unity
/// (gleiche Sprites, Einfärbung und Sortierung) – nur ohne Animation.
/// </summary>
static class SceneRender
{
    const int PxPerUnit = 50;

    sealed class Draw
    {
        public Sprite S;
        public Vector2 Pos;
        public float Scale, ScaleY = 1f;
        public bool Flip;
        public int Order;
        public Color Tint;
    }

    static int SortFor(float y) => -Mathf.RoundToInt(y * 100f) * 4;

    public static void Render(LevelDefinition def, string path, bool markSeals)
    {
        var world = ContentDatabase.Worlds[def.World];
        var plan = SceneGenerator.Generate(world, def, ContentDatabase.Species);
        var art = new ArtLibrary();
        int W = Mathf.RoundToInt(plan.Bounds.width * PxPerUnit), H = Mathf.RoundToInt(plan.Bounds.height * PxPerUnit);
        var img = new Color[W * H];

        var bg = BackgroundPainter.Paint(world, plan.Bounds, plan.Seed);
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
            img[y * W + x] = bg.Pixels[(y * bg.height / H) * bg.width + x * bg.width / W];

        var draws = new List<Draw>();
        void AddProp(PropPlacement p)
        {
            var info = PropCatalog.Get(p.Kind);
            Color c = info.Glows ? Color.white : world.Ambient;
            if (info.RockTinted) c = c * world.RockTint;
            c.a = 1f;
            bool sky = p.Kind == PropKind.Sun || p.Kind == PropKind.Cloud || p.Kind == PropKind.GullFlying;
            draws.Add(new Draw { S = art.Prop(p.Kind, p.Variant), Pos = p.Position, Scale = p.Scale, Flip = p.Flip, Order = sky ? -31000 : SortFor(p.Position.y), Tint = c });
        }
        plan.Props.ForEach(AddProp);
        plan.Decoys.ForEach(AddProp);

        foreach (var s in plan.Seals)
        {
            var sp = ContentDatabase.Species[s.SpeciesIndex];
            Color tint = Color.Lerp(sp.BodyColor, world.EnvironmentColorAt(s.Position.y), s.Camouflage) * world.Ambient;
            tint.a = 1f;
            int o = SortFor(s.Position.y);
            draws.Add(new Draw { S = art.SealBody(sp.IsBaby), Pos = s.Position, Scale = s.Scale, Flip = s.Flip, Order = o, Tint = tint });
            draws.Add(new Draw { S = art.SealFaceSprite(sp.IsBaby, s.Sleeping ? SealFace.Blink : SealFace.Open), Pos = s.Position, Scale = s.Scale, Flip = s.Flip, Order = o + 1, Tint = world.Ambient });
            var acc = art.SealAccessory(sp.Accessory, sp.IsBaby);
            if (acc != null) draws.Add(new Draw { S = acc, Pos = s.Position, Scale = s.Scale, Flip = s.Flip, Order = o + 2, Tint = world.Ambient });
            if (s.HasWaterCover)
            {
                var rig = SealRig.For(sp.IsBaby);
                float line = rig.HeadLocal.y - rig.HeadRadiusLocal * 0.55f;
                var wc = world.WaterColorAt(s.Position.y + line * s.Scale);
                wc.a = 1f;
                draws.Add(new Draw { S = art.WaterCover, Pos = s.Position + new Vector2(0f, line * s.Scale), Scale = s.Scale * 1.05f, ScaleY = 1.1f / 1.05f, Order = o + 3, Tint = wc });
            }
        }

        draws.Sort((a, b) => a.Order.CompareTo(b.Order));
        foreach (var d in draws) Blit(img, W, H, d, plan.Bounds);
        if (markSeals)
            foreach (var s in plan.Seals)
                Ring(img, W, H, s.Position + new Vector2(0f, 0.45f * s.Scale) - plan.Bounds.min(), 0.9f * s.Scale,
                    s.IsTarget ? new Color(0.1f, 1f, 0.2f) : new Color(1f, 0.6f, 0f));

        Png.Save(path, img, W, H);
        Console.WriteLine($"{path}: {world.Name}, {plan.Props.Count} Deko, {plan.Seals.Count} Robben, {plan.Decoys.Count} Köder");
    }

    static Vector2 min(this Rect r) => new Vector2(r.xMin, r.yMin);

    static void Blit(Color[] img, int W, int H, Draw d, Rect bounds)
    {
        var t = d.S.texture;
        float wu = t.width / d.S.ppu * d.Scale, hu = t.height / d.S.ppu * d.Scale * d.ScaleY;
        float x0 = d.Pos.x - d.S.pivot.x * wu - bounds.xMin, y0 = d.Pos.y - d.S.pivot.y * hu - bounds.yMin;
        int px0 = (int)(x0 * PxPerUnit), py0 = (int)(y0 * PxPerUnit);
        int pw = Math.Max(1, (int)(wu * PxPerUnit)), ph = Math.Max(1, (int)(hu * PxPerUnit));
        for (int y = 0; y < ph; y++)
        for (int x = 0; x < pw; x++)
        {
            int ix = px0 + x, iy = py0 + y;
            if (ix < 0 || iy < 0 || ix >= W || iy >= H) continue;
            int sx = (int)((x + 0.5f) / pw * t.width);
            if (d.Flip) sx = t.width - 1 - sx;
            int sy = (int)((y + 0.5f) / ph * t.height);
            Color src = t.Pixels[sy * t.width + sx];
            var c = src * d.Tint;
            float a = src.a * d.Tint.a;
            var dst = img[iy * W + ix];
            img[iy * W + ix] = new Color(c.r * a + dst.r * (1 - a), c.g * a + dst.g * (1 - a), c.b * a + dst.b * (1 - a), 1f);
        }
    }

    static void Ring(Color[] img, int W, int H, Vector2 c, float r, Color col)
    {
        for (int a = 0; a < 900; a++)
        for (int k = 0; k < 3; k++)
        {
            float ang = a * Mathf.PI / 450f, rr = r + k * 0.02f;
            int x = (int)((c.x + Mathf.Cos(ang) * rr) * PxPerUnit), y = (int)((c.y + Mathf.Sin(ang) * rr) * PxPerUnit);
            if (x >= 0 && y >= 0 && x < W && y < H) img[y * W + x] = col;
        }
    }
}
