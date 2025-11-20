using System.Collections.Generic;
using System;
using UnityEngine;

namespace UniVCC
{
    [Serializable]
    public class ImportableAsset
    {
        [Tooltip("Display name for this asset")]
        public string displayName = "Unnamed Asset";

        [Tooltip("Prefab to include in this package")]
        public GameObject prefab;

        [Tooltip("Where the materials and textures should be copied to")]
        public AssetResourceStorage storage = new AssetResourceStorage();

        [Tooltip("Materials that should not get copied")]
        public List<Material> uncopyableMaterials = new List<Material>();

        [Tooltip("Materials that should default to not copied")]
        public List<Material> discouragedCopyableMaterials = new List<Material>();

        public string GetSubPath(UniVCCAssetPackage package)
        {
            return storage.GetSubPath(package, this);
        }
    }
}