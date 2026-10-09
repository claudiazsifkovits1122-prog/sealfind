using System;
using UnityEngine;

namespace SealFind
{
    public enum Zone { Sky, Water, Ground }

    public enum PropKind
    {
        Rock, RockSmall, SealRock, Umbrella, Palm, Shell, Starfish, Gull, GullFlying, Boat, Buoy,
        Crate, Barrel, IceBlock, Iceberg, Penguin, Fish, Seaweed, Coral, Cloud, Sun, Ball, Towel,
        Sandcastle, Crab, Lantern, Chest, Flag, Bollard, Snowman, Bubble, Anchor
    }

    /// <summary>Welche Deko-Objekte in welcher Zone einer Welt vorkommen.</summary>
    [Serializable]
    public sealed class PropRule
    {
        public PropKind Kind;
        public Zone Zone;
        public float Weight;
        public float MinScale;
        public float MaxScale;

        public PropRule(PropKind kind, Zone zone, float weight, float minScale = 0.85f, float maxScale = 1.15f)
        {
            Kind = kind; Zone = zone; Weight = weight; MinScale = minScale; MaxScale = maxScale;
        }
    }

    /// <summary>
    /// Eine Welt (Strand, Hafen, Eiswelt …): Farbpalette, Aufteilung in Zonen und Deko-Regeln.
    /// Das Bild ist in drei horizontale Zonen geteilt: Himmel über <see cref="HorizonY"/>,
    /// Wasser zwischen <see cref="ShoreY"/> und <see cref="HorizonY"/>, Boden darunter.
    /// </summary>
    [Serializable]
    public sealed class WorldDefinition
    {
        public int Index;
        public string Id;
        public string Name;
        public string Tagline;

        public Color SkyTop, SkyBottom, WaterTop, WaterBottom, GroundTop, GroundBottom, RockTint, Foam;
        public Color Ambient = Color.white;

        public float HorizonY;
        public float ShoreY;

        public bool Night;
        public bool Underwater;
        public bool HasSun = true;
        public int StarsToUnlock;
        public float Camouflage;
        public Accessory ThemeAccessory = Accessory.Sunglasses;
        public PropRule[] Props = Array.Empty<PropRule>();

        public bool HasWater => HorizonY - ShoreY > 1.5f;

        public Rect ZoneRect(Zone zone, Rect bounds)
        {
            switch (zone)
            {
                case Zone.Sky:
                    return Rect.MinMaxRect(bounds.xMin, Mathf.Min(HorizonY, bounds.yMax), bounds.xMax, bounds.yMax);
                case Zone.Water:
                    return Rect.MinMaxRect(bounds.xMin, ShoreY, bounds.xMax, Mathf.Min(HorizonY, bounds.yMax));
                default:
                    return Rect.MinMaxRect(bounds.xMin, bounds.yMin, bounds.xMax, ShoreY);
            }
        }

        public Zone ZoneAt(float y) => y >= HorizonY ? Zone.Sky : y >= ShoreY ? Zone.Water : Zone.Ground;

        public Color WaterColorAt(float y) =>
            Color.Lerp(WaterBottom, WaterTop, Mathf.InverseLerp(ShoreY, HorizonY, y));

        public Color GroundColorAt(float y) =>
            Color.Lerp(GroundBottom, GroundTop, Mathf.InverseLerp(-9f, ShoreY, y));

        /// <summary>Umgebungsfarbe an einer Stelle – für Tarnung der Robben.</summary>
        public Color EnvironmentColorAt(float y) =>
            ZoneAt(y) == Zone.Ground ? GroundColorAt(y) : WaterColorAt(y);
    }
}
