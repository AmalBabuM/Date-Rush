using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
namespace DateRush
{
    public sealed class DateRushUI : MonoBehaviour
    {
        sealed class Floating {public Text text;public Vector2 origin;public float life=1.2f;}
        DateRushGame game;
        RectTransform root,hud,modal,effects;
        Text targetName,scoreLabel,harvestLabel,timeLabel,notice,soundLabel,fact,comboLabel;
        Image targetIcon,progress;
        Font bodyFont,titleFont;
        float noticeLife;
        readonly List<Floating> floating=new List<Floating>();
        public void Initialise(DateRushGame owner)
        {
            game=owner;
            bodyFont=Resources.Load<Font>("BodyFont")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleFont=Resources.Load<Font>("TitleFont")??bodyFont;
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=game.GameCamera;canvas.planeDistance=5;canvas.sortingOrder=100;
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=.5f;
            gameObject.AddComponent<GraphicRaycaster>();root=GetComponent<RectTransform>();
            if(FindAnyObjectByType<EventSystem>()==null)
            {var system=new GameObject("UI Event System",typeof(EventSystem),typeof(InputSystemUIInputModule));system.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            hud=Group("Harvest HUD",root);effects=Group("Catch feedback",root);modal=Group("Menus",root);
            BuildHud();
        }
        RectTransform Group(string name,Transform parent)
        {
            var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var r=obj.GetComponent<RectTransform>();
            r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;
        }
        RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var r=obj.GetComponent<RectTransform>();
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        Image Image(string name,Transform parent,float x,float y,float w,float h,Color color,Sprite sprite=null)
        {
            var r=Rect(name,parent,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=color;image.sprite=sprite??DateRushArt.Shape("rounded");image.type=sprite==null?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;image.raycastTarget=false;return image;
        }
        Text Text(string name,Transform parent,float x,float y,float w,float h,string content,int size,Color color,bool bold=false,TextAnchor align=TextAnchor.MiddleLeft,bool title=false)
        {
            var r=Rect(name,parent,x,y,w,h);var text=r.gameObject.AddComponent<Text>();text.font=title?titleFont:bodyFont;text.text=content;text.fontSize=size;text.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;text.color=color;text.alignment=align;text.raycastTarget=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            return text;
        }
        RectTransform Panel(string name,Transform parent,float x,float y,float w,float h)
        {
            Image(name+" shadow",parent,x+5,y+8,w,h,new Color(0,0,0,.3f));
            var border=Image(name,parent,x,y,w,h,DateRushArt.Gold).rectTransform;
            Image("Dark walnut",border,2,2,w-4,h-4,new Color(.18f,.09f,.05f,.98f));return border;
        }
        Button Button(string name,Transform parent,float x,float y,float w,float h,string content,Action action,bool primary=false,int fontSize=20)
        {
            var img=Image(name,parent,x,y,w,h,primary?DateRushArt.Gold:new Color(.36f,.21f,.12f));img.raycastTarget=true;
            var button=img.gameObject.AddComponent<Button>();button.targetGraphic=img;var colors=button.colors;
            colors.highlightedColor=new Color(1,1,.87f);colors.pressedColor=new Color(.76f,.68f,.55f);button.colors=colors;
            Text("Label",img.transform,8,0,w-16,h,content,fontSize,primary?DateRushArt.Ink:DateRushArt.Cream,true,TextAnchor.MiddleCenter);
            button.onClick.AddListener(()=>action());return button;
        }
        void BuildHud()
        {
            Image("Top rail",hud,0,0,1600,106,new Color(.1f,.05f,.03f,.96f));
            Text("Brand",hud,35,15,230,43,"DATE RUSH",31,DateRushArt.Gold,true,title:true);
            Text("Subline",hud,37,60,240,24,"CATCH  /  LEARN  /  DISCOVER",12,DateRushArt.Cream);
            var targetPanel=Panel("Target date",hud,309,13,338,77);
            targetIcon=Image("Target portrait",targetPanel,16,14,78,50,Color.white,DateRushArt.Date(0));targetIcon.preserveAspect=true;
            Text("Target caption",targetPanel,110,10,200,22,"CATCH THIS DATE",13,DateRushArt.Gold,true);
            targetName=Text("Target name",targetPanel,110,30,210,40,"KHALAS",28,DateRushArt.Cream,true);
            Text("Harvest caption",hud,687,19,180,21,"HARVEST",13,DateRushArt.Gold,true);
            harvestLabel=Text("Harvest count",hud,687,40,180,43,"0 / 15",30,DateRushArt.Cream,true);
            Text("Score caption",hud,893,19,180,21,"SCORE",13,DateRushArt.Gold,true);
            scoreLabel=Text("Score",hud,893,40,180,43,"0",30,DateRushArt.Cream,true);
            Text("Timer caption",hud,1087,19,180,21,"TIME LEFT",13,DateRushArt.Gold,true);
            timeLabel=Text("Time",hud,1087,40,180,43,"90s",30,DateRushArt.Cream,true);
            var sound=Button("Sound",hud,1308,29,113,49,"SOUND ON",game.ToggleSound,fontSize:15);soundLabel=sound.GetComponentInChildren<Text>();
            Button("Pause",hud,1440,29,125,49,"II  PAUSE",game.Pause,fontSize:16);
            Image("Progress track",hud,0,103,1600,4,new Color(.38f,.22f,.11f));
            progress=Image("Harvest progress",hud,0,103,0,4,DateRushArt.Gold);
            var sign=Panel("Learning sign",hud,40,594,258,145);
            Text("Sign heading",sign,18,13,222,24,"FROM THE DATE GARDEN",13,DateRushArt.Gold,true);
            fact=Text("Date fact",sign,18,44,222,87,"",17,DateRushArt.Cream);
            notice=Text("Target change announcement",hud,360,523,880,49,"",26,DateRushArt.Cream,true,TextAnchor.MiddleCenter);
            var outline=notice.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.15f,.06f,.02f,.9f);outline.effectDistance=new Vector2(2,-2);
            comboLabel=Text("Combo",hud,1292,594,268,52,"",27,DateRushArt.Gold,true,TextAnchor.MiddleRight);
            Image("Control rail",hud,0,823,1600,77,new Color(.1f,.05f,.03f,.96f));
            var left=Button("Move left",hud,28,837,68,49,"<",()=>{},fontSize:28);Hold(left,-1);
            var right=Button("Move right",hud,107,837,68,49,">",()=>{},fontSize:28);Hold(right,1);
            Text("Keyboard controls",hud,207,833,938,57,"A / D  MOVE      SPACE  JUMP      SHIFT  DASH      P  PAUSE\nMouse or touch: drag along the floor to move your basket",16,DateRushArt.Cream,false,TextAnchor.MiddleCenter);
            Button("Touch jump",hud,1205,837,155,49,"JUMP",game.Player.Jump,fontSize:19);
            Button("Touch dash",hud,1378,837,180,49,"DASH",game.Player.Dash,fontSize:19);
        }
        void Hold(Button button,int direction)
        {
            var trigger=button.gameObject.AddComponent<EventTrigger>();
            void Add(EventTriggerType type,Action callback){var entry=new EventTrigger.Entry{eventID=type};entry.callback.AddListener(_=>callback());trigger.triggers.Add(entry);}
            Add(EventTriggerType.PointerDown,()=>game.Player.TouchAxis=direction);
            Add(EventTriggerType.PointerUp,()=>game.Player.TouchAxis=0);
            Add(EventTriggerType.PointerExit,()=>game.Player.TouchAxis=0);
        }
        void ClearModal()
        {
            foreach(Transform child in modal){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            foreach(var item in floating)if(item.text!=null)Destroy(item.text.gameObject);floating.Clear();
            game.Player.TouchAxis=0;
        }
        RectTransform ModalPanel(string name,float x,float y,float w,float h)
        {
            ClearModal();hud.gameObject.SetActive(false);modal.gameObject.SetActive(true);
            var shade=Image("Dim market",modal,0,0,1600,900,new Color(.07f,.03f,.01f,.73f));shade.raycastTarget=true;
            return Panel(name,modal,x,y,w,h);
        }
        public void ShowHud(){ClearModal();modal.gameObject.SetActive(false);hud.gameObject.SetActive(true);UpdateHud();}
        public void ShowMenu()
        {
            var p=ModalPanel("Welcome to the market",416,116,768,638);
            Text("Kicker",p,48,27,672,28,"A LITTLE HARVEST. A BIG DISCOVERY.",16,DateRushArt.Gold,true,TextAnchor.MiddleCenter);
            Text("Title",p,28,76,712,96,"Date Rush",76,DateRushArt.Cream,true,TextAnchor.MiddleCenter,true);
            Text("Tagline",p,48,182,672,43,"Catch the flavour of the Emirates",25,DateRushArt.Gold,false,TextAnchor.MiddleCenter);
            for(int i=0;i<6;i++)
            {Image("Date portrait",p,68+i*107,258,84,57,Color.white,DateRushArt.Date(i)).preserveAspect=true;Text("Variety",p,60+i*107,322,99,26,DateCatalog.All[i].name,15,DateRushArt.Cream,true,TextAnchor.MiddleCenter);}
            Text("How to play",p,80,373,608,70,"Watch the target. Move your basket.\nCatch 15 matching dates before the market closes!",23,DateRushArt.Cream,false,TextAnchor.MiddleCenter);
            Text("Rewards",p,76,454,616,29,"MATCH +100   /   WRONG -25   /   STREAKS EARN MORE",15,DateRushArt.Gold,true,TextAnchor.MiddleCenter);
            Button("Start harvest",p,128,507,512,64,"LET'S HARVEST  >",game.BeginRound,true,24);
            Button("View collection",p,128,586,245,38,"DATE COLLECTION",game.Collection,fontSize:15);
            var s=Button("Menu sound",p,393,586,247,38,game.Muted?"SOUND OFF":"SOUND ON",game.ToggleSound,fontSize:15);soundLabel=s.GetComponentInChildren<Text>();
            Text("Personal best",modal,500,779,600,32,"PERSONAL BEST  /  "+game.Best.ToString("N0"),17,DateRushArt.Cream,true,TextAnchor.MiddleCenter);
        }
        public void ShowPause()
        {
            var p=ModalPanel("Paused",510,236,580,417);
            Text("Title",p,40,35,500,67,"Take a breather",40,DateRushArt.Cream,true,TextAnchor.MiddleCenter,true);
            Text("Subtitle",p,45,112,490,58,"Your harvest will be right here.",22,DateRushArt.Gold,false,TextAnchor.MiddleCenter);
            Button("Resume",p,80,203,420,64,"KEEP CATCHING  >",game.Resume,true,23);
            Button("Restart",p,80,292,199,51,"RESTART",game.BeginRound);
            Button("Main menu",p,299,292,201,51,"MAIN MENU",game.Menu);
        }
        public void ShowResults()
        {
            var p=ModalPanel("Harvest results",410,134,780,634);
            Text("Kicker",p,45,31,690,29,game.Won?"THE MARKET IS CELEBRATING!":"A LITTLE PRACTICE, A SWEETER HARVEST",16,DateRushArt.Gold,true,TextAnchor.MiddleCenter);
            Text("Title",p,45,80,690,75,game.Won?"Beautiful harvest!":"Keep growing!",48,DateRushArt.Cream,true,TextAnchor.MiddleCenter,true);
            Text("Score",p,45,168,690,65,game.Score.ToString("N0")+" POINTS",43,DateRushArt.Gold,true,TextAnchor.MiddleCenter);
            Text("Stats",p,50,246,680,50,game.Caught+" dates caught   /   "+game.Missed+" missed   /   Best "+game.Best,21,DateRushArt.Cream,false,TextAnchor.MiddleCenter);
            Text("Learning",p,95,324,590,83,"Every harvest is a new discovery.\nExplore your collection and meet the date varieties!",23,DateRushArt.Cream,false,TextAnchor.MiddleCenter);
            Button("Play again",p,135,444,510,64,"HARVEST AGAIN  >",game.BeginRound,true,24);
            Button("Collection",p,135,529,244,51,"COLLECTION",game.Collection);
            Button("Main menu",p,399,529,246,51,"MAIN MENU",game.Menu);
        }
        public void ShowCollection(){CollectionPage(0);}
        void CollectionPage(int selected)
        {
            var p=ModalPanel("Date collection",229,106,1142,684);
            Text("Title",p,45,28,1052,63,"The date collection",44,DateRushArt.Cream,true,TextAnchor.MiddleCenter,true);
            Text("Subtitle",p,45,99,1052,31,"Meet the varieties. Collect them as you play.",21,DateRushArt.Gold,false,TextAnchor.MiddleCenter);
            for(int i=0;i<6;i++)
            {
                int index=i;float x=49+i*177;
                var card=Button("Discover "+DateCatalog.All[i].name,p,x,169,159,176,"",()=>CollectionPage(index));
                if(i==selected)card.GetComponent<Image>().color=new Color(.54f,.33f,.16f);
                Image("Date",card.transform,27,19,105,73,Color.white,DateRushArt.Date(i)).preserveAspect=true;
                Text("Name",card.transform,4,97,151,33,DateCatalog.All[i].name,22,DateRushArt.Cream,true,TextAnchor.MiddleCenter);
                Text("Discovered",card.transform,4,135,151,29,game.Discoveries[i]>0?game.Discoveries[i]+" caught":"Ready to discover",14,DateRushArt.Gold,false,TextAnchor.MiddleCenter);
            }
            Text("Selected name",p,110,387,922,47,DateCatalog.All[selected].name.ToUpper(),29,DateRushArt.Gold,true,TextAnchor.MiddleCenter);
            Text("Selected fact",p,126,447,890,82,DateCatalog.All[selected].description,25,DateRushArt.Cream,false,TextAnchor.MiddleCenter);
            Button("Back",p,341,576,460,62,"BACK TO THE MARKET",game.Menu,true,22);
        }
        public void UpdateHud()
        {
            if(targetName==null)return;
            targetName.text=DateCatalog.All[game.Target].name.ToUpper();targetIcon.sprite=DateRushArt.Date(game.Target);
            scoreLabel.text=game.Score.ToString("N0");harvestLabel.text=game.Caught+" / "+game.harvestGoal;timeLabel.text=Mathf.CeilToInt(game.Remaining)+"s";
            timeLabel.color=game.Remaining<15?DateRushArt.Coral:DateRushArt.Cream;
            progress.rectTransform.sizeDelta=new Vector2(1600f*game.Caught/game.harvestGoal,4);
            fact.text=DateCatalog.All[game.Target].description;comboLabel.text=game.Combo>1?"STREAK x"+game.Combo:"";
        }
        public void RefreshSound()
        {
            // Menus are rebuilt infrequently, so find the current sound buttons rather than keeping stale references.
            foreach(var t in GetComponentsInChildren<Text>(true))if(t.transform.parent.name=="Sound" || t.transform.parent.name=="Menu sound")t.text=game.Muted?"SOUND OFF":"SOUND ON";
        }
        public void Notice(string text,Color color){notice.text=text;notice.color=color;noticeLife=2.7f;}
        public void FloatText(Vector2 at,string content,Color color)
        {
            at.x=Mathf.Clamp(at.x,170,1430);
            var t=Text("Catch reward",effects,at.x-200,at.y,400,48,content,25,color,true,TextAnchor.MiddleCenter);
            var outline=t.gameObject.AddComponent<Outline>();outline.effectColor=DateRushArt.Ink;outline.effectDistance=new Vector2(2,-2);
            floating.Add(new Floating{text=t,origin=t.rectTransform.anchoredPosition});
        }
        void Update()
        {
            if(game.State!=RoundState.Playing)return;
            float dt=Time.deltaTime;noticeLife-=dt;notice.enabled=noticeLife>0;
            for(int i=floating.Count-1;i>=0;i--)
            {
                var f=floating[i];f.life-=dt;
                if(f.life<=0){Destroy(f.text.gameObject);floating.RemoveAt(i);continue;}
                f.text.rectTransform.anchoredPosition=f.origin+new Vector2(0,(1.2f-f.life)*45);var c=f.text.color;c.a=Mathf.Min(1,f.life*2);f.text.color=c;
            }
        }
    }
}

