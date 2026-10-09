using UnityEngine;
using UnityEngine.UI;

namespace SealFind
{
    /// <summary>Sammelalbum aller Robben-Arten. Unentdeckte erscheinen als Silhouette – macht neugierig.</summary>
    public sealed class CollectionScreen : UIScreen
    {
        RectTransform _grid;
        Text _title;
        Image _progress;
        RectTransform _detail;
        SealPortrait _detailPortrait;
        Text _detailName, _detailRarity, _detailBio, _detailInfo;
        RectTransform _detailStars;

        protected override void Build()
        {
            Backdrop(new Color32(140, 98, 214, 255), new Color32(92, 64, 160, 255));
            _title = Header("Sammlung", () => Game.UI.ShowMain());

            var barBg = Kit.Panel(Root, new Color(0f, 0f, 0f, 0.25f), "ProgressBg");
            barBg.raycastTarget = false;
            UIKit.Place(barBg, UIKit.Top, UIKit.Top, new Vector2(0f, -150f), new Vector2(800f, 28f));
            _progress = Kit.Panel(barBg.transform, Theme.Accent, "Progress");
            _progress.raycastTarget = false;
            var prt = _progress.rectTransform;
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = new Vector2(0f, 1f);
            prt.pivot = new Vector2(0f, 0.5f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;

            _grid = Kit.Scroll(Root, false, out var scroll);
            UIKit.Stretch((RectTransform)scroll.transform, 60, 20, 60, 200);
            var g = _grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(300f, 330f);
            g.spacing = new Vector2(28f, 28f);
            g.padding = new RectOffset(20, 20, 20, 40);
            g.childAlignment = TextAnchor.UpperCenter;
            g.constraint = GridLayoutGroup.Constraint.Flexible;

            BuildDetail();
        }

        protected override void OnShow()
        {
            var save = Game.Save;
            int found = save.DiscoveredSpecies, total = ContentDatabase.Species.Count;
            _title.text = $"Sammlung  {found}/{total}";
            float pct = total > 0 ? found / (float)total : 0f;
            _progress.rectTransform.anchorMax = new Vector2(pct, 1f);
            _progress.gameObject.SetActive(pct > 0f);
            _detail.gameObject.SetActive(false);

            Kit.Clear(_grid);
            int i = 0;
            foreach (var sp in ContentDatabase.Species) BuildCard(sp, save.SpeciesCount(sp.Id), i++);
        }

        void BuildCard(SealSpecies sp, int count, int index)
        {
            bool known = count > 0;
            var rarity = RarityInfo.Color(sp.Rarity);
            var card = Kit.Button(_grid, null, known ? Color.white : new Color(0.88f, 0.86f, 0.95f), () => OpenDetail(sp));
            var strip = Kit.Panel(card.transform, rarity, "Rarity");
            strip.raycastTarget = false;
            UIKit.Place(strip, UIKit.Top, UIKit.Top, new Vector2(0f, -10f), new Vector2(270f, 16f));

            var portrait = new SealPortrait(Kit, card.transform, 250f);
            UIKit.Place(portrait.Root, UIKit.Top, UIKit.Top, new Vector2(0f, -34f), portrait.Root.sizeDelta);
            portrait.Set(sp, known);

            var name = Kit.Text(card.transform, known ? sp.Name : "???", 32, known ? Theme.Ink : Theme.Muted);
            UIKit.Place(name, UIKit.Bottom, UIKit.Bottom, new Vector2(0f, 62f), new Vector2(280f, 50f));
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 20;
            name.resizeTextMaxSize = 32;

            var stars = Kit.Rect("Rarity", card.transform);
            int n = RarityInfo.StarCount(sp.Rarity);
            UIKit.Place(stars, UIKit.Bottom, UIKit.Bottom, new Vector2(known ? -40f : 0f, 24f), new Vector2(n * 30f, 30f));
            for (int s = 0; s < n; s++)
            {
                var st = Kit.Icon(stars, Kit.Art.Star, rarity, 28f);
                UIKit.Place(st, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(s * 30f, 0f), new Vector2(28f, 28f));
            }
            if (known)
            {
                var c = Kit.Text(card.transform, "x" + count, 28, Theme.Muted, TextAnchor.MiddleRight);
                UIKit.Place(c, UIKit.BottomRight, UIKit.BottomRight, new Vector2(-20f, 20f), new Vector2(100f, 40f));
            }
            if (index < 24) PopIn(card.transform, 0.015f * index);
        }

        void BuildDetail()
        {
            _detail = Kit.Rect("Detail", Root);
            UIKit.Stretch(_detail);
            var dim = Kit.Dim(_detail);
            var close = dim.gameObject.AddComponent<Button>();
            close.transition = Selectable.Transition.None;
            close.onClick.AddListener(() => _detail.gameObject.SetActive(false));

            var panel = Kit.Panel(_detail, Theme.Panel, "Card");
            UIKit.Place(panel, UIKit.Center, UIKit.Center, Vector2.zero, new Vector2(1100f, 640f));
            _detailPortrait = new SealPortrait(Kit, panel.transform, 460f);
            UIKit.Place(_detailPortrait.Root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 40f), _detailPortrait.Root.sizeDelta);
            _detailPortrait.Root.gameObject.AddComponent<Bobbing>().Amplitude = 8f;

            _detailName = Kit.Text(panel.transform, "", 70, Theme.Ink, TextAnchor.MiddleLeft);
            UIKit.Place(_detailName, UIKit.TopLeft, UIKit.TopLeft, new Vector2(530f, -50f), new Vector2(540f, 90f));
            _detailRarity = Kit.Text(panel.transform, "", 36, Theme.Muted, TextAnchor.MiddleLeft);
            UIKit.Place(_detailRarity, UIKit.TopLeft, UIKit.TopLeft, new Vector2(530f, -140f), new Vector2(300f, 50f));
            _detailStars = Kit.Rect("Stars", panel.transform);
            UIKit.Place(_detailStars, UIKit.TopLeft, UIKit.TopLeft, new Vector2(830f, -142f), new Vector2(240f, 46f));
            _detailBio = Kit.Text(panel.transform, "", 40, Theme.Ink, TextAnchor.UpperLeft, FontStyle.Italic);
            UIKit.Place(_detailBio, UIKit.TopLeft, UIKit.TopLeft, new Vector2(530f, -215f), new Vector2(530f, 220f));
            _detailInfo = Kit.Text(panel.transform, "", 30, Theme.Muted, TextAnchor.UpperLeft, FontStyle.Normal);
            UIKit.Place(_detailInfo, UIKit.TopLeft, UIKit.TopLeft, new Vector2(530f, -450f), new Vector2(530f, 140f));

            var ok = Kit.Button(panel.transform, "Schließen", Theme.Deep, () => _detail.gameObject.SetActive(false), 36);
            UIKit.Place(ok, UIKit.BottomRight, UIKit.BottomRight, new Vector2(-30f, 30f), new Vector2(260f, 90f));
            _detail.gameObject.SetActive(false);
        }

