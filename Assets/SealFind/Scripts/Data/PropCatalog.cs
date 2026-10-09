using UnityEngine;

namespace SealFind
{
    public enum PropMotion { None, Bob, Drift, Fly, Swim, Sway, Rise }

    /// <summary>Feste Eigenschaften eines Deko-Typs (Größe in Welteinheiten, Versteck-Eignung …).</summary>
    public readonly struct PropInfo
    {
        public readonly Vector2 Size;
        public readonly bool CanHide;
        public readonly int Variants;
        public readonly PropMotion Motion;
        public readonly bool RockTinted;
        public readonly bool Glows;

        public PropInfo(float w, float h, bool canHide = false, int variants = 1,
            PropMotion motion = PropMotion.None, bool rockTinted = false, bool glows = false)
        {
            Size = new Vector2(w, h);
            CanHide = canHide;
            Variants = variants;
            Motion = motion;
            RockTinted = rockTinted;
            Glows = glows;
        }
    }

    public static class PropCatalog
    {
        public static PropInfo Get(PropKind kind)
        {
            switch (kind)
            {
                case PropKind.Rock: return new PropInfo(1.7f, 1.15f, canHide: true, variants: 3, rockTinted: true);
                case PropKind.RockSmall: return new PropInfo(0.8f, 0.5f, variants: 3, rockTinted: true);
                case PropKind.SealRock: return new PropInfo(1.6f, 1.2f, variants: 2, rockTinted: true);
                case PropKind.Umbrella: return new PropInfo(1.7f, 2.1f, variants: 3);
                case PropKind.Palm: return new PropInfo(2.2f, 3.3f, motion: PropMotion.Sway);
                case PropKind.Shell: return new PropInfo(0.42f, 0.36f, variants: 2);
                case PropKind.Starfish: return new PropInfo(0.48f, 0.48f, variants: 2);
                case PropKind.Gull: return new PropInfo(0.7f, 0.5f);
                case PropKind.GullFlying: return new PropInfo(0.9f, 0.45f, motion: PropMotion.Fly);
                case PropKind.Boat: return new PropInfo(2.6f, 2.4f, canHide: true, variants: 2, motion: PropMotion.Bob);
                case PropKind.Buoy: return new PropInfo(0.6f, 1.0f, motion: PropMotion.Bob);
                case PropKind.Crate: return new PropInfo(1.05f, 0.95f, canHide: true);
                case PropKind.Barrel: return new PropInfo(0.85f, 1.05f, canHide: true);
                case PropKind.IceBlock: return new PropInfo(1.5f, 1.05f, canHide: true, variants: 2);
                case PropKind.Iceberg: return new PropInfo(2.8f, 1.9f, canHide: true, motion: PropMotion.Bob);
                case PropKind.Penguin: return new PropInfo(0.6f, 0.9f);
                case PropKind.Fish: return new PropInfo(0.65f, 0.38f, variants: 3, motion: PropMotion.Swim);
                case PropKind.Seaweed: return new PropInfo(0.7f, 1.7f, motion: PropMotion.Sway);
                case PropKind.Coral: return new PropInfo(1.3f, 1.1f, canHide: true, variants: 2);
                case PropKind.Cloud: return new PropInfo(3.2f, 1.3f, motion: PropMotion.Drift);
                case PropKind.Sun: return new PropInfo(1.8f, 1.8f, variants: 2, glows: true);
                case PropKind.Ball: return new PropInfo(0.55f, 0.55f);
                case PropKind.Towel: return new PropInfo(1.7f, 0.55f, variants: 2);
                case PropKind.Sandcastle: return new PropInfo(1.3f, 1.1f, canHide: true);
                case PropKind.Crab: return new PropInfo(0.55f, 0.38f);
                case PropKind.Lantern: return new PropInfo(0.45f, 1.7f, glows: true);
                case PropKind.Chest: return new PropInfo(1.05f, 0.8f, canHide: true);
                case PropKind.Flag: return new PropInfo(0.9f, 1.9f, motion: PropMotion.Sway);
                case PropKind.Bollard: return new PropInfo(0.5f, 0.6f);
                case PropKind.Snowman: return new PropInfo(0.95f, 1.4f, canHide: true);
                case PropKind.Bubble: return new PropInfo(0.3f, 0.3f, motion: PropMotion.Rise);
                case PropKind.Anchor: return new PropInfo(0.9f, 1.0f);
                default: return new PropInfo(1f, 1f);
            }
        }
    }
}
