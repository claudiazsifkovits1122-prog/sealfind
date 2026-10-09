using System;
using System.Collections.Generic;
using UnityEngine;

namespace SealFind
{
    /// <summary>
    /// Alle Spielinhalte an einem Ort: Welten, Robben-Arten und die Level-Kurve.
    /// Neue Welt? Einfach in <see cref="BuildWorlds"/> ergänzen. Neue Robbe? In <see cref="BuildSpecies"/>.
    /// </summary>
    public static class ContentDatabase
    {
        public const int LevelsPerWorld = 6;

        static WorldDefinition[] _worlds;
        static SealSpecies[] _species;

        public static IReadOnlyList<WorldDefinition> Worlds => _worlds ?? (_worlds = BuildWorlds());
        public static IReadOnlyList<SealSpecies> Species => _species ?? (_species = BuildSpecies());

        public static int MaxStars => Worlds.Count * LevelsPerWorld * 3;

        public static SealSpecies FindSpecies(string id)
        {
            foreach (var s in Species) if (s.Id == id) return s;
            return null;
        }

        public static int SpeciesIndex(string id)
        {
            for (int i = 0; i < Species.Count; i++) if (Species[i].Id == id) return i;
            return -1;
        }

        // ------------------------------------------------------------------ Level-Kurve

        struct LevelTemplate
        {
            public GoalType Goal;
            public int Targets;
            public int Extra;
            public HidingStyle Styles;
            public float TimeLimit;
            public string Name;
        }

        static readonly LevelTemplate[] Templates =
        {
            new LevelTemplate { Name = "Aufwärmen", Goal = GoalType.FindSeals, Targets = 5, Styles = HidingStyle.Open | HidingStyle.BehindProp },
            new LevelTemplate { Name = "Gut versteckt", Goal = GoalType.FindSeals, Targets = 8, Styles = HidingStyle.Basic | HidingStyle.InWater | HidingStyle.Tiny | HidingStyle.Sleeping },
            new LevelTemplate { Name = "Kindergarten", Goal = GoalType.FindBabies, Targets = 4, Extra = 5, Styles = HidingStyle.Basic | HidingStyle.InWater | HidingStyle.Sleeping },
            new LevelTemplate { Name = "Modenschau", Goal = GoalType.FindAccessory, Targets = 3, Extra = 6, Styles = HidingStyle.Basic | HidingStyle.InWater | HidingStyle.Tiny },
            new LevelTemplate { Name = "Gegen die Uhr", Goal = GoalType.TimeAttack, Targets = 10, Styles = HidingStyle.Basic | HidingStyle.InWater | HidingStyle.Peek, TimeLimit = 80f },
            new LevelTemplate { Name = "Finale", Goal = GoalType.FindSeals, Targets = 12, Styles = HidingStyle.All },
        };

        public static string LevelName(int level) => Templates[Mathf.Clamp(level, 0, Templates.Length - 1)].Name;

        public static LevelDefinition BuildLevel(int world, int level)
        {
            var w = Worlds[Mathf.Clamp(world, 0, Worlds.Count - 1)];
            var t = Templates[Mathf.Clamp(level, 0, Templates.Length - 1)];
            int tier = w.Index;

            var def = new LevelDefinition
            {
                Mode = GameMode.Campaign,
                World = w.Index,
                Level = level,
                Seed = 7919 * (w.Index + 1) + 104729 * (level + 1),
                Goal = t.Goal,
                GoalAccessory = t.Goal == GoalType.FindAccessory ? w.ThemeAccessory : Accessory.None,
                TargetCount = t.Targets + (t.Goal == GoalType.FindSeals || t.Goal == GoalType.TimeAttack ? tier : tier / 3),
                ExtraSeals = t.Extra > 0 ? t.Extra + tier / 2 : 0,
                Decoys = 1 + tier + level / 2,
                Styles = t.Styles,
                PropDensity = Mathf.Min(1.25f, 0.55f + tier * 0.09f + level * 0.04f),
                SealScale = Mathf.Max(0.72f, 1.0f - tier * 0.035f - level * 0.01f),
                Camouflage = Mathf.Min(0.55f, 0.05f + tier * 0.06f) + w.Camouflage,
                TimeLimit = t.TimeLimit > 0 ? t.TimeLimit + tier * 6f : 0f,
                BonusChance = 0.08f + level * 0.02f,
            };
            if (w.Underwater) def.Styles |= HidingStyle.Moving;
            def.ParTime = ParTimeFor(def);
            // Zeitlimit wächst mit der Schwierigkeit mit, bleibt aber knackig
            if (def.IsTimed) def.TimeLimit = Mathf.Max(def.TimeLimit, Mathf.Round(def.ParTime * 1.1f / 5f) * 5f);
            return def;
        }

