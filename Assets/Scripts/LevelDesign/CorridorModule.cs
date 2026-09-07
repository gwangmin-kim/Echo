using System;
using UnityEngine;

namespace Echo.LevelDesign
{
    public enum CorridorKind { Straight, EndCap, Corner, ThreeWay, FourWay }

    [DisallowMultipleComponent]
    public sealed class CorridorModule : MonoBehaviour
    {
        public const float CornerMinimumAngle = 60f;
        public const float BranchMinimumAngle = 30f;
        public float MinimumAngle => kind == CorridorKind.ThreeWay || kind == CorridorKind.FourWay
            ? BranchMinimumAngle : CornerMinimumAngle;
        [SerializeField] private CorridorKind kind;
        [SerializeField] private float width = 3f;
        [SerializeField] private float height = 3f;
        [SerializeField] private float length = 8f;
        [SerializeField] private float thickness = 0.25f;
        [SerializeField] private float armLength = 2f;
        [SerializeField] private float exitAYaw = 90f;
        [SerializeField] private float exitBYaw = 270f;
        [SerializeField] private float exitCYaw;
        [SerializeField] private float slopeAngle;
        [SerializeField] private bool walls = true;
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
        public bool Walls => walls;
        public bool IsJunction => kind == CorridorKind.Corner || kind == CorridorKind.ThreeWay || kind == CorridorKind.FourWay;
        public float SlopeAngle => kind == CorridorKind.Straight ? slopeAngle : 0f;
        public float SlopeRisePerMeter => (float)Math.Tan(SlopeAngle * Math.PI / 180.0);
        public float ExitElevation => kind == CorridorKind.Straight ? length * SlopeRisePerMeter : 0f;
        public Material SurfaceMaterial => surfaceMaterial;
        public Transform GeneratedRoot => generatedRoot;
        public bool NeedsRebuild => generatedRoot == null || builtSettings != SettingsKey || builtMaterial != surfaceMaterial;
        private string SettingsKey => string.Join("|", (int)kind, width.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            height.ToString("R", System.Globalization.CultureInfo.InvariantCulture), length.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            thickness.ToString("R", System.Globalization.CultureInfo.InvariantCulture), armLength.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            exitAYaw.ToString("R", System.Globalization.CultureInfo.InvariantCulture), exitBYaw.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            ceiling) + ExtensionSettingsKey;

        // Keep the original key for unchanged legacy modules; new defaults do not invalidate their geometry.
        private string ExtensionSettingsKey =>
            (kind == CorridorKind.FourWay ? "|port3=" + exitCYaw.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "") +
            (kind == CorridorKind.Straight && slopeAngle != 0f ? "|slope=" + slopeAngle.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "") +
            (kind != CorridorKind.EndCap && !walls ? "|walls=False" : "");

        public float[] GetPortYaws()
        {
            return kind switch
            {
                CorridorKind.Straight => new[] { 180f, 0f },
                CorridorKind.EndCap => new[] { 180f },
                CorridorKind.Corner => new[] { 180f, Mathf.Repeat(exitAYaw, 360f) },
                CorridorKind.ThreeWay => new[] { 180f, Mathf.Repeat(exitAYaw, 360f), Mathf.Repeat(exitBYaw, 360f) },
                CorridorKind.FourWay => new[] { 180f, Mathf.Repeat(exitAYaw, 360f), Mathf.Repeat(exitBYaw, 360f), Mathf.Repeat(exitCYaw, 360f) },
                _ => Array.Empty<float>(),
            };
        }

        public bool TryValidate(out string error)
        {
            if (!Enum.IsDefined(typeof(CorridorKind), kind)) { error = "Unknown module type."; return false; }
            if (!Positive(width) || !Positive(height) || !Positive(thickness) ||
                (kind == CorridorKind.Straight && !Positive(length)) ||
                (IsJunction && !Positive(armLength)))
            { error = "Dimensions must be finite and at least 0.05 m."; return false; }
            if (kind == CorridorKind.Straight && (!Finite(slopeAngle) || Mathf.Abs(slopeAngle) >= 90f || !Finite(ExitElevation)))
            { error = "Slope must be finite, greater than -90 and less than 90 degrees, with a finite exit elevation."; return false; }
            if (IsJunction)
            {
                if (!Finite(exitAYaw) || (kind != CorridorKind.Corner && !Finite(exitBYaw)) ||
                    (kind == CorridorKind.FourWay && !Finite(exitCYaw)))
                { error = "Angles must be finite."; return false; }
                var yaws = GetPortYaws();
                for (int i = 0; i < yaws.Length; i++)
                    for (int j = i + 1; j < yaws.Length; j++)
                    {
                        float angle = Mathf.Abs(Mathf.DeltaAngle(yaws[i], yaws[j]));
                        if (angle < MinimumAngle)
                        { error = $"Port {i} / Port {j}: {angle:0.###} degrees. Every pair must be at least {MinimumAngle:0} degrees."; return false; }
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
