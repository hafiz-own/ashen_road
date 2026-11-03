using AshenRoad.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AshenRoad.UI
{
    /// <summary>
    /// In-game overlay: hearts, level name and timer, how close the disaster front is, hints, and the boss bar.
    /// Everything is driven by GameEvents; the HUD never looks up gameplay objects.
    /// </summary>
    public class HUDController : UIPanel
    {
        HeartDisplay hearts;
        Text levelName;
        Text timer;
        Text frontLabel;
        Image frontFill;
        Text hint;
        CanvasGroup hintGroup;
        RectTransform bossBar;
        Image bossFill;
        Text bossName;
        float levelStart;
        float hintUntil;
        bool timing;

        public string HintText => hint != null ? hint.text : "";
        public int HeartsShown => hearts != null ? hearts.Filled : 0;
        public bool BossBarVisible => bossBar != null && bossBar.gameObject.activeSelf;

        protected override void Build()
        {
            RectTransform heartRow = UIFactory.Rect("Hearts", transform);
            UIFactory.Place(heartRow, new Vector2(0f, 1f), new Vector2(40f, -36f), new Vector2(400f, 60f));
            hearts = heartRow.gameObject.AddComponent<HeartDisplay>();
            hearts.Init(style);

            levelName = UIFactory.Text("LevelName", transform, style, "", 30, TextAnchor.MiddleCenter);
            UIFactory.Place(levelName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(900f, 44f));
            timer = UIFactory.Text("Timer", transform, style, "00:00.00", 26, TextAnchor.MiddleCenter, style.textDim);
            UIFactory.Place(timer.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(400f, 36f));

            // disaster front distance meter
            frontLabel = UIFactory.Text("FrontLabel", transform, style, "", 24, TextAnchor.MiddleRight);
            UIFactory.Place(frontLabel.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(500f, 36f));
            Image frontBack = UIFactory.Image("FrontBar", transform, style.white, new Color(0f, 0f, 0f, 0.55f));
            UIFactory.Place(frontBack.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -72f), new Vector2(320f, 18f));
            frontFill = UIFactory.Image("Fill", frontBack.transform, style.white, style.danger);
            frontFill.type = Image.Type.Filled;
            frontFill.fillMethod = Image.FillMethod.Horizontal;
            frontFill.fillOrigin = 1;
            UIFactory.Stretch(frontFill.rectTransform, 2f);

            hint = UIFactory.Text("Hint", transform, style, "", 34, TextAnchor.MiddleCenter, style.text);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1500f, 60f));
            hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            hintGroup.alpha = 0f;

            bossBar = UIFactory.Rect("BossBar", transform);
            UIFactory.Place(bossBar, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(900f, 60f));
            bossName = UIFactory.Text("BossName", bossBar, style, "", 26, TextAnchor.MiddleCenter, style.accent);
            UIFactory.Place(bossName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 10f), new Vector2(900f, 34f));
            Image bossBack = UIFactory.Image("Back", bossBar, style.white, new Color(0f, 0f, 0f, 0.6f));
            UIFactory.Place(bossBack.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(900f, 22f));
            bossFill = UIFactory.Image("Fill", bossBack.transform, style.white, new Color(0.8f, 0.15f, 0.2f));
            bossFill.type = Image.Type.Filled;
            bossFill.fillMethod = Image.FillMethod.Horizontal;
            UIFactory.Stretch(bossFill.rectTransform, 3f);
            bossBar.gameObject.SetActive(false);
        }

        // Subscribed for the whole lifetime (not OnEnable) so events raised while the HUD is hidden still land.
        void Awake()
        {
            GameEvents.LevelStarted += OnLevelStarted;
            GameEvents.FrontDistanceChanged += OnFrontDistance;
            GameEvents.HintRequested += ShowHint;
            GameEvents.BossIntroStarted += OnBossIntro;
            GameEvents.BossHealthChanged += OnBossHealth;
            GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.ExitReached += StopTimer;
            GameEvents.PlayerDied += OnPlayerDied;
        }

        void OnDestroy()
        {
            GameEvents.LevelStarted -= OnLevelStarted;
            GameEvents.FrontDistanceChanged -= OnFrontDistance;
            GameEvents.HintRequested -= ShowHint;
            GameEvents.BossIntroStarted -= OnBossIntro;
            GameEvents.BossHealthChanged -= OnBossHealth;
            GameEvents.BossDefeated -= OnBossDefeated;
            GameEvents.ExitReached -= StopTimer;
            GameEvents.PlayerDied -= OnPlayerDied;
        }

        void OnLevelStarted(int index)
        {
            levelStart = Time.time;
            timing = true;
            var level = Game != null ? Game.CurrentLevel : null;
            levelName.text = level != null ? $"{index + 1}. {level.displayName}" : "";
            bossBar.gameObject.SetActive(false);
            frontLabel.text = "";
        }

        void StopTimer() => timing = false;

        void OnPlayerDied(DeathCause _) => timing = false;

        void OnFrontDistance(float gap)
        {
            string name = Game != null && Game.CurrentLevel != null ? Game.CurrentLevel.frontName : "front";
            bool safe = gap > 30f;
            frontLabel.text = safe ? name.ToUpperInvariant() + "  far behind" : $"{name.ToUpperInvariant()}  {Mathf.Max(0f, gap):0} m";
            frontLabel.color = gap < 6f ? style.danger : style.text;
            frontFill.fillAmount = 1f - Mathf.Clamp01(gap / 30f);
        }

        public void ShowHint(string text)
        {
            if (hint == null) return;
            hint.text = text;
            hintUntil = Time.unscaledTime + 3.5f;
        }

        void OnBossIntro()
        {
            bossBar.gameObject.SetActive(true);
            bossName.text = "THE ASH WARDEN";
        }

        void OnBossHealth(int current, int max)
        {
            bossFill.fillAmount = max > 0 ? (float)current / max : 0f;
        }

        void OnBossDefeated() => bossBar.gameObject.SetActive(false);

        void Update()
        {
            if (timing) timer.text = LevelResult.FormatTime(Time.time - levelStart);
            float target = Time.unscaledTime < hintUntil ? 1f : 0f;
            hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, target, Time.unscaledDeltaTime * 3f);
        }
    }
}
