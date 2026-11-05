using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace SharedGameFeel.Editor
{
    public static class SharedGameFeelBuilder
    {
        public static readonly string[] Roots = { "Assets/SharedGameAudio", "Assets/SharedGameJuice", "Assets/SharedGameFeel" };
        [Serializable] class Records { public Record[] items; }
        [Serializable] class Record { public string file, license, sha256; }
        static bool Portable(string path) => Roots.Any(root => path == root || path.StartsWith(root + "/", StringComparison.Ordinal));

        [MenuItem("Tools/Shared Game Feel/Validate and export combined package")]
        public static void Export()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var records=JsonUtility.FromJson<Records>("{\"items\":"+File.ReadAllText("Assets/SharedGameAudio/Licenses/AudioManifest.json")+"}").items;
            foreach (var record in records)
            {
                if (record.license!="CC0-1.0" || !Portable(record.file)) throw new Exception("Unapproved audio: "+record.file);
                using (var hash=SHA256.Create())
                {
                    string actual=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(record.file))).Replace("-", "").ToLowerInvariant();
                    if (actual!=record.sha256) throw new Exception("Audio differs from licensed manifest: "+record.file);
                }
            }
            var paths=AssetDatabase.GetAllAssetPaths().Where(Portable).ToArray();
            foreach (var path in paths)
            {
                if ((path.EndsWith(".wav") || path.EndsWith(".ogg") || path.EndsWith(".mp3")) && !records.Any(r=>r.file==path))
                    throw new Exception("Audio absent from license manifest: "+path);
                foreach (var dependency in AssetDatabase.GetDependencies(path, true))
                    if (dependency.StartsWith("Assets/") && !Portable(dependency)) throw new Exception("External dependency: "+path+" -> "+dependency);
                if (path.EndsWith(".prefab"))
                {
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    foreach(var transform in prefab.GetComponentsInChildren<Transform>(true))
                        if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)>0) throw new Exception("Missing script in "+path);
                }
            }
            Directory.CreateDirectory("Exports");
            AssetDatabase.ExportPackage(Roots,"Exports/SharedGameFeel.unitypackage",ExportPackageOptions.Recurse);
            Directory.CreateDirectory("Logs/SharedGameFeel");
            File.WriteAllText("Logs/SharedGameFeel/export.txt",$"PASS: {paths.Length} assets/folders; {records.Length} CC0 audio hashes; dependency closure and prefab scripts verified.\nUnity {Application.unityVersion}\n");
            Debug.Log("SharedGameFeel export and dependency validation PASS");
        }
    }
}
