using UnityEngine;

namespace SealFind
{
    public enum SealFace { Open, Blink, Happy }

    /// <summary>
    /// Geometrie einer Robbe (Seitenansicht, Kopf rechts) in Pixeln auf einer 256×192-Zeichenfläche.
    /// Wird fürs Zeichnen UND für die Trefferprüfung verwendet – so passen Grafik und Klickfläche immer zusammen.
    /// </summary>
    public sealed class SealRig
    {
        public const int Width = 256;
        public const int Height = 192;
        public const float PixelsPerUnit = 160f;
        public static readonly Vector2 Pivot = new Vector2(0.5f, 0f);

        public Vector2 Body, BodyRadii;
        public float BodyRotation;
        public Vector2 Head;
        public float HeadRadius;
        public Vector2 Snout, SnoutRadii;
        public Vector2 EyeBack, EyeFront;
        public float EyeRadius;
        public Vector2 Nose;
        public Vector2 Flipper, FlipperRadii;
        public Vector2 Tail1, Tail2, TailRadii;

        public static readonly SealRig Adult = new SealRig
        {
            Body = new Vector2(110, 62), BodyRadii = new Vector2(88, 44), BodyRotation = -6f,
            Head = new Vector2(180, 94), HeadRadius = 40f,
            Snout = new Vector2(208, 84), SnoutRadii = new Vector2(20, 15),
            EyeBack = new Vector2(177, 106), EyeFront = new Vector2(199, 104), EyeRadius = 7f,
            Nose = new Vector2(225, 92),
            Flipper = new Vector2(140, 30), FlipperRadii = new Vector2(28, 11),
            Tail1 = new Vector2(28, 50), Tail2 = new Vector2(26, 34), TailRadii = new Vector2(22, 9),
        };

        public static readonly SealRig Baby = new SealRig
        {
            Body = new Vector2(116, 64), BodyRadii = new Vector2(72, 48), BodyRotation = -4f,
            Head = new Vector2(178, 100), HeadRadius = 46f,
            Snout = new Vector2(208, 88), SnoutRadii = new Vector2(18, 14),
            EyeBack = new Vector2(171, 113), EyeFront = new Vector2(198, 111), EyeRadius = 10f,
            Nose = new Vector2(223, 96),
            Flipper = new Vector2(140, 30), FlipperRadii = new Vector2(22, 10),
            Tail1 = new Vector2(42, 52), Tail2 = new Vector2(40, 38), TailRadii = new Vector2(18, 8),
        };

        public static SealRig For(bool baby) => baby ? Baby : Adult;

        /// <summary>Pixel → lokale Einheiten (Pivot unten Mitte).</summary>
        public static Vector2 ToLocal(Vector2 px) => new Vector2((px.x - Width * 0.5f) / PixelsPerUnit, px.y / PixelsPerUnit);

        public static float ToLocal(float px) => px / PixelsPerUnit;

        public Vector2 HeadLocal => ToLocal(Head);
        public float HeadRadiusLocal => ToLocal(HeadRadius);
        public Vector2 BodyLocal => ToLocal(Body);
        public Vector2 BodyRadiiLocal => BodyRadii / PixelsPerUnit;
    }
}
