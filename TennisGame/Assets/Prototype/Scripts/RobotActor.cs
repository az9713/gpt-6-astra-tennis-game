using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoboOpen
{
    public class RobotActor : MonoBehaviour
    {
        public Transform model, racket, racketHead;
        public Animation animationPlayer;
        public bool isCpu;
        float shotUntil, walkClock;
        Vector3 previous;
        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        public void Initialize()
        {
            previous=transform.position;
            animationPlayer=model.GetComponentInChildren<Animation>();
            if(animationPlayer!=null)
            {
                foreach(AnimationState state in animationPlayer) clips[state.name] = state.clip;
                animationPlayer.cullingType=AnimationCullingType.AlwaysAnimate;
            }
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.updateWhenOffscreen=true;
            Play("idle",true);
        }
        public void Play(string name, bool loop=false)
        {
            if(animationPlayer==null) return;
            var key=clips.Keys.FirstOrDefault(k=>k.ToLowerInvariant().Contains(name));
            if(key==null)return;
            animationPlayer[key].wrapMode=loop?WrapMode.Loop:WrapMode.Once;
            animationPlayer.CrossFade(key,.12f);
        }
        public void Strike(bool serve, bool backhand=false)
        {
            shotUntil=Time.time+.52f;
            Play(serve?"serve":backhand?"backhand":"forehand");
        }
        void LateUpdate()
        {
            float speed=(transform.position-previous).magnitude/Mathf.Max(Time.deltaTime,.001f);
            previous=transform.position;
            walkClock+=Time.deltaTime*Mathf.Clamp(speed,0,6)*2.5f;
            if(Time.time > shotUntil && animationPlayer!=null)
            {
                string wanted=speed>.25f?"run":"idle";
                var key=clips.Keys.FirstOrDefault(k=>k.ToLowerInvariant().Contains(wanted));
                if(key!=null && !animationPlayer.IsPlaying(key))Play(wanted,true);
            }
        }
    }
}
