using UnityEngine;

namespace RoboOpen
{
    public struct ReturnPlan
    {
        public bool available;
        public string reason;
        public float delay, contactError;
        public Vector3 target;
        public RobotActor.Stroke stroke;
    }

    // The HUD, human input and CPU use the same bounded contact forecast.
    public static class ReturnPlanner
    {
        public const float BufferSeconds = .48f, ContactTolerance = .60f;
        public const float MinHeight = .38f, MaxHeight = 2.75f;
        public static string ReachReason(RobotActor actor, Vector3 ball, int bounces, bool serve)
        {
            if (serve && bounces == 0) return "serve_must_bounce";
            if (bounces >= 2) return "second_bounce";
            if (actor.isCpu ? ball.z < .7f : ball.z > -.7f) return "wrong_side";
            if (ball.y < MinHeight) return "too_low";
            if (ball.y > MaxHeight) return "too_high";
            var d = ball - actor.transform.position; d.y = 0;
            return d.magnitude > (actor.isCpu ? 1.58f : 1.85f) ? "too_far" : "ready";
        }

        public static ReturnPlan Find(TennisGame game, RobotActor actor)
        {
            var plan = new ReturnPlan { reason = "not_receiving" };
            int side = actor.isCpu ? 1 : 0;
            if (game.phase != TennisGame.Phase.Rally || game.Receiver != side) return plan;
            if (game.StrikePending) { plan.reason = "strike_pending"; return plan; }
            if (game.ReturnCooldown > 0) { plan.reason = "cooldown"; return plan; }
            Vector3 ball = game.ball.position, velocity = game.velocity;
            int bounces = game.bounces;
            plan.reason = ReachReason(actor, ball, bounces, game.servingShot);
            // Match the live simulation's substeps, including a legal first bounce.
            for (int step = 1; step <= 34; step++)
            {
                if (!PredictStep(ref ball, ref velocity, ref bounces, game.servingShot, game.lastHitter, game.ServiceParity, 1f / 120f))
                { if (plan.reason == "ready") plan.reason = "ball_ends_before_contact"; break; }
                float delay = step / 120f;
                if (step < 10 || step % 3 != 1) continue;
                string reason = ReachReason(actor, ball, bounces, game.servingShot);
                if (reason != "ready") { if (plan.reason == "ready") plan.reason = reason; continue; }
                var stroke = ball.y >= 1.92f && velocity.y <= 1.5f ? RobotActor.Stroke.Smash :
                    actor.transform.InverseTransformPoint(ball).x < -.12f ? RobotActor.Stroke.Backhand : RobotActor.Stroke.Forehand;
                if (stroke == RobotActor.Stroke.Smash && delay < .12f) continue;
                float error = actor.PreviewContact(stroke, ball, true);
                if (error > ContactTolerance - .08f) { plan.reason = "racket_unreachable"; continue; }
                return new ReturnPlan { available = true, reason = "ready", delay = delay, target = ball, stroke = stroke, contactError = error };
            }
            if (plan.reason == "ready") plan.reason = "no_contact_window";
            return plan;
        }

        public static bool PredictStep(ref Vector3 ball, ref Vector3 velocity, ref int bounces, bool serve, int lastHitter, int parity, float dt)
        {
            var old = ball;
            ball += velocity * dt + Vector3.down * (.5f * CourtRules.Gravity * dt * dt);
            velocity.y -= CourtRules.Gravity * dt;
            if (old.z * ball.z < 0 && Mathf.Abs(ball.x) < 5.15f && Mathf.Lerp(old.y, ball.y, -old.z / (ball.z - old.z)) < CourtRules.NetHeight + CourtRules.BallRadius) return false;
            if (ball.y <= CourtRules.BallRadius && velocity.y < 0)
            {
                ball.y = CourtRules.BallRadius;
                if (bounces == 0 && !(serve ? CourtRules.InServiceBox(ball, 1 - lastHitter, parity) : CourtRules.InCourt(ball) && (lastHitter == 0 ? ball.z > 0 : ball.z < 0))) return false;
                if (++bounces >= 2) return false;
                velocity = new Vector3(velocity.x * .84f, Mathf.Abs(velocity.y) * .72f, velocity.z * .84f);
            }
            return Mathf.Abs(ball.z) <= 19 && Mathf.Abs(ball.x) <= 12;
        }
    }
}
