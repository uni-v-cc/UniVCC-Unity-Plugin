using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;

namespace UniVCC
{
    public class MaterialDuplicator
    {
        public static readonly string COMMON_PATH = "Assets/!Uni-V.CC Packages";
        private List<IMaterialScanner> instances = new List<IMaterialScanner>();
        protected string avatarName;

        public MaterialDuplicator(string avatarName)
        {
            this.avatarName = avatarName;
        }

        public void SetupDuplicator(Predicate<Material> checkForCopy)
        {
            instances.Add(new RendererMaterialScanner(checkForCopy));
        }

        public GatheringMaterialScanner SetupScanner()
        {
            var gatherer = new GatheringMaterialScanner();
            instances.Add(gatherer);
            return gatherer;
        }

        public void VisitGameObject(GameObject go)
        {
            foreach (var scanner in instances)
            {
                scanner.Reset();
            }

            TraverseAndReplaceMaterials(go.transform);

            foreach (var scanner in instances)
            {
                scanner.Finalize(this);
            }
        }

        private void TraverseAndReplaceMaterials(Transform transform)
        {
            foreach (var scanner in instances)
            {
                scanner.Gather(transform);
            }

            foreach (Transform child in transform)
            {
                TraverseAndReplaceMaterials(child);
            }
        }

        public Material Copy(Material material)
        {
            if (material == null) return null;

            string sourcePath = AssetDatabase.GetAssetPath(material);
            if (string.IsNullOrEmpty(sourcePath))
            {
                Debug.LogWarning($"Material '{material.name}' is not a saved asset. Skipping.");
                return material;
            }

            bool isEmbedded = !string.IsNullOrEmpty(sourcePath) && Path.GetExtension(sourcePath).ToLower() == ".fbx";

            string materialsDir = $"{COMMON_PATH}/{avatarName}/Materials";
            Directory.CreateDirectory(materialsDir); // creates full path if needed

            string destPath = $"{materialsDir}/{material.name}.mat";

            if (File.Exists(destPath))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(destPath);
                if (mat != null)
                {
                    Debug.Log($"Material '{destPath}' already exists. Reusing!");
                    return mat;
                }
            }

            destPath = AssetDatabase.GenerateUniqueAssetPath(destPath);

            var type = isEmbedded ? "embedded" : "separate";
            Debug.Log($"Copying {type} material '{sourcePath}' to '{destPath}'");

            Material newMaterial;

            if (isEmbedded)
            {
                newMaterial = new Material(material.shader)
                {
                    name = material.name
                };

                newMaterial.CopyMatchingPropertiesFromMaterial(material);

                foreach (var propName in material.GetTexturePropertyNames())
                {
                    Texture tex = material.GetTexture(propName);
                    if (tex != null)
                        newMaterial.SetTexture(propName, CopyTexture(material.name, tex));
                }

                AssetDatabase.CreateAsset(newMaterial, destPath);
            }
            else
            {
                // Regular material asset, just copy the file
                AssetDatabase.CopyAsset(sourcePath, destPath);
                newMaterial = AssetDatabase.LoadAssetAtPath<Material>(destPath);

                if (newMaterial != null)
                {
                    // Copy textures
                    foreach (var propName in newMaterial.GetTexturePropertyNames())
                    {
                        Texture texture = newMaterial.GetTexture(propName);
                        if (texture != null)
                            newMaterial.SetTexture(propName, CopyTexture(material.name, texture));
                    }
                }
            }

            EditorUtility.SetDirty(newMaterial);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return newMaterial;
        }

        private Texture CopyTexture(string materialName, Texture tx)
        {
            if (tx == null) return null;

            string sourcePath = AssetDatabase.GetAssetPath(tx);
            if (string.IsNullOrEmpty(sourcePath))
            {
                Debug.LogWarning($"Texture '{tx.name}' is not a saved asset. Skipping.");
                return tx;
            }
            
            // get file extension from source path
            string extension = Path.GetExtension(sourcePath);

            string materialsDir = $"{COMMON_PATH}/{avatarName}/Textures/{materialName}";
            Directory.CreateDirectory(materialsDir); // creates full path if needed

            string destPath = $"{materialsDir}/{tx.name}{extension}";

            if (File.Exists(destPath))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture>(destPath);
                if (tex != null)
                {
                    Debug.Log($"Texture '{destPath}' already exists. Reusing!");
                    return tex;
                }
            }

