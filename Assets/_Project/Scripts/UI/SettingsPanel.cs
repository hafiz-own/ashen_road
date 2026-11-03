using AshenRoad.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace AshenRoad.UI
{
    /// <summary>Music and effects volume, fullscreen, and resetting saved progress.</summary>
    public class SettingsPanel : UIPanel
    {
        Slider music;
        Slider sfx;
        Button fullscreen;
        Button reset;
        Button back;
        bool confirmReset;

        protected override void Build()
        {
            UIFactory.Overlay(transform, style);
            Text title = UIFactory.Text("Title", transform, style, "SETTINGS", 64, TextAnchor.MiddleCenter, style.accent);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1000f, 100f));

            Row("Music", 140f, out music, v => Apply());
            Row("Effects", 60f, out sfx, v => { Apply(); Core.GameEvents.RaiseSound("ui_move"); });

            RectTransform col = ButtonColumn(new Vector2(0f, -150f), 460f, 3);
            Vector2 size = new Vector2(460f, 64f);
            fullscreen = UIFactory.Button("Fullscreen", col, style, "", ToggleFullscreen, size);
            reset = UIFactory.Button("Reset", col, style, "Reset Progress", ResetProgress, size);
            back = UIFactory.Button("Back", col, style, "Back", () => flow.CloseSettings(), size);
            UIFactory.ChainVertical(music, sfx, fullscreen, reset, back);
            firstSelected = music;
        }

        void Row(string label, float y, out Slider slider, System.Action<float> changed)
        {
            Text t = UIFactory.Text(label + "Label", transform, style, label, 32, TextAnchor.MiddleRight);
            UIFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-250f, y), new Vector2(260f, 60f));
            slider = UIFactory.Slider(label + "Slider", transform, style, 0.7f, changed, new Vector2(440f, 50f));
            UIFactory.Place((RectTransform)slider.transform, new Vector2(0.5f, 0.5f), new Vector2(110f, y), new Vector2(440f, 50f));
        }

        protected override void OnShow()
        {
            confirmReset = false;
            if (Game != null)
            {
                music.SetValueWithoutNotify(Game.Save.MusicVolume);
                sfx.SetValueWithoutNotify(Game.Save.SfxVolume);
            }
            RefreshLabels();
        }

        void Apply()
        {
            if (Game == null) return;
            Game.Save.MusicVolume = music.value;
            Game.Save.SfxVolume = sfx.value;
            AudioManager.Instance?.SetVolumes(music.value, sfx.value);
        }

        void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            RefreshLabels(!Screen.fullScreen);
        }

        void ResetProgress()
        {
            if (!confirmReset)
            {
                confirmReset = true;
                RefreshLabels();
                return;
            }
            Game?.Save.ResetProgress();
            confirmReset = false;
            RefreshLabels();
        }

        void RefreshLabels(bool? fullscreenOverride = null)
        {
            bool fs = fullscreenOverride ?? Screen.fullScreen;
            fullscreen.GetComponentInChildren<Text>().text = "Fullscreen: " + (fs ? "On" : "Off");
            reset.GetComponentInChildren<Text>().text = confirmReset ? "Press again to confirm" : "Reset Progress";
        }
    }
}
