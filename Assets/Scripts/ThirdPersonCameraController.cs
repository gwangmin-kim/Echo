using Unity.Cinemachine;
using UnityEngine;

namespace Echo.Gameplay
{
    [RequireComponent(typeof(CinemachineOrbitalFollow))]
    public sealed class ThirdPersonCameraController : MonoBehaviour
    {
        [SerializeField] private float horizontalSensitivity = 0.12f;
        [SerializeField] private float verticalSensitivity = 0.12f;
        [SerializeField] private bool invertVertical;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private float standingTargetHeight = 1.4f;
        [SerializeField] private float crouchingTargetHeight = 0.9f;
        [SerializeField] private float stanceTransitionSpeed = 8f;

        private CinemachineOrbitalFollow orbitalFollow;
        private GlobalInputManager input;

        private void Awake()
        {
            orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
        }

        private void Start()
        {
            input = GlobalInputManager.Instance;
            if (cameraTarget == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                cameraTarget = player != null ? player.transform.Find("CameraTarget") : null;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (input == null)
                return;

            Vector2 look = input.LookInput;
            orbitalFollow.HorizontalAxis.Value += look.x * horizontalSensitivity;
            float verticalDelta = look.y * verticalSensitivity * (invertVertical ? 1f : -1f);
            orbitalFollow.VerticalAxis.Value = orbitalFollow.VerticalAxis.ClampValue(
                orbitalFollow.VerticalAxis.Value + verticalDelta);

            if (cameraTarget != null)
            {
                float desiredHeight = input.ActiveMovementModifier == MovementModifier.Crouch
                    ? crouchingTargetHeight
                    : standingTargetHeight;
                Vector3 targetPosition = cameraTarget.localPosition;
                targetPosition.y = Mathf.MoveTowards(
                    targetPosition.y,
                    desiredHeight,
                    stanceTransitionSpeed * Time.deltaTime);
                cameraTarget.localPosition = targetPosition;
            }
        }

        private void OnDestroy()
        {
            if (GlobalInputManager.Instance == input)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
