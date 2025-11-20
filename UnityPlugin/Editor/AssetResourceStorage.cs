using System;
using UnityEngine;

namespace UniVCC
{
    [Serializable]
    public class AssetResourceStorage
    {
        [Tooltip("Should a sub-folder be allocated for materials and textures for this asset variant?")]
        public bool subFolder = false;

        [Tooltip("Should this package use standalone folder in the packages directory?")]
        public bool standaloneFolder = false;

        [Tooltip("Actual folder name for copied materials & textures. Will be inside the asset's folder, or in the uni-vcc packages if standalone.")]
        public string separateFolderName = "";

        [ShowOnly, Tooltip("The final path for this prefab.")]
        public string finalPath;

        public string GetSubPath(UniVCCAssetPackage package, ImportableAsset asset)
        {
            string folderName = string.IsNullOrEmpty(separateFolderName) ? asset.displayName : separateFolderName;
            if (standaloneFolder)
            {
                if (subFolder && !string.IsNullOrEmpty(separateFolderName)) return folderName + "/" + asset.displayName;
                return folderName;
            }
            return package.packageName + (subFolder ? "/" + folderName : "");
        }
    }
}