using AshenRoad.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AshenRoad.UI
{
    /// <summary>Results after reaching the gate: time, best time, hearts left, undead slain; next, replay or menu.</summary>
    public class LevelCompletePanel : UIPanel
    {
        Text heading;
        Text stats;
        Button next, replay, menu;

        protected override void Build()
        {
            UIFactory.Overlay(transform, style);
            Text title = UIFactory.Text("Title", transform, style, "LEVEL COMPLETE", 80, TextAnchor.MiddleCenter, style.accent);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1400f, 120f));
            heading = UIFactory.Text("Level", transform, style, "", 36, TextAnchor.MiddleCenter, style.text);
            UIFactory.Place(heading.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(1400f, 60f));
            stats = UIFactory.Text("Stats", transform, style, "", 32, TextAnchor.UpperCenter, style.text);
            UIFactory.Place(stats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(900f, 200f));

            RectTransform col = ButtonColumn(new Vector2(0f, -170f), 460f, 3);
            Vector2 size = new Vector2(460f, 64f);
            next = UIFactory.Button("Next", col, style, "Next Level", () => Game?.NextLevel(), size);
            replay = UIFactory.Button("Replay", col, style, "Replay", () => Game?.RestartLevel(), size);
            menu = UIFactory.Button("Menu", col, style, "Main Menu", () => Game?.GoToMainMenu(), size);
            UIFactory.ChainVertical(next, replay, menu);
            firstSelected = next;
        }

        protected override void OnShow()
        {
            if (Game == null) return;
            LevelResult r = Game.LastResult;
            var level = Game.Catalog.Get(r.LevelIndex);
            heading.text = level != null ? $"{r.LevelIndex + 1}. {level.displayName}" : "";
            string best = r.NewBestTime ? "  <color=#ffb040>NEW BEST</color>" : "";
            stats.text =
                $"Time   {LevelResult.FormatTime(r.Time)}{best}\n" +
                $"Best   {LevelResult.FormatTime(r.BestTime)}\n" +
                $"Hearts   {r.HeartsLeft} / {r.MaxHearts}\n" +
                $"Undead slain   {r.EnemiesKilled}";
            bool hasNext = !Game.Catalog.IsLast(r.LevelIndex);
            next.gameObject.SetActive(hasNext);
            if (hasNext) UIFactory.ChainVertical(next, replay, menu);
            else UIFactory.ChainVertical(replay, menu);
            firstSelected = hasNext ? next : replay;
        }
    }
}
