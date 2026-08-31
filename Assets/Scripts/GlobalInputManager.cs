using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echo.Gameplay
{
    public enum MovementModifier
    {
        None,
        Crouch,
        Sprint
    }

    [DefaultExecutionOrder(-100)]
    public sealed class GlobalInputManager : MonoBehaviour
    {
        public static GlobalInputManager Instance { get; private set; }

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public MovementModifier ActiveMovementModifier { get; private set; }

        public event Action<Vector2> MoveInputChanged;
        public event Action<Vector2> LookInputChanged;
        public event Action<MovementModifier> MovementModifierChanged;

        private InputActionMap gameplayMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction crouchAction;
        private InputAction sprintAction;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
            {
                Debug.LogError("Project-wide Input Actions asset is not configured.");
                return;
            }

            gameplayMap = actions.FindActionMap("Gameplay", true);
            moveAction = gameplayMap.FindAction("Move", true);
            lookAction = gameplayMap.FindAction("Look", true);
            crouchAction = gameplayMap.FindAction("Crouch", true);
            sprintAction = gameplayMap.FindAction("Sprint", true);

            moveAction.performed += OnMove;
            moveAction.canceled += OnMove;
            lookAction.performed += OnLook;
            lookAction.canceled += OnLook;
            crouchAction.performed += OnCrouch;
            crouchAction.canceled += OnCrouch;
            sprintAction.performed += OnSprint;
            sprintAction.canceled += OnSprint;
            gameplayMap.Enable();
        }

        private void OnDisable()
        {
            if (moveAction != null)
            {
                moveAction.performed -= OnMove;
                moveAction.canceled -= OnMove;
            }

            if (lookAction != null)
            {
                lookAction.performed -= OnLook;
                lookAction.canceled -= OnLook;
            }

            if (crouchAction != null)
            {
                crouchAction.performed -= OnCrouch;
                crouchAction.canceled -= OnCrouch;
            }

            if (sprintAction != null)
            {
                sprintAction.performed -= OnSprint;
                sprintAction.canceled -= OnSprint;
            }

            gameplayMap?.Disable();
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
            MoveInputChanged?.Invoke(MoveInput);
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            LookInput = context.ReadValue<Vector2>();
            LookInputChanged?.Invoke(LookInput);
        }

        private void OnCrouch(InputAction.CallbackContext context)
        {
            UpdateMovementModifier(MovementModifier.Crouch, context.ReadValueAsButton());
        }

        private void OnSprint(InputAction.CallbackContext context)
        {
            UpdateMovementModifier(MovementModifier.Sprint, context.ReadValueAsButton());
        }

        private void UpdateMovementModifier(MovementModifier modifier, bool pressed)
        {
            if (pressed)
            {
                // The latest modifier press always replaces the previous one.
                ActiveMovementModifier = modifier;
            }
            else if (ActiveMovementModifier == modifier)
            {
                ActiveMovementModifier = MovementModifier.None;
            }

            MovementModifierChanged?.Invoke(ActiveMovementModifier);
        }

        private void LateUpdate()
        {
            // Mouse Delta is a per-frame value. Movement remains held until canceled.
            if (LookInput != Vector2.zero)
            {
                LookInput = Vector2.zero;
                LookInputChanged?.Invoke(LookInput);
            }
        }
    }
}
