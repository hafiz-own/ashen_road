using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AshenRoad.UI
{
    /// <summary>Builds styled uGUI elements from code so every screen is consistent and lives in version control.</summary>
    public static class UIFactory
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        public static Text Text(string name, Transform parent, UIStyle style, string content, int size, TextAnchor align, Color? color = null)
        {
            RectTransform rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = style.font != null ? style.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = content;
            t.fontSize = size;
            t.alignment = align;
            t.color = color ?? style.text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        public static Image Panel(string name, Transform parent, UIStyle style, Vector2 size)
        {
            Image img = Image(name, parent, style.panel, Color.white);
            Place(img.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            img.raycastTarget = true;
            return img;
        }

        public static Image Overlay(Transform parent, UIStyle style)
        {
            Image img = Image("Overlay", parent, style.white, style.overlay);
            Stretch(img.rectTransform);
            img.raycastTarget = true;
            return img;
        }

        public static Button Button(string name, Transform parent, UIStyle style, string label, UnityAction onClick, Vector2 size)
        {
            Image img = Image(name, parent, style.button, Color.white);
            img.raycastTarget = true;
            img.rectTransform.sizeDelta = size;
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = style.buttonHighlight,
                selectedSprite = style.buttonHighlight,
                pressedSprite = style.buttonHighlight
            };
            if (onClick != null) button.onClick.AddListener(onClick);
            button.onClick.AddListener(() => Core.GameEvents.RaiseSound("ui_click"));
            Text t = Text("Label", img.transform, style, label, 26, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform, 4f);
            img.gameObject.AddComponent<ButtonSelectSound>();
            return button;
        }

        public static Slider Slider(string name, Transform parent, UIStyle style, float value, Action<float> onChanged, Vector2 size)
        {
            RectTransform root = Rect(name, parent);
            root.sizeDelta = size;
            Image bg = Image("Background", root, style.panel, new Color(1f, 1f, 1f, 0.9f));
            Stretch(bg.rectTransform);
            bg.rectTransform.offsetMin = new Vector2(0f, size.y * 0.3f);
            bg.rectTransform.offsetMax = new Vector2(0f, -size.y * 0.3f);
            RectTransform fillArea = Stretch(Rect("Fill Area", root));
            fillArea.offsetMin = new Vector2(6f, size.y * 0.35f);
            fillArea.offsetMax = new Vector2(-6f, -size.y * 0.35f);
            Image fill = Image("Fill", fillArea, style.white, style.accent);
            Stretch(fill.rectTransform);
            RectTransform handleArea = Stretch(Rect("Handle Slide Area", root));
            handleArea.offsetMin = new Vector2(10f, 0f);
            handleArea.offsetMax = new Vector2(-10f, 0f);
            Image handle = Image("Handle", handleArea, style.button, Color.white);
            handle.rectTransform.sizeDelta = new Vector2(22f, 0f);
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            handle.raycastTarget = true;

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            slider.transition = Selectable.Transition.SpriteSwap;
            slider.spriteState = new SpriteState { highlightedSprite = style.buttonHighlight, selectedSprite = style.buttonHighlight };
            if (onChanged != null) slider.onValueChanged.AddListener(v => onChanged(v));
            root.gameObject.AddComponent<ButtonSelectSound>();
            return slider;
        }

        public static VerticalLayoutGroup Column(RectTransform rt, float spacing)
        {
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        /// <summary>Links buttons top-to-bottom for keyboard/gamepad navigation (wrapping round).</summary>
        public static void ChainVertical(params Selectable[] items)
        {
            var list = new System.Collections.Generic.List<Selectable>();
            foreach (Selectable s in items) if (s != null) list.Add(s);
            for (int i = 0; i < list.Count; i++)
            {
                var nav = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = list[(i - 1 + list.Count) % list.Count],
                    selectOnDown = list[(i + 1) % list.Count]
                };
                list[i].navigation = nav;
            }
        }
    }
}
