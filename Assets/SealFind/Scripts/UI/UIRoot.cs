using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SealFind
{
    /// <summary>Erzeugt Canvas + EventSystem und verwaltet alle Bildschirme sowie Toasts und schwebende Texte.</summary>
    public sealed class UIRoot : MonoBehaviour
    {
        public UIKit Kit { get; private set; }
        public Canvas Canvas { get; private set; }

        public MainMenuScreen Main { get; private set; }
        public WorldSelectScreen Worlds { get; private set; }
        public LevelSelectScreen Levels { get; private set; }
        public HudScreen Hud { get; private set; }
        public PauseScreen PauseMenu { get; private set; }
        public ResultsScreen Results { get; private set; }
        public CollectionScreen Collection { get; private set; }
        public SettingsScreen Settings { get; private set; }
        public TutorialScreen Tutorial { get; private set; }

        RectTransform _screens;
        RectTransform _toast;
        Text _toastText;
        CanvasGroup _toastGroup;
        Image _fade;

        public void Init(GameManager game, ArtLibrary art)
        {
            Kit = new UIKit(art);

            Canvas = gameObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.6f;
            gameObject.AddComponent<GraphicRaycaster>();

            if (FindAnyEventSystem() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(es);
            }

            _screens = Kit.Rect("Screens", transform);
            UIKit.Stretch(_screens);

            Hud = Create<HudScreen>(game);
            Main = Create<MainMenuScreen>(game);
            Worlds = Create<WorldSelectScreen>(game);
            Levels = Create<LevelSelectScreen>(game);
            Collection = Create<CollectionScreen>(game);
            PauseMenu = Create<PauseScreen>(game);
            Results = Create<ResultsScreen>(game);
            Settings = Create<SettingsScreen>(game);
            Tutorial = Create<TutorialScreen>(game);

            BuildToast();

            _fade = Kit.Image(transform, null, new Color(0.04f, 0.08f, 0.16f, 0f), "Fade");
            UIKit.Stretch(_fade.rectTransform);
            _fade.raycastTarget = false;
        }

        static EventSystem FindAnyEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            return FindAnyObjectByType<EventSystem>();
#else
            return FindObjectOfType<EventSystem>();
#endif
        }

        T Create<T>(GameManager game) where T : UIScreen, new()
        {
            var s = new T();
            s.Create(Kit, game, _screens);
            return s;
        }

        public void HideAll()
        {
            foreach (var s in new UIScreen[] { Main, Worlds, Levels, Hud, PauseMenu, Results, Collection, Settings, Tutorial })
                s.Hide(true);
        }

        public void ShowMain()
        {
            HideAll();
            Main.Show();
        }

        /// <summary>Kurzer Überblendeffekt beim Szenenwechsel.</summary>
        public void FadeTransition(System.Action midpoint)
        {
            _fade.raycastTarget = true;
            Tweener.Kill(_fade);
            Tweener.Run(_fade, 0.22f, k => _fade.color = new Color(0.04f, 0.08f, 0.16f, k), Ease.OutQuad, () =>
            {
                midpoint?.Invoke();
                Tweener.Run(_fade, 0.3f, k => _fade.color = new Color(0.04f, 0.08f, 0.16f, 1f - k), Ease.InQuad,
                    () => _fade.raycastTarget = false, 0.05f, true);
            }, 0f, true);
        }

        // ------------------------------------------------------------------ Toast

        void BuildToast()
        {
            var img = Kit.Panel(transform, new Color(0.05f, 0.1f, 0.2f, 0.88f), "Toast");
            img.raycastTarget = false;
            _toast = img.rectTransform;
            UIKit.Place(_toast, UIKit.Bottom, UIKit.Bottom, new Vector2(0f, 60f), new Vector2(1000f, 100f));
            _toastText = Kit.Text(_toast, "", 40, Color.white);
            UIKit.Stretch(_toastText.rectTransform, 30, 6, 30, 6);
            _toastGroup = _toast.gameObject.AddComponent<CanvasGroup>();
            _toastGroup.blocksRaycasts = false;
            _toast.gameObject.SetActive(false);
        }

        public void Toast(string message, float duration = 2.2f)
        {
            _toastText.text = message;
            float width = Mathf.Clamp(message.Length * 21f + 100f, 400f, 1500f);
            _toast.sizeDelta = new Vector2(width, 100f);
            _toast.gameObject.SetActive(true);
            _toast.SetAsLastSibling();
            Tweener.Kill(_toastGroup);
            Tweener.Kill(_toast);
            Tweener.Fade(_toastGroup, 0f, 1f, 0.2f);
            Tweener.Move(_toast, new Vector3(0f, 20f, 0f), new Vector3(0f, 60f, 0f), 0.3f, Ease.OutBack, null, 0f, true);
            Tweener.Run(_toastGroup, 0.3f, k => _toastGroup.alpha = 1f - k, Ease.InQuad, () => _toast.gameObject.SetActive(false), duration, true);
        }

        // ------------------------------------------------------------------ Schwebende Texte

        public void Floating(Camera cam, Vector2 worldPos, string text, Color color, int size = 52)
        {
            var layer = Hud.FloatLayer;
            Vector2 screen = cam.WorldToScreenPoint(worldPos);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out var local)) return;
            var t = Kit.Text(layer, text, size, color);
            UIKit.Outline(t, new Color(0f, 0f, 0f, 0.5f), 3f);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600f, 100f);
            Vector3 start = local + new Vector2(0f, 40f);
            rt.localPosition = start;
            Tweener.Scale(rt, Vector3.one * 0.5f, Vector3.one, 0.3f, Ease.OutBack, null, 0f, true);
            Tweener.Run(t, 1.1f, k =>
            {
                rt.localPosition = start + new Vector3(0f, 110f * k, 0f);
                var c = color;
                c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                t.color = c;
            }, Ease.OutQuad, () => Destroy(t.gameObject), 0f, true);
        }
    }
}
