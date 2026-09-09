using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Echo.LevelDesign.Editor
{
    [CustomEditor(typeof(RoomModule))]
    public sealed class RoomModuleEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var module = (RoomModule)target;
            var root = new VisualElement();
            root.Add(new HelpBox("Pivot: -Z interior wall floor centre (Y=0, Z=0). Width grows about X=0; depth grows toward +Z. Open sides end exactly at their interior boundary. Put props under Details; rebuilding replaces Generated.", HelpBoxMessageType.Info));
            void Field(VisualElement parent, string path, string label) => parent.Add(new PropertyField(serializedObject.FindProperty(path), label));
            Field(root, "settings.width", "Interior Width X (m)");
            Field(root, "settings.depth", "Interior Depth Z (m)");
            Field(root, "settings.height", "Interior Height (m)");
            Field(root, "settings.thickness", "Wall / Slab Thickness (m)");
            Field(root, "settings.ceiling", "Include Ceiling");
            Field(root, "surfaceMaterial", "Surface Material");
            var openings = new VisualElement[4];
            var paths = new[] { "positiveX", "negativeX", "positiveZ", "negativeZ" };
            var labels = new[] { "+X Wall (offset toward +Z)", "-X Wall (offset toward +Z)", "+Z Wall (offset toward +X)", "-Z Wall (offset toward +X)" };
            for (int i = 0; i < paths.Length; i++)
            {
                var foldout = new Foldout { text = labels[i], value = true };
                root.Add(foldout);
                string path = "settings." + paths[i];
                Field(foldout, path + ".mode", "Wall Mode");
                openings[i] = new VisualElement();
                foldout.Add(openings[i]);
                Field(openings[i], path + ".openingWidth", "Opening Width (m)");
                Field(openings[i], path + ".openingHeight", "Opening Height (m)");
                Field(openings[i], path + ".horizontalOffset", "Horizontal Centre Offset (m)");
                Field(openings[i], path + ".bottomHeight", "Opening Bottom Height (m)");
            }
            var status = new HelpBox();
            root.Add(status);
            var rebuild = new Button(() => { serializedObject.ApplyModifiedProperties(); RoomModuleBuilder.Rebuild(module); }) { text = "Rebuild Geometry" };
            root.Add(rebuild);
            var unpack = new Button(() => PrefabUtility.UnpackPrefabInstance(module.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction)) { text = "Unpack Room for Independent Editing" };
            root.Add(unpack);
            root.Add(new HelpBox("For manual ProBuilder editing, remove the generator first. Meshes remain, but parameter editing and module snapping end. Undo restores the generator.", HelpBoxMessageType.Info));
            var detach = new Button(() => Undo.DestroyObjectImmediate(module)) { text = "Keep Geometry and Remove Generator" };
            root.Add(detach);
            void Refresh()
            {
                if (module == null) return;
                for (int i = 0; i < openings.Length; i++)
                    openings[i].style.display = module.Settings.Wall((RoomSide)i)?.mode == RoomWallMode.Doorway ? DisplayStyle.Flex : DisplayStyle.None;
                bool valid = module.TryValidate(out string error);
                bool editable = RoomModuleBuilder.CanEdit(module, out string editError);
                rebuild.SetEnabled(valid && editable);
                detach.SetEnabled(editable);
                unpack.style.display = PrefabUtility.IsAnyPrefabInstanceRoot(module.gameObject) && !EditorUtility.IsPersistent(module) ? DisplayStyle.Flex : DisplayStyle.None;
                unpack.SetEnabled(!Application.isPlaying);
                status.text = !valid ? error : !editable ? editError : module.NeedsRebuild ? "Settings changed. Rebuild geometry, then save the scene or prefab." : "Geometry is up to date.";
                status.messageType = !valid ? HelpBoxMessageType.Error : !editable || module.NeedsRebuild ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info;
            }
            root.TrackSerializedObjectValue(serializedObject, _ => Refresh());
            root.schedule.Execute(Refresh).Every(300);
            Refresh();
            return root;
        }

        private void OnSceneGUI()
        {
            var module = (RoomModule)target;
            if (module.GeneratedRoot == null) return;
            Handles.color = Color.cyan;
            foreach (var port in module.GeneratedRoot.GetComponentsInChildren<CorridorPort>())
            {
                var t = port.transform;
                var left = t.position - t.right * port.Width * 0.5f;
                var right = t.position + t.right * port.Width * 0.5f;
                Handles.DrawLine(left, right);
                Handles.DrawLine(left, left + t.up * port.Height);
                Handles.DrawLine(right, right + t.up * port.Height);
                Handles.DrawLine(left + t.up * port.Height, right + t.up * port.Height);
                Handles.Label(t.position, port.name);
                Handles.ArrowHandleCap(0, t.position, t.rotation, 1f, EventType.Repaint);
            }
            var s = module.Settings;
            foreach (RoomSide side in System.Enum.GetValues(typeof(RoomSide)))
            {
                bool alongZ = side == RoomSide.PositiveX || side == RoomSide.NegativeX;
                float sign = side == RoomSide.PositiveX || side == RoomSide.PositiveZ ? 1 : -1;
                Vector3 point = alongZ ? new Vector3(sign * s.width * .5f, s.height * .5f, 0) : new Vector3(0, s.height * .5f, sign * s.depth * .5f);
                point.z += s.depth * .5f;
                Vector3 direction = module.transform.TransformDirection(alongZ ? Vector3.forward : Vector3.right);
                Handles.ArrowHandleCap(0, module.transform.TransformPoint(point), Quaternion.LookRotation(direction), 1f, EventType.Repaint);
            }
        }
    }
}
