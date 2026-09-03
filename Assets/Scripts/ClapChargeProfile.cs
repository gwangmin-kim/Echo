using System;
using UnityEngine;

namespace Echo.Gameplay
{
    [Serializable]
    public sealed class ClapWaveValues
    {
        [Min(0.01f)] public float MaxRadius = 8f;
        [Min(0f)] public float VisualThickness = 0.2f;
        [Min(0f)] public float TraceDuration = 0.4f;
        [Min(0f)] public float VisualIntensity = 0.75f;
        [Min(0f)] public float HearingIntensity = 0.75f;
        [Range(0f, 1f)] public float AudioVolume = 0.75f;
        [Range(-3f, 3f)] public float AudioPitch = 1f;
        [Min(0.01f)] public float AudioMaxDistance = 20f;
    }

    [CreateAssetMenu(menuName = "Echo/Sound Wave/Clap Charge Profile", fileName = "ClapChargeProfile")]
    public sealed class ClapChargeProfile : ScriptableObject
    {
        [Header("Charge Timing")]
        [Min(0f)] public float ShortReleaseDuration = 0.12f;
        [Min(0.01f)] public float MaximumChargeTime = 1.5f;
        [Min(0f)] public float MinimumReleaseInterval = 0.25f;
        public AnimationCurve ChargeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Short Release")]
        public AudioClip SmallReleaseClip;
        public ClapWaveValues SmallRelease = new();

        [Header("Charged Release")]
        public AudioClip ChargedClip;
        public ClapWaveValues ChargedMinimum = new();
        public ClapWaveValues ChargedMaximum = new()
        {
            MaxRadius = 24f,
            VisualThickness = 0.6f,
            TraceDuration = 1.2f,
            VisualIntensity = 1.4f,
            HearingIntensity = 2f,
            AudioVolume = 1f,
            AudioPitch = 0.85f,
            AudioMaxDistance = 30f
        };

        [Header("Detection")]
        public AnimationCurve HearingFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        public ClapWaveValues Evaluate(float heldTime, out AudioClip clip)
        {
            if (heldTime < ShortReleaseDuration)
            {
                clip = SmallReleaseClip;
                return SmallRelease;
            }

            float charge01 = Mathf.InverseLerp(ShortReleaseDuration, MaximumChargeTime, heldTime);
            float charge = ChargeCurve != null ? ChargeCurve.Evaluate(charge01) : charge01;
            clip = ChargedClip != null ? ChargedClip : SmallReleaseClip;
            return Lerp(ChargedMinimum, ChargedMaximum, Mathf.Clamp01(charge));
        }

        private static ClapWaveValues Lerp(ClapWaveValues from, ClapWaveValues to, float t)
        {
            return new ClapWaveValues
            {
                MaxRadius = Mathf.Lerp(from.MaxRadius, to.MaxRadius, t),
                VisualThickness = Mathf.Lerp(from.VisualThickness, to.VisualThickness, t),
                TraceDuration = Mathf.Lerp(from.TraceDuration, to.TraceDuration, t),
                VisualIntensity = Mathf.Lerp(from.VisualIntensity, to.VisualIntensity, t),
                HearingIntensity = Mathf.Lerp(from.HearingIntensity, to.HearingIntensity, t),
                AudioVolume = Mathf.Lerp(from.AudioVolume, to.AudioVolume, t),
                AudioPitch = Mathf.Lerp(from.AudioPitch, to.AudioPitch, t),
                AudioMaxDistance = Mathf.Lerp(from.AudioMaxDistance, to.AudioMaxDistance, t)
            };
        }
    }
}
