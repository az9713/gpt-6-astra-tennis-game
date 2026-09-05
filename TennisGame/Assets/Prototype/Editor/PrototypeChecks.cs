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
        string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Evidence/Prototype/rules-tests.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(new Report{passed=true,count=checks.Count,checks=checks.ToArray()},true));
        Debug.Log("ROBO_RULES_PASS "+checks.Count);
    }
    [Serializable] class Report{public bool passed;public int count;public string[] checks;}
}
