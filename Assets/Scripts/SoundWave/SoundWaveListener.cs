using System;
using UnityEngine;

namespace Echo.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SoundWaveListener : MonoBehaviour, ISoundWaveListener
    {
        [SerializeField, Min(0f)] private float sensitivity = 1f;
        [SerializeField, Min(0f)] private float minimumIntensity;

        public Transform ListenerTransform => transform;
        public float ListenerSensitivity => sensitivity;
        public event Action<SoundWaveHit> SoundWaveArrived;

        private void OnEnable()
        {
            SoundWaveSystem.Instance?.RegisterListener(this);
        }

        private void OnDisable()
        {
            SoundWaveSystem.Instance?.UnregisterListener(this);
        }

        public void OnSoundWaveArrived(in SoundWaveHit hit)
        {
            if (hit.PerceivedIntensity < minimumIntensity)
                return;

            SoundWaveArrived?.Invoke(hit);
        }
    }
}
