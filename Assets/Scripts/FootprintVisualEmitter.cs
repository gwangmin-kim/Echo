using UnityEngine;

namespace Echo.Gameplay
{
    [RequireComponent(typeof(FootstepEventEmitter))]
    public sealed class FootprintVisualEmitter : MonoBehaviour
    {
        [SerializeField] private FootprintRendererBase footprintRenderer;

        private FootstepEventEmitter emitter;

        private void Awake()
        {
            emitter = GetComponent<FootstepEventEmitter>();
        }

        private void OnEnable()
        {
            if (emitter == null)
                emitter = GetComponent<FootstepEventEmitter>();

            if (emitter != null)
            {
                emitter.FootstepOccurred -= OnFootstepOccurred;
                emitter.FootstepOccurred += OnFootstepOccurred;
            }
        }

        private void OnDisable()
        {
            if (emitter != null)
                emitter.FootstepOccurred -= OnFootstepOccurred;
        }

        private void OnFootstepOccurred(FootstepEventData data)
        {
            if (footprintRenderer == null)
                footprintRenderer = GetComponent<FootprintRendererBase>();

            if (footprintRenderer != null)
                footprintRenderer.Spawn(in data);
        }
    }
}
