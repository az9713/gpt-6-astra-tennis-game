using System;
using System.IO;
using System.Linq;
using UnityEngine;
using RoboOpen;
public static class MotionDiagnostics
{
    public static void Run()
    {
        var actor=UnityEngine.Object.FindObjectsByType<RobotActor>().First(x=>!x.isCpu);actor.Initialize();
        var skin=actor.model.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();skin.BakeMesh(baked);
        var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath,"Prototype/Models/robot-basecolor.png")));
        var vertices=baked.vertices;var uv=skin.sharedMesh.uv;Vector3 sum=Vector3.zero;int count=0;
        for(int i=0;i<uv.Length;i++)
        {
            var c=texture.GetPixelBilinear(uv[i].x,uv[i].y);var position=skin.transform.TransformPoint(vertices[i]);
            if(position.y>1.1f&&c.b>c.r*1.5f&&c.g>c.r*1.5f&&c.b>.25f){sum+=position;count++;}
        }
        var shoulder=actor.model.GetComponentsInChildren<Transform>().First(x=>x.name=="UpperArm.R");
        var result=new Result{eyeLocal=actor.transform.InverseTransformPoint(sum/Mathf.Max(count,1)),rightShoulderLocal=actor.transform.InverseTransformPoint(shoulder.position),eyeSamples=count,modelEuler=actor.model.localEulerAngles};
        string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Evidence/Motion/orientation.json"));File.WriteAllText(path,JsonUtility.ToJson(result,true));
        Debug.Log("MOTION_ORIENTATION "+JsonUtility.ToJson(result));UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(baked);
        if(count==0||result.eyeLocal.z<.15f||result.rightShoulderLocal.x<.1f)throw new Exception("Robot face and racket shoulder do not match court orientation.");
    }
    [Serializable] class Result{public Vector3 eyeLocal,rightShoulderLocal,modelEuler;public int eyeSamples;}
}
