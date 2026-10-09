using System;
using System.Collections.Generic;
using UnityEngine;

namespace SealFind
{
    public sealed class LevelResult
    {
        public LevelDefinition Level;
        public bool Success;
        public float Time;
        public int Score;
        public int TimeBonus;
        public int Stars;
        public int Misses;
        public int HintsUsed;
        public int TargetFound, TargetTotal, BonusFound;
        public bool NewRecord;
        public int MaxCombo;
        public readonly List<SealSpecies> NewSpecies = new List<SealSpecies>();
    }

    /// <summary>
    /// Die Spielregeln einer laufenden Runde: Zeit, Treffer, Fehlklicks, Combos, Hinweise, Sieg/Niederlage.
    /// Kennt kein UI – alles wird über Events gemeldet, die der <see cref="GameManager"/> verdrahtet.
    /// </summary>
    public sealed class LevelSession : MonoBehaviour
    {
        public const int StartHints = 3;
        public const float WhisperDelay = 15f;

        public LevelDefinition Level { get; private set; }
        public BuiltScene Scene { get; private set; }

        public int TargetTotal { get; private set; }
        public int TargetFound { get; private set; }
        public int BonusFound { get; private set; }
        public float Elapsed { get; private set; }
        public float Penalty { get; private set; }
        public int Misses { get; private set; }
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int HintsLeft { get; private set; }
        public int HintsUsed { get; private set; }
        public bool Running { get; private set; }
        public bool Finished { get; private set; }

        public float EffectiveTime => Elapsed + Penalty;
        public float TimeLeft => Level.IsTimed ? Mathf.Max(0f, Level.TimeLimit - EffectiveTime) : 0f;

        public event Action<Seal, int, int> SealFound;      // Robbe, Punkte, Combo
        public event Action<Vector2, bool, float> Missed;   // Position, war Köder?, Strafsekunden
        public event Action<Seal> Tricked;
        public event Action<Seal> Whispered;
        public event Action<LevelResult> Ended;

        float _lastFindTime = -99f;
        float _lastMissTime = -99f;
        int _missStreak;
        float _idle;

        public void Setup(LevelDefinition level, BuiltScene scene)
        {
            Level = level;
            Scene = scene;
            TargetTotal = 0;
            foreach (var s in scene.Seals) if (s.IsTarget) TargetTotal++;
            HintsLeft = StartHints;
        }

        public void Begin() => Running = true;

        public void HandleTap(Vector2 worldPos, float tolerance)
        {
            if (!Running) return;

            Seal best = null;
            float bestDist = float.MaxValue;
            foreach (var s in Scene.Seals)
            {
                if (!s.HitTest(worldPos, tolerance)) continue;
                float d = Vector2.Distance(worldPos, s.CenterWorld);
                // Ziel-Robben bevorzugen, wenn mehrere getroffen werden
                if (s.IsTarget) d -= 0.5f;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = s;
                }
            }

            if (best != null)
            {
                _idle = 0f;
                if (best.TryTrick())
                {
                    Tricked?.Invoke(best);
                    return;
                }
                Find(best);
                return;
            }

            Miss(worldPos, Scene.HitsDecoy(worldPos, tolerance * 0.5f));
        }

        void Find(Seal seal)
        {
            seal.MarkFound();
            Combo = ScoreCalculator.NextCombo(Combo, Elapsed - _lastFindTime);
            MaxCombo = Mathf.Max(MaxCombo, Combo);
            _lastFindTime = Elapsed;
            _missStreak = 0;

            int points = ScoreCalculator.SealPoints(seal.Species, seal.IsTarget, seal.IsBonus, Combo);
            Score += points;
            if (seal.IsTarget) TargetFound++;
            else if (seal.IsBonus) BonusFound++;

            SealFound?.Invoke(seal, points, Combo);
            if (TargetFound >= TargetTotal) End(true);
        }

        void Miss(Vector2 pos, bool decoy)
        {
            Misses++;
            _missStreak = Elapsed - _lastMissTime < 2.5f ? _missStreak + 1 : 1;
            _lastMissTime = Elapsed;
            Combo = 0;
            float penalty = ScoreCalculator.MissPenalty(_missStreak);
            Penalty += penalty;
            Score = Mathf.Max(0, Score - ScoreCalculator.MissScorePenalty);
            Missed?.Invoke(pos, decoy, penalty);
            if (Level.IsTimed && TimeLeft <= 0f) End(false);
        }

        /// <summary>Aktiver Hinweis (Lupe). Kostet Strafsekunden.</summary>
        public Seal UseHint(Vector2 near)
        {
            if (!Running || HintsLeft <= 0) return null;
            var seal = PickUnfound(near);
            if (seal == null) return null;
            HintsLeft--;
            HintsUsed++;
            Penalty += ScoreCalculator.HintPenaltySeconds;
            _idle = 0f;
            seal.Wiggle();
            return seal;
        }

        Seal PickUnfound(Vector2 near)
        {
            Seal best = null;
            float bestD = float.MaxValue;
            foreach (var s in Scene.Seals)
            {
                if (s.IsFound || !s.IsTarget) continue;
                float d = Vector2.Distance(near, s.CenterWorld);
                if (d < bestD)
                {
                    bestD = d;
                    best = s;
                }
            }
            return best;
        }

        void Update()
        {
            if (!Running) return;
            float dt = Time.deltaTime;
            Elapsed += dt;

            // "Die Robbe merkt, dass du sie suchst": nach einer Weile ohne Fund macht eine Robbe "psst…"
            _idle += dt;
            if (_idle >= WhisperDelay)
            {
                _idle = 0f;
                var candidates = Scene.Seals.FindAll(s => !s.IsFound && s.IsTarget);
                if (candidates.Count > 0)
                {
                    var s = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    s.Wiggle();
                    Whispered?.Invoke(s);
                }
            }

            if (Level.IsTimed && TimeLeft <= 0f) End(false);
        }

        void End(bool success)
        {
            if (Finished) return;
            Running = false;
            Finished = true;

            if (!success)
                foreach (var s in Scene.Seals) if (s.IsTarget) s.Reveal();

            int bonus = success ? ScoreCalculator.TimeBonus(Level, EffectiveTime) : 0;
            var result = new LevelResult
            {
                Level = Level,
                Success = success,
                Time = EffectiveTime,
                TimeBonus = bonus,
                Score = Score + bonus,
                Stars = ScoreCalculator.Stars(Level, EffectiveTime, success),
                Misses = Misses,
                HintsUsed = HintsUsed,
                TargetFound = TargetFound,
                TargetTotal = TargetTotal,
                BonusFound = BonusFound,
                MaxCombo = MaxCombo,
            };
            Ended?.Invoke(result);
        }

        public void Abort()
        {
            Running = false;
            Finished = true;
        }
    }
}
