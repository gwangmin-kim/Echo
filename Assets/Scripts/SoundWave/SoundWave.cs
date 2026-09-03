using UnityEngine;

namespace Echo.Gameplay
{
    public readonly struct SoundWave
    {
        public readonly int Id;
        public readonly Vector3 Origin;
        public readonly double StartTime;
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

        public SoundWave(int id, Vector3 origin, double startTime,
            in SoundWaveEmission emission)
        {
            Id = id;
            Origin = emission.Origin;
            StartTime = startTime;
            Source = emission.Source;
            MaxRadius = emission.MaxRadius;
            VisualThickness = emission.VisualThickness;
            TraceDuration = emission.TraceDuration;
            VisualIntensity = emission.VisualIntensity;
            HearingIntensity = emission.HearingIntensity;
            HearingFalloff = emission.HearingFalloff;
            AudioClip = emission.AudioClip;
            AudioVolume = emission.AudioVolume;
            AudioPitch = emission.AudioPitch;
            AudioMaxDistance = emission.AudioMaxDistance;
        }
    }
}
