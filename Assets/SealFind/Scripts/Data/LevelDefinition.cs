using System;

namespace SealFind
{
    public enum GoalType { FindSeals, FindBabies, FindAccessory, TimeAttack }

    public enum GameMode { Campaign, Daily, Quick, Backdrop }

    /// <summary>Wie eine Robbe versteckt wird. Basis-Stile plus Modifikatoren.</summary>
    [Flags]
    public enum HidingStyle
    {
        None = 0,
        Open = 1 << 0,        // liegt frei herum (aber zwischen viel Deko)
        BehindProp = 1 << 1,  // halb hinter Stein, Schirm, Kiste …
        InWater = 1 << 2,     // nur Kopf und Rücken schauen aus dem Wasser
        Peek = 1 << 3,        // taucht immer wieder ab und auf
        Moving = 1 << 4,      // schwimmt hin und her
        Tiny = 1 << 5,        // Modifikator: sehr klein
        Sleeping = 1 << 6,    // Modifikator: schläft (Augen zu, Zzz)
        Trickster = 1 << 7,   // Modifikator: hüpft beim ersten Antippen weg

        Basic = Open | BehindProp,
        Placement = Open | BehindProp | InWater | Peek | Moving,
        All = Placement | Tiny | Sleeping | Trickster,
    }

    /// <summary>Alle Parameter eines Levels. Wird von <see cref="ContentDatabase"/> erzeugt.</summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        public GameMode Mode;
        public int World;
        public int Level;
        public int Seed;

        public GoalType Goal;
        public Accessory GoalAccessory;
        public int TargetCount;
        /// <summary>Zusätzliche Robben, die nicht zum Ziel zählen (z.B. Erwachsene bei "Finde Babys").</summary>
        public int ExtraSeals;
        /// <summary>Robbenförmige Steine – "Warte… ist DAS eine Robbe?!"</summary>
        public int Decoys;
        public HidingStyle Styles = HidingStyle.Basic;

        public float PropDensity = 0.6f;
        public float SealScale = 1f;
        public float Camouflage;
        /// <summary>0 = kein Zeitlimit.</summary>
        public float TimeLimit;
        /// <summary>Zielzeit für 3 Sterne.</summary>
        public float ParTime = 60f;
        /// <summary>Chance auf eine legendäre Bonus-Robbe.</summary>
        public float BonusChance = 0.1f;

        public bool IsTimed => TimeLimit > 0f;

        public string Key
        {
            get
            {
                switch (Mode)
                {
                    case GameMode.Campaign: return "w" + World + "-l" + Level;
                    case GameMode.Daily: return "daily-" + Seed;
                    default: return Mode.ToString().ToLowerInvariant();
                }
            }
        }
    }
}
