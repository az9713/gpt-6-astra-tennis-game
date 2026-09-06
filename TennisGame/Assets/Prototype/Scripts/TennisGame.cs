using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace RoboOpen
{
    public class TennisGame : MonoBehaviour
    {
        public enum Phase { Menu, Ready, Serving, Rally, Point, Finished }
        public RobotActor player, cpu;
        public Transform ball, landingMarker, aimMarker, ballShadow;
        public TrailRenderer trail;
        public Camera gameCamera;
        public TennisHud hud;
        public TennisScore score = new TennisScore();
        public Phase phase = Phase.Menu;
        public bool paused, autoplay;
        public int rally, bestRally, playerHits, cpuHits, pointsPlayed, netPoints, outPoints, doubleBouncePoints, faults;
        public float aimX, aimDepth = 8.3f;
        public string message="Your court. Your moment.", pointReason="";
        public Vector3 velocity, landing;
        public int lastHitter, bounces;
        public bool servingShot;
        float phaseClock, swingBuffer, cooldown, clock, lastHitTime;
        int serveFaults, serviceParity;
        System.Random random = new System.Random(1729);
        AudioSource audioSource;
        AudioClip hitSound,bounceSound,pointSound;
        Vector3 cameraBase;
        float shake;
        string evidencePath;
        bool automatedTest;
        int pendingHitter=-1;
        float pendingLeft;
        bool pendingLob,pendingPower;
        Vector3 tossStart,tossContact;
        public int smashHits,forehandHits,backhandHits;
        public string lastStroke="";
        public float maxContactError;
        public bool StrikePending => pendingHitter>=0;
        public bool CanSmash => CanPlayerHit && ball.position.y>=1.92f && velocity.y<=1.5f;

        public int Receiver => 1-lastHitter;
        public bool CanPlayerHit => phase==Phase.Rally && Receiver==0 && CanHit(player);
        void Start()
        {
            Application.runInBackground=true;Application.targetFrameRate=60;
            player.Initialize();cpu.Initialize();cameraBase=gameCamera.transform.position;
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=0;audioSource.volume=.3f;
            hitSound=Tone("racket",420,.09f);bounceSound=Tone("bounce",190,.055f);pointSound=Tone("point",680,.28f);
            hud=gameObject.AddComponent<TennisHud>();hud.game=this;hud.Build();
            ball.gameObject.SetActive(false);landingMarker.gameObject.SetActive(false);aimMarker.gameObject.SetActive(false);ballShadow.gameObject.SetActive(false);
            var args=Environment.GetCommandLineArgs();
            int index=Array.IndexOf(args,"--prototype-output");if(index>=0 && index+1<args.Length)evidencePath=args[index+1];
            automatedTest=Array.IndexOf(args,"--prototype-test")>=0;
            if(Array.IndexOf(args,"--motion-showcase")>=0){gameObject.AddComponent<MotionShowcase>().game=this;return;}
            if(automatedTest){autoplay=true;BeginMatch();StartCoroutine(AutomatedRun());}
            if(Array.IndexOf(args,"--prototype-input-test")>=0)
            {
                var test=gameObject.AddComponent<PrototypeInputChecks>();test.game=this;test.output=evidencePath;
                Directory.CreateDirectory(evidencePath);StartCoroutine(test.Run());
            }
        }
        public void BeginMatch()
        {
            pendingHitter=-1;smashHits=forehandHits=backhandHits=0;maxContactError=0;lastStroke="";player.ResetMotion();cpu.ResetMotion();score=new TennisScore();paused=false;Time.timeScale=1;phase=Phase.Ready;
            rally=bestRally=playerHits=cpuHits=pointsPlayed=netPoints=outPoints=doubleBouncePoints=faults=0;
            serveFaults=0;random=new System.Random(1729);PreparePoint();
        }
        public void Pause()
        {
            if(phase==Phase.Menu || phase==Phase.Finished)return;
            paused=!paused;Time.timeScale=paused?0:1;hud.Refresh();
        }
        public void ReturnToMenu()
        { pendingHitter=-1;player.ResetMotion();cpu.ResetMotion();paused=false;Time.timeScale=1;phase=Phase.Menu;autoplay=false;ball.gameObject.SetActive(false);landingMarker.gameObject.SetActive(false);aimMarker.gameObject.SetActive(false);ballShadow.gameObject.SetActive(false);trail.Clear();hud.Refresh(); }
        void PreparePoint()
        {
            pendingHitter=-1;player.ResetMotion();cpu.ResetMotion();phase=Phase.Ready;phaseClock=0;rally=0;bounces=0;cooldown=0;swingBuffer=0;
            serviceParity=(score.points[0]+score.points[1])%2;
            float serveX=serviceParity==0?1.4f:-1.4f;
            player.transform.position=new Vector3(score.server==0?serveX:0,0,score.server==0?-12.25f:-9.3f);
            cpu.transform.position=new Vector3(score.server==1?-serveX:0,0,score.server==1?12.25f:9.3f);
            aimX=0;aimDepth=8.3f;
            player.Play("idle",true);cpu.Play("idle",true);
            var server=score.server==0?player:cpu;
            ball.position=server.transform.position+new Vector3(.45f,1.28f,score.server==0?.5f:-.5f);
            ball.gameObject.SetActive(true);ballShadow.gameObject.SetActive(true);trail.Clear();trail.emitting=false;
            landingMarker.gameObject.SetActive(false);aimMarker.gameObject.SetActive(score.server==0);
            message=score.server==0?(serveFaults==1?"SECOND SERVE  ·  SPACE":"YOUR SERVE  ·  SPACE"):"MINT IS SERVING";
        }
        void Update()
        {
            var key=Keyboard.current;
            if(key!=null && key.escapeKey.wasPressedThisFrame)Pause();
            if(key!=null && key.rKey.wasPressedThisFrame && phase!=Phase.Menu)BeginMatch();
            if(paused){hud.Refresh();return;}
            float dt=Mathf.Min(Time.deltaTime,.05f);clock+=dt;phaseClock+=dt;cooldown-=dt;swingBuffer-=dt;
            if(phase==Phase.Menu){hud.Refresh();return;}
            if(phase==Phase.Finished){hud.Refresh();return;}
            if(phase==Phase.Point)
            { if(phaseClock>2.15f)PreparePoint();hud.Refresh();return; }
            bool swing=key!=null&&(key.spaceKey.wasPressedThisFrame||key.enterKey.wasPressedThisFrame);
            if(Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame && !(EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject()))swing=true;
            Vector2 move=Vector2.zero;
            if(key!=null)
            {
                move.x=(key.dKey.isPressed?1:0)-(key.aKey.isPressed?1:0);
                move.y=(key.wKey.isPressed?1:0)-(key.sKey.isPressed?1:0);
                aimX=Mathf.Clamp(aimX+((key.rightArrowKey.isPressed?1:0)-(key.leftArrowKey.isPressed?1:0))*dt*4,-3.3f,3.3f);
                aimDepth=Mathf.Clamp(aimDepth+((key.upArrowKey.isPressed?1:0)-(key.downArrowKey.isPressed?1:0))*dt*3,5.5f,10.7f);
            }
            if(autoplay)
            {
                if((phase==Phase.Rally||score.server!=0)&&pendingHitter!=0)MoveReceiver(player,dt,7.5f);
                swing=phase==Phase.Ready?score.server==0&&phaseClock>.85f:CanPlayerHit&&(bounces>0||CourtRules.InCourt(landing));
                aimX=Mathf.Sin(clock*.73f)*3.1f;
            }
            else if((phase==Phase.Rally || score.server!=0)&&pendingHitter!=0)
            {
                var p=player.transform.position+(new Vector3(move.x,0,move.y).normalized*7.1f*dt);
                p.x=Mathf.Clamp(p.x,-5.9f,5.9f);p.z=Mathf.Clamp(p.z,-13.0f,-1.2f);player.transform.position=p;
            }
            if(phase==Phase.Ready)
            {
                if((score.server==0&&swing)||(score.server==1&&phaseClock>1.1f))Serve();
            }
            else if(phase==Phase.Serving)
            { UpdateServe(); }
            else if(phase==Phase.Rally)
            {
                if(pendingHitter!=1)MoveReceiver(cpu,dt,5.0f);
                if(swing&&pendingHitter<0){swingBuffer=.27f; if(!CanPlayerHit)message="GET CLOSER  ·  SWING NEAR THE BALL";}
                if(swingBuffer>0 && CanPlayerHit && cooldown<=0 && pendingHitter<0)
                {
                    bool lob=key!=null&&key.zKey.isPressed;
                    bool power=key!=null&&(key.leftShiftKey.isPressed||key.rightShiftKey.isPressed);
                    QueueReturn(0,lob,power);swingBuffer=0;
                }
                else if(Receiver==1 && CanHit(cpu) && cooldown<=0 && pendingHitter<0 && (bounces>0||CourtRules.InCourt(landing)))QueueReturn(1,false,false);
                if(phase==Phase.Rally)
                    for(float t=0;t<dt && phase==Phase.Rally;t+=1f/120f)StepBall(Mathf.Min(1f/120f,dt-t));
                if(pendingHitter>=0 && phase==Phase.Rally)
                {
                    var actor=pendingHitter==0?player:cpu;actor.AdvancePlant(dt);pendingLeft-=dt;
                    if(pendingLeft<=0)
                    {
                        int hitter=pendingHitter;pendingHitter=-1;
                        if(Receiver==hitter && CanHit(actor))
                        {
                            Vector3 contact=actor.Contact(ball.position);
                            if(actor.LastContactError<=.60f){ball.position=contact;maxContactError=Mathf.Max(maxContactError,actor.LastContactError);ReturnShot(hitter,pendingLob,pendingPower);}
                            else{message="JUST OUT OF REACH";cooldown=.18f;}
                        }
                    }
                }
            }
            player.Anticipate(ball.position,phase==Phase.Rally&&Receiver==0);cpu.Anticipate(ball.position,phase==Phase.Rally&&Receiver==1);
            if(phase==Phase.Rally && pendingHitter<0 && clock-lastHitTime>.55f && swingBuffer<=0)
                message=Receiver==0?(CanPlayerHit?(CanSmash?"SMASH NOW  ·  SPACE":"SWING NOW  ·  SPACE"):"CHASE THE LANDING MARKER"):"RECOVER TO THE CENTRE";
            aimMarker.gameObject.SetActive(phase==Phase.Ready?score.server==0:phase==Phase.Rally&&Receiver==0);
            aimMarker.position=new Vector3(aimX,.055f,aimDepth);
            ballShadow.position=new Vector3(ball.position.x,.035f,ball.position.z);
            float shadowScale=Mathf.Lerp(.27f,.13f,Mathf.Clamp01(ball.position.y/6));ballShadow.localScale=new Vector3(shadowScale,.007f,shadowScale);
            shake=Mathf.MoveTowards(shake,0,dt*3);
            gameCamera.transform.position=cameraBase+new Vector3(Mathf.Sin(clock*80)*shake*.03f,Mathf.Cos(clock*63)*shake*.022f,0);
            hud.Refresh();
        }
        void MoveReceiver(RobotActor actor,float dt,float speed)
        {
            int side=actor.isCpu?1:0;
            Vector3 target=new Vector3(0,0,side==1?9.3f:-9.3f);
            if(phase==Phase.Rally && Receiver==side)
            {
                float sign=side==1?1:-1;
                // Stand just behind the first bounce; follow the ball after it bounces.
                target=new Vector3(landing.x,0,Mathf.Clamp(Mathf.Abs(landing.z)+1.05f,2.2f,12.2f)*sign);
                if(bounces>0)target=new Vector3(ball.position.x,0,ball.position.z+sign*.55f);
                target.x=Mathf.Clamp(target.x,-5.7f,5.7f);target.z=Mathf.Clamp(Mathf.Abs(target.z),1.3f,12.8f)*sign;
            }
            actor.transform.position=Vector3.MoveTowards(actor.transform.position,target,speed*dt);
        }
        public bool CanHit(RobotActor actor)
        {
            if(servingShot&&bounces==0)return false;
            if(ball.position.y<.38f || ball.position.y>2.75f)return false;
            if(actor.isCpu ? ball.position.z<.7f : ball.position.z>-.7f)return false;
            Vector3 d=ball.position-actor.transform.position;d.y=0;
            return d.magnitude < (actor.isCpu?1.58f:1.85f);
        }
        public void Serve()
        {
            if(phase!=Phase.Ready)return;
            phase=Phase.Serving;phaseClock=0;
            var actor=score.server==0?player:cpu;float sign=score.server==0?1:-1;
            tossStart=actor.transform.position+new Vector3(-.20f*sign,1.12f,.22f*sign);
            tossContact=actor.transform.position+new Vector3(.16f*sign,2.22f,.24f*sign);
            actor.BeginStroke(RobotActor.Stroke.Serve,tossContact,1.4f);trail.emitting=false;message="TOSS  ·  TROPHY  ·  STRIKE";
        }
        void UpdateServe()
        {
            var actor=score.server==0?player:cpu;
            if(phaseClock<.3f)ball.position=Vector3.Lerp(tossStart,tossStart+Vector3.up*.25f,phaseClock/.3f);
            else
            {
                var start=tossStart+Vector3.up*.25f;var tossVelocity=CourtRules.LaunchVelocity(start,tossContact,1.1f);
                ball.position=CourtRules.Position(start,tossVelocity,Mathf.Min(phaseClock-.3f,1.1f));
            }
            if(phaseClock<1.4f)return;
            lastHitter=score.server;servingShot=true;bounces=0;float sign=score.server==0?1:-1;
            ball.position=actor.Contact(tossContact);maxContactError=Mathf.Max(maxContactError,actor.LastContactError);lastStroke="Serve";
            float x=(serviceParity==0?-1:1)*sign*2.15f;
            Launch(new Vector3(x,CourtRules.BallRadius,sign*5.8f),1.35f);
            phase=Phase.Rally;phaseClock=0;rally=1;cooldown=.3f;lastHitTime=clock;message=lastHitter==0?"NICE SERVE":"RETURN THE SERVE";
        }
        void QueueReturn(int hitter,bool lob,bool power)
        {
            var actor=hitter==0?player:cpu;
            bool smash=ball.position.y>=1.92f && velocity.y<=1.5f;
            float lead=smash?.14f:.10f;var target=CourtRules.Position(ball.position,velocity,lead);
            if(target.y<.36f || target.y>2.7f)return;
            var kind=smash?RobotActor.Stroke.Smash:actor.transform.InverseTransformPoint(target).x<-.12f?RobotActor.Stroke.Backhand:RobotActor.Stroke.Forehand;
            pendingHitter=hitter;pendingLeft=lead;pendingLob=lob&&!smash;pendingPower=power||smash;
            actor.BeginStroke(kind,target,lead);message=smash?"OVERHEAD SMASH":kind==RobotActor.Stroke.Backhand?"BACKHAND":"FOREHAND";
        }
        void ReturnShot(int hitter,bool lob,bool power)
        {
            var actor=hitter==0?player:cpu;
            bool smash=actor.activeStroke==RobotActor.Stroke.Smash;lastStroke=actor.activeStroke.ToString();
            if(smash)smashHits++;else if(actor.activeStroke==RobotActor.Stroke.Backhand)backhandHits++;else forehandHits++;
            lastHitter=hitter;servingShot=false;bounces=0;rally++;bestRally=Mathf.Max(bestRally,rally);
            if(hitter==0)playerHits++;else cpuHits++;
            if(hitter==1&&!smash&&cpuHits%4==0)lob=true;
            float x=hitter==0?aimX:(float)(random.NextDouble()*6.1-3.05);
            float z=hitter==0?aimDepth:-(7.4f+(float)random.NextDouble()*2.1f);
            // CPU occasionally overhits during a long rally; the normal boundary rule decides the point.
            if(hitter==1 && rally>6 && random.NextDouble()<.12)x=Mathf.Sign(x)*5.4f;
            float duration=smash?1.05f:lob?2.0f:power?1.18f:1.55f;
            Vector3 target=new Vector3(x,CourtRules.BallRadius,z);
            // Ensure ordinary returns clear the net while preserving a ballistic path.
            Vector3 v=CourtRules.LaunchVelocity(ball.position,target,duration);
            float netT=-ball.position.z/v.z;
            while(netT>0 && netT<duration && CourtRules.Position(ball.position,v,netT).y<1.27f && duration<2.2f)
            { duration+=.08f;v=CourtRules.LaunchVelocity(ball.position,target,duration);netT=-ball.position.z/v.z; }
            Launch(target,duration);cooldown=.3f;lastHitTime=clock;shake=1;
            message=hitter==0?(smash?"OVERHEAD SMASH":lob?"LOFTED RETURN":power?"POWER SHOT":"CLEAN RETURN"):(lob?"MINT LOBS  ·  WATCH THE HIGH BALL":"MINT RETURNS");
        }
        void Launch(Vector3 target,float duration)
        {
            landing=target;velocity=CourtRules.LaunchVelocity(ball.position,target,duration);
            landingMarker.position=new Vector3(target.x,.05f,target.z);landingMarker.gameObject.SetActive(true);
            trail.Clear();trail.emitting=true;audioSource.PlayOneShot(hitSound);
        }
        void StepBall(float dt)
        {
            var old=ball.position;var next=old+velocity*dt+Vector3.down*(.5f*CourtRules.Gravity*dt*dt);velocity.y-=CourtRules.Gravity*dt;
            if(old.z*next.z<0 && Mathf.Abs(next.x)<5.15f)
            {
                float y=Mathf.Lerp(old.y,next.y,-old.z/(next.z-old.z));
                if(y<CourtRules.NetHeight+CourtRules.BallRadius){ball.position=new Vector3(next.x,y,0);netPoints++;FaultOrPoint("NET",Receiver);return;}
            }
            ball.position=next;
            if(next.y<=CourtRules.BallRadius && velocity.y<0)
            {
                ball.position=new Vector3(next.x,CourtRules.BallRadius,next.z);
                if(bounces==0)
                {
                    bool rightSide=lastHitter==0?next.z>0:next.z<0;
                    bool inside=servingShot?CourtRules.InServiceBox(next,Receiver,serviceParity):CourtRules.InCourt(next)&&rightSide;
                    if(!inside){outPoints++;FaultOrPoint(servingShot?"SERVICE FAULT":"OUT",Receiver);return;}
                }
                bounces++;
                if(bounces>=2){doubleBouncePoints++;AwardPoint(lastHitter,"DOUBLE BOUNCE");return;}
                velocity=new Vector3(velocity.x*.84f,Mathf.Abs(velocity.y)*.72f,velocity.z*.84f);
                audioSource.PlayOneShot(bounceSound);landingMarker.gameObject.SetActive(false);
            }
            if(Mathf.Abs(next.z)>19 || Mathf.Abs(next.x)>12 || next.y<-.5f)AwardPoint(bounces>0?lastHitter:Receiver,"OUT OF REACH");
        }
        void FaultOrPoint(string reason,int winner)
        {
            if(servingShot && serveFaults==0)
            {
                serveFaults++;faults++;phase=Phase.Point;phaseClock=.55f;message="FAULT  ·  SECOND SERVE";pointReason="FIRST SERVE FAULT";trail.emitting=false;return;
            }
            AwardPoint(winner,servingShot?"DOUBLE FAULT":reason);
        }
        public void AwardPoint(int winner,string reason)
        {
            if(phase!=Phase.Rally)return;
            pendingHitter=-1;pointsPlayed++;pointReason=reason;bool gameWon=score.Award(winner);serveFaults=0;
            phase=score.winner>=0?Phase.Finished:Phase.Point;phaseClock=0;trail.emitting=false;landingMarker.gameObject.SetActive(false);aimMarker.gameObject.SetActive(false);
            message=score.winner>=0?(winner==0?"YOU WIN THE MATCH":"MINT WINS THE MATCH"):(gameWon?(winner==0?"GAME, YOU":"GAME, MINT"):(winner==0?"YOUR POINT":"MINT'S POINT"));
            audioSource.PlayOneShot(pointSound);Debug.Log("ROBO_POINT "+winner+" "+reason+" rally="+rally);
        }
        AudioClip Tone(string name,float hz,float duration)
        {
            int n=(int)(22050*duration);var samples=new float[n];
            for(int i=0;i<n;i++){float t=(float)i/22050;samples[i]=Mathf.Sin(2*Mathf.PI*(hz*t-hz*.28f*t*t/duration))*Mathf.Exp(-t*7/duration)*.5f;}
            var c=AudioClip.Create(name,n,1,22050,false);c.SetData(samples,0);return c;
        }
        public void Capture(string path)
        {
            var rt=new RenderTexture(1600,900,GraphicsFormat.R8G8B8A8_SRGB,GraphicsFormat.D24_UNorm_S8_UInt);
            var old=RenderTexture.active;var oldTarget=gameCamera.targetTexture;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try{gameCamera.targetTexture=rt;if(hud!=null)hud.Fit();RenderPipeline.SubmitRenderRequest(gameCamera,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
            finally{gameCamera.targetTexture=oldTarget;if(hud!=null)hud.Fit();RenderTexture.active=old;rt.Release();Destroy(rt);Destroy(tex);}
        }
        [Serializable] public class RuntimeReceipt
        {
            public bool passed;public int playerHits,cpuHits,bestRally,pointsPlayed;public string phase,graphicsDevice;public int[] games;public float elapsed;public int smashHits,forehandHits,backhandHits;public float maxContactError;
        }
        public RuntimeReceipt Receipt()=>new RuntimeReceipt{passed=playerHits>=3&&cpuHits>=3&&bestRally>=5&&pointsPlayed>=1,playerHits=playerHits,cpuHits=cpuHits,bestRally=bestRally,pointsPlayed=pointsPlayed,phase=phase.ToString(),graphicsDevice=SystemInfo.graphicsDeviceName,games=score.games,elapsed=clock,smashHits=smashHits,forehandHits=forehandHits,backhandHits=backhandHits,maxContactError=maxContactError};
        IEnumerator AutomatedRun()
        {
            yield return new WaitForSeconds(8);
            if(!string.IsNullOrEmpty(evidencePath)){Directory.CreateDirectory(evidencePath);Capture(Path.Combine(evidencePath,"prototype-rally.png"));}
            yield return new WaitForSeconds(52);
            var receipt=Receipt();Debug.Log("ROBO_RUNTIME "+JsonUtility.ToJson(receipt));
            if(!string.IsNullOrEmpty(evidencePath)){File.WriteAllText(Path.Combine(evidencePath,"prototype-runtime.json"),JsonUtility.ToJson(receipt,true));Capture(Path.Combine(evidencePath,"prototype-final.png"));}
            if(!Application.isEditor)Application.Quit(receipt.passed?0:1);
        }
        void OnDestroy(){Time.timeScale=1;}
    }
}
