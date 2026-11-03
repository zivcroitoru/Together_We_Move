using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class TutorialValidation
{
    static TutorialValidation(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Logs/tutorial-check.request"))return;
        File.Delete("Logs/tutorial-check.request");EditorApplication.update-=Poll;Run();
    }
    [MenuItem("Tools/Playtest/Validate onboarding and render hand")]
    public static void Run()
    {
        Directory.CreateDirectory("Logs/Tutorial");
        var level=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Level_01_Tutorial.unity");
        try
        {
            var tutorial=level.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TutorialManager>(true)).Single();
            var serialized=new SerializedObject(tutorial);var steps=serialized.FindProperty("steps");Require(steps.arraySize==8,"Expected paired left/right/jump, plate, crossing actions");
            Require(serialized.FindProperty("positiveSound").objectReferenceValue is AudioClip,"Positive feedback clip missing");
            Require(serialized.FindProperty("repeatHandAfterSeconds").floatValue==5,"Default inactivity repeat must be five seconds");
            for(int i=0;i<steps.arraySize;i++)
            {
                var step=steps.GetArrayElementAtIndex(i);var player=step.FindPropertyRelative("player").objectReferenceValue as PlayerController;
                Require(player,"Step has no player");
                if(i<6)
                {
                    Require(player.PlayerId==i%2+1,"Each control phase must contain both children");
                    Require(step.FindPropertyRelative("phase").intValue==i/2,"Incorrect left/right/jump phase order");
                    Require(step.FindPropertyRelative("action").enumValueIndex==(i<4?0:1),"Incorrect control action");
                    if(i<4)Require(step.FindPropertyRelative("direction").intValue==(i<2?-1:1),"Incorrect movement direction");
                }
                else Require(step.FindPropertyRelative("phase").intValue==i-3,"Objective phase order");
                var control=step.FindPropertyRelative("control").objectReferenceValue as RectTransform;
                if(control)
                {
                    if(i<4)
                    {
                        var trigger=control.GetComponent<UnityEngine.EventSystems.EventTrigger>();Require(trigger,"Movement control lacks existing input wiring");
                        Require(trigger.triggers.Any(t=>Enumerable.Range(0,t.callback.GetPersistentEventCount()).Any(n=>t.callback.GetPersistentTarget(n)==player)),"Control belongs to wrong child");
                        Require(control.name==(i<2?"Btn_Left":"Btn_Right"),"Wrong directional control");
                    }
                    else
                    {
                        var button=control.GetComponent<Button>();Require(button,"Jump is not the real input button");
                        Require(Enumerable.Range(0,button.onClick.GetPersistentEventCount()).Any(n=>button.onClick.GetPersistentTarget(n)==player&&button.onClick.GetPersistentMethodName(n)=="Jump"),"Jump belongs to wrong child");
                    }
                }
                if(i<6)Require(control,"Control phase has no target");
                if(i>=6)Require(step.FindPropertyRelative("plate").objectReferenceValue is PressurePlate,"Cooperation step has no real plate");
                if(i==7)
                {
                    var bridge=step.FindPropertyRelative("bridge").objectReferenceValue as BridgeController;
                    var plate=step.FindPropertyRelative("plate").objectReferenceValue as PressurePlate;
                    Require(bridge&&plate.Bridge==bridge,"Cooperation step bridge does not match its plate");
                }
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(level);}
        RenderHand();
        File.WriteAllText("Logs/Tutorial/result.txt","PASS: paired left/right/jump phases, six actual player controls, real plate/bridge pairing, feedback clip, five-second repeat and hand mesh/render. Play Mode action progression, audio playback and simultaneous touch still require verification.");
        Debug.Log("[Tutorial] Scene wiring and hand rendering checks passed.");
    }
    static void RenderHand()
    {
        var scene=EditorSceneManager.NewPreviewScene();RenderTexture rt=null;Texture2D image=null;
        try
        {
            var cameraGo=new GameObject("Hand preview camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraGo,scene);var camera=cameraGo.GetComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=3;camera.transform.position=new Vector3(0,0,-20);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.10f,.13f,.18f);
            var root=new GameObject("Hand preview",typeof(Canvas));SceneManager.MoveGameObjectToScene(root,scene);var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;root.GetComponent<RectTransform>().sizeDelta=new Vector2(800,600);root.transform.localScale=Vector3.one*.01f;
            var button=new GameObject("Actual target",typeof(RectTransform),typeof(Image));button.transform.SetParent(root.transform,false);button.GetComponent<RectTransform>().sizeDelta=new Vector2(240,180);button.GetComponent<Image>().color=new Color(.16f,.55f,.86f);
            var arrow=new GameObject("Movement arrow",typeof(RectTransform),typeof(VisualIcon));arrow.transform.SetParent(button.transform,false);arrow.GetComponent<RectTransform>().sizeDelta=new Vector2(100,100);arrow.GetComponent<VisualIcon>().shape=VisualIcon.Shape.Arrow;arrow.GetComponent<VisualIcon>().raycastTarget=false;
            var hand=new GameObject("Demonstrating hand",typeof(RectTransform),typeof(VisualIcon));hand.transform.SetParent(root.transform,false);hand.GetComponent<RectTransform>().sizeDelta=new Vector2(130,150);hand.GetComponent<RectTransform>().anchoredPosition=new Vector2(22,-52);var icon=hand.GetComponent<VisualIcon>();icon.shape=VisualIcon.Shape.Hand;icon.raycastTarget=false;Require(icon.GetComponent<CanvasRenderer>(),"Hand requires a renderer");
            Canvas.ForceUpdateCanvases();rt=new RenderTexture(800,600,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;image=new Texture2D(800,600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,600),0,0);image.Apply();RenderTexture.active=old;File.WriteAllBytes("Logs/Tutorial/hand.png",image.EncodeToPNG());
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);if(image)UnityEngine.Object.DestroyImmediate(image);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
    }
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException("[Tutorial] "+message);}
}
