using UnityEditor;
using UnityEngine;
using System.IO;

namespace UniVCC
{
    public class UniVCCPopupMenu : EditorWindow
    {
        [MenuItem("GameObject/Uni-V.CC/Import Prefab", false, 0)]
        public static void CreatePrefabFromAssetPackage()
        {
            UniVCCPopupMenu window = EditorWindow.GetWindow<UniVCCPopupMenu>("Select Prefab");
            window.Show();
        }

        private int selectedPackageIndex = 0;
        private UniVCCAssetPackage[] assetPackages;
        private string[] assetPackageNames;

        private string[] prefabList;
        private int selectedPrefabIndex = 0;

        private void OnEnable()
        {
            assetPackages = UniVCCAssetPackage.GetAllAssetPackages();
            assetPackageNames = new string[assetPackages.Length];
            for (int i = 0; i < assetPackages.Length; i++)
            {
                var package = assetPackages[i];
                assetPackageNames[i] = string.IsNullOrEmpty(package.packageName) || "Unnamed".Equals(package.packageName) ? Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(package)) : package.packageName;
            }
        }

        private void OnGUI()
        {
            if (assetPackages.Length == 0)
            {
                EditorGUILayout.LabelField("No UniVCCAssetPackage found.");
                return;
            }

            // Display the list of prefabs in a dropdown
            EditorGUILayout.LabelField("Select a Package");
            selectedPackageIndex = EditorGUILayout.Popup(selectedPackageIndex, assetPackageNames);

            if (selectedPackageIndex >= 0 && selectedPackageIndex < assetPackages.Length)
            {
                prefabList = assetPackages[selectedPackageIndex].prefabs;

                string[] prefabNames = new string[prefabList.Length];
                for (int i = 0; i < prefabList.Length; i++)
                {
                    prefabNames[i] = Path.GetFileNameWithoutExtension(prefabList[i].Replace('|', '/'));
                }

                EditorGUILayout.LabelField("Select a Variant");
                selectedPrefabIndex = EditorGUILayout.Popup(selectedPrefabIndex, prefabNames);
            }

            // Create the 'Create' and 'Cancel' buttons
            GUILayout.Space(20);
            if (GUILayout.Button("Create"))
            {
                if (selectedPackageIndex >= 0 && selectedPrefabIndex >= 0 && selectedPackageIndex < assetPackages.Length && selectedPrefabIndex < prefabList.Length)
                {
                    CreatePrefab(assetPackages[selectedPackageIndex], prefabList[selectedPrefabIndex]);
                    Close();
                }
            }
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
        }

        private void CreatePrefab(UniVCCAssetPackage data, string prefabName)
        {
            GameObject prefab = UniVCCAssetPackage.ResolvePrefab(prefabName, data);
            if (prefab != null)
            {
                // Instantiate the prefab in the scene
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                Debug.Log(instance);
                if (instance != null)
                {
                    MaterialDuplicator duplicator = new MaterialDuplicator(data.packageName);
                    duplicator.VisitGameObject(instance);
                    
                    Undo.RegisterCreatedObjectUndo(instance, "Create Prefab");
                    Selection.activeGameObject = instance; // Optionally select the new object
                }
            }
            else Debug.LogError("Prefab not found: " + prefabName);
        }
    }
}