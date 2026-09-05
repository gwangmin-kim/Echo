using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

namespace Echo.LevelDesign.Editor
{
    public static class CorridorModuleBuilder
    {
        public static bool CanEdit(CorridorModule module, out string error)
        {
            if (Application.isPlaying) { error = "Edit modules outside Play Mode."; return false; }
            if (EditorUtility.IsPersistent(module)) { error = "Open this prefab in Prefab Mode to edit its geometry."; return false; }
            if (PrefabUtility.IsPartOfPrefabInstance(module))
            { error = "Open the source prefab, or unpack this module instance before rebuilding independently."; return false; }
            if (module.transform.lossyScale != Vector3.one)
            { error = "Use unit scale on the module and its parents. Change dimensions instead of Transform Scale."; return false; }
            error = null;
            return true;
        }

        public static void Rebuild(CorridorModule module)
        {
            if (!CanEdit(module, out string error) || !module.TryValidate(out error)) throw new InvalidOperationException(error);
            var outline = CorridorGeometry.Create(module);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Rebuild Corridor Module");
            try
            {
                var generated = new GameObject("Generated");
                SceneManager.MoveGameObjectToScene(generated, module.gameObject.scene);
                generated.transform.SetParent(module.transform, false);
                Undo.RegisterCreatedObjectUndo(generated, "Create corridor geometry");
                var floor = new MeshData();
                var walls = new MeshData();
                var ceiling = new MeshData();
                if (module.Kind == CorridorKind.EndCap)
                {
                    float h = module.Width * 0.5f + module.Thickness;
                    walls.Box(new Vector3(-h, -module.Thickness, 0), new Vector3(h, module.Height + (module.Ceiling ? module.Thickness : 0), module.Thickness));
                }
                else
                {
                    floor.Slab(outline.Outer, outline.Center, -module.Thickness, 0f);
                    if (module.Ceiling) ceiling.Slab(outline.Outer, outline.Center, module.Height, module.Height + module.Thickness);
                    for (int i = 0; i < outline.Inner.Count; i++)
                    {
                        if (outline.OpenEdges.Contains(i)) continue;
                        int j = (i + 1) % outline.Inner.Count;
                        walls.Prism(new[] { outline.Inner[i], outline.Inner[j], outline.Outer[j], outline.Outer[i] }, 0f, module.Height);
                    }
                }
                CreateMesh("Floor", floor, generated.transform, module);
                CreateMesh("Walls", walls, generated.transform, module);
                CreateMesh("Ceiling", ceiling, generated.transform, module);
                var ports = new GameObject("Ports");
                ports.transform.SetParent(generated.transform, false);
                for (int i = 0; i < outline.PortYaws.Length; i++)
                {
                    var port = new GameObject("Port " + i).AddComponent<CorridorPort>();
                    port.transform.SetParent(ports.transform, false);
                    port.transform.localPosition = outline.PortPositions[i];
                    port.transform.localRotation = Quaternion.Euler(0, outline.PortYaws[i], 0);
                    port.SetDimensions(module.Width, module.Height);
                }
                if (module.GeneratedRoot != null) Undo.DestroyObjectImmediate(module.GeneratedRoot.gameObject);
                Undo.RecordObject(module, "Update corridor generation");
                module.SetGeneratedRoot(generated.transform);
                if (module.transform.Find("Details") == null)
                {
                    var details = new GameObject("Details");
                    details.transform.SetParent(module.transform, false);
                    Undo.RegisterCreatedObjectUndo(details, "Create details container");
                }
                EditorUtility.SetDirty(module);
                EditorSceneManager.MarkSceneDirty(module.gameObject.scene);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        private static void CreateMesh(string name, MeshData data, Transform parent, CorridorModule module)
        {
            if (data.Vertices.Count == 0) return;
            var mesh = ProBuilderMesh.Create(data.Vertices, data.Faces);
            mesh.name = name;
            mesh.transform.SetParent(parent, false);
            mesh.ToMesh();
            mesh.Refresh();
            var renderer = mesh.GetComponent<MeshRenderer>();
            if (module.SurfaceMaterial != null) renderer.sharedMaterial = module.SurfaceMaterial;
            var collider = mesh.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh.GetComponent<MeshFilter>().sharedMesh;
            mesh.gameObject.AddComponent<Echo.Gameplay.SoundReactiveSurface>();
        }

        public static void Snap(CorridorPort moving, CorridorPort destination)
        {
            if (moving == null || destination == null) throw new ArgumentException("Choose both ports.");
            var module = moving.GetComponentInParent<CorridorModule>();
            var other = destination.GetComponentInParent<CorridorModule>();
            if (module == null || other == null || module == other) throw new ArgumentException("Ports must belong to different modules.");
            if (module.NeedsRebuild || other.NeedsRebuild) throw new InvalidOperationException("Rebuild both modules before connecting.");
            if (Application.isPlaying || EditorUtility.IsPersistent(module) || EditorUtility.IsPersistent(other))
                throw new InvalidOperationException("Connect scene instances outside Play Mode.");
            if (module.gameObject.scene != other.gameObject.scene) throw new InvalidOperationException("Ports must be in the same scene.");
            if (module.transform.lossyScale != Vector3.one || other.transform.lossyScale != Vector3.one)
                throw new InvalidOperationException("Both module hierarchies must use unit scale.");
            if (!Mathf.Approximately(moving.Width, destination.Width) || !Mathf.Approximately(moving.Height, destination.Height))
                throw new InvalidOperationException("Opening widths and heights must match.");
            Undo.RecordObject(module.transform, "Connect Corridor Ports");
            module.transform.rotation = destination.transform.rotation * Quaternion.Euler(0, 180, 0) * Quaternion.Inverse(moving.transform.rotation) * module.transform.rotation;
            module.transform.position += destination.transform.position - moving.transform.position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(module.transform);
            EditorSceneManager.MarkSceneDirty(module.gameObject.scene);
        }

        [MenuItem("GameObject/Echo/Corridors/Straight", false, 10)]
        private static void Straight() => Create(CorridorKind.Straight);
        [MenuItem("GameObject/Echo/Corridors/End Cap", false, 11)]
        private static void EndCap() => Create(CorridorKind.EndCap);
        [MenuItem("GameObject/Echo/Corridors/Corner", false, 12)]
        private static void Corner() => Create(CorridorKind.Corner);
        [MenuItem("GameObject/Echo/Corridors/Three Way", false, 13)]
        private static void ThreeWay() => Create(CorridorKind.ThreeWay);

        public static CorridorModule Create(CorridorKind kind)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Create modules outside Play Mode.");
            var go = new GameObject("Corridor " + kind);
            StageUtility.PlaceGameObjectInCurrentStage(go);
            Undo.RegisterCreatedObjectUndo(go, "Create Corridor Module");
            var module = go.AddComponent<CorridorModule>();
            var settings = new SerializedObject(module);
            settings.FindProperty("kind").enumValueIndex = (int)kind;
            settings.FindProperty("surfaceMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arts/Materials/SoundReactiveSurface.mat");
            settings.ApplyModifiedPropertiesWithoutUndo();
            Rebuild(module);
            Selection.activeGameObject = go;
            return module;
        }

        private sealed class MeshData
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Face> Faces = new List<Face>();
            private void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0) { var t = b; b = c; c = t; }
                int n = Vertices.Count;
                Vertices.AddRange(new[] { a, b, c });
                Faces.Add(new Face(new[] { n, n + 1, n + 2 }));
            }
            private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0) { var t = b; b = d; d = t; }
                int n = Vertices.Count;
                Vertices.AddRange(new[] { a, b, c, d });
                Faces.Add(new Face(new[] { n, n + 1, n + 2, n, n + 2, n + 3 }));
            }
            public void Slab(IReadOnlyList<Vector3> outline, Vector3 center, float bottom, float top)
            {
                for (int i = 0; i < outline.Count; i++)
                {
                    Vector3 a = outline[i], b = outline[(i + 1) % outline.Count];
                    Triangle(center + Vector3.up * top, a + Vector3.up * top, b + Vector3.up * top, Vector3.up);
                    Triangle(center + Vector3.up * bottom, a + Vector3.up * bottom, b + Vector3.up * bottom, Vector3.down);
                    Quad(a + Vector3.up * bottom, b + Vector3.up * bottom, b + Vector3.up * top, a + Vector3.up * top, Vector3.Cross(b - a, Vector3.up));
                }
            }
            public void Prism(Vector3[] outline, float bottom, float top)
            {
                Vector3 center = Vector3.zero;
                foreach (var point in outline) center += point;
                center /= outline.Length;
                // Wall strips can have either winding. Face normals are chosen from the solid's center.
                Quad(outline[0] + Vector3.up * top, outline[1] + Vector3.up * top, outline[2] + Vector3.up * top, outline[3] + Vector3.up * top, Vector3.up);
                Quad(outline[0] + Vector3.up * bottom, outline[1] + Vector3.up * bottom, outline[2] + Vector3.up * bottom, outline[3] + Vector3.up * bottom, Vector3.down);
                for (int i = 0; i < outline.Length; i++)
                {
                    Vector3 a = outline[i], b = outline[(i + 1) % outline.Length];
                    Quad(a + Vector3.up * bottom, b + Vector3.up * bottom, b + Vector3.up * top, a + Vector3.up * top, (a + b) * 0.5f - center);
                }
            }
            public void Box(Vector3 min, Vector3 max) => Prism(new[] { new Vector3(min.x, 0, min.z), new Vector3(min.x, 0, max.z), new Vector3(max.x, 0, max.z), new Vector3(max.x, 0, min.z) }, min.y, max.y);
        }
    }
}
