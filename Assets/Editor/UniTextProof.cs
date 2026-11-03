using System;
using System.IO;
using System.Linq;
using System.Text;
using LightSide;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class UniTextProof
{
    const string Package="Packages/media.lightside.unitext/";
    public static readonly string[] Cases={"יופי של שיתוף!"};
    static UniTextProof(){EditorApplication.update+=Poll;}
    // A request is consumed before execution. Neither asset refresh nor domain reload
    // creates a new request, so a failure cannot become an Editor update retry loop.
    static bool running;
    static void Poll()
    {
        if (running || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            !File.Exists("Logs/unitext-proof.request")) return;
        File.Delete("Logs/unitext-proof.request");
        EditorApplication.update -= Poll;
        Run(); // Exceptions remain visible in Unity; there is no retry/catch-and-hide.
    }

    static T RequiredAsset<T>(string path) where T : UnityEngine.Object
    {
        var asset=AssetDatabase.LoadAssetAtPath<T>(path);
        if (!asset) throw new InvalidOperationException("UniText proof requires " + path);
        return asset;
    }

    public static void PrepareAssets()
    {
        Directory.CreateDirectory("Assets/Resources/Localization");
        if(!AssetDatabase.LoadAssetAtPath<UniTextSettings>("Assets/Resources/UniTextSettings.asset"))AssetDatabase.CopyAsset(Package+"Defaults/UniTextSettings.asset","Assets/Resources/UniTextSettings.asset");
        if(!AssetDatabase.LoadAssetAtPath<UniTextFont>("Assets/Resources/Localization/GameFont.asset"))
        {var font=UniTextFont.CreateFontAsset(File.ReadAllBytes("Assets/TextMesh Pro/Fonts/LiberationSans.ttf"));font.name="Game Hebrew and Latin";AssetDatabase.CreateAsset(font,"Assets/Resources/Localization/GameFont.asset");}
        if(!AssetDatabase.LoadAssetAtPath<UniTextFontStack>("Assets/Resources/Localization/GameFonts.asset"))
        {var stack=ScriptableObject.CreateInstance<UniTextFontStack>();stack.fonts.Add(AssetDatabase.LoadAssetAtPath<UniTextFont>("Assets/Resources/Localization/GameFont.asset"));stack.fonts.Add(AssetDatabase.LoadAssetAtPath<UniTextFont>(Package+"Defaults/NotoSansHebrew-Regular.asset"));AssetDatabase.CreateAsset(stack,"Assets/Resources/Localization/GameFonts.asset");}
        if(!AssetDatabase.LoadAssetAtPath<UniTextAppearance>("Assets/Resources/Localization/GameAppearance.asset"))AssetDatabase.CopyAsset(Package+"Defaults/UniTextAppearance_Default.asset","Assets/Resources/Localization/GameAppearance.asset");
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Tools/Hebrew/UniText proof and regression")]
    public static void Run()
    {
        if (running) throw new InvalidOperationException("UniText proof is already running.");
        PrepareAssets();Directory.CreateDirectory("Logs/UniText");
        var fonts=RequiredAsset<UniTextFontStack>("Assets/Resources/Localization/GameFonts.asset");
        var appearance=RequiredAsset<UniTextAppearance>("Assets/Resources/Localization/GameAppearance.asset");
        if (!fonts.MainFont || !fonts.MainFont.HasFontData)
            throw new InvalidOperationException("The proof font stack has no embedded main font data.");
        var materials=appearance.GetMaterials(fonts.MainFont);
        if (materials.Length==0 || materials.Any(m=>!m || !m.shader))
            throw new InvalidOperationException("The proof appearance has missing material/shader references.");
        running=true;
        var scene=EditorSceneManager.NewPreviewScene();var report=new StringBuilder();RenderTexture rt=null;Texture2D png=null;
        try
        {
            var camGo=new GameObject("Proof Camera",typeof(Camera));SceneManager.MoveGameObjectToScene(camGo,scene);
            var camera=camGo.GetComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=6.2f;camera.transform.position=new Vector3(0,0,-20);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(1,.98f,.91f);
            var root=new GameObject("UniText Hebrew Proof",typeof(Canvas));SceneManager.MoveGameObjectToScene(root,scene);var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;var cr=root.GetComponent<RectTransform>();cr.sizeDelta=new Vector2(1600,1240);cr.localScale=Vector3.one*.01f;
            for(int i=0;i<Cases.Length;i++)
            {
                var go=new GameObject("Proof "+i,typeof(RectTransform));go.SetActive(false);go.transform.SetParent(root.transform,false);var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(1400,115);rect.anchoredPosition=Vector2.zero;
                var label=go.AddComponent<UniText>();
                if (label.FontProvider != null) throw new InvalidOperationException("Expected a fresh, unprepared proof component.");
                // UniText 1.0.0 Appearance's setter dereferences fontProvider before
                // the first canvas pass. Configure initial serialized references while
                // inactive, exactly as the official sample scenes/prefabs do.
                var serialized=new SerializedObject(label);
                serialized.FindProperty("fontStack").objectReferenceValue=fonts;
                serialized.FindProperty("appearance").objectReferenceValue=appearance;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                label.FontSize=64;label.color=new Color(.08f,.19f,.25f);
                label.HorizontalAlignment=HorizontalAlignment.Center;
                label.VerticalAlignment=VerticalAlignment.Middle;
                label.BaseDirection=TextDirection.Auto;label.raycastTarget=false;
                label.Text=Cases[i];
                go.SetActive(true);

            }
            Canvas.ForceUpdateCanvases();Canvas.ForceUpdateCanvases();
            foreach(var label in root.GetComponentsInChildren<UniText>())
            {
                if (label.FontProvider == null || label.MeshGenerator == null)
                    throw new InvalidOperationException("Canvas preparation did not initialize the proof component.");
                if (label.FontProvider.Appearance != appearance)
                    throw new InvalidOperationException("The initialized provider has the wrong appearance.");
                var glyphs=label.ResultGlyphs.ToArray().OrderBy(g=>g.x).ToArray();if(glyphs.Length==0)throw new Exception("No glyphs: "+label.Text);if(glyphs.Any(g=>g.glyphId==0))throw new Exception("Missing glyph: "+label.Text);
                string visual=new string(glyphs.Select(g=>label.Text[g.cluster]).ToArray());report.AppendLine(label.Text+" | "+visual);
                if (label.Text != Cases[0] || visual != "!ףותיש לש יפוי")
                    throw new InvalidOperationException("Hebrew cluster order or source text changed.");
                report.AppendLine("PASS: provider was null before preparation, initialized after canvas, and uses the configured appearance.");if(label.Text.Contains("75%")&&!visual.Contains("75%"))throw new Exception("Number order changed");if(label.Text.Contains("Unity")&&!visual.Contains("Unity 123"))throw new Exception("Latin run changed");
            }
            rt=new RenderTexture(1600,1240,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;png=new Texture2D(1600,1240,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,1600,1240),0,0);png.Apply();RenderTexture.active=old;File.WriteAllBytes("Logs/UniText/proof.png",png.EncodeToPNG());
            // This is the isolated proof scene; no production label is migrated here.
            Directory.CreateDirectory("Assets/Prefabs");PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/UniText_Hebrew_POC.prefab");
            File.WriteAllText("Logs/unitext-proof-result.txt","PASS: UniText initialization and glyph-order checks; visual review still required.\n"+report);Debug.Log("[UniText] Single-label initialization and glyph-order checks passed. Inspect the image before migration.");
        }
        finally{running=false;EditorSceneManager.ClosePreviewScene(scene);if(png)UnityEngine.Object.DestroyImmediate(png);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}}
    }
}
