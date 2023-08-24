using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace Baidu.Meta.ComponentsTool.Editor
{
    public class ABBuildConfig : ScriptableObject
    {
        static public string kBuildConfigPath = "ABBuildConfig";

        [Header("Runtime Config")]
        [Tooltip("You can add fields to the ABBootstrap class if you want.  " +
            "This is the first file loaded on startup, which you can use to set URLs to server endpoints, for example.")]
        public ABBootstrap bootstrap = new ABBootstrap();

        [Header("Build Config")]
        [Tooltip("This indicates the root folder where you want each subfolder to become an asset bundle." +
            "\nFormat it with slashes before and after as such: /Bundles/\nThis will expand to Assets/Bundles/...")]
        public string sourceBundleFolder = "/Bundles/";

        [HideInInspector]
        public PlatformOptions[] platforms = new PlatformOptions[]
        {
                new PlatformOptions(BuildTarget.StandaloneWindows),
                new PlatformOptions(BuildTarget.StandaloneWindows64),
                new PlatformOptions(BuildTarget.WSAPlayer),
                new PlatformOptions(BuildTarget.StandaloneOSX),
                new PlatformOptions(BuildTarget.StandaloneLinux64),
                new PlatformOptions(BuildTarget.WebGL),
                new PlatformOptions(BuildTarget.Android),
                new PlatformOptions(BuildTarget.iOS),
                new PlatformOptions(BuildTarget.tvOS),
                new PlatformOptions(BuildTarget.PS4),
                new PlatformOptions(BuildTarget.XboxOne),
                new PlatformOptions(BuildTarget.Switch),
                new PlatformOptions(BuildTarget.Lumin),
                new PlatformOptions(BuildTarget.Stadia),
                //new PlatformOptions(BuildTarget.LinuxHeadlessSimulation),
                new PlatformOptions(BuildTarget.PS5),
                //new PlatformOptions(BuildTarget.GameCoreXboxOne),
                //new PlatformOptions(BuildTarget.GameCoreXboxSeries),
        };

        [Header("Ignoring Assets")]
        [Tooltip("Any assets whose full path EndsWith any entries here will NOT be added to an asset bundle.  " +
            "Only use lowercase and forward slashes.  Examples: .pdf or _ignored.asset")]
        public string[] ignoreEndsWith = new string[0] { };

        [Tooltip("Any assets whose path that Contains any entries here will NOT be added to an asset bundle.  " +
            "Only use lowercase and forward slashes.  Examples: /ignore/ or _nobundle")]

        public string[] ignoreContains = new string[0] { };
        [Tooltip("Explicit full path of an asset can be added here, but not generally recommended, " +
            "since you can't easily reorganize files in folders.  " +
            "Only use lowercase and forward slashes.  Example: assets/bundles/bundlex/filename.png")]

        public string[] ignoreExact = new string[0] { };


        // Each platform gets its own unique space to set options.
        [Serializable]
        public class PlatformOptions
        {
            // This must come first to show up in the Inspector as the drop-down label.
            public string Name;
            public BuildTarget Platform;

            [Space]
            [Header("Asset Bundles Options")]
            public bool Uncompressed = false;
            public bool ForceRebuild = false;
            public bool DisableWriteTypeTree = false;
            public bool IgnoreTypeTreeChanges = false;
            public bool ChunkBasedCompression = true;
            public bool AssetBundleStripUnityVersion = true;
            public bool StrictMode = true;

            public bool DisableLoadAssetByFileName = false;
            public bool DisableLoadAssetByFileNameWithExtension = false;

            //-------------------
            // This combines the settings saved in the ABBuildConfig scriptable object to control the build of asset bundles.
            public BuildAssetBundleOptions GenerateBundleOptionsFromSettings()
            {
                BuildAssetBundleOptions options = BuildAssetBundleOptions.None;
                if (Uncompressed)
                    options |= BuildAssetBundleOptions.UncompressedAssetBundle;
                if (DisableWriteTypeTree)
                    options |= BuildAssetBundleOptions.DisableWriteTypeTree;
                //					if (DeterministicAssetBundle)
                //						options |= BuildAssetBundleOptions.DeterministicAssetBundle;
                if (ForceRebuild)
                    options |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
                if (IgnoreTypeTreeChanges)
                    options |= BuildAssetBundleOptions.IgnoreTypeTreeChanges;
                if (ChunkBasedCompression)
                    options |= BuildAssetBundleOptions.ChunkBasedCompression;
                if (StrictMode)
                    options |= BuildAssetBundleOptions.StrictMode;
                if (DisableLoadAssetByFileName)
                    options |= BuildAssetBundleOptions.DisableLoadAssetByFileName;
                if (DisableLoadAssetByFileNameWithExtension)
                    options |= BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;
                if (AssetBundleStripUnityVersion)
                    options |= BuildAssetBundleOptions.AssetBundleStripUnityVersion;
                return options;
            }

            // Constructor to set name properly.
            public PlatformOptions(BuildTarget platform)
            {
                Platform = platform;
                Name = ABEditorUtilities.GetPlatform(platform);
            }
        }

        /// <summary>
        /// 指定一个单独的平台打包，不管配置表中激活与否
        /// </summary>
        /// <param name="_platformStr">传win64,android,ios,webgl其中一个</param>
        /// <param name="buildVersion">版本号</param>
        /// <param name="buildConfig"></param>
        /// <param name="isBos"></param>
        /// <param name="selfContainedBuild">是否内嵌包</param>
        /// <returns></returns>
        public Dictionary<string, ABBuildInfo> ConfigBuildTarget(string _platformStr, string buildVersion, ABBuildConfig buildConfig, bool isBos = false,bool selfContainedBuild=false)
        {
            if (string.IsNullOrEmpty(Application.productName))
                Debug.LogError("Product Name is not set.  Do this in Edit->ProjectSettings->Player");

            // Build a configuration dictionary with all the settings stored PER-PLATFORM, for easy extraction.
            Dictionary<string, ABBuildInfo> buildTargets = new Dictionary<string, ABBuildInfo>();
            foreach (PlatformOptions po in platforms)
            {
                if (_platformStr == "win64" && po.Platform == BuildTarget.StandaloneWindows64 ||
                   _platformStr == "android" && po.Platform == BuildTarget.Android ||
                   _platformStr == "ios" && po.Platform == BuildTarget.iOS ||
                   _platformStr == "webgl" && po.Platform == BuildTarget.WebGL)
                {
                    // If this platform is specified twice in an ENABLED state, this is an error.
                    string platformString = ABEditorUtilities.GetPlatform(po.Platform);

                    if (buildTargets.ContainsKey(platformString))
                        Debug.LogError("Same platform is enabled twice: " + platformString);

                    // dest folder for all configuration files, which tend to NOT go to the CDN, but instead to a web site
                    string configBuildFolder = ABEditorUtilities.GetConfigBuildFolder(buildVersion, platformString);
                    string bundleBuildFolder = ABEditorUtilities.GetBundleBuildFolder(buildVersion, platformString);  // dest asset bundles folder  
                    string absSrcBundlesFolder = Application.dataPath + buildConfig.sourceBundleFolder;

                    // If this is a selfContainedBuild, this is where we will put the config.json, manifest, and bundles just before building the Player.
                    string embedBundlesFolder = ABEditorUtilities.GetEmbedBundlesFolder();


                    if (po.Platform == BuildTarget.WebGL)
                        po.DisableWriteTypeTree = false;


                    ABBootstrap tmpBootstrap = new ABBootstrap(bootstrap);
                    if (selfContainedBuild)
                    {
                        tmpBootstrap.cdnBundleUrl = embedBundlesFolder;
                    }
                    else
                    {
                        tmpBootstrap.cdnBundleUrl = ABEditorUtilities.GetBootstrapUrl(platformString, buildVersion);

                        if (!tmpBootstrap.cdnBundleUrl.EndsWith("/"))
                            tmpBootstrap.cdnBundleUrl = tmpBootstrap.cdnBundleUrl + "/";
                    }
                    string configJson = JsonUtility.ToJson(tmpBootstrap);
                    // Generated straight from checkboxes in the ScriptableObject.
                    BuildAssetBundleOptions bundleOptions = po.GenerateBundleOptionsFromSettings();
                    buildTargets.Add(platformString, new ABBuildInfo(po.Platform, platformString, bundleOptions,
                        absSrcBundlesFolder, bundleBuildFolder, configBuildFolder,
                        configJson, selfContainedBuild, embedBundlesFolder,
                        buildVersion, true, buildConfig.ignoreEndsWith, buildConfig.ignoreContains, buildConfig.ignoreExact));
                }
            }

            return buildTargets;
        }

    }
}
