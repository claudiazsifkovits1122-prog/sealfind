using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SealFind
{
    /// <summary>Anzeige während des Spiels: Ziel, Fortschritt, Zeit, Hinweis- und Pause-Button, schwebende Texte.</summary>
    public sealed class HudScreen : UIScreen
    {
        const int MaxIcons = 16;

        Text _goal, _counter, _timer, _combo, _hintCount;
        Image _timerPill, _goalPanel;
        RectTransform _icons, _floatLayer;
        readonly List<Image> _iconImages = new List<Image>();
        Button _hintButton;
        CanvasGroup _banner;
        Text _bannerTitle, _bannerSub;
        int _filled;
        bool _timed;
        int _lastWarnSecond = -1;

        public RectTransform FloatLayer => _floatLayer;

        protected override void Build()
        {
            var safe = Kit.Rect("Safe", Root);
            UIKit.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            // Ziel + Fortschritt
            _goalPanel = Kit.Panel(safe, new Color(1f, 1f, 1f, 0.92f), "Goal");
            UIKit.Place(_goalPanel, UIKit.TopLeft, UIKit.TopLeft, new Vector2(30f, -24f), new Vector2(720f, 150f));
            UIKit.Shadow(_goalPanel, 6f, 0.2f);
            _goal = Kit.Text(_goalPanel.transform, "", 36, Theme.Ink, TextAnchor.MiddleLeft);
            UIKit.Place(_goal, UIKit.TopLeft, UIKit.TopLeft, new Vector2(28f, -12f), new Vector2(560f, 60f));
            _goal.resizeTextForBestFit = true;
            _goal.resizeTextMinSize = 22;
            _goal.resizeTextMaxSize = 36;
            _goal.horizontalOverflow = HorizontalWrapMode.Wrap;
            _goal.verticalOverflow = VerticalWrapMode.Truncate;
            _counter = Kit.Text(_goalPanel.transform, "0/0", 44, Theme.Primary, TextAnchor.MiddleRight);
            UIKit.Place(_counter, UIKit.TopRight, UIKit.TopRight, new Vector2(-24f, -12f), new Vector2(160f, 60f));
            _icons = Kit.Rect("Icons", _goalPanel.transform);
            UIKit.Place(_icons, UIKit.BottomLeft, UIKit.BottomLeft, new Vector2(24f, 14f), new Vector2(680f, 62f));
            var row = _icons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 2f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;

            // Zeit
            _timerPill = Kit.Panel(safe, new Color(0.05f, 0.1f, 0.2f, 0.65f), "Timer");
            _timerPill.raycastTarget = false;
            UIKit.Place(_timerPill, UIKit.Top, UIKit.Top, new Vector2(80f, -30f), new Vector2(270f, 96f));
            var clock = Kit.Icon(_timerPill.transform, Kit.Art.ClockIcon, Color.white, 56f);
            UIKit.Place(clock, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(56f, 56f));
            _timer = Kit.Text(_timerPill.transform, "0.0s", 46, Color.white, TextAnchor.MiddleLeft);
            UIKit.Place(_timer, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(92f, 0f), new Vector2(170f, 80f));

            // Pause + Hinweis
            var pause = Kit.IconButton(safe, Kit.Art.PauseIcon, Theme.Deep, () => Game.Pause(), 110f, 0.45f);
            UIKit.Place(pause, UIKit.TopRight, UIKit.TopRight, new Vector2(-30f, -30f), new Vector2(110f, 110f));
            _hintButton = Kit.IconButton(safe, Kit.Art.Magnifier, Theme.Accent, () => Game.UseHint(), 110f, 0.5f);
            UIKit.Place(_hintButton, UIKit.TopRight, UIKit.TopRight, new Vector2(-160f, -30f), new Vector2(110f, 110f));
            var badge = Kit.Badge(_hintButton.transform, "3", Theme.Danger, 48f);
            UIKit.Place(badge, UIKit.TopRight, UIKit.Center, new Vector2(-8f, -8f), new Vector2(48f, 48f));
            _hintCount = badge.GetComponentInChildren<Text>();

            // Combo
            _combo = Kit.Text(safe, "", 72, Theme.Accent);
            UIKit.Place(_combo, UIKit.Top, UIKit.Top, new Vector2(0f, -170f), new Vector2(800f, 100f));
            UIKit.Outline(_combo, Theme.Deep, 5f);
            _combo.gameObject.SetActive(false);

            _floatLayer = Kit.Rect("Floating", Root);
            UIKit.Stretch(_floatLayer);

            // Intro-Banner
            var bannerImg = Kit.Panel(Root, new Color(0.04f, 0.12f, 0.25f, 0.82f), "Banner");
            bannerImg.raycastTarget = false;
            UIKit.Place(bannerImg, UIKit.Center, UIKit.Center, new Vector2(0f, 60f), new Vector2(1300f, 260f));
            _banner = bannerImg.gameObject.AddComponent<CanvasGroup>();
            _banner.blocksRaycasts = false;
            _bannerTitle = Kit.Text(bannerImg.transform, "", 70, Color.white);
            UIKit.Place(_bannerTitle, UIKit.Center, UIKit.Center, new Vector2(0f, 40f), new Vector2(1240f, 110f));
            _bannerSub = Kit.Text(bannerImg.transform, "", 40, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIKit.Place(_bannerSub, UIKit.Center, UIKit.Center, new Vector2(0f, -55f), new Vector2(1240f, 70f));
            _banner.gameObject.SetActive(false);
        }

        public void Bind(LevelSession session)
        {
            var def = session.Level;
            _timed = def.IsTimed;
            _lastWarnSecond = -1;
            _goal.text = ContentDatabase.GoalText(def);
            _counter.text = $"0/{session.TargetTotal}";
            _filled = 0;
            Kit.Clear(_icons);
            _iconImages.Clear();
            int icons = Mathf.Min(session.TargetTotal, MaxIcons);
            float size = icons > 12 ? 40f : 52f;
            _icons.gameObject.SetActive(icons > 0);
            for (int i = 0; i < icons; i++)
            {
                var img = Kit.Image(_icons, Kit.Art.SealBody(def.Goal == GoalType.FindBabies), new Color(0.15f, 0.2f, 0.3f, 0.35f), "Icon" + i);
                img.preserveAspect = true;
                img.rectTransform.sizeDelta = new Vector2(size * 1.33f, size);
                _iconImages.Add(img);
            }
            float width = Mathf.Max(620f, icons * (size * 1.33f + 2f) + 60f);
            _goalPanel.rectTransform.sizeDelta = new Vector2(width, 150f);
            UpdateHints(session.HintsLeft);
            _timer.color = Color.white;
            _combo.gameObject.SetActive(false);
            Kit.Clear(_floatLayer);
        }

        public void Tick(LevelSession s)
        {
            if (s == null) return;
            float shown = _timed ? s.TimeLeft : s.EffectiveTime;
            _timer.text = UIKit.FormatTime(shown);
            if (_timed && shown <= 10f)
            {
                _timer.color = Color.Lerp(Theme.Danger, Color.white, Mathf.PingPong(Time.unscaledTime * 2f, 1f) * 0.5f);
                int sec = Mathf.CeilToInt(shown);
                if (sec != _lastWarnSecond && s.Running)
                {
                    _lastWarnSecond = sec;
                    Game.Audio.Play(Sfx.Tick, 0.6f);
                    Tweener.Punch(_timerPill.transform, 0.12f, 0.2f, true);
                }
            }
        }

        public void OnFound(Seal seal, LevelSession s, int combo)
        {
            _counter.text = $"{s.TargetFound}/{s.TargetTotal}";
            Tweener.Punch(_counter.transform, 0.35f, 0.3f, true);
            if (seal.IsTarget && _filled < _iconImages.Count)
            {
                var img = _iconImages[_filled++];
                img.sprite = Kit.Art.SealBody(seal.Species.IsBaby);
                img.color = seal.Species.BodyColor;
                Tweener.Scale(img.transform, Vector3.one * 1.8f, Vector3.one, 0.4f, Ease.OutBack, null, 0f, true);
            }
            if (combo >= 2)
            {
                _combo.gameObject.SetActive(true);
                _combo.text = $"COMBO x{combo}!";
                _combo.color = combo >= 4 ? Theme.Danger : Theme.Accent;
                Tweener.Kill(_combo.transform);
                Tweener.Scale(_combo.transform, Vector3.one * 0.4f, Vector3.one, 0.35f, Ease.OutBack, null, 0f, true);
                Tweener.Delay(_combo.transform, 1.2f, () => _combo.gameObject.SetActive(false), true);
            }
        }

        public void OnPenalty()
        {
            Tweener.Shake(_timerPill.transform, 12f, 0.3f);
        }

        public void UpdateHints(int left)
        {
            _hintCount.text = left.ToString();
            _hintButton.interactable = left > 0;
            _hintButton.targetGraphic.color = left > 0 ? Theme.Accent : Theme.Muted;
        }

        public void ShowBanner(string title, string sub, float duration = 2.2f)
        {
            _bannerTitle.text = title;
            _bannerSub.text = sub;
            _banner.gameObject.SetActive(true);
            Tweener.Kill(_banner);
            Tweener.Fade(_banner, 0f, 1f, 0.25f);
            Tweener.Scale(_banner.transform, Vector3.one * 0.7f, Vector3.one, 0.4f, Ease.OutBack, null, 0f, true);
            Tweener.Run(_banner, 0.35f, k => _banner.alpha = 1f - k, Ease.InQuad, () => _banner.gameObject.SetActive(false), duration, true);
        }

        public void HideBanner()
        {
            Tweener.Kill(_banner);
            _banner.gameObject.SetActive(false);
        }
    }

    /// <summary>Pausenmenü.</summary>
    public sealed class PauseScreen : UIScreen
    {
        Text _goal;
        RectTransform _panel;

        protected override void Build()
        {
            Kit.Dim(Root);
            var panel = Kit.Panel(Root, Theme.Panel);
            _panel = panel.rectTransform;
            UIKit.Place(panel, UIKit.Center, UIKit.Center, Vector2.zero, new Vector2(760f, 760f));
            var title = Kit.Text(panel.transform, "Pause", 84, Theme.Ink);
            UIKit.Place(title, UIKit.Top, UIKit.Top, new Vector2(0f, -40f), new Vector2(700f, 110f));
            _goal = Kit.Text(panel.transform, "", 36, Theme.Muted, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIKit.Place(_goal, UIKit.Top, UIKit.Top, new Vector2(0f, -150f), new Vector2(680f, 70f));

            var resume = Kit.Button(panel.transform, "Weiter", Theme.Primary, () => Game.Resume(), 54);
            UIKit.Place(resume, UIKit.Top, UIKit.Top, new Vector2(0f, -250f), new Vector2(560f, 130f));
            var restart = Kit.Button(panel.transform, "Neu starten", Theme.Secondary, () => Game.Retry(), 46);
            UIKit.Place(restart, UIKit.Top, UIKit.Top, new Vector2(0f, -410f), new Vector2(560f, 110f));
            var menu = Kit.Button(panel.transform, "Zum Menü", Theme.Deep, () => Game.GoToMenu(), 46);
            UIKit.Place(menu, UIKit.Top, UIKit.Top, new Vector2(0f, -550f), new Vector2(560f, 110f));
        }

        protected override void OnShow()
        {
            _goal.text = Game.CurrentLevel != null ? ContentDatabase.GoalText(Game.CurrentLevel) : "";
            PopIn(_panel);
        }
    }

    /// <summary>Ergebnis-Bildschirm mit Sternen, Statistik und neu entdeckten Robben.</summary>
    public sealed class ResultsScreen : UIScreen
    {
        RectTransform _panel, _content;
        LevelResult _result;

        public void Open(LevelResult result)
        {
            _result = result;
            Show();
        }

        protected override void Build()
        {
            Kit.Dim(Root);
            var panel = Kit.Panel(Root, Theme.Panel);
            _panel = panel.rectTransform;
            UIKit.Place(panel, UIKit.Center, UIKit.Center, Vector2.zero, new Vector2(1240f, 940f));
            _content = Kit.Rect("Content", panel.transform);
            UIKit.Stretch(_content);
        }

        protected override void OnShow()
        {
            Kit.Clear(_content);
            var r = _result;
            var save = Game.Save;
            var def = r.Level;

            string title = r.Success
                ? (r.Stars == 3 ? "Perfekt!" : "Geschafft!")
                : "Zeit abgelaufen!";
            var t = Kit.Text(_content, title, 96, r.Success ? Theme.Primary : Theme.Danger);
            UIKit.Place(t, UIKit.Top, UIKit.Top, new Vector2(0f, -30f), new Vector2(1100f, 120f));

            string sub = def.Mode == GameMode.Campaign
                ? $"{ContentDatabase.Worlds[def.World].Name} · Level {def.Level + 1}"
                : def.Mode == GameMode.Daily ? "Tages-Challenge" : "Zufallsrunde";
            var st = Kit.Text(_content, sub, 36, Theme.Muted, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIKit.Place(st, UIKit.Top, UIKit.Top, new Vector2(0f, -145f), new Vector2(1100f, 50f));

            // Sterne nacheinander einfliegen lassen
            var starRow = Kit.Rect("Stars", _content);
            UIKit.Place(starRow, UIKit.Top, UIKit.Top, new Vector2(0f, -210f), new Vector2(520f, 170f));
            for (int i = 0; i < 3; i++)
            {
                float size = i == 1 ? 170f : 140f;
                var bg = Kit.Icon(starRow, Kit.Art.Star, Theme.StarOff, size, "StarBg" + i);
                UIKit.Place(bg, new Vector2(0.5f, 1f), UIKit.Top, new Vector2((i - 1) * 175f, i == 1 ? 0f : -24f), new Vector2(size, size));
                if (i < r.Stars)
                {
                    var star = Kit.Icon(bg.transform, Kit.Art.Star, Theme.Accent, size, "Star");
                    UIKit.Stretch(star.rectTransform);
                    UIKit.Outline(star, new Color(0.8f, 0.5f, 0f, 0.6f), 3f);
                    float delay = 0.35f + i * 0.3f;
                    star.transform.localScale = Vector3.zero;
                    int idx = i;
                    Tweener.Scale(star.transform, Vector3.zero, Vector3.one, 0.45f, Ease.OutBack, null, delay, true);
                    Tweener.Delay(star, delay, () => Game.Audio.Play(Sfx.Star, 0.8f, 1f + idx * 0.12f), true);
                }
            }

            // Statistik
            var stats = Kit.Rect("Stats", _content);
            UIKit.Place(stats, UIKit.Top, UIKit.Top, new Vector2(0f, -400f), new Vector2(1100f, 170f));
            AddStat(stats, 0, "Zeit", UIKit.FormatTime(r.Time));
            AddStat(stats, 1, "Gefunden", $"{r.TargetFound}/{r.TargetTotal}");
            AddStat(stats, 2, "Fehlklicks", r.Misses.ToString());
            AddStat(stats, 3, "Punkte", r.Score.ToString());

            float y = -585f;
            if (r.NewRecord && r.Success)
            {
                var rec = Kit.Panel(_content, Theme.Accent, "Record");
                rec.raycastTarget = false;
                UIKit.Place(rec, UIKit.Top, UIKit.Top, new Vector2(0f, y), new Vector2(460f, 64f));
                var rt = Kit.Text(rec.transform, def.Mode == GameMode.Daily ? "Neue Tages-Bestzeit!" : "Neue Bestzeit!", 38, Theme.Ink);
                UIKit.Stretch(rt.rectTransform);
                Tweener.Punch(rec.transform, 0.2f, 0.5f, true);
                rec.transform.localRotation = Quaternion.Euler(0f, 0f, -3f);
                y -= 80f;
            }
            else if (!r.Success)
            {
                var hint = Kit.Text(_content, "Die roten Kreise zeigen, wo sie sich versteckt hatten.", 34, Theme.Muted, TextAnchor.MiddleCenter, FontStyle.Normal);
                UIKit.Place(hint, UIKit.Top, UIKit.Top, new Vector2(0f, y), new Vector2(1100f, 60f));
                y -= 70f;
            }
            else if (r.BonusFound > 0)
            {
                var bonus = Kit.Text(_content, "Bonus: Legendäre Robbe gefunden!", 38, Theme.Primary);
                UIKit.Place(bonus, UIKit.Top, UIKit.Top, new Vector2(0f, y), new Vector2(1100f, 60f));
                y -= 70f;
            }

            // Neue Robben
            if (r.NewSpecies.Count > 0)
            {
                var label = Kit.Text(_content, r.NewSpecies.Count == 1 ? "Neue Robbe entdeckt!" : $"{r.NewSpecies.Count} neue Robben entdeckt!", 36, Theme.Ink);
                UIKit.Place(label, UIKit.Top, UIKit.Top, new Vector2(0f, y), new Vector2(1100f, 50f));
                int n = Mathf.Min(r.NewSpecies.Count, 4);
                for (int i = 0; i < n; i++)
                {
                    var sp = r.NewSpecies[i];
                    var p = new SealPortrait(Kit, _content, 150f);
                    p.Set(sp, true, SealFace.Happy);
                    float x = (i - (n - 1) * 0.5f) * 250f;
                    UIKit.Place(p.Root, UIKit.Top, UIKit.Top, new Vector2(x - 40f, y - 50f), p.Root.sizeDelta);
                    var nm = Kit.Text(_content, sp.Name, 26, RarityInfo.Color(sp.Rarity), TextAnchor.MiddleLeft);
                    UIKit.Place(nm, UIKit.Top, UIKit.Top, new Vector2(x + 75f, y - 80f), new Vector2(160f, 60f));
                    nm.resizeTextForBestFit = true;
                    nm.resizeTextMinSize = 16;
                    nm.resizeTextMaxSize = 26;
                    PopIn(p.Root, 1.2f + i * 0.15f);
                }
            }

            // Buttons
            var buttons = Kit.Rect("Buttons", _content);
            UIKit.Place(buttons, UIKit.Bottom, UIKit.Bottom, new Vector2(0f, 40f), new Vector2(1160f, 130f));
            var menu = Kit.Button(buttons, "Menü", Theme.Deep, () => Game.GoToMenu(), 44);
            UIKit.Place(menu, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(300f, 120f));
            var again = Kit.Button(buttons, "Nochmal", Theme.Secondary, () => Game.Retry(), 44);
            UIKit.Place(again, UIKit.Center, UIKit.Center, new Vector2(r.Success ? -60f : 160f, 0f), new Vector2(320f, 120f));
            if (r.Success && def.Mode != GameMode.Daily)
            {
                var next = Kit.Button(buttons, "Weiter", Theme.Primary, () => Game.PlayNext(), 56);
                UIKit.Place(next, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(400f, 130f));
                next.transform.Find("Label").gameObject.AddComponent<Pulse>();
            }
            else if (def.Mode == GameMode.Daily)
            {
                var info = Kit.Text(buttons, $"Serie: {save.DailyStreak} {(save.DailyStreak == 1 ? "Tag" : "Tage")}", 36, Theme.Muted, TextAnchor.MiddleRight);
                UIKit.Place(info, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(380f, 120f));
            }
            PopIn(_panel);
        }

        void AddStat(RectTransform parent, int index, string label, string value)
        {
            var box = Kit.Panel(parent, new Color(0.93f, 0.95f, 0.98f), "Stat" + index);
            box.raycastTarget = false;
            UIKit.Place(box, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(index * 280f, 0f), new Vector2(260f, 150f));
            var v = Kit.Text(box.transform, value, 52, Theme.Ink);
            UIKit.Place(v, UIKit.Top, UIKit.Top, new Vector2(0f, -18f), new Vector2(250f, 70f));
            var l = Kit.Text(box.transform, label, 28, Theme.Muted, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIKit.Place(l, UIKit.Bottom, UIKit.Bottom, new Vector2(0f, 14f), new Vector2(250f, 44f));
        }
    }
}
