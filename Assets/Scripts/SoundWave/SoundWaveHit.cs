namespace Echo.Gameplay
{
    public readonly struct SoundWaveHit
    {
        public readonly SoundWave Wave;
        public readonly float TravelDistance;
        public readonly float PerceivedIntensity;
        public readonly double ArrivalTime;

        public SoundWaveHit(SoundWave wave, float travelDistance,
            float perceivedIntensity, double arrivalTime)
        {
            Wave = wave;
            TravelDistance = travelDistance;
            PerceivedIntensity = perceivedIntensity;
            ArrivalTime = arrivalTime;
        }
    }
}
