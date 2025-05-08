using UnityEditor;
using UnityEngine;
using System.IO;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;

namespace UniVCC
{
    public class UniVCCPopupMenu : EditorWindow
    {
        [MenuItem("GameObject/Uni-V.CC Asset", false, 0)]
        public static void CreatePrefabFromAssetPackage()
        {
            EditorWindow.GetWindow<UniVCCPopupMenu>("Select Asset").Show();
        }

        private bool preferAvatar;

        private int selectedPackageIndex = 0;
        private UniVCCAssetPackage[] assetPackages;
        private string[] assetPackageNames;

        private string[] prefabList;
        private int selectedPrefabIndex = 0;

        private void OnEnable()
        {
            preferAvatar = Selection.activeGameObject == null || !HasDescriptorInParents(Selection.activeGameObject.transform);

            List<UniVCCAssetPackage> packages = new List<UniVCCAssetPackage>();
            packages.AddRange(UniVCCAssetPackage.GetAllAssetPackages());
            if (!preferAvatar) // We're currently inspecting an avatar object, let's hide avatars:
                packages.RemoveAll(a => a.isAvatar);
            packages.Sort((a, b) =>
            {
                int prefAvi = preferAvatar ? -1 : 1;
                int aviA = (a.isAvatar ? 1 : -1) * prefAvi;
                int aviB = (b.isAvatar ? 1 : -1) * prefAvi;
                return aviA.CompareTo(aviB);
            });
            assetPackages = packages.ToArray();

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
            EditorGUILayout.LabelField("Select Asset Package");
            selectedPackageIndex = EditorGUILayout.Popup(selectedPackageIndex, assetPackageNames);

            if (selectedPackageIndex >= 0 && selectedPackageIndex < assetPackages.Length)
            {
                prefabList = assetPackages[selectedPackageIndex].prefabs;

                string[] prefabNames = new string[prefabList.Length];
                for (int i = 0; i < prefabList.Length; i++)
                {
                    prefabNames[i] = Path.GetFileNameWithoutExtension(prefabList[i].Replace('|', '/'));
                }

                EditorGUILayout.LabelField("Select Asset Variant");
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
                    
                    if(Selection.activeGameObject != null && !data.isAvatar) instance.transform.SetParent(Selection.activeGameObject.transform, worldPositionStays: true);
                    Selection.activeGameObject = instance;
                }
            }
            else Debug.LogError("Prefab not found: " + prefabName);
        }

        private static bool HasDescriptorInParents(Transform tf)
        {
            while (tf != null)
            {
                if (tf.GetComponent(typeof(VRCAvatarDescriptor)) != null)
                    return true;
                tf = tf.parent;
            }
            return false;
        }
    }
}