using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace RoboOpen
{
    public class TennisHud : MonoBehaviour
    {
        public TennisGame game;
        Canvas canvas; RectTransform root;
        Font font;
        readonly Color cream=new Color(.98f,.95f,.86f),ink=new Color(.095f,.19f,.21f),coral=new Color(.85f,.29f,.19f),teal=new Color(.10f,.48f,.45f);
        Text playerPoint,cpuPoint,playerGames,cpuGames,status,rally,reason,matchTitle,serveLabel;
        GameObject menu,scorePanel,statusPanel,controls,pointPanel,pausePanel,finishPanel,brand,rallyPanel,legend;
        Sprite rounded;
        public void Build()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");rounded=Rounded();
            var go=new GameObject("Robo Open HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=game.gameCamera;canvas.planeDistance=1;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
            root=new GameObject("Layout",typeof(RectTransform)).GetComponent<RectTransform>();root.SetParent(go.transform,false);
            root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.sizeDelta=new Vector2(1600,900);Fit();
            if(EventSystem.current==null)new GameObject("Event System",typeof(EventSystem),typeof(InputSystemUIInputModule));

            scorePanel=Panel("Scoreboard",32,30,438,150,cream).gameObject;
            var s=(RectTransform)scorePanel.transform;
            Label("EXHIBITION  /  FIRST TO 2 GAMES",18,12,390,24,14,teal,s);
            Panel("Rule",18,43,400,2,new Color(.1f,.4f,.4f,.18f),s);
            Panel("You color",18,58,6,29,coral,s);Panel("CPU color",18,102,6,29,teal,s);
            Label("YOU",38,57,200,32,24,ink,s,true);Label("MINT",38,101,200,32,24,ink,s,true);
            playerPoint=Label("0",267,49,76,43,37,coral,s,true,TextAnchor.MiddleCenter);
            cpuPoint=Label("0",267,94,76,43,37,teal,s,true,TextAnchor.MiddleCenter);
            playerGames=Label("0",370,56,38,34,27,ink,s,true,TextAnchor.MiddleCenter);
            cpuGames=Label("0",370,101,38,34,27,ink,s,true,TextAnchor.MiddleCenter);
            Label("PTS",281,12,54,24,12,teal,s,false,TextAnchor.MiddleCenter);Label("G",373,12,34,24,12,teal,s,false,TextAnchor.MiddleCenter);

            brand=Panel("Brand",1284,30,282,99,cream).gameObject;
            Label("ROBO OPEN",18,11,247,40,32,ink,(RectTransform)brand.transform,true,TextAnchor.MiddleCenter);
            Label("SUNSET COURT  /  01",18,58,247,23,13,teal,(RectTransform)brand.transform,false,TextAnchor.MiddleCenter);
            statusPanel=Panel("Shot callout",538,32,540,75,new Color(.06f,.18f,.20f,.92f)).gameObject;
            status=Label("YOUR SERVE",14,10,510,32,23,cream,(RectTransform)statusPanel.transform,true,TextAnchor.MiddleCenter);
            serveLabel=Label("",14,44,510,20,13,new Color(.7f,.87f,.82f),(RectTransform)statusPanel.transform,false,TextAnchor.MiddleCenter);
            controls=Panel("Controls",32,790,1090,78,cream).gameObject;
            var c=(RectTransform)controls.transform;
            Label("W A S D",18,11,136,27,20,ink,c,true);Label("MOVE",18,42,136,20,12,teal,c);
            Label("SPACE",180,11,145,27,20,ink,c,true);Label("SERVE / SWING",180,42,145,20,12,teal,c);
            Label("ARROW KEYS",350,11,180,27,20,ink,c,true);Label("AIM LEFT / RIGHT / DEPTH",350,42,218,20,12,teal,c);
            Label("SHIFT + SPACE",595,11,205,27,20,ink,c,true);Label("POWER SHOT",595,42,180,20,12,teal,c);
            Label("Z + SPACE",830,11,145,27,20,ink,c,true);Label("LOB",830,42,120,20,12,teal,c);
            Button("II",1006,14,62,49,ink,cream,c,()=>game.Pause());
            var rp=Panel("Rally",1284,782,282,86,cream);rallyPanel=rp.gameObject;
            rally=Label("RALLY  0",14,8,250,38,27,coral,rp,true,TextAnchor.MiddleCenter);
            Label("KEEP IT IN PLAY",14,48,250,22,12,teal,rp,false,TextAnchor.MiddleCenter);
            // The landing marker and aim marker use distinct colors, also named in the HUD.
            legend=Label("YELLOW = BALL LANDING     TEAL = YOUR AIM",1170,733,397,30,12,cream,root,true,TextAnchor.MiddleRight).gameObject;

            pointPanel=Panel("Point announcement",505,309,590,205,cream).gameObject;
            reason=Label("",22,32,546,72,37,ink,(RectTransform)pointPanel.transform,true,TextAnchor.MiddleCenter);
            Label("NEXT POINT IN A MOMENT",22,139,546,23,14,teal,(RectTransform)pointPanel.transform,false,TextAnchor.MiddleCenter);

            menu=Panel("Welcome",70,231,565,486,cream).gameObject;
            var m=(RectTransform)menu.transform;
            Label("THE ROBOT TENNIS CLUB",32,27,480,23,14,teal,m,true);
            Label("ROBO",28,65,505,100,80,ink,m,true);
            Label("OPEN",28,159,505,100,80,ink,m,true);
            Panel("Accent",32,266,94,5,coral,m);
            Label("One court. Two robots. Your first serve.",32,291,490,62,23,ink,m);
            Button("PLAY MATCH   >",32,371,500, height:70, bg:coral,fg:cream,parent:m,action:()=>game.BeginMatch());
            Label("Move with WASD. Press SPACE near the ball to return.",32,450,498,23,13,teal,m);

            pausePanel=Panel("Pause",505,285,590,325,cream).gameObject;
            var p=(RectTransform)pausePanel.transform;
            Label("TAKE A BREATHER",25,32,540,49,34,ink,p,true,TextAnchor.MiddleCenter);
            Button("RESUME",44,112,502,61,coral,cream,p,()=>game.Pause());
            Button("RESTART MATCH",44,189,244,58,ink,cream,p,()=>game.BeginMatch());
            Button("MAIN MENU",305,189,242,58,teal,cream,p,()=>game.ReturnToMenu());
            Label("ESC to resume  /  R to restart",25,270,540,22,14,teal,p,false,TextAnchor.MiddleCenter);

            finishPanel=Panel("Match result",505,276,590,338,cream).gameObject;
            var f=(RectTransform)finishPanel.transform;
            Label("MATCH COMPLETE",25,28,540,23,14,teal,f,true,TextAnchor.MiddleCenter);
            matchTitle=Label("YOU WIN",25,76,540,110,42,ink,f,true,TextAnchor.MiddleCenter);
            Button("PLAY AGAIN",44,214,502,69,coral,cream,f,()=>game.BeginMatch());
            Refresh();
        }
        public void Refresh()
        {
            if(canvas==null)return;
            bool playing=game.phase!=TennisGame.Phase.Menu;
            menu.SetActive(!playing);scorePanel.SetActive(playing);statusPanel.SetActive(playing);controls.SetActive(playing);brand.SetActive(playing);rallyPanel.SetActive(playing);legend.SetActive(playing);
            pointPanel.SetActive(game.phase==TennisGame.Phase.Point&&!game.paused);pausePanel.SetActive(game.paused);finishPanel.SetActive(game.phase==TennisGame.Phase.Finished);
            playerPoint.text=game.score.PointLabel(0);cpuPoint.text=game.score.PointLabel(1);playerGames.text=game.score.games[0].ToString();cpuGames.text=game.score.games[1].ToString();
            status.text=game.message;serveLabel.text=game.CanPlayerHit?"THE BALL IS IN REACH":"ESC  PAUSE   /   R  RESTART";
            rally.text="RALLY  "+game.rally;reason.text=game.message+"\n<size=18>"+game.pointReason+"</size>";
            matchTitle.text=game.message+"\n<size=22>"+game.score.games[0]+"  –  "+game.score.games[1]+"   /   Best rally "+game.bestRally+"</size>";
        }
        RectTransform Place(GameObject go,float x,float y,float w,float h,RectTransform parent)
        {
            var rt=go.GetComponent<RectTransform>();rt.SetParent(parent??root,false);rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);return rt;
        }
        RectTransform Panel(string name,float x,float y,float w,float h,Color color,RectTransform parent=null)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));var rt=Place(go,x,y,w,h,parent);
            var image=go.GetComponent<Image>();image.sprite=rounded;image.type=Image.Type.Sliced;image.color=color;image.raycastTarget=false;
            return rt;
        }
        Text Label(string value,float x,float y,float w,float h,int size,Color color,RectTransform parent,bool bold=false,TextAnchor align=TextAnchor.UpperLeft)
        {
            var go=new GameObject("Text "+value.Split('\n')[0],typeof(RectTransform),typeof(Text));Place(go,x,y,w,h,parent);
            var text=go.GetComponent<Text>();text.font=font;text.text=value;text.fontSize=size;text.color=color;text.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;text.alignment=align;text.raycastTarget=false;text.supportRichText=true;
            return text;
        }
        public void Fit()
        {
            if(canvas==null)return;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.scaleFactor=Mathf.Min(game.gameCamera.pixelWidth/1600f,game.gameCamera.pixelHeight/900f);
            canvas.scaleFactor=scaler.scaleFactor;Canvas.ForceUpdateCanvases();
        }
        void LateUpdate(){Fit();}
        void Button(string title,float x,float y,float w,float height,Color bg,Color fg,RectTransform parent,UnityEngine.Events.UnityAction action)
        {
            var r=Panel(title,x,y,w,height,bg,parent);var img=r.GetComponent<Image>();img.raycastTarget=true;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=img;var colors=b.colors;colors.highlightedColor=new Color(1.12f,1.12f,1.12f);colors.pressedColor=new Color(.8f,.8f,.8f);b.colors=colors;b.onClick.AddListener(action);
            Label(title,8,0,w-16,height,20,fg,r,true,TextAnchor.MiddleCenter);
        }
        Sprite Rounded()
        {
            const int size=32;var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);tex.filterMode=FilterMode.Bilinear;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {float dx=Mathf.Max(7-x,x-24),dy=Mathf.Max(7-y,y-24);float d=new Vector2(Mathf.Max(0,dx),Mathf.Max(0,dy)).magnitude;tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(8-d)));}
            tex.Apply();return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(9,9,9,9));
        }
    }
}
