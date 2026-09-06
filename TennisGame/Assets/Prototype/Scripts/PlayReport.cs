using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;

namespace RoboOpen
{
    public static class PlayReport
    {
        static string E(string value)=>WebUtility.HtmlEncode(value??"");
        public static string Render(PlayDiagnostics.Summary data)
        {
            var html=new StringBuilder(@"<!doctype html><html lang='en'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Robo Open - Play report</title><style>
*{box-sizing:border-box}body{background:#f3efe5;color:#173b33;font:17px/1.65 system-ui;margin:0}header{background:#173b33;color:#f4efd8;padding:30px 6vw}main{max-width:1200px;margin:auto;padding:30px 5vw}h1,h2{font-family:Georgia,serif;line-height:1.15}h1{font-size:44px}h2{font-size:30px}.players{display:grid;grid-template-columns:1fr 1fr;gap:24px}article,.replay{background:#fffdf4;padding:24px;border:1px solid #cbd6c7;border-radius:8px}.metric{font-size:36px;color:#b05235;margin:0}.small{font-size:13px;color:#576b60}.cause{border-top:1px solid #d5ddcc;padding:15px 0}.cause p{margin:8px 0}button,select{font:inherit;background:#e2e9d9;color:#173b33;padding:8px 12px;border:1px solid #98ad9e;border-radius:4px;margin:5px}canvas{display:block;background:#a34736;max-width:100%;margin:16px auto}input{width:100%}footer{margin-top:35px;font-size:13px}a{color:#236a59}@media(max-width:700px){.players{grid-template-columns:1fr}h1{font-size:34px}}
</style><header><div>ROBO OPEN / LOCAL PLAY REPORT</div><h1>What happened.<br>What to try next.</h1></header><main>");
            html.Append("<p>Build "+E(data.build)+" · "+E(data.mode)+" session · started "+E(data.startedUtc)+"</p>");
            if(data.mode!="human")html.Append("<p><strong>Validation session: these synthetic or automated scenarios do not assess a human player's performance.</strong></p>");
            html.Append("<p>This report uses recorded game decisions. It separates possible player improvements from game failures. Suggestions are hypotheses to test; they are not proof of skill or fault.</p><div class='players'>");
            foreach(var player in data.players)
            {
                int resolved=player.returned+player.missedReturns;
                string rate=resolved==0?"No completed returns yet":(100f*player.returned/resolved).ToString("0")+"% return completion";
                html.Append("<article><h2>"+E(player.player)+"</h2><p class='metric'>"+rate+"</p><p>"+player.returned+" returned · "+player.missedReturns+" missed · "+player.shotErrors+" outgoing errors</p><p>"+player.pointsWon+" points won · "+player.pointsLost+" lost · "+player.firstServeFaults+" first-serve faults</p><p class='small'>"+player.attempts+" attempts. Completion excludes incoming balls that ended as the opponent's shot error and interrupted exchanges.</p>");
                if(player.causes.Count==0)html.Append("<p>No diagnosed losses yet. Keep playing to collect evidence.</p>");
                foreach(var cause in player.causes.OrderByDescending(c=>c.count))
                {
                    html.Append("<div class='cause'><strong>"+E(cause.code.Replace('_',' '))+" · "+cause.count+"</strong><p>"+E(PlayDiagnostics.Advice(cause.code,player.player=="Mint"))+"</p></div>");
                }
                html.Append("</article>");
            }
            html.Append("</div><h2>Missed-return replays</h2><p>The most recent eight misses retain roughly three seconds before the outcome and up to one second after it. This is a sampled court trace, not a video or a deterministic physics replay. Orange: You. Mint: opponent. Yellow: ball. The height is shown separately.</p><div class='replay'><select id='cases' aria-label='Choose a missed return'></select><button id='play'>Play / pause</button><canvas id='court' width='420' height='650' aria-label='Top-down court replay'></canvas><input id='time' type='range' min='0' max='1' value='0' step='1' aria-label='Replay frame'><p id='frame'></p><p id='case'></p></div>");
            html.Append("<h2>Evidence behind the advice</h2><p>The latest 20 completed or interrupted exchanges. Window duration is sampled from the game's eligibility checks, not a measurement of human reaction time.</p><div style='overflow:auto'><table style='font-size:13px;text-align:left;border-spacing:12px'><thead><tr><th>Ball</th><th>Receiver</th><th>Attempts</th><th>Window (s)</th><th>Closest (m)</th><th>Outcome / cause</th></tr></thead><tbody>");
            foreach(var item in data.shots.Skip(Mathf.Max(0,data.shots.Count-20)))
                html.Append("<tr><td>"+item.id+"</td><td>"+(item.receiver==0?"You":"Mint")+"</td><td>"+item.attempts+"</td><td>"+item.windowSeconds.ToString("0.00")+"</td><td>"+(item.closest>=900?"not reached":item.closest.ToString("0.00"))+"</td><td>"+E(item.outcome)+" / "+E(item.cause)+"</td></tr>");
            html.Append("</tbody></table></div><h2>Recording quality</h2><p>"+data.focusLosses+" focus losses · "+data.slowFrames+" of "+data.frames+" frames exceeded 50 ms. These observations can affect interpretation but do not alone establish why a shot was missed.</p>");
            if(!data.recordingHealthy||data.sizeLimitReached)html.Append("<p><strong>Recording is incomplete: "+(!data.recordingHealthy?"a file write failed":"a recording size limit was reached")+".</strong></p>");
            html.Append("<p class='small'>Local files: events.jsonl contains timestamped decisions; windows.jsonl contains completed replay windows; summary.json contains both players' totals and recent exchanges. Match restarts remain in the same session. Recording stops at the per-file size limit. Older recorder-owned sessions are retained within a 20-session / approximately 100 MiB budget, checked on launch.</p><footer>Only gameplay data is recorded. Nothing is uploaded automatically. Session: "+E(data.session)+". Policy: "+E(data.policy)+".</footer>");
            html.Append("<script id='data' type='application/json'>"+JsonUtility.ToJson(data).Replace("<","\\u003c")+"</script>");
            html.Append(@"<script>
const data=JSON.parse(document.querySelector('#data').textContent),cases=document.querySelector('#cases'),slider=document.querySelector('#time'),canvas=document.querySelector('#court'),ctx=canvas.getContext('2d');let running=false,last=0;
data.replays.forEach((r,i)=>{let o=document.createElement('option');o.value=i;o.textContent='Ball '+r.shot+' · '+(r.actor===0?'You':'Mint')+' · '+r.cause.replaceAll('_',' ');cases.append(o)});
function draw(){ctx.fillStyle='#a34736';ctx.fillRect(0,0,420,650);ctx.strokeStyle='#f4efd8';ctx.lineWidth=2;const px=x=>210+x*26,py=z=>325-z*22;ctx.strokeRect(px(-4.115),py(11.885),8.23*26,23.77*22);ctx.beginPath();ctx.moveTo(px(-4.115),py(0));ctx.lineTo(px(4.115),py(0));ctx.moveTo(px(-4.115),py(6.4));ctx.lineTo(px(4.115),py(6.4));ctx.moveTo(px(-4.115),py(-6.4));ctx.lineTo(px(4.115),py(-6.4));ctx.moveTo(px(0),py(-6.4));ctx.lineTo(px(0),py(6.4));ctx.stroke();
let r=data.replays[Number(cases.value)||0];if(!r||!r.frames.length){document.querySelector('#frame').textContent='No missed-return replay recorded yet.';return}slider.max=r.frames.length-1;let f=r.frames[Math.min(Number(slider.value),r.frames.length-1)];for(let [p,c,size] of [[f.you,'#ffad50',10],[f.mint,'#78d6bb',10],[f.ball,'#f5e85b',6]]){ctx.fillStyle=c;ctx.beginPath();ctx.arc(px(p.x),py(p.z),size,0,Math.PI*2);ctx.fill()}document.querySelector('#frame').textContent='Time '+f.time.toFixed(2)+' s · ball height '+f.ball.y.toFixed(2)+' m · bounces '+f.bounces+' · '+f.phase+' · '+(f.swingHeld?'swing held':'swing released');document.querySelector('#case').textContent='HUD: '+f.hud+' | You: '+f.youCheck+' | Mint: '+f.mintCheck;}
cases.onchange=()=>{slider.value=0;draw()};slider.oninput=()=>{running=false;draw()};document.querySelector('#play').onclick=()=>running=!running;function tick(t){if(running&&t-last>=100){last=t;slider.value=Number(slider.value)>=Number(slider.max)?0:Number(slider.value)+1;draw()}requestAnimationFrame(tick)}draw();requestAnimationFrame(tick);
</script></main></html>");
            return html.ToString();
        }
    }
}
