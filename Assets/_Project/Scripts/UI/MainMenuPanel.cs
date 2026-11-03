using UnityEngine;
using UnityEngine.UI;

namespace AshenRoad.UI
{
    /// <summary>Title screen: continue, new game, level select, settings, quit.</summary>
    public class MainMenuPanel : UIPanel
    {
        Button continueButton;
        Button newGameButton;
        Button levelSelectButton;
        Button settingsButton;
        Button quitButton;

        protected override void Build()
        {
            Text title = UIFactory.Text("Title", transform, style, "ASHEN ROAD", 120, TextAnchor.MiddleCenter, style.accent);
            if (style.titleFont != null) title.font = style.titleFont;
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1400f, 160f));
            Text sub = UIFactory.Text("Subtitle", transform, style, "The Ashfall spreads. Reach the princess before the kingdom is lost.", 28, TextAnchor.MiddleCenter, style.textDim);
            UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(1400f, 60f));

            RectTransform col = ButtonColumn(new Vector2(0f, -90f), 420f, 5);
            Vector2 size = new Vector2(420f, 64f);
            continueButton = UIFactory.Button("Continue", col, style, "Continue", () => Game?.ContinueGame(), size);
            newGameButton = UIFactory.Button("NewGame", col, style, "New Game", () => Game?.StartNewGame(), size);
            levelSelectButton = UIFactory.Button("LevelSelect", col, style, "Level Select", () => flow.OpenLevelSelect(), size);
            settingsButton = UIFactory.Button("Settings", col, style, "Settings", () => flow.OpenSettings(), size);
            quitButton = UIFactory.Button("Quit", col, style, "Quit", () => Game?.QuitGame(), size);

            Text help = UIFactory.Text("Controls", transform, style,
                "A/D or Arrows  move     Space/W  jump     Shift  roll     J / 0 / Click  attack     Esc  pause",
                22, TextAnchor.MiddleCenter, style.textDim);
            UIFactory.Place(help.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1700f, 40f));
        }

        protected override void OnShow()
        {
            bool hasProgress = Game != null && (Game.Save.HighestUnlockedLevel > 0 || Game.Save.GameFinished);
            continueButton.gameObject.SetActive(hasProgress);
            if (hasProgress) UIFactory.ChainVertical(continueButton, newGameButton, levelSelectButton, settingsButton, quitButton);
            else UIFactory.ChainVertical(newGameButton, levelSelectButton, settingsButton, quitButton);
            firstSelected = hasProgress ? continueButton : newGameButton;
        }
    }
}