        /// <summary>Tages-Challenge: für alle Spieler am gleichen Tag das gleiche Bild.</summary>
        public static LevelDefinition BuildDaily(DateTime date)
        {
            int seed = date.Year * 10000 + date.Month * 100 + date.Day;
            var rng = new SeededRandom(seed);
            int world = rng.Range(0, Worlds.Count);
            var w = Worlds[world];
            var def = new LevelDefinition
            {
                Mode = GameMode.Daily,
                World = world,
                Seed = seed,
                Goal = GoalType.FindSeals,
                TargetCount = 15,
                Decoys = 5,
                Styles = HidingStyle.All,
                PropDensity = 1.0f,
                SealScale = 0.85f,
                Camouflage = 0.3f + w.Camouflage,
                BonusChance = 0.35f,
            };
            def.ParTime = ParTimeFor(def);
            return def;
        }

        /// <summary>Zufallsrunde für "Noch eine Runde!" nach der Kampagne.</summary>
        public static LevelDefinition BuildQuick(int seed, int maxWorld)
        {
            var rng = new SeededRandom(seed);
            var def = BuildLevel(rng.Range(0, Mathf.Clamp(maxWorld + 1, 1, Worlds.Count)), rng.Range(0, LevelsPerWorld));
            def.Mode = GameMode.Quick;
            def.Seed = seed;
            return def;
        }

        /// <summary>Hintergrund-Szene fürs Hauptmenü.</summary>
        public static LevelDefinition BuildBackdrop(int seed)
        {
            return new LevelDefinition
            {
                Mode = GameMode.Backdrop,
                World = 0,
                Seed = seed,
                Goal = GoalType.FindSeals,
                TargetCount = 6,
                Styles = HidingStyle.Open | HidingStyle.InWater | HidingStyle.BehindProp | HidingStyle.Sleeping,
                PropDensity = 0.55f,
                BonusChance = 0f,
            };
        }

        static float ParTimeFor(LevelDefinition def)
        {
            float perSeal = 4.5f + def.World * 0.6f + def.PropDensity * 1.5f;
            if (def.Goal == GoalType.FindBabies || def.Goal == GoalType.FindAccessory) perSeal += 3f;
            return Mathf.Round(def.TargetCount * perSeal / 5f) * 5f;
        }

        public static string GoalText(LevelDefinition def)
        {
            switch (def.Goal)
            {
                case GoalType.FindBabies:
                    return $"Finde {def.TargetCount} Babyrobben";
                case GoalType.FindAccessory:
                    return $"Finde {def.TargetCount} Robben mit {AccessoryName(def.GoalAccessory)}";
                case GoalType.TimeAttack:
                    return $"Finde {def.TargetCount} Robben in {Mathf.RoundToInt(def.TimeLimit)} Sekunden";
                default:
                    return def.Styles.HasFlag(HidingStyle.Moving) && def.Mode == GameMode.Campaign
                        ? $"Finde {def.TargetCount} Robben – manche bewegen sich!"
                        : $"Finde {def.TargetCount} Robben";
            }
        }

        public static string AccessoryName(Accessory a)
        {
            switch (a)
            {
                case Accessory.Sunglasses: return "Sonnenbrille";
                case Accessory.PirateHat: return "Piratenhut";
                case Accessory.Scarf: return "Schal";
                case Accessory.Crown: return "Krone";
                case Accessory.Snorkel: return "Schnorchel";
                case Accessory.PartyHat: return "Partyhut";
                default: return "nichts";
            }
        }

        // ------------------------------------------------------------------ Welten

        /// <summary>"#RRGGBB" → Color (eigener Parser, damit Inhalte auch ohne Unity-Engine testbar sind).</summary>
        static Color C(string hex)
        {
            int v = Convert.ToInt32(hex.TrimStart('#'), 16);
            return new Color32((byte)(v >> 16), (byte)(v >> 8), (byte)v, 255);
        }

