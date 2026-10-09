using UnityEngine;
using UnityEngine.UI;

namespace SealFind
{
    /// <summary>Basisklasse aller Bildschirme: Aufbau einmalig im Code, Ein-/Ausblenden mit kurzer Animation.</summary>
    public abstract class UIScreen
    {
        protected UIKit Kit;
        protected GameManager Game;
        public RectTransform Root { get; private set; }
        CanvasGroup _group;

        public bool IsVisible => Root != null && Root.gameObject.activeSelf && _group.blocksRaycasts;

        public void Create(UIKit kit, GameManager game, Transform parent)
        {
            Kit = kit;
            Game = game;
            Root = kit.Rect(GetType().Name, parent);
            UIKit.Stretch(Root);
            _group = Root.gameObject.AddComponent<CanvasGroup>();
            Build();
            Root.gameObject.SetActive(false);
        }

        protected abstract void Build();

        protected virtual void OnShow() { }

        public void Show()
        {
            Tweener.Kill(_group);
            Root.gameObject.SetActive(true);
            Root.SetAsLastSibling();
            _group.interactable = true;
            _group.blocksRaycasts = true;
            OnShow();
            Tweener.Fade(_group, 0f, 1f, 0.2f);
        }

        public void Hide(bool instant = false)
        {
            if (Root == null || !Root.gameObject.activeSelf) return;
            Tweener.Kill(_group);
            _group.interactable = false;
            _group.blocksRaycasts = false;
            if (instant)
            {
                Root.gameObject.SetActive(false);
                return;
            }
            Tweener.Fade(_group, _group.alpha, 0f, 0.15f, () => Root.gameObject.SetActive(false));
        }

        /// <summary>Panel springt beim Öffnen leicht auf.</summary>
        protected static void PopIn(Transform t, float delay = 0f) =>
            Tweener.Scale(t, Vector3.one * 0.85f, Vector3.one, 0.35f, Ease.OutBack, null, delay, true);

        /// <summary>Kopfzeile mit Zurück-Button und Titel.</summary>
        protected Text Header(string title, System.Action onBack)
        {
            var back = Kit.IconButton(Root, Kit.Art.BackIcon, Theme.Deep, onBack, 110f);
            UIKit.Place(back, UIKit.TopLeft, UIKit.TopLeft, new Vector2(40f, -36f), new Vector2(110f, 110f));
            var t = Kit.Text(Root, title, 72, Color.white);
            UIKit.Place(t, UIKit.Top, UIKit.Top, new Vector2(0f, -36f), new Vector2(1200f, 110f));
            UIKit.Outline(t, new Color(0f, 0f, 0f, 0.35f), 3f);
            return t;
        }

        protected Image Backdrop(Color top, Color bottom)
        {
            var bg = Kit.Image(Root, null, bottom, "Backdrop", true);
            UIKit.Stretch(bg.rectTransform);
            var upper = Kit.Image(bg.transform, null, top, "Top");
            upper.rectTransform.anchorMin = new Vector2(0f, 0.45f);
            upper.rectTransform.anchorMax = Vector2.one;
            upper.rectTransform.offsetMin = upper.rectTransform.offsetMax = Vector2.zero;
            return bg;
        }
    }

    /// <summary>Robben-Porträt aus den gleichen Sprite-Teilen wie in der Szene.</summary>
    public sealed class SealPortrait
    {
        public readonly RectTransform Root;
        readonly Image _body, _face, _acc;
        readonly ArtLibrary _art;

        public SealPortrait(UIKit kit, Transform parent, float width)
        {
            _art = kit.Art;
            Root = kit.Rect("Portrait", parent);
            Root.sizeDelta = new Vector2(width, width * SealRig.Height / SealRig.Width);
            _body = Part(kit, "Body");
            _face = Part(kit, "Face");
            _acc = Part(kit, "Accessory");
        }

        Image Part(UIKit kit, string name)
        {
            var img = kit.Image(Root, null, Color.white, name);
            UIKit.Stretch(img.rectTransform);
            img.preserveAspect = true;
            return img;
        }

        public void Set(SealSpecies species, bool discovered, SealFace face = SealFace.Open)
        {
            _body.sprite = _art.SealBody(species.IsBaby);
            _body.color = discovered ? (Color)species.BodyColor : new Color(0.16f, 0.2f, 0.3f, 0.85f);
            _face.sprite = _art.SealFaceSprite(species.IsBaby, face);
            _face.enabled = discovered;
            var acc = _art.SealAccessory(species.Accessory, species.IsBaby);
            _acc.sprite = acc;
            _acc.enabled = acc != null;
            _acc.color = discovered ? Color.white : new Color(0.16f, 0.2f, 0.3f, 0.85f);
        }
    }
}
