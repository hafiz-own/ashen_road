using System.Collections.Generic;
using AshenRoad.Core;
using AshenRoad.Data;
using UnityEngine;
using UnityEngine.UI;

namespace AshenRoad.UI
{
    /// <summary>Lists every level with its best time; locked levels can't be chosen.</summary>
    public class LevelSelectPanel : UIPanel
    {
        readonly List<Button> buttons = new List<Button>();
        readonly List<Text> labels = new List<Text>();
        Button back;
        RectTransform grid;

        protected override void Build()
        {
            UIFactory.Overlay(transform, style);
            Text title = UIFactory.Text("Title", transform, style, "LEVEL SELECT", 64, TextAnchor.MiddleCenter, style.accent);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1200f, 100f));

            grid = UIFactory.Rect("Grid", transform);
            UIFactory.Place(grid, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1240f, 560f));
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(600f, 120f);
            layout.spacing = new Vector2(30f, 18f);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;

            back = UIFactory.Button("Back", transform, style, "Back", () => flow.BackToMainMenu(), new Vector2(320f, 64f));
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(320f, 64f));
        }

        protected override void OnShow()
        {
            LevelCatalog catalog = Game != null ? Game.Catalog : null;
            if (catalog == null) return;
            while (buttons.Count < catalog.Count)
            {
                int index = buttons.Count;
                Button b = UIFactory.Button("Level" + (index + 1), grid, style, "", () => Game.StartLevel(index), new Vector2(600f, 120f));
                buttons.Add(b);
                labels.Add(b.GetComponentInChildren<Text>());
            }
            for (int i = 0; i < catalog.Count; i++)
            {
                LevelData level = catalog.Get(i);
                bool unlocked = Game.Save.IsUnlocked(i);
                float best = Game.Save.GetBestTime(i);
                buttons[i].interactable = unlocked;
                labels[i].fontSize = 26;
                labels[i].text = unlocked
                    ? $"{i + 1}. {level.displayName}\n<size=20>{(best >= 0f ? "Best " + LevelResult.FormatTime(best) : level.subtitle)}</size>"
                    : $"{i + 1}. Locked";
                labels[i].color = unlocked ? style.text : style.textDim;
            }
            // keyboard navigation: grid order, then Back
            var nav = new List<Selectable>();
            for (int i = 0; i < catalog.Count; i++) if (buttons[i].interactable) nav.Add(buttons[i]);
            for (int i = 0; i < nav.Count; i++)
            {
                nav[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? nav[i - 1] : back,
                    selectOnRight = i < nav.Count - 1 ? nav[i + 1] : back,
                    selectOnUp = i >= 2 ? nav[i - 2] : back,
                    selectOnDown = i + 2 < nav.Count ? nav[i + 2] : back
                };
            }
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = nav.Count > 0 ? nav[nav.Count - 1] : null, selectOnDown = nav.Count > 0 ? nav[0] : null };
            firstSelected = nav.Count > 0 ? nav[0] : back;
        }
    }
}
