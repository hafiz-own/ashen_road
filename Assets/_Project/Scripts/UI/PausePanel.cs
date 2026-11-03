using UnityEngine;
using UnityEngine.UI;

namespace AshenRoad.UI
{
    /// <summary>Pause menu: resume, retry from the checkpoint, restart, settings, main menu.</summary>
    public class PausePanel : UIPanel
    {
        Button resume, retry, restart, settings, menu;

        protected override void Build()
        {
            UIFactory.Overlay(transform, style);
            Text title = UIFactory.Text("Title", transform, style, "PAUSED", 80, TextAnchor.MiddleCenter, style.accent);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(900f, 120f));

            RectTransform col = ButtonColumn(new Vector2(0f, -40f), 460f, 5);
            Vector2 size = new Vector2(460f, 64f);
            resume = UIFactory.Button("Resume", col, style, "Resume", () => Game?.Resume(), size);
            retry = UIFactory.Button("Retry", col, style, "Retry from Checkpoint", () => Game?.RetryFromCheckpoint(), size);
            restart = UIFactory.Button("Restart", col, style, "Restart Level", () => Game?.RestartLevel(), size);
            settings = UIFactory.Button("Settings", col, style, "Settings", () => flow.OpenSettings(), size);
            menu = UIFactory.Button("Menu", col, style, "Main Menu", () => Game?.GoToMainMenu(), size);
            firstSelected = resume;
        }

        protected override void OnShow()
        {
            bool hasCheckpoint = Game != null && Game.CheckpointIndex >= 0;
            retry.gameObject.SetActive(hasCheckpoint);
            if (hasCheckpoint) UIFactory.ChainVertical(resume, retry, restart, settings, menu);
            else UIFactory.ChainVertical(resume, restart, settings, menu);
        }
    }
}
