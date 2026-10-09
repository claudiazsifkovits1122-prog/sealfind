using System;
using System.Collections.Generic;
using UnityEngine;

namespace SealFind
{
    /// <summary>
    /// Erzeugt aus Welt + Level + Seed ein komplettes Wimmelbild (als Daten).
    ///
    /// Ablauf:
    ///  1. Deko per Poisson-Disc in Boden-, Wasser- und Himmelszone verteilen.
    ///  2. Robben-Besetzung festlegen (Ziel-Robben, Extra-Robben, evtl. legendäre Bonus-Robbe).
    ///  3. Jede Robbe mit einem Versteck-Stil platzieren (hinter Deko, im Wasser, …).
    ///  4. Robbenförmige Steine als Köder verteilen.
    ///  5. Deko entfernen, die eine Robbe unfair komplett verdecken würde.
    /// </summary>
    public static class SceneGenerator
    {
        public static readonly Rect WorldBounds = new Rect(-16f, -9f, 32f, 18f);

        /// <summary>Robben nicht ganz oben platzieren – dort liegt die HUD-Leiste.</summary>
        public const float MaxSealY = 6.2f;
        const float EdgeMargin = 1.0f;

        struct RosterEntry
        {
            public int Species;
            public bool Target;
            public bool Bonus;
        }

        public static ScenePlan Generate(WorldDefinition world, LevelDefinition level, IReadOnlyList<SealSpecies> species)
        {
            var rng = new SeededRandom(level.Seed);
            var plan = new ScenePlan { Seed = level.Seed, World = world.Index, Bounds = WorldBounds };

            PlaceProps(world, level, rng, plan);

            var usedHiders = new HashSet<int>();
            int tricksters = 0;
            foreach (var entry in BuildRoster(world, level, species, rng))
            {
                var sp = species[entry.Species];
                var seal = new SealPlacement
                {
                    SpeciesIndex = entry.Species,
                    IsTarget = entry.Target,
                    IsBonus = entry.Bonus,
                    Camouflage = sp.Golden ? 0f : Mathf.Clamp(level.Camouflage + rng.Range(-0.05f, 0.1f), 0f, 0.7f),
                };

                var style = PickStyle(world, level, rng, entry.Bonus);
                seal.Tiny = level.Styles.HasFlag(HidingStyle.Tiny) && rng.Chance(0.22f);
                seal.Scale = level.SealScale * sp.Size * (seal.Tiny ? 0.62f : 1f) * rng.Range(0.92f, 1.08f);

                if (!TryPlace(style, seal, world, rng, plan, usedHiders) &&
                    !TryPlace(HidingStyle.Open, seal, world, rng, plan, usedHiders))
                {
                    ForcePlace(seal, world, rng);
                }

                bool landBased = seal.Style == HidingStyle.Open || seal.Style == HidingStyle.BehindProp;
                seal.Sleeping = landBased && level.Styles.HasFlag(HidingStyle.Sleeping) && rng.Chance(0.3f);
                if (landBased && level.Styles.HasFlag(HidingStyle.Trickster) && tricksters < 2 && rng.Chance(0.25f))
                {
                    if (TryFindOpenSpot(world, rng, plan, seal.Scale, seal, out var alt))
                    {
                        seal.Trickster = true;
                        seal.AltPosition = alt;
                        tricksters++;
                    }
                }
                plan.Seals.Add(seal);
            }

            PlaceDecoys(world, level, rng, plan);
            RemoveOccluders(plan);
            return plan;
        }

        // ------------------------------------------------------------------ Deko

        static void PlaceProps(WorldDefinition world, LevelDefinition level, SeededRandom rng, ScenePlan plan)
        {
            var b = plan.Bounds;
            float density = Mathf.Max(0.2f, level.PropDensity);

            if (world.HasSun && world.HorizonY < b.yMax - 2.5f)
            {
                float x = rng.Range(6f, 12f) * rng.Sign();
                float y = Mathf.Min(world.HorizonY + 0.8f, b.yMax - 2.4f);
                plan.Props.Add(new PropPlacement { Kind = PropKind.Sun, Variant = world.Night ? 1 : 0, Position = new Vector2(x, y), Scale = 1f });
            }

            foreach (Zone zone in new[] { Zone.Ground, Zone.Water, Zone.Sky })
            {
                var rules = new List<PropRule>();
                var weights = new List<float>();
                foreach (var r in world.Props)
                {
                    if (r.Zone != zone) continue;
                    rules.Add(r);
                    weights.Add(r.Weight);
                }
                if (rules.Count == 0) continue;

                Rect area = PlacementArea(world, zone, b);
                if (area.height <= 0.2f) continue;

                float radius = zone == Zone.Ground ? 1.25f / Mathf.Sqrt(density)
                    : zone == Zone.Water ? (world.Underwater ? 1.7f : 2.1f) / Mathf.Sqrt(density)
                    : 3.8f;

                foreach (var p in PoissonDisc.Sample(area, radius, rng, 200))
                {
                    var rule = rules[rng.WeightedIndex(weights)];
                    var info = PropCatalog.Get(rule.Kind);
                    plan.Props.Add(new PropPlacement
                    {
                        Kind = rule.Kind,
                        Variant = rng.Range(0, info.Variants),
                        Position = p,
                        Scale = rng.Range(rule.MinScale, rule.MaxScale),
                        Flip = rng.Chance(0.5f),
                    });
                }
            }
        }

