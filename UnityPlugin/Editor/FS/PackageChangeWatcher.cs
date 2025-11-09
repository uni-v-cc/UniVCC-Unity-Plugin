using UnityEditor;
using UnityEditor.PackageManager;
using static UnityEditor.PackageManager.Events;
using UnityEngine;

using UniVCC;

[InitializeOnLoad]
public static class PackageChangeWatcher
{
    static PackageChangeWatcher()
    {
        Events.registeredPackages += OnPackagesChanged;

        AssetPackageEditor.ApplyIcon(UniVCCAssetPackage.GetAllAssetPackages());
    }

    private static void OnPackagesChanged(PackageRegistrationEventArgs args)
    {
        AssetPackageEditor.ApplyIcon(UniVCCAssetPackage.GetAllAssetPackages());
    }
}