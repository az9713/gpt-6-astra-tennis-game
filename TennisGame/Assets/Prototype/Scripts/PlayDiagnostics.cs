using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RoboOpen
{
    // Gameplay-only telemetry. No accounts, machine paths, raw keyboard text or network calls.
    public sealed class PlayDiagnostics : MonoBehaviour
    {
        public const string Build = "0.3.0", Policy = "return-window-1";
        [Serializable] public class Frame
        {
            public float time, realTime, frameMs, buffer, holdSeconds;
            public int frame, shot, receiver, bounces, youPoints, mintPoints, youGames, mintGames;
            public string phase, hud, youCheck, mintCheck;
            public bool focused, paused, serve, swingHeld;
            public Vector3 ball, velocity, you, mint, youRacket, mintRacket, youPlannedContact, mintPlannedContact;
            public Vector2 movementInput, aim;
            public string youStroke, mintStroke;
            public float youStrokeTime, mintStrokeTime;
        }
        [Serializable] public class Event
        {
            public int sequence, actor, attempt;
            public string kind, reason;
            public float value;
            public Frame state;
        }
        [Serializable] public class Shot
        {
            public int id, receiver, attempts;
            public float start, end, firstWindow=-1, lastWindow=-1, windowSeconds, closest=999, lastAttempt=-1;
            public bool broadReach, scheduled, contactFailure, heldThroughWindow, focused=true, scriptedError;
            public string outcome, cause;
        }
        [Serializable] public class Cause { public string code; public int count; }
        [Serializable] public class PlayerSummary
        {
            public string player;
            public int incoming, returned, missedReturns, shotErrors, attempts, pointsWon, pointsLost, firstServeFaults;
            public List<Cause> causes=new List<Cause>();
        }
        [Serializable] public class Replay { public int shot, actor; public string cause; public float captureUntil; public List<Frame> frames=new List<Frame>(); }
        [Serializable] public class Summary
        {
            public string schema="robo-play-session-1", build=Build, policy=Policy, session, startedUtc, mode, endedUtc="", unity;
            public float earlyBuffer=ReturnPlanner.BufferSeconds, contactTolerance=ReturnPlanner.ContactTolerance;
            public int focusLosses, slowFrames, frames, eventCount;
            public bool recordingHealthy=true, sizeLimitReached;
            public PlayerSummary[] players={new PlayerSummary{player="You"},new PlayerSummary{player="Mint"}};
            public List<Shot> shots=new List<Shot>();
            public List<Replay> replays=new List<Replay>();
            public string scope="Observed events and rule-based suggestions, not a causal or biomechanical diagnosis. No key text or personal identity is recorded.";
        }
        TennisGame game;
        public Summary Data {get;private set;}
        public string SessionDirectory {get;private set;}
        public string ReportPath=>SessionDirectory==null?null:Path.Combine(SessionDirectory,"report.html");
        StreamWriter events,windows;
        readonly Queue<Frame> history=new Queue<Frame>();
        readonly List<Replay> pending=new List<Replay>();
        Shot shot;
        int nextShot,attempt;
        float nextSample,heldSince=-1,lastObserve;
        bool held,lastFocus=true,finished;
        string[] previousChecks={"",""};
        public void Initialize(TennisGame owner,string root,string mode)
        {
            game=owner;var id=DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ")+"-"+Guid.NewGuid().ToString("N").Substring(0,8);
            Data=new Summary{session=id,startedUtc=DateTime.UtcNow.ToString("o"),mode=mode,unity=Application.unityVersion};
            try
            {
                Directory.CreateDirectory(root);Prune(root);SessionDirectory=Path.Combine(root,id);Directory.CreateDirectory(SessionDirectory);
                events=new StreamWriter(Path.Combine(SessionDirectory,"events.jsonl"),false,new UTF8Encoding(false)){AutoFlush=true};
                windows=new StreamWriter(Path.Combine(SessionDirectory,"windows.jsonl"),false,new UTF8Encoding(false)){AutoFlush=true};
                File.WriteAllText(Path.Combine(SessionDirectory,"session.json"),JsonUtility.ToJson(Data,true));
                Record("session_start",-1,mode);Save();
            }
            catch(Exception){Fail();}
        }
        static void Prune(string root)
        {
            string boundary=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            var directories=new DirectoryInfo(root).GetDirectories().Where(d=>(d.Attributes&FileAttributes.ReparsePoint)==0&&Regex.IsMatch(d.Name,@"^\d{8}T\d{9}Z-[a-f0-9]{8}$")&&File.Exists(Path.Combine(d.FullName,"session.json"))).OrderByDescending(d=>d.Name).ToArray();
            long bytes=0;
            for(int i=0;i<directories.Length;i++)
            {
                var d=directories[i];bytes+=d.GetFiles().Sum(f=>f.Length);
                if(i<19&&bytes<100L*1024*1024)continue;
                if(!Path.GetFullPath(d.FullName).StartsWith(boundary,StringComparison.OrdinalIgnoreCase))continue;
                // Delete only files this recorder owns, never recurse into unrelated data.
                try
                {
                    foreach(string name in new[]{"events.jsonl","windows.jsonl","summary.json","report.html","session.json"})
                    {string p=Path.Combine(d.FullName,name);if(File.Exists(p))File.Delete(p);}
                    if(d.GetFileSystemInfos().Length==0)d.Delete();
                }
                catch(IOException){ /* An older session can still be open in another player process. */ }
            }
        }
        void Fail(){if(Data!=null)Data.recordingHealthy=false;Debug.LogWarning("ROBO_DIAGNOSTICS_UNAVAILABLE: gameplay continues; local recording could not be written.");}
        Frame Snapshot()=>new Frame{time=game.PlayClock,realTime=Time.realtimeSinceStartup,frame=Time.frameCount,frameMs=Time.unscaledDeltaTime*1000,
            shot=shot?.id??0,receiver=game.Receiver,bounces=game.bounces,youPoints=game.score.points[0],mintPoints=game.score.points[1],youGames=game.score.games[0],mintGames=game.score.games[1],phase=game.phase.ToString(),hud=game.message,youCheck=game.PlayerPlan.reason,mintCheck=game.CpuPlan.reason,
            focused=Application.isFocused,paused=game.paused,serve=game.servingShot,swingHeld=held,holdSeconds=held?Mathf.Max(0,game.PlayClock-heldSince):0,buffer=game.SwingBuffer,
            ball=game.ball.position,velocity=game.velocity,you=game.player.transform.position,mint=game.cpu.transform.position,youRacket=game.player.racketHead.position,mintRacket=game.cpu.racketHead.position,
            youPlannedContact=game.PlayerPlan.target,mintPlannedContact=game.CpuPlan.target,movementInput=game.MovementInput,aim=new Vector2(game.aimX,game.aimDepth),youStroke=game.player.activeStroke.ToString(),mintStroke=game.cpu.activeStroke.ToString(),youStrokeTime=game.player.StrokeTime,mintStrokeTime=game.cpu.StrokeTime};
        public void Record(string kind,int actor,string reason="",float value=0)
        {
            if(Data==null)return;
            var item=new Event{sequence=++Data.eventCount,kind=kind,actor=actor,attempt=attempt,reason=reason,value=value,state=Snapshot()};
            Write(events,JsonUtility.ToJson(item));
        }
        void Write(StreamWriter writer,string line)
        {
            if(writer==null||!Data.recordingHealthy)return;
            try{if(writer.BaseStream.Length>8L*1024*1024){Data.sizeLimitReached=true;return;}writer.WriteLine(line);}
            catch(Exception){Fail();}
        }
        public void Input(bool pressed,bool isHeld,bool released,string source)
        {
            if(pressed){heldSince=game.PlayClock;Record("swing_press",0,source);}
            held=isHeld;
            if(released){Record("swing_release",0,source,heldSince<0?0:game.PlayClock-heldSince);heldSince=-1;}
        }
        public void Attempt(int actor,string reason)
        {
            attempt++;Data.players[actor].attempts++;
            if(shot!=null&&shot.receiver==actor){shot.attempts++;shot.lastAttempt=game.PlayClock;}
            Record("swing_attempt",actor,reason);
        }
        public void BeginIncoming(int receiver,bool scriptedError)
        {
            if(shot!=null)Resolve("interrupted","interrupted");
            shot=new Shot{id=++nextShot,receiver=receiver,start=game.PlayClock,scriptedError=scriptedError};
            Data.players[receiver].incoming++;Record("incoming_ball",receiver,scriptedError?"scripted_cpu_overhit":"shot");
        }
        public void Scheduled(int actor,ReturnPlan plan)
        {if(shot!=null&&shot.receiver==actor)shot.scheduled=true;Record("strike_scheduled",actor,plan.stroke.ToString(),plan.delay);}
        public void ContactFailed(int actor,string reason,float error)
        {if(shot!=null&&shot.receiver==actor)shot.contactFailure=true;Record("contact_rejected",actor,reason,error);}
        public void Returned(int actor,string stroke,float error)
        {
            Data.players[actor].returned++;Record("contact_accepted",actor,stroke,error);Resolve("returned","success");
        }
        public void Observe(float dt)
        {
            if(Data==null)return;Data.frames++;if(Time.unscaledDeltaTime>.05f)Data.slowFrames++;
            if(Application.isFocused!=lastFocus){lastFocus=Application.isFocused;if(!lastFocus)Data.focusLosses++;Record("focus",0,lastFocus?"gained":"lost");}
            if(Time.realtimeSinceStartup>=nextSample)
            {
                nextSample=Time.realtimeSinceStartup+.1f;var frame=Snapshot();history.Enqueue(frame);while(history.Count>30)history.Dequeue();
                foreach(var replay in pending.ToArray())
                {
                    replay.frames.Add(frame);
                    if(frame.realTime>=replay.captureUntil){Write(windows,JsonUtility.ToJson(replay));pending.Remove(replay);}
                }
            }
            if(Time.realtimeSinceStartup-lastObserve>30){lastObserve=Time.realtimeSinceStartup;Save();}
        }
        public void ObserveOpportunities(float dt)
        {
            if(Data==null)return;
            if(game.phase==TennisGame.Phase.Rally&&!game.paused)
            {
                for(int i=0;i<2;i++)
                {
                    var plan=i==0?game.PlayerPlan:game.CpuPlan;
                    if(plan.reason!=previousChecks[i]){Record("eligibility",i,plan.reason);previousChecks[i]=plan.reason;}
                }
                if(shot!=null)
                {
                    int side=shot.receiver;var actor=side==0?game.player:game.cpu;
                    var d=game.ball.position-actor.transform.position;d.y=0;
                    if((side==0?game.ball.position.z<0:game.ball.position.z>0))shot.closest=Mathf.Min(shot.closest,d.magnitude);
                    if(ReturnPlanner.ReachReason(actor,game.ball.position,game.bounces,game.servingShot)=="ready")shot.broadReach=true;
                    if((side==0?game.PlayerPlan:game.CpuPlan).available)
                    {
                        if(shot.firstWindow<0)shot.firstWindow=game.PlayClock;
                        shot.lastWindow=game.PlayClock;shot.windowSeconds+=dt;
                        if(side==0&&held&&heldSince>=0&&game.PlayClock-heldSince>ReturnPlanner.BufferSeconds)shot.heldThroughWindow=true;
                    }
                    if(!Application.isFocused&&side==0&&Data.mode=="human")shot.focused=false;
                }
            }
        }
        public static string MissCause(Shot item)
        {
            if(item.contactFailure||item.scheduled)return "game_contact_failure";
            if(!item.focused&&item.receiver==0)return "focus_interrupted";
            if(item.firstWindow<0)return item.broadReach?"game_no_contact_window":"positioning_or_difficult_ball";
            if(item.receiver==1)return "cpu_decision_miss";
            if(item.heldThroughWindow)return "held_swing";
            if(item.attempts==0)return "no_swing_in_window";
            if(item.lastAttempt+ReturnPlanner.BufferSeconds<item.firstWindow)return "pressed_too_early";
            if(item.lastAttempt>item.lastWindow)return "pressed_too_late";
            return "game_buffer_or_timing";
        }
        public void PointEnded(int winner,string reason)
        {
            int loser=1-winner;string cause;
            Data.players[winner].pointsWon++;Data.players[loser].pointsLost++;
            if(reason=="NET"||reason=="OUT"||reason=="DOUBLE FAULT"||loser==game.lastHitter)
            {
                cause=shot!=null&&shot.scriptedError?"scripted_cpu_error":reason=="NET"?"shot_into_net":"shot_out";
                Data.players[loser].shotErrors++;Count(loser,cause);Resolve("opponent_shot_error",cause);
            }
            else
            {
                cause=shot==null?"insufficient_evidence":MissCause(shot);
                Data.players[loser].missedReturns++;Count(loser,cause);CaptureMiss(loser,cause);Resolve("missed_return",cause);
            }
            Record("point_end",loser,cause);Save();
        }
        void Count(int side,string code)
        {var counts=Data.players[side].causes;var cause=counts.Find(c=>c.code==code);if(cause==null){cause=new Cause{code=code};counts.Add(cause);}cause.count++;}
        void CaptureMiss(int actor,string cause)
        {
            var replay=new Replay{shot=shot?.id??0,actor=actor,cause=cause,captureUntil=Time.realtimeSinceStartup+1,frames=history.ToList()};pending.Add(replay);Data.replays.Add(replay);
            while(Data.replays.Count>8)Data.replays.RemoveAt(0);
        }
        void Resolve(string outcome,string cause)
        {
            if(shot==null)return;shot.end=game.PlayClock;shot.outcome=outcome;shot.cause=cause;Data.shots.Add(shot);
            if(Data.shots.Count>200)Data.shots.RemoveAt(0);Record("incoming_resolved",shot.receiver,cause);shot=null;
        }
        public void Interrupt(string reason){Resolve("interrupted",reason);Record("match_boundary",-1,reason);Save();}
        public static string Advice(string cause,bool cpu)
        {
            switch(cause)
            {
                case "held_swing":return "Release the swing key between shots, then tap as the ball approaches. Holding it does not repeatedly swing.";
                case "pressed_too_early":return "Tap nearer the ball's arrival. Your recorded press expired before a return window opened.";
                case "pressed_too_late":return "Prepare sooner and tap as the return cue appears; the last recorded press followed the available window.";
                case "no_swing_in_window":return "A valid window was observed without a swing attempt. Tap Space when ready; the game also accepts an early tap for 0.48 seconds.";
                case "positioning_or_difficult_ball":return cpu?"Mint had no observed reachable window. Inspect its recovery position, interception target and movement speed; the opponent's shot may also have been difficult.":"No reachable window was observed. Recover toward centre and move behind the yellow bounce marker. This evidence cannot separate positioning from an unusually difficult shot.";
                case "shot_out":return "Aim farther inside the sidelines and baseline. Review the outgoing trajectory before changing swing timing.";
                case "shot_into_net":return "Allow more net clearance. Inspect the outgoing trajectory and shot settings; timing alone may not explain this error.";
                case "scripted_cpu_error":return "Mint's deliberate random overhit caused this error. This is a game difficulty setting, not evidence of poor player execution.";
                case "focus_interrupted":return "The game window lost focus during this incoming ball. Repeat the play with focus restored before judging skill.";
                case "cpu_decision_miss":return "Mint had an available return window but did not complete a return. Inspect CPU scheduling rather than prescribing human reaction practice.";
                case "game_contact_failure":return "The game scheduled a strike but did not complete contact. Inspect prediction, planting and racket reach; do not attribute this directly to the player.";
                case "game_no_contact_window":return "The ball entered the broad reach zone but no feasible racket contact was found. Inspect reach geometry and contact prediction.";
                case "game_buffer_or_timing":return "An input and an available window were both observed. Inspect their event sequence for a buffering or scheduling failure.";
                default:return "Not enough evidence to attribute this outcome. Inspect the event trace and gather another play session.";
            }
        }
        public void Save()
        {
            if(Data==null||!Data.recordingHealthy||SessionDirectory==null)return;
            try
            {
                File.WriteAllText(Path.Combine(SessionDirectory,"summary.json"),JsonUtility.ToJson(Data,true),new UTF8Encoding(false));
                File.WriteAllText(ReportPath,PlayReport.Render(Data),new UTF8Encoding(false));
            }
            catch(Exception){Fail();}
        }
        public void OpenReport(){Save();if(File.Exists(ReportPath))Application.OpenURL(new Uri(ReportPath).AbsoluteUri);else game.message="PLAY REPORT UNAVAILABLE";}
        void OnApplicationQuit(){Finish();}
        void OnDestroy(){Finish();}
        void Finish()
        {
            if(finished||Data==null)return;finished=true;Interrupt("session_end");
            foreach(var replay in pending)Write(windows,JsonUtility.ToJson(replay));pending.Clear();
            Data.endedUtc=DateTime.UtcNow.ToString("o");Record("session_end",-1);Save();events?.Dispose();windows?.Dispose();
        }
    }
}
