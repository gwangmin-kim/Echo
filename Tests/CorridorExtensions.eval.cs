// Run outside Play Mode: unity command eval_file Tests/CorridorExtensions.eval.cs --format json
if(Application.isPlaying) throw new Exception("Run outside Play Mode.");
var oldScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var oldSelection=Selection.objects;
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
int cases=0,rays=0,triangles=0;
void Assert(bool value,string message){if(!value)throw new Exception(message);}
void Set(Echo.LevelDesign.CorridorModule m,string key,float value){var s=new SerializedObject(m);s.FindProperty(key).floatValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
void Flag(Echo.LevelDesign.CorridorModule m,string key,bool value){var s=new SerializedObject(m);s.FindProperty(key).boolValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
void Meshes(Echo.LevelDesign.CorridorModule m)
{
    foreach(var filter in m.GetComponentsInChildren<MeshFilter>())
    {
        var mesh=filter.sharedMesh;Assert(mesh!=null&&mesh.vertexCount>0,"missing mesh");
        Assert(filter.GetComponent<MeshCollider>().sharedMesh==mesh,"collider mismatch");
        var v=mesh.vertices;var t=mesh.triangles;
        for(int i=0;i<t.Length;i+=3){var normal=Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]);Assert(normal.sqrMagnitude>1e-8f&&!float.IsNaN(normal.x),"invalid triangle");triangles++;}
    }
}
try
{
    var four=Echo.LevelDesign.Editor.CorridorModuleBuilder.Create(Echo.LevelDesign.CorridorKind.FourWay);
    foreach(var angles in new[]{new[]{90f,270f,0f},new[]{60f,240f,0f},new[]{-60f,420f,0f},new[]{120f,240f,0f}})
    {Set(four,"exitAYaw",angles[0]);Set(four,"exitBYaw",angles[1]);Set(four,"exitCYaw",angles[2]);Assert(four.TryValidate(out _),"60 boundary rejected");cases++;}
    // Cover every pair: 0/1, 0/2, 0/3, 1/2, 1/3, 2/3, and wrap-around.
    foreach(var angles in new[]{new[]{150f,270f,0f},new[]{90f,210f,0f},new[]{90f,270f,150f},new[]{90f,100f,0f},new[]{90f,270f,100f},new[]{90f,270f,280f},new[]{0f,90f,359f},new[]{120.001f,240f,0f},new[]{90f,270f,float.NaN}})
    {
        Set(four,"exitAYaw",angles[0]);Set(four,"exitBYaw",angles[1]);Set(four,"exitCYaw",angles[2]);Assert(!four.TryValidate(out _),"invalid fourway accepted");
        var before=four.GeneratedRoot;bool rejected=false;try{Echo.LevelDesign.Editor.CorridorModuleBuilder.Rebuild(four);}catch(InvalidOperationException){rejected=true;}
        Assert(rejected&&four.GeneratedRoot==before,"invalid generation changed existing geometry");cases++;
    }
    for(int a=0;a<360;a+=30)for(int b=a+30;b<360;b+=30)for(int c=b+30;c<360;c+=30)
    {
        Set(four,"exitAYaw",a);Set(four,"exitBYaw",b);Set(four,"exitCYaw",c);
        if(!four.TryValidate(out _))continue;
        Echo.LevelDesign.Editor.CorridorModuleBuilder.Rebuild(four);Meshes(four);
        var ports=four.GetComponentsInChildren<Echo.LevelDesign.CorridorPort>();Assert(ports.Length==4,"fourway port count");
        var floor=four.GeneratedRoot.Find("Floor").GetComponent<MeshCollider>();
        foreach(var port in ports)
        {
            for(int step=0;step<=10;step++)
            {
                var point=Vector3.Lerp(Vector3.zero,port.transform.position,step*0.099f);
                Assert(floor.Raycast(new Ray(point+Vector3.up,Vector3.down),out _,2),"junction floor gap");rays++;
            }
            var ray=new Ray(port.transform.position+port.transform.forward*0.1f+Vector3.up,-port.transform.forward);
            Assert(!four.GetComponentsInChildren<MeshCollider>().Any(x=>x.Raycast(ray,out _,0.2f)),"fourway opening blocked");rays++;
        }
        cases++;
    }
    UnityEngine.Object.DestroyImmediate(four.gameObject);
    foreach(Echo.LevelDesign.CorridorKind kind in Enum.GetValues(typeof(Echo.LevelDesign.CorridorKind)))
    {
        var m=Echo.LevelDesign.Editor.CorridorModuleBuilder.Create(kind);
        var detail=new GameObject("Detail sentinel");detail.transform.SetParent(m.transform.Find("Details"),false);
        foreach(bool walls in new[]{true,false})foreach(bool ceiling in new[]{true,false})
        foreach(float slope in kind==Echo.LevelDesign.CorridorKind.Straight?new[]{-60f,-30f,0f,15f,45f,60f}:new[]{30f})
        {
            Flag(m,"walls",walls);Flag(m,"ceiling",ceiling);Set(m,"slopeAngle",slope);
            Echo.LevelDesign.Editor.CorridorModuleBuilder.Rebuild(m);Meshes(m);
            Assert(detail!=null&&detail.transform.parent==m.transform.Find("Details"),"detail lost");
            bool cap=kind==Echo.LevelDesign.CorridorKind.EndCap;
            Assert((m.GeneratedRoot.Find("Walls")!=null)==(walls||cap),"walls toggle");
            Assert((m.GeneratedRoot.Find("Ceiling")!=null)==(ceiling&&!cap),"ceiling toggle");
            if(!cap)
            {
                var floor=m.GeneratedRoot.Find("Floor").GetComponent<MeshCollider>();
                bool straight=kind==Echo.LevelDesign.CorridorKind.Straight;
                float k=straight?(float)Math.Tan(slope*Math.PI/180):0;
                foreach(var port in m.GetComponentsInChildren<Echo.LevelDesign.CorridorPort>())Assert(Vector3.Dot(port.transform.up,Vector3.up)>.99999f&&Mathf.Abs(port.transform.forward.y)<1e-5,"tilted port");
                for(int i=1;i<10;i++)
                {
                    float z=straight?m.Length*i/10f:0;
                    var point=new Vector3(0,z*k,z);
                    Assert(floor.Raycast(new Ray(point+Vector3.up,Vector3.down),out var hit,2),"ramp floor gap");rays++;
                    Assert(Mathf.Abs(hit.point.y-point.y)<.0002f,"incorrect ramp elevation");
                    Assert(Vector3.Dot(hit.normal,new Vector3(0,1,-k).normalized)>.999f,"incorrect slope normal");
                    if(ceiling)
                    {Assert(m.GeneratedRoot.Find("Ceiling").GetComponent<MeshCollider>().Raycast(new Ray(point+Vector3.up*(m.Height-.1f),Vector3.up),out var roofHit,.2f),"missing ceiling underside");Assert(Mathf.Abs(roofHit.point.y-point.y-m.Height)<.0002f,"incorrect vertical clearance");rays++;}
                }
                if(straight)
                {
                    foreach(var filter in m.GetComponentsInChildren<MeshFilter>())foreach(var v in filter.sharedMesh.vertices)
                    {
                        if(Mathf.Abs(v.z)>.0001f&&Mathf.Abs(v.z-m.Length)>.0001f)continue;
                        float y=v.y-v.z*k;
                        Assert(new[]{-m.Thickness,0,m.Height,m.Height+m.Thickness}.Any(expected=>Mathf.Abs(expected-y)<.0002f),"end face not vertical");
                    }
                    var sideRay=new Ray(new Vector3(0,m.Length*.5f*k+m.Height*.5f,m.Length*.5f),Vector3.right);
                    bool blocked=m.GetComponentsInChildren<MeshCollider>().Any(x=>x.Raycast(sideRay,out _,m.Width));
                    Assert(blocked==walls,"side collider did not follow walls toggle");rays++;
                }
            }
            cases++;
        }
        UnityEngine.Object.DestroyImmediate(m.gameObject);
    }
    var ramp=Echo.LevelDesign.Editor.CorridorModuleBuilder.Create(Echo.LevelDesign.CorridorKind.Straight);
    foreach(float bad in new[]{-90f,90f,120f,float.NaN,float.PositiveInfinity}){Set(ramp,"slopeAngle",bad);Assert(!ramp.TryValidate(out _),"invalid slope accepted");cases++;}
    Set(ramp,"slopeAngle",30);Echo.LevelDesign.Editor.CorridorModuleBuilder.Rebuild(ramp);
    ramp.transform.SetPositionAndRotation(new Vector3(8,2,14),Quaternion.Euler(0,37,0));
    var level=Echo.LevelDesign.Editor.CorridorModuleBuilder.Create(Echo.LevelDesign.CorridorKind.Straight);
    var moving=level.GetComponentsInChildren<Echo.LevelDesign.CorridorPort>()[0];var destination=ramp.GetComponentsInChildren<Echo.LevelDesign.CorridorPort>()[1];
    Echo.LevelDesign.Editor.CorridorModuleBuilder.Snap(moving,destination);
    Assert(Vector3.Distance(moving.transform.position,destination.transform.position)<.0001f,"ramp snap height");
    Assert(Vector3.Dot(level.transform.up,Vector3.up)>.99999f,"ramp tilted connected corridor");
    Assert(Mathf.Abs(level.transform.position.y-(2+8*(float)Math.Tan(Math.PI/6)))<.0001f,"wrong slope rise");cases++;
    Set(ramp,"slopeAngle",-15);Flag(ramp,"walls",false);Echo.LevelDesign.Editor.CorridorModuleBuilder.Rebuild(ramp);
    Undo.FlushUndoRecordObjects();Undo.PerformUndo();Assert(ramp.GeneratedRoot.Find("Walls")!=null,"Undo walls lost");
    Undo.PerformRedo();Assert(ramp.GeneratedRoot.Find("Walls")==null&&!ramp.NeedsRebuild,"Redo geometry stale");Meshes(ramp);cases++;
}
finally
{
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(oldScene);
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
    Selection.objects=oldSelection;
}
return $"PASS: {cases} extension cases, {rays} raycasts, {triangles} nondegenerate triangles. Four-way constraints, slopes, walls/ceiling combinations, upright connections, Details and Undo/Redo.";
