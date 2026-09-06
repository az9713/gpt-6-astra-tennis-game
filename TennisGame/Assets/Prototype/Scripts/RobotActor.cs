using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RoboOpen
{
    public class RobotActor : MonoBehaviour
    {
        public enum Stroke { Forehand, Backhand, Smash, Serve }
        public Transform model, racket, racketHead;
        public Animation animationPlayer;
        public bool isCpu;
        public Stroke activeStroke;
        public bool IsStriking { get; private set; }
        public float LastContactError { get; private set; }
        public float StrokeTime => strokeTime;
        public Transform RightHand => hand;
        readonly Dictionary<string,AnimationClip> clips=new Dictionary<string,AnimationClip>();
        Transform upper,forearm,hand;
        float strokeTime,walkClock,speed,prepared,contactTime,lead,stepClock;
        Vector3 previous,contactTarget,startPosition,stepTarget;
        bool contactLocked;
        Vector3 modelPosition,modelScale;
        Quaternion modelRotation;
        public static float ContactFrameSeconds(Stroke stroke)=>(stroke==Stroke.Serve?42:stroke==Stroke.Smash?25:stroke==Stroke.Backhand?15:14)/30f;
        public void Initialize()
        {
            previous=transform.position;modelPosition=model.localPosition;modelRotation=model.localRotation;modelScale=model.localScale;animationPlayer=model.GetComponentInChildren<Animation>();
            if(animationPlayer!=null){foreach(AnimationState state in animationPlayer)clips[state.name]=state.clip;animationPlayer.Stop();animationPlayer.enabled=false;}
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
            var bones=model.GetComponentsInChildren<Transform>();upper=bones.First(t=>t.name=="UpperArm.R");forearm=bones.First(t=>t.name=="Forearm.R");hand=bones.First(t=>t.name=="Hand.R");ResetMotion();
        }
        AnimationClip Clip(string name)=>clips.FirstOrDefault(k=>k.Key.ToLowerInvariant().Contains(name.ToLowerInvariant())).Value;
        public void ResetMotion(){IsStriking=false;contactLocked=false;prepared=0;strokeTime=0;previous=transform.position;Play("idle",true);}
        public void Play(string name,bool loop=false){Sample(name,0);}
        void Sample(string name,float time)
        {
            var clip=Clip(name);if(clip==null)return;clip.SampleAnimation(model.gameObject,Mathf.Clamp(time,0,clip.length));
            model.localPosition=modelPosition;model.localRotation=modelRotation;model.localScale=modelScale;AlignRacket();
        }
        void AlignRacket()
        {
            if(hand==null||racket==null)return;racket.position=hand.position;
            Vector3 shaft=hand.up,normal=Vector3.ProjectOnPlane(transform.forward,shaft);
            if(normal.sqrMagnitude<.001f)normal=Vector3.ProjectOnPlane(transform.right,shaft);
            racket.rotation=Quaternion.LookRotation(normal.normalized,shaft);
        }
        public void Anticipate(Vector3 ball,bool incoming)
        {
            if(IsStriking)return;float distance=Vector3.Distance(new Vector3(ball.x,0,ball.z),transform.position);
            prepared=Mathf.MoveTowards(prepared,incoming&&distance<5?1:0,Time.deltaTime*4);activeStroke=ChooseStroke(ball);
        }
        public Stroke ChooseStroke(Vector3 position)
        {if(position.y>=1.92f)return Stroke.Smash;return transform.InverseTransformPoint(position).x<-.12f?Stroke.Backhand:Stroke.Forehand;}
        public void BeginStroke(Stroke kind,Vector3 contact,float secondsUntilContact)
        {
            activeStroke=kind;IsStriking=true;contactLocked=false;contactTarget=contact;
            contactTime=ContactFrameSeconds(kind);lead=Mathf.Max(.001f,secondsUntilContact);strokeTime=Mathf.Max(0,contactTime-lead);stepClock=0;startPosition=transform.position;
            var local=kind==Stroke.Smash?new Vector3(.18f,0,.20f):new Vector3(kind==Stroke.Backhand?-.58f:.88f,0,.35f);
            Vector3 desired=contact-transform.TransformDirection(local);desired.y=0;stepTarget=startPosition+Vector3.ClampMagnitude(desired-startPosition,.85f);
            if(kind==Stroke.Serve)stepTarget=startPosition;
            stepTarget.x=Mathf.Clamp(stepTarget.x,-5.9f,5.9f);stepTarget.z=Mathf.Clamp(Mathf.Abs(stepTarget.z),1.2f,13f)*(isCpu?1:-1);
        }
        public void AdvancePlant(float dt)
        {
            if(!IsStriking||contactLocked||activeStroke==Stroke.Serve)return;stepClock+=dt;
            transform.position=Vector3.Lerp(startPosition,stepTarget,Mathf.SmoothStep(0,1,Mathf.Clamp01(stepClock/lead)));
        }
        public Vector3 Contact(Vector3 target)
        {
            strokeTime=contactTime;contactTarget=target;contactLocked=true;Sample(activeStroke.ToString(),strokeTime);FitContact(target,1);
            LastContactError=Vector3.Distance(racketHead.position,target);return racketHead.position;
        }
        void FitContact(Vector3 target,float weight)
        {
            if(upper==null||weight<=0)return;
            float height=target.y-transform.position.y;
            float handHeight=Mathf.Clamp(height-.15f,.62f,1.13f);
            float rise=Mathf.Clamp((height-handHeight)/.65f,-.65f,.85f);
            Vector3 shaft=activeStroke==Stroke.Smash||activeStroke==Stroke.Serve?new Vector3(-.32f,.948f,0):new Vector3((activeStroke==Stroke.Backhand?-1:1)*Mathf.Sqrt(1-rise*rise),rise,0);
            shaft=transform.TransformDirection(shaft.normalized);Vector3 wrist=target-shaft*.65f;
            SolveArm(upper,forearm,hand,Vector3.Lerp(hand.position,wrist,weight),upper.position+transform.right*.6f-transform.forward*.25f);
            racket.position=hand.position;racket.rotation=Quaternion.LookRotation(transform.forward,shaft);
        }
        static void SolveArm(Transform a,Transform b,Transform c,Vector3 target,Vector3 pole)
        {
            float l1=Vector3.Distance(a.position,b.position),l2=Vector3.Distance(b.position,c.position);Vector3 delta=target-a.position;if(delta.sqrMagnitude<.000001f)return;
            float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(l1-l2)+.002f,l1+l2-.005f);Vector3 axis=delta.normalized;
            Vector3 bend=Vector3.ProjectOnPlane(pole-a.position,axis).normalized;if(bend.sqrMagnitude<.001f)bend=Vector3.Cross(axis,Vector3.up).normalized;
            float along=(l1*l1-l2*l2+distance*distance)/(2*distance);Vector3 elbow=a.position+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
            a.rotation=Quaternion.FromToRotation(b.position-a.position,elbow-a.position)*a.rotation;
            b.rotation=Quaternion.FromToRotation(c.position-b.position,target-b.position)*b.rotation;
        }
        void LateUpdate()
        {
            float dt=Time.deltaTime;if(dt<=0)return;speed=(transform.position-previous).magnitude/Mathf.Max(dt,.001f);previous=transform.position;
            if(IsStriking)
            {
                strokeTime+=dt;Sample(activeStroke.ToString(),strokeTime);float distance=Mathf.Abs(strokeTime-contactTime);
                if(distance<.16f)FitContact(contactTarget,1-Mathf.SmoothStep(0,1,distance/.16f));
                var clip=Clip(activeStroke.ToString());if(clip==null||strokeTime>=clip.length){IsStriking=false;contactLocked=false;prepared=0;}
            }
            else if(prepared>.1f&&speed<.7f)Sample(activeStroke.ToString(),prepared*ContactFrameSeconds(activeStroke)*.47f);
            else
            {
                string playing=speed>.3f?"run":"idle";var clip=Clip(playing);walkClock+=dt*(playing=="run"?Mathf.Clamp(speed/3.5f,.65f,1.8f):1);
                if(clip!=null)Sample(playing,walkClock%clip.length);
            }
        }
    }
}
