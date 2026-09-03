using System.Collections.Generic;
using UnityEngine;

namespace Echo.Gameplay
{
    public sealed class FootprintPool : FootprintRendererBase
    {
        [Header("Pool")]
        [SerializeField] private FootprintInstance footprintPrefab;
        [SerializeField, Min(1)] private int initialPoolSize = 32;
        [SerializeField, Min(1)] private int maximumPoolSize = 64;

        [Header("Placement")]
        [SerializeField, Min(0f)] private float surfaceOffset = 0.002f;
        [SerializeField, Min(0.01f)] private float surfaceProbeHeight = 0.5f;
        [SerializeField, Min(0.01f)] private float surfaceProbeDistance = 2f;
        [SerializeField] private LayerMask surfaceMask = Physics.DefaultRaycastLayers;

        [Header("Fade")]
        [SerializeField, Min(0.01f)] private float lifetime = 14f;
        [SerializeField] private AnimationCurve fadeCurve = new(
            new Keyframe(0f, 1f),
            new Keyframe(0.12f, 0.35f),
            new Keyframe(1f, 0f));

        private readonly List<FootprintInstance> pool = new();
        private readonly RaycastHit[] surfaceHits = new RaycastHit[8];

        private void Awake()
        {
            int count = Mathf.Min(Mathf.Max(1, initialPoolSize), Mathf.Max(1, maximumPoolSize));
            for (int i = 0; i < count; i++)
                CreateInstance();
        }

        public override void Spawn(in FootstepEventData data)
        {
            if (footprintPrefab == null)
                return;

            FootprintInstance instance = GetAvailableInstance();
            if (instance == null)
                return;

            Vector3 normal = Vector3.up;
            Color color = Color.white;
            TryGetSurfaceAppearance(data, ref normal, ref color);

            Vector3 forward = Vector3.ProjectOnPlane(data.Forward, normal);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;

            Quaternion rotation = Quaternion.LookRotation(normal, forward.normalized);
            instance.transform.SetPositionAndRotation(
                data.Position + normal * surfaceOffset,
                rotation);
            instance.Play(color, !data.IsLeftFoot, lifetime, fadeCurve, Release);
        }

        private FootprintInstance GetAvailableInstance()
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (!pool[i].IsActive)
                    return pool[i];
            }

            return pool.Count < Mathf.Max(1, maximumPoolSize) ? CreateInstance() : null;
        }

        private FootprintInstance CreateInstance()
        {
            FootprintInstance instance = Instantiate(footprintPrefab, transform);
            instance.name = $"Footprint_{pool.Count:00}";
            instance.Stop();
            pool.Add(instance);
            return instance;
        }

        private void Release(FootprintInstance instance)
        {
            instance.Stop();
        }

        private void TryGetSurfaceAppearance(in FootstepEventData data,
            ref Vector3 normal, ref Color color)
        {
            Vector3 origin = data.Position + Vector3.up * surfaceProbeHeight;
            int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, surfaceHits,
                surfaceProbeDistance, surfaceMask, QueryTriggerInteraction.Ignore);

            RaycastHit hit = default;
            float closestDistance = float.MaxValue;
            bool foundSurface = false;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = surfaceHits[i];
                if (candidate.collider == null || IsSourceCollider(candidate.collider, data.Source))
                    continue;
                if (candidate.distance < closestDistance)
                {
                    hit = candidate;
                    closestDistance = candidate.distance;
                    foundSurface = true;
                }
            }

            if (!foundSurface)
                return;

            normal = hit.normal;
            SoundReactiveSurface surface = hit.collider.GetComponentInParent<SoundReactiveSurface>();
            if (surface != null)
                color = surface.ResponseColor;
        }

        private static bool IsSourceCollider(Collider collider, GameObject source)
        {
            return source != null && collider.transform.IsChildOf(source.transform);
        }
    }
}
