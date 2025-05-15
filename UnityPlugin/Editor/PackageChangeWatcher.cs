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
    }

    private static void OnPackagesChanged(PackageRegistrationEventArgs args)
    {
        var packages = UniVCCAssetPackage.GetAllAssetPackages();
        foreach (var package in packages)
        {
            Debug.Log("Package found: " + package.name);
            Debug.Log(" - Is Avatar: " + package.isAvatar);
            Debug.Log(" - Prefabs: " + string.Join(", ", package.prefabs));
        }
    }
}