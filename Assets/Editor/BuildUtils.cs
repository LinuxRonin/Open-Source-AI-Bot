#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildUtils
{
    [MenuItem("Build/Build Knowledge Base AssetBundle")]
    public static void BuildKnowledgeBaseAssetBundle()
    {
        string assetBundleDirectory = "Assets/AssetBundles";
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }

        AssetBundleBuild[] buildMap = new AssetBundleBuild[1];
        buildMap[0].assetBundleName = "knowledge_base";
        buildMap[0].assetNames = new string[] { "Assets/AI/KnowledgeBase/KnowledgeBase.asset" }; // Path to your KnowledgeBase asset

        BuildPipeline.BuildAssetBundles(assetBundleDirectory, buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows); // Change BuildTarget as needed

        Debug.Log("Knowledge Base AssetBundle built to: " + assetBundleDirectory);
    }
}
#endif