        static Rect PlacementArea(WorldDefinition world, Zone zone, Rect b)
        {
            float xMin = b.xMin + 0.6f, xMax = b.xMax - 0.6f;
            switch (zone)
            {
                case Zone.Ground:
                    return Rect.MinMaxRect(xMin, b.yMin + 0.1f, xMax, world.ShoreY - 0.3f);
                case Zone.Water:
                    return Rect.MinMaxRect(xMin, world.ShoreY + 0.2f, xMax,
                        world.Underwater ? b.yMax - 1.5f : world.HorizonY - 0.6f);
                default:
                    return Rect.MinMaxRect(xMin, world.HorizonY + 0.8f, xMax, b.yMax - 1.2f);
            }
        }

        // ------------------------------------------------------------------ Robben-Besetzung

        static List<RosterEntry> BuildRoster(WorldDefinition world, LevelDefinition level, IReadOnlyList<SealSpecies> species, SeededRandom rng)
        {
            Func<SealSpecies, bool> isTarget;
            switch (level.Goal)
            {
                case GoalType.FindBabies: isTarget = s => s.IsBaby; break;
                case GoalType.FindAccessory: isTarget = s => s.Accessory == level.GoalAccessory; break;
                default: isTarget = s => true; break;
            }
            bool filtered = level.Goal == GoalType.FindBabies || level.Goal == GoalType.FindAccessory;

            var roster = new List<RosterEntry>();
            for (int i = 0; i < level.TargetCount; i++)
                roster.Add(new RosterEntry { Species = PickSpecies(world, species, rng, isTarget), Target = true });

            if (filtered)
            {
                for (int i = 0; i < level.ExtraSeals; i++)
                    roster.Add(new RosterEntry { Species = PickSpecies(world, species, rng, s => !isTarget(s)) });
            }

            if (level.BonusChance > 0f && rng.Chance(level.BonusChance))
            {
                roster.Add(new RosterEntry
                {
                    Species = PickSpecies(world, species, rng, s => s.Rarity == SealRarity.Legendary && (!filtered || !isTarget(s))),
                    Bonus = true,
                });
            }
            return roster;
        }

