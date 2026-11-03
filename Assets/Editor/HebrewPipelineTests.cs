using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using RTLTMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class HebrewPipelineTests
{
    public static readonly string[] Cases = {"יופי של שיתוף!", "כל הכבוד!", "ננסה שוב ביחד?", "ביחד", "שוב!", "קדימה!", "הצלחה של 75%"};
    static HebrewPipelineTests() { EditorApplication.update += Poll; if(File.Exists("Logs/rtl-autotest.enabled")) File.WriteAllText("Logs/rtl-test.request", ""); }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists("Logs/rtl-test.request")) return;
        File.Delete("Logs/rtl-test.request");
        try { Run(); } catch(Exception e) { File.WriteAllText("Logs/rtl-test-result.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Tools/Hebrew/Run pipeline regression and render")]
    public static void Run()
    {
        Directory.CreateDirectory("Logs/Hebrew");
        var report = new StringBuilder();
        var scene = EditorSceneManager.NewPreviewScene();
        TMP_FontAsset font = null;
        RenderTexture rt = null;
        Texture2D png = null;
        try
        {
            font = TMP_FontAsset.CreateFontAsset(Resources.Load<Font>("Localization/Hebrew"));
            string coverage = string.Concat(Enumerable.Range(0x20,95).Concat(Enumerable.Range(0x590,112)).Select(c => (char)c));
            font.TryAddCharacters(coverage, out string missing);
            foreach(char c in string.Concat(Cases)) if (!char.IsWhiteSpace(c) && !font.HasCharacter(c)) throw new Exception("Missing glyph U+"+((int)c).ToString("X4"));
            var camGo = new GameObject("RTL Test Camera",typeof(Camera)); SceneManager.MoveGameObjectToScene(camGo,scene);
            var camera = camGo.GetComponent<Camera>(); camera.scene=scene; camera.orthographic=true; camera.orthographicSize=5.5f;
            camera.transform.position=new Vector3(0,0,-20); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(1,.98f,.91f);
            var root = new GameObject("Isolated RTL fixture",typeof(Canvas)); SceneManager.MoveGameObjectToScene(root,scene);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var cr=root.GetComponent<RectTransform>();cr.sizeDelta=new Vector2(1800,1100);cr.localScale=Vector3.one*.01f;
            for(int col=0;col<3;col++) for(int row=0;row<Cases.Length;row++)
            {
                var go=new GameObject("case-"+col+"-"+row,typeof(RectTransform)); go.transform.SetParent(root.transform,false);
                var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(540,100);rect.anchoredPosition=new Vector2((col-1)*585,430-row*140);
                TMP_Text label;
                if(col==0) {var native=go.AddComponent<RTLTextMeshPro>();native.Farsi=false;native.PreserveNumbers=true;label=native;}
                else label=go.AddComponent<TextMeshProUGUI>();
                report.AppendLine("features="+string.Join(",",label.fontFeatures));
                if(col==0) label.fontFeatures.RemoveAll(feature => feature.ToString() == "kern");
                if(col==2) label.fontFeatures.Clear();
                label.font=font;label.fontSize=42;label.color=new Color(.08f,.19f,.25f);label.alignment=TextAlignmentOptions.Center;
                if(col==1) HebrewTextSupport.Ensure(label);
                if(col==2) {var buffer=new FastStringBuilder(256);RTLSupport.FixRTL(Cases[row],buffer,false,true,true);label.text=buffer.ToString();label.isRightToLeftText=false;}
                else label.text=Cases[row];
                if(label is RTLTextMeshPro nativeLabel) nativeLabel.UpdateText();
                label.ForceMeshUpdate();
                var chars=label.textInfo.characterInfo.Take(label.textInfo.characterCount).Where(c=>c.isVisible).OrderBy(c=>c.bottomLeft.x).ToArray();
                string visual=new string(chars.Select(c=>c.character).ToArray());
                report.AppendLine(col+" | "+Cases[row]+" | "+visual);
                if(row==3) foreach(var c in label.textInfo.characterInfo.Take(label.textInfo.characterCount)) report.AppendLine($"U+{(int)c.character:X4} x={c.origin} advance={c.xAdvance} bounds={c.bottomLeft.x},{c.topRight.x} glyphAdvance={c.textElement.glyph.metrics.horizontalAdvance} rect={c.textElement.glyph.glyphRect}");
                foreach(var c in chars) if(c.character=='\u25a1') throw new Exception("Missing glyph box");
            }
            Canvas.ForceUpdateCanvases();
            rt=new RenderTexture(1800,1100,24);camera.targetTexture=rt;camera.Render();
            var old=RenderTexture.active;RenderTexture.active=rt;png=new Texture2D(1800,1100,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,1800,1100),0,0);png.Apply();RenderTexture.active=old;
            File.WriteAllBytes("Logs/Hebrew/pipeline-comparison.png",png.EncodeToPNG());
            File.WriteAllText("Logs/rtl-test-result.txt",report.ToString());
            Debug.Log("[Hebrew] Pipeline comparison rendered: native RTLTMPro / old adapter / visual-order LTR.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene);if(png)UnityEngine.Object.DestroyImmediate(png);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);} }
    }
}
