using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace UniVCC
{
    public class UniVCCPopupMenu : EditorWindow
    {
        [MenuItem("GameObject/Uni-VCC Asset", false, 0)]
        public static void CreatePrefabFromAssetPackage()
        {
            EditorWindow.GetWindow<UniVCCPopupMenu>("Select Asset").Show();
        }

        private bool preferAvatar;

        private int selectedPackageIndex = 0;
        private UniVCCAssetPackage[] assetPackages;

        private ImportableAsset[] prefabList;
        private int selectedPrefabIndex = 0;

        private bool setupSettings;
        private CopiedMaterials copySettings = new CopiedMaterials();

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
        }

        private void OnGUI()
        {
            if (assetPackages.Length == 0)
            {
                EditorGUILayout.LabelField("No UniVCCAssetPackage found.");
                return;
            }

            // Display the list of prefabs in a dropdown
            EditorGUILayout.LabelField("Select Asset Package", EditorStyles.boldLabel);

            List<string> assetPackageNames = new List<string>();
            Dictionary<int, int> filterIndices = new Dictionary<int, int>();
            Dictionary<int, int> revertIndices = new Dictionary<int, int>();

            bool prefAvatar = Selection.activeGameObject == null || !HasDescriptorInParents(Selection.activeGameObject.transform);
            for (int i = 0, j = 0; i < assetPackages.Length; i++)
            {
                var package = assetPackages[i];

                if(!prefAvatar && package.isAvatar)
                {
                    continue;
                }

                filterIndices[i] = j;
                revertIndices[j] = i;
                assetPackageNames.Add(string.IsNullOrEmpty(package.packageName) || "Unnamed".Equals(package.packageName) ? Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(package)) : package.packageName);
                ++j;
            }

            int prevPackageIndex = selectedPackageIndex;
            selectedPackageIndex = revertIndices[EditorGUILayout.Popup(filterIndices.ContainsKey(selectedPackageIndex) ? filterIndices[selectedPackageIndex] : 0, assetPackageNames.ToArray())];

            if (selectedPackageIndex >= 0 && selectedPackageIndex < assetPackages.Length)
            {
                prefabList = assetPackages[selectedPackageIndex].GetImportableAssets().ToArray();

                string[] prefabNames = new string[prefabList.Length];
                for (int i = 0; i < prefabList.Length; i++)
                {
                    prefabNames[i] = prefabList[i].displayName;
                }

                EditorGUILayout.LabelField("Select Asset Variant", EditorStyles.boldLabel);
                int prevPrefabIndex = selectedPrefabIndex;
                selectedPrefabIndex = EditorGUILayout.Popup(selectedPrefabIndex, prefabNames);

                if (prevPackageIndex != selectedPackageIndex || prevPrefabIndex != selectedPrefabIndex || !setupSettings)
                {
                    UpdateMaterialScanner();
                    setupSettings = true;
                }
            }

            if (copySettings != null && copySettings.scanner != null)
            {
                EditorGUILayout.LabelField("Materials to Copy", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Copy All"))
                {
                    foreach (var item in copySettings.scanner.materials)
                        copySettings.shouldCopy[item.Key] = true;
                }
                if (GUILayout.Button("Default"))
                {
                    foreach (var item in copySettings.scanner.materials)
                        if(copySettings.shouldCopyDefaults.ContainsKey(item.Key))
                            copySettings.shouldCopy[item.Key] = copySettings.shouldCopyDefaults[item.Key];
                }
                if (GUILayout.Button("Copy None"))
                {
                    foreach (var item in copySettings.scanner.materials)
                        copySettings.shouldCopy[item.Key] = false;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                foreach (var renderer in copySettings.keysByRenderer)
                {
                    if(renderer.Value.Count == 0) continue;

                    EditorGUILayout.LabelField(renderer.Key.name);
                    EditorGUI.indentLevel++;

                    foreach(var key in renderer.Value)
                    {
                        string name = key.name;
                        bool value = copySettings.ShouldCopy(key);
                        bool newValue = EditorGUILayout.Toggle(name, value);
                        if (newValue != value)
                        {
                            copySettings.shouldCopy[key] = newValue;
                        }
                    }

                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }

            // Create the 'Create' and 'Cancel' buttons
            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
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
            EditorGUILayout.EndHorizontal();
        }

        private void CreatePrefab(UniVCCAssetPackage data, ImportableAsset prefabName)
        {
            GameObject prefab = prefabName.prefab;
            if (prefab != null)
            {
                // Instantiate the prefab in the scene
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                Debug.Log(instance);
                if (instance != null)
                {
                    MaterialDuplicator duplicator = new MaterialDuplicator(data.packageName + (prefabName.separateMaterialsFolder ? "/" + prefabName.displayName : ""));
                    duplicator.SetupDuplicator(m => copySettings.ShouldCopy(m));
                    duplicator.VisitGameObject(instance);

                    AssetDatabase.Refresh();
                    
                    if(Selection.activeGameObject != null && !data.isAvatar)
                    {
                        instance.transform.SetParent(Selection.activeGameObject.transform, worldPositionStays: true);
                        instance.transform.localPosition = Vector3.zero;
                        instance.transform.localScale = Vector3.one;
                        instance.transform.localRotation = Quaternion.identity;
                    }

                    Selection.activeGameObject = instance;
                    Undo.RegisterCreatedObjectUndo(instance, "Create Prefab");
                }
            }
            else Debug.LogError("Prefab not found: " + prefabName);
        }

        private GatheringMaterialScanner GatherPrefabMaterials(UniVCCAssetPackage data, ImportableAsset prefabName)
        {
            GameObject prefab = prefabName.prefab;
            if (prefab != null)
            {
                MaterialDuplicator duplicator = new MaterialDuplicator(data.packageName);
                var scan = duplicator.SetupScanner();
                duplicator.VisitGameObject(prefab);

                return scan;
            }
            return null;
        }

        private void UpdateMaterialScanner()
        {
            if (selectedPackageIndex >= 0 && selectedPrefabIndex >= 0 &&
                selectedPackageIndex < assetPackages.Length && selectedPrefabIndex < prefabList.Length)
            {
                var scan = GatherPrefabMaterials(assetPackages[selectedPackageIndex], prefabList[selectedPrefabIndex]);
                if (scan != null)
                {
                    copySettings.SetScanner(scan, prefabList[selectedPrefabIndex]);
                }
            }
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

        public class CopiedMaterials
        {
            public GatheringMaterialScanner scanner;
            public Dictionary<MaterialKey, bool> shouldCopy = new Dictionary<MaterialKey, bool>();
            public Dictionary<MaterialKey, bool> shouldCopyDefaults = new Dictionary<MaterialKey, bool>();
            public Dictionary<Renderer, List<MaterialKey>> keysByRenderer = new Dictionary<Renderer, List<MaterialKey>>();

            public void SetScanner(GatheringMaterialScanner scanner, ImportableAsset asset)
            {
                this.shouldCopy.Clear();
                this.keysByRenderer.Clear();
                this.shouldCopyDefaults.Clear();
                this.scanner = scanner;
                
                foreach (var renderer in scanner.renderers)
                {
                    var originalMaterials = renderer.sharedMaterials;
                    List<MaterialKey> keys = new List<MaterialKey>();
                    foreach(var material in originalMaterials)
                    {
                        if(asset.uncopyableMaterials.Contains(material)) continue;
                        var mkey = MaterialKey.GetKeyFromMaterial(material);
                        keys.Add(mkey);
                        if(!shouldCopy.ContainsKey(mkey))
                        {
                            bool cp = !asset.discouragedCopyableMaterials.Contains(material);
                            this.shouldCopy.Add(mkey, cp);
                            this.shouldCopyDefaults.Add(mkey, cp);
                        }
                    }
                    keysByRenderer[renderer] = keys;
                }

            }

            public bool ShouldCopy(Material mat)
            {
                return ShouldCopy(MaterialKey.GetKeyFromMaterial(mat));
            }

            public bool ShouldCopy(MaterialKey mat)
            {
                bool copy = false;
                shouldCopy.TryGetValue(mat, out copy);
                return copy;
            }
        }
    }
}