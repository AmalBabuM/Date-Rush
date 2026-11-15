using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace DateRush
{
    public enum RoundState { Menu, Briefing, Playing, Paused, Results, Collection }
    public sealed class DateRushGame : MonoBehaviour
    {
        [Header("Round settings")]
        [Min(1)] public int harvestGoal=15;
        [Min(10)] public float roundSeconds=90;
        [Tooltip("Base time between drops. A short random delay is added to each drop.")]
        [Range(.4f,5)] public float dropInterval=1.5f;
        [Tooltip("Seconds before the first date can fall in a new round.")]
        [Range(0,8)] public float firstDropDelay=3f;
        [Tooltip("How strongly incoming dates and releases favor the target variety.")]
        [Range(0,1)] public float targetChance=.7f;

        [Header("Conveyor settings")]
        [Tooltip("Number of dates in one conveyor loop. Set this before entering Play mode.")]
        [Range(1,36)] public int datesPerWave=18;
        [Tooltip("Speed multiplier for dates riding the belts and for the pulley wheels.")]
        [Range(.25f,3f)] public float beltSpeed=1f;
        [Tooltip("Chance to use the upper belt when both the upper and lower belts have a date ready to drop.")]
        [Range(0,1)] public float upperBeltDropChance=.75f;
        [Tooltip("Fraction of each visible belt end where dates cannot drop.")]
        [Range(.05f,.35f)] public float releaseEdgeMargin=.18f;

        [Header("Rolling hay bale settings")]
        [Tooltip("How fast the rolling hay bale moves across the floor in game pixels per second.")]
        [Range(60,900)] public float snakeSpeed=360f;

        [Header("Custom artwork")]
        [Tooltip("Use a 16:9 sprite for the market background.")]
        [SerializeField] Sprite background = null;
        [Tooltip("Use a square sprite with a centered hub for the rotating conveyor wheels.")]
        [SerializeField] Sprite conveyorWheel = null;
        [Tooltip("Optional sprite for the pause button in the top-left corner.")]
        [SerializeField] Sprite pauseButtonSprite = null;
        [Tooltip("Optional. Assign one sprite here to override every date variety below.")]
        [SerializeField] Sprite allDates = null;
        [SerializeField] Sprite khalas = null;
        [SerializeField] Sprite fardh = null;
        [SerializeField] Sprite khenaizi = null;
        [SerializeField] Sprite lulu = null;
        [SerializeField] Sprite sukkary = null;
        [SerializeField] Sprite ajwa = null;

        public RoundState State {get;private set;}=RoundState.Menu;
        public int Target {get;private set;}
        public int Score {get;private set;}
        public int Caught {get;private set;}
        public int Combo {get;private set;}
        public int Missed {get;private set;}
        public int Best {get;private set;}
        public int[] Discoveries {get;private set;}=new int[6];
        public float Remaining {get;private set;}
        public float Elapsed {get;private set;}
        public bool Muted {get;private set;}
        public bool Won {get;private set;}
        public DateRushPlayer Player {get;private set;}
        public DateRushConveyors Conveyors {get;private set;}
        public DateRushUI UI {get;private set;}
        public Camera GameCamera {get;private set;}
        internal Sprite PauseButtonSprite=>pauseButtonSprite;
        internal Sprite SharedBackgroundSprite=>background?background:DateRushArt.Load("Market");
        public readonly List<DateRushDrop> Drops=new List<DateRushDrop>();
        AudioSource audioSource;
        AudioClip good,bad,victory;
        Transform world,backdrop;
        float nextDrop,nextSnake;
        DateRushSnake snake;
        int roundNumber;
        Rect lastCameraRect;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if(FindAnyObjectByType<DateRushGame>()==null)
                new GameObject("Date Rush - Game Controller").AddComponent<DateRushGame>();
        }
        void Awake()
        {
            Application.targetFrameRate=60;
            Best=PlayerPrefs.GetInt("DateRush.Best",0);
            Muted=PlayerPrefs.GetInt("DateRush.Muted",0)==1;
            for(int i=0;i<6;i++)Discoveries[i]=PlayerPrefs.GetInt("DateRush.Discovery."+i,0);
            GameCamera=Camera.main;
            if(GameCamera==null)
            {
                var obj=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));
                obj.tag="MainCamera";GameCamera=obj.GetComponent<Camera>();
            }
            GameCamera.orthographic=true;GameCamera.orthographicSize=4.5f;
            GameCamera.transform.SetPositionAndRotation(new Vector3(0,0,-10),Quaternion.identity);
            GameCamera.clearFlags=CameraClearFlags.SolidColor;GameCamera.backgroundColor=DateRushArt.Ink;
            FitCamera();
            world=new GameObject("Market - 2D World").transform;world.SetParent(transform);
            backdrop=DateRushArt.Sprite("Emirati date market",SharedBackgroundSprite,new Vector2(800,450),new Vector2(1656,931.5f),-100,world).transform;
            Conveyors=new GameObject("Conveyor network").AddComponent<DateRushConveyors>();
            Conveyors.transform.SetParent(world);Conveyors.Initialise(this,conveyorWheel);
            Player=new GameObject("Arabic boy - Player").AddComponent<DateRushPlayer>();
            Player.transform.SetParent(world);Player.Initialise(this);
            UI=new GameObject("Date Rush - UI").AddComponent<DateRushUI>();UI.Initialise(this);
            audioSource=gameObject.AddComponent<AudioSource>();
            good=Tone(670,.14f);bad=Tone(150,.2f);victory=Tone(880,.45f);
            Remaining=roundSeconds;UI.ShowMenu();
        }
        Sprite DateOverride(int variety)
        {
            if(allDates)return allDates;
            return variety switch
            {
                0 => khalas,
                1 => fardh,
                2 => khenaizi,
                3 => lulu,
                4 => sukkary,
                5 => ajwa,
                _ => null
            };
        }
        internal Sprite GetDateSprite(int variety)
        {
            var sprite = DateOverride(variety);
            return sprite ? sprite : DateRushArt.Date(variety);
        }
        internal SpriteRenderer CreateDateSprite(string name, int variety, Vector2 position, Vector2 size, int order, Transform parent)
        {
            return DateRushArt.DateSprite(name,variety,position,size,order,parent,DateOverride(variety));
        }
        internal bool IsTargetDate(int variety)=>variety==Target && (State==RoundState.Briefing || State==RoundState.Playing || State==RoundState.Paused);
        void FitCamera()
        {
            float aspect=Screen.width/(float)Mathf.Max(Screen.height,1);
            Rect rect=aspect>16f/9?new Rect((1-16f/9/aspect)/2,0,16f/9/aspect,1):new Rect(0,(1-aspect/(16f/9))/2,1,aspect/(16f/9));
            if(rect!=lastCameraRect){GameCamera.rect=rect;lastCameraRect=rect;}
        }
        void Update()
        {
            FitCamera();
            if(Keyboard.current!=null && (Keyboard.current.pKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
            {if(State==RoundState.Playing)Pause();else if(State==RoundState.Paused && !UI.TryCloseAboutDates() && !UI.IsWaveTransition)Resume();}
            if(State!=RoundState.Playing)return;
            float dt=Time.deltaTime;Elapsed+=dt;Remaining=Mathf.Max(0,Remaining-dt);
            nextDrop-=dt;nextSnake-=dt;
            if(nextDrop<=0 && Conveyors.TryReleaseDate(Target,targetChance,out int variety,out Vector2 release))
            {
                SpawnDrop(variety,release);
                nextDrop=dropInterval+Random.Range(.2f,.6f);
            }
            if(nextSnake<=0 && !snake)
            {SpawnSnake();nextSnake=Random.Range(11f,15f);}
            if(Remaining<=0)Finish(false);
        }
        void LateUpdate()
        {
            if(State!=RoundState.Playing && State!=RoundState.Menu)return;
            Vector3 offset=Vector3.zero;
            if(State==RoundState.Playing)
            {
                float playerX=Mathf.Clamp(Player.transform.position.x/6.9f,-1,1);
                offset=new Vector3(-playerX*.21f,-Mathf.Clamp01(Player.Height/150f)*.055f,0);
            }
            backdrop.localPosition=Vector3.Lerp(backdrop.localPosition,offset,1-Mathf.Exp(-7*Time.deltaTime));
        }
        public void BeginRound()
        {
            ClearRoundObjects();Score=Caught=Combo=Missed=0;Elapsed=0;
            Target=roundNumber++%6;Remaining=roundSeconds;nextDrop=firstDropDelay;nextSnake=12;
            Player.ResetPlayer();Player.ShowTargetDates(Target);Conveyors.ResetDates();
            State=RoundState.Briefing;UI.ShowTargetBriefing();
        }
        public void StartPlaying()
        {
            if(State!=RoundState.Briefing)return;
            State=RoundState.Playing;UI.ShowHud();
        }
        public void SpawnDrop(int variety,Vector2 position)
        {
            var drop=new GameObject(DateCatalog.All[variety].name+" - falling date").AddComponent<DateRushDrop>();
            drop.transform.SetParent(world);drop.Initialise(this,variety,position);Drops.Add(drop);
        }
        void SpawnSnake()
        {
            snake=new GameObject("Big rolling hay bale - jump obstacle").AddComponent<DateRushSnake>();
            snake.transform.SetParent(world,false);snake.Initialise(this);
            UI.Notice("BIG HAY BALE AHEAD! Jump over it.",DateRushArt.Gold);
        }
        public void CatchDate(DateRushDrop drop)
        {
            bool match=drop.Variety==Target;Combo=match?Combo+1:0;
            int points=CatchRules.Points(match,Combo);Score=Mathf.Max(0,Score+points);
            UI.FloatText(Player.BasketPosition+new Vector2(0,-65),match?"+"+points+(Combo>1?"  x"+Combo:"  PERFECT!"):"-25  WRONG VARIETY",match?DateRushArt.Mint:DateRushArt.Coral);
            Player.CatchFeedback(match);Play(match?good:bad);
            if(!match){UI.Notice("Look for "+DateCatalog.All[Target].name+" dates",DateRushArt.Coral);return;}
            Caught++;Discoveries[Target]++;PlayerPrefs.SetInt("DateRush.Discovery."+Target,Discoveries[Target]);
            if(Caught>=harvestGoal){Finish(true);return;}
            if(Caught%3==0)
            {
                Target=(Target+1)%6;Player.ShowTargetDates(Target);
                State=RoundState.Paused;
                UI.ShowWavePause();
            }
        }
        public void MissDate(int variety,Vector2 at)
        {if(variety!=Target)return;Missed++;Combo=0;UI.FloatText(at+new Vector2(0,-45),"MISSED",DateRushArt.Cream);}
        internal void HitSnake()
        {Combo=0;Score=Mathf.Max(0,Score-50);Play(bad);UI.FloatText(Player.BasketPosition+new Vector2(0,-70),"-50  HAY BALE HIT!",DateRushArt.Coral);}
        public void Pause(){if(State!=RoundState.Playing)return;State=RoundState.Paused;UI.ShowPause();}
        public void Resume(){if(State!=RoundState.Paused)return;State=RoundState.Playing;UI.ShowHud();}
        public void Menu(){State=RoundState.Menu;ClearRoundObjects();UI.ShowMenu();}
        public void Collection(){State=RoundState.Collection;ClearRoundObjects();UI.ShowCollection();}
        public void ToggleSound(){Muted=!Muted;PlayerPrefs.SetInt("DateRush.Muted",Muted?1:0);PlayerPrefs.Save();UI.RefreshSound();}
        public void Finish(bool success)
        {
            Won=success;State=RoundState.Results;
            if(Score>Best){Best=Score;PlayerPrefs.SetInt("DateRush.Best",Best);}
            PlayerPrefs.Save();if(success)Play(victory);UI.ShowResults();
        }
        void ClearRoundObjects()
        {
            foreach(var drop in Drops)if(drop!=null)Destroy(drop.gameObject);
            Drops.Clear();
            if(snake)Destroy(snake.gameObject);
            snake=null;
        }
        void Play(AudioClip clip){if(!Muted)audioSource.PlayOneShot(clip,.4f);}
        void OnApplicationFocus(bool focus){if(!focus)Pause();}
        void OnDestroy(){Destroy(good);Destroy(bad);Destroy(victory);DateRushArt.Release();}
        static AudioClip Tone(float frequency,float length)
        {
            int n=(int)(44100*length);var samples=new float[n];
            for(int i=0;i<n;i++){float t=i/44100f;samples[i]=Mathf.Sin(2*Mathf.PI*(frequency*t+frequency*.35f*t*t))*Mathf.Sin(Mathf.PI*i/n)*.24f;}
            var clip=AudioClip.Create("Date Rush chime",n,1,44100,false);clip.SetData(samples,0);return clip;
        }
    }
}