        static WorldDefinition[] BuildWorlds()
        {
            var list = new List<WorldDefinition>
            {
                new WorldDefinition
                {
                    Id = "beach", Name = "Strand", Tagline = "Sonne, Sand und sehr faule Robben.",
                    SkyTop = C("#6EC6FF"), SkyBottom = C("#D4F1FF"),
                    WaterTop = C("#2E9BD6"), WaterBottom = C("#5FD3E6"),
                    GroundTop = C("#F6E2A8"), GroundBottom = C("#E9C77B"),
                    RockTint = C("#9AA3AD"), Foam = C("#FFFFFF"),
                    HorizonY = 5f, ShoreY = 1.2f, StarsToUnlock = 0,
                    ThemeAccessory = Accessory.Sunglasses,
                    Props = new[]
                    {
                        new PropRule(PropKind.Rock, Zone.Ground, 4), new PropRule(PropKind.RockSmall, Zone.Ground, 3),
                        new PropRule(PropKind.Umbrella, Zone.Ground, 3), new PropRule(PropKind.Palm, Zone.Ground, 1.5f),
                        new PropRule(PropKind.Shell, Zone.Ground, 3), new PropRule(PropKind.Starfish, Zone.Ground, 2),
                        new PropRule(PropKind.Gull, Zone.Ground, 2), new PropRule(PropKind.Ball, Zone.Ground, 1),
                        new PropRule(PropKind.Towel, Zone.Ground, 1.5f), new PropRule(PropKind.Sandcastle, Zone.Ground, 1.5f),
                        new PropRule(PropKind.Crab, Zone.Ground, 1.5f),
                        new PropRule(PropKind.Rock, Zone.Water, 3), new PropRule(PropKind.Buoy, Zone.Water, 1.5f),
                        new PropRule(PropKind.Boat, Zone.Water, 0.8f, 0.8f, 1.0f),
                        new PropRule(PropKind.Cloud, Zone.Sky, 3), new PropRule(PropKind.GullFlying, Zone.Sky, 2),
                    }
                },
                new WorldDefinition
                {
                    Id = "harbor", Name = "Hafen", Tagline = "Zwischen Kisten, Fässern und Booten.",
                    SkyTop = C("#7FB3D5"), SkyBottom = C("#E3EEF5"),
                    WaterTop = C("#1F6F8B"), WaterBottom = C("#3C9BB5"),
                    GroundTop = C("#B98A5A"), GroundBottom = C("#8E643D"),
                    RockTint = C("#8A8F96"), Foam = C("#E8F6FF"),
                    HorizonY = 5.5f, ShoreY = -0.5f, StarsToUnlock = 10,
                    ThemeAccessory = Accessory.Scarf,
                    Props = new[]
                    {
                        new PropRule(PropKind.Crate, Zone.Ground, 4), new PropRule(PropKind.Barrel, Zone.Ground, 4),
                        new PropRule(PropKind.Bollard, Zone.Ground, 2), new PropRule(PropKind.Anchor, Zone.Ground, 1),
                        new PropRule(PropKind.Gull, Zone.Ground, 3), new PropRule(PropKind.Lantern, Zone.Ground, 1),
                        new PropRule(PropKind.RockSmall, Zone.Ground, 1),
                        new PropRule(PropKind.Boat, Zone.Water, 3, 0.8f, 1.1f), new PropRule(PropKind.Buoy, Zone.Water, 3),
                        new PropRule(PropKind.Rock, Zone.Water, 1.5f), new PropRule(PropKind.Barrel, Zone.Water, 1, 0.7f, 0.9f),
                        new PropRule(PropKind.Cloud, Zone.Sky, 3), new PropRule(PropKind.GullFlying, Zone.Sky, 3),
                    }
                },
                new WorldDefinition
                {
                    Id = "ice", Name = "Eiswelt", Tagline = "Weiße Robben auf weißem Eis. Viel Glück!",
                    SkyTop = C("#9CC9E8"), SkyBottom = C("#EAF6FF"),
                    WaterTop = C("#2A7AA8"), WaterBottom = C("#4FA8CF"),
                    GroundTop = C("#F4FAFF"), GroundBottom = C("#D5E7F5"),
                    RockTint = C("#B6C7D6"), Foam = C("#FFFFFF"),
                    HorizonY = 5f, ShoreY = 1.8f, StarsToUnlock = 22, Camouflage = 0.1f,
                    ThemeAccessory = Accessory.Scarf,
                    Props = new[]
                    {
                        new PropRule(PropKind.IceBlock, Zone.Ground, 5), new PropRule(PropKind.Snowman, Zone.Ground, 1.5f),
                        new PropRule(PropKind.Penguin, Zone.Ground, 4), new PropRule(PropKind.RockSmall, Zone.Ground, 2),
                        new PropRule(PropKind.Rock, Zone.Ground, 1.5f), new PropRule(PropKind.Fish, Zone.Ground, 0.5f),
                        new PropRule(PropKind.Iceberg, Zone.Water, 3, 0.7f, 1.0f), new PropRule(PropKind.IceBlock, Zone.Water, 2),
                        new PropRule(PropKind.Cloud, Zone.Sky, 3),
                    }
                },
                new WorldDefinition
                {
                    Id = "sunset", Name = "Sonnenuntergang", Tagline = "Goldenes Licht, lange Schatten.",
                    SkyTop = C("#5B4B8A"), SkyBottom = C("#FFB36B"),
                    WaterTop = C("#C0607A"), WaterBottom = C("#6B6FA8"),
                    GroundTop = C("#E7B98A"), GroundBottom = C("#C48D63"),
                    RockTint = C("#8E7A80"), Foam = C("#FFE3C7"),
                    Ambient = C("#FFE2CC"),
                    HorizonY = 4.5f, ShoreY = 0.6f, StarsToUnlock = 36, Camouflage = 0.05f,
                    ThemeAccessory = Accessory.Sunglasses,
                    Props = new[]
                    {
                        new PropRule(PropKind.Rock, Zone.Ground, 5), new PropRule(PropKind.RockSmall, Zone.Ground, 3),
                        new PropRule(PropKind.Palm, Zone.Ground, 2), new PropRule(PropKind.Umbrella, Zone.Ground, 2),
                        new PropRule(PropKind.Shell, Zone.Ground, 3), new PropRule(PropKind.Crab, Zone.Ground, 2),
                        new PropRule(PropKind.Towel, Zone.Ground, 1.5f), new PropRule(PropKind.Gull, Zone.Ground, 2),
                        new PropRule(PropKind.Rock, Zone.Water, 4), new PropRule(PropKind.Boat, Zone.Water, 1, 0.8f, 1f),
                        new PropRule(PropKind.Cloud, Zone.Sky, 3), new PropRule(PropKind.GullFlying, Zone.Sky, 2),
                    }
                },
                new WorldDefinition
                {
                    Id = "night", Name = "Nacht", Tagline = "Im Laternenlicht sieht jeder Stein wie eine Robbe aus.",
                    SkyTop = C("#0B1236"), SkyBottom = C("#27306B"),
                    WaterTop = C("#14244F"), WaterBottom = C("#22407A"),
                    GroundTop = C("#5A5F7A"), GroundBottom = C("#3B3F57"),
                    RockTint = C("#7A7F99"), Foam = C("#B9C6FF"),
                    Ambient = C("#A9B4E6"),
                    HorizonY = 5f, ShoreY = 0.2f, StarsToUnlock = 50, Night = true, Camouflage = 0.1f,
                    ThemeAccessory = Accessory.PartyHat,
                    Props = new[]
                    {
                        new PropRule(PropKind.Rock, Zone.Ground, 5), new PropRule(PropKind.Lantern, Zone.Ground, 2.5f),
                        new PropRule(PropKind.Crate, Zone.Ground, 2), new PropRule(PropKind.Barrel, Zone.Ground, 2),
                        new PropRule(PropKind.RockSmall, Zone.Ground, 3), new PropRule(PropKind.Bollard, Zone.Ground, 1.5f),
                        new PropRule(PropKind.Rock, Zone.Water, 3), new PropRule(PropKind.Boat, Zone.Water, 1.5f, 0.8f, 1f),
                        new PropRule(PropKind.Buoy, Zone.Water, 2),
                        new PropRule(PropKind.Cloud, Zone.Sky, 1.5f),
                    }
                },
                new WorldDefinition
                {
                    Id = "pirates", Name = "Pirateninsel", Tagline = "Arrr! Hier liegt ein Schatz – und viele Robben.",
                    SkyTop = C("#4FB3E8"), SkyBottom = C("#C9EEFF"),
                    WaterTop = C("#127A9C"), WaterBottom = C("#29B5C9"),
                    GroundTop = C("#EDD08E"), GroundBottom = C("#D4AE62"),
                    RockTint = C("#857B70"), Foam = C("#FFFFFF"),
                    HorizonY = 5f, ShoreY = 1.5f, StarsToUnlock = 66, Camouflage = 0.1f,
                    ThemeAccessory = Accessory.PirateHat,
                    Props = new[]
                    {
                        new PropRule(PropKind.Chest, Zone.Ground, 3), new PropRule(PropKind.Barrel, Zone.Ground, 3),
                        new PropRule(PropKind.Palm, Zone.Ground, 3), new PropRule(PropKind.Flag, Zone.Ground, 1.5f),
                        new PropRule(PropKind.Rock, Zone.Ground, 3), new PropRule(PropKind.Crab, Zone.Ground, 2),
                        new PropRule(PropKind.Anchor, Zone.Ground, 1), new PropRule(PropKind.Shell, Zone.Ground, 2),
                        new PropRule(PropKind.Crate, Zone.Ground, 2),
                        new PropRule(PropKind.Boat, Zone.Water, 2, 0.9f, 1.15f), new PropRule(PropKind.Rock, Zone.Water, 3),
                        new PropRule(PropKind.Cloud, Zone.Sky, 3), new PropRule(PropKind.GullFlying, Zone.Sky, 2),
                    }
                },
                new WorldDefinition
                {
                    Id = "underwater", Name = "Unterwasser", Tagline = "Robben zwischen Fischen, Korallen und Blasen.",
                    SkyTop = C("#1C8FB8"), SkyBottom = C("#1C8FB8"),
                    WaterTop = C("#3FC1E0"), WaterBottom = C("#0E4E7A"),
                    GroundTop = C("#E2CF9A"), GroundBottom = C("#B79A62"),
                    RockTint = C("#6E8A93"), Foam = C("#BFF3FF"),
                    Ambient = C("#D5F2FF"),
                    HorizonY = 9.5f, ShoreY = -5.5f, StarsToUnlock = 84, Underwater = true, HasSun = false, Camouflage = 0.1f,
                    ThemeAccessory = Accessory.Snorkel,
                    Props = new[]
                    {
                        new PropRule(PropKind.Coral, Zone.Ground, 4), new PropRule(PropKind.Seaweed, Zone.Ground, 4),
                        new PropRule(PropKind.Rock, Zone.Ground, 3), new PropRule(PropKind.Chest, Zone.Ground, 1),
                        new PropRule(PropKind.Starfish, Zone.Ground, 2), new PropRule(PropKind.Shell, Zone.Ground, 2),
                        new PropRule(PropKind.Fish, Zone.Water, 6), new PropRule(PropKind.Bubble, Zone.Water, 3),
                        new PropRule(PropKind.Seaweed, Zone.Water, 1.5f), new PropRule(PropKind.Coral, Zone.Water, 1.5f),
                        new PropRule(PropKind.Rock, Zone.Water, 1.5f),
                    }
                },
            };
            for (int i = 0; i < list.Count; i++) list[i].Index = i;
            return list.ToArray();
        }

