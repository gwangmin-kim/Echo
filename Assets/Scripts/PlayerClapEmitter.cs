using UnityEngine;

namespace Echo.Gameplay
{
    public sealed class PlayerClapEmitter : MonoBehaviour
    {
        [SerializeField] private ClapChargeProfile profile;

        [Header("Sound Origin")]
        [Min(0f)]
        [SerializeField] private float originYOffset = 1.2f;

        private SoundWaveSystem soundWaveSystem;
        private double lastClapTime = double.NegativeInfinity;

        private void Start()
        {
            soundWaveSystem = SoundWaveSystem.Instance;
            if (soundWaveSystem == null)
                Debug.LogError("PlayerClapEmitter requires a SoundWaveSystem in the scene.", this);
        }

        private void OnEnable()
        {
            GlobalInputManager.ClapReleased += OnClapReleased;
        }

        private void OnDisable()
        {
            GlobalInputManager.ClapReleased -= OnClapReleased;
        }

        private void OnClapReleased(float heldTime)
        {
            if (soundWaveSystem == null)
                soundWaveSystem = SoundWaveSystem.Instance;

            if (soundWaveSystem == null || profile == null)
                return;

            double currentTime = Time.timeAsDouble;
            if (currentTime - lastClapTime < profile.MinimumReleaseInterval)
                return;

            lastClapTime = currentTime;

            AudioClip clip;
            ClapWaveValues values = profile.Evaluate(heldTime, out clip);
            SoundWaveEmission emission = new(
                GetSoundOrigin(),
                gameObject,
                values.MaxRadius,
                values.VisualThickness,
                values.TraceDuration,
                values.VisualIntensity,
                values.HearingIntensity,
                profile.HearingFalloff,
                clip,
                values.AudioVolume,
                values.AudioPitch,
                values.AudioMaxDistance);

            soundWaveSystem.Emit(emission);
        }

        private Vector3 GetSoundOrigin()
        {
            return transform.position + Vector3.up * originYOffset;
        }

        private void OnDrawGizmos()
        {
            Vector3 center = transform.position;
            Vector3 origin = center + Vector3.up * originYOffset;

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(center, origin);
            Gizmos.DrawWireSphere(origin, 0.08f);
        }
    }
}
