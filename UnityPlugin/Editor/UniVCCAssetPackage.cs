using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace UniVCC
{
    [CreateAssetMenu(fileName = "UniVCCAssetPackage", menuName = "VRChat/Uni-VCC/Asset Package")]
    public class UniVCCAssetPackage : ScriptableObject
    {
        public bool isAvatar = false;
        public string packageName = "Unnamed";

        public ImportableAsset[] prefabList = new ImportableAsset[0];
        public string[] prefabs = new string[0];

        public static UniVCCAssetPackage[] GetAllAssetPackages()
        {
            string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(UniVCCAssetPackage)));
            UniVCCAssetPackage[] packages = new UniVCCAssetPackage[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                packages[i] = AssetDatabase.LoadAssetAtPath<UniVCCAssetPackage>(path);
            }

            return packages;
        }

        public static string GetPrefabFile(string prefabPath, UniVCCAssetPackage package)
        {
            return Path.Combine(Path.GetDirectoryName(AssetDatabase.GetAssetPath(package)), prefabPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static GameObject ResolvePrefab(string prefabPath, UniVCCAssetPackage package)
        {
            string fullPath = Path.GetFullPath(GetPrefabFile(prefabPath, package));

            string projectPath = Path.GetFullPath(Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length));
            if (!fullPath.StartsWith(projectPath))
            {
                Debug.LogWarning("Prefab path resolves outside project(" + projectPath + "): " + fullPath);
                return null;
            }

            string assetRelativePath = fullPath.Substring(projectPath.Length).Replace(Path.DirectorySeparatorChar, '/');

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Prefab not found at path: " + fullPath);
                return null;
            }

            Debug.Log("Loading prefab file: " + assetRelativePath);

            return AssetDatabase.LoadAssetAtPath<GameObject>(assetRelativePath);
        }

        public void MigrateOldPrefabs()
        {
            if (prefabs == null || prefabs.Length == 0) return;

            var list = new List<ImportableAsset>();
            foreach (var path in prefabs)
            {
                var obj = ImportableAssetFromLegacy(path, this);
                if (obj != null) list.Add(obj);
            }
            prefabList = list.ToArray();
            prefabs = null;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log("Migrated old prefab paths to ImportableAsset objects");
        }

        public List<ImportableAsset> GetImportableAssets()
        {
            List<ImportableAsset> all = new List<ImportableAsset>();
            if (prefabs != null) // legacy code
                foreach (var path in prefabs)
                {
                    var obj = ImportableAssetFromLegacy(path, this);
                    if (obj != null) all.Add(obj);
                }
            all.AddRange(prefabList);
            return all;
        }

        private static ImportableAsset ImportableAssetFromLegacy(string prefabPath, UniVCCAssetPackage package)
        {
            var obj = ResolvePrefab(prefabPath, package);
            if (obj != null) return new ImportableAsset { prefab = obj, displayName = Path.GetFileNameWithoutExtension(prefabPath) };
            return null;
        }
    }
}