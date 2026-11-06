using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DateRush.Editor
{
    [InitializeOnLoad]
    public static class DateRushEditor
    {
        const string ScenePath="Assets/Scenes/DateRush.unity";
        static double nextPoll;
        static DateRushEditor(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.timeSinceStartup<nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            nextPoll=EditorApplication.timeSinceStartup+.5;
            const string path="Temp/DateRush.command";
            if(!File.Exists(path))return;
            string command=File.ReadAllText(path).Trim();File.Delete(path);
            try
            {
                switch(command)
                {
                    case "setup": CreateScene();break;
                    case "play": EditorApplication.isPlaying=true;break;
                    case "stop": EditorApplication.isPlaying=false;break;
                    case "build": BuildWindows();break;
                    case "verify": DateRushVerification.Begin();break;
                    case "capture": Directory.CreateDirectory("Documentation/Screenshots");ScreenCapture.CaptureScreenshot("Documentation/Screenshots/DateRush.png");break;
                }
                File.WriteAllText("Temp/DateRush.command-result",command+" accepted");
            }
            catch(Exception e){File.WriteAllText("Temp/DateRush.command-result",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("Date Rush/Create or Open Game Scene")]
        public static void CreateScene()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before creating a scene.");
            if(File.Exists(ScenePath)){EditorSceneManager.OpenScene(ScenePath);return;}
            // Preserve the original starter scene. This creates a separate, build-ready game scene.
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";
            var cam=camera.GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=4.5f;cam.transform.position=new Vector3(0,0,-10);cam.backgroundColor=DateRushArt.Ink;
            new GameObject("Date Rush - Game Controller").AddComponent<DateRushGame>();
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.productName="Date Rush";PlayerSettings.companyName="Date Rush";
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=false;
            AssetDatabase.SaveAssets();
            Selection.activeGameObject=GameObject.Find("Date Rush - Game Controller");
            Debug.Log("Date Rush scene created. Press Play to enter the date market.");
        }
        [MenuItem("Date Rush/Build Windows Game")]
        public static void BuildWindows()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before building.");
            CreateScene();Directory.CreateDirectory("Builds/Windows");
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            // Runtime-created sprites use this unlit shader; preserve it from build stripping.
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=settings.FindProperty("m_AlwaysIncludedShaders");var shader=Shader.Find("Sprites/Default");bool found=false;
            for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;
            if(!found){shaders.InsertArrayElementAtIndex(shaders.arraySize);shaders.GetArrayElementAtIndex(shaders.arraySize-1).objectReferenceValue=shader;settings.ApplyModifiedPropertiesWithoutUndo();}
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Windows/Date Rush.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            Directory.CreateDirectory("Documentation");
            File.WriteAllText("Documentation/BuildReport.txt",report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nSize: "+report.summary.totalSize+" bytes\nDuration: "+report.summary.totalTime);
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Date Rush build failed. See the Unity Console.");
            File.Copy("Documentation/Ubuntu-Font-Licence.txt","Builds/Windows/Ubuntu-Font-Licence.txt",true);
            File.Copy("Documentation/Controls.txt","Builds/Windows/README.txt",true);
            Debug.Log("Date Rush built: Builds/Windows/Date Rush.exe");
        }
        [MenuItem("Date Rush/Run Gameplay Verification")]
        public static void Verify()=>DateRushVerification.Begin();
    }
}
