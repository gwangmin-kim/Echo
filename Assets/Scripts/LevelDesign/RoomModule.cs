using System;
using UnityEngine;

namespace Echo.LevelDesign
{
    public enum RoomWallMode { Open, Solid, Doorway }
    public enum RoomSide { PositiveX, NegativeX, PositiveZ, NegativeZ }

    [Serializable]
    public sealed class RoomWallSettings
    {
        public RoomWallMode mode = RoomWallMode.Solid;
        public float openingWidth = 3f;
        public float openingHeight = 3f;
        public float horizontalOffset;
        public float bottomHeight;
    }

    [Serializable]
    public sealed class RoomSettings
    {
        public float width = 10f;
        public float depth = 10f;
        public float height = 4f;
        public float thickness = 0.25f;
        public bool ceiling = true;
        public RoomWallSettings positiveX = new();
        public RoomWallSettings negativeX = new();
        public RoomWallSettings positiveZ = new();
        public RoomWallSettings negativeZ = new();

        public RoomWallSettings Wall(RoomSide side) => side switch
        {
            RoomSide.PositiveX => positiveX,
            RoomSide.NegativeX => negativeX,
            RoomSide.PositiveZ => positiveZ,
            _ => negativeZ
        };
    }

    [DisallowMultipleComponent]
    public sealed class RoomModule : MonoBehaviour
    {
        [SerializeField] private RoomSettings settings = new();
        [SerializeField] private Material surfaceMaterial;
        [SerializeField, HideInInspector] private Transform generatedRoot;
        [SerializeField, HideInInspector] private string builtSettings;
        [SerializeField, HideInInspector] private Material builtMaterial;
        public RoomSettings Settings => settings;
        public Material SurfaceMaterial => surfaceMaterial;
        public Transform GeneratedRoot => generatedRoot;
        public bool NeedsRebuild => generatedRoot == null || builtSettings != SettingsKey || builtMaterial != surfaceMaterial;

        private string SettingsKey => "NegativeZPivot|" + JsonUtility.ToJson(settings);

        public bool TryValidate(out string error)
        {
            if (settings == null || !Positive(settings.width) || !Positive(settings.depth) || !Positive(settings.height) || !Positive(settings.thickness))
            { error = "Room dimensions must be finite and at least 0.05 m."; return false; }
            foreach (RoomSide side in Enum.GetValues(typeof(RoomSide)))
            {
                var wall = settings.Wall(side);
                if (wall == null || !Enum.IsDefined(typeof(RoomWallMode), wall.mode))
                { error = $"{side}: invalid wall settings."; return false; }
                if (wall.mode != RoomWallMode.Doorway) continue;
                float span = side == RoomSide.PositiveX || side == RoomSide.NegativeX ? settings.depth : settings.width;
                if (!Positive(wall.openingWidth) || !Positive(wall.openingHeight) || !Finite(wall.horizontalOffset) || !Finite(wall.bottomHeight) || wall.bottomHeight < 0f ||
                    Math.Abs((double)wall.horizontalOffset) + wall.openingWidth * 0.5 > span * 0.5 || (double)wall.bottomHeight + wall.openingHeight > settings.height)
                { error = $"{side}: opening must fit inside the wall (span {span:0.###} m, height {settings.height:0.###} m)."; return false; }
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
