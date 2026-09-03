using UnityEngine;

namespace Echo.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float crouchSpeedMultiplier = 0.5f;
        [SerializeField] private float sprintSpeedMultiplier = 1.75f;
        [SerializeField] private float turnSpeed = 720f;
        [SerializeField] private Transform movementReference;

        private Rigidbody body;
        private GlobalInputManager input;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void Start()
        {
            input = GlobalInputManager.Instance;
            if (input == null)
                Debug.LogError("PlayerMovement requires a GlobalInputManager in the scene.");

            if (movementReference == null)
            {
                movementReference = Camera.main.transform;
                if (movementReference == null)
                    Debug.LogError("Playermovement requires a Camera in the scene as a movementReference");
            }
        }

        private void FixedUpdate()
        {
            if (input == null)
                return;

            body.angularVelocity = Vector3.zero;

            Transform reference = movementReference != null ? movementReference : Camera.main.transform;
            Vector3 forward = reference != null ? reference.forward : Vector3.forward;
            Vector3 right = reference != null ? reference.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector2 axis = Vector2.ClampMagnitude(input.MoveInput, 1f);
            Vector3 direction = right * axis.x + forward * axis.y;
            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
            }

            Vector3 velocity = body.linearVelocity;
            float speedMultiplier = input.ActiveMovementModifier switch
            {
                MovementModifier.Crouch => crouchSpeedMultiplier,
                MovementModifier.Sprint => sprintSpeedMultiplier,
                _ => 1f
            };
            velocity.x = direction.x * moveSpeed * speedMultiplier;
            velocity.z = direction.z * moveSpeed * speedMultiplier;
            body.linearVelocity = velocity;
        }
    }
}
