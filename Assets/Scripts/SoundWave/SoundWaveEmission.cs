using UnityEngine;

namespace Echo.Gameplay
{
    public readonly struct SoundWaveEmission
    {
        public readonly Vector3 Origin;
        public readonly GameObject Source;
        public readonly float MaxRadius;
        public readonly float VisualThickness;
        public readonly float TraceDuration;
        public readonly float VisualIntensity;
        public readonly float HearingIntensity;
        public readonly AnimationCurve HearingFalloff;
        public readonly AudioClip AudioClip;
        public readonly float AudioVolume;
        public readonly float AudioPitch;
        public readonly float AudioMaxDistance;

        public SoundWaveEmission(Vector3 origin, GameObject source, float maxRadius,
            float visualThickness, float traceDuration, float visualIntensity,
            float hearingIntensity, AnimationCurve hearingFalloff, AudioClip audioClip,
            float audioVolume, float audioPitch, float audioMaxDistance)
        {
            Origin = origin;
            Source = source;
            MaxRadius = Mathf.Max(0.01f, maxRadius);
            VisualThickness = Mathf.Max(0f, visualThickness);
            TraceDuration = Mathf.Max(0f, traceDuration);
            VisualIntensity = Mathf.Max(0f, visualIntensity);
            HearingIntensity = Mathf.Max(0f, hearingIntensity);
            HearingFalloff = hearingFalloff;
            AudioClip = audioClip;
            AudioVolume = Mathf.Clamp01(audioVolume);
            AudioPitch = audioPitch;
            AudioMaxDistance = Mathf.Max(0.01f, audioMaxDistance);
        }
    }
}
