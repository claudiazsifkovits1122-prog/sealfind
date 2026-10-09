using System.Collections.Generic;
using UnityEngine;

namespace SealFind
{
    /// <summary>Ein platziertes Deko-Objekt. Position = Fußpunkt (unten Mitte).</summary>
    public struct PropPlacement
    {
        public PropKind Kind;
        public int Variant;
        public Vector2 Position;
        public float Scale;
        public bool Flip;

        public Vector2 Size => PropCatalog.Get(Kind).Size * Scale;

        public Rect Bounds
        {
            get
            {
                var s = Size;
                return new Rect(Position.x - s.x * 0.5f, Position.y, s.x, s.y);
            }
        }
    }

    /// <summary>Wo und wie eine Robbe versteckt ist.</summary>
    public sealed class SealPlacement
    {
        public int SpeciesIndex;
        public bool IsTarget;
        public bool IsBonus;

        public HidingStyle Style = HidingStyle.Open;
        public bool Tiny;
        public bool Sleeping;
        public bool Trickster;

        public Vector2 Position;
        public float Scale = 1f;
        public bool Flip;
        public int HiderProp = -1;
        public Vector2 AltPosition;
        public float MoveRange;
        public float Camouflage;
        /// <summary>Wasser verdeckt den unteren Teil (Kopf schaut raus).</summary>
        public bool HasWaterCover;

        /// <summary>Grobe Umrandung (Welteinheiten), für Überlappungs-Checks.</summary>
        public Rect Bounds => SealBounds(Position, Scale);

        public static Rect SealBounds(Vector2 pos, float scale) =>
            new Rect(pos.x - 0.8f * scale, pos.y, 1.6f * scale, 0.95f * scale);
    }

    /// <summary>
    /// Ergebnis des <see cref="SceneGenerator"/>: reine Daten, noch keine GameObjects.
    /// Dadurch lässt sich die Generierung ohne Szene testen.
    /// </summary>
    public sealed class ScenePlan
    {
        public int Seed;
        public int World;
        public Rect Bounds;
        public readonly List<PropPlacement> Props = new List<PropPlacement>();
        public readonly List<SealPlacement> Seals = new List<SealPlacement>();
        public readonly List<PropPlacement> Decoys = new List<PropPlacement>();

        public int TargetCount
        {
            get
            {
                int n = 0;
                foreach (var s in Seals) if (s.IsTarget) n++;
                return n;
            }
        }
    }
}
