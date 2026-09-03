using System;
using UnityEngine;

namespace Echo.Gameplay
{
    public enum FootstepMovementState
    {
        Walk,
        Sprint,
        Crouch
    }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class FootstepEventEmitter : MonoBehaviour
    {
        [Header("Foot Placement")]
        [SerializeField] private float footHorizontalOffset = 0.25f;

        [Header("User-facing Step Thresholds")]
        [SerializeField] private float standingStepThreshold = 2f;
        [SerializeField] private float crouchStepThreshold = 1.5f;
        [SerializeField] private float sprintStepThreshold = 2.5f;

        public event Action<Vector3, FootstepMovementState> FootstepOccurred;

        private Rigidbody body;
        private GlobalInputManager input;
        private Vector3 previousPosition;
        private float accumulatedDistance;
        private float fixedStepThreshold;
        private float standingMultiplier;
        private float crouchMultiplier;
        private float sprintMultiplier;
        private bool nextFootIsLeft = true;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            previousPosition = body.position;

            // The standing threshold is the internal fixed threshold. State-specific
            // thresholds are converted to multipliers once and never changed at runtime.
            fixedStepThreshold = Mathf.Max(0.01f, standingStepThreshold);
            standingMultiplier = 1f;
            crouchMultiplier = fixedStepThreshold / Mathf.Max(0.01f, crouchStepThreshold);
            sprintMultiplier = fixedStepThreshold / Mathf.Max(0.01f, sprintStepThreshold);
        }

        private void Start()
        {
            input = GlobalInputManager.Instance;
            if (input == null)
                Debug.LogError("FootstepEventEmitter requires a GlobalInputManager in the scene.");
        }

        private void FixedUpdate()
        {
            Vector3 currentPosition = body.position;
            Vector3 displacement = currentPosition - previousPosition;
            previousPosition = currentPosition;
            displacement.y = 0f;

            accumulatedDistance += displacement.magnitude;

            float multiplier = GetCurrentMultiplier();
            if (accumulatedDistance * multiplier < fixedStepThreshold)
                return;

            // Reset the raw accumulated distance at each event. This keeps state changes
            // from discarding progress while still preventing threshold switching exploits.
            accumulatedDistance = 0f;
            EmitFootstep();
        }

        private float GetCurrentMultiplier()
        {
            if (input == null)
                return standingMultiplier;

            return input.ActiveMovementModifier switch
            {
                MovementModifier.Crouch => crouchMultiplier,
                MovementModifier.Sprint => sprintMultiplier,
                _ => standingMultiplier
            };
        }

        private void EmitFootstep()
        {
            float side = nextFootIsLeft ? -1f : 1f;
            Vector3 worldPosition = transform.TransformPoint(new Vector3(side * footHorizontalOffset, 0f, 0f));
            nextFootIsLeft = !nextFootIsLeft;

            FootstepMovementState movementState = GetCurrentMovementState();
            Debug.Log($"Footstep event ({movementState}) at {worldPosition}", this);
            FootstepOccurred?.Invoke(worldPosition, movementState);
        }

        private FootstepMovementState GetCurrentMovementState()
        {
            if (input == null)
                return FootstepMovementState.Walk;

            return input.ActiveMovementModifier switch
            {
                MovementModifier.Sprint => FootstepMovementState.Sprint,
                MovementModifier.Crouch => FootstepMovementState.Crouch,
                _ => FootstepMovementState.Walk
            };
        }

        private void OnDrawGizmos()
        {
            Vector3 center = transform.position;
            Vector3 left = transform.TransformPoint(new Vector3(-footHorizontalOffset, 0f, 0f));
            Vector3 right = transform.TransformPoint(new Vector3(footHorizontalOffset, 0f, 0f));

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(center, left);
            Gizmos.DrawLine(center, right);
            Gizmos.DrawWireSphere(left, 0.06f);
            Gizmos.DrawWireSphere(right, 0.06f);
        }
    }
}
