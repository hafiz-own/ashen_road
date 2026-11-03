using System.Collections.Generic;
using AshenRoad.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AshenRoad.UI
{
    /// <summary>Row of hearts bound to the knight's health through GameEvents; lost and gained hearts pop.</summary>
    public class HeartDisplay : MonoBehaviour
    {
        readonly List<Image> hearts = new List<Image>();
        readonly List<float> popUntil = new List<float>();
        UIStyle style;
        int shown = -1;

        public int Filled { get; private set; }
        public int Max => hearts.Count;

        public void Init(UIStyle uiStyle) => style = uiStyle;

        // Lifetime subscription: the knight reports its health before the HUD is shown.
        void Awake() => GameEvents.PlayerHealthChanged += Set;

        void OnDestroy() => GameEvents.PlayerHealthChanged -= Set;

        public void Set(int current, int max)
        {
            while (hearts.Count < max)
            {
                Image img = UIFactory.Image("Heart" + hearts.Count, transform, style.heartFull, Color.white);
                UIFactory.Place(img.rectTransform, new Vector2(0f, 1f), new Vector2(hearts.Count * 70f, 0f), new Vector2(60f, 52f));
                img.preserveAspect = true;
                hearts.Add(img);
                popUntil.Add(0f);
            }
            for (int i = 0; i < hearts.Count; i++)
            {
                bool active = i < max;
                hearts[i].gameObject.SetActive(active);
                bool full = i < current;
                if (shown >= 0 && full != (i < shown)) popUntil[i] = Time.unscaledTime + 0.25f;
                hearts[i].sprite = full ? style.heartFull : style.heartEmpty;
            }
            Filled = current;
            shown = current;
        }

        void Update()
        {
            for (int i = 0; i < hearts.Count; i++)
            {
                float t = Mathf.Clamp01((popUntil[i] - Time.unscaledTime) / 0.25f);
                hearts[i].rectTransform.localScale = Vector3.one * (1f + 0.4f * t);
            }
        }
    }
}
