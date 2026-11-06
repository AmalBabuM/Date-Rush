using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace DateRush
{
    public enum RoundState { Menu, Playing, Paused, Results, Collection }
    public sealed class DateRushGame : MonoBehaviour
    {
        [Header("Round settings")]
        [Min(1)] public int harvestGoal=15;
        [Min(10)] public float roundSeconds=90;
        [Range(.4f,3)] public float dropInterval=1.5f;
        [Range(0,1)] public float targetChance=.7f;
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
        public readonly List<DateRushDrop> Drops=new List<DateRushDrop>();
        AudioSource audioSource;
        AudioClip good,bad,victory;
        Transform world;
        float nextDrop,nextObstacle;
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
            DateRushArt.Sprite("Emirati date market",DateRushArt.Load("Market"),new Vector2(800,450),new Vector2(1600,900),-100,world);
            Conveyors=new GameObject("Conveyor network").AddComponent<DateRushConveyors>();
            Conveyors.transform.SetParent(world);Conveyors.Initialise(this);
            Player=new GameObject("Arabic boy - Player").AddComponent<DateRushPlayer>();
            Player.transform.SetParent(world);Player.Initialise(this);
            UI=new GameObject("Date Rush - UI").AddComponent<DateRushUI>();UI.Initialise(this);
            audioSource=gameObject.AddComponent<AudioSource>();
            good=Tone(670,.14f);bad=Tone(150,.2f);victory=Tone(880,.45f);
            Remaining=roundSeconds;UI.ShowMenu();
        }
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
            {if(State==RoundState.Playing)Pause();else if(State==RoundState.Paused)Resume();}
            if(State!=RoundState.Playing)return;
            float dt=Time.deltaTime;Elapsed+=dt;Remaining=Mathf.Max(0,Remaining-dt);
            nextDrop-=dt;nextObstacle-=dt;
            if(nextDrop<=0)
            {
                Vector2 release=Conveyors.ReleasePoint(Target,targetChance,out int variety,out int trayIndex);
                SpawnDrop(variety,release);Conveyors.Pulse(trayIndex);
                nextDrop=Mathf.Lerp(dropInterval,dropInterval*.72f,Elapsed/roundSeconds);
            }
            if(nextObstacle<=0)
            {SpawnObstacle();nextObstacle=Random.Range(11f,15f);UI.Notice("LOOK OUT! Jump over the rolling date",DateRushArt.Gold);}
            if(Remaining<=0)Finish(false);
            UI.UpdateHud();
        }
        public void BeginRound()
        {
            ClearDrops();Score=Caught=Combo=Missed=0;Elapsed=0;
            Target=roundNumber++%6;Remaining=roundSeconds;nextDrop=1.2f;nextObstacle=12;
            Player.ResetPlayer();Conveyors.ResetTrays();State=RoundState.Playing;UI.ShowHud();
            UI.Notice("CATCH "+DateCatalog.All[Target].name.ToUpper()+" DATES",DateRushArt.Gold);
        }
        public void SpawnDrop(int variety,Vector2 position)
        {
            var drop=new GameObject(DateCatalog.All[variety].name+" - falling date").AddComponent<DateRushDrop>();
            drop.transform.SetParent(world);drop.Initialise(this,variety,position,false);Drops.Add(drop);
        }
        public void SpawnObstacle()
        {
            var drop=new GameObject("Rolling date - jump obstacle").AddComponent<DateRushDrop>();
            drop.transform.SetParent(world);drop.Initialise(this,5,new Vector2(1670,786),true);Drops.Add(drop);
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
            {Target=(Target+1)%6;UI.Notice("NEW TARGET - "+DateCatalog.All[Target].name.ToUpper(),DateRushArt.Gold);}
        }
        public void MissDate(int variety,Vector2 at)
        {if(variety!=Target)return;Missed++;Combo=0;UI.FloatText(at+new Vector2(0,-45),"MISSED",DateRushArt.Cream);}
        public void HitObstacle()
        {Combo=0;Score=Mathf.Max(0,Score-50);Play(bad);UI.FloatText(Player.BasketPosition+new Vector2(0,-70),"-50  JUMP THE ROLLER!",DateRushArt.Coral);}
        public void Pause(){if(State!=RoundState.Playing)return;State=RoundState.Paused;UI.ShowPause();}
        public void Resume(){if(State!=RoundState.Paused)return;State=RoundState.Playing;UI.ShowHud();}
        public void Menu(){State=RoundState.Menu;ClearDrops();UI.ShowMenu();}
        public void Collection(){State=RoundState.Collection;ClearDrops();UI.ShowCollection();}
        public void ToggleSound(){Muted=!Muted;PlayerPrefs.SetInt("DateRush.Muted",Muted?1:0);PlayerPrefs.Save();UI.RefreshSound();}
        public void Finish(bool success)
        {
            Won=success;State=RoundState.Results;
            if(Score>Best){Best=Score;PlayerPrefs.SetInt("DateRush.Best",Best);}
            PlayerPrefs.Save();if(success)Play(victory);UI.ShowResults();
        }
        void ClearDrops(){foreach(var drop in Drops)if(drop!=null)Destroy(drop.gameObject);Drops.Clear();}
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