            destPath = AssetDatabase.GenerateUniqueAssetPath(destPath);

            Debug.Log($"Copying texture '{sourcePath}' to '{destPath}'");

            AssetDatabase.CopyAsset(sourcePath, destPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<Texture>(destPath);
        }
    }

    interface IMaterialScanner
    {
        void Reset();

        void Gather(Transform transform);

        void Finalize(MaterialDuplicator duplicator);
    }

    public class RendererMaterialScanner : IMaterialScanner
    {
        protected readonly Predicate<Material> checkForCopy;
        protected List<Renderer> renderers = new List<Renderer>();

        public RendererMaterialScanner(Predicate<Material> checkForCopy)
        {
            this.checkForCopy = checkForCopy;
        }

        public void Reset()
        {
            renderers.Clear();
        }

        public void Gather(Transform transform)
        {
            var renderer = transform.GetComponent<Renderer>();
            if (renderer != null) renderers.Add(renderer);
        }

        public void Finalize(MaterialDuplicator duplicator)
        {
            Dictionary<MaterialKey, Material> copies = new Dictionary<MaterialKey, Material>();
            foreach (Renderer renderer in renderers)
            {
                var originalMaterials = renderer.sharedMaterials;
                var newMaterials = new Material[originalMaterials.Length];

                for (int i = 0; i < originalMaterials.Length; i++)
                {
                    var omat = originalMaterials[i];
                    var mkey = MaterialKey.GetKeyFromMaterial(omat);

                    Material m;
                    if (!copies.ContainsKey(mkey))
                    {
                        m = checkForCopy(omat) ? duplicator.Copy(omat) : omat;
                        copies.Add(mkey, m);
                    } else copies.TryGetValue(mkey, out m);

                    newMaterials[i] = m;
                }

                renderer.sharedMaterials = newMaterials;
            }
        }
    }

    public class GatheringMaterialScanner : IMaterialScanner
    {
        public readonly List<Renderer> renderers = new List<Renderer>();
        public readonly Dictionary<MaterialKey, Material> materials = new Dictionary<MaterialKey, Material>();

        public void Reset()
        {
            renderers.Clear();
            materials.Clear();
        }

        public void Gather(Transform transform)
        {
            var renderer = transform.GetComponent<Renderer>();
            if (renderer != null) renderers.Add(renderer);
        }

        public void Finalize(MaterialDuplicator duplicator)
        {
            foreach (Renderer renderer in renderers)
            {
                var originalMaterials = renderer.sharedMaterials;
                var newMaterials = new Material[originalMaterials.Length];
                for (int i = 0; i < originalMaterials.Length; i++)
                {
                    var mkey = MaterialKey.GetKeyFromMaterial(originalMaterials[i]);

                    if (!materials.ContainsKey(mkey))
                    {
                        materials[mkey] = originalMaterials[i];
                    }
                }
            }
        }
    }

    public struct MaterialKey
    {
        public GUID guid;
        public ulong localId;
        public string path;
        public string name;

        public MaterialKey(GUID guid, ulong localId, string path, string name)
        {
            this.guid = guid;
            this.localId = localId;
            this.path = path;
            this.name = name;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + localId.GetHashCode();
            hash = hash * 31 + guid.GetHashCode();
            hash = hash * 31 + path.GetHashCode();
            hash = hash * 31 + name.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj) =>
            obj is MaterialKey other && guid.Equals(other.guid) && localId == other.localId && name == other.name && path == other.path;

        public static MaterialKey GetKeyFromMaterial(Material material)
        {
            if (material == null)
                return new MaterialKey(new GUID(), 0, "", "Unnamed");
            string path = AssetDatabase.GetAssetPath(material);
            if (string.IsNullOrEmpty(path))
                return new MaterialKey(new GUID(), 0, "", "Unnamed");
            string guidStr = AssetDatabase.AssetPathToGUID(path);
            ulong localId = Unsupported.GetLocalIdentifierInFileForPersistentObject(material);
            return new MaterialKey(new GUID(guidStr), localId, path, material.name);
        }
    }

}