        // ------------------------------------------------------------------ Robben

        static SealSpecies S(string id, string name, SealRarity rarity, string color, string bio,
            bool baby = false, Accessory acc = Accessory.None, float size = 1f, bool golden = false, params int[] worlds)
        {
            return new SealSpecies
            {
                Id = id, Name = name, Rarity = rarity, BodyColor = C(color), Bio = bio,
                IsBaby = baby, Accessory = acc, Size = baby ? size * 0.72f : size, Golden = golden,
                Worlds = worlds ?? Array.Empty<int>(),
            };
        }

        // Welt-Indizes zur besseren Lesbarkeit
        const int Beach = 0, Harbor = 1, Ice = 2, Sunset = 3, Night = 4, Pirates = 5, Underwater = 6;

        static SealSpecies[] BuildSpecies()
        {
            return new[]
            {
                S("bruno", "Bruno", SealRarity.Common, "#9C8F86", "Liebt Fische und hasst Montage."),
                S("gerda", "Gerda", SealRarity.Common, "#B8BEC6", "Hat noch nie einen Sonnenuntergang verpasst."),
                S("otto", "Otto", SealRarity.Common, "#7D848E", "Schläft 20 Stunden am Tag. Mindestens."),
                S("mia", "Mia", SealRarity.Common, "#C9B79C", "Sammelt glänzende Steine. Und Muscheln. Und alles andere."),
                S("hannes", "Hannes", SealRarity.Common, "#8FA2B5", "Behauptet, er hätte mal einen Hai gesehen."),
                S("pummel", "Pummel", SealRarity.Common, "#A7998F", "Hat heute schon drei Fische gegessen. Vor dem Frühstück.", size: 1.12f),
                S("frieda", "Frieda", SealRarity.Common, "#B3A79B", "Grüßt jede Möwe persönlich."),

                S("kruemel", "Krümel", SealRarity.Uncommon, "#F2EFEA", "Noch ganz flauschig. Und ganz neugierig.", baby: true),
                S("lotte", "Lotte", SealRarity.Uncommon, "#EFE3CC", "Übt heimlich Purzelbäume.", baby: true),
                S("flocke", "Flocke", SealRarity.Uncommon, "#FFFFFF", "Ist im Schnee praktisch unsichtbar.", baby: true, worlds: new[] { Ice, Night }),
                S("pieps", "Pieps", SealRarity.Uncommon, "#D9DEE4", "Macht Geräusche wie ein Quietscheentchen.", baby: true),
                S("sigrid", "Sigrid", SealRarity.Uncommon, "#A9B3BD", "Strickt sich jeden Winter einen neuen Schal.", acc: Accessory.Scarf),
                S("ole", "Ole", SealRarity.Uncommon, "#8E9BA6", "Hält sich für einen Tiefseetaucher.", acc: Accessory.Snorkel, worlds: new[] { Beach, Underwater, Pirates }),
                S("paul", "Party-Paul", SealRarity.Uncommon, "#A39A92", "Feiert jeden Tag Geburtstag. Sicher ist sicher.", acc: Accessory.PartyHat),

                S("carl", "Cool Carl", SealRarity.Rare, "#8B97A3", "Trägt die Sonnenbrille auch nachts.", acc: Accessory.Sunglasses),
                S("sonja", "Sonnige Sonja", SealRarity.Rare, "#C7AE92", "Hat den besten Platz am Strand reserviert. Seit 1998.", acc: Accessory.Sunglasses),
                S("ray", "Baby Ray", SealRarity.Rare, "#ECE6DE", "Zu cool für die Schule.", baby: true, acc: Accessory.Sunglasses),
                S("piet", "Pirat Piet", SealRarity.Rare, "#857A70", "Sucht seit Jahren seinen Schatz. Hat ihn nie vergraben.", acc: Accessory.PirateHat),
                S("nora", "Nachteule Nora", SealRarity.Rare, "#5E6A8C", "Kommt erst raus, wenn die Sterne leuchten.", worlds: new[] { Night, Sunset }),
                S("ulli", "Ulli Unsichtbar", SealRarity.Rare, "#E3D3A8", "Gewinnt jedes Versteckspiel.", worlds: new[] { Beach, Pirates, Sunset }),
                S("bibi", "Blubber-Bibi", SealRarity.Rare, "#7FA6B8", "Macht die schönsten Luftblasen der Welt.", acc: Accessory.Snorkel),

                S("klaus", "Kapitän Klaus", SealRarity.Epic, "#6F6B66", "Hat sieben Meere besegelt. Oder einen Teich.", acc: Accessory.PirateHat, size: 1.1f),
                S("flosse", "Dr. Flosse", SealRarity.Epic, "#9AA7A1", "Hat über 300 Fischarten katalogisiert. Und gegessen.", acc: Accessory.Scarf),
                S("schorsch", "Schelm Schorsch", SealRarity.Epic, "#A08E7A", "Versteckt sich gern zweimal.", acc: Accessory.PartyHat),
                S("rosalie", "Rosalie", SealRarity.Epic, "#E7A9B5", "Die einzige rosa Robbe der Welt. Sagt sie."),
                S("ahoi", "Admiralin Ahoi", SealRarity.Epic, "#4F5D75", "Kommandiert die Möwen. Die Möwen hören nicht zu.", acc: Accessory.PirateHat, worlds: new[] { Pirates, Harbor }),

                S("goldi", "Goldi", SealRarity.Legendary, "#FFC93C", "Glänzt mehr als die Sonne. Und weiß das auch.", golden: true),
                S("robbia", "Königin Robbia", SealRarity.Legendary, "#D8DDE6", "Regiert die Eisschollen mit sanfter Flosse.", acc: Accessory.Crown, size: 1.1f),
                S("sternchen", "Sternchen", SealRarity.Legendary, "#FFE38A", "Eine Babyrobbe aus Sternenstaub.", baby: true, golden: true, worlds: new[] { Night, Ice, Underwater }),
            };
        }
    }
}
