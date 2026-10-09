using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SealFind
{
    /// <summary>Farbpalette des UI. An einer Stelle ändern = ganzes Spiel neu eingefärbt.</summary>
    public static class Theme
    {
        public static readonly Color Primary = new Color32(255, 122, 89, 255);
        public static readonly Color Secondary = new Color32(46, 196, 182, 255);
        public static readonly Color Accent = new Color32(255, 201, 60, 255);
        public static readonly Color Danger = new Color32(239, 71, 111, 255);
        public static readonly Color Success = new Color32(76, 200, 110, 255);
        public static readonly Color Ink = new Color32(43, 45, 66, 255);
        public static readonly Color Muted = new Color32(141, 153, 174, 255);
        public static readonly Color Panel = new Color32(255, 252, 246, 255);
        public static readonly Color PanelDark = new Color32(33, 47, 78, 235);
        public static readonly Color Dim = new Color(0.05f, 0.07f, 0.15f, 0.6f);
        public static readonly Color StarOff = new Color32(210, 214, 222, 255);
        public static readonly Color Deep = new Color32(23, 63, 107, 255);
    }

    /// <summary>
    /// Baukasten für das komplett im Code erzeugte uGUI. Keine Prefabs nötig –
    /// dadurch lässt sich das ganze Projekt ohne Editor-Klickerei nachvollziehen.
    /// </summary>
    public sealed class UIKit
    {
        public readonly Font Font;
        public readonly ArtLibrary Art;
        public static Action ClickSound;

        public UIKit(ArtLibrary art)
        {
            Art = art;
#if UNITY_2022_2_OR_NEWER
            Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            Font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        // ------------------------------------------------------------------ Layout-Helfer

        public RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Place(Component c, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size) =>
            Place((RectTransform)c.transform, anchor, pivot, pos, size);

        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 Top = new Vector2(0.5f, 1f);
        public static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        // ------------------------------------------------------------------ Elemente

        public Image Image(Transform parent, Sprite sprite, Color color, string name = "Image", bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border.sqrMagnitude > 0f) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        public Image Panel(Transform parent, Color color, string name = "Panel")
        {
            var img = Image(parent, Art.RoundedRect, color, name, true);
            return img;
        }

        public Image Icon(Transform parent, Sprite sprite, Color color, float size, string name = "Icon")
        {
            var img = Image(parent, sprite, color, name);
            img.preserveAspect = true;
            ((RectTransform)img.transform).sizeDelta = new Vector2(size, size);
            return img;
        }

        public Text Text(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter,
            FontStyle style = FontStyle.Bold, string name = "Text")
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        public static T Outline<T>(T graphic, Color color, float distance = 3f) where T : Graphic
        {
            var o = graphic.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(distance, -distance);
            return graphic;
        }

        public static T Shadow<T>(T graphic, float distance = 4f, float alpha = 0.25f) where T : Graphic
        {
            var s = graphic.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, alpha);
            s.effectDistance = new Vector2(0f, -distance);
            return graphic;
        }

        public Button Button(Transform parent, string label, Color color, Action onClick, int fontSize = 46, Color? textColor = null)
        {
            var bg = Panel(parent, color, "Button_" + label);
            Shadow(bg, 6f, 0.22f);
            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.onClick.AddListener(() =>
            {
                ClickSound?.Invoke();
                onClick?.Invoke();
            });
            bg.gameObject.AddComponent<ButtonFeedback>();

            if (!string.IsNullOrEmpty(label))
            {
                var t = Text(bg.transform, label, fontSize, textColor ?? Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, "Label");
                Stretch(t.rectTransform, 16, 6, 16, 6);
                Shadow(t, 2f, 0.2f);
            }
            return btn;
        }

        public Button IconButton(Transform parent, Sprite icon, Color color, Action onClick, float size = 120f, float iconScale = 0.55f)
        {
            var btn = Button(parent, null, color, onClick);
            ((RectTransform)btn.transform).sizeDelta = new Vector2(size, size);
            var img = Icon(btn.transform, icon, Color.white, size * iconScale);
            Place(img, Center, Center, Vector2.zero, new Vector2(size * iconScale, size * iconScale));
            return btn;
        }

        public Image Dim(Transform parent)
        {
            var img = Image(parent, null, Theme.Dim, "Dim", true);
            Stretch(img.rectTransform);
            return img;
        }

        public Image Badge(Transform parent, string text, Color color, float size = 54f)
        {
            var b = Image(parent, Art.Circle, color, "Badge");
            b.rectTransform.sizeDelta = new Vector2(size, size);
            var t = Text(b.transform, text, Mathf.RoundToInt(size * 0.55f), Color.white);
            Stretch(t.rectTransform);
            return b;
        }

        public void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        /// <summary>ScrollRect mit Viewport-Maske; gibt den Content-Container zurück.</summary>
        public RectTransform Scroll(Transform parent, bool horizontal, out ScrollRect scroll)
        {
            var root = Rect("Scroll", parent);
            scroll = root.gameObject.AddComponent<ScrollRect>();
            var bgImg = root.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0);
            var viewport = Rect("Viewport", root);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport);
            if (horizontal)
            {
                content.anchorMin = new Vector2(0f, 0f);
                content.anchorMax = new Vector2(0f, 1f);
                content.pivot = new Vector2(0f, 0.5f);
            }
            else
            {
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
            }
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = horizontal;
            scroll.vertical = !horizontal;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            if (horizontal) fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            else fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return content;
        }

        public RectTransform StarRow(Transform parent, int stars, float size, float spacing = 8f)
        {
            var row = Rect("Stars", parent);
            row.sizeDelta = new Vector2(size * 3 + spacing * 2, size);
            for (int i = 0; i < 3; i++)
            {
                var s = Icon(row, Art.Star, i < stars ? Theme.Accent : Theme.StarOff, size, "Star" + i);
                Place(s, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * (size + spacing), i == 1 ? size * 0.12f : 0f), new Vector2(size, size));
            }
            return row;
        }

        public static string FormatTime(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int m = (int)(seconds / 60f);
            float s = seconds - m * 60;
            return m > 0 ? $"{m}:{Mathf.FloorToInt(s):00}" : $"{s:0.0}s";
        }
    }

    /// <summary>Buttons federn beim Drücken leicht ein.</summary>
    public sealed class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Vector3 _base = Vector3.one;
        bool _pressed;

        void Awake() => _base = transform.localScale;

        public void OnPointerDown(PointerEventData e)
        {
            _pressed = true;
            Tweener.Kill(transform);
            Tweener.Scale(transform, transform.localScale, _base * 0.93f, 0.08f, Ease.OutQuad, null, 0f, true);
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        void Release()
        {
            if (!_pressed) return;
            _pressed = false;
            Tweener.Kill(transform);
            Tweener.Scale(transform, transform.localScale, _base, 0.25f, Ease.OutBack, null, 0f, true);
        }
    }

    /// <summary>Hält Inhalte aus Notch- und Rundungsbereichen von Smartphones heraus.</summary>
    public sealed class SafeArea : MonoBehaviour
    {
        Rect _last;

        void Update()
        {
            var area = Screen.safeArea;
            if (area == _last || Screen.width <= 0 || Screen.height <= 0) return;
            _last = area;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rt.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>Langsames Auf-und-ab, z.B. für das Maskottchen im Menü.</summary>
    public sealed class Bobbing : MonoBehaviour
    {
        public float Amplitude = 12f;
        public float Speed = 1.6f;
        Vector2 _base;
        float _t;

        void Start() => _base = ((RectTransform)transform).anchoredPosition;

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            ((RectTransform)transform).anchoredPosition = _base + new Vector2(0f, Mathf.Sin(_t * Speed) * Amplitude);
        }
    }
}