        static int PickSpecies(WorldDefinition world, IReadOnlyList<SealSpecies> species, SeededRandom rng, Func<SealSpecies, bool> filter)
        {
            var weights = new float[species.Count];
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < species.Count; i++)
                {
                    var s = species[i];
                    bool ok = filter(s) && (pass == 1 || s.LivesIn(world.Index));
                    weights[i] = ok ? RarityInfo.Weight(s.Rarity) : 0f;
                }
                int idx = rng.WeightedIndex(weights);
                if (idx >= 0) return idx;
            }
            return 0;
        }

        // ------------------------------------------------------------------ Versteck-Stile

        static HidingStyle PickStyle(WorldDefinition world, LevelDefinition level, SeededRandom rng, bool bonus)
        {
            var styles = new List<HidingStyle>();
            var weights = new List<float>();

            void Offer(HidingStyle s, float w, bool possible = true)
            {
                if (!possible || !level.Styles.HasFlag(s)) return;
                styles.Add(s);
                weights.Add(w);
            }

            bool surface = world.HasWater && !world.Underwater;
            Offer(HidingStyle.Open, world.Underwater ? 3f : 2.5f);
            Offer(HidingStyle.BehindProp, bonus ? 6f : 4f);
            Offer(HidingStyle.InWater, 3f, surface && !bonus);
            Offer(HidingStyle.Peek, 1.5f, surface && !bonus);
            Offer(HidingStyle.Moving, world.Underwater ? 3f : 1.5f, world.HasWater && !bonus);

            if (styles.Count == 0) return HidingStyle.Open;
            return styles[rng.WeightedIndex(weights)];
        }

        static bool TryPlace(HidingStyle style, SealPlacement seal, WorldDefinition world, SeededRandom rng,
            ScenePlan plan, HashSet<int> usedHiders)
        {
            seal.Style = style;
            seal.HasWaterCover = false;
            seal.MoveRange = 0f;
            seal.HiderProp = -1;

            switch (style)
            {
                case HidingStyle.BehindProp:
                    return TryPlaceBehindProp(seal, world, rng, plan, usedHiders);

                case HidingStyle.InWater:
                case HidingStyle.Peek:
                case HidingStyle.Moving:
                {
                    if (!world.HasWater) return false;
                    float range = style == HidingStyle.Moving ? rng.Range(1.5f, 3.5f) : 0f;
                    float yMin = world.ShoreY + (world.Underwater ? 0.6f : 0.15f);
                    float yMax = Mathf.Min(world.HorizonY - 1.0f * seal.Scale - 0.25f, MaxSealY - seal.Scale);
                    if (yMax <= yMin) return false;
                    var area = Rect.MinMaxRect(WorldBounds.xMin + EdgeMargin + range, yMin, WorldBounds.xMax - EdgeMargin - range, yMax);
                    seal.MoveRange = range;
                    for (int i = 0; i < 40; i++)
                    {
                        var p = new Vector2(rng.Range(area.xMin, area.xMax), rng.Range(area.yMin, area.yMax));
                        if (!IsFree(plan, p, seal.Scale, range, null)) continue;
                        seal.Position = p;
                        seal.Flip = rng.Chance(0.5f);
                        seal.HasWaterCover = !world.Underwater;
                        return true;
                    }
                    return false;
                }

                default:
                {
                    if (!TryFindOpenSpot(world, rng, plan, seal.Scale, null, out var p)) return false;
                    seal.Position = p;
                    seal.Flip = rng.Chance(0.5f);
                    return true;
                }
            }
        }

        static bool TryPlaceBehindProp(SealPlacement seal, WorldDefinition world, SeededRandom rng, ScenePlan plan, HashSet<int> usedHiders)
        {
            var candidates = new List<int>();
            for (int i = 0; i < plan.Props.Count; i++)
            {
                var p = plan.Props[i];
                if (usedHiders.Contains(i) || !PropCatalog.Get(p.Kind).CanHide) continue;
                if (world.ZoneAt(p.Position.y) == Zone.Sky || p.Position.y > MaxSealY - 1f) continue;
                candidates.Add(i);
            }
            rng.Shuffle(candidates);

            foreach (int idx in candidates)
            {
                var prop = plan.Props[idx];
                var size = prop.Size;
                float scale = Mathf.Clamp(Mathf.Min(seal.Scale, size.y * 0.8f / 0.95f), 0.42f, seal.Scale);
                float side = rng.Sign();
                var pos = prop.Position + new Vector2(side * size.x * 0.42f, Mathf.Clamp(size.y * 0.12f, 0.08f, 0.3f));
                if (!IsInside(pos, scale) || !IsFree(plan, pos, scale, 0f, null)) continue;

                seal.Scale = scale;
                seal.Position = pos;
                seal.Flip = side < 0f; // Kopf zeigt nach außen
                seal.HiderProp = idx;
                // Hinter Boot oder Eisberg: Robbe schwimmt im Wasser
                seal.HasWaterCover = !world.Underwater && world.ZoneAt(pos.y) == Zone.Water;
                usedHiders.Add(idx);
                return true;
            }
            return false;
        }

        static bool TryFindOpenSpot(WorldDefinition world, SeededRandom rng, ScenePlan plan, float scale, SealPlacement ignore, out Vector2 pos)
        {
            Rect area = world.Underwater
                ? Rect.MinMaxRect(WorldBounds.xMin + EdgeMargin, world.ShoreY + 0.5f, WorldBounds.xMax - EdgeMargin, MaxSealY - scale)
                : Rect.MinMaxRect(WorldBounds.xMin + EdgeMargin, WorldBounds.yMin + 0.2f, WorldBounds.xMax - EdgeMargin,
                    Mathf.Min(world.ShoreY - 0.95f * scale, MaxSealY - scale));

            for (int i = 0; i < 50 && area.height > 0f; i++)
            {
                var p = new Vector2(rng.Range(area.xMin, area.xMax), rng.Range(area.yMin, area.yMax));
                if (IsFree(plan, p, scale, 0f, ignore))
                {
                    pos = p;
                    return true;
                }
            }
            pos = default;
            return false;
        }

        static void ForcePlace(SealPlacement seal, WorldDefinition world, SeededRandom rng)
        {
            seal.Style = HidingStyle.Open;
            float top = world.Underwater ? MaxSealY - seal.Scale : Mathf.Min(world.ShoreY - seal.Scale, MaxSealY - seal.Scale);
            seal.Position = new Vector2(rng.Range(-14f, 14f), rng.Range(WorldBounds.yMin + 0.2f, Mathf.Max(WorldBounds.yMin + 0.3f, top)));
        }

        static bool IsInside(Vector2 pos, float scale)
        {
            var r = SealPlacement.SealBounds(pos, scale);
            return r.xMin > WorldBounds.xMin + 0.3f && r.xMax < WorldBounds.xMax - 0.3f &&
                   r.yMin > WorldBounds.yMin && pos.y < MaxSealY;
        }

        static Rect PathBounds(Vector2 pos, float scale, float range)
        {
            var r = SealPlacement.SealBounds(pos, scale);
            r.xMin -= range;
            r.xMax += range;
            return r;
        }

        static bool IsFree(ScenePlan plan, Vector2 pos, float scale, float range, SealPlacement ignore)
        {
            var me = PathBounds(pos, scale, range);
            me.xMin -= 0.3f; me.xMax += 0.3f; me.yMin -= 0.25f; me.yMax += 0.25f;
            foreach (var s in plan.Seals)
            {
                if (s == ignore) continue;
                if (me.Overlaps(PathBounds(s.Position, s.Scale, s.MoveRange))) return false;
                if (s.Trickster && me.Overlaps(SealPlacement.SealBounds(s.AltPosition, s.Scale))) return false;
            }
            foreach (var d in plan.Decoys)
                if (me.Overlaps(d.Bounds)) return false;
            return true;
        }

        // ------------------------------------------------------------------ Köder & Aufräumen

        static void PlaceDecoys(WorldDefinition world, LevelDefinition level, SeededRandom rng, ScenePlan plan)
        {
            var area = Rect.MinMaxRect(WorldBounds.xMin + EdgeMargin, WorldBounds.yMin + 0.2f, WorldBounds.xMax - EdgeMargin, world.ShoreY - 1.1f);
            if (area.height <= 0f) return;
            for (int n = 0; n < level.Decoys; n++)
            {
                float scale = rng.Range(0.8f, 1.05f) * level.SealScale;
                for (int i = 0; i < 30; i++)
                {
                    var p = new Vector2(rng.Range(area.xMin, area.xMax), rng.Range(area.yMin, area.yMax));
                    if (!IsFree(plan, p, scale, 0f, null)) continue;
                    plan.Decoys.Add(new PropPlacement { Kind = PropKind.SealRock, Variant = rng.Range(0, 2), Position = p, Scale = scale, Flip = rng.Chance(0.5f) });
                    break;
                }
            }
        }

        /// <summary>
        /// Entfernt Deko, die VOR einer Robbe steht und sie überlappt – sonst wäre sie unfindbar.
        /// (Der gewollte "Versteck"-Prop bleibt natürlich stehen.)
        /// </summary>
        static void RemoveOccluders(ScenePlan plan)
        {
            var remove = new bool[plan.Props.Count];
            var hiders = new HashSet<int>();
            foreach (var s in plan.Seals)
                if (s.HiderProp >= 0) hiders.Add(s.HiderProp);

            void Check(Rect target, float targetY, int keep)
            {
                for (int i = 0; i < plan.Props.Count; i++)
                {
                    // Versteck-Deko anderer Robben bleibt immer stehen
                    if (i == keep || remove[i] || hiders.Contains(i)) continue;
                    var p = plan.Props[i];
                    if (p.Kind == PropKind.Sun || p.Kind == PropKind.Cloud || p.Kind == PropKind.GullFlying) continue;
                    bool inFront = p.Position.y < targetY + 0.05f;
                    var pr = p.Bounds;
                    if (!inFront)
                    {
                        // Große Deko dahinter ist ok. Nur wenn sie fast auf gleicher Höhe steht, stört sie.
                        if (p.Position.y > targetY + 0.4f) continue;
                    }
                    if (pr.Overlaps(target)) remove[i] = true;
                }
            }

            foreach (var s in plan.Seals)
            {
                Check(PathBounds(s.Position, s.Scale, s.MoveRange), s.Position.y, s.HiderProp);
                if (s.Trickster) Check(SealPlacement.SealBounds(s.AltPosition, s.Scale), s.AltPosition.y, -1);
            }
            foreach (var d in plan.Decoys) Check(d.Bounds, d.Position.y, -1);

            // Liste neu aufbauen und Versteck-Indizes umrechnen
            var remap = new int[plan.Props.Count];
            var kept = new List<PropPlacement>();
            for (int i = 0; i < plan.Props.Count; i++)
            {
                remap[i] = remove[i] ? -1 : kept.Count;
                if (!remove[i]) kept.Add(plan.Props[i]);
            }
            plan.Props.Clear();
            plan.Props.AddRange(kept);
            foreach (var s in plan.Seals)
                if (s.HiderProp >= 0) s.HiderProp = remap[s.HiderProp];
        }
    }
}
