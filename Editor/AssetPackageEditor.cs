using UnityEngine;
using UnityEditor;
using System.IO;

namespace UniVCC
{
    [CustomEditor(typeof(UniVCCAssetPackage))]
    public class AssetPackageEditor : Editor
    {
        private static readonly string iconGuid = "f7cfbf2a2e31793449bd08abac2b4964";

        public override void OnInspectorGUI()
        {
            UniVCCAssetPackage data = (UniVCCAssetPackage)target;

            string iconPath = AssetDatabase.GUIDToAssetPath(iconGuid);
            if (!string.IsNullOrEmpty(iconPath))
            {
                Texture2D customIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);

                if (customIcon != null)
                    EditorGUIUtility.SetIconForObject(data, customIcon);
                else
                    Debug.LogWarning("Icon not found at path: " + iconPath);
            }

            data.packageName = EditorGUILayout.TextField("Name", data.packageName);
            data.isAvatar = EditorGUILayout.Toggle("Is Avatar", data.isAvatar);

            string[] oldPrefabs = data.prefabs;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.richText = true;

            data.prefabs = new string[EditorGUILayout.IntField("Prefab Count", oldPrefabs.Length)];
            for (int i = 0; i < data.prefabs.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var prevPath = i < oldPrefabs.Length ? oldPrefabs[i] : "";

                var fullPath = UniVCCAssetPackage.GetPrefabFile(prevPath, data);
                EditorGUILayout.LabelField((i == 0 ? "Prefabs:\t" : "\t") + "\t" + (string.IsNullOrEmpty(prevPath) ? "" : File.Exists(fullPath) ? "<color=#88FF88>Located</color>" : "<color=#FF8888>Missing</color>"), style, GUILayout.Width(160));

                data.prefabs[i] = EditorGUILayout.TextField(prevPath);
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    // Open file selection dialog and filter for prefabs
                    string filePath = EditorUtility.OpenFilePanel("Select Prefab", Path.GetDirectoryName(AssetDatabase.GetAssetPath(data)), "prefab,unity");
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        // Get the relative path minus AssetPackage's path
                        data.prefabs[i] = GetRelativePath(filePath, data).Replace(Path.DirectorySeparatorChar, '/');
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            // if changed, mark the object as dirty
            if (GUI.changed)
            {
                EditorUtility.SetDirty(data);
            }
        }

        private string GetRelativePath(string fullPath, UniVCCAssetPackage data)
        {
            return Path.GetRelativePath(Path.GetDirectoryName(AssetDatabase.GetAssetPath(data)), fullPath);
        }
    }
}