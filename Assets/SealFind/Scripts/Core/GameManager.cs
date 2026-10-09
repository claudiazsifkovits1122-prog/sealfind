using System;
using System.Collections.Generic;
using UnityEngine;

namespace SealFind
{
    /// <summary>
    /// Zentrale Steuerung: erzeugt alle Systeme, startet Level, verdrahtet Spielregeln mit UI/Sound/Effekten
    /// und speichert den Fortschritt.
    ///
    /// Core Loop: Szene erscheint → suchen → finden → Belohnung → "Weiter" → nächstes Level.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public SaveData Save { get; private set; }
        public ArtLibrary Art { get; private set; }
        public AudioManager Audio { get; private set; }
        public FxSystem Fx { get; private set; }
        public UIRoot UI { get; private set; }
        public CameraController Cam { get; private set; }
        public PointerInput Pointer { get; private set; }
        public LevelDefinition CurrentLevel { get; private set; }

        LevelSession _session;
        BuiltScene _scene;
        Transform _world;
        readonly List<SealSpecies> _newSpecies = new List<SealSpecies>();
        bool _paused;

        static int Today => DateKey(DateTime.Today);
        static int DateKey(DateTime d) => d.Year * 10000 + d.Month * 100 + d.Day;

        public bool DailyPlayedToday => Save.LastDailyDate == Today;

        // ================================================================== Setup

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            Input.multiTouchEnabled = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;

            Save = SaveSystem.Load();
            Art = new ArtLibrary();

            var cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            DontDestroyOnLoad(cam.gameObject);
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            Cam = cam.GetComponent<CameraController>();
            if (Cam == null) Cam = cam.gameObject.AddComponent<CameraController>();

            Audio = Child("Audio").AddComponent<AudioManager>();
            Fx = Child("Fx").AddComponent<FxSystem>();
            Fx.Init(Art);
            _world = Child("World").transform;

            Pointer = gameObject.AddComponent<PointerInput>();
            Pointer.Cam = Cam;
            Pointer.Tapped += OnTap;

            UI = Child("UI").AddComponent<UIRoot>();
            UI.Init(this, Art);
            UIKit.ClickSound = () => Audio.Play(Sfx.Click, 0.5f);

