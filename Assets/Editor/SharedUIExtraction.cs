using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
[InitializeOnLoad]
public static class SharedUIExtraction
{
    static SharedUIExtraction(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Logs/shared-ui.request"))return;
        File.Delete("Logs/shared-ui.request");
        try{Run();File.WriteAllText("Logs/SharedUI/extraction.txt","PASS: extraction, export and available regression checks completed.");}
        catch(Exception error){File.WriteAllText("Logs/SharedUI/extraction.txt",error.ToString());Debug.LogException(error);}
    }
    const string Target="Assets/SharedUI/Prefabs/Resources/SharedUI/";
    public static void Run()
    {
        SharedUIBuilder.Build();
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var root=new GameObject("Extraction Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));SceneManager.MoveGameObjectToScene(root,preview);
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var owner=new GameObject("Extraction owner");SceneManager.MoveGameObjectToScene(owner,preview);var manager=owner.AddComponent(typeof(CooperationTracker).Assembly.GetType("GameManager"));var ui=owner.GetComponent<PlaytestUI>();
            var data=new SerializedObject(ui);data.FindProperty("canvas").objectReferenceValue=root.GetComponent<Canvas>();data.ApplyModifiedPropertiesWithoutUndo();typeof(PlaytestUI).GetMethod("Initialize").Invoke(ui,new object[]{manager,null});
            foreach(var pair in new[]{new[]{"Visual HUD","HintHUD"},new[]{"Visual Results","ResultsScreen"},new[]{"Researcher tools (F8)","Dashboard"}})
            {
                var source=root.transform.Find(pair[0]);var clone=UnityEngine.Object.Instantiate(source.gameObject);SceneManager.MoveGameObjectToScene(clone,preview);clone.name=pair[1];clone.SetActive(true);
                if(pair[1]=="ResultsScreen"){clone.GetComponent<CanvasGroup>().alpha=1;clone.GetComponent<CanvasGroup>().interactable=true;clone.GetComponent<CanvasGroup>().blocksRaycasts=true;clone.transform.Find("Celebration").gameObject.SetActive(true);}
                foreach(var avatar in clone.GetComponentsInChildren<Image>(true).Where(i=>i.name=="Character"))avatar.sprite=null;
                foreach(var icon in clone.GetComponentsInChildren<VisualIcon>(true))icon.RefreshArtwork();
                PrefabUtility.SaveAsPrefabAsset(clone,Target+pair[1]+".prefab");UnityEngine.Object.DestroyImmediate(clone);
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Level_01_Tutorial.unity");
        try
        {
            foreach(var pad in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RectTransform>(true)).Where(r=>r.name=="P1_Controls"||r.name=="P2_Controls"))
            {
                var clone=UnityEngine.Object.Instantiate(pad.gameObject);SceneManager.MoveGameObjectToScene(clone,scene);
                foreach(var trigger in clone.GetComponentsInChildren<EventTrigger>(true))foreach(var entry in trigger.triggers)for(int i=entry.callback.GetPersistentEventCount()-1;i>=0;i--)UnityEditor.Events.UnityEventTools.RemovePersistentListener(entry.callback,i);
                foreach(var button in clone.GetComponentsInChildren<Button>(true))for(int i=button.onClick.GetPersistentEventCount()-1;i>=0;i--)UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick,i);
                PrefabUtility.SaveAsPrefabAsset(clone,Target+(pad.name=="P1_Controls"?"BlueTouchControls":"RedTouchControls")+".prefab");UnityEngine.Object.DestroyImmediate(clone);
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        AssetDatabase.SaveAssets();SharedUIBuilder.Validate();SharedUIBuilder.Export();VisualPlaytestChecks.Run();if(SceneManager.sceneCount==0||!SceneManager.GetActiveScene().IsValid())EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        if(!SceneManager.GetActiveScene().isDirty)PlaytestValidation.Run();else Debug.Log("[SharedUI] Scene-switching regression deferred to preserve unsaved scene edits.");
        TutorialValidation.Run();
    }
}
