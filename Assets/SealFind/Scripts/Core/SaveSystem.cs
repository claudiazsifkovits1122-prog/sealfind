using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SealFind
{
    [Serializable]
    public sealed class LevelRecord
    {
        public string Key;
        public int Stars;
        public float BestTime;
        public int BestScore;
    }

    [Serializable]
    public sealed class SpeciesRecord
    {
        public string Id;
        public int Count;
    }

    /// <summary>Spielstand. JsonUtility kann keine Dictionaries – daher Listen + Lookup-Methoden.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public int Version = 1;
        public List<LevelRecord> Levels = new List<LevelRecord>();
        public List<SpeciesRecord> Species = new List<SpeciesRecord>();
        public int TotalSealsFound;
        public int TotalPlaySeconds;

        public bool SoundOn = true;
        public bool AmbienceOn = true;
        public bool VibrationOn = true;
        public bool TutorialSeen;

        public int LastDailyDate;
        public float DailyBestTime;
        public int DailyStreak;
        public int QuickRoundsPlayed;

        public LevelRecord GetLevel(string key) => Levels.Find(l => l.Key == key);

        public int StarsFor(string key) => GetLevel(key)?.Stars ?? 0;

        public int TotalStars
        {
            get
            {
                int n = 0;
                foreach (var l in Levels) if (l.Key.StartsWith("w")) n += l.Stars;
                return n;
            }
        }

        public int WorldStars(int world)
        {
            int n = 0;
            for (int l = 0; l < ContentDatabase.LevelsPerWorld; l++) n += StarsFor("w" + world + "-l" + l);
            return n;
        }

        public bool IsWorldUnlocked(int world) =>
            world == 0 || TotalStars >= ContentDatabase.Worlds[world].StarsToUnlock;

        /// <summary>Level ist spielbar, wenn die Welt offen ist und das vorherige Level geschafft wurde.</summary>
        public bool IsLevelUnlocked(int world, int level) =>
            IsWorldUnlocked(world) && (level == 0 || StarsFor("w" + world + "-l" + (level - 1)) > 0);

        public int SpeciesCount(string id) => Species.Find(s => s.Id == id)?.Count ?? 0;

        public int DiscoveredSpecies
        {
            get
            {
                int n = 0;
                foreach (var s in Species) if (s.Count > 0) n++;
                return n;
            }
        }

        /// <summary>Zählt einen Fund. Gibt true zurück, wenn die Art neu entdeckt wurde.</summary>
        public bool AddSpecies(string id)
        {
            var rec = Species.Find(s => s.Id == id);
            if (rec == null)
            {
                Species.Add(new SpeciesRecord { Id = id, Count = 1 });
                return true;
            }
            rec.Count++;
            return rec.Count == 1;
        }

        /// <summary>Speichert ein Level-Ergebnis. Gibt true zurück bei neuer Bestzeit.</summary>
        public bool RecordLevel(string key, int stars, float time, int score)
        {
            var rec = GetLevel(key);
            if (rec == null)
            {
                Levels.Add(new LevelRecord { Key = key, Stars = stars, BestTime = time, BestScore = score });
                return true;
            }
            bool record = rec.BestTime <= 0f || time < rec.BestTime;
            rec.Stars = Mathf.Max(rec.Stars, stars);
            rec.BestScore = Mathf.Max(rec.BestScore, score);
            if (record) rec.BestTime = time;
            return record;
        }
    }

    /// <summary>Speichert als JSON-Datei in persistentDataPath (atomar über temporäre Datei).</summary>
    public static class SaveSystem
    {
        const string FileName = "sealfind_save.json";

        static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                    if (data != null) return data;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SealFind] Spielstand konnte nicht geladen werden: " + e.Message);
            }
            return new SaveData();
        }

        public static void Save(SaveData data)
        {
            try
            {
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(data));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SealFind] Spielstand konnte nicht gespeichert werden: " + e.Message);
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SealFind] Spielstand konnte nicht gelöscht werden: " + e.Message);
            }
        }
    }
}