        void OpenDetail(SealSpecies sp)
        {
            int count = Game.Save.SpeciesCount(sp.Id);
            bool known = count > 0;
            _detailPortrait.Set(sp, known, known ? SealFace.Happy : SealFace.Open);
            _detailName.text = known ? sp.Name : "???";
            _detailRarity.text = RarityInfo.Label(sp.Rarity);
            _detailRarity.color = RarityInfo.Color(sp.Rarity);
            Kit.Clear(_detailStars);
            for (int s = 0; s < RarityInfo.StarCount(sp.Rarity); s++)
            {
                var st = Kit.Icon(_detailStars, Kit.Art.Star, RarityInfo.Color(sp.Rarity), 44f);
                UIKit.Place(st, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(s * 46f, 0f), new Vector2(44f, 44f));
            }
            _detailBio.text = known ? "„" + sp.Bio + "“" : "Diese Robbe hast du noch nicht entdeckt. Halte die Augen offen!";

            string where = "Überall";
            if (sp.Worlds.Length > 0)
            {
                var names = new string[sp.Worlds.Length];
                for (int i = 0; i < names.Length; i++) names[i] = ContentDatabase.Worlds[sp.Worlds[i]].Name;
                where = string.Join(", ", names);
            }
            _detailInfo.text = (known ? $"Gefunden: {count}x\n" : "") + "Lebt in: " + where +
                               (sp.Accessory != Accessory.None ? "\nTrägt: " + ContentDatabase.AccessoryName(sp.Accessory) : "") +
                               (sp.IsBaby ? "\nBabyrobbe" : "");
            _detail.gameObject.SetActive(true);
            _detail.SetAsLastSibling();
            PopIn(_detail.GetChild(1));
        }
    }

    /// <summary>Einstellungen: Sound, Umgebung, Vibration, Fortschritt zurücksetzen.</summary>
    public sealed class SettingsScreen : UIScreen
    {
        Text _sound, _ambience, _vibration, _reset;
        RectTransform _panel;
        bool _confirmReset;

