using System.Collections.Generic;
using UnityEngine;

namespace SealFind
{
    /// <summary>Die fertig aufgebaute Szene eines Levels.</summary>
    public sealed class BuiltScene
    {
        public GameObject Root;
        public Texture2D Background;
        public Sprite BackgroundSprite;
        public Rect Bounds;
        public WorldDefinition World;
        public readonly List<Seal> Seals = new List<Seal>();
        public readonly List<Transform> Decoys = new List<Transform>();

        /// <summary>Wurde ein robbenförmiger Stein angetippt?</summary>
        public bool HitsDecoy(Vector2 worldPos, float tolerance)
        {
            foreach (var d in Decoys)
            {
                float s = Mathf.Abs(d.localScale.y);
                var center = (Vector2)d.position + new Vector2(0f, 0.45f * s);
                var q = worldPos - center;
                float rx = 0.75f * s + tolerance, ry = 0.42f * s + tolerance;
                if ((q.x * q.x) / (rx * rx) + (q.y * q.y) / (ry * ry) <= 1f) return true;
            }
            return false;
        }

        public void Destroy()
        {
            if (Root) Object.Destroy(Root);
            if (BackgroundSprite) Object.Destroy(BackgroundSprite);
            if (Background) Object.Destroy(Background);
            BackgroundSprite = null;
            Root = null;
            Background = null;
        }
    }

    /// <summary>Verwandelt einen <see cref="ScenePlan"/> in GameObjects mit SpriteRenderern.</summary>
    public static class SceneBuilder
    {
        const int BackgroundOrder = -32000;
        const int SkyDecoOrder = -31000;

        /// <summary>Tiefe aus der y-Position: weiter unten = weiter vorne.</summary>
        public static int SortFor(float y) => -Mathf.RoundToInt(y * 100f) * 4;

        public static BuiltScene Build(ScenePlan plan, WorldDefinition world, IReadOnlyList<SealSpecies> species,
            ArtLibrary art, FxSystem fx, AudioManager audio, Transform parent)
        {
            var scene = new BuiltScene { Root = new GameObject("Level"), Bounds = plan.Bounds, World = world };
            scene.Root.transform.SetParent(parent, false);

            // Hintergrund
            scene.Background = BackgroundPainter.Paint(world, plan.Bounds, plan.Seed);
            var bgSprite = Sprite.Create(scene.Background, new Rect(0, 0, scene.Background.width, scene.Background.height),
                new Vector2(0.5f, 0.5f), scene.Background.width / plan.Bounds.width, 0, SpriteMeshType.FullRect);
            var bg = new GameObject("Background").AddComponent<SpriteRenderer>();
            bg.transform.SetParent(scene.Root.transform, false);
            bg.transform.position = plan.Bounds.center;
            bg.sprite = bgSprite;
            scene.BackgroundSprite = bgSprite;
            bg.sortingOrder = BackgroundOrder;

            var propsRoot = new GameObject("Props").transform;
            propsRoot.SetParent(scene.Root.transform, false);
            foreach (var p in plan.Props) CreateProp(p, world, art, propsRoot, plan.Bounds);

            var decoyRoot = new GameObject("Decoys").transform;
            decoyRoot.SetParent(scene.Root.transform, false);
            foreach (var d in plan.Decoys) scene.Decoys.Add(CreateProp(d, world, art, decoyRoot, plan.Bounds).transform);

            var sealRoot = new GameObject("Seals").transform;
            sealRoot.SetParent(scene.Root.transform, false);
            foreach (var s in plan.Seals)
            {
                var sp = species[s.SpeciesIndex];
                var go = new GameObject("Seal_" + sp.Id);
                go.transform.SetParent(sealRoot, false);
                var seal = go.AddComponent<Seal>();
                seal.Init(sp, s, world, art, fx, audio);
                scene.Seals.Add(seal);
            }
            return scene;
        }

        static SpriteRenderer CreateProp(PropPlacement p, WorldDefinition world, ArtLibrary art, Transform parent, Rect bounds)
        {
            var info = PropCatalog.Get(p.Kind);
            var go = new GameObject(p.Kind.ToString());
            go.transform.SetParent(parent, false);
            go.transform.position = p.Position;
            go.transform.localScale = new Vector3(p.Scale, p.Scale, 1f);

            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = art.Prop(p.Kind, p.Variant);
            r.flipX = p.Flip && info.Motion != PropMotion.Swim && info.Motion != PropMotion.Fly;

            Color c = info.Glows ? Color.white : world.Ambient;
            if (info.RockTinted) c *= world.RockTint;
            c.a = 1f;
            r.color = c;

            bool skyDeco = p.Kind == PropKind.Sun || p.Kind == PropKind.Cloud || p.Kind == PropKind.GullFlying;
            r.sortingOrder = skyDeco ? SkyDecoOrder + (int)p.Kind : SortFor(p.Position.y);

            if (info.Motion != PropMotion.None)
            {
                var zone = world.ZoneRect(world.ZoneAt(p.Position.y), bounds);
                go.AddComponent<AmbientMotion>().Init(info.Motion, bounds, zone);
            }
            return r;
        }
    }
}
