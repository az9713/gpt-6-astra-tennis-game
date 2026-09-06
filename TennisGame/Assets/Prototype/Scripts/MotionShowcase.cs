using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RoboOpen
{
    // A separate launch mode exposes the real game rig and animations at useful scale.
    public class MotionShowcase : MonoBehaviour
    {
        public TennisGame game;
        string output;
        bool recording;
        Text label;
        readonly List<LineRenderer> lines=new List<LineRenderer>();
        readonly List<Transform> bones=new List<Transform>();
        IEnumerator Start()
        {
            yield return null;
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--motion-output");
            if(index>=0&&index+1<args.Length){output=args[index+1];recording=true;Directory.CreateDirectory(output);Time.captureFramerate=30;}
            game.enabled=false;
            foreach(var c in FindObjectsByType<Canvas>())c.gameObject.SetActive(false);
            game.cpu.gameObject.SetActive(false);game.player.transform.position=new Vector3(0,0,-6);
            game.player.transform.rotation=Quaternion.Euler(0,145,0);game.player.ResetMotion();
            game.ball.gameObject.SetActive(false);game.trail.enabled=false;game.landingMarker.gameObject.SetActive(false);game.aimMarker.gameObject.SetActive(false);game.ballShadow.gameObject.SetActive(false);
            game.gameCamera.transform.position=new Vector3(0,2.35f,-10.7f);game.gameCamera.transform.LookAt(new Vector3(0,1.20f,-6));game.gameCamera.fieldOfView=40;
            var panel=new GameObject("Motion studio labels",typeof(Canvas),typeof(CanvasScaler));var canvas=panel.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=game.gameCamera;canvas.planeDistance=.5f;
            var scaler=panel.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);
            var backdrop=new GameObject("Label background",typeof(RectTransform),typeof(Image));backdrop.transform.SetParent(panel.transform,false);backdrop.GetComponent<Image>().color=new Color(.035f,.12f,.12f,.94f);
            var br=backdrop.GetComponent<RectTransform>();br.anchorMin=br.anchorMax=br.pivot=new Vector2(0,1);br.anchoredPosition=new Vector2(24,-16);br.sizeDelta=new Vector2(710,142);
            var text=new GameObject("Stroke / frame",typeof(RectTransform),typeof(Text));text.transform.SetParent(panel.transform,false);label=text.GetComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=26;label.color=new Color(.99f,.97f,.88f);label.alignment=TextAnchor.UpperLeft;
            var rect=text.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(40,-30);rect.sizeDelta=new Vector2(1300,135);
            var material=new Material(Resources.Load<Shader>("RigGuide"));material.SetColor("_BaseColor",new Color(.2f,1,.87f,.85f));
            foreach(var bone in game.player.model.GetComponentsInChildren<Transform>())
            {
                if(bone.parent==null || !(bone.name.Contains("Arm")||bone.name.Contains("Forearm")||bone.name.Contains("Hand")||bone.name.Contains("Shin")||bone.name.Contains("Foot")||bone.name.Contains("Thigh")||bone.name=="Head"||bone.name=="Spine"))continue;
                var go=new GameObject("Rig guide");var line=go.AddComponent<LineRenderer>();line.material=material;line.positionCount=2;line.startWidth=line.endWidth=.009f;line.useWorldSpace=true;lines.Add(line);bones.Add(bone);
            }
            int frame=0;
            do
            {
                foreach(var kind in new[]{RobotActor.Stroke.Forehand,RobotActor.Stroke.Backhand,RobotActor.Stroke.Smash,RobotActor.Stroke.Serve})
                {
                    game.player.ResetMotion();float contact=RobotActor.ContactFrameSeconds(kind);
                    Vector3 target=game.player.transform.TransformPoint(kind==RobotActor.Stroke.Smash||kind==RobotActor.Stroke.Serve?new Vector3(.16f,2.22f,.24f):new Vector3(kind==RobotActor.Stroke.Backhand?-.58f:.88f,1.10f,.35f));
                    game.player.BeginStroke(kind,target,contact);bool captured=false;
                    for(int local=0;local<90;local++)
                    {
                        float seconds=local/30f;
                        label.text="ROBO OPEN  /  STRUCTURAL MOTION\n"+kind.ToString().ToUpperInvariant()+"  ·  CONTACT "+Mathf.RoundToInt(contact*30)+" / 30 FPS\nB: rig guides  ·  ESC: close";
                        yield return new WaitForEndOfFrame();
                        if(!captured&&seconds>=contact){game.Capture(Path.Combine(output??Application.temporaryCachePath,"motion-"+kind.ToString().ToLowerInvariant()+".png"));captured=true;}
                        if(recording)game.Capture(Path.Combine(output,"frame-"+frame.ToString("D4")+".png"));frame++;
                    }
                }
            }while(!recording);
            Time.captureFramerate=0;Application.Quit();
        }
        void Update()
        {
            if(Keyboard.current==null)return;if(Keyboard.current.escapeKey.wasPressedThisFrame)Application.Quit();
            if(Keyboard.current.bKey.wasPressedThisFrame)foreach(var line in lines)line.enabled=!line.enabled;
        }
        void LateUpdate()
        {
            for(int i=0;i<lines.Count;i++){lines[i].SetPosition(0,bones[i].position);lines[i].SetPosition(1,bones[i].parent.position);}
        }
    }
}
