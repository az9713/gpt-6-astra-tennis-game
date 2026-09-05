using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RoboOpen;

public static class PrototypeSetup
{
    public const string ScenePath="Assets/Prototype/RoboOpen.unity",ModelPath="Assets/Prototype/Models/RoboPlayer.fbx";
    static string Root=>Directory.GetParent(Application.dataPath).Parent.FullName;
    static string Evidence=>Path.Combine(Root,"Evidence/Prototype");
    static Material clay,teal,cream,ink,orange,mint,wood,gold;
    static Font font;
    [MenuItem("Robo Open/Build prototype scene")]
    public static void Prepare()
    {
        Directory.CreateDirectory(Evidence);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var importer=AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if(importer==null)throw new Exception("Robot FBX is required: "+ModelPath);
        importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=true;importer.isReadable=true;importer.globalScale=1;
        importer.importNormals=ModelImporterNormals.Import;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        clay=Mat("Clay",new Color(.70f,.17f,.115f));teal=Mat("Teal",new Color(.025f,.40f,.35f));cream=Mat("Cream",new Color(.99f,.93f,.76f));ink=Mat("Ink",new Color(.045f,.125f,.15f));orange=Mat("Orange",new Color(1,.34f,.04f));mint=Mat("Mint",new Color(.13f,.78f,.67f));wood=Mat("Warm concrete",new Color(.64f,.49f,.28f));gold=Mat("Gold",new Color(.97f,.70f,.18f));
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Cube("Park ground",new Vector3(0,-.18f,10),new Vector3(90,.20f,85),Mat("Park green",new Color(.35f,.43f,.26f)));
        Cube("Island base",new Vector3(0,-.5f,1),new Vector3(35,.8f,43),wood);
        Cube("Club plaza",new Vector3(0,-.07f,0),new Vector3(28,.15f,35),teal);
        Cube("Coral playing surface",new Vector3(0,.012f,0),new Vector3(12.2f,.035f,27),clay);
        float w=CourtRules.HalfWidth,l=CourtRules.HalfLength;
        foreach(float x in new[]{-w,w,-5.48f,5.48f})Cube("Sideline",new Vector3(x,.04f,0),new Vector3(.075f,.014f,l*2),cream,false);
        foreach(float z in new[]{-l,l})Cube("Baseline",new Vector3(0,.042f,z),new Vector3(11,.014f,.085f),cream,false);
        foreach(float z in new[]{-CourtRules.ServiceLine,CourtRules.ServiceLine})Cube("Service line",new Vector3(0,.045f,z),new Vector3(w*2,.014f,.065f),cream,false);
        Cube("Service centre",new Vector3(0,.045f,0),new Vector3(.065f,.014f,12.8f),cream,false);
        foreach(float z in new[]{-l+.14f,l-.14f})Cube("Centre mark",new Vector3(0,.05f,z),new Vector3(.08f,.014f,.3f),cream,false);
        CourtText("RT",new Vector3(0,.07f,-4.1f),2.6f,new Color(1,.78f,.57f,.24f));
        CourtText("ROBO OPEN",new Vector3(0,.065f,-13f),.56f,cream.color);
        CourtText("ROBO OPEN",new Vector3(0,.065f,13f),.56f,cream.color,180);
        Net();Stadium();
        var cam=new GameObject("Main Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.position=new Vector3(0,14.4f,-25.8f);cam.transform.LookAt(new Vector3(0,.35f,-1.0f));cam.fieldOfView=51;cam.nearClipPlane=.15f;cam.farClipPlane=180;cam.backgroundColor=new Color(.92f,.66f,.40f);cam.clearFlags=CameraClearFlags.SolidColor;cam.gameObject.AddComponent<AudioListener>();cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var sun=new GameObject("Golden-hour sun").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(37,-42,0);sun.color=new Color(1,.84f,.63f);sun.intensity=1.55f;sun.shadows=LightShadows.Soft;sun.shadowBias=.035f;sun.shadowNormalBias=.28f;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.48f,.60f,.67f);RenderSettings.ambientEquatorColor=new Color(.34f,.40f,.39f);RenderSettings.ambientGroundColor=new Color(.21f,.24f,.20f);RenderSettings.ambientIntensity=1;RenderSettings.fog=true;RenderSettings.fogColor=new Color(.83f,.67f,.46f);RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=50;RenderSettings.fogEndDistance=140;
        var game=new GameObject("Robo Open Match").AddComponent<RoboOpen.TennisGame>();game.gameCamera=cam;
        game.player=Robot("You / Ember",false,new Vector3(1.4f,0,-10.1f));game.cpu=Robot("CPU / Mint",true,new Vector3(0,0,9.3f));
        var ball=Primitive("Tennis ball",PrimitiveType.Sphere,new Vector3(0,2,-9),Vector3.one*.24f,Mat("Tennis yellow",new Color(.88f,1f,.08f),"Universal Render Pipeline/Unlit"));game.ball=ball.transform;
        game.trail=ball.AddComponent<TrailRenderer>();game.trail.material=Mat("Ball trail",new Color(1,.87f,.38f),"Universal Render Pipeline/Unlit");game.trail.time=.23f;game.trail.startWidth=.10f;game.trail.endWidth=.015f;game.trail.minVertexDistance=.07f;game.trail.shadowCastingMode=ShadowCastingMode.Off;game.trail.emitting=false;
        game.landingMarker=Ring("Ball landing",.52f,.06f,Mat("Landing yellow",new Color(1f,.88f,.12f),"Universal Render Pipeline/Unlit"));game.aimMarker=Ring("Shot target",.40f,.055f,Mat("Aim teal",new Color(.12f,.95f,.78f),"Universal Render Pipeline/Unlit"));
        game.ballShadow=Primitive("Ball ground shadow",PrimitiveType.Sphere,Vector3.zero,new Vector3(.25f,.007f,.25f),Mat("Ball shadow",new Color(.14f,.16f,.09f))).transform;
        PlayerSettings.companyName="Local Tennis Project";PlayerSettings.productName="Robo Open";PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        ValidateRig(game.player);PrototypeChecks.Run();
        Debug.Log("ROBO_SCENE_READY "+ScenePath);
    }
    static RobotActor Robot(string name,bool cpu,Vector3 position)
    {
        var root=new GameObject(name);root.transform.position=position;root.transform.rotation=Quaternion.Euler(0,cpu?180:0,0);
        var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath),root.transform);model.name="Meshy robot / Blender animation";
        model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.Euler(0,180,0);
        var renderers=model.GetComponentsInChildren<Renderer>();
        var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        float scale=1.8f/bounds.size.y;model.transform.localScale=Vector3.one*scale;
        bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        model.transform.position+=Vector3.up*(position.y-bounds.min.y);
        foreach(var r in renderers)
        {
            r.sharedMaterials=r.sharedMaterials.Select(source=>
            {
                string path="Assets/Prototype/Materials/Robot-"+(cpu?"mint-":"orange-")+source.name+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(Shader.Find("RoboOpen/RobotTint"));AssetDatabase.CreateAsset(mat,path);}
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Prototype/Models/robot-basecolor.png");
                mat.SetTexture("_BaseMap",texture);mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_TintAmount",cpu?1:0);mat.SetColor("_TeamTint",new Color(.13f,.82f,.70f));return mat;
            }).ToArray();
        }
        var animation=model.GetComponent<Animation>()??model.AddComponent<Animation>();
        foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(x=>!x.name.StartsWith("__preview__")))animation.AddClip(clip,clip.name);
        var actor=root.AddComponent<RobotActor>();actor.model=model.transform;actor.isCpu=cpu;
        var racket=new GameObject("Racket").transform;racket.SetParent(root.transform,false);racket.localPosition=new Vector3(.67f,1,.26f);actor.racket=racket;
        Cube("Racket grip",new Vector3(0,0,0),new Vector3(.10f,.40f,.10f),ink).transform.SetParent(racket,false);
        Cube("Racket neck",new Vector3(0,.29f,0),new Vector3(.045f,.24f,.045f),cream).transform.SetParent(racket,false);
        var ring=Ring("Racket frame",.28f,.034f,cpu?mint:orange);ring.SetParent(racket,false);ring.localPosition=new Vector3(0,.65f,0);ring.localRotation=Quaternion.Euler(90,0,0);ring.localScale=new Vector3(1,1,1.25f);actor.racketHead=ring;
        for(int i=-3;i<=3;i++)
        {
            float x=i*.066f,extent=Mathf.Sqrt(.27f*.27f-x*x);
            Cube("Racket string",new Vector3(x,.65f,0),new Vector3(.009f,extent*2*.99f,.009f),cream,false).transform.SetParent(racket,false);
            Cube("Racket string",new Vector3(0,.65f+x,0),new Vector3(extent*2,.009f,.009f),cream,false).transform.SetParent(racket,false);
        }
        var hand=model.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Hand.R");
        if(hand!=null){racket.SetParent(hand,true);racket.position=hand.position;racket.rotation=root.transform.rotation*Quaternion.Euler(-15,0,15);}
        return actor;
    }
    static void ValidateRig(RobotActor actor)
    {
        var skins=actor.model.GetComponentsInChildren<SkinnedMeshRenderer>();if(skins.Length==0)throw new Exception("Robot has no skin");
        var clips=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        foreach(var name in new[]{"idle","run","serve","forehand","backhand"})if(!clips.Any(c=>c.name.ToLowerInvariant().Contains(name)))throw new Exception("Missing clip "+name);
        float delta=0;int boneCount=0,vertices=0;
        foreach(var skin in skins)
        {
            boneCount=Math.Max(boneCount,skin.bones.Length);vertices+=skin.sharedMesh.vertexCount;
            foreach(var weight in skin.sharedMesh.boneWeights)if(Mathf.Abs(weight.weight0+weight.weight1+weight.weight2+weight.weight3-1)>.01f)throw new Exception("Skin weights not normalized");
            var clip=clips.First(c=>c.name.ToLowerInvariant().Contains("forehand"));var bake=new Mesh();clip.SampleAnimation(actor.model.gameObject,0);skin.BakeMesh(bake);var a=bake.vertices;clip.SampleAnimation(actor.model.gameObject,.23f);skin.BakeMesh(bake);delta=Mathf.Max(delta,a.Zip(bake.vertices,(x,y)=>Vector3.Distance(x,y)).Max());UnityEngine.Object.DestroyImmediate(bake);clip.SampleAnimation(actor.model.gameObject,0);
        }
        if(delta<.01f)throw new Exception("Forehand did not deform skin");
        File.WriteAllText(Path.Combine(Evidence,"robot-import.json"),JsonUtility.ToJson(new RigReceipt{passed=true,bones=boneCount,vertices=vertices,clips=clips.Select(c=>c.name).ToArray(),forehandSkinDelta=delta},true));
    }
    [Serializable] class RigReceipt{public bool passed;public int bones,vertices;public string[] clips;public float forehandSkinDelta;}
    static void Net()
    {
        foreach(float x in new[]{-5.15f,5.15f})Primitive("Net post",PrimitiveType.Cylinder,new Vector3(x,.62f,0),new Vector3(.14f,.64f,.14f),ink);
        for(float x=-5.1f;x<=5.1f;x+=.16f)Cube("Net vertical mesh",new Vector3(x,.51f,0),new Vector3(.012f,.85f,.015f),ink,false);
        for(float y=.13f;y<=.9f;y+=.15f)Cube("Net horizontal mesh",new Vector3(0,y,0),new Vector3(10.3f,.014f,.018f),ink,false);
        Cube("Net white tape",new Vector3(0,CourtRules.NetHeight,0),new Vector3(10.4f,.075f,.075f),cream);
    }
    static void Stadium()
    {
        var random=new System.Random(81);var palettes=new[]{cream,mint,gold,orange,Mat("Spectator blue",new Color(.19f,.39f,.57f)),Mat("Spectator lilac",new Color(.62f,.42f,.62f))};
        for(int side=-1;side<=1;side+=2)
        {
            Cube("Sponsor rail",new Vector3(side*7.3f,.48f,1),new Vector3(.25f,.95f,29),ink);
            for(int row=0;row<4;row++)
            {
                float x=side*(8.6f+row*.92f),y=.3f+row*.62f;
                Cube("Stepped bleacher",new Vector3(x,y/2,1),new Vector3(.94f,y,28),wood);
                Cube("Bench",new Vector3(x,y+.12f,1),new Vector3(.55f,.19f,27),cream);
                for(int seat=0;seat<20;seat++)
                {
                    float z=-11.5f+seat*1.28f+(row%2)*.22f;var mat=palettes[random.Next(palettes.Length)];
                    Cube("Spectator shirt",new Vector3(x,y+.52f,z),new Vector3(.35f,.46f,.40f),mat,false);
                    Primitive("Spectator head",PrimitiveType.Sphere,new Vector3(x,y+.90f,z),Vector3.one*.30f,random.NextDouble()>.5?cream:gold,false);
                }
            }
            for(int z=-10;z<=10;z+=10)
            {
                var plaque=Cube("Sponsor panel",new Vector3(side*7.13f,.56f,z),new Vector3(.03f,.55f,3),z==0?gold:cream);
            }
            foreach(float z in new[]{-10.5f,11f})
            {
                Primitive("Floodlight pole",PrimitiveType.Cylinder,new Vector3(side*7.7f,3.7f,z),new Vector3(.11f,3.7f,.11f),ink);
                Cube("Floodlight crossbar",new Vector3(side*7.7f,7.35f,z),new Vector3(1.55f,.12f,.12f),ink);
                foreach(float dx in new[]{-.48f,0,.48f})Cube("Warm floodlight",new Vector3(side*7.7f+dx,7.26f,z),new Vector3(.33f,.32f,.18f),cream);
            }
        }
        Cube("Far stadium wall",new Vector3(0,.65f,15.5f),new Vector3(25,1.3f,.4f),ink);
        for(int row=0;row<3;row++)
        {
            float z=16.2f+row*.9f,y=.55f+row*.63f;Cube("Far bleachers",new Vector3(0,y/2,z),new Vector3(25,y,.95f),wood);
            for(int col=-10;col<=10;col++){Cube("Far spectator",new Vector3(col*1.1f,y+.38f,z),new Vector3(.38f,.55f,.35f),palettes[random.Next(palettes.Length)],false);Primitive("Far head",PrimitiveType.Sphere,new Vector3(col*1.1f,y+.8f,z),Vector3.one*.30f,cream,false);}
        }
        WallText("ROBO OPEN",new Vector3(0,1.4f,15.2f),1.05f,cream.color);
        for(int i=0;i<15;i++)
        {
            float x=-24+i*3.5f,z=23+(float)random.NextDouble()*10,h=3+(float)random.NextDouble()*3;
            Primitive("Tree trunk",PrimitiveType.Cylinder,new Vector3(x,h/2,z),new Vector3(.35f,h/2,.35f),wood);
            Primitive("Round tree canopy",PrimitiveType.Sphere,new Vector3(x,h+1,z),new Vector3(3.6f,4.3f,3.4f),i%3==0?gold:teal);
        }
        // Side bench and umpire chair establish scale without obscuring the playable court.
        Cube("Players bench",new Vector3(-6.65f,.38f,-5),new Vector3(.55f,.16f,3),cream);
        Cube("Umpire chair platform",new Vector3(6.2f,1.9f,0),new Vector3(.75f,.16f,.75f),cream);
        foreach(float z in new[]{-.3f,.3f})Cube("Umpire chair legs",new Vector3(6.2f,1,z),new Vector3(.07f,2,.07f),ink);
    }
    static Material Mat(string name,Color color,string shader="Universal Render Pipeline/Lit")
    {
        Directory.CreateDirectory("Assets/Prototype/Materials");string path="Assets/Prototype/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",color);if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",.2f);return mat;
    }
    static GameObject Primitive(string name,PrimitiveType type,Vector3 p,Vector3 s,Material m,bool shadows=true)
    {var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.position=p;g.transform.localScale=s;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());var r=g.GetComponent<Renderer>();r.sharedMaterial=m;r.shadowCastingMode=shadows?ShadowCastingMode.On:ShadowCastingMode.Off;return g;}
    static GameObject Cube(string name,Vector3 p,Vector3 s,Material m,bool shadows=true)=>Primitive(name,PrimitiveType.Cube,p,s,m,shadows);
    static Transform Ring(string name,float radius,float thickness,Material m)
    {
        var g=new GameObject(name);var line=g.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=48;line.widthMultiplier=thickness;line.sharedMaterial=m;line.shadowCastingMode=ShadowCastingMode.Off;
        for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}return g.transform;
    }
    static void CourtText(string text,Vector3 p,float size,Color color,float y=0){var t=WallText(text,p,size,color);t.transform.rotation=Quaternion.Euler(90,y,0);}
    static TextMesh WallText(string value,Vector3 p,float size,Color color)
    {var t=new GameObject(value).AddComponent<TextMesh>();t.transform.position=p;t.text=value;t.font=font;t.fontSize=64;t.characterSize=size/6;t.color=color;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.fontStyle=FontStyle.Bold;t.GetComponent<MeshRenderer>().sharedMaterial=font.material;t.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;return t;}
    public static void BuildWindows()
    {
        Directory.CreateDirectory(Evidence);
        string path=Path.Combine(Root,"Builds/RoboOpen-Windows/RoboOpen.exe");Directory.CreateDirectory(Path.GetDirectoryName(path));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        File.WriteAllText(Path.Combine(Evidence,"windows-build.json"),JsonUtility.ToJson(new BuildReceipt{result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,seconds=report.summary.totalTime.TotalSeconds},true));
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Prototype build failed");
    }
    [Serializable] class BuildReceipt{public string result;public int errors,warnings;public double seconds;}
}
