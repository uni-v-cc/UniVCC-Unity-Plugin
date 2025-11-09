using UnityEngine;
using UnityEditor;
using System.IO;

namespace UniVCC
{
    [CustomEditor(typeof(UniVCCAssetPackage))]
    public class AssetPackageEditor : Editor
    {
        private static readonly string iconGuid = "f7cfbf2a2e31793449bd08abac2b4964";

        public static void ApplyIcon(UniVCCAssetPackage[] data)
        {
            if(data == null || data.Length == 0) return;

            string iconPath = AssetDatabase.GUIDToAssetPath(iconGuid);
            if (string.IsNullOrEmpty(iconPath)) return;

            Texture2D customIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (customIcon == null) return;

            foreach (var item in data)
                EditorGUIUtility.SetIconForObject(item, customIcon);
        }

        public override void OnInspectorGUI()
        {
            UniVCCAssetPackage data = (UniVCCAssetPackage)target;

            bool readOnly = AssetDatabase.GetAssetPath(data).ToLower().StartsWith("packages/");
            if(readOnly)
                EditorGUILayout.HelpBox("This asset package is imported as package, and can not be modified.", MessageType.Info);
            EditorGUI.BeginDisabledGroup(readOnly);

            bool validName = FilesHelper.IsValidName(data.packageName);
            GUI.color = validName ? Color.white : new Color(1f, 0.6f, 0.6f);
            data.packageName = EditorGUILayout.TextField("Name", data.packageName);
            GUI.color = Color.white;
            if (!validName) EditorGUILayout.HelpBox("Package name contains invalid characters for file paths.", MessageType.Error);

            data.isAvatar = EditorGUILayout.Toggle("Is Avatar", data.isAvatar);

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.richText = true;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Importable Prefabs", EditorStyles.boldLabel);

            if (data.prefabs != null && data.prefabs.Length > 0)
            {
                EditorGUILayout.HelpBox("The legacy prefabs detected. Please use the 'Migrate' to upgrade to new prefab list.", MessageType.Warning);

                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"<color=#FFAA88>Legacy Prefabs: {data.prefabs.Length}</color>", style, GUILayout.ExpandWidth(true));
                for (int i = 0; i < data.prefabs.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    var prevPath = i < data.prefabs.Length ? data.prefabs[i] : "";

                    var fullPath = UniVCCAssetPackage.GetPrefabFile(prevPath, data);
                    EditorGUILayout.LabelField("\t" + (string.IsNullOrEmpty(prevPath) ? "" : File.Exists(fullPath) ? "<color=#88FF88>Located</color>" : "<color=#FF8888>Missing</color>"), style, GUILayout.Width(160));

                    EditorGUILayout.LabelField(prevPath);

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;

                if (GUILayout.Button("Migrate", GUILayout.ExpandWidth(true)))
                {
                    data.MigrateOldPrefabs();
                    EditorUtility.SetDirty(data);
                }

                EditorGUILayout.Space();
            }

            SerializedProperty arrayProp = serializedObject.FindProperty("prefabList");
            EditorGUILayout.PropertyField(arrayProp, includeChildren: true);
            for (int i = 0; i < data.prefabList.Length; i++)
            {
                var entry = data.prefabList[i];
                if (entry != null && !FilesHelper.IsValidName(entry.displayName)) 
                    EditorGUILayout.HelpBox($"Prefab #{i + 1} ({entry.displayName}) has an invalid display name.", MessageType.Error);
            }
            serializedObject.ApplyModifiedProperties();

            EditorGUI.EndDisabledGroup();
            if (!readOnly && GUI.changed) EditorUtility.SetDirty(data);
        }

        private string GetRelativePath(string fullPath, UniVCCAssetPackage data)
        {
            return Path.GetRelativePath(Path.GetDirectoryName(AssetDatabase.GetAssetPath(data)), fullPath);
        }
    }
}