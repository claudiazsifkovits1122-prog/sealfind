using System.Collections.Generic;

namespace SealFind
{
    /// <summary>
    /// Kleiner deterministischer Zufallsgenerator (xorshift32).
    /// Gleicher Seed = gleiches Wimmelbild – auf jedem Gerät. Wichtig für die Tages-Challenge.
    /// </summary>
    public sealed class SeededRandom
    {
        uint _state;

        public SeededRandom(int seed)
        {
            _state = (uint)seed * 2654435761u ^ 0x9E3779B9u;
            if (_state == 0) _state = 1;
            for (int i = 0; i < 4; i++) NextUInt();
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>Zufallszahl in [0, 1).</summary>
        public float Value => (NextUInt() >> 8) * (1f / 16777216f);

        public float Range(float min, float max) => min + (max - min) * Value;

        /// <summary>Ganzzahl in [min, maxExclusive).</summary>
        public int Range(int min, int maxExclusive)
        {
            if (maxExclusive <= min) return min;
            return min + (int)(NextUInt() % (uint)(maxExclusive - min));
        }

        public bool Chance(float probability) => Value < probability;

        public float Sign() => Value < 0.5f ? -1f : 1f;

        public T Pick<T>(IList<T> list) => list[Range(0, list.Count)];

        public int WeightedIndex(IList<float> weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++) total += weights[i] > 0 ? weights[i] : 0;
            if (total <= 0f) return -1;
            float r = Value * total;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0) continue;
                r -= weights[i];
                if (r < 0f) return i;
            }
            return weights.Count - 1;
        }

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
