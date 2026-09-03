using UnityEngine;

namespace Echo.Gameplay
{
    public readonly struct FootstepEventData
    {
        public readonly Vector3 Position;
        public readonly Vector3 Forward;
        public readonly bool IsLeftFoot;
        public readonly FootstepMovementState MovementState;
        public readonly GameObject Source;

        public FootstepEventData(Vector3 position, Vector3 forward, bool isLeftFoot,
            FootstepMovementState movementState, GameObject source)
        {
            Position = position;
            Forward = forward;
            IsLeftFoot = isLeftFoot;
            MovementState = movementState;
            Source = source;
        }
    }
}
