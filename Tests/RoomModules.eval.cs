// Run outside Play Mode: unity command eval_file Tests/RoomModules.eval.cs 30000 --format json
if(Application.isPlaying)throw new Exception("Run outside Play Mode");
var oldScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var selected=Selection.objects;
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
int cases=0,rays=0;
void Assert(bool v,string message){if(!v)throw new Exception(message);}
bool Hit(GameObject go,Ray ray,float distance){rays++;return go.GetComponentsInChildren<MeshCollider>().Any(c=>c.Raycast(ray,out _,distance));}
try
{
    var room=Echo.LevelDesign.Editor.RoomModuleBuilder.Create();var s=room.Settings;
    var detail=new GameObject("Sentinel");detail.transform.SetParent(room.transform.Find("Details"),false);
    for(int mask=0;mask<16;mask++)foreach(bool roof in new[]{true,false})
    {
        s.width=10;s.depth=8;s.height=4;s.ceiling=roof;
        for(int i=0;i<4;i++)s.Wall((Echo.LevelDesign.RoomSide)i).mode=(mask&(1<<i))!=0?Echo.LevelDesign.RoomWallMode.Solid:Echo.LevelDesign.RoomWallMode.Open;
        Echo.LevelDesign.Editor.RoomModuleBuilder.Rebuild(room);Physics.SyncTransforms();
        Assert(detail!=null&&detail.transform.parent.name=="Details","lost detail");
        Assert(!room.NeedsRebuild,"stale rebuild");
        float xmin=-5-((mask&2)!=0?.25f:0),xmax=5+((mask&1)!=0?.25f:0),zmin=0-((mask&8)!=0?.25f:0),zmax=8+((mask&4)!=0?.25f:0);
        var bounds=room.GeneratedRoot.Find("Floor").GetComponent<MeshRenderer>().bounds;
        Assert(Vector3.Distance(bounds.min,new Vector3(xmin,-.25f,zmin))<.0001f&&Vector3.Distance(bounds.max,new Vector3(xmax,0,zmax))<.0001f,"floor extends across open boundary");
        foreach(var r in room.GetComponentsInChildren<MeshRenderer>())
            Assert(r.bounds.min.x>=xmin-.0001f&&r.bounds.max.x<=xmax+.0001f&&r.bounds.min.z>=zmin-.0001f&&r.bounds.max.z<=zmax+.0001f,"wall overhang at open side");
        for(int i=0;i<4;i++)
        {
            var direction=i==0?Vector3.right:i==1?Vector3.left:i==2?Vector3.forward:Vector3.back;
            Assert(Hit(room.gameObject,new Ray(new Vector3(0,2,4),direction),7)==((mask&(1<<i))!=0),"wall state does not match collider");
        }
        Assert((room.GeneratedRoot.Find("Ceiling")!=null)==roof,"ceiling toggle");
        Assert(Hit(room.gameObject,new Ray(new Vector3(0,1,4),Vector3.down),2),"missing floor");
        cases++;
    }
    s.width=10;s.depth=8;s.height=4;s.ceiling=true;
    foreach(Echo.LevelDesign.RoomSide side in Enum.GetValues(typeof(Echo.LevelDesign.RoomSide)))
    foreach(int variation in new[]{0,1,2,3})
    {
        for(int i=0;i<4;i++)s.Wall((Echo.LevelDesign.RoomSide)i).mode=Echo.LevelDesign.RoomWallMode.Solid;
        bool alongZ=(int)side<2;float span=alongZ?s.depth:s.width;float sign=side==Echo.LevelDesign.RoomSide.PositiveX||side==Echo.LevelDesign.RoomSide.PositiveZ?1:-1;
        var wall=s.Wall(side);wall.mode=Echo.LevelDesign.RoomWallMode.Doorway;wall.openingWidth=variation==3?span:3;wall.openingHeight=variation==3?4:2;wall.horizontalOffset=variation==1?span*.5f-1.5f:variation==2?-span*.5f+1.5f:0;wall.bottomHeight=variation==0?1:0;
        Echo.LevelDesign.Editor.RoomModuleBuilder.Rebuild(room);Physics.SyncTransforms();
        Vector3 direction=alongZ?Vector3.right*sign:Vector3.forward*sign;
        Vector3 point=alongZ?new Vector3(0,wall.bottomHeight+wall.openingHeight*.5f,wall.horizontalOffset):new Vector3(wall.horizontalOffset,wall.bottomHeight+wall.openingHeight*.5f,0);
        point.z += s.depth * .5f;
        Assert(!Hit(room.gameObject,new Ray(point,direction),(alongZ?5:4)+.5f),"doorway blocked");
        if(wall.bottomHeight>0)Assert(Hit(room.gameObject,new Ray(new Vector3(point.x,.5f,point.z),direction),6),"missing sill");
        var port=room.GetComponentsInChildren<Echo.LevelDesign.CorridorPort>().Single();
        Assert(Mathf.Abs(Vector3.Dot(port.transform.position-Vector3.forward*s.depth*.5f,direction)-(alongZ?5.25f:4.25f))<.0001f,"port not on outer wall face");
        Assert(Mathf.Abs(port.transform.position.y-wall.bottomHeight)<.0001f,"port wrong elevation");
        var before=room.GeneratedRoot;wall.openingWidth=span+.001f;Assert(!room.TryValidate(out _),"oversized opening accepted");
        bool rejected=false;try{Echo.LevelDesign.Editor.RoomModuleBuilder.Rebuild(room);}catch(InvalidOperationException){rejected=true;}
        Assert(rejected&&room.GeneratedRoot==before,"invalid rebuild replaced geometry");wall.openingWidth=3;wall.horizontalOffset=0;cases++;
    }
    for(int i=0;i<4;i++)s.Wall((Echo.LevelDesign.RoomSide)i).mode=Echo.LevelDesign.RoomWallMode.Open;
    s.width=12;s.depth=6;s.height=7;Echo.LevelDesign.Editor.RoomModuleBuilder.Rebuild(room);Physics.SyncTransforms();
    var floor=room.GeneratedRoot.Find("Floor").GetComponent<MeshRenderer>().bounds;
    Assert(floor.max.y==0&&Mathf.Abs(floor.center.x)<.0001f&&Mathf.Abs(floor.min.z)<.0001f&&Mathf.Abs(floor.max.z-6)<.0001f,"pivot moved during resize");cases++;
    s.negativeZ.mode=Echo.LevelDesign.RoomWallMode.Solid;
    foreach(float depth in new[]{4f,12f,20f})
    {
        s.depth=depth;Echo.LevelDesign.Editor.RoomModuleBuilder.Rebuild(room);Physics.SyncTransforms();
        var wallBounds=room.GeneratedRoot.Find("NegativeZ/Wall").GetComponent<MeshRenderer>().bounds;
        Assert(Mathf.Abs(wallBounds.min.z+.25f)<.0001f&&Mathf.Abs(wallBounds.max.z)<.0001f,"negative Z wall moved with depth");cases++;
    }
    s.width=10;s.depth=10;s.height=3;
    for(int i=0;i<4;i++)s.Wall((Echo.LevelDesign.RoomSide)i).mode=Echo.LevelDesign.RoomWallMode.Solid;
    s.positiveX.mode=Echo.LevelDesign.RoomWallMode.Doorway;s.positiveX.openingWidth=3;s.positiveX.openingHeight=3;s.positiveX.bottomHeight=0;s.positiveX.horizontalOffset=1;
    Echo.LevelDesign.Editor.RoomModuleBuilder.Rebuild(room);
    var corridor=Echo.LevelDesign.Editor.CorridorModuleBuilder.Create(Echo.LevelDesign.CorridorKind.Straight);
    var destination=room.GetComponentsInChildren<Echo.LevelDesign.CorridorPort>().Single();var moving=corridor.GetComponentsInChildren<Echo.LevelDesign.CorridorPort>()[0];
    Echo.LevelDesign.Editor.CorridorModuleBuilder.Snap(moving,destination);Physics.SyncTransforms();
    Assert(Vector3.Distance(moving.transform.position,destination.transform.position)<.0001f,"corridor to room snap");
    foreach(float step in new[]{-.24f,-.1f,.01f,.1f})
    {var p=destination.transform.position+destination.transform.forward*step+Vector3.up;Assert(Hit(room.gameObject,new Ray(p,Vector3.down),2)||Hit(corridor.gameObject,new Ray(p,Vector3.down),2),"gap across doorway thickness");}
    cases++;
    UnityEngine.Object.DestroyImmediate(corridor.gameObject);
    s.positiveX.mode=Echo.LevelDesign.RoomWallMode.Open;Echo.LevelDesign.Editor.RoomModuleBuilder.Rebuild(room);
    var other=Echo.LevelDesign.Editor.RoomModuleBuilder.Create();other.Settings.height=3;other.Settings.negativeX.mode=Echo.LevelDesign.RoomWallMode.Open;Echo.LevelDesign.Editor.RoomModuleBuilder.Rebuild(other);
    Echo.LevelDesign.Editor.CorridorModuleBuilder.Snap(other.GetComponentsInChildren<Echo.LevelDesign.CorridorPort>().Single(),room.GetComponentsInChildren<Echo.LevelDesign.CorridorPort>().Single());Physics.SyncTransforms();
    Assert(Mathf.Abs(other.transform.position.x-10)<.0001f,"room to room alignment");
    foreach(float dx in new[]{-.01f,.01f})Assert(Hit(room.gameObject,new Ray(new Vector3(5+dx,1,5),Vector3.down),2)||Hit(other.gameObject,new Ray(new Vector3(5+dx,1,5),Vector3.down),2),"room seam gap");cases++;
    s.ceiling=false;Echo.LevelDesign.Editor.RoomModuleBuilder.Rebuild(room);Undo.FlushUndoRecordObjects();Undo.PerformUndo();Assert(room.GeneratedRoot.Find("Ceiling")!=null,"Undo roof");Undo.PerformRedo();Assert(room.GeneratedRoot.Find("Ceiling")==null&&!room.NeedsRebuild,"Redo roof");cases++;
}
finally{UnityEngine.SceneManagement.SceneManager.SetActiveScene(oldScene);UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);Selection.objects=selected;}
return $"PASS: {cases} room cases, {rays} raycasts. Wall boundaries, doorway edges/elevation, negative-Z anchored resize, detail preservation, room/corridor seams and Undo/Redo.";
