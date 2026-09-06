using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RoboOpen
{
    // Only instantiated by the explicit validation command-line flag.
    public class PrototypeInputChecks : MonoBehaviour
    {
        public TennisGame game;
        public string output;
        [Serializable] class Report { public bool passed;public List<string> checks=new List<string>();public List<string> failures=new List<string>(); }
        Report report=new Report();
        void Check(bool condition,string description){(condition?report.checks:report.failures).Add(description);}
        IEnumerator KeyPress(Key key,float seconds=.12f,Key modifier=Key.None)
        {
            InputSystem.QueueStateEvent(Keyboard.current,modifier==Key.None?new KeyboardState(key):new KeyboardState(key,modifier));yield return new WaitForSecondsRealtime(seconds);
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return new WaitForSecondsRealtime(.08f);
        }
        void ReturnFixture()
        {
            game.BeginMatch();game.phase=TennisGame.Phase.Rally;game.lastHitter=1;game.servingShot=false;game.bounces=1;
            game.player.transform.position=new Vector3(0,0,-8);game.ball.position=new Vector3(.5f,1.3f,-7.4f);game.velocity=Vector3.zero;
        }
        IEnumerator Click(string name)
        {
            var rt=GameObject.Find(name).GetComponent<RectTransform>();
            Vector2 p=RectTransformUtility.WorldToScreenPoint(game.gameCamera,rt.TransformPoint(rt.rect.center));
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p});yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p}.WithButton(MouseButton.Left));yield return new WaitForSecondsRealtime(.1f);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p});yield return new WaitForSecondsRealtime(.2f);
        }
        public IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1);
            Check(game.phase==TennisGame.Phase.Menu,"Starts at main menu");game.Capture(Path.Combine(output,"prototype-menu.png"));
            yield return Click("PLAY MATCH   >");Check(game.phase==TennisGame.Phase.Ready,"Pointer click starts match");
            game.Capture(Path.Combine(output,"prototype-serve.png"));
            yield return KeyPress(Key.Space);Check(game.phase==TennisGame.Phase.Serving,"Space begins visible serve preparation");
            var toss=game.ball.position;yield return KeyPress(Key.Escape);Check(game.paused,"Serve toss can be paused");
            toss=game.ball.position;yield return new WaitForSecondsRealtime(.2f);Check(Vector3.Distance(toss,game.ball.position)<.001f,"Serve toss freezes during pause");
            yield return KeyPress(Key.Escape);yield return new WaitForSecondsRealtime(1.5f);Check(game.phase==TennisGame.Phase.Rally,"Serve launches after trophy and contact frame");
            var p=game.player.transform.position;yield return KeyPress(Key.W,.2f);Check(game.player.transform.position.z>p.z+.2f,"W moves toward net");
            p=game.player.transform.position;yield return KeyPress(Key.D,.2f);Check(game.player.transform.position.x>p.x+.2f,"D moves right");
            float aim=game.aimX;yield return KeyPress(Key.RightArrow,.2f);Check(game.aimX>aim+.1f,"Right arrow changes shot aim");
            yield return KeyPress(Key.Escape);Check(game.paused,"Escape pauses");
            p=game.ball.position;yield return new WaitForSecondsRealtime(.3f);Check(Vector3.Distance(p,game.ball.position)<.001f,"Pause freezes ball simulation");
            game.Capture(Path.Combine(output,"prototype-pause.png"));
            yield return Click("RESUME");Check(!game.paused,"Resume button resumes");
            yield return KeyPress(Key.R);Check(game.phase==TennisGame.Phase.Ready&&game.pointsPlayed==0,"R resets match to serve");
            yield return KeyPress(Key.Escape);yield return Click("MAIN MENU");Check(game.phase==TennisGame.Phase.Menu,"Main menu button returns to menu");
            yield return Click("PLAY MATCH   >");Check(game.phase==TennisGame.Phase.Ready,"Match can be started again");
            ReturnFixture();yield return KeyPress(Key.Space,.05f);yield return new WaitForSecondsRealtime(.14f);float normal=game.velocity.y;Check(game.playerHits==1,"Space returns reachable incoming ball");
            ReturnFixture();yield return KeyPress(Key.Space,.05f,Key.LeftShift);yield return new WaitForSecondsRealtime(.14f);float power=game.velocity.y;Check(game.playerHits==1&&power<normal,"Shift + Space produces flatter power return");
            ReturnFixture();yield return KeyPress(Key.Space,.05f,Key.Z);yield return new WaitForSecondsRealtime(.14f);Check(game.playerHits==1&&game.velocity.y>normal,"Z + Space produces higher lob return");
            ReturnFixture();game.ball.position=new Vector3(-.65f,1.3f,-7.55f);
            yield return KeyPress(Key.Space,.05f);yield return new WaitForSecondsRealtime(.18f);
            Check(game.playerHits==1&&game.lastStroke=="Backhand","Ball on opposite side selects backhand");
            ReturnFixture();game.ball.position=new Vector3(.3f,2.32f,-7.7f);game.velocity=new Vector3(0,-.3f,0);game.bounces=0;
            Check(game.CanSmash,"Reachable descending high ball signals smash");
            yield return KeyPress(Key.Space,.05f);yield return new WaitForSecondsRealtime(.22f);
            Check(game.playerHits==1&&game.smashHits==1&&game.lastStroke=="Smash","Space automatically smashes suitable high ball without modifier");
            Check(game.maxContactError<.6f,"Accepted contacts stay within bounded racket assistance");
            game.Capture(Path.Combine(output,"motion-smash.png"));
            ReturnFixture();game.ball.position=new Vector3(.3f,2.32f,-7.7f);game.servingShot=true;game.bounces=0;
            yield return KeyPress(Key.Space,.05f);yield return new WaitForSecondsRealtime(.15f);
            Check(game.playerHits==0&&game.smashHits==0,"An incoming serve cannot be smashed before its bounce");
            ReturnFixture();game.ball.position=new Vector3(5.5f,2.2f,-6);
            yield return KeyPress(Key.Space,.05f);yield return new WaitForSecondsRealtime(.15f);
            Check(game.playerHits==0,"An unreachable high ball is not granted a smash");
            // A match-point fixture exercises the actual double-bounce, result and replay paths.
            game.BeginMatch();game.score.games[0]=1;game.score.points[0]=3;game.phase=TennisGame.Phase.Rally;
            game.lastHitter=0;game.servingShot=false;game.bounces=1;game.ball.position=new Vector3(3, .13f,4);game.velocity=Vector3.down;
            yield return new WaitForSecondsRealtime(.2f);
            Check(game.phase==TennisGame.Phase.Finished&&game.score.winner==0,"Double bounce at match point reaches result screen");
            game.Capture(Path.Combine(output,"prototype-result.png"));
            yield return Click("PLAY AGAIN");Check(game.phase==TennisGame.Phase.Ready&&game.score.winner<0,"Play Again resets finished match");
            report.passed=report.failures.Count==0;
            File.WriteAllText(Path.Combine(output,"prototype-input-tests.json"),JsonUtility.ToJson(report,true));
            Debug.Log("ROBO_INPUT_TESTS "+JsonUtility.ToJson(report));
            if(!Application.isEditor)Application.Quit(report.passed?0:1);
        }
    }
}
