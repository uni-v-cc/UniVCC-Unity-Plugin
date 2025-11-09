using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UniVCC;

public class IconPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                                       string[] movedAssets, string[] movedFromAssetPaths)
    {
        List<UniVCCAssetPackage> assetPackages = new List<UniVCCAssetPackage>();
        foreach (string assetPath in importedAssets)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniVCCAssetPackage>(assetPath);
            if (asset != null) assetPackages.Add(asset);
        }

        if (assetPackages.Count > 0)
        {
            AssetPackageEditor.ApplyIcon(assetPackages.ToArray());
        }
    }
}