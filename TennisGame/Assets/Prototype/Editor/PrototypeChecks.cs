using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RoboOpen;

public static class PrototypeChecks
{
    public static void Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string name){if(!ok)throw new Exception("RULE CHECK FAILED: "+name);checks.Add(name);}
        var score=new TennisScore();score.Award(0);Check(score.PointLabel(0)=="15","First point is 15");
        score.Award(0);score.Award(0);for(int i=0;i<3;i++)score.Award(1);
        Check(score.PointLabel(0)=="40"&&score.PointLabel(1)=="40","Deuce is 40-all");score.Award(0);
        Check(score.PointLabel(0)=="AD"&&score.games[0]==0,"Advantage does not award game");score.Award(1);
        Check(score.PointLabel(0)=="40"&&score.PointLabel(1)=="40","Lost advantage returns to deuce");score.Award(1);score.Award(1);
        Check(score.games[1]==1&&score.server==1&&score.points[0]==0&&score.points[1]==0,"Game win clears points and alternates server");
        for(int i=0;i<4;i++)score.Award(1);Check(score.winner==1,"First to two games wins short match");
        int old=score.totalPoints;score.Award(0);Check(score.totalPoints==old,"Finished match ignores extra points");
        Check(CourtRules.InCourt(new Vector3(CourtRules.HalfWidth,0,CourtRules.HalfLength)),"Boundary line counts in");
        Check(!CourtRules.InCourt(new Vector3(CourtRules.HalfWidth+.2f,0,0)),"Outside line is out");
        Check(CourtRules.InServiceBox(new Vector3(-2,0,5),1,0),"Near deuce serve crosses diagonally");
        Check(!CourtRules.InServiceBox(new Vector3(2,0,5),1,0),"Wrong service box is a fault");
        Check(CourtRules.InServiceBox(new Vector3(2,0,5),1,1),"Near advantage serve changes box");
        Check(CourtRules.InServiceBox(new Vector3(2,0,-5),0,0),"Far deuce serve mirrors near serve");
        Check(!CourtRules.InServiceBox(new Vector3(-2,0,8),1,0),"Deep serve is a fault");
        var from=new Vector3(1,1.2f,-9);var target=new Vector3(-3,CourtRules.BallRadius,8);var velocity=CourtRules.LaunchVelocity(from,target,1.6f);
        Check(Vector3.Distance(CourtRules.Position(from,velocity,1.6f),target)<.0001f,"Ballistic shot lands at selected target");
        Check(Mathf.Abs(CourtRules.TimeToGround(from,velocity)-1.6f)<.0001f,"Predicted landing time matches trajectory");
        Check(CourtRules.Position(from,velocity,-from.z/velocity.z).y>CourtRules.NetHeight,"Representative rally clears net");
        var falling=new Vector3(.5f,.60f,-7.4f);var downward=new Vector3(0,-4,0);int bounceCount=0;bool forecast=true;
        for(int i=0;i<30;i++)forecast&=ReturnPlanner.PredictStep(ref falling,ref downward,ref bounceCount,true,1,0,1f/120f);
        // Use an ordinary rally for this deep bounce; the same spot is an illegal serve box.
        Check(!forecast,"Contact forecast rejects an illegal serve bounce");
        falling=new Vector3(.5f,.60f,-7.4f);downward=new Vector3(0,-4,0);bounceCount=0;forecast=true;
        for(int i=0;i<30;i++)forecast&=ReturnPlanner.PredictStep(ref falling,ref downward,ref bounceCount,false,1,0,1f/120f);
        Check(forecast&&bounceCount==1&&falling.y>.38f,"Contact forecast follows a legal bounce back into reach");
        var observation=new PlayDiagnostics.Shot{receiver=0,firstWindow=2,lastWindow=2.4f,lastAttempt=1,attempts=1};
        Check(PlayDiagnostics.MissCause(observation)=="pressed_too_early","Expired input before a later opportunity is classified as early");
        observation.lastAttempt=2.5f;Check(PlayDiagnostics.MissCause(observation)=="pressed_too_late","Input after the opportunity is classified as late");
        observation.heldThroughWindow=true;Check(PlayDiagnostics.MissCause(observation)=="held_swing","Held input gets a release-and-tap recommendation");
        observation.scheduled=true;Check(PlayDiagnostics.MissCause(observation)=="game_contact_failure","Scheduled failure is not attributed to held input");
        observation=new PlayDiagnostics.Shot{receiver=1,firstWindow=2};Check(PlayDiagnostics.MissCause(observation)=="cpu_decision_miss","Mint's missed opportunity is attributed to CPU scheduling");
        observation=new PlayDiagnostics.Shot{receiver=0,broadReach=true};Check(PlayDiagnostics.MissCause(observation)=="game_no_contact_window","Broad reach without a feasible contact is flagged for game inspection");
        observation.broadReach=false;Check(PlayDiagnostics.MissCause(observation)=="positioning_or_difficult_ball","No reachable window preserves uncertainty about positioning and shot difficulty");
        Check(PlayDiagnostics.Advice("scripted_cpu_error",true).Contains("difficulty setting"),"Scripted CPU overhits are explained as a difficulty setting");
        string retentionRoot=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Evidence/Diagnostics/retention-"+Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(retentionRoot);
        for(int i=0;i<23;i++){string d=Path.Combine(retentionRoot,"20260101T000000"+i.ToString("000")+"Z-1234abcd");Directory.CreateDirectory(d);File.WriteAllText(Path.Combine(d,"session.json"),"{}");File.WriteAllText(Path.Combine(d,"report.html"),"test");}
        string unrelated=Path.Combine(retentionRoot,"unrelated");Directory.CreateDirectory(unrelated);File.WriteAllText(Path.Combine(unrelated,"keep.txt"),"keep");
        typeof(PlayDiagnostics).GetMethod("Prune",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{retentionRoot});
        Check(Directory.GetFiles(retentionRoot,"session.json",SearchOption.AllDirectories).Length==19,"Retention leaves room for a twentieth session");
        Check(File.Exists(Path.Combine(unrelated,"keep.txt")),"Retention preserves unrelated files");
        var reportData=new PlayDiagnostics.Summary();reportData.players[0].player="<script>alert(1)</script>";
        Check(!PlayReport.Render(reportData).Contains("<script>alert(1)</script>"),"Local report escapes embedded text");
        string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Evidence/Prototype/rules-tests.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(new Report{passed=true,count=checks.Count,checks=checks.ToArray()},true));
        Debug.Log("ROBO_RULES_PASS "+checks.Count);
    }
    [Serializable] class Report{public bool passed;public int count;public string[] checks;}
}
