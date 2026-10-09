using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SealFind.Tests
{
    public class SceneGeneratorTests
    {
        static IEnumerable<TestCaseData> AllCampaignLevels()
        {
            for (int w = 0; w < ContentDatabase.Worlds.Count; w++)
            for (int l = 0; l < ContentDatabase.LevelsPerWorld; l++)
                yield return new TestCaseData(w, l).SetName($"Welt{w}_Level{l}");
        }

        static ScenePlan Generate(LevelDefinition def) =>
            SceneGenerator.Generate(ContentDatabase.Worlds[def.World], def, ContentDatabase.Species);

        [TestCaseSource(nameof(AllCampaignLevels))]
        public void Level_HasAllTargetSeals(int world, int level)
        {
            var def = ContentDatabase.BuildLevel(world, level);
            Assert.AreEqual(def.TargetCount, Generate(def).TargetCount);
        }

        [TestCaseSource(nameof(AllCampaignLevels))]
        public void Level_IsDeterministic(int world, int level)
        {
            var def = ContentDatabase.BuildLevel(world, level);
            var a = Generate(def);
            var b = Generate(def);
            Assert.AreEqual(a.Props.Count, b.Props.Count);
            Assert.AreEqual(a.Seals.Count, b.Seals.Count);
            for (int i = 0; i < a.Seals.Count; i++)
            {
                Assert.AreEqual(a.Seals[i].Position, b.Seals[i].Position);
                Assert.AreEqual(a.Seals[i].SpeciesIndex, b.Seals[i].SpeciesIndex);
            }
        }

        [TestCaseSource(nameof(AllCampaignLevels))]
        public void Level_SealsDoNotOverlapAndStayInside(int world, int level)
        {
            var plan = Generate(ContentDatabase.BuildLevel(world, level));
            var still = plan.Seals.Where(s => s.MoveRange <= 0f).ToList();
            for (int i = 0; i < still.Count; i++)
            {
                var r = still[i].Bounds;
                Assert.Greater(r.xMin, plan.Bounds.xMin, "links raus");
                Assert.Less(r.xMax, plan.Bounds.xMax, "rechts raus");
                Assert.LessOrEqual(still[i].Position.y, SceneGenerator.MaxSealY, "unter dem HUD");
                for (int j = i + 1; j < still.Count; j++)
                    Assert.IsFalse(r.Overlaps(still[j].Bounds), $"Robbe {i} überlappt Robbe {j}");
            }
        }

        [TestCaseSource(nameof(AllCampaignLevels))]
        public void Level_HiddenSealsKeepTheirHidingProp(int world, int level)
        {
            var plan = Generate(ContentDatabase.BuildLevel(world, level));
            foreach (var s in plan.Seals.Where(s => s.Style == HidingStyle.BehindProp))
            {
                Assert.That(s.HiderProp, Is.InRange(0, plan.Props.Count - 1));
                Assert.IsTrue(PropCatalog.Get(plan.Props[s.HiderProp].Kind).CanHide);
            }
        }

        [TestCaseSource(nameof(AllCampaignLevels))]
        public void Level_TargetsMatchGoal(int world, int level)
        {
            var def = ContentDatabase.BuildLevel(world, level);
            var plan = Generate(def);
            var species = ContentDatabase.Species;
            foreach (var s in plan.Seals)
            {
                var sp = species[s.SpeciesIndex];
                bool matches = def.Goal == GoalType.FindBabies ? sp.IsBaby
                    : def.Goal == GoalType.FindAccessory ? sp.Accessory == def.GoalAccessory
                    : true;
                if (s.IsTarget) Assert.IsTrue(matches, $"{sp.Name} passt nicht zum Ziel {def.Goal}");
                else if (!s.IsBonus && def.Goal != GoalType.FindSeals) Assert.IsFalse(matches, $"{sp.Name} wäre ein Ziel");
            }
        }

        [Test]
        public void Daily_SameDateSamePicture_DifferentDateDifferentPicture()
        {
            var a = ContentDatabase.BuildDaily(new DateTime(2026, 10, 9));
            var b = ContentDatabase.BuildDaily(new DateTime(2026, 10, 9));
            var c = ContentDatabase.BuildDaily(new DateTime(2026, 10, 10));
            Assert.AreEqual(a.Seed, b.Seed);
            Assert.AreNotEqual(a.Seed, c.Seed);
            Assert.AreEqual(Generate(a).Seals[0].Position, Generate(b).Seals[0].Position);
        }

        [Test]
        public void RandomRounds_AlwaysContainAllTargets([Range(1, 200, 13)] int seed)
        {
            var def = ContentDatabase.BuildQuick(seed * 7919, ContentDatabase.Worlds.Count - 1);
            Assert.AreEqual(def.TargetCount, Generate(def).TargetCount);
        }
    }

    public class ContentTests
    {
        [Test]
        public void SpeciesIdsAreUnique()
        {
            var ids = ContentDatabase.Species.Select(s => s.Id).ToList();
            CollectionAssert.AllItemsAreUnique(ids);
        }

        [Test]
        public void EveryWorldThemeAccessoryExistsOnSomeSpecies()
        {
            foreach (var w in ContentDatabase.Worlds)
                Assert.IsTrue(ContentDatabase.Species.Any(s => s.Accessory == w.ThemeAccessory), w.Name);
        }

        [Test]
        public void WorldUnlockCostsIncreaseAndAreReachable()
        {
            var worlds = ContentDatabase.Worlds;
            Assert.AreEqual(0, worlds[0].StarsToUnlock);
            for (int i = 1; i < worlds.Count; i++)
            {
                Assert.Greater(worlds[i].StarsToUnlock, worlds[i - 1].StarsToUnlock);
                // mit allen Sternen der vorherigen Welten erreichbar
                Assert.LessOrEqual(worlds[i].StarsToUnlock, i * ContentDatabase.LevelsPerWorld * 3);
            }
        }

        [Test]
        public void EveryWorldHasBabiesAvailable()
        {
            Assert.IsTrue(ContentDatabase.Species.Any(s => s.IsBaby && s.Worlds.Length == 0));
        }
    }
}
