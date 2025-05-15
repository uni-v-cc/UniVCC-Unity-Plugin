using UnityEditor;
using UnityEngine;
using System.IO;

namespace UniVCC
{
    [CreateAssetMenu(fileName = "UniVCCAssetPackage", menuName = "VRChat/Uni-V.CC/Asset Package")]
    public class UniVCCAssetPackage : ScriptableObject
    {
        public bool isAvatar = false;
        public string packageName = "Unnamed";
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
            string[] paths = prefabPath.Split(new[] { '|' });
            return Path.Combine(Path.GetDirectoryName(AssetDatabase.GetAssetPath(package)), paths[0].Replace('/', Path.DirectorySeparatorChar));
        }

        public static GameObject ResolvePrefab(string prefabPath, UniVCCAssetPackage package)
        {
            string[] paths = prefabPath.Split(new[] { '|' });
            string fullPath = Path.Combine(Path.GetDirectoryName(AssetDatabase.GetAssetPath(package)), paths[0].Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Prefab not found at path: " + fullPath);
                return null;
            }

            // FIXME: not working
            if (paths[0].EndsWith(".unity"))
            {
                var sceneElementPath = paths[1];
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(fullPath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
                var gameObject = GameObject.Find(sceneElementPath);
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
                if (gameObject == null)
                {
                    Debug.LogWarning("GameObject not found in scene: " + sceneElementPath);
                    return null;
                }
                return gameObject;
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
        }
    }
}