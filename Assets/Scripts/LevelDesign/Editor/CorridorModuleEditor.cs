using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Echo.LevelDesign.Editor
{
    [CustomEditor(typeof(CorridorModule))]
    public sealed class CorridorModuleEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var module = (CorridorModule)target;
            root.Add(new HelpBox("Dimensions are clear interior metres. Port 0 faces local -Z. Other yaws are clockwise from +Z. Every pair must be at least 60°. Place props under Details; rebuilding replaces Generated.", HelpBoxMessageType.Info));
            var fields = new VisualElement();
            root.Add(fields);
            Add(fields, "kind", "Module Type");
            Add(fields, "width", "Interior Width (m)");
            Add(fields, "height", "Interior Height (m)");
            Add(fields, "thickness", "Wall / Slab Thickness (m)");
            var length = Add(fields, "length", "Straight Length (m)");
            var arm = Add(fields, "armLength", "Arm Beyond Joint (m)");
            var yawA = Add(fields, "exitAYaw", "Port 1 Yaw (degrees)");
            var yawB = Add(fields, "exitBYaw", "Port 2 Yaw (degrees)");
            Add(fields, "ceiling", "Include Ceiling");
            Add(fields, "surfaceMaterial", "Surface Material");
            var status = new HelpBox();
            root.Add(status);
            var rebuild = new Button(() => { serializedObject.ApplyModifiedProperties(); CorridorModuleBuilder.Rebuild(module); }) { text = "Rebuild Geometry" };
            root.Add(rebuild);
            var unpack = new Button(() =>
            {
                PrefabUtility.UnpackPrefabInstance(module.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
            }) { text = "Unpack Module for Independent Editing" };
            root.Add(unpack);
            var detach = new Button(() =>
            {
                Undo.DestroyObjectImmediate(module);
            }) { text = "Keep Geometry and Remove Generator" };
            root.Add(new HelpBox("For manual ProBuilder edits, remove the generator first. Geometry and ports remain; parameter editing ends. Undo restores the generator.", HelpBoxMessageType.Info));
            root.Add(detach);
            void Refresh()
            {
                if (module == null) return;
                bool junction = module.Kind == CorridorKind.Corner || module.Kind == CorridorKind.ThreeWay;
                length.style.display = module.Kind == CorridorKind.Straight ? DisplayStyle.Flex : DisplayStyle.None;
                arm.style.display = yawA.style.display = junction ? DisplayStyle.Flex : DisplayStyle.None;
                yawB.style.display = module.Kind == CorridorKind.ThreeWay ? DisplayStyle.Flex : DisplayStyle.None;
                bool valid = module.TryValidate(out string error);
                bool editable = CorridorModuleBuilder.CanEdit(module, out string editError);
                rebuild.SetEnabled(valid && editable);
                detach.SetEnabled(editable);
                unpack.style.display = PrefabUtility.IsAnyPrefabInstanceRoot(module.gameObject) && !EditorUtility.IsPersistent(module) ? DisplayStyle.Flex : DisplayStyle.None;
                unpack.SetEnabled(!Application.isPlaying);
                status.text = !valid ? error : !editable ? editError : module.NeedsRebuild ? "Settings changed. Rebuild to update geometry and ports." : "Geometry is up to date. All port angles are valid.";
                status.messageType = !valid ? HelpBoxMessageType.Error : module.NeedsRebuild || !editable ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info;
            }
            root.TrackSerializedObjectValue(serializedObject, _ => Refresh());
            root.schedule.Execute(Refresh).Every(300);
            Refresh();
            return root;
        }

        private PropertyField Add(VisualElement parent, string property, string label)
        {
            var field = new PropertyField(serializedObject.FindProperty(property), label);
            parent.Add(field);
            return field;
        }

        private void OnSceneGUI()
        {
            var module = (CorridorModule)target;
            if (module.GeneratedRoot == null) return;
            Handles.color = Color.cyan;
            foreach (var port in module.GeneratedRoot.GetComponentsInChildren<CorridorPort>())
            {
                Handles.Label(port.transform.position + Vector3.up * 0.15f, port.name);
                Handles.ArrowHandleCap(0, port.transform.position, port.transform.rotation, 1f, EventType.Repaint);
                Vector3 left = port.transform.position - port.transform.right * port.Width * 0.5f;
                Vector3 right = port.transform.position + port.transform.right * port.Width * 0.5f;
                Handles.DrawLine(left, right);
                Handles.DrawLine(left, left + port.transform.up * port.Height);
                Handles.DrawLine(right, right + port.transform.up * port.Height);
            }
        }
    }

    [CustomEditor(typeof(CorridorPort))]
    public sealed class CorridorPortEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var port = (CorridorPort)target;
            root.Add(new HelpBox($"Opening: {port.Width:0.##} × {port.Height:0.##} m. This port points out of its module. Choose a destination to move this entire module into alignment.", HelpBoxMessageType.Info));
            var destination = new ObjectField("Destination Port") { objectType = typeof(CorridorPort), allowSceneObjects = true };
            root.Add(destination);
            var result = new HelpBox("Select a port on another module.", HelpBoxMessageType.Info);
            root.Add(result);
            root.Add(new Button(() =>
            {
                try { CorridorModuleBuilder.Snap(port, destination.value as CorridorPort); result.text = "Connected."; result.messageType = HelpBoxMessageType.Info; }
                catch (System.Exception error) { result.text = error.Message; result.messageType = HelpBoxMessageType.Error; }
            }) { text = "Move This Module to Destination" });
            return root;
        }
    }
}
