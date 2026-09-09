using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

namespace Echo.LevelDesign.Editor
{
    public static class RoomModuleBuilder
    {
        public static bool CanEdit(RoomModule module, out string error)
        {
            if (Application.isPlaying) { error = "Edit rooms outside Play Mode."; return false; }
            if (EditorUtility.IsPersistent(module)) { error = "Open this prefab in Prefab Mode to edit its geometry."; return false; }
            if (PrefabUtility.IsPartOfPrefabInstance(module)) { error = "Open the source prefab or unpack this room before rebuilding independently."; return false; }
            if (module.transform.lossyScale != Vector3.one) { error = "Use unit scale on the room and its parents; edit dimensions instead."; return false; }
            error = null;
            return true;
        }

        public static void Rebuild(RoomModule module)
        {
            if (!CanEdit(module, out string error) || !module.TryValidate(out error)) throw new InvalidOperationException(error);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Rebuild Room Module");
            try
            {
                var root = new GameObject("Generated");
                SceneManager.MoveGameObjectToScene(root, module.gameObject.scene);
                root.transform.SetParent(module.transform, false);
                Undo.RegisterCreatedObjectUndo(root, "Create room geometry");
                var s = module.Settings;
                root.transform.localPosition = Vector3.forward * (s.depth * 0.5f);
                float x = s.width * 0.5f, z = s.depth * 0.5f, t = s.thickness;
                float xMin = -x - (s.negativeX.mode == RoomWallMode.Open ? 0 : t);
                float xMax = x + (s.positiveX.mode == RoomWallMode.Open ? 0 : t);
                float zMin = -z - (s.negativeZ.mode == RoomWallMode.Open ? 0 : t);
                float zMax = z + (s.positiveZ.mode == RoomWallMode.Open ? 0 : t);
                Box("Floor", new Vector3(xMin, -t, zMin), new Vector3(xMax, 0, zMax), root.transform, module);
                if (s.ceiling) Box("Ceiling", new Vector3(xMin, s.height, zMin), new Vector3(xMax, s.height + t, zMax), root.transform, module);
                var ports = new GameObject("Ports");
                ports.transform.SetParent(root.transform, false);
                foreach (RoomSide side in Enum.GetValues(typeof(RoomSide)))
                {
                    var wall = s.Wall(side);
                    bool alongZ = side == RoomSide.PositiveX || side == RoomSide.NegativeX;
                    float span = alongZ ? s.depth : s.width;
                    float sign = side == RoomSide.PositiveX || side == RoomSide.PositiveZ ? 1 : -1;
                    float inner = sign * (alongZ ? x : z);
                    if (wall.mode != RoomWallMode.Open)
                    {
                        var wallRoot = new GameObject(side.ToString());
                        wallRoot.transform.SetParent(root.transform, false);
                        // Z walls own corner volumes; X walls end at the interior Z boundary.
                        float low = alongZ ? -z : xMin, high = alongZ ? z : xMax;
                        void Panel(string name, float a, float b, float bottom, float top)
                        {
                            if (b <= a || top <= bottom) return;
                            float outside = inner + sign * t;
                            Vector3 min = alongZ ? new Vector3(Mathf.Min(inner, outside), bottom, a) : new Vector3(a, bottom, Mathf.Min(inner, outside));
                            Vector3 max = alongZ ? new Vector3(Mathf.Max(inner, outside), top, b) : new Vector3(b, top, Mathf.Max(inner, outside));
                            Box(name, min, max, wallRoot.transform, module);
                        }
                        if (wall.mode == RoomWallMode.Solid) Panel("Wall", low, high, 0, s.height);
                        else
                        {
                            float a = wall.horizontalOffset - wall.openingWidth * 0.5f;
                            float b = wall.horizontalOffset + wall.openingWidth * 0.5f;
                            Panel("Negative Side", low, a, 0, s.height);
                            Panel("Positive Side", b, high, 0, s.height);
                            Panel("Below Opening", a, b, 0, wall.bottomHeight);
                            Panel("Above Opening", a, b, wall.bottomHeight + wall.openingHeight, s.height);
                        }
                    }
                    if (wall.mode == RoomWallMode.Solid) continue;
                    bool doorway = wall.mode == RoomWallMode.Doorway;
                    float normal = inner + (doorway ? sign * t : 0);
                    float offset = doorway ? wall.horizontalOffset : 0;
                    float bottomHeight = doorway ? wall.bottomHeight : 0;
                    var port = new GameObject(side + " Port").AddComponent<CorridorPort>();
                    port.transform.SetParent(ports.transform, false);
                    port.transform.localPosition = alongZ ? new Vector3(normal, bottomHeight, offset) : new Vector3(offset, bottomHeight, normal);
                    port.transform.localRotation = Quaternion.LookRotation(alongZ ? Vector3.right * sign : Vector3.forward * sign);
                    port.SetDimensions(doorway ? wall.openingWidth : span, doorway ? wall.openingHeight : s.height);
                }
                if (module.GeneratedRoot != null) Undo.DestroyObjectImmediate(module.GeneratedRoot.gameObject);
                Undo.RecordObject(module, "Update room generation");
                module.SetGeneratedRoot(root.transform);
                if (module.transform.Find("Details") == null)
                {
                    var details = new GameObject("Details");
                    details.transform.SetParent(module.transform, false);
                    Undo.RegisterCreatedObjectUndo(details, "Create room details");
                }
                EditorUtility.SetDirty(module);
                EditorSceneManager.MarkSceneDirty(module.gameObject.scene);
                Undo.CollapseUndoOperations(group);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        private static void Box(string name, Vector3 min, Vector3 max, Transform parent, RoomModule module)
        {
            var mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, max - min);
            mesh.name = name;
            mesh.transform.SetParent(parent, false);
            mesh.transform.localPosition = (min + max) * 0.5f;
            if (module.SurfaceMaterial != null) mesh.GetComponent<MeshRenderer>().sharedMaterial = module.SurfaceMaterial;
            var collider = mesh.GetComponent<MeshCollider>();
            if (collider == null) collider = mesh.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh.GetComponent<MeshFilter>().sharedMesh;
            mesh.gameObject.AddComponent<Echo.Gameplay.SoundReactiveSurface>();
        }

        [MenuItem("GameObject/Echo/Rooms/Rectangular Room", false, 20)]
        public static void CreateMenu() => Create();

        public static RoomModule Create()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Create rooms outside Play Mode.");
            var go = new GameObject("Rectangular Room");
            StageUtility.PlaceGameObjectInCurrentStage(go);
            Undo.RegisterCreatedObjectUndo(go, "Create Room Module");
            var module = go.AddComponent<RoomModule>();
            var settings = new SerializedObject(module);
            settings.FindProperty("surfaceMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arts/Materials/SoundReactiveSurface.mat");
            settings.ApplyModifiedPropertiesWithoutUndo();
            Rebuild(module);
            Selection.activeGameObject = go;
            return module;
        }
    }
}
