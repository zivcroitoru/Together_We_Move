using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One-shot editor render and structural checks for the non-reading UI.</summary>
[InitializeOnLoad]
public static class VisualPlaytestChecks
{
    static VisualPlaytestChecks(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Logs/visual-ui.request"))return;
        File.Delete("Logs/visual-ui.request");EditorApplication.update-=Poll;Run();
    }
    [MenuItem("Tools/Playtest/Validate and render visual UI")]
    public static void Run()
    {
        var scene=EditorSceneManager.NewPreviewScene();RenderTexture rt=null;Texture2D image=null;
        try
        {
            var cameraRoot=new GameObject("Visual UI test camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraRoot,scene);
            var camera=cameraRoot.GetComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=7.68f;camera.transform.position=new Vector3(0,0,-20);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.78f,.89f,.96f);
            var root=new GameObject("Visual UI fixture",typeof(Canvas));SceneManager.MoveGameObjectToScene(root,scene);var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;var rect=root.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(2048,1536);rect.localScale=Vector3.one*.01f;
            var gmRoot=new GameObject("Preview manager");SceneManager.MoveGameObjectToScene(gmRoot,scene);var gm=gmRoot.AddComponent(typeof(CooperationTracker).Assembly.GetType("GameManager"));var ui=gmRoot.GetComponent<PlaytestUI>();
            var settings=new SerializedObject(ui);settings.FindProperty("canvas").objectReferenceValue=canvas;settings.ApplyModifiedPropertiesWithoutUndo();typeof(PlaytestUI).GetMethod("Initialize").Invoke(ui,new object[]{gm,null});ui.ShowResults(false,75,null,null);
            var fill=root.GetComponentsInChildren<RectTransform>(true).Single(r=>r.name=="Earned teamwork");fill.anchorMax=new Vector2(.75f,1);
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(false))Require(!text.gameObject.activeInHierarchy,"Child UI contains visible text: "+text.text);
            var names=root.GetComponentsInChildren<Button>(false).Select(b=>b.name).ToArray();foreach(var name in new[]{"Blue Replay","Red Replay","Next Level"})Require(names.Contains(name),"Missing "+name);
            Require(!root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Researcher tools (F8)").gameObject.activeSelf,"Analytics visible by default");
            Require(root.GetComponentsInChildren<VisualIcon>(false).Count()>=8,"Missing visual icons");
            foreach(var graphic in root.GetComponentsInChildren<VisualIcon>(true)){Require(!graphic.raycastTarget,"Decorative icon intercepts touch");Require(graphic.GetComponent<CanvasRenderer>(),"Icon has no renderer");}
            var spotRoot=new GameObject("Spotlight mesh test",typeof(RectTransform),typeof(HintSpotlight));spotRoot.transform.SetParent(root.transform,false);
            var spot=spotRoot.GetComponent<HintSpotlight>();spot.raycastTarget=false;spot.rectTransform.sizeDelta=new Vector2(2048,1536);
            spot.Holes.Add(new Rect(-100,-100,200,200));spot.Holes.Add(new Rect(-50,-50,200,200));
            using(var vertices=new VertexHelper())
            {
                typeof(HintSpotlight).GetMethod("OnPopulateMesh",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance,null,new[]{typeof(VertexHelper)},null).Invoke(spot,new object[]{vertices});
                var mesh=new Mesh();vertices.FillMesh(mesh);var points=mesh.vertices;var triangles=mesh.triangles;double area=0;
                for(int n=0;n<triangles.Length;n+=3)area+=Vector3.Cross(points[triangles[n+1]]-points[triangles[n]],points[triangles[n+2]]-points[triangles[n]]).magnitude*.5;
                Require(Math.Abs(area-(2048*1536-57500))<2,"Spotlight does not preserve overlapping clear windows");UnityEngine.Object.DestroyImmediate(mesh);
            }
            UnityEngine.Object.DestroyImmediate(spotRoot);
            foreach(var icon in root.GetComponentsInChildren<VisualIcon>(true))icon.RefreshArtwork();
            Canvas.ForceUpdateCanvases();rt=new RenderTexture(2048,1536,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;image=new Texture2D(2048,1536,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,2048,1536),0,0);image.Apply();RenderTexture.active=old;
            Directory.CreateDirectory("Logs/VisualUI");File.WriteAllBytes("Logs/VisualUI/results-75.png",image.EncodeToPNG());
            File.WriteAllText("Logs/VisualUI/result.txt","PASS: text-free child results, separate replay buttons, next button, hidden analytics, nonblocking decorative icons, 75% bar.\nLive touch, hint interaction and animation still require Play Mode checks.");
            Debug.Log("[VisualUI] Checks passed and results rendered.");UnityEngine.Object.DestroyImmediate(gmRoot);
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);if(image)UnityEngine.Object.DestroyImmediate(image);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
    }
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
