using UnityEngine;

namespace Echo.Gameplay
{
    public interface ISoundWaveListener
    {
        Transform ListenerTransform { get; }
        float ListenerSensitivity { get; }
        void OnSoundWaveArrived(in SoundWaveHit hit);
    }
}
