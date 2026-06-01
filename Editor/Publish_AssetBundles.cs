using UnityEditor;
using System.IO;

public class Build_AssetBundles
{
    [MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        string assetBundleDir = "Assets/AssetBundles";
        if (!Directory.Exists(assetBundleDir))
        {
            Directory.CreateDirectory(assetBundleDir);
        }
        BuildPipeline.BuildAssetBundles(assetBundleDir, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
    }
}
