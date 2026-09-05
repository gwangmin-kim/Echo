// Run outside Play Mode: unity command eval_file Tests/CorridorModules.eval.cs --format json
// Pipeline eval uses method-body snippets, so namespace imports must be fully qualified.

var oldScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
var reports = new List<string>();
int cases = 0, rays = 0, triangles = 0;
void Assert(bool value, string text) { if (!value) throw new Exception(text); }
void Set(Echo.LevelDesign.CorridorModule m, string key, float value) { var s = new SerializedObject(m); s.FindProperty(key).floatValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
try
{
    foreach (Echo.LevelDesign.CorridorKind kind in Enum.GetValues(typeof(Echo.LevelDesign.CorridorKind)))
    {
        var m = Echo.LevelDesign.Editor.CorridorModuleBuilder.Create(kind);
        Assert(!m.NeedsRebuild, "fresh module marked stale");
        Assert(m.GetComponentsInChildren<Echo.LevelDesign.CorridorPort>().Length == (kind == Echo.LevelDesign.CorridorKind.EndCap ? 1 : kind == Echo.LevelDesign.CorridorKind.ThreeWay ? 3 : 2), "port count");
        var detail = new GameObject("Preserve me"); detail.transform.SetParent(m.transform.Find("Details"), false);
        Set(m, "width", 4);
        Assert(m.NeedsRebuild, "changed settings not detected");
        Echo.LevelDesign.Editor.CorridorModuleBuilder.Rebuild(m);
        Assert(detail != null && detail.transform.parent.name == "Details", "detail lost");
        Assert(!m.NeedsRebuild, "rebuilt stale");
        UnityEngine.Object.DestroyImmediate(m.gameObject);
        cases++;
    }
    var corner = Echo.LevelDesign.Editor.CorridorModuleBuilder.Create(Echo.LevelDesign.CorridorKind.Corner);
    foreach (float yaw in new[] { 120f, 240f, 0f, 360f, -120f })
    { Set(corner, "exitAYaw", yaw); Assert(corner.TryValidate(out _), "valid corner rejected " + yaw); cases++; }
    foreach (float yaw in new[] { 120.001f, 239.999f, 180f, float.NaN, float.PositiveInfinity })
    {
        Set(corner, "exitAYaw", yaw);
        Assert(!corner.TryValidate(out _), "invalid corner accepted " + yaw);
        var before = corner.GeneratedRoot;
        bool rejected = false; try { Echo.LevelDesign.Editor.CorridorModuleBuilder.Rebuild(corner); } catch (InvalidOperationException) { rejected = true; }
        Assert(rejected && before == corner.GeneratedRoot, "invalid rebuild changed geometry"); cases++;
    }
    UnityEngine.Object.DestroyImmediate(corner.gameObject);
    var junction = Echo.LevelDesign.Editor.CorridorModuleBuilder.Create(Echo.LevelDesign.CorridorKind.ThreeWay);
    foreach (var pair in new[] { new[] { 120f, 240f }, new[] { 0f, 60f }, new[] { 0f, 300f }, new[] { 90f, 270f }, new[] { -120f, 480f } })
    { Set(junction, "exitAYaw", pair[0]); Set(junction, "exitBYaw", pair[1]); Assert(junction.TryValidate(out _), "valid junction rejected"); cases++; }
    foreach (var pair in new[] { new[] { 0f, 359f }, new[] { 0f, 59.999f }, new[] { 0f, 300.001f }, new[] { 120.001f, 270f }, new[] { 90f, 239.999f }, new[] { 90f, 90f } })
    { Set(junction, "exitAYaw", pair[0]); Set(junction, "exitBYaw", pair[1]); Assert(!junction.TryValidate(out _), "invalid junction accepted"); cases++; }
    UnityEngine.Object.DestroyImmediate(junction.gameObject);

    foreach (Echo.LevelDesign.CorridorKind kind in new[] { Echo.LevelDesign.CorridorKind.Straight, Echo.LevelDesign.CorridorKind.Corner, Echo.LevelDesign.CorridorKind.ThreeWay, Echo.LevelDesign.CorridorKind.EndCap })
    {
        var m = Echo.LevelDesign.Editor.CorridorModuleBuilder.Create(kind);
        var combos = new List<Vector2> { new Vector2(90, 270) };
        if (kind == Echo.LevelDesign.CorridorKind.Corner) combos = new List<Vector2> { new Vector2(0,0), new Vector2(60,0), new Vector2(120,0), new Vector2(240,0), new Vector2(300,0) };
        if (kind == Echo.LevelDesign.CorridorKind.ThreeWay)
        {
            combos.Clear();
            for (int a = 0; a < 360; a += 15)
                for (int b = a + 15; b < 360; b += 15)
                { Set(m,"exitAYaw",a); Set(m,"exitBYaw",b); if (m.TryValidate(out _)) combos.Add(new Vector2(a,b)); }
        }
        foreach (var pair in combos)
        {
            Set(m,"exitAYaw",pair.x); Set(m,"exitBYaw",pair.y);
            Echo.LevelDesign.Editor.CorridorModuleBuilder.Rebuild(m);
            foreach (var mesh in m.GetComponentsInChildren<MeshFilter>())
            {
                var vertices = mesh.sharedMesh.vertices; var indices = mesh.sharedMesh.triangles;
                Assert(mesh.GetComponent<MeshCollider>().sharedMesh == mesh.sharedMesh, "collider mismatch");
                for (int k = 0; k < indices.Length; k += 3)
                {
                    var normal = Vector3.Cross(vertices[indices[k+1]]-vertices[indices[k]], vertices[indices[k+2]]-vertices[indices[k]]);
                    Assert(normal.sqrMagnitude > 0.00000001f && !float.IsNaN(normal.x), "degenerate triangle " + kind + pair); triangles++;
                }
            }
            var colliders = m.GetComponentsInChildren<MeshCollider>();
            var o = Echo.LevelDesign.Editor.CorridorGeometry.Create(m);
            if (kind != Echo.LevelDesign.CorridorKind.EndCap)
            {
                var floor = m.GeneratedRoot.Find("Floor").GetComponent<MeshCollider>();
                for (int p = 0; p < o.PortPositions.Length; p++)
                {
                    Vector3 inner = kind == Echo.LevelDesign.CorridorKind.Straight ? o.Center : Vector3.zero;
                    for (int step=0;step<=12;step++)
                    {
                        Vector3 point = Vector3.Lerp(inner, o.PortPositions[p], step/12f);
                        Assert(floor.Raycast(new Ray(point + Vector3.up, Vector3.down), out var hit, 2f), "missing floor " + kind + pair + " port " + p); rays++;
                        Assert(Mathf.Abs(hit.point.y) < 0.001f, "floor height");
                    }
                    Vector3 dir=Echo.LevelDesign.Editor.CorridorGeometry.Direction(o.PortYaws[p]);
                    var ray = new Ray(o.PortPositions[p] + dir*0.3f + Vector3.up, -dir);
                    Assert(!colliders.Any(c=>c.Raycast(ray,out _,0.6f)), "blocked opening " + kind + pair); rays++;
                }
                var wall = m.GeneratedRoot.Find("Walls").GetComponent<MeshCollider>();
                for (int e=0;e<o.Inner.Count;e++)
                {
                    if(o.OpenEdges.Contains(e))continue;
                    var a=o.Inner[e]; var b=o.Inner[(e+1)%o.Inner.Count];
                    var outward=Vector3.Cross(b-a,Vector3.up).normalized;
                    var ray=new Ray((a+b)*0.5f-outward*0.03f+Vector3.up,outward);
                    Assert(wall.Raycast(ray,out _,0.08f), "missing inner wall " + kind + pair); rays++;
                }
            }
            else Assert(colliders.Any(c=>c.Raycast(new Ray(new Vector3(0,1,-1),Vector3.forward),out _,2)), "end cap not blocking");
            cases++;
        }
        UnityEngine.Object.DestroyImmediate(m.gameObject);
    }
    var first = Echo.LevelDesign.Editor.CorridorModuleBuilder.Create(Echo.LevelDesign.CorridorKind.Straight);
    var second = Echo.LevelDesign.Editor.CorridorModuleBuilder.Create(Echo.LevelDesign.CorridorKind.Corner);
    first.transform.SetPositionAndRotation(new Vector3(10,2,20), Quaternion.Euler(0,37,0));
    var moving = second.GetComponentsInChildren<Echo.LevelDesign.CorridorPort>()[0];
    var targetPort=first.GetComponentsInChildren<Echo.LevelDesign.CorridorPort>()[1];
    Echo.LevelDesign.Editor.CorridorModuleBuilder.Snap(moving,targetPort);
    Assert(Vector3.Distance(moving.transform.position,targetPort.transform.position)<0.0001f,"snap position");
    Assert(Vector3.Dot(moving.transform.forward,targetPort.transform.forward)<-0.9999f,"snap direction");
    cases++;
    reports.Add($"PASS: {cases} cases, {triangles} nondegenerate triangles, {rays} floor/opening/wall raycasts; details preserved; invalid rebuild rejected; rotated snap aligned.");
}
finally
{
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(oldScene);
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
}
return reports;
