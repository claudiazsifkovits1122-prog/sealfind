using System;
using UnityEngine;
using UnityEngine.UI;

namespace SealFind
{
    /// <summary>Hauptmenü – liegt über einer lebendigen Strandszene.</summary>
    public sealed class MainMenuScreen : UIScreen
    {
        Text _playSub, _collection, _stars, _dailySub;
        Image _dailyBadge;
        SealPortrait _mascot;

        protected override void Build()
        {
            var safe = Kit.Rect("Safe", Root);
            UIKit.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            // Leichter Verlauf links, damit Titel und Buttons lesbar sind
            var shade = Kit.Image(Root, null, new Color(0.03f, 0.1f, 0.2f, 0.35f), "Shade");
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = new Vector2(0.55f, 1f);
            shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
            shade.transform.SetAsFirstSibling();

            var title = Kit.Text(safe, "ROBBEN-SUCHE", 132, Color.white);
            UIKit.Place(title, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(90f, -70f), new Vector2(1100f, 160f));
            title.alignment = TextAnchor.MiddleLeft;
            UIKit.Outline(title, Theme.Deep, 6f);
            UIKit.Shadow(title, 10f, 0.3f);

            var sub = Kit.Text(safe, "Wo haben sie sich bloß versteckt?", 46, Color.white, TextAnchor.MiddleLeft, FontStyle.Italic);
            UIKit.Place(sub, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, -225f), new Vector2(1100f, 60f));
            UIKit.Outline(sub, new Color(0f, 0f, 0f, 0.35f), 2f);

            var column = Kit.Rect("Buttons", safe);
            UIKit.Place(column, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(90f, -90f), new Vector2(640f, 640f));

            var play = Kit.Button(column, "SPIELEN", Theme.Primary, () => Game.PlayContinue(), 70);
            UIKit.Place(play, UIKit.Top, UIKit.Top, Vector2.zero, new Vector2(640f, 190f));
            var playLabel = play.transform.Find("Label") as RectTransform;
            UIKit.Stretch(playLabel, 16, 50, 16, 10);
            _playSub = Kit.Text(play.transform, "", 34, new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UIKit.Place(_playSub, UIKit.Bottom, UIKit.Bottom, new Vector2(0f, 18f), new Vector2(600f, 50f));
            playLabel.gameObject.AddComponent<Pulse>();

            var worlds = Kit.Button(column, "Welten", Theme.Secondary, () => Game.UI.Worlds.Show(), 48);
            UIKit.Place(worlds, UIKit.Top, UIKit.Top, new Vector2(0f, -220f), new Vector2(640f, 120f));

            var daily = Kit.Button(column, "Tages-Challenge", Theme.Deep, () => Game.PlayDaily(), 48);
            UIKit.Place(daily, UIKit.Top, UIKit.Top, new Vector2(0f, -365f), new Vector2(640f, 120f));
            UIKit.Stretch(daily.transform.Find("Label") as RectTransform, 16, 34, 16, 6);
            _dailySub = Kit.Text(daily.transform, "", 28, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UIKit.Place(_dailySub, UIKit.Bottom, UIKit.Bottom, new Vector2(0f, 10f), new Vector2(600f, 40f));
            _dailyBadge = Kit.Badge(daily.transform, "!", Theme.Danger, 60f);
            UIKit.Place(_dailyBadge, UIKit.TopRight, Center, new Vector2(-14f, -10f), new Vector2(60f, 60f));

            var coll = Kit.Button(column, "Sammlung", new Color32(140, 98, 214, 255), () => Game.UI.Collection.Show(), 48);
            UIKit.Place(coll, UIKit.Top, UIKit.Top, new Vector2(0f, -510f), new Vector2(640f, 120f));
            _collection = coll.transform.Find("Label").GetComponent<Text>();

            var settings = Kit.IconButton(safe, Kit.Art.GearIcon, Theme.Deep, () => Game.UI.Settings.Show(), 110f);
            UIKit.Place(settings, UIKit.TopRight, UIKit.TopRight, new Vector2(-40f, -40f), new Vector2(110f, 110f));

            var starPill = Kit.Panel(safe, new Color(0f, 0f, 0f, 0.35f), "StarPill");
            UIKit.Place(starPill, UIKit.TopRight, UIKit.TopRight, new Vector2(-170f, -50f), new Vector2(260f, 90f));
            starPill.raycastTarget = false;
            var starIcon = Kit.Icon(starPill.transform, Kit.Art.Star, Theme.Accent, 60f);
            UIKit.Place(starIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(60f, 60f));
            _stars = Kit.Text(starPill.transform, "0", 40, Color.white, TextAnchor.MiddleLeft);
            UIKit.Place(_stars, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(92f, 0f), new Vector2(160f, 80f));

            _mascot = new SealPortrait(Kit, safe, 520f);
            UIKit.Place(_mascot.Root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-120f, 70f), _mascot.Root.sizeDelta);
            _mascot.Root.gameObject.AddComponent<Bobbing>();

            var hint = Kit.Text(safe, "Tipp: Ziehen = Bild verschieben  ·  Zwei Finger / Mausrad = Zoomen", 28,
                new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleLeft, FontStyle.Normal);
            UIKit.Place(hint, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(96f, 30f), new Vector2(1200f, 40f));
        }

