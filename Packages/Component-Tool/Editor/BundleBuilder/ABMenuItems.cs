using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;

namespace Baidu.Meta.ComponentsTool.Editor
{
    /// <summary>
    /// 菜单项
    /// </summary>
    public class ABMenuItems
    {
        //const string buildAll = "Components-Tool/基础数资/BuildAll";
        //const string buildWin64 = "Components-Tool/基础数资/BuildWin64";
        //const string buildWin64Group = "Components-Tool/基础数资/buildWin64Group";
        //const string buildAndroid = "Components-Tool/基础数资/BuildWndroid";
        //const string buildIOS = "Components-Tool/基础数资/BuildIOS";
        //const string buildWebGL = "Components-Tool/基础数资/BuildWebGL";

        //[MenuItem(buildAll, false, 1)]
        //public static void DoBuildAll()
        //{
        //    //BuildAndUpload("win64", false, false);
        //    //BuildAndUpload("android", false, false);
        //    //BuildAndUpload("ios", false, false);
        //    //BuildAndUpload("webgl", false, false);

        //    string[] groups = AssetDatabase.FindAssets("t:ABBundleCollector");
        //    for (int id = 0; id < groups.Length; id++)
        //    {
        //        string filePath = AssetDatabase.GUIDToAssetPath(groups[id]);
        //        ABBundleCollector aBBundleCollector = AssetDatabase.LoadAssetAtPath<ABBundleCollector>(filePath);
        //        aBBundleCollector.CollectAssets(aBBundleCollector, OnCollect);
        //    }

        //}

        //[MenuItem(buildWin64Group, false, 1)]
        public static void DoBuildWinGroup()
        {
            //BuildAndUpload("win64", false, false);
            string[] groups = AssetDatabase.FindAssets("t:ABBundleCollector");
            for (int id = 0; id < groups.Length; id++)
            {
                string filePath= AssetDatabase.GUIDToAssetPath(groups[id]);
                ABBundleCollector aBBundleCollector= AssetDatabase.LoadAssetAtPath<ABBundleCollector>(filePath);
                if(aBBundleCollector.platform==BundlePlatform.win64)
                    aBBundleCollector.CollectAssets(aBBundleCollector, OnCollect);
            }
            //ABBundleCollector.CollectAssets(group, (assetsPerBundle) =>
            //{
            //    Debug.Log(assetsPerBundle.Count);
            //});
        }

        //[MenuItem(buildWin64, false, 1)]
        public static void DoBuildWin()
        {
            BuildAndUpload("android", false, false);
        }

        public static void OnCollect(string _platform,Dictionary<string, List<string>> assetsPerBundle)
        {
            Debug.Log($"OnCollect: collect assets ok build: {_platform}");
            BuildAndUpload(_platform, false, assetsPerBundle);
        }

        //[MenuItem(buildAndroid, false, 1)]
        public static void DoBuildAndroid()
        {
            BuildAndUpload("android", false, false);
        }

        //[MenuItem(buildIOS, false, 1)]
        public static void DoBuildIos()
        {
            BuildAndUpload("ios", false, false);
        }

        //[MenuItem(buildWebGL, false, 1)]
        public static void DoBuildWebgl()
        {
            BuildAndUpload("webgl", false, false);
        }

