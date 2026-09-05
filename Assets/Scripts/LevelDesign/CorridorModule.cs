using System;
using UnityEngine;

namespace Echo.LevelDesign
{
    public enum CorridorKind { Straight, EndCap, Corner, ThreeWay }

    [DisallowMultipleComponent]
    public sealed class CorridorModule : MonoBehaviour
    {
        public const float MinimumAngle = 60f;
        [SerializeField] private CorridorKind kind;
        [SerializeField] private float width = 3f;
        [SerializeField] private float height = 3f;
        [SerializeField] private float length = 8f;
        [SerializeField] private float thickness = 0.25f;
        [SerializeField] private float armLength = 2f;
        [SerializeField] private float exitAYaw = 90f;
        [SerializeField] private float exitBYaw = 270f;
        [SerializeField] private bool ceiling = true;
        [SerializeField] private Material surfaceMaterial;
        [SerializeField, HideInInspector] private Transform generatedRoot;
        [SerializeField, HideInInspector] private string builtSettings;
        [SerializeField, HideInInspector] private Material builtMaterial;

        public CorridorKind Kind => kind;
        public float Width => width;
        public float Height => height;
        public float Length => length;
        public float Thickness => thickness;
        public float ArmLength => armLength;
        public bool Ceiling => ceiling;
        public Material SurfaceMaterial => surfaceMaterial;
        public Transform GeneratedRoot => generatedRoot;
        public bool NeedsRebuild => generatedRoot == null || builtSettings != SettingsKey || builtMaterial != surfaceMaterial;
        private string SettingsKey => string.Join("|", (int)kind, width.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            height.ToString("R", System.Globalization.CultureInfo.InvariantCulture), length.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            thickness.ToString("R", System.Globalization.CultureInfo.InvariantCulture), armLength.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            exitAYaw.ToString("R", System.Globalization.CultureInfo.InvariantCulture), exitBYaw.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            ceiling);

        public float[] GetPortYaws()
        {
            switch (kind)
            {
                case CorridorKind.Straight: return new[] { 180f, 0f };
                case CorridorKind.EndCap: return new[] { 180f };
                case CorridorKind.Corner: return new[] { 180f, Mathf.Repeat(exitAYaw, 360f) };
                default: return new[] { 180f, Mathf.Repeat(exitAYaw, 360f), Mathf.Repeat(exitBYaw, 360f) };
            }
        }

        public bool TryValidate(out string error)
        {
            if (!Enum.IsDefined(typeof(CorridorKind), kind)) { error = "Unknown module type."; return false; }
            if (!Positive(width) || !Positive(height) || !Positive(thickness) ||
                (kind == CorridorKind.Straight && !Positive(length)) ||
                ((kind == CorridorKind.Corner || kind == CorridorKind.ThreeWay) && !Positive(armLength)))
            { error = "Dimensions must be finite and at least 0.05 m."; return false; }
            if (kind == CorridorKind.Corner || kind == CorridorKind.ThreeWay)
            {
                if (!Finite(exitAYaw) || (kind == CorridorKind.ThreeWay && !Finite(exitBYaw)))
                { error = "Angles must be finite."; return false; }
                var yaws = GetPortYaws();
                for (int i = 0; i < yaws.Length; i++)
                    for (int j = i + 1; j < yaws.Length; j++)
                    {
                        float angle = Mathf.Abs(Mathf.DeltaAngle(yaws[i], yaws[j]));
                        if (angle < MinimumAngle)
                        { error = $"Port {i} / Port {j}: {angle:0.###} degrees. Every pair must be at least 60 degrees."; return false; }
                    }
            }
            error = null;
            return true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Positive(float value) => Finite(value) && value >= 0.05f;

#if UNITY_EDITOR
        public void SetGeneratedRoot(Transform root)
        {
            generatedRoot = root;
            builtSettings = SettingsKey;
            builtMaterial = surfaceMaterial;
        }
#endif
    }
}
