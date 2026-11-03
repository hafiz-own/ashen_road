using AshenRoad.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AshenRoad.UI
{
    /// <summary>Shown after a death: the cause, then retry from the checkpoint, restart, or menu.</summary>
    public class GameOverPanel : UIPanel
    {
        Text cause;
        Button retry, restart, menu;

        protected override void Build()
        {
            UIFactory.Overlay(transform, style);
            Text title = UIFactory.Text("Title", transform, style, "THE KNIGHT HAS FALLEN", 76, TextAnchor.MiddleCenter, style.danger);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1500f, 120f));
            cause = UIFactory.Text("Cause", transform, style, "", 34, TextAnchor.MiddleCenter, style.text);
            UIFactory.Place(cause.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(1400f, 60f));

            RectTransform col = ButtonColumn(new Vector2(0f, -80f), 460f, 3);
            Vector2 size = new Vector2(460f, 64f);
            retry = UIFactory.Button("Retry", col, style, "Retry", () => Game?.RetryFromCheckpoint(), size);
            restart = UIFactory.Button("Restart", col, style, "Restart Level", () => Game?.RestartLevel(), size);
            menu = UIFactory.Button("Menu", col, style, "Main Menu", () => Game?.GoToMainMenu(), size);
            UIFactory.ChainVertical(retry, restart, menu);
            firstSelected = retry;
        }

        protected override void OnShow()
        {
            if (Game == null) return;
            string front = Game.CurrentLevel != null ? Game.CurrentLevel.frontName : "front";
            cause.text = DeathText.For(Game.LastDeathCause, front);
            retry.GetComponentInChildren<Text>().text = Game.CheckpointIndex >= 0 ? "Retry from Checkpoint" : "Retry";
        }
    }
}
