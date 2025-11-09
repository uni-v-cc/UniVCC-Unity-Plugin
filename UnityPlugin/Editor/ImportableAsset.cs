using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace UniVCC
{
    [Serializable]
    public class ImportableAsset
    {
        [Tooltip("Display name for this asset")]
        public string displayName = "Unnamed Asset";

        [Tooltip("Should a separate folder be allocated for materials and textures for this asset variant?")]
        public bool separateMaterialsFolder = false;

        [Tooltip("Prefab to include in this package")]
        public GameObject prefab;

        [Tooltip("Materials that should not get copied")]
        public List<Material> uncopyableMaterials = new List<Material>();

        [Tooltip("Materials that should default to not copied")]
        public List<Material> discouragedCopyableMaterials = new List<Material>();

        public string GetAssetPath()
        {
            if (prefab == null) return null;
            return AssetDatabase.GetAssetPath(prefab);
        }
    }
}