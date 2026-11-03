using AshenRoad.Core;
using AshenRoad.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace AshenRoad.UI
{
    /// <summary>
    /// Shows exactly the screens that belong to the current game state (Observer on GameStateChanged), and handles
    /// "back" from sub-menus. Screens are built from code at start-up.
    /// </summary>
    public class UIFlowController : MonoBehaviour
    {
        [SerializeField] UIStyle style;
        [SerializeField] InputReader input;
        [SerializeField] InputSystemUIInputModule inputModule;
        [SerializeField] RectTransform panelRoot;

        GameState state = GameState.Boot;
        bool settingsOpen;
        bool levelSelectOpen;

        public MainMenuPanel MainMenu { get; private set; }
        public LevelSelectPanel LevelSelect { get; private set; }
        public SettingsPanel Settings { get; private set; }
        public HUDController Hud { get; private set; }
        public PausePanel Pause { get; private set; }
        public GameOverPanel GameOver { get; private set; }
        public LevelCompletePanel LevelComplete { get; private set; }
        public CreditsPanel Credits { get; private set; }

        void Awake()
        {
            if (panelRoot == null) panelRoot = (RectTransform)transform;
            Hud = Create<HUDController>("HUD");
            MainMenu = Create<MainMenuPanel>("MainMenu");
            LevelSelect = Create<LevelSelectPanel>("LevelSelect");
            Pause = Create<PausePanel>("Pause");
            GameOver = Create<GameOverPanel>("GameOver");
            LevelComplete = Create<LevelCompletePanel>("LevelComplete");
            Credits = Create<CreditsPanel>("Credits");
            Settings = Create<SettingsPanel>("Settings");
            BindInputModule();
            Refresh();
        }

        T Create<T>(string panelName) where T : UIPanel
        {
            RectTransform rt = UIFactory.Stretch(UIFactory.Rect(panelName, panelRoot));
            T panel = rt.gameObject.AddComponent<T>();
            panel.Init(style, this);
            rt.gameObject.SetActive(false);
            return panel;
        }

        /// <summary>Points the UI input module at our UI action map so menus use the same bindings asset.</summary>
        void BindInputModule()
        {
            if (inputModule == null || input == null || input.Actions == null) return;
            InputActionAsset asset = input.Actions;
            inputModule.actionsAsset = asset;
            inputModule.point = InputActionReference.Create(asset.FindAction("UI/Point"));
            inputModule.leftClick = InputActionReference.Create(asset.FindAction("UI/Click"));
            inputModule.move = InputActionReference.Create(asset.FindAction("UI/Navigate"));
            inputModule.submit = InputActionReference.Create(asset.FindAction("UI/Submit"));
            inputModule.cancel = InputActionReference.Create(asset.FindAction("UI/Cancel"));
        }

        void OnEnable()
        {
            GameEvents.GameStateChanged += OnStateChanged;
            if (input != null) input.CancelPressed += OnCancel;
        }

        void OnDisable()
        {
            GameEvents.GameStateChanged -= OnStateChanged;
            if (input != null) input.CancelPressed -= OnCancel;
        }

        void OnStateChanged(GameState from, GameState to)
        {
            state = to;
            settingsOpen = false;
            levelSelectOpen = false;
            Refresh();
        }

        void Refresh()
        {
            HideAll();
            switch (state)
            {
                case GameState.MainMenu:
                    if (settingsOpen) Settings.Show();
                    else if (levelSelectOpen) LevelSelect.Show();
                    else MainMenu.Show();
                    break;
                case GameState.Playing:
                case GameState.Ending:
                    Hud.Show();
                    break;
                case GameState.Paused:
                    Hud.Show();
                    if (settingsOpen) Settings.Show();
                    else Pause.Show();
                    break;
                case GameState.GameOver:
                    Hud.Show();
                    GameOver.Show();
                    break;
                case GameState.LevelComplete:
                    LevelComplete.Show();
                    break;
                case GameState.GameFinished:
                    Credits.Show();
                    break;
            }
        }

        void HideAll()
        {
            MainMenu.Hide();
            LevelSelect.Hide();
            Settings.Hide();
            Pause.Hide();
            GameOver.Hide();
            LevelComplete.Hide();
            Credits.Hide();
            if (state != GameState.Playing && state != GameState.Paused && state != GameState.GameOver && state != GameState.Ending)
                Hud.Hide();
        }

        public void OpenLevelSelect()
        {
            levelSelectOpen = true;
            Refresh();
        }

        public void OpenSettings()
        {
            settingsOpen = true;
            Refresh();
        }

        public void CloseSettings()
        {
            settingsOpen = false;
            Refresh();
        }

        public void BackToMainMenu()
        {
            settingsOpen = false;
            levelSelectOpen = false;
            Refresh();
        }

        void OnCancel()
        {
            if (settingsOpen) CloseSettings();
            else if (levelSelectOpen) BackToMainMenu();
            else if (state == GameState.Paused) GameManager.Instance?.Resume();
        }

        public UIPanel VisiblePanel
        {
            get
            {
                UIPanel[] panels = { Settings, LevelSelect, MainMenu, Pause, GameOver, LevelComplete, Credits };
                foreach (UIPanel p in panels) if (p.IsShown) return p;
                return Hud.IsShown ? Hud : null;
            }
        }

        public bool AnyMenuSelected => EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null;
    }
}
