using System.Collections.Generic;
using UnityEngine;

namespace SealFind
{
    /// <summary>
    /// Poisson-Disc-Sampling (Bridson): verteilt Punkte gleichmäßig, aber natürlich wirkend –
    /// kein Raster, keine Klumpen. Perfekt für Wimmelbild-Deko.
    /// </summary>
    public static class PoissonDisc
    {
        public static List<Vector2> Sample(Rect area, float radius, SeededRandom rng, int maxPoints = 500, int attempts = 20)
        {
            var points = new List<Vector2>();
            if (area.width <= 0f || area.height <= 0f || radius <= 0f) return points;

            float cell = radius / Mathf.Sqrt(2f);
            int cols = Mathf.CeilToInt(area.width / cell);
            int rows = Mathf.CeilToInt(area.height / cell);
            var grid = new int[cols * rows];
            for (int i = 0; i < grid.Length; i++) grid[i] = -1;

            var active = new List<int>();

            void Add(Vector2 p)
            {
                points.Add(p);
                int idx = points.Count - 1;
                active.Add(idx);
                int gx = Mathf.Clamp((int)((p.x - area.xMin) / cell), 0, cols - 1);
                int gy = Mathf.Clamp((int)((p.y - area.yMin) / cell), 0, rows - 1);
                grid[gy * cols + gx] = idx;
            }

            bool Fits(Vector2 p)
            {
                if (!area.Contains(p)) return false;
                int gx = (int)((p.x - area.xMin) / cell);
                int gy = (int)((p.y - area.yMin) / cell);
                for (int y = Mathf.Max(0, gy - 2); y <= Mathf.Min(rows - 1, gy + 2); y++)
                for (int x = Mathf.Max(0, gx - 2); x <= Mathf.Min(cols - 1, gx + 2); x++)
                {
                    int other = grid[y * cols + x];
                    if (other >= 0 && (points[other] - p).sqrMagnitude < radius * radius) return false;
                }
                return true;
            }

            Add(new Vector2(rng.Range(area.xMin, area.xMax), rng.Range(area.yMin, area.yMax)));

            while (active.Count > 0 && points.Count < maxPoints)
            {
                int ai = rng.Range(0, active.Count);
                Vector2 origin = points[active[ai]];
                bool found = false;
                for (int k = 0; k < attempts; k++)
                {
                    float angle = rng.Range(0f, Mathf.PI * 2f);
                    float dist = rng.Range(radius, radius * 2f);
                    var candidate = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
                    if (Fits(candidate))
                    {
                        Add(candidate);
                        found = true;
                        break;
                    }
                }
                if (!found) active.RemoveAt(ai);
            }
            return points;
        }
    }
}
