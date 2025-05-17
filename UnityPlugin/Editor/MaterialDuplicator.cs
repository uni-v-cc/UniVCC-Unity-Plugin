using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace UniVCC
{
    public class MaterialDuplicator
    {
        public static readonly string COMMON_PATH = "Assets/!Uni-V.CC Packages";
        private static List<IMaterialScanner> instances = new List<IMaterialScanner>();
        protected string avatarName;

        static MaterialDuplicator()
        {
            instances.Add(new RendererMaterialScanner());
        }

        public MaterialDuplicator(string avatarName)
        {
            this.avatarName = avatarName;
        }

        public void VisitGameObject(GameObject go)
        {
            TraverseAndReplaceMaterials(go.transform);
        }

        private void TraverseAndReplaceMaterials(Transform transform)
        {
            foreach (var scanner in instances)
            {
                scanner.CopyMaterials(transform, this);
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

            Debug.Log($"Copying material '{sourcePath}' to '{destPath}'");

            AssetDatabase.CopyAsset(sourcePath, destPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Material mater = AssetDatabase.LoadAssetAtPath<Material>(destPath);

            if (mater != null)
            {
                // Copy textures from the original material to the new one
                foreach (var property in mater.GetTexturePropertyNames())
                {
                    Texture texture = mater.GetTexture(property);
                    if (texture != null)
                    {
                        Texture newTexture = CopyTexture(material.name, texture);
                        mater.SetTexture(property, newTexture);
                    }
                }

                // Save the modified material
                EditorUtility.SetDirty(mater);
                AssetDatabase.SaveAssets();
            }

            return mater;
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
        void CopyMaterials(Transform transform, MaterialDuplicator duplicator);
    }

    public class RendererMaterialScanner : IMaterialScanner
    {
        public void CopyMaterials(Transform transform, MaterialDuplicator duplicator)
        {
            var renderer = transform.GetComponent<Renderer>();
            if (renderer != null)
            {
                var originalMaterials = renderer.sharedMaterials;
                var newMaterials = new Material[originalMaterials.Length];

                for (int i = 0; i < originalMaterials.Length; i++)
                {
                    newMaterials[i] = duplicator.Copy(originalMaterials[i]);
                }

                renderer.sharedMaterials = newMaterials;
            }
        }
    }
}