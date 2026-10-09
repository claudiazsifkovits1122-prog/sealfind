using NUnit.Framework;
using UnityEngine;

namespace SealFind.Tests
{
    public class ScoreCalculatorTests
    {
        static LevelDefinition Untimed(float par) => new LevelDefinition { ParTime = par };
        static LevelDefinition Timed(float limit) => new LevelDefinition { TimeLimit = limit, ParTime = limit };

        [Test]
        public void Stars_Untimed()
        {
            var def = Untimed(60f);
            Assert.AreEqual(3, ScoreCalculator.Stars(def, 59f, true));
            Assert.AreEqual(2, ScoreCalculator.Stars(def, 100f, true));
            Assert.AreEqual(1, ScoreCalculator.Stars(def, 200f, true));
            Assert.AreEqual(0, ScoreCalculator.Stars(def, 10f, false));
        }

        [Test]
        public void Stars_Timed_DependOnRemainingTime()
        {
            var def = Timed(100f);
            Assert.AreEqual(3, ScoreCalculator.Stars(def, 50f, true));
            Assert.AreEqual(2, ScoreCalculator.Stars(def, 75f, true));
            Assert.AreEqual(1, ScoreCalculator.Stars(def, 95f, true));
        }

        [Test]
        public void Combo_GrowsInsideWindow_AndResetsAfter()
        {
            Assert.AreEqual(2, ScoreCalculator.NextCombo(1, 1f));
            Assert.AreEqual(1, ScoreCalculator.NextCombo(3, ScoreCalculator.ComboWindow + 0.1f));
            Assert.AreEqual(ScoreCalculator.MaxCombo, ScoreCalculator.NextCombo(ScoreCalculator.MaxCombo, 0.5f));
        }

        [Test]
        public void MissPenalty_GrowsWithSpamming_ButIsCapped()
        {
            Assert.AreEqual(2f, ScoreCalculator.MissPenalty(1));
            Assert.AreEqual(3f, ScoreCalculator.MissPenalty(2));
            Assert.AreEqual(5f, ScoreCalculator.MissPenalty(20));
        }

        [Test]
        public void RareSealsAreWorthMore()
        {
            var common = new SealSpecies { Rarity = SealRarity.Common };
            var legendary = new SealSpecies { Rarity = SealRarity.Legendary };
            Assert.Greater(ScoreCalculator.SealPoints(legendary, true, false, 1), ScoreCalculator.SealPoints(common, true, false, 1));
            Assert.AreEqual(ScoreCalculator.SealPoints(common, true, false, 1) * 3, ScoreCalculator.SealPoints(common, true, false, 3));
        }
    }

    public class SaveDataTests
    {
        [Test]
        public void RecordLevel_KeepsBestValues()
        {
            var s = new SaveData();
            Assert.IsTrue(s.RecordLevel("w0-l0", 2, 50f, 1000));
            Assert.IsFalse(s.RecordLevel("w0-l0", 1, 60f, 500));
            Assert.IsTrue(s.RecordLevel("w0-l0", 3, 40f, 900));
            var rec = s.GetLevel("w0-l0");
            Assert.AreEqual(3, rec.Stars);
            Assert.AreEqual(40f, rec.BestTime);
            Assert.AreEqual(1000, rec.BestScore);
        }

        [Test]
        public void AddSpecies_ReportsOnlyFirstDiscovery()
        {
            var s = new SaveData();
            Assert.IsTrue(s.AddSpecies("bruno"));
            Assert.IsFalse(s.AddSpecies("bruno"));
            Assert.AreEqual(2, s.SpeciesCount("bruno"));
            Assert.AreEqual(1, s.DiscoveredSpecies);
        }

        [Test]
        public void Unlocking_FollowsStarsAndLevelOrder()
        {
            var s = new SaveData();
            Assert.IsTrue(s.IsLevelUnlocked(0, 0));
            Assert.IsFalse(s.IsLevelUnlocked(0, 1));
            s.RecordLevel("w0-l0", 1, 30f, 100);
            Assert.IsTrue(s.IsLevelUnlocked(0, 1));
            Assert.IsFalse(s.IsWorldUnlocked(1));
            for (int l = 0; l < ContentDatabase.LevelsPerWorld; l++) s.RecordLevel("w0-l" + l, 3, 10f, 100);
            Assert.AreEqual(ContentDatabase.LevelsPerWorld * 3, s.TotalStars);
            Assert.IsTrue(s.IsWorldUnlocked(1));
        }

        [Test]
        public void JsonRoundTrip()
        {
            var s = new SaveData { DailyStreak = 4, SoundOn = false };
            s.RecordLevel("w1-l2", 2, 33.5f, 777);
            s.AddSpecies("goldi");
            var copy = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(s));
            Assert.AreEqual(4, copy.DailyStreak);
            Assert.IsFalse(copy.SoundOn);
            Assert.AreEqual(2, copy.StarsFor("w1-l2"));
            Assert.AreEqual(1, copy.SpeciesCount("goldi"));
        }

        [Test]
        public void SeededRandom_IsDeterministic()
        {
            var a = new SeededRandom(42);
            var b = new SeededRandom(42);
            for (int i = 0; i < 100; i++) Assert.AreEqual(a.NextUInt(), b.NextUInt());
            var r = new SeededRandom(7);
            for (int i = 0; i < 1000; i++)
            {
                float v = r.Value;
                Assert.That(v, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }
        }
    }
}
