using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SharedGameJuice.Editor
{
    [InitializeOnLoad]
    public static class JuiceBuilder
    {
        const string Root="Assets/SharedGameJuice/", R=Root+"Resources/SharedGameJuice/";
        static string ProjectPath(string path)=>Path.Combine(Path.GetDirectoryName(Application.dataPath),path);
        static JuiceBuilder(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(ProjectPath("Logs/shared-juice.request")))return;
            File.Delete(ProjectPath("Logs/shared-juice.request"));
            try{Build();File.WriteAllText(ProjectPath("Logs/shared-juice-build.txt"),"PASS: built portable prefabs, validated dependencies, exported SharedGameJuice.unitypackage");}
            catch(Exception e){File.WriteAllText(ProjectPath("Logs/shared-juice-build.txt"),e.ToString());Debug.LogException(e);}
        }
        [MenuItem("Tools/Shared Game Juice/Build and export")]
        public static void Build()
        {
            Directory.CreateDirectory(ProjectPath(R));
            // Original CC0 paper silhouette with an angled edge and a subtle folded face.
            var texture=new Texture2D(32,48,TextureFormat.RGBA32,false);
            for(int y=0;y<48;y++)for(int x=0;x<32;x++)
            {
                bool inside=x>=3&&x<=28&&y>=3&&y<=44&&!(y>40&&x<7-(44-y));
                float shade=x>21?.84f:1;texture.SetPixel(x,y,inside?new Color(shade,shade,shade,1):Color.clear);
            }
            texture.Apply();File.WriteAllBytes(ProjectPath(R+"Paper.png"),texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string name in new[]{"Paper","Glow","Sparkle"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(R+name+".png");
                importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=128;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false;importer.maxTextureSize=128;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>(R+"Sparkle.mat");
            if(!material){material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,R+"Sparkle.mat");}
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(R+"Sparkle.png");EditorUtility.SetDirty(material);
            Burst("SparkleBurst",material, .7f);Burst("SwitchBurst",material,.5f);Burst("BridgeSparkles",material,.55f);
            var go=new GameObject("PaperConfetti");go.AddComponent<PaperConfetti>().paper=AssetDatabase.LoadAssetAtPath<Sprite>(R+"Paper.png");Save(go,"PaperConfetti");
            foreach(bool idle in new[]{false,true})
            {
                go=new GameObject(idle?"GentleGoalGlow":"PressFeedback");var sr=go.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(R+"Sparkle.png");
                var fx=go.AddComponent<SpriteJuice>();fx.source=sr;fx.idleGlow=idle;fx.useProxy=!idle;Save(go,go.name);
            }
            go=new GameObject("BridgePresentation");go.AddComponent<BridgePresentation>();Save(go,"BridgePresentation");
            go=new GameObject("ScoreJuice",typeof(RectTransform));var fill=new GameObject("Fill",typeof(RectTransform),typeof(Image));fill.transform.SetParent(go.transform,false);
            fill.GetComponent<Image>().raycastTarget=false;var score=go.AddComponent<ScoreJuice>();score.fill=fill.GetComponent<RectTransform>();Save(go,"ScoreJuice");
            AssetDatabase.SaveAssets();
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{Root}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach(var component in prefab.GetComponentsInChildren<Transform>(true))
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(component.gameObject)>0)throw new Exception("Missing script: "+path);
                foreach(string dependency in AssetDatabase.GetDependencies(path,true))
                    if(dependency.StartsWith("Assets/")&&!dependency.StartsWith(Root))throw new Exception("Nonportable dependency: "+dependency);
            }
            Directory.CreateDirectory(ProjectPath("Exports"));AssetDatabase.ExportPackage("Assets/SharedGameJuice",ProjectPath("Exports/SharedGameJuice.unitypackage"),ExportPackageOptions.Recurse);
        }
        static void Burst(string name,Material material,float lifetime)
        {
            var go=new GameObject(name);var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=false;main.playOnAwake=false;main.duration=.1f;main.startLifetime=new ParticleSystem.MinMaxCurve(lifetime*.65f,lifetime);
            main.startSpeed=new ParticleSystem.MinMaxCurve(.6f,2);main.startSize=new ParticleSystem.MinMaxCurve(.07f,.15f);main.maxParticles=16;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.useUnscaledTime=true;main.gravityModifier=.18f;main.startRotation=new ParticleSystem.MinMaxCurve(0,6.28f);
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=.15f;
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.y=.5f;
            var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(1,.35f),new GradientAlphaKey(0,1)});color.color=gradient;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,0));
            var spin=ps.rotationOverLifetime;spin.enabled=true;spin.z=new ParticleSystem.MinMaxCurve(-2,2);
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=45;renderer.maxParticleSize=.025f;
            go.AddComponent<JuiceBurst>();Save(go,name);
        }
        static void Save(GameObject go,string name){try{PrefabUtility.SaveAsPrefabAsset(go,R+name+".prefab");}finally{UnityEngine.Object.DestroyImmediate(go);}}
    }
}