        static readonly Vector2 Center = UIKit.Center;

        protected override void OnShow()
        {
            var save = Game.Save;
            Game.NextCampaignLevel(out int w, out int l, out bool campaignDone);
            _playSub.text = campaignDone
                ? "Zufallsrunde – noch eine Runde!"
                : $"{ContentDatabase.Worlds[w].Name} · Level {l + 1}";
            _collection.text = $"Sammlung  {save.DiscoveredSpecies}/{ContentDatabase.Species.Count}";
            _stars.text = $"{save.TotalStars}/{ContentDatabase.MaxStars}";
            bool playedToday = Game.DailyPlayedToday;
            _dailyBadge.gameObject.SetActive(!playedToday);
            _dailySub.text = save.DailyStreak > 1 ? $"{save.DailyStreak} Tage in Folge!" : playedToday ? "Heute schon gespielt" : "Neues Bild für heute";

            // Maskottchen: zufällige bereits entdeckte Robbe (oder Bruno)
            var species = ContentDatabase.Species[0];
            var found = ContentDatabase.Species;
            int start = UnityEngine.Random.Range(0, found.Count);
            for (int i = 0; i < found.Count; i++)
            {
                var s = found[(start + i) % found.Count];
                if (save.SpeciesCount(s.Id) > 0) { species = s; break; }
            }
            _mascot.Set(species, true, SealFace.Happy);
            PopIn(_mascot.Root, 0.1f);
        }
    }

    /// <summary>Weltauswahl als horizontale Karten-Leiste.</summary>
    public sealed class WorldSelectScreen : UIScreen
    {
        RectTransform _content;
        Text _stars;

        protected override void Build()
        {
            Backdrop(new Color32(110, 198, 255, 255), new Color32(46, 155, 214, 255));
            var safe = Kit.Rect("Safe", Root);
            UIKit.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();
            Header("Welten", () => Game.UI.ShowMain());

            _stars = Kit.Text(Root, "", 44, Color.white, TextAnchor.MiddleRight);
            UIKit.Place(_stars, UIKit.TopRight, UIKit.TopRight, new Vector2(-60f, -36f), new Vector2(400f, 110f));

            var scrollContent = Kit.Scroll(safe, true, out var scroll);
            UIKit.Stretch((RectTransform)scroll.transform, 0, 60, 0, 180);
            var layout = scrollContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 40f;
            layout.padding = new RectOffset(80, 80, 10, 10);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            _content = scrollContent;
        }

        protected override void OnShow()
        {
            Kit.Clear(_content);
            var save = Game.Save;
            _stars.text = $"{save.TotalStars} / {ContentDatabase.MaxStars} Sterne";
            int i = 0;
            foreach (var w in ContentDatabase.Worlds) BuildCard(w, save, i++);
        }

