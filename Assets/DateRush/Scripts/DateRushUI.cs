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
        const int TextSampleScale = 2;
        sealed class Floating {public Text text;public Vector2 origin;public float life=1.2f;}
        DateRushGame game;
        Canvas canvas;
        float textCanvasScale;
        RectTransform root,hud,modal,effects;
        Text notice,soundLabel;
        Text musicLabel;
        Text waveCountdownLabel;
        Text targetDateLabel;
        Image noticeFrame,targetDateIcon,targetDateFrame,targetDateBackground;
        Font bodyFont,titleFont;
        float noticeLife;
        bool showingAboutDates;
        bool aboutDatesFromMenu;
        bool wavePause;
        float waveCountdown;
        internal bool IsWaveTransition=>wavePause;
        readonly List<Floating> floating=new List<Floating>();
        public void Initialise(DateRushGame owner)
        {
            game=owner;
            bodyFont=Resources.Load<Font>("BodyFont")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleFont=Resources.Load<Font>("TitleFont")??bodyFont;
            canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=game.GameCamera;canvas.planeDistance=5;canvas.sortingOrder=100;
            canvas.pixelPerfect=true;
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
            // Generate higher-resolution glyphs while keeping the label's on-screen size.
            var r=Rect(name,parent,x,y,w*TextSampleScale,h*TextSampleScale);
            r.localScale=new Vector3(1f/TextSampleScale,1f/TextSampleScale,1);
            var text=r.gameObject.AddComponent<Text>();text.font=title?titleFont:bodyFont;text.text=content;text.fontSize=size*TextSampleScale;text.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;text.color=color;text.alignment=align;text.raycastTarget=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            return text;
        }
        RectTransform Panel(string name,Transform parent,float x,float y,float w,float h)
        {
            Image(name+" shadow",parent,x+5,y+8,w,h,new Color(0,0,0,.3f));
            var border=Image(name,parent,x,y,w,h,DateRushArt.Gold).rectTransform;
            Image("Dark walnut",border,2,2,w-4,h-4,new Color(.18f,.09f,.05f,.98f));return border;
        }
        Button Button(string name,Transform parent,float x,float y,float w,float h,string content,Action action,bool primary=false,int fontSize=20,Sprite sprite=null)
        {
            var img=Image(name,parent,x,y,w,h,primary?DateRushArt.Gold:new Color(.36f,.21f,.12f),sprite);img.raycastTarget=true;
            var button=img.gameObject.AddComponent<Button>();button.targetGraphic=img;var colors=button.colors;
            colors.highlightedColor=new Color(1,1,.87f);colors.pressedColor=new Color(.76f,.68f,.55f);button.colors=colors;
            if(!string.IsNullOrEmpty(content))
                Text("Label",img.transform,8,0,w-16,h,content,fontSize,primary?DateRushArt.Ink:DateRushArt.Cream,true,TextAnchor.MiddleCenter);
            button.onClick.AddListener(()=>{game.EnsureMusicPlaying();action();});return button;
        }
        void BuildHud()
        {
            var customPause=game.PauseButtonSprite;
            var pause=Button("Pause",hud,14,14,56,56,"",game.Pause,sprite:customPause?customPause:DateRushArt.Shape("oval"));
            pause.image.preserveAspect=true;
            pause.image.color=customPause?Color.white:new Color(.65f,.38f,.22f);
            if(!customPause)
            {
                Image("Left pause bar",pause.transform,19,18,6,20,DateRushArt.Cream);
                Image("Right pause bar",pause.transform,31,18,6,20,DateRushArt.Cream);
            }
            noticeFrame=Image("Notice rounded white outline",hud,590,493,420,38,Color.white,DateRushArt.Shape("rounded border"));
            noticeFrame.type=UnityEngine.UI.Image.Type.Sliced;
            noticeFrame.enabled=false;
            notice=Text("Target change announcement",hud,598,494,404,36,"",18,Color.white,true,TextAnchor.MiddleCenter);
            var outline=notice.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.15f,.06f,.02f,.9f);outline.effectDistance=new Vector2(2,-2)*TextSampleScale;
            targetDateFrame=Image("Target date white outline",hud,24,818,220,58,Color.white,DateRushArt.Shape("rounded border"));
            targetDateFrame.type=UnityEngine.UI.Image.Type.Sliced;
            targetDateBackground=Image("Target date background",hud,27,821,214,52,new Color(0,0,0,.72f));
            targetDateIcon=Image("Target date fruit",hud,34,825,62,44,Color.white,game.GetDateSprite(game.Target));
            targetDateIcon.preserveAspect=true;
            Text("Target date kicker",hud,104,822,128,17,"CATCH THIS DATE",12,DateRushArt.Gold,true,TextAnchor.MiddleLeft);
            targetDateLabel=Text("Target date name",hud,104,838,128,27,DateCatalog.All[game.Target].name.ToUpper(),19,Color.white,true,TextAnchor.MiddleLeft);
            RefreshTargetDate();
        }
        void ClearModal()
        {
            showingAboutDates=false;
            foreach(Transform child in modal){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            foreach(var item in floating)if(item.text!=null)Destroy(item.text.gameObject);floating.Clear();
        }
        RectTransform ModalPanel(string name,float x,float y,float w,float h)
        {
            ClearModal();hud.gameObject.SetActive(false);modal.gameObject.SetActive(true);
            var shade=Image("Dim market",modal,0,0,1600,900,new Color(.07f,.03f,.01f,.73f));shade.raycastTarget=true;
            return Panel(name,modal,x,y,w,h);
        }
        public void ShowHud()
        {
            wavePause=false;waveCountdown=0;waveCountdownLabel=null;noticeLife=0;
            notice.enabled=false;noticeFrame.enabled=false;RefreshTargetDate();
            ClearModal();modal.gameObject.SetActive(false);hud.gameObject.SetActive(true);
        }
        void RefreshTargetDate()
        {
            if(!targetDateIcon||!targetDateLabel)return;
            targetDateIcon.sprite=game.GetDateSprite(game.Target);
            targetDateLabel.text=DateCatalog.All[game.Target].name.ToUpper();
        }
        public void ShowMenu()
        {
            aboutDatesFromMenu=false;
            PrepareFullScreenMenu();
            Text("Title",modal,400,175,800,110,"DATE RUSH",76,DateRushArt.Cream,true,TextAnchor.MiddleCenter,true);
            Button("Start game",modal,560,345,480,72,"START GAME",game.BeginRound,true,25);
            Button("Game settings",modal,560,440,480,72,"GAME SETTINGS",ShowSettings,fontSize:23);
            Button("All dates info",modal,560,535,480,72,"ALL DATES INFO",ShowAllDates,fontSize:23);
            var music=Button("Menu music setting",modal,560,630,480,62,game.MusicEnabled?"MUSIC: ON":"MUSIC: OFF",game.ToggleMusic,fontSize:22);
            musicLabel=music.GetComponentInChildren<Text>();
        }
        void PrepareFullScreenMenu()
        {
            ClearModal();hud.gameObject.SetActive(false);modal.gameObject.SetActive(true);
            var menuBackground=Image("Menu background image",modal,0,0,1600,900,Color.white,DateRushArt.Load("MenuBackground"));
            menuBackground.preserveAspect=true;
            var shade=Image("Menu background shade",modal,0,0,1600,900,new Color(.07f,.03f,.01f,.2f));shade.raycastTarget=true;
        }
        void ShowSettings()
        {
            PrepareFullScreenMenu();
            Text("Title",modal,300,145,1000,80,"GAME SETTINGS",48,DateRushArt.Cream,true,TextAnchor.MiddleCenter,true);
            Text("How to play",modal,360,252,880,135,"Catch the date shown in your briefing. Every 3 correct catches starts a new wave and reveals a new date. Pause during a wave to review its date information.",23,DateRushArt.Cream,false,TextAnchor.MiddleCenter);
            Text("Controls",modal,360,400,880,125,"MOVE   A / D or Left / Right Arrow\nJUMP   Space, W, or Up Arrow\nDASH   Shift",23,DateRushArt.Cream,false,TextAnchor.MiddleCenter);
            var sound=Button("Sound effects setting",modal,600,535,400,54,game.Muted?"SOUND EFFECTS: OFF":"SOUND EFFECTS: ON",game.ToggleSound,true,19);
            soundLabel=sound.GetComponentInChildren<Text>();
            var music=Button("Music setting",modal,600,598,400,54,game.MusicEnabled?"MUSIC: ON":"MUSIC: OFF",game.ToggleMusic,true,19);
            musicLabel=music.GetComponentInChildren<Text>();
            Button("Back to menu",modal,600,661,400,54,"BACK TO MENU",ShowMenu,fontSize:21);
        }
        void ShowAllDates()
        {
            aboutDatesFromMenu=true;
            ShowAboutDates();
        }
        public void ShowPause()=>ShowPause(false);
        public void ShowWavePause()=>ShowPause(true);
        void ShowPause(bool waveChange)
        {
            wavePause=waveChange;
            waveCountdown=waveChange?5f:0f;
            var p=ModalPanel(waveChange?"Next wave":"Paused",460,waveChange?210:146,680,waveChange?480:610);
            var target=DateCatalog.All[game.Target];
            if(waveChange)
            {
                Text("Title",p,40,32,600,57,"WAVE "+(game.Caught/3+1),38,DateRushArt.Cream,true,TextAnchor.MiddleCenter,true);
                Image("Current target date",p,267,114,146,100,Color.white,game.GetDateSprite(game.Target)).preserveAspect=true;
                Text("Target name",p,45,224,590,43,target.name,30,DateRushArt.Gold,true,TextAnchor.MiddleCenter);
                Text("Target fact",p,68,275,544,95,target.description,20,DateRushArt.Cream,false,TextAnchor.MiddleCenter);
                Text("Countdown label",p,50,367,580,24,"CONTINUING IN",15,DateRushArt.Gold,true,TextAnchor.MiddleCenter);
                waveCountdownLabel=Text("Automatic wave countdown",p,50,390,580,51,"3",39,DateRushArt.Cream,true,TextAnchor.MiddleCenter);
                var waveMusic=Button("Wave pause music setting",p,184,444,312,42,game.MusicEnabled?"MUSIC: ON":"MUSIC: OFF",game.ToggleMusic,fontSize:17);
                musicLabel=waveMusic.GetComponentInChildren<Text>();
                return;
            }
            waveCountdownLabel=null;
            Text("Title",p,40,26,600,57,"Game paused",38,DateRushArt.Cream,true,TextAnchor.MiddleCenter,true);
            Image("Current target date",p,267,96,146,100,Color.white,game.GetDateSprite(game.Target)).preserveAspect=true;
            Text("Wave",p,50,202,580,27,"WAVE "+(game.Caught/3+1)+"  •  CATCH THIS DATE",16,DateRushArt.Gold,true,TextAnchor.MiddleCenter);
            Text("Target name",p,45,230,590,43,target.name,30,DateRushArt.Cream,true,TextAnchor.MiddleCenter);
            Text("Target fact",p,68,276,544,75,target.description,19,DateRushArt.Cream,false,TextAnchor.MiddleCenter);
            Text("Pause controls",p,50,353,580,28,"MOVE A / D or ← →   •   JUMP Space   •   DASH Shift",16,DateRushArt.Gold,false,TextAnchor.MiddleCenter);
            Button("Resume",p,94,391,492,54,"RESUME GAME  >",game.Resume,true,22);
            Button("About Dates",p,94,458,492,42,"ABOUT ALL DATES",ShowAboutDates);
            Button("Restart",p,94,511,235,42,"RESTART",game.BeginRound);
            Button("Main menu",p,351,511,235,42,"MAIN MENU",game.Menu);
            var pauseMusic=Button("Pause music setting",p,184,565,312,40,game.MusicEnabled?"MUSIC: ON":"MUSIC: OFF",game.ToggleMusic,fontSize:17);
            musicLabel=pauseMusic.GetComponentInChildren<Text>();
        }
        public void ShowTargetBriefing()
        {
            var p=ModalPanel("Starting date briefing",390,111,820,678);
            var target=DateCatalog.All[game.Target];
            Text("Kicker",p,45,25,730,29,"YOUR FIRST HARVEST",17,DateRushArt.Gold,true,TextAnchor.MiddleCenter);
            Text("Title",p,45,59,730,58,"Know your target",38,DateRushArt.Cream,true,TextAnchor.MiddleCenter,true);
            Image("Starting date",p,325,132,170,117,Color.white,game.GetDateSprite(game.Target)).preserveAspect=true;
            Text("Date name",p,70,253,680,45,target.name,31,DateRushArt.Gold,true,TextAnchor.MiddleCenter);
            Text("Date information",p,100,303,620,75,target.description,21,DateRushArt.Cream,false,TextAnchor.MiddleCenter);
            Text("Goal",p,70,390,680,33,"Catch "+game.harvestGoal+" matching dates before the "+game.roundSeconds+"-second timer ends.",19,DateRushArt.Cream,false,TextAnchor.MiddleCenter);
            Button("Begin catching",p,145,465,530,62,"START CATCHING  >",game.StartPlaying,true,23);
            Text("Wave changes",p,70,544,680,35,"Your target changes every 3 correct catches. Pause to see its details.",17,DateRushArt.Cream,false,TextAnchor.MiddleCenter);
        }
        void ShowAboutDates()
        {
            if(!aboutDatesFromMenu)aboutDatesFromMenu=game.State==RoundState.Menu;
            var p=ModalPanel("About dates",220,110,1160,680);
            showingAboutDates=true;
            Text("Title",p,45,23,1070,58,"About dates",43,DateRushArt.Cream,true,TextAnchor.MiddleCenter,true);
            Text("Subtitle",p,45,86,1070,32,"Meet the six dates you can catch.",22,DateRushArt.Gold,false,TextAnchor.MiddleCenter);
            for(int i=0;i<DateCatalog.All.Length;i++)
            {
                var variety=DateCatalog.All[i];
                float x=35+(i%2)*556,y=133+(i/2)*143;
                var card=Image(variety.name+" fact card",p,x,y,534,125,new Color(.34f,.19f,.105f)).rectTransform;
                Image("Date portrait",card,17,25,111,76,Color.white,game.GetDateSprite(i)).preserveAspect=true;
                Text("Variety name",card,144,13,366,32,variety.name,24,DateRushArt.Gold,true);
                Text("Description",card,144,48,366,63,variety.description,20,DateRushArt.Cream,false,TextAnchor.UpperLeft);
            }
            if(aboutDatesFromMenu)
                Button("Back to menu",p,365,584,430,58,"BACK TO MENU",ShowMenu,true,22);
            else
                Button("Back to pause",p,365,584,430,58,"BACK TO PAUSE",()=>ShowPause(wavePause),true,22);
        }
        internal bool TryCloseAboutDates()
        {
            if(!showingAboutDates)return false;
            ShowPause();
            return true;
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
                Image("Date",card.transform,27,19,105,73,Color.white,game.GetDateSprite(i)).preserveAspect=true;
                Text("Name",card.transform,4,97,151,33,DateCatalog.All[i].name,22,DateRushArt.Cream,true,TextAnchor.MiddleCenter);
                Text("Discovered",card.transform,4,135,151,29,game.Discoveries[i]>0?game.Discoveries[i]+" caught":"Ready to discover",14,DateRushArt.Gold,false,TextAnchor.MiddleCenter);
            }
            Text("Selected name",p,110,387,922,47,DateCatalog.All[selected].name.ToUpper(),29,DateRushArt.Gold,true,TextAnchor.MiddleCenter);
            Text("Selected fact",p,126,447,890,82,DateCatalog.All[selected].description,25,DateRushArt.Cream,false,TextAnchor.MiddleCenter);
            Button("Back",p,341,576,460,62,"BACK TO THE MARKET",game.Menu,true,22);
        }
        public void RefreshSound()
        {
            if(soundLabel)soundLabel.text=game.Muted?"SOUND EFFECTS: OFF":"SOUND EFFECTS: ON";
        }
        public void RefreshMusic()
        {
            if(musicLabel)musicLabel.text=game.MusicEnabled?"MUSIC: ON":"MUSIC: OFF";
        }
        public void Notice(string text,Color color)
        {
            notice.text=text;notice.color=Color.white;noticeLife=2.7f;notice.enabled=true;
            float width=Mathf.Clamp(text.Length*(notice.fontSize/(float)TextSampleScale)*.55f+34,150,420);
            float left=(1600-width)*.5f;
            noticeFrame.rectTransform.anchoredPosition=new Vector2(left,-493);
            noticeFrame.rectTransform.sizeDelta=new Vector2(width,38);
            notice.rectTransform.anchoredPosition=new Vector2(left+8,-494);
            notice.rectTransform.sizeDelta=new Vector2((width-16)*TextSampleScale,36*TextSampleScale);
            noticeFrame.enabled=true;
        }
        public void FloatText(Vector2 at,string content,Color color)
        {
            at.x=Mathf.Clamp(at.x,170,1430);
            var t=Text("Catch reward",effects,at.x-200,at.y,400,48,content,25,color,true,TextAnchor.MiddleCenter);
            var outline=t.gameObject.AddComponent<Outline>();outline.effectColor=DateRushArt.Ink;outline.effectDistance=new Vector2(2,-2)*TextSampleScale;
            floating.Add(new Floating{text=t,origin=t.rectTransform.anchoredPosition});
        }
        void LateUpdate()
        {
            if(!canvas || Mathf.Approximately(textCanvasScale,canvas.scaleFactor))return;
            textCanvasScale=canvas.scaleFactor;
            // A viewport resize can change the glyph size without changing label rectangles.
            foreach(var label in GetComponentsInChildren<Text>())label.SetVerticesDirty();
        }
        void Update()
        {
            if(wavePause && game.State==RoundState.Paused)
            {
                waveCountdown-=Time.unscaledDeltaTime;
                int shown=Mathf.Max(1,Mathf.CeilToInt(waveCountdown));
                if(waveCountdownLabel)waveCountdownLabel.text=shown.ToString();
                if(waveCountdown<=0)game.Resume();
                return;
            }
            if(game.State!=RoundState.Playing)return;
            float dt=Time.deltaTime;noticeLife-=dt;notice.enabled=noticeLife>0;
            noticeFrame.enabled=noticeLife>0;
            for(int i=floating.Count-1;i>=0;i--)
            {
                var f=floating[i];f.life-=dt;
                if(f.life<=0){Destroy(f.text.gameObject);floating.RemoveAt(i);continue;}
                f.text.rectTransform.anchoredPosition=f.origin+new Vector2(0,(1.2f-f.life)*45);var c=f.text.color;c.a=Mathf.Min(1,f.life*2);f.text.color=c;
            }
        }
    }
}

