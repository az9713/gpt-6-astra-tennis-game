using System;
using UnityEngine;

namespace RoboOpen
{
    [Serializable]
    public class TennisScore
    {
        public int[] points = new int[2];
        public int[] games = new int[2];
        public int server, totalPoints, winner = -1;
        public int GamesToWin = 2;
        public string PointLabel(int player)
        {
            if (points[0] >= 3 && points[1] >= 3)
                return points[player] == points[1-player] ? "40" : points[player] > points[1-player] ? "AD" : "40";
            return new[] { "0", "15", "30", "40" }[Math.Min(points[player],3)];
        }
        public bool Award(int player)
        {
            if (winner >= 0) return false;
            points[player]++; totalPoints++;
            if (points[player] < 4 || points[player] - points[1-player] < 2) return false;
            games[player]++; points[0]=points[1]=0; server=1-server;
            if (games[player] >= GamesToWin) winner=player;
            return true;
        }
    }

    public static class CourtRules
    {
        public const float HalfWidth = 4.115f, HalfLength = 11.885f, ServiceLine = 6.4f, NetHeight = .96f, BallRadius = .12f;
        public const float Gravity = 9.81f;
        public static bool InCourt(Vector3 p) => Mathf.Abs(p.x) <= HalfWidth + BallRadius && Mathf.Abs(p.z) <= HalfLength + BallRadius;
        public static bool InServiceBox(Vector3 p, int receiver, int parity)
        {
            float side = receiver == 1 ? 1 : -1;
            float desiredX = (parity % 2 == 0 ? -1 : 1) * side;
            return InCourt(p) && p.z*side >= -BallRadius && p.z*side <= ServiceLine+BallRadius && p.x*desiredX >= -BallRadius;
        }
        public static Vector3 LaunchVelocity(Vector3 from, Vector3 target, float time)
        {
            if(time <= 0) throw new ArgumentOutOfRangeException(nameof(time));
            var v=(target-from)/time;
            v.y += .5f*Gravity*time;
            return v;
        }
        public static Vector3 Position(Vector3 origin, Vector3 velocity, float time) => origin+velocity*time+Vector3.down*(.5f*Gravity*time*time);
        public static float TimeToGround(Vector3 origin, Vector3 velocity)
            => (velocity.y+Mathf.Sqrt(velocity.y*velocity.y+2*Gravity*Mathf.Max(0,origin.y-BallRadius)))/Gravity;
    }
}