        void BuildCard(WorldDefinition w, SaveData save, int index)
        {
            bool unlocked = save.IsWorldUnlocked(w.Index);
            var card = Kit.Panel(_content, Color.white, "World_" + w.Id);
            card.rectTransform.sizeDelta = new Vector2(460f, 700f);
            UIKit.Shadow(card, 10f, 0.25f);

            // Mini-Landschaft der Welt
            var sky = Kit.Panel(card.transform, w.Underwater ? w.WaterTop : w.SkyTop, "Sky");
            sky.raycastTarget = false;
            UIKit.Stretch(sky.rectTransform, 14, 330, 14, 14);
            var water = Kit.Image(sky.transform, null, w.Underwater ? w.WaterBottom : w.WaterTop, "Water");
            water.rectTransform.anchorMin = new Vector2(0f, 0f);
            water.rectTransform.anchorMax = new Vector2(1f, w.Underwater ? 0.6f : 0.45f);
            water.rectTransform.offsetMin = new Vector2(10f, 60f);
            water.rectTransform.offsetMax = new Vector2(-10f, 0f);
            var ground = Kit.Image(sky.transform, null, w.GroundTop, "Ground");
            ground.rectTransform.anchorMin = Vector2.zero;
            ground.rectTransform.anchorMax = new Vector2(1f, 0f);
            ground.rectTransform.offsetMin = new Vector2(10f, 10f);
            ground.rectTransform.offsetMax = new Vector2(-10f, 70f);

            var portrait = new SealPortrait(Kit, sky.transform, 230f);
            UIKit.Place(portrait.Root, UIKit.Bottom, UIKit.Bottom, new Vector2(0f, 30f), portrait.Root.sizeDelta);
            var sp = ContentDatabase.Species[(index * 5 + 2) % ContentDatabase.Species.Count];
            foreach (var s in ContentDatabase.Species)
                if (s.Worlds.Length > 0 && s.LivesIn(w.Index)) { sp = s; break; }
            portrait.Set(sp, unlocked);

            var name = Kit.Text(card.transform, w.Name, 54, Theme.Ink);
            UIKit.Place(name, UIKit.Bottom, UIKit.Bottom, new Vector2(0f, 245f), new Vector2(430f, 70f));
            var tag = Kit.Text(card.transform, w.Tagline, 28, Theme.Muted, TextAnchor.UpperCenter, FontStyle.Normal);
            UIKit.Place(tag, UIKit.Bottom, UIKit.Bottom, new Vector2(0f, 130f), new Vector2(400f, 110f));

            int stars = save.WorldStars(w.Index);
            var starIcon = Kit.Icon(card.transform, Kit.Art.Star, Theme.Accent, 56f);
            UIKit.Place(starIcon, UIKit.Bottom, UIKit.Bottom, new Vector2(-70f, 40f), new Vector2(56f, 56f));
            var starText = Kit.Text(card.transform, $"{stars}/{ContentDatabase.LevelsPerWorld * 3}", 40, Theme.Ink, TextAnchor.MiddleLeft);
            UIKit.Place(starText, UIKit.Bottom, UIKit.Bottom, new Vector2(60f, 40f), new Vector2(200f, 60f));

            var btn = card.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            card.gameObject.AddComponent<ButtonFeedback>();

            if (!unlocked)
            {
                var lockDim = Kit.Panel(card.transform, new Color(0.08f, 0.1f, 0.18f, 0.72f), "Locked");
                lockDim.raycastTarget = false;
                UIKit.Stretch(lockDim.rectTransform);
                var lockIcon = Kit.Icon(lockDim.transform, Kit.Art.Lock, Color.white, 140f);
                UIKit.Place(lockIcon, UIKit.Center, UIKit.Center, new Vector2(0f, 60f), new Vector2(140f, 140f));
                var need = Kit.Text(lockDim.transform, $"Benötigt {w.StarsToUnlock} Sterne", 38, Color.white);
                UIKit.Place(need, UIKit.Center, UIKit.Center, new Vector2(0f, -60f), new Vector2(420f, 60f));
                btn.onClick.AddListener(() =>
                {
                    UIKit.ClickSound?.Invoke();
                    Tweener.Shake(card.transform, 14f, 0.35f);
                    Game.UI.Toast($"Sammle {w.StarsToUnlock - save.TotalStars} weitere Sterne!");
                });
            }
            else
            {
                btn.onClick.AddListener(() =>
                {
                    UIKit.ClickSound?.Invoke();
                    Game.UI.Levels.Open(w.Index);
                });
            }
            PopIn(card.transform, 0.04f * index);
        }
    }

