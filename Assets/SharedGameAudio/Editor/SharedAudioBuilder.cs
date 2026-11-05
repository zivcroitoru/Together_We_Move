using System;
using System.IO;
using System.Linq;
using SharedGameAudio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using AudioManager=SharedGameAudio.AudioManager;
namespace SharedGameAudio.Editor
{
    [InitializeOnLoad] public static class SharedAudioBuilder
    {
        const string Root="Assets/SharedGameAudio/",Resource=Root+"Resources/SharedGameAudio/";
        static SharedAudioBuilder(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Logs/shared-audio.request"))return;
            File.Delete("Logs/shared-audio.request");Directory.CreateDirectory("Logs/Audio");
            try{Build();File.WriteAllText("Logs/Audio/build.txt","PASS: imported audio, configured mixer/library/prefabs, validated dependencies, exported package.");}
            catch(Exception e){File.WriteAllText("Logs/Audio/build.txt",e.ToString());Debug.LogException(e);}
        }
        static AudioClip Clip(string name)
        {
            string path=Root+"Audio/"+(name.StartsWith("Level")||name=="Menu"?"Music/"+name+".ogg":"SFX/"+name+".wav");
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(!clip)throw new Exception("Missing clip "+path);return clip;
        }
        static SoundEntry Entry(SoundId id,string files,float volume=.4f,float cooldown=.12f,int max=2,float pitch=.035f,int priority=128)
        {return new SoundEntry{id=id,clips=files.Split(',').Select(Clip).ToArray(),volume=volume,cooldown=cooldown,maxVoices=max,pitchVariation=pitch,priority=priority};}
        [MenuItem("Tools/Shared Game Audio/Build and export")]
        public static void Build()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var guid in AssetDatabase.FindAssets("t:AudioClip",new[]{Root}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);var importer=(AudioImporter)AssetImporter.GetAtPath(path);bool music=path.Contains("/Music/");
                importer.forceToMono=!music;importer.loadInBackground=music;
                var settings=importer.defaultSampleSettings;settings.preloadAudioData=!music;settings.loadType=music?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=music?AudioCompressionFormat.Vorbis:AudioCompressionFormat.ADPCM;settings.quality=.75f;
                settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;settings.sampleRateOverride=32000;importer.defaultSampleSettings=settings;
                importer.SetOverrideSampleSettings("iOS",settings);importer.SaveAndReimport();
            }
            var spriteImporter=(TextureImporter)AssetImporter.GetAtPath(Resource+"Panel.png");spriteImporter.textureType=TextureImporterType.Sprite;spriteImporter.spriteBorder=new Vector4(16,16,16,16);spriteImporter.mipmapEnabled=false;spriteImporter.SaveAndReimport();
            var library=AssetDatabase.LoadAssetAtPath<SoundLibrary>(Resource+"SoundLibrary.asset");
            if(!library){library=ScriptableObject.CreateInstance<SoundLibrary>();AssetDatabase.CreateAsset(library,Resource+"SoundLibrary.asset");}
            library.menuMusic=Clip("Menu");library.levelOneMusic=Clip("Level1");library.levelTwoMusic=Clip("Level2");
            library.sounds=new[]{
                Entry(SoundId.Jump,"phaseJump1,phaseJump2",.22f,.16f),Entry(SoundId.Land,"impactSoft_medium_000",.20f,.18f),
                Entry(SoundId.Move,"footstep_carpet_000,footstep_carpet_001",.13f,.32f),Entry(SoundId.Push,"impactWood_light_000",.19f,.32f),
                Entry(SoundId.PlateOn,"switch_002",.32f,.12f),Entry(SoundId.PlateOff,"toggle_001",.24f,.18f),Entry(SoundId.Switch,"switch_002",.38f,.2f),
                Entry(SoundId.BridgeOn,"maximize_003",.27f,.35f),Entry(SoundId.BridgeOff,"minimize_003",.20f,.35f),
                Entry(SoundId.Cooperate,"jingles_PIZZI07",.32f,2,1,0,48),Entry(SoundId.Goal,"confirmation_001",.35f,.16f,2,.02f,48),
                Entry(SoundId.Tutorial,"pluck_001",.28f,.3f),Entry(SoundId.Hint,"question_001",.22f,2,1,.015f),Entry(SoundId.Retry,"drop_001",.28f,.4f),
                Entry(SoundId.Button,"click_001",.23f,.08f),Entry(SoundId.Score,"tick_001",.14f,.15f),Entry(SoundId.Star,"pluck_001",.26f,.18f),
                Entry(SoundId.Confetti,"maximize_003",.15f,1,1),Entry(SoundId.Victory,"jingles_PIZZI10",.48f,3,1,0,24)};
            EditorUtility.SetDirty(library);
            var mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>(Resource+"SharedAudio.mixer");if(!mixer)throw new Exception("Missing mixer");
            var root=new GameObject("AudioManager");try
            {
                var manager=root.AddComponent<AudioManager>();manager.library=library;manager.mixer=mixer;
                manager.musicGroup=mixer.FindMatchingGroups("Music").FirstOrDefault();manager.sfxGroup=mixer.FindMatchingGroups("SFX").FirstOrDefault();
                if(!manager.musicGroup||!manager.sfxGroup)throw new Exception("Missing mixer groups");
                PrefabUtility.SaveAsPrefabAsset(root,Resource+"AudioManager.prefab");
            }finally{UnityEngine.Object.DestroyImmediate(root);}
            root=AudioSettingsUI.Create();try{PrefabUtility.SaveAsPrefabAsset(root,Resource+"AudioSettings.prefab");}finally{UnityEngine.Object.DestroyImmediate(root);}
            AssetDatabase.SaveAssets();
            foreach(var file in new[]{Resource+"AudioManager.prefab",Resource+"AudioSettings.prefab",Resource+"SoundLibrary.asset"})
                foreach(var dependency in AssetDatabase.GetDependencies(file,true))
                    if(dependency.StartsWith("Assets/")&&!dependency.StartsWith(Root))throw new Exception("Nonportable dependency: "+dependency);
            Directory.CreateDirectory("Exports");AssetDatabase.ExportPackage("Assets/SharedGameAudio","Exports/SharedGameAudio.unitypackage",ExportPackageOptions.Recurse);
        }
    }
}
