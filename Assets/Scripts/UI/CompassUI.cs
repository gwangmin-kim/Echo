using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// Screen-space horizontal compass. The tick and label objects are authored in the prefab;
    /// this component only positions and culls them at runtime.
    /// </summary>
    public sealed class CompassUI : MonoBehaviour
    {
        [SerializeField] private RectTransform strip;
        [SerializeField] private Camera headingCamera;
        [SerializeField] private float visibleDegrees = 120f;
        [SerializeField] private float stripWidth = 860f;
        [SerializeField] private float tickY = 38f;
        [SerializeField] private float longTickHeight = 22f;
        [SerializeField] private float shortTickHeight = 11f;
        [SerializeField] private float labelY = 5f;

        private RectTransform[] ticks;
        private UnityEngine.UI.Image[] tickImages;
        private TextMeshProUGUI[] cardinalLabels;

        private void Awake()
        {
            if (strip == null)
                strip = transform.Find("Viewport/Strip") as RectTransform;
            if (headingCamera == null)
                headingCamera = Camera.main;

            CacheAuthoredElements();
        }

        private void LateUpdate()
        {
            if (headingCamera == null)
                headingCamera = Camera.main;
            if (headingCamera == null || strip == null)
                return;

            float heading = Mathf.Repeat(headingCamera.transform.eulerAngles.y, 360f);
            UpdateTicks(heading);
            UpdateCardinalLabels(heading);
        }

        private void CacheAuthoredElements()
        {
            if (strip == null)
                return;

            ticks = new RectTransform[36];
            tickImages = new UnityEngine.UI.Image[ticks.Length];
            for (int i = 0; i < ticks.Length; i++)
            {
                ticks[i] = strip.Find($"Tick_{i * 10:000}") as RectTransform;
                if (ticks[i] != null)
                    tickImages[i] = ticks[i].GetComponent<UnityEngine.UI.Image>();
            }

            cardinalLabels = new TextMeshProUGUI[4];
            string[] names = { "North", "East", "South", "West" };
            for (int i = 0; i < cardinalLabels.Length; i++)
            {
                Transform label = strip.Find(names[i]);
                cardinalLabels[i] = label != null ? label.GetComponent<TextMeshProUGUI>() : null;
            }
        }

        private void UpdateTicks(float heading)
        {
            float halfVisible = visibleDegrees * 0.5f;
            for (int i = 0; i < ticks.Length; i++)
            {
                RectTransform tick = ticks[i];
                if (tick == null)
                    continue;

                float angle = i * 10f;
                float delta = Mathf.DeltaAngle(heading, angle);
                bool visible = Mathf.Abs(delta) <= halfVisible + 10f;
                tick.gameObject.SetActive(visible);
                if (!visible)
                    continue;

                tick.anchoredPosition = new Vector2(delta / visibleDegrees * stripWidth, tickY);
                UnityEngine.UI.Image image = tickImages[i];
                if (image != null)
                    image.rectTransform.sizeDelta = new Vector2(image.rectTransform.sizeDelta.x,
                        Mathf.Abs(Mathf.Repeat(angle, 360f)) % 30f < 0.01f ? longTickHeight : shortTickHeight);
            }
        }

        private void UpdateCardinalLabels(float heading)
        {
            if (cardinalLabels == null)
                return;

            float halfVisible = visibleDegrees * 0.5f;
            for (int i = 0; i < cardinalLabels.Length; i++)
            {
                TextMeshProUGUI label = cardinalLabels[i];
                if (label == null)
                    continue;

                float angle = i * 90f;
                float delta = Mathf.DeltaAngle(heading, angle);
                bool visible = Mathf.Abs(delta) <= halfVisible;
                label.gameObject.SetActive(visible);
                if (visible)
                    label.rectTransform.anchoredPosition = new Vector2(delta / visibleDegrees * stripWidth, labelY);
            }
        }

        /// <summary>Reserved extension point for future world-direction pings.</summary>
        public Vector2 GetStripPositionForWorldDirection(Vector3 worldDirection)
        {
            if (headingCamera == null)
                headingCamera = Camera.main;
            Vector3 flat = worldDirection;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f)
                return new Vector2(float.NaN, float.NaN);

            float direction = Mathf.Repeat(Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg, 360f);
            float heading = Mathf.Repeat(headingCamera.transform.eulerAngles.y, 360f);
            float delta = Mathf.DeltaAngle(heading, direction);
            return new Vector2(delta / visibleDegrees * stripWidth, 0f);
        }
    }
}
