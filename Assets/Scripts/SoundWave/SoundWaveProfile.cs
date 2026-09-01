using UnityEngine;

namespace Echo.Gameplay
{
    [CreateAssetMenu(menuName = "Echo/Sound Wave/Profile", fileName = "SoundWaveProfile")]
    public sealed class SoundWaveProfile : ScriptableObject
    {
        [Header("Propagation")]
        [Min(0.01f)] public float MaxRadius = 12f;

        [Header("Visualisation")]
        [Min(0f)] public float VisualThickness = 0.25f;
        [Min(0f)] public float TraceDuration = 0.5f;
        [Min(0f)] public float VisualIntensity = 1f;
        public AnimationCurve VisualFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [Header("Detection")]
        [Min(0f)] public float HearingIntensity = 1f;
        public AnimationCurve HearingFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [Header("Audio")]
        public AudioClip AudioClip;
        [Range(0f, 1f)] public float AudioVolume = 1f;
        [Range(-3f, 3f)] public float AudioPitch = 1f;
        [Min(0.01f)] public float AudioMaxDistance = 20f;
    }
}
