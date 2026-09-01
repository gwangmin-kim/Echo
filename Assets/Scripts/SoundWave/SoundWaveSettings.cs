using UnityEngine;

namespace Echo.Gameplay
{
    [CreateAssetMenu(menuName = "Echo/Sound Wave/Settings", fileName = "SoundWaveSettings")]
    public sealed class SoundWaveSettings : ScriptableObject
    {
        [Min(0.01f)] public float PropagationSpeed = 25f;
        [Min(1)] public int MaxVisibleWaves = 16;
        [Min(1)] public int MaxLogicalWaves = 64;
    }
}
