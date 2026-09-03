using System;
using System.Collections.Generic;
using UnityEngine;

namespace Echo.Gameplay
{
    [DefaultExecutionOrder(-50)]
    public sealed class SoundWaveSystem : MonoBehaviour
    {
        private const int MaxShaderWaves = 16;
        private static readonly int ShaderWaveCountId = Shader.PropertyToID("_SoundWaveCount");
        private static readonly int ShaderWaveOriginsId = Shader.PropertyToID("_SoundWaveOrigins");
        private static readonly int ShaderWaveParamsId = Shader.PropertyToID("_SoundWaveParams");
        private static readonly int ShaderWaveTimeId = Shader.PropertyToID("_SoundWaveTime");
        private static readonly int ShaderPropagationSpeedId = Shader.PropertyToID("_SoundWavePropagationSpeed");

        private sealed class ActiveWave
        {
            public SoundWave Wave;
            public float LastRadius;
            public readonly HashSet<ISoundWaveListener> NotifiedListeners = new();
        }

        public static SoundWaveSystem Instance { get; private set; }

        [SerializeField] private SoundWaveSettings settings;

        private readonly List<ActiveWave> activeWaves = new();
        private readonly List<ISoundWaveListener> listeners = new();
        private readonly SoundWaveAudioService audioService = new();
        private readonly Vector4[] shaderWaveOrigins = new Vector4[MaxShaderWaves];
        private readonly Vector4[] shaderWaveParams = new Vector4[MaxShaderWaves];
        private int nextWaveId = 1;

        public int ActiveWaveCount => activeWaves.Count;

        public event Action<SoundWave> WaveEmitted;
        public event Action<SoundWave> WaveExpired;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public int Emit(in SoundWaveEmission emission)
        {
            int maxWaves = settings != null ? Mathf.Max(1, settings.MaxLogicalWaves) : 64;
            if (activeWaves.Count >= maxWaves)
            {
                Debug.LogWarning($"Sound wave limit ({maxWaves}) reached; emission was ignored.", this);
                return 0;
            }

            SoundWave wave = new(nextWaveId++, emission.Origin, Time.timeAsDouble, in emission);
            activeWaves.Add(new ActiveWave { Wave = wave });
            audioService.Play(wave);
            WaveEmitted?.Invoke(wave);
            return wave.Id;
        }

        public void RegisterListener(ISoundWaveListener listener)
        {
            if (listener != null && !listeners.Contains(listener))
                listeners.Add(listener);
        }

        public void UnregisterListener(ISoundWaveListener listener)
        {
            listeners.Remove(listener);
            foreach (ActiveWave activeWave in activeWaves)
                activeWave.NotifiedListeners.Remove(listener);
        }

        private void Update()
        {
            double currentTime = Time.timeAsDouble;
            float propagationSpeed = settings != null ? Mathf.Max(0.01f, settings.PropagationSpeed) : 25f;

            for (int waveIndex = activeWaves.Count - 1; waveIndex >= 0; waveIndex--)
            {
                ActiveWave activeWave = activeWaves[waveIndex];
                float currentRadius = Mathf.Max(0f, (float)((currentTime - activeWave.Wave.StartTime) * propagationSpeed));

                NotifyArrivals(activeWave, currentRadius, currentTime);

                float visualLifetime = activeWave.Wave.MaxRadius / propagationSpeed + activeWave.Wave.TraceDuration;
                bool lifetimeEnded = currentTime - activeWave.Wave.StartTime >= visualLifetime &&
                                     AllListenersProcessed(activeWave);
                if (!lifetimeEnded)
                {
                    activeWave.LastRadius = currentRadius;
                    continue;
                }

                SoundWave expiredWave = activeWave.Wave;
                activeWaves.RemoveAt(waveIndex);
                WaveExpired?.Invoke(expiredWave);
            }

            UploadShaderWaves(currentTime, propagationSpeed);
        }

        private void UploadShaderWaves(double currentTime, float propagationSpeed)
        {
            int configuredVisibleWaves = settings != null ? settings.MaxVisibleWaves : MaxShaderWaves;
            int visibleCount = Mathf.Min(activeWaves.Count, Mathf.Clamp(configuredVisibleWaves, 0, MaxShaderWaves));
            int firstWave = activeWaves.Count - visibleCount;

            for (int i = 0; i < visibleCount; i++)
            {
                SoundWave wave = activeWaves[firstWave + i].Wave;
                shaderWaveOrigins[i] = new Vector4(wave.Origin.x, wave.Origin.y, wave.Origin.z, (float)wave.StartTime);
                shaderWaveParams[i] = new Vector4(wave.MaxRadius, wave.VisualThickness,
                    wave.TraceDuration, wave.VisualIntensity);
            }

            for (int i = visibleCount; i < MaxShaderWaves; i++)
            {
                shaderWaveOrigins[i] = Vector4.zero;
                shaderWaveParams[i] = Vector4.zero;
            }

            Shader.SetGlobalInt(ShaderWaveCountId, visibleCount);
            Shader.SetGlobalVectorArray(ShaderWaveOriginsId, shaderWaveOrigins);
            Shader.SetGlobalVectorArray(ShaderWaveParamsId, shaderWaveParams);
            Shader.SetGlobalFloat(ShaderWaveTimeId, (float)currentTime);
            Shader.SetGlobalFloat(ShaderPropagationSpeedId, propagationSpeed);
        }

        private void NotifyArrivals(ActiveWave activeWave, float currentRadius, double currentTime)
        {
            SoundWave wave = activeWave.Wave;
            float propagationSpeed = settings != null ? Mathf.Max(0.01f, settings.PropagationSpeed) : 25f;

            for (int listenerIndex = listeners.Count - 1; listenerIndex >= 0; listenerIndex--)
            {
                ISoundWaveListener listener = listeners[listenerIndex];
                if (listener == null || listener.ListenerTransform == null)
                {
                    listeners.RemoveAt(listenerIndex);
                    continue;
                }

                if (activeWave.NotifiedListeners.Contains(listener))
                    continue;

                Vector3 listenerPosition = listener.ListenerTransform.position;
                float distance = Vector3.Distance(wave.Origin, listenerPosition);
                if (activeWave.LastRadius >= distance || currentRadius < distance)
                    continue;

                float normalizedDistance = Mathf.Clamp01(distance / Mathf.Max(0.01f, wave.MaxRadius));
                float falloff = wave.HearingFalloff != null
                    ? wave.HearingFalloff.Evaluate(normalizedDistance)
                    : 1f - normalizedDistance;
                float intensity = wave.HearingIntensity * falloff * Mathf.Max(0f, listener.ListenerSensitivity);
                double arrivalTime = wave.StartTime + distance / propagationSpeed;

                activeWave.NotifiedListeners.Add(listener);
                SoundWaveHit hit = new(wave, distance, intensity, arrivalTime);
                listener.OnSoundWaveArrived(in hit);
            }
        }

        private bool AllListenersProcessed(ActiveWave activeWave)
        {
            for (int i = 0; i < listeners.Count; i++)
            {
                if (listeners[i] != null && !activeWave.NotifiedListeners.Contains(listeners[i]))
                    return false;
            }

            return true;
        }
    }
}
