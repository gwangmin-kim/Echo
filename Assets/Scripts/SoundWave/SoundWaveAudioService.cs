using UnityEngine;

namespace Echo.Gameplay
{
    public sealed class SoundWaveAudioService
    {
        public void Play(in SoundWave wave)
        {
            SoundWaveProfile profile = wave.Profile;
            if (profile == null || profile.AudioClip == null)
                return;

            GameObject audioObject = new($"SoundWaveAudio_{wave.Id}");
            audioObject.transform.position = wave.Origin;

            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.clip = profile.AudioClip;
            source.volume = profile.AudioVolume;
            source.pitch = profile.AudioPitch;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 1f;
            source.maxDistance = profile.AudioMaxDistance;
            source.Play();

            Object.Destroy(audioObject, profile.AudioClip.length / Mathf.Max(0.01f, Mathf.Abs(profile.AudioPitch)) + 0.1f);
        }
    }
}
