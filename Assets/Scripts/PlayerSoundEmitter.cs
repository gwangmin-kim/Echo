using UnityEngine;
using UnityEngine.Serialization;

namespace Echo.Gameplay
{
    [RequireComponent(typeof(FootstepEventEmitter))]
    public sealed class PlayerSoundEmitter : MonoBehaviour
    {
        [Header("Footstep Sound Profiles")]
        [FormerlySerializedAs("footstepProfile")]
        [SerializeField] private SoundWaveProfile walkingFootstepProfile;
        [SerializeField] private SoundWaveProfile sprintingFootstepProfile;
        [SerializeField] private SoundWaveProfile crouchingFootstepProfile;

        [Header("Per-step Variation")]
        [Range(0f, 1f)]
        [SerializeField] private float volumeVariationRatio = 0.1f;
        [Range(0f, 1f)]
        [SerializeField] private float pitchVariationRatio = 0.1f;

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

        private void OnFootstepOccurred(FootstepEventData data)
        {
            if (soundWaveSystem == null)
                soundWaveSystem = SoundWaveSystem.Instance;

            SoundWaveProfile profile = GetProfile(data.MovementState);
            if (soundWaveSystem == null || profile == null)
                return;

            float volumeVariation = GetRandomMultiplier(volumeVariationRatio);
            float pitchVariation = GetRandomMultiplier(pitchVariationRatio);
            float volume = Mathf.Clamp01(profile.AudioVolume * volumeVariation);
            float pitch = profile.AudioPitch * pitchVariation;

            SoundWaveEmission emission = new(
                data.Position,
                gameObject,
                profile.MaxRadius,
                profile.VisualThickness,
                profile.TraceDuration,
                profile.VisualIntensity,
                profile.HearingIntensity,
                profile.HearingFalloff,
                profile.AudioClip,
                volume,
                pitch,
                profile.AudioMaxDistance);

            soundWaveSystem.Emit(in emission);
        }

        private static float GetRandomMultiplier(float variationRatio)
        {
            float ratio = Mathf.Clamp01(variationRatio);
            return Random.Range(1f - ratio, 1f + ratio);
        }

        private SoundWaveProfile GetProfile(FootstepMovementState movementState)
        {
            return movementState switch
            {
                FootstepMovementState.Sprint => sprintingFootstepProfile != null
                    ? sprintingFootstepProfile : walkingFootstepProfile,
                FootstepMovementState.Crouch => crouchingFootstepProfile != null
                    ? crouchingFootstepProfile : walkingFootstepProfile,
                _ => walkingFootstepProfile
            };
        }
    }
}
