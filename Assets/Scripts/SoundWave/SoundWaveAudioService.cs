using UnityEngine;

namespace Echo.Gameplay
{
    public sealed class SoundWaveAudioService
    {
        public void Play(in SoundWave wave)
        {
            if (wave.AudioClip == null)
                return;

            GameObject audioObject = new($"SoundWaveAudio_{wave.Id}");
            audioObject.transform.position = wave.Origin;

            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.clip = wave.AudioClip;
            source.volume = wave.AudioVolume;
            source.pitch = wave.AudioPitch;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 1f;
            source.maxDistance = wave.AudioMaxDistance;
            source.Play();

            Object.Destroy(audioObject, wave.AudioClip.length / Mathf.Max(0.01f, Mathf.Abs(wave.AudioPitch)) + 0.1f);
        }
    }
}
