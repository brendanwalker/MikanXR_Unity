using UnityEngine;
using UnityEditor;

public class BuildAssetBundles
{
    [MenuItem("Build/Build AssetBundles")]
    public static void BuildAllAssetBundles()
    {
        string outputFolder = "Assets/__Bundles";

        //Check if __Bundles folder exist
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            Debug.Log("Folder '__Bundles' does not exist, creating new folder");

            AssetDatabase.CreateFolder("Assets", "__Bundles");
        }

        BuildPipeline.BuildAssetBundles(outputFolder, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
    }

    [MenuItem("Tools/Count Scene Objects")]
    public static void Execute()
    {
        int count = GameObject.FindObjectsOfType<Transform>().Length;
        EditorUtility.DisplayDialog(
            "Count Scene Objects",
            "There are " + count + " objects on your scene!",
            "OK");
    }

    [MenuItem("Assets/Create/My Custom Asset")]
    public static void CreateAsset()
    {
        EditorUtility.DisplayDialog(
            "Create Custom Asset",
            "This will create custom asset!",
            "OK");
    }
}