    /// <summary>Levelauswahl innerhalb einer Welt.</summary>
    public sealed class LevelSelectScreen : UIScreen
    {
        int _world;
        Text _title;
        RectTransform _grid;
        Image _bg;

        public void Open(int world)
        {
            _world = world;
            Show();
        }

        protected override void Build()
        {
            _bg = Backdrop(Color.white, Color.white);
            _title = Header("", () => Game.UI.Worlds.Show());
            _grid = Kit.Rect("Grid", Root);
            UIKit.Place(_grid, UIKit.Center, UIKit.Center, new Vector2(0f, -60f), new Vector2(1500f, 720f));
            var g = _grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(460f, 330f);
            g.spacing = new Vector2(40f, 40f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;
            g.childAlignment = TextAnchor.MiddleCenter;
        }

        protected override void OnShow()
        {
            var w = ContentDatabase.Worlds[_world];
            _title.text = w.Name;
            _bg.color = w.Underwater ? w.WaterBottom : w.WaterTop;
            _bg.transform.GetChild(0).GetComponent<Image>().color = w.Underwater ? w.WaterTop : w.SkyTop;
            Kit.Clear(_grid);
            var save = Game.Save;
            for (int l = 0; l < ContentDatabase.LevelsPerWorld; l++) BuildButton(w, l, save);
        }

        void BuildButton(WorldDefinition w, int l, SaveData save)
        {
            bool unlocked = save.IsLevelUnlocked(w.Index, l);
            var def = ContentDatabase.BuildLevel(w.Index, l);
            var rec = save.GetLevel(def.Key);
            int level = l;
            var btn = Kit.Button(_grid, null, unlocked ? Color.white : new Color(0.85f, 0.87f, 0.9f), () =>
            {
                if (unlocked) Game.PlayLevel(w.Index, level);
                else Game.UI.Toast("Schaffe zuerst das vorherige Level!");
            });

            var num = Kit.Text(btn.transform, (l + 1).ToString(), 92, unlocked ? Theme.Primary : Theme.Muted);
            UIKit.Place(num, UIKit.TopLeft, UIKit.TopLeft, new Vector2(30f, -14f), new Vector2(120f, 120f));
            var name = Kit.Text(btn.transform, ContentDatabase.LevelName(l), 40, Theme.Ink, TextAnchor.MiddleLeft);
            UIKit.Place(name, UIKit.TopLeft, UIKit.TopLeft, new Vector2(150f, -30f), new Vector2(290f, 60f));
            var goal = Kit.Text(btn.transform, ContentDatabase.GoalText(def), 28, Theme.Muted, TextAnchor.UpperLeft, FontStyle.Normal);
            UIKit.Place(goal, UIKit.TopLeft, UIKit.TopLeft, new Vector2(150f, -92f), new Vector2(290f, 110f));

            if (unlocked)
            {
                var stars = Kit.StarRow(btn.transform, rec?.Stars ?? 0, 70f);
                UIKit.Place(stars, UIKit.BottomLeft, UIKit.BottomLeft, new Vector2(30f, 26f), stars.sizeDelta);
                if (rec != null && rec.BestTime > 0f)
                {
                    var best = Kit.Text(btn.transform, "Best: " + UIKit.FormatTime(rec.BestTime), 28, Theme.Muted, TextAnchor.MiddleRight, FontStyle.Normal);
                    UIKit.Place(best, UIKit.BottomRight, UIKit.BottomRight, new Vector2(-26f, 36f), new Vector2(200f, 50f));
                }
            }
            else
            {
                var lockIcon = Kit.Icon(btn.transform, Kit.Art.Lock, Theme.Muted, 80f);
                UIKit.Place(lockIcon, UIKit.BottomRight, UIKit.BottomRight, new Vector2(-30f, 30f), new Vector2(80f, 80f));
            }
            PopIn(btn.transform, 0.04f * l);
        }
    }

    /// <summary>Pulsierende Beschriftung – lädt zum Tippen ein.</summary>
    public sealed class Pulse : MonoBehaviour
    {
        float _t;

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            float s = 1f + Mathf.Sin(_t * 3f) * 0.025f;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