        /// <summary>
        /// 打包指定的平台到线上或线下
        /// </summary>
        /// <param name="_platform">指定的平台：win64,ios,android</param>
        /// <param name="isBos">是否为线上</param>
        /// <param name="assetsPerBundle">已经配置好的资源配置</param>
        static void BuildAndUpload(string _platform, bool isBos, Dictionary<string, List<string>> assetsPerBundle)
        {
            //string absSrcBundlesFolder = Application.dataPath + ABEditorUtilities.GetSourceBundleFolder();
            //if (Directory.Exists(absSrcBundlesFolder) == false)
            //{
            //    Debug.Log($"Root of bundles folders {absSrcBundlesFolder} does not exist, exit build bundles");
            //    return;
            //}

            if (assetsPerBundle == null || assetsPerBundle.Count == 0)
            {
                Debug.Log($"assetsPerBundle ==null or count==0");
                return;
            }

            if (EditorApplication.isPlaying == false && EditorApplication.isCompiling == false)
            {
                //从Resources文件夹内的ABBuildConfig读取配置
                //ABBuildConfig buildConfig = ABBuildConfig.LoadFromResource();
                ABBuildConfig buildConfig = ABEditorUtilities.GetABBuildConfig();
                //从package.json读取版本号
                string buildVersion = ABEditorUtilities.GetBundleVersionFromPackageJson();
                Debug.Log(buildVersion);
                // get the list of build platforms

                //删除已经存在的打包文件
                string buildRoot = ABEditorUtilities.GetBuildRootFolder(buildVersion);
                if (Directory.Exists(buildRoot))
                {
                    Debug.Log("Build dir exist will delete: " + buildRoot);
                    Directory.Delete(buildRoot, true);
                }

                //vr包按照安卓包配置打包
                string realPlatform = _platform;
                if (_platform == "vr")
                {
                    Debug.Log("platform is vr, real platform is android");
                    realPlatform = "android";
                }
                Dictionary<string, ABBuildInfo> allTargets = buildConfig.ConfigBuildTarget(realPlatform, buildVersion, buildConfig, isBos);

                int failedCount = ABAutoBuilder.DoBuilds(new List<string>(allTargets.Keys), allTargets, assetsPerBundle);

                //如果都打包成功了，上传到服务器
                if (failedCount == 0)
                {
                    //配置文件所在文件夹
                    string localFolder = ABEditorUtilities.GetConfigBuildFolder(buildVersion, realPlatform);
                    Debug.Log("localFolder: " + localFolder);

                    List<string> tempFiles = ABEditorUtilities.GetDirectoryFiles(localFolder);
                    for (int id = 0, cnt = tempFiles.Count; id < cnt; id++)
                    {
                        string metaPath = $"{tempFiles[id]}.meta";
                        ABGenDllMeta.FileMetaFileGen(metaPath);
                    }
                    //
                    string packageFolder = ABEditorUtilities.GetPackageOutputFolder(_platform);
                    Debug.Log("packageFolder: " + packageFolder);
                    ABEditorUtilities.CopyFolder(localFolder, packageFolder);

                    string packageFolderMetaPath = $"{packageFolder}.meta";
                    ABGenDllMeta.FolderMetaFileGen(packageFolderMetaPath);
                }
                else
                {
                    Debug.Log("build failed delete output root");
                    Directory.Delete(ABEditorUtilities.GetBuildRootFolder(buildVersion), true);
                }
            }
            else
            {
                UnityEngine.Debug.Log("<color=#ff8080>AutoBuilder cannot build bundles while running or compiling.</color>");
            }
        }

        /// <summary>
        /// 打包指定的平台到线上或线下
        /// </summary>
        /// <param name="_platform">指定的平台：win64,ios,android,webgl</param>
        /// <param name="isBos">是否为线上</param>
        static void BuildAndUpload(string _platform, bool isBos, bool _isDev = false)
        {
            string absSrcBundlesFolder = Application.dataPath + ABEditorUtilities.GetSourceBundleFolder();
            if (Directory.Exists(absSrcBundlesFolder) == false)
            {
                Debug.Log($"Root of bundles folders {absSrcBundlesFolder} does not exist, exit build bundles");
                return;
            }

            if (EditorApplication.isPlaying == false && EditorApplication.isCompiling == false)
            {
                //从Resources文件夹内的ABBuildConfig读取配置
                //ABBuildConfig buildConfig = ABBuildConfig.LoadFromResource();
                ABBuildConfig buildConfig = ABEditorUtilities.GetABBuildConfig();
                //从package.json读取版本号
                string buildVersion = ABEditorUtilities.GetBundleVersionFromPackageJson();
                Debug.Log(buildVersion);
                // get the list of build platforms

                //删除已经存在的打包文件
                string buildRoot = ABEditorUtilities.GetBuildRootFolder(buildVersion);
                if (Directory.Exists(buildRoot))
                {
                    Debug.Log("Build dir exist will delete: " + buildRoot);
                    Directory.Delete(buildRoot, true);
                }

                Dictionary<string, ABBuildInfo> allTargets = buildConfig.ConfigBuildTarget(_platform, buildVersion, buildConfig, isBos);

                int failedCount = ABAutoBuilder.DoBuilds(new List<string>(allTargets.Keys), allTargets);

                //如果都打包成功了，上传到服务器
                if (failedCount == 0)
                {
                    //配置文件所在文件夹
                    string localFolder = ABEditorUtilities.GetConfigBuildFolder(buildVersion, _platform);
                    Debug.Log("localFolder: " + localFolder);

                    List<string> tempFiles = ABEditorUtilities.GetDirectoryFiles(localFolder);
                    for (int id = 0, cnt = tempFiles.Count; id < cnt; id++)
                    {
                        string metaPath = $"{tempFiles[id]}.meta";
                        ABGenDllMeta.FileMetaFileGen(metaPath);
                    }
                    //
                    string packageFolder = ABEditorUtilities.GetPackageOutputFolder(_platform);
                    Debug.Log("packageFolder: " + packageFolder);
                    ABEditorUtilities.CopyFolder(localFolder, packageFolder);

                    string packageFolderMetaPath = $"{packageFolder}.meta";
                    ABGenDllMeta.FolderMetaFileGen(packageFolderMetaPath);
                }
                else
                {
                    Debug.Log("build failed delete output root");
                    Directory.Delete(ABEditorUtilities.GetBuildRootFolder(buildVersion), true);
                }
            }
            else
            {
                UnityEngine.Debug.Log("<color=#ff8080>AutoBuilder cannot build bundles while running or compiling.</color>");
            }
        }

    }
}
