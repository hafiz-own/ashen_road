using UnityEngine;
using UnityEngine.UI;

namespace AshenRoad.UI
{
    /// <summary>The end screen after the reunion: epilogue and credits.</summary>
    public class CreditsPanel : UIPanel
    {
        protected override void Build()
        {
            UIFactory.Overlay(transform, style).color = new Color(0.03f, 0.01f, 0.02f, 0.92f);
            Text title = UIFactory.Text("Title", transform, style, "THE ROAD IS ENDED", 84, TextAnchor.MiddleCenter, style.accent);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1500f, 120f));
            Text story = UIFactory.Text("Story", transform, style,
                "The Ash Warden is dust and the princess is free.\nBehind the knight the fires burn low, and the old royal road is quiet at last.",
                30, TextAnchor.MiddleCenter, style.text);
            UIFactory.Place(story.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(1500f, 110f));
            Text credits = UIFactory.Text("Credits", transform, style,
                "ASHEN ROAD  ·  EC-334 Game Design and Development\n\n" +
                "Hassan Ayoub  ·  Hafiz Own  ·  Abdul Basit\n\n" +
                "Art: Gothicvania by Luis Zuno (ansimuz), CC0\n" +
                "Fonts, items, sounds: Kenney (kenney.nl), CC0\n" +
                "Music: Juhani Junkala, Retro Game Music Pack, CC0\n" +
                "Built with Unity and C#",
                24, TextAnchor.MiddleCenter, style.textDim);
            UIFactory.Place(credits.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1500f, 300f));
            Button menu = UIFactory.Button("Menu", transform, style, "Main Menu", () => Game?.GoToMainMenu(), new Vector2(400f, 64f));
            UIFactory.Place((RectTransform)menu.transform, new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(400f, 64f));
            firstSelected = menu;
        }
    }
}
