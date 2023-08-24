using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor.Build.Reporting;

namespace Baidu.Meta.ComponentsTool.Editor
{
    public class ABAutoBuilder
    {
        // This handles the noisy business of actually doing builds, printing out results, etc.  Returns the number that FAILED.
        static public int DoBuilds(List<string> buildTargets, Dictionary<string, ABBuildInfo> allTargets)
        {
            int builds = 0;
            int failed = 0;
            foreach (string target in buildTargets)
            {
                builds++;

                BuildResult buildResult = DoBuild(allTargets[target]);  // try the build

                if (buildResult != BuildResult.Succeeded)  // count failures
                    failed++;

                if (buildResult == BuildResult.Cancelled)  // quit all builds immediately if one was canceled
                    break;
            }
            Debug.Log(failed + " builds failed out of " + builds);
            return failed;
        }

        static public int DoBuilds(List<string> buildTargets, Dictionary<string, ABBuildInfo> allTargets, Dictionary<string, List<string>> assetsPerBundle)
        {
            int builds = 0;
            int failed = 0;
            foreach (string target in buildTargets)
            {
                builds++;

                BuildResult buildResult = DoBuild(allTargets[target], assetsPerBundle);  // try the build

                if (buildResult != BuildResult.Succeeded)  // count failures
                    failed++;

                if (buildResult == BuildResult.Cancelled)  // quit all builds immediately if one was canceled
                    break;
            }
            Debug.Log(failed + " builds failed out of " + builds);
            return failed;
        }

        /// <summary>
        /// 使用已配置好的资源buildbundle
        /// </summary>
        /// <param name="buildInfo"></param>
        /// <param name="assetsPerBundle"></param>
        /// <returns></returns>
        static private BuildResult DoBuild(ABBuildInfo buildInfo, Dictionary<string, List<string>> assetsPerBundle)
        {
            // nuke the dest folder--if you make multiple builds in a row and *remove* something from the build, it will still be there.

            Directory.CreateDirectory(buildInfo.DestBundleFolder);
            Directory.CreateDirectory(buildInfo.DestConfigFolder);

            // Write out the config file
            string configEmbedPath = Application.streamingAssetsPath + "/config_" + buildInfo.TargetString + ".json";

            // Write out the Version file (for easy scripting)
            string versionPath = buildInfo.DestConfigFolder + "/version.txt";
            File.WriteAllText(versionPath, buildInfo.BuildVersion);

            // This requires bundles to be build BEFORE the app, because the bundles need to be IN the app.
            BuildResult result = BuildResult.Failed;
            if (buildInfo.SelfContainedBuild)
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);

                string absEmbedPath = Application.streamingAssetsPath + buildInfo.EmbedBundleFolder;
                if (Directory.Exists(absEmbedPath))
                    Directory.Delete(absEmbedPath, true);  // nuke anything that might be still sitting there since last build

                //先删除之前的Build文件
                File.Delete(absEmbedPath.TrimEnd('/', '\\') + ".meta");  // clean up the meta files we produce while making a build
                //防止切平台之后，之前平台config文件未删除
                string configEmbedPath1 = Application.streamingAssetsPath + "/config_win64" + ".json";
                string configEmbedPath2 = Application.streamingAssetsPath + "/config_android" + ".json";
                string configEmbedPath3 = Application.streamingAssetsPath + "/config_ios" + ".json";
                string configEmbedPath4 = Application.streamingAssetsPath + "/config_webgl" + ".json";
                File.Delete(configEmbedPath1);
                File.Delete(configEmbedPath1 + ".meta");
                File.Delete(configEmbedPath2);
                File.Delete(configEmbedPath2 + ".meta");
                File.Delete(configEmbedPath3);
                File.Delete(configEmbedPath3 + ".meta");
                File.Delete(configEmbedPath4);
                File.Delete(configEmbedPath4 + ".meta");

                File.Delete(configEmbedPath);
                File.Delete(configEmbedPath + ".meta");

