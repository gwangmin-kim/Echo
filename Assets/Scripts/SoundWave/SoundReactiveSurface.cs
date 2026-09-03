using UnityEngine;

namespace Echo.Gameplay
{
    [RequireComponent(typeof(Renderer))]
    public sealed class SoundReactiveSurface : MonoBehaviour
    {
        [SerializeField] private Color responseColor = Color.white;
        [SerializeField, Min(0f)] private float brightness = 1f;

        private static readonly int ResponseColorId = Shader.PropertyToID("_SoundWaveColor");
        private static readonly int BrightnessId = Shader.PropertyToID("_SoundWaveBrightness");

        private Renderer targetRenderer;
        private MaterialPropertyBlock properties;

        public Color ResponseColor => responseColor;

        private void Awake()
        {
            targetRenderer = GetComponent<Renderer>();
            properties = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            ApplyProperties();
        }

        private void ApplyProperties()
        {
            if (targetRenderer == null)
                return;

            targetRenderer.GetPropertyBlock(properties);
            properties.SetColor(ResponseColorId, responseColor);
            properties.SetFloat(BrightnessId, brightness);
            targetRenderer.SetPropertyBlock(properties);
        }
    }
}
