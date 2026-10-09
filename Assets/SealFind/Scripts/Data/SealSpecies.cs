using System;
using UnityEngine;

namespace SealFind
{
    public enum SealRarity { Common, Uncommon, Rare, Epic, Legendary }

    public enum Accessory { None, Sunglasses, PirateHat, Scarf, Crown, Snorkel, PartyHat }

    /// <summary>
    /// Eine sammelbare Robben-Art (Eintrag im Sammelalbum).
    /// Reine Daten – das Aussehen wird von <see cref="ArtLibrary"/> prozedural erzeugt.
    /// </summary>
    [Serializable]
    public sealed class SealSpecies
    {
        public string Id;
        public string Name;
        public string Bio;
        public SealRarity Rarity;
        public Color32 BodyColor;
        public bool IsBaby;
        public Accessory Accessory;
        public float Size = 1f;
        public bool Golden;
        /// <summary>Welt-Indizes, in denen die Art vorkommt. Leer = überall.</summary>
        public int[] Worlds = Array.Empty<int>();

        public bool LivesIn(int world) => Worlds == null || Worlds.Length == 0 || Array.IndexOf(Worlds, world) >= 0;
    }

    public static class RarityInfo
    {
        public static int Points(SealRarity r)
        {
            switch (r)
            {
                case SealRarity.Uncommon: return 200;
                case SealRarity.Rare: return 500;
                case SealRarity.Epic: return 800;
                case SealRarity.Legendary: return 1000;
                default: return 100;
            }
        }

        /// <summary>Gewicht für die zufällige Auswahl beim Verstecken.</summary>
        public static float Weight(SealRarity r)
        {
            switch (r)
            {
                case SealRarity.Uncommon: return 25f;
                case SealRarity.Rare: return 10f;
                case SealRarity.Epic: return 4f;
                case SealRarity.Legendary: return 1f;
                default: return 60f;
            }
        }

        public static string Label(SealRarity r)
        {
            switch (r)
            {
                case SealRarity.Uncommon: return "Ungewöhnlich";
                case SealRarity.Rare: return "Selten";
                case SealRarity.Epic: return "Episch";
                case SealRarity.Legendary: return "Legendär";
                default: return "Häufig";
            }
        }

        public static Color Color(SealRarity r)
        {
            switch (r)
            {
                case SealRarity.Uncommon: return new Color32(76, 175, 80, 255);
                case SealRarity.Rare: return new Color32(33, 150, 243, 255);
                case SealRarity.Epic: return new Color32(156, 39, 176, 255);
                case SealRarity.Legendary: return new Color32(255, 179, 0, 255);
                default: return new Color32(140, 150, 160, 255);
            }
        }

        public static int StarCount(SealRarity r) => (int)r + 1;
    }
}