                // Build asset bundles AFTER we build the player successfully.
                if (ABBundleBuilder.DoBuildBundles(assetsPerBundle, buildInfo.BuildVersion,
                    buildInfo.DestBundleFolder, buildInfo.BundleOptions, buildInfo.Target, buildInfo.TargetString,
                    buildInfo.Logging, buildInfo.IgnoreEndsWith, buildInfo.IgnoreContains, buildInfo.IgnoreExact))
                {
                    // Copy in the bundles to the right place in /StreamingAssets/
                    Directory.CreateDirectory(absEmbedPath);

                    // Write out the config file
                    ABBootstrap tmpBootstrap = JsonUtility.FromJson<ABBootstrap>(buildInfo.ConfigJson);
                    long totalLength = GetDirectoryLength(ABEditorUtilities.GetBuildRootFolder(buildInfo.BuildVersion));
                    Debug.Log("Bundle Total Length: " + totalLength);
                    tmpBootstrap.totalFileSize = totalLength;
                    //tmpBootstrap.packageName = ABEditorUtilities.GetPackageName();
                    configPath = buildInfo.DestConfigFolder + "/config_" + buildInfo.TargetString + ".json";
                    //string configEmbedPath = Application.streamingAssetsPath + "/config_" + buildInfo.TargetString + ".json";
                    string configJson = JsonUtility.ToJson(tmpBootstrap);
                    File.WriteAllText(configPath, configJson, System.Text.Encoding.UTF8);

                    File.Copy(configPath, configEmbedPath, true);

                    // Copy all bundles.  This only works because there are no subdirectories.
                    foreach (var file in Directory.GetFiles(buildInfo.DestBundleFolder))
                        File.Copy(file, Path.Combine(absEmbedPath, Path.GetFileName(file)), true);

                    result = BuildResult.Succeeded;
                }
            }
            else
            {

                bool bundleStatus = ABBundleBuilder.DoBuildBundles(assetsPerBundle, buildInfo.BuildVersion,
                    buildInfo.DestBundleFolder, buildInfo.BundleOptions, buildInfo.Target, buildInfo.TargetString,
                    buildInfo.Logging, buildInfo.IgnoreEndsWith, buildInfo.IgnoreContains, buildInfo.IgnoreExact);

                // Write out the config file
                ABBootstrap tmpBootstrap = JsonUtility.FromJson<ABBootstrap>(buildInfo.ConfigJson);
                long totalLength = GetDirectoryLength(ABEditorUtilities.GetBuildRootFolder(buildInfo.BuildVersion));
                Debug.Log("Bundle Total Length: " + totalLength);
                tmpBootstrap.totalFileSize = totalLength;
                //tmpBootstrap.packageName = ABEditorUtilities.GetPackageName();
                configPath = buildInfo.DestConfigFolder + "/config_" + buildInfo.TargetString + ".json";
                string configJson = JsonUtility.ToJson(tmpBootstrap);
                File.WriteAllText(configPath, configJson, System.Text.Encoding.UTF8);

                result = bundleStatus ? BuildResult.Succeeded : BuildResult.Failed;
            }
            return result;
        }

        static private BuildResult DoBuild(ABBuildInfo buildInfo)
        {
            // nuke the dest folder--if you make multiple builds in a row and *remove* something from the build, it will still be there.

            Directory.CreateDirectory(buildInfo.DestBundleFolder);
            Directory.CreateDirectory(buildInfo.DestConfigFolder);
           
            // Write out the config file
            string configEmbedPath = Application.streamingAssetsPath + "/config_" + buildInfo.TargetString + ".json";

            // Write out the Version file (for easy scripting)
            string versionPath = buildInfo.DestConfigFolder + "/version.txt";
            File.WriteAllText(versionPath, buildInfo.BuildVersion);

            // This requires bundles to be build BEFORE the app, because the bundles need to be IN the app.
            BuildResult result = BuildResult.Failed;
            if (buildInfo.SelfContainedBuild)
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);

                string absEmbedPath = Application.streamingAssetsPath + buildInfo.EmbedBundleFolder;
                if (Directory.Exists(absEmbedPath))
                    Directory.Delete(absEmbedPath, true);  // nuke anything that might be still sitting there since last build

                //先删除之前的Build文件
                File.Delete(absEmbedPath.TrimEnd('/', '\\') + ".meta");  // clean up the meta files we produce while making a build
                //防止切平台之后，之前平台config文件未删除
                string configEmbedPath1 = Application.streamingAssetsPath + "/config_win64"  + ".json";
                string configEmbedPath2 = Application.streamingAssetsPath + "/config_android" + ".json";
                string configEmbedPath3 = Application.streamingAssetsPath + "/config_ios" + ".json";
                string configEmbedPath4 = Application.streamingAssetsPath + "/config_webgl" + ".json";
                File.Delete(configEmbedPath1);
                File.Delete(configEmbedPath1 + ".meta");
                File.Delete(configEmbedPath2);
                File.Delete(configEmbedPath2 + ".meta");
                File.Delete(configEmbedPath3);
                File.Delete(configEmbedPath3 + ".meta");
                File.Delete(configEmbedPath4);
                File.Delete(configEmbedPath4 + ".meta");

                File.Delete(configEmbedPath);
                File.Delete(configEmbedPath + ".meta");

                // Build asset bundles AFTER we build the player successfully.
                if (ABBundleBuilder.DoBuildBundles(buildInfo.SrcBundleFolder, buildInfo.BuildVersion,
                    buildInfo.DestBundleFolder, buildInfo.BundleOptions, buildInfo.Target, buildInfo.TargetString,
                    buildInfo.Logging, buildInfo.IgnoreEndsWith, buildInfo.IgnoreContains, buildInfo.IgnoreExact))
                {
                    // Copy in the bundles to the right place in /StreamingAssets/
                    Directory.CreateDirectory(absEmbedPath);

                    // Write out the config file
                    ABBootstrap tmpBootstrap = JsonUtility.FromJson<ABBootstrap>(buildInfo.ConfigJson);
                    long totalLength = GetDirectoryLength(ABEditorUtilities.GetBuildRootFolder(buildInfo.BuildVersion));
                    Debug.Log("Bundle Total Length: " + totalLength);
                    tmpBootstrap.totalFileSize = totalLength;
                    //tmpBootstrap.packageName = ABEditorUtilities.GetPackageName();
                    configPath = buildInfo.DestConfigFolder + "/config_" + buildInfo.TargetString + ".json";
                    //string configEmbedPath = Application.streamingAssetsPath + "/config_" + buildInfo.TargetString + ".json";
                    string configJson = JsonUtility.ToJson(tmpBootstrap);
                    File.WriteAllText(configPath, configJson, System.Text.Encoding.UTF8);

                    File.Copy(configPath, configEmbedPath, true);

                    // Copy all bundles.  This only works because there are no subdirectories.
                    foreach (var file in Directory.GetFiles(buildInfo.DestBundleFolder))
                        File.Copy(file, Path.Combine(absEmbedPath, Path.GetFileName(file)), true);

                    result = BuildResult.Succeeded;
                }
            }
            else
            {

                bool bundleStatus = ABBundleBuilder.DoBuildBundles(buildInfo.SrcBundleFolder, buildInfo.BuildVersion,
                    buildInfo.DestBundleFolder, buildInfo.BundleOptions, buildInfo.Target, buildInfo.TargetString,
                    buildInfo.Logging, buildInfo.IgnoreEndsWith, buildInfo.IgnoreContains, buildInfo.IgnoreExact);

                // Write out the config file
                ABBootstrap tmpBootstrap = JsonUtility.FromJson<ABBootstrap>(buildInfo.ConfigJson);
                long totalLength = GetDirectoryLength(ABEditorUtilities.GetBuildRootFolder(buildInfo.BuildVersion));
                Debug.Log("Bundle Total Length: " + totalLength);
                tmpBootstrap.totalFileSize = totalLength;
                //tmpBootstrap.packageName = ABEditorUtilities.GetPackageName();
                configPath = buildInfo.DestConfigFolder + "/config_" + buildInfo.TargetString + ".json";
                string configJson = JsonUtility.ToJson(tmpBootstrap);
                File.WriteAllText(configPath, configJson, System.Text.Encoding.UTF8);

                result = bundleStatus ? BuildResult.Succeeded : BuildResult.Failed;
            }
            return result;
        }

        static string configPath = "";
        /// <summary>
        /// 获取文件夹大小
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        static long GetDirectoryLength(string dirPath)
        {
            //判断给定的路径是否存在,如果不存在则退出
            if (!Directory.Exists(dirPath))
                return 0;
            long len = 0;
            //定义一个DirectoryInfo对象
            DirectoryInfo di = new DirectoryInfo(dirPath);
            //通过GetFiles方法,获取di目录中的所有文件的大小
            foreach (FileInfo fi in di.GetFiles())
            {
                //排除Config文件的大小
                if (fi.FullName != configPath)
                    len += fi.Length;
            }
            //获取di中所有的文件夹,并存到一个新的对象数组中,以进行递归
            DirectoryInfo[] dis = di.GetDirectories();
            if (dis.Length > 0)
            {
                for (int i = 0; i < dis.Length; i++)
                {
                    len += GetDirectoryLength(dis[i].FullName);
                }
            }
            return len;
        }
    }
}
