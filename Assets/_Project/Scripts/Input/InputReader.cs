using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AshenRoad.Input
{
    /// <summary>
    /// The only class that talks to the Input System. It turns actions into plain C# events and a move axis,
    /// and switches between the Gameplay and UI maps. Gameplay code never sees a key code.
    /// </summary>
    public class InputReader : MonoBehaviour
    {
        [SerializeField] InputActionAsset actions;

        InputActionMap gameplay;
        InputActionMap ui;
        InputAction move, jump, roll, attack, pause, cancel;
        bool ownsAsset;

        public InputActionAsset Actions => actions;
        public float MoveAxis => move != null && gameplay.enabled ? move.ReadValue<float>() : 0f;
        public bool JumpHeld => jump != null && gameplay.enabled && jump.IsPressed();
        public bool GameplayEnabled => gameplay != null && gameplay.enabled;

        public event Action JumpPressed;
        public event Action JumpReleased;
        public event Action RollPressed;
        public event Action AttackPressed;
        public event Action PausePressed;
        public event Action CancelPressed;

        void Awake()
        {
            if (actions == null)
            {
                actions = InputActionAsset.FromJson(InputBindings.Json);
                ownsAsset = true;
            }
            gameplay = actions.FindActionMap(InputBindings.Gameplay, true);
            ui = actions.FindActionMap(InputBindings.UI, true);
            move = gameplay.FindAction("Move", true);
            jump = gameplay.FindAction("Jump", true);
            roll = gameplay.FindAction("Roll", true);
            attack = gameplay.FindAction("Attack", true);
            pause = gameplay.FindAction("Pause", true);
            cancel = ui.FindAction("Cancel", true);
        }

        void OnEnable()
        {
            jump.performed += OnJump;
            jump.canceled += OnJumpCanceled;
            roll.performed += OnRoll;
            attack.performed += OnAttack;
            pause.performed += OnPause;
            cancel.performed += OnCancel;
        }

        void OnDisable()
        {
            jump.performed -= OnJump;
            jump.canceled -= OnJumpCanceled;
            roll.performed -= OnRoll;
            attack.performed -= OnAttack;
            pause.performed -= OnPause;
            cancel.performed -= OnCancel;
            gameplay.Disable();
            ui.Disable();
        }

        void OnDestroy()
        {
            if (ownsAsset && actions != null) Destroy(actions);
        }

        public void EnableGameplay()
        {
            ui.Disable();
            gameplay.Enable();
        }

        public void EnableUI()
        {
            gameplay.Disable();
            ui.Enable();
        }

        public void DisableAll()
        {
            gameplay.Disable();
            ui.Disable();
        }

        void OnJump(InputAction.CallbackContext _) => JumpPressed?.Invoke();
        void OnJumpCanceled(InputAction.CallbackContext _) => JumpReleased?.Invoke();
        void OnRoll(InputAction.CallbackContext _) => RollPressed?.Invoke();
        void OnAttack(InputAction.CallbackContext _) => AttackPressed?.Invoke();
        void OnPause(InputAction.CallbackContext _) => PausePressed?.Invoke();
        void OnCancel(InputAction.CallbackContext _) => CancelPressed?.Invoke();
    }
}
