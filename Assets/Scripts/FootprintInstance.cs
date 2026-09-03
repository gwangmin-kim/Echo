using UnityEngine;

namespace Echo.Gameplay
{
    public sealed class FootprintInstance : MonoBehaviour
    {
        private static readonly int FootprintColorId = Shader.PropertyToID("_FootprintColor");

        private Renderer targetRenderer;
        private MaterialPropertyBlock properties;
        private Color color;
        private AnimationCurve fadeCurve;
        private Vector3 baseScale;
        private float lifetime;
        private float elapsed;
        private bool active;
        private System.Action<FootprintInstance> release;

        public bool IsActive => active;

        private void Awake()
        {
            targetRenderer = GetComponentInChildren<Renderer>();
            properties = new MaterialPropertyBlock();
            baseScale = transform.localScale;
        }

        public void Play(Color footprintColor, bool mirrored,
            float duration, AnimationCurve curve, System.Action<FootprintInstance> releaseCallback)
        {
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();

            color = footprintColor;
            fadeCurve = curve;
            lifetime = Mathf.Max(0.01f, duration);
            elapsed = 0f;
            release = releaseCallback;
            active = true;

            transform.localScale = new Vector3(mirrored ? -baseScale.x : baseScale.x, baseScale.y, baseScale.z);
            gameObject.SetActive(true);
            ApplyAlpha(1f);
        }

        private void Update()
        {
            if (!active)
                return;

            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / lifetime);
            ApplyAlpha(fadeCurve == null ? 1f - normalizedTime : fadeCurve.Evaluate(normalizedTime));

            if (normalizedTime >= 1f)
            {
                active = false;
                release?.Invoke(this);
            }
        }

        public void Stop()
        {
            active = false;
            release = null;
            gameObject.SetActive(false);
        }

        private void ApplyAlpha(float alpha)
        {
            if (targetRenderer == null)
                return;

            targetRenderer.GetPropertyBlock(properties);
            color.a = Mathf.Clamp01(alpha);
            properties.SetColor(FootprintColorId, color);
            targetRenderer.SetPropertyBlock(properties);
        }
    }
}