        protected override void Build()
        {
            Kit.Dim(Root);
            var panel = Kit.Panel(Root, Theme.Panel);
            _panel = panel.rectTransform;
            UIKit.Place(panel, UIKit.Center, UIKit.Center, Vector2.zero, new Vector2(820f, 900f));
            var title = Kit.Text(panel.transform, "Einstellungen", 70, Theme.Ink);
            UIKit.Place(title, UIKit.Top, UIKit.Top, new Vector2(0f, -36f), new Vector2(760f, 100f));

            _sound = Toggle(panel.transform, -170f, () => { Game.Save.SoundOn = !Game.Save.SoundOn; Apply(); });
            _ambience = Toggle(panel.transform, -300f, () => { Game.Save.AmbienceOn = !Game.Save.AmbienceOn; Apply(); });
            _vibration = Toggle(panel.transform, -430f, () => { Game.Save.VibrationOn = !Game.Save.VibrationOn; Apply(); });

            var reset = Kit.Button(panel.transform, "Fortschritt löschen", Theme.Danger, () =>
            {
                if (!_confirmReset)
                {
                    _confirmReset = true;
                    _reset.text = "Wirklich alles löschen?";
                    return;
                }
                _confirmReset = false;
                Game.ResetProgress();
                Refresh();
                Game.UI.Toast("Fortschritt gelöscht.");
            }, 38);
            UIKit.Place(reset, UIKit.Top, UIKit.Top, new Vector2(0f, -580f), new Vector2(620f, 100f));
            _reset = reset.transform.Find("Label").GetComponent<Text>();

            var close = Kit.Button(panel.transform, "Fertig", Theme.Primary, () => Hide(), 46);
            UIKit.Place(close, UIKit.Bottom, UIKit.Bottom, new Vector2(0f, 40f), new Vector2(400f, 110f));

            var credits = Kit.Text(panel.transform, "Robben-Suche · ein Unity-Übungsprojekt\nAlle Grafiken & Sounds werden prozedural erzeugt.", 24, Theme.Muted, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIKit.Place(credits, UIKit.Bottom, UIKit.Bottom, new Vector2(0f, 160f), new Vector2(760f, 80f));
        }

        Text Toggle(Transform parent, float y, System.Action onClick)
        {
            var b = Kit.Button(parent, "…", Theme.Secondary, onClick, 40);
            UIKit.Place(b, UIKit.Top, UIKit.Top, new Vector2(0f, y), new Vector2(620f, 100f));
            return b.transform.Find("Label").GetComponent<Text>();
        }

        void Apply()
        {
            Game.ApplySettings();
            SaveSystem.Save(Game.Save);
            Refresh();
        }

        void Refresh()
        {
            var s = Game.Save;
            _sound.text = "Soundeffekte: " + (s.SoundOn ? "An" : "Aus");
            _ambience.text = "Meeresrauschen: " + (s.AmbienceOn ? "An" : "Aus");
            _vibration.text = "Vibration: " + (s.VibrationOn ? "An" : "Aus");
            _reset.text = "Fortschritt löschen";
        }

        protected override void OnShow()
        {
            _confirmReset = false;
            Refresh();
            PopIn(_panel);
        }
    }

    /// <summary>Kurze Erklärung beim allerersten Level.</summary>
    public sealed class TutorialScreen : UIScreen
    {
        System.Action _onClose;
        RectTransform _panel;

        public void Open(System.Action onClose)
        {
            _onClose = onClose;
            Show();
        }

        protected override void Build()
        {
            Kit.Dim(Root);
            var panel = Kit.Panel(Root, Theme.Panel);
            _panel = panel.rectTransform;
            UIKit.Place(panel, UIKit.Center, UIKit.Center, Vector2.zero, new Vector2(1200f, 760f));
            var title = Kit.Text(panel.transform, "So geht's", 76, Theme.Primary);
            UIKit.Place(title, UIKit.Top, UIKit.Top, new Vector2(0f, -36f), new Vector2(1100f, 100f));

            string[] lines =
            {
                "Tippe auf versteckte Robben, um sie zu finden.",
                "Ziehen verschiebt das Bild – zwei Finger oder Mausrad zoomen.",
                "Vorsicht: Daneben tippen kostet Strafsekunden. Manche Steine sehen nur aus wie Robben!",
                "Steckst du fest? Die Lupe gibt einen Hinweis.",
            };
            for (int i = 0; i < lines.Length; i++)
            {
                var dot = Kit.Icon(panel.transform, Kit.Art.Circle, i % 2 == 0 ? Theme.Secondary : Theme.Accent, 26f);
                UIKit.Place(dot, UIKit.TopLeft, UIKit.Center, new Vector2(90f, -190f - i * 110f), new Vector2(26f, 26f));
                var t = Kit.Text(panel.transform, lines[i], 38, Theme.Ink, TextAnchor.MiddleLeft, FontStyle.Normal);
                UIKit.Place(t, UIKit.TopLeft, new Vector2(0f, 0.5f), new Vector2(130f, -190f - i * 110f), new Vector2(1010f, 100f));
            }
            var go = Kit.Button(panel.transform, "Los geht's!", Theme.Primary, () =>
            {
                Hide();
                _onClose?.Invoke();
            }, 54);
            UIKit.Place(go, UIKit.Bottom, UIKit.Bottom, new Vector2(0f, 40f), new Vector2(480f, 130f));
        }

        protected override void OnShow() => PopIn(_panel);
    }
}
