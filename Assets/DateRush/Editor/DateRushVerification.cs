using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace DateRush.Editor
{
    [InitializeOnLoad]
    public static class DateRushVerification
    {
        static int phase;
        static double due;
        static float pausedTime;
        static readonly List<string> results=new List<string>();
        static readonly List<string> errors=new List<string>();
        static int savedBest;
        static int[] savedDiscoveries;
        static DateRushVerification(){EditorApplication.update+=Tick;}
        public static void Begin()
        {
            phase=-1;due=EditorApplication.timeSinceStartup+1;results.Clear();errors.Clear();
            if(!EditorApplication.isPlaying)
            {
                DateRushEditor.CreateScene();SessionState.SetBool("DateRush.Verifying",true);EditorApplication.isPlaying=true;return;
            }
            SessionState.SetBool("DateRush.Verifying",true);
        }
        static void Check(bool condition,string label)
        {results.Add((condition?"PASS ":"FAIL ")+label);if(!condition)throw new Exception(label);}
        static void Wait(float seconds){due=EditorApplication.timeSinceStartup+seconds;phase++;}
        static void Capture(string name){Directory.CreateDirectory("Documentation/Screenshots");ScreenCapture.CaptureScreenshot("Documentation/Screenshots/"+name+".png");}
        static void Tick()
        {
            if(!SessionState.GetBool("DateRush.Verifying",false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup<due)return;
            var game=UnityEngine.Object.FindAnyObjectByType<DateRushGame>();if(game==null)return;
            try
            {
                switch(phase)
                {
                    case -1: Wait(1);break;
                    case 0:
                        results.Clear();errors.Clear();Application.logMessageReceived+=Log;
                        savedBest=PlayerPrefs.GetInt("DateRush.Best",0);savedDiscoveries=new int[6];for(int i=0;i<6;i++)savedDiscoveries[i]=PlayerPrefs.GetInt("DateRush.Discovery."+i,0);
                        Check(game.State==RoundState.Menu,"Game opens on the welcome screen");
                        Check(Resources.Load<Texture2D>("Catcher")!=null,"Arabic boy artwork is bundled");
                        for(int i=0;i<6;i++)Check(Resources.Load<Texture2D>("Date"+i)!=null,"Date sprite "+i+" is bundled");
                        Check(CatchRules.Points(true,1)==100 && CatchRules.Points(false,0)==-25 && CatchRules.Points(true,99)==200,"Scoring and combo cap");
                        Check(CatchRules.CrossedMovingBasket(new Vector2(865,620),new Vector2(865,900),new Vector2(865,708),new Vector2(865,708)),"Fast date sweeps through basket without tunnelling");
                        Check(!CatchRules.CrossedMovingBasket(new Vector2(100,620),new Vector2(100,900),new Vector2(865,708),new Vector2(865,708)),"A date outside the basket cannot be caught");
                        Check(CatchRules.CrossedMovingBasket(new Vector2(865,600),new Vector2(865,680),new Vector2(865,708),new Vector2(865,620)),"Jumping basket catches by relative swept motion");
                        Check(!CatchRules.CrossedMovingBasket(new Vector2(865,730),new Vector2(865,760),new Vector2(865,708),new Vector2(865,708)),"A date below the rim cannot be caught retroactively");
                        Capture("01-Welcome");Wait(.5f);break;
                    case 1:
                        game.BeginRound();game.SpawnDrop(game.Target,game.Player.BasketPosition-new Vector2(0,80));Wait(.9f);break;
                    case 2:
                        Check(game.Caught==1 && game.Score==100,"Falling matching date is physically caught and awards 100");
                        game.SpawnDrop((game.Target+1)%6,game.Player.BasketPosition-new Vector2(0,80));Wait(.9f);break;
                    case 3:
                        Check(game.Caught==1 && game.Score==75,"Wrong variety deducts 25 and does not advance harvest");
                        game.Pause();pausedTime=game.Remaining;Wait(.4f);break;
                    case 4:
                        Check(game.State==RoundState.Paused && Mathf.Abs(game.Remaining-pausedTime)<.01f,"Pause freezes round timer");
                        game.Resume();game.Player.Jump();Wait(.2f);break;
                    case 5:
                        Check(game.Player.Height>60,"Jump lifts player above rolling obstacle");Wait(.65f);break;
                    case 6:
                        Check(game.Player.Grounded,"Player lands after a jump");game.Player.Dash();Wait(.1f);break;
                    case 7:
                        Check(game.Player.IsSprinting,"Shift burst activates sprint speed");
                        game.Player.ResetPlayer();Capture("02-Gameplay");Wait(.6f);break;
                    case 8:
                        // Force the rest of the round through the same scoring entry point.
                        int initialTarget=game.Target;
                        while(game.Caught<3){var d=new GameObject("Verification date").AddComponent<DateRushDrop>();d.Initialise(game,game.Target,game.Player.BasketPosition);game.CatchDate(d);UnityEngine.Object.Destroy(d.gameObject);}
                        Check(game.Target!=initialTarget,"Target changes after three matching catches");
                        while(game.State==RoundState.Playing){var d=new GameObject("Verification date").AddComponent<DateRushDrop>();d.Initialise(game,game.Target,game.Player.BasketPosition);game.CatchDate(d);UnityEngine.Object.Destroy(d.gameObject);}
                        Check(game.Won && game.Caught==game.harvestGoal && game.State==RoundState.Results,"Harvest goal completes the round");Capture("03-Results");Wait(.5f);break;
                    case 9:
                        game.Collection();Check(game.State==RoundState.Collection,"Date collection opens");Capture("04-Collection");Wait(.5f);break;
                    case 10:
                        game.BeginRound();Check(game.Caught==0 && game.Score==0 && game.Remaining>89,"Restart resets score, progress and clock");game.Finish(false);Check(!game.Won && game.State==RoundState.Results,"Timeout result is distinct from victory");
                        Check(errors.Count==0,"No Date Rush runtime exceptions");Complete(game);break;
                }
            }
            catch(Exception e){results.Add("ERROR "+e);Complete(game);}
        }
        static void Log(string message,string stack,LogType type)
        {if((type==LogType.Exception || type==LogType.Error) && (stack.Contains("DateRush") || message.Contains("DateRush")))errors.Add(message);}
        static void Complete(DateRushGame game)
        {
            SessionState.SetBool("DateRush.Verifying",false);Application.logMessageReceived-=Log;
            if(savedDiscoveries!=null){PlayerPrefs.SetInt("DateRush.Best",savedBest);for(int i=0;i<6;i++)PlayerPrefs.SetInt("DateRush.Discovery."+i,savedDiscoveries[i]);PlayerPrefs.Save();}
            results.AddRange(errors);Directory.CreateDirectory("Documentation");File.WriteAllLines("Documentation/Verification.txt",results);
            Debug.Log("Date Rush verification finished. See Documentation/Verification.txt");EditorApplication.isPlaying=false;
        }
    }
}
