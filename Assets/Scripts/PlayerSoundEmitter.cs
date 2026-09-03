using UnityEngine;

namespace Echo.Gameplay
{
    [RequireComponent(typeof(FootstepEventEmitter))]
    public sealed class PlayerSoundEmitter : MonoBehaviour
    {
        [SerializeField] private SoundWaveProfile footstepProfile;

        private FootstepEventEmitter footstepEmitter;
        private SoundWaveSystem soundWaveSystem;

        private void Awake()
        {
            footstepEmitter = GetComponent<FootstepEventEmitter>();
        }

        private void OnEnable()
        {
            footstepEmitter.FootstepOccurred += OnFootstepOccurred;
        }

        private void Start()
        {
            soundWaveSystem = SoundWaveSystem.Instance;
            if (soundWaveSystem == null)
                Debug.LogError("PlayerSoundEmitter requires a SoundWaveSystem in the scene.", this);
        }

        private void OnDisable()
        {
            if (footstepEmitter != null)
                footstepEmitter.FootstepOccurred -= OnFootstepOccurred;
        }

        private void OnFootstepOccurred(Vector3 position)
        {
            if (soundWaveSystem == null)
                soundWaveSystem = SoundWaveSystem.Instance;

            if (soundWaveSystem == null || footstepProfile == null)
                return;

            SoundWaveEmission emission = new(
                position,
                gameObject,
                footstepProfile.MaxRadius,
                footstepProfile.VisualThickness,
                footstepProfile.TraceDuration,
                footstepProfile.VisualIntensity,
                footstepProfile.HearingIntensity,
                footstepProfile.HearingFalloff,
                footstepProfile.AudioClip,
                footstepProfile.AudioVolume,
                footstepProfile.AudioPitch,
                footstepProfile.AudioMaxDistance);

            soundWaveSystem.Emit(in emission);
        }
    }
}
