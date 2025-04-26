using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace UniVCC
{
    public class MaterialDuplicator
    {
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

            string materialsDir = $"Assets/_UniVCC-Avatars/{avatarName}/Materials";
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

            return AssetDatabase.LoadAssetAtPath<Material>(destPath);
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