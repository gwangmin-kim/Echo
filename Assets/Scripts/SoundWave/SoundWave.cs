using UnityEngine;

namespace Echo.Gameplay
{
    public readonly struct SoundWave
    {
        public readonly int Id;
        public readonly Vector3 Origin;
        public readonly double StartTime;
        public readonly SoundWaveProfile Profile;
        public readonly GameObject Source;

        public SoundWave(int id, Vector3 origin, double startTime,
            SoundWaveProfile profile, GameObject source)
        {
            Id = id;
            Origin = origin;
            StartTime = startTime;
            Profile = profile;
            Source = source;
        }
    }
}