            ApplySettings();
            Audio.Warmup();
        }

        GameObject Child(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go;
        }

        void Start()
        {
            ShowBackdrop();
            UI.ShowMain();
        }

        public void ApplySettings()
        {
            Audio.SoundOn = Save.SoundOn;
            Audio.AmbienceOn = Save.AmbienceOn;
            Haptics.Enabled = Save.VibrationOn;
        }

        public void ResetProgress()
        {
            var fresh = new SaveData
            {
                SoundOn = Save.SoundOn,
                AmbienceOn = Save.AmbienceOn,
                VibrationOn = Save.VibrationOn,
            };
            Save = fresh;
            SaveSystem.Save(Save);
        }

        void Update()
        {
            if (_session != null && UI.Hud.IsVisible) UI.Hud.Tick(_session);

#if UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE
            // Android-Zurücktaste / Escape
            if (Input.GetKeyDown(KeyCode.Escape)) OnBack();
#endif
        }

        void OnBack()
        {
            if (UI.Settings.IsVisible) UI.Settings.Hide();
            else if (_paused) Resume();
            else if (_session != null && _session.Running) Pause();
            else if (UI.Levels.IsVisible) UI.Worlds.Show();
            else if (UI.Worlds.IsVisible || UI.Collection.IsVisible) UI.ShowMain();
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            if (_session != null && _session.Running && !_paused) Pause();
            SaveSystem.Save(Save);
        }

        void OnApplicationQuit() => SaveSystem.Save(Save);

        // ================================================================== Navigation

        public void GoToMenu()
        {
            SetPaused(false);
            UI.FadeTransition(() =>
            {
                ShowBackdrop();
                UI.ShowMain();
            });
        }

        public void NextCampaignLevel(out int world, out int level, out bool campaignDone)
        {
            for (int w = 0; w < ContentDatabase.Worlds.Count; w++)
            {
                if (!Save.IsWorldUnlocked(w)) break;
                for (int l = 0; l < ContentDatabase.LevelsPerWorld; l++)
                {
                    if (Save.StarsFor("w" + w + "-l" + l) == 0)
                    {
                        world = w;
                        level = l;
                        campaignDone = false;
                        return;
                    }
                }
            }
            world = 0;
            level = 0;
            campaignDone = true;
        }

        int HighestUnlockedWorld()
        {
            int best = 0;
            for (int w = 0; w < ContentDatabase.Worlds.Count; w++)
                if (Save.IsWorldUnlocked(w)) best = w;
            return best;
        }

        public void PlayContinue()
        {
            NextCampaignLevel(out int w, out int l, out bool done);
            if (done) PlayQuick();
            else PlayLevel(w, l);
        }

        public void PlayLevel(int world, int level) => Play(ContentDatabase.BuildLevel(world, level));

        public void PlayDaily() => Play(ContentDatabase.BuildDaily(DateTime.Today));

        public void PlayQuick() => Play(ContentDatabase.BuildQuick(Environment.TickCount, HighestUnlockedWorld()));

        public void Retry()
        {
            if (CurrentLevel != null) Play(CurrentLevel);
        }

        public void PlayNext()
        {
            var cur = CurrentLevel;
            if (cur == null || cur.Mode != GameMode.Campaign)
            {
                PlayQuick();
                return;
            }
            int w = cur.World, l = cur.Level + 1;
            if (l >= ContentDatabase.LevelsPerWorld)
            {
                w++;
                l = 0;
            }
            if (w < ContentDatabase.Worlds.Count && Save.IsLevelUnlocked(w, l))
            {
                if (l == 0) UI.Toast($"Neue Welt freigeschaltet: {ContentDatabase.Worlds[w].Name}!", 3f);
                PlayLevel(w, l);
            }
            else PlayContinue();
        }

        // ================================================================== Level

        void ClearScene()
        {
            if (_session != null)
            {
                _session.Abort();
                Destroy(_session);
                _session = null;
            }
            _scene?.Destroy();
            _scene = null;
            Fx.Clear();
            Pointer.Enabled = false;
            Cam.AttractMode = false;
        }

        void ShowBackdrop()
        {
            ClearScene();
            CurrentLevel = null;
            var def = ContentDatabase.BuildBackdrop(UnityEngine.Random.Range(1, 99999));
            var world = ContentDatabase.Worlds[def.World];
            var plan = SceneGenerator.Generate(world, def, ContentDatabase.Species);
            _scene = SceneBuilder.Build(plan, world, ContentDatabase.Species, Art, Fx, Audio, _world);
            Cam.Setup(plan.Bounds);
            Cam.AttractMode = true;
            Audio.SetAmbience(world);
        }

        void Play(LevelDefinition def)
        {
            SetPaused(false);
            UI.FadeTransition(() => StartLevel(def));
        }

        void StartLevel(LevelDefinition def)
        {
            ClearScene();
            CurrentLevel = def;
            _newSpecies.Clear();

            var world = ContentDatabase.Worlds[def.World];
            var plan = SceneGenerator.Generate(world, def, ContentDatabase.Species);
            _scene = SceneBuilder.Build(plan, world, ContentDatabase.Species, Art, Fx, Audio, _world);
            Cam.Setup(plan.Bounds);
            Audio.SetAmbience(world);

            _session = gameObject.AddComponent<LevelSession>();
            _session.Setup(def, _scene);
            _session.SealFound += OnSealFound;
            _session.Missed += OnMissed;
            _session.Tricked += OnTricked;
            _session.Whispered += OnWhisper;
            _session.Ended += OnEnded;

            UI.HideAll();
            UI.Hud.Show();
            UI.Hud.Bind(_session);

            string title = def.Mode == GameMode.Daily ? "Tages-Challenge"
                : def.Mode == GameMode.Quick ? "Zufallsrunde"
                : $"{world.Name} · Level {def.Level + 1}";

            if (!Save.TutorialSeen && def.Mode == GameMode.Campaign)
            {
                UI.Tutorial.Open(() =>
                {
                    Save.TutorialSeen = true;
                    SaveSystem.Save(Save);
                    BeginWithBanner(title, def);
                });
            }
            else BeginWithBanner(title, def);
        }

        void BeginWithBanner(string title, LevelDefinition def)
        {
            UI.Hud.ShowBanner(ContentDatabase.GoalText(def), title, 1.5f);
            Audio.Play(Sfx.Whoosh, 0.6f);
            var session = _session;
            Tweener.Delay(session, 1.3f, () =>
            {
                if (session == null || session != _session) return;
                session.Begin();
                Pointer.Enabled = true;
            });
        }

        public void Pause()
        {
            if (_session == null || !_session.Running) return;
            SetPaused(true);
            UI.PauseMenu.Show();
        }

        public void Resume()
        {
            SetPaused(false);
            UI.PauseMenu.Hide();
        }

        void SetPaused(bool paused)
        {
            _paused = paused;
            Time.timeScale = paused ? 0f : 1f;
            Pointer.Enabled = !paused && _session != null && _session.Running;
        }

        // ================================================================== Eingabe

        void OnTap(Vector2 screenPos)
        {
            if (_session == null || !_session.Running || _paused) return;
            var world = Cam.ScreenToWorld(screenPos);
            // Fingerfreundliche Toleranz: ca. 2,5 mm um den Tippunkt
            float tolerancePx = Mathf.Max(18f, (Screen.dpi > 0 ? Screen.dpi : 160f) * 0.1f);
            _session.HandleTap(world, tolerancePx * Cam.WorldPerPixel);
        }

        public void UseHint()
        {
            if (_session == null || !_session.Running) return;
            if (_session.HintsLeft <= 0)
            {
                UI.Toast("Keine Hinweise mehr übrig!");
                return;
            }
            var seal = _session.UseHint(Cam.transform.position);
            if (seal == null) return;

            Audio.Play(Sfx.Hint);
            UI.Hud.UpdateHints(_session.HintsLeft);
            UI.Hud.OnPenalty();
            var center = seal.CenterWorld + UnityEngine.Random.insideUnitCircle * 0.7f;
            Cam.FocusOn(center, Cam.Cam.orthographicSize * 0.75f, 0.7f);
            Fx.Pulse(center, new Color(1f, 0.95f, 0.4f), 1.8f, 1.1f);
            Tweener.Delay(this, 0.45f, () => Fx.Pulse(center, new Color(1f, 0.95f, 0.4f), 1.8f, 1.1f));
            UI.Toast($"Sie ist ziemlich nah ... (+{ScoreCalculator.HintPenaltySeconds:0}s)");
        }

        // ================================================================== Spielereignisse

        void OnSealFound(Seal seal, int points, int combo)
        {
            var sp = seal.Species;
            bool rare = sp.Rarity >= SealRarity.Rare;
            Audio.Play(rare ? Sfx.FoundRare : Sfx.Found, 1f, 1f + (combo - 1) * 0.08f);
            Fx.Burst(seal.CenterWorld, rare ? 24 : 14, rare ? 5f : 3.5f, 0.2f * Mathf.Max(0.6f, seal.Placement.Scale));

            string text = "+" + points;
            var color = Color.white;
            if (!seal.IsTarget && !seal.IsBonus)
            {
                text = CurrentLevel.Goal == GoalType.FindBabies ? "Keine Babyrobbe!" : $"Ohne {ContentDatabase.AccessoryName(CurrentLevel.GoalAccessory)}!";
                color = new Color(0.8f, 0.9f, 1f);
            }
            UI.Floating(Cam.Cam, seal.CenterWorld, text, color);
            UI.Hud.OnFound(seal, _session, combo);

            Save.TotalSealsFound++;
            if (Save.AddSpecies(sp.Id))
            {
                _newSpecies.Add(sp);
                Tweener.Delay(this, 0.35f, () => Audio.Play(Sfx.NewSpecies, 0.8f));
                UI.Toast($"Neue Robbe entdeckt: {sp.Name}  ({RarityInfo.Label(sp.Rarity)})", 2.6f);
            }
            else if (seal.IsBonus)
            {
                UI.Toast($"Legendär! {sp.Name} gefunden!", 2.6f);
            }
        }

        void OnMissed(Vector2 pos, bool decoy, float penalty)
        {
            Audio.Play(Sfx.Miss, 0.7f);
            Fx.MissMark(pos);
            UI.Floating(Cam.Cam, pos, (decoy ? "Nur ein Stein! " : "") + $"+{penalty:0}s", new Color(1f, 0.45f, 0.45f), 46);
            UI.Hud.OnPenalty();
            Haptics.Buzz();
        }

        void OnTricked(Seal seal)
        {
            UI.Floating(Cam.Cam, seal.CenterWorld, "Hihi! Fast!", new Color(1f, 0.9f, 0.5f));
        }

        void OnWhisper(Seal seal)
        {
            Audio.Play(Sfx.Psst, 0.9f);
            Vector2 view = Cam.transform.position;
            float h = Cam.Cam.orthographicSize, w = h * Cam.Cam.aspect;
            var d = seal.CenterWorld - view;
            string where;
            if (Mathf.Abs(d.x) < w * 0.8f && Mathf.Abs(d.y) < h * 0.8f) where = "ganz in der Nähe!";
            else if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) where = d.x < 0 ? "weiter links ..." : "weiter rechts ...";
            else where = d.y < 0 ? "weiter unten ..." : "weiter oben ...";
            UI.Toast("psst ... " + where);
        }

        void OnEnded(LevelResult result)
        {
            Pointer.Enabled = false;
            var def = result.Level;
            Save.TotalPlaySeconds += Mathf.RoundToInt(result.Time);

            if (result.Success)
            {
                switch (def.Mode)
                {
                    case GameMode.Campaign:
                        result.NewRecord = Save.RecordLevel(def.Key, result.Stars, result.Time, result.Score);
                        break;
                    case GameMode.Daily:
                    {
                        int yesterday = DateKey(DateTime.Today.AddDays(-1));
                        if (Save.LastDailyDate != Today)
                        {
                            Save.DailyStreak = Save.LastDailyDate == yesterday ? Save.DailyStreak + 1 : 1;
                            Save.LastDailyDate = Today;
                            Save.DailyBestTime = result.Time;
                            result.NewRecord = true;
                        }
                        else if (result.Time < Save.DailyBestTime)
                        {
                            Save.DailyBestTime = result.Time;
                            result.NewRecord = true;
                        }
                        break;
                    }
                    case GameMode.Quick:
                        Save.QuickRoundsPlayed++;
                        break;
                }
            }
            result.NewSpecies.AddRange(_newSpecies);
            SaveSystem.Save(Save);

            if (result.Success)
            {
                Audio.Play(Sfx.Win);
                Haptics.Buzz();
                Cam.ZoomOutFull(0.9f);
                Tweener.Delay(this, 0.5f, () => Fx.Confetti(Cam.Cam));
                int i = 0;
                foreach (var s in _scene.Seals) if (s.IsFound) s.Celebrate(0.4f + 0.06f * i++);
                UI.Hud.ShowBanner(result.Stars == 3 ? "Perfekt!" : "Alle gefunden!", "", 1.4f);
            }
            else
            {
                Audio.Play(Sfx.Lose);
                Cam.ZoomOutFull(0.9f);
                UI.Hud.ShowBanner("Zeit abgelaufen!", "Hier haben sie sich versteckt ...", 1.6f);
            }

            Tweener.Delay(this, result.Success ? 1.8f : 2.4f, () =>
            {
                if (CurrentLevel != def) return;
                UI.Results.Open(result);
            });
        }
    }
}
