using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Text;
using Newtonsoft.Json.Linq;
using System;

namespace Baidu.Meta.ComponentsTool.Editor
{
    public class ABEditorUtilities
    {
        // Clean up filenames so they are valid for the file system.
        static public string ReplaceInvalidFilenameChars(string filename)
        {
            string output = filename;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                string cstr = "" + c;
                output.Replace(cstr, "");
            }
            return output;
        }

        //-------------------
        // We can't get at the bits of the Hash128, annoyingly,
        // and we want to save a crapload of space in the manifest, so just use
        // the string of the Hash128 and generate a simple hash from the string.
        static public int HashOfHash128(Hash128 h)
        {
            string s = h.ToString();
            int r = 0;
            for (int i = 0; i < 32; i++)
            {
                char c = s[i];
                int cv = (int)(c - '0');  // parse the hex string
                if (cv > 9)
                    cv -= 39;  // handle 'a'-'f'
                r = r ^ (cv << i);  // blend/xor the original values into a single 32 bit value
            }
            return r;
        }

        //-------------------
        /// <summary>
        /// 平台名称字符串，win64,android,ios等
        /// </summary>
        /// <param name="bt"></param>
        /// <returns></returns>
        static public string GetPlatform(BuildTarget bt)
        {
            switch (bt)
            {
                case BuildTarget.StandaloneWindows:
                    return "win32";
                case BuildTarget.StandaloneWindows64:
                    return "win64";
                case BuildTarget.WSAPlayer:
                    return "winstore";
                case BuildTarget.StandaloneOSX:
                    return "osx";
                case BuildTarget.StandaloneLinux64:
                    return "linux64";
                case BuildTarget.Android:
                    return "android";
                case BuildTarget.iOS:
                    return "ios";
                case BuildTarget.tvOS:
                    return "tvos";
                case BuildTarget.WebGL:
                    return "webgl";
                case BuildTarget.PS4:
                    return "ps4";
                case BuildTarget.Switch:
                    return "switch";
                case BuildTarget.XboxOne:
                    return "xboxone";
                case BuildTarget.Lumin:
                    return "magicleap";
#if !UNITY_2019_2_OR_NEWER
					case BuildTarget.StandaloneLinux:
						return "linux32";
					case BuildTarget.StandaloneLinuxUniversal:
						return "linuxuniv";
#endif
#if UNITY_2019_3_OR_NEWER
                case BuildTarget.Stadia:
                    return "stadia";
#endif
#if UNITY_2019_4_OR_NEWER
                case BuildTarget.PS5:
                    return "ps5";
                //case BuildTarget.LinuxHeadlessSimulation:
                //    return "cloud";
                //case BuildTarget.GameCoreXboxOne:
                //    return "gamecorexboxone";
                //case BuildTarget.GameCoreXboxSeries:
                //    return "gamecorexboxseries";
#endif
            }
            Debug.Assert(false, "Requested platform is unknown: " + bt);
            return "unknown";
        }

        //-------------------
        // Here's how you get the list of scenes from BuildSettings window.
        static public void GetBuildSettingsSceneList(List<string> scenes)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                scenes.Add(path);
            }
        }

        //-------------------
        // Making a scriptable object requires specific steps.
        static public void CreateScriptableObjectAsset(ScriptableObject asset, string path)
        {
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
        }

        //-------------------
        // Easier to configure stuff if it's broken out in an obvious place like this.
        //static public string GetInstallFolder()
        //{
        //    // Allow user to move things around without having to change the code
        //    string[] autobuilderFolder = Directory.GetDirectories(Application.dataPath, "AutoAssetBundleBuilder", SearchOption.AllDirectories);

        //    // If you rename the folder, we can't find it.
        //    // You can change this to explicitly set where you want Resources to go.
        //    if (autobuilderFolder.Length == 0)
        //        return "/AutoAssetBundleBuilder";
        //    string installFolder = autobuilderFolder[0].Replace(Application.dataPath, "");
        //    installFolder = installFolder.Replace('\\', '/');
        //    return installFolder;
        //}

        /// <summary>
        /// 打包输出的根目录和Assets同级的BaseBundlesBuild/versionString目录
        /// </summary>
        /// <returns></returns>
        static public string GetBuildRootFolder(string versionString)
        {
            string buildRoot = Application.dataPath.Remove(Application.dataPath.LastIndexOf('/')) + "/ComponentCompiled/" + versionString + "/";
            return buildRoot;
        }

        /// <summary>
        /// 获取package包内Bundle文件夹
        /// </summary>
        /// <param name="_platformStr"></param>
        /// <returns></returns>
        static public string GetPackageOutputFolder(string _platformStr)
        {
            string platform = "";
            if (_platformStr=="win64")
            {
                platform = "Windows";
            }
            else if (_platformStr=="android")
            {
                platform = "Android";
            }
            else if (_platformStr=="ios")
            {
                platform = "iOS";
            }
            else if (_platformStr == "webgl")
            {
                platform = "WebGL";
            }
            else if (_platformStr == "vr")
            {
                platform = "VR";
            }

            string pluginsFolder= Application.dataPath.Remove(Application.dataPath.LastIndexOf('/')) +
                "/ComponentCompiled/CompiledPackage/Plugins";
            if (!Directory.Exists(pluginsFolder))
            {
                Directory.CreateDirectory(pluginsFolder);
            }
            string pluginsFolderMetaPath = $"{pluginsFolder}.meta";
            if (!File.Exists(pluginsFolderMetaPath))
                ABGenDllMeta.FolderMetaFileGen(pluginsFolderMetaPath);

            //生成各个平台文件夹的meta文件
            string platformFolder = pluginsFolder+"/" + platform;
            if (!Directory.Exists(platformFolder))
            {
                Directory.CreateDirectory(platformFolder);
            }
            //Plugin下各个平台的meta文件
            string platformFolderMetaPath = $"{platformFolder}.meta";
            if(!File.Exists(platformFolderMetaPath))
                ABGenDllMeta.FolderMetaFileGen(platformFolderMetaPath);

            string bundlesFolder= platformFolder + "/Bundles";
           
            if (!Directory.Exists(bundlesFolder))
            {
                Directory.CreateDirectory(bundlesFolder);
            }
            //bundles文件夹的meta文件
            string bundlesFolderMetaPath = $"{bundlesFolder}.meta";
            if (!File.Exists(bundlesFolderMetaPath))
                ABGenDllMeta.FolderMetaFileGen(bundlesFolderMetaPath);

            string buildRoot = bundlesFolder+ "/" + GetPackageName()+ GetBundleVersionFromPackageJson();
            return buildRoot;
        }

        /// <summary>
        /// Copy all folders and all files deeply.
        /// </summary>
        /// <param name="sourceFolder">Source folder path.</param>
        /// <param name="destFolder">Destination folder path.</param>
        public static void CopyFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(sourceFolder))
            {
                Debug.LogError($"SourceFolder: {sourceFolder} does not exist!");
                return;
            }

            try
            {
                if (!Directory.Exists(destFolder))
                {
                    Directory.CreateDirectory(destFolder);
                }
                //Get all files in top directory.
                string[] files = Directory.GetFiles(sourceFolder);
                foreach (string file in files)
                {
                    //Do not copy meta files and manifest files.
                    //if (file.EndsWith(".meta") || file.EndsWith(".manifest")) continue;
                    string name = Path.GetFileName(file);
                    string dest = Path.Combine(destFolder, name);
                    File.Copy(file, dest, true); //Copy the file.
                    //File.Move(file, dest); //Copy the file.
                }

                //Get all directories in top directory.
                string[] folders = Directory.GetDirectories(sourceFolder);
                foreach (string folder in folders)
                {
                    string name = Path.GetFileName(folder);
                    //Building the destination path.
                    string dest = Path.Combine(destFolder, name);
                    //Recursive copying.
                    CopyFolder(folder, dest);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// 获取文件夹包括子文件夹内所有文件
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        static public List<string> GetDirectoryFiles(string dirPath)
        {
            //判断给定的路径是否存在,如果不存在则退出
            if (!Directory.Exists(dirPath))
                return new List<string>();
            List<string> files=new List<string>();
            //定义一个DirectoryInfo对象
            DirectoryInfo di = new DirectoryInfo(dirPath);
            //通过GetFiles方法,获取di目录中的所有文件的大小
            foreach (FileInfo fi in di.GetFiles())
            {
                files.Add(fi.FullName);
            }
            //获取di中所有的文件夹,并存到一个新的对象数组中,以进行递归
            DirectoryInfo[] dirs = di.GetDirectories();
            if (dirs.Length > 0)
            {
                for (int i = 0; i < dirs.Length; i++)
                {
                    //文件夹路径也加上
                    files.Add(dirs[i].FullName);
                    files.AddRange(GetDirectoryFiles(dirs[i].FullName));
                }
            }
            return files;
        }

        /// <summary>
        /// config_platform.json 文件的路径，一般在Build文件夹下
        /// </summary>
        /// <param name="versionString"></param>
        /// <param name="platformStr"></param>
        /// <returns></returns>
        static public string GetConfigBuildFolder(string versionString, string platformStr)
        {
            string configPath = GetBuildRootFolder(versionString) + platformStr;
            return configPath;
        }

        /// <summary>
        /// bundle包内置到应用时的文件夹
        /// </summary>
        /// <returns></returns>
        static public string GetEmbedBundlesFolder()
        {
            string embedPath = "/bundles/";
            return embedPath;
        }

        /// <summary>
        /// Build文件夹下的资源文件夹，例如：com_baidu_meta_0.1.0_win64
        /// </summary>
        /// <param name="platformStr"></param>
        /// <returns></returns>
        static public string GetBundleBuildFolder(string versionString, string platformStr)
        {
            string bundlePath = GetBuildRootFolder(versionString) + platformStr + "/"+GetPackageName() + versionString +"_"+ platformStr;
            return bundlePath;
        }

        /// <summary>
        /// 获取package包名com_baidu_,带下划线以下划线结尾
        /// </summary>
        /// <returns></returns>
        static public string GetPackageName()
        {
            string pkgName = "";
            JObject jsonObj = GetPackageJson();
            if (jsonObj != null)
            {
                pkgName = jsonObj["name"].ToString();
                //Debug.Log($"package name: {pkgName}");
                pkgName =ABUtilities.MakePackageNameEasy(pkgName);
                //Debug.Log($"package name replaced: {pkgName}");
            }
            return pkgName;
        }

        /// <summary>
        /// 解析package.json获取版本号
        /// 使用了NewtonSoft.Json.dll
        /// </summary>
        /// <returns></returns>
        static public string GetBundleVersionFromPackageJson()
        {
            string version = "0.1.0";

            JObject jsonObj = GetPackageJson();
            if (jsonObj != null)
            {
                version = jsonObj["version"].ToString();
                //Debug.Log($"package version: {version}");
            }
            return version;
        }

        /// <summary>
        /// 从package.json中解析出json
        /// </summary>
        /// <returns></returns>
        static public JObject GetPackageJson()
        {
            string dataPath = Application.dataPath;
            DirectoryInfo directoryInfo = new DirectoryInfo(dataPath);
            FileInfo[] allFiles = directoryInfo.GetFiles("package.json", SearchOption.AllDirectories);
            if (allFiles == null || allFiles.Length == 0)
            {
                Debug.Log("not find package.json, will use default version: 0.1.0");
                return null;
            }
            if (allFiles.Length > 1)
            {
                Debug.Log("find more than one package.json, will use first one");
            }
            string filePath = allFiles[0].FullName;
            string jsonStr = File.ReadAllText(filePath, Encoding.UTF8);

            JObject jsonObj = JObject.Parse(jsonStr);
            return jsonObj;
        }

        /// <summary>
        /// 生成一个BuildConfig，配置文件主要在这里修改
        /// 主要设置sourceBundleFolder，需要打包bundle的文件夹
        /// </summary>
        /// <returns></returns>
        static public ABBuildConfig GetABBuildConfig()
        {
            ABBuildConfig config= ScriptableObject.CreateInstance<ABBuildConfig>();
            //注意这个地址在这里写无效，
            //会在打包的时候被重写为ABUtilities. GetResUrl(string _platform,bool _isBos)
            config.bootstrap = new ABBootstrap("Assets/Bundles");
            config.sourceBundleFolder = GetSourceBundleFolder();
            return config;
        }

        /// <summary>
        /// 需要打包Bundle的文件夹
        /// </summary>
        /// <returns></returns>
        static public string GetSourceBundleFolder()
        {
            return "/Src/Assets/";
        }

        /// <summary>
        /// 远程的bundle文件夹
        /// </summary>
        /// <param name="_platformStr"></param>
        /// <param name="_versionStr"></param>
        /// <returns></returns>
        static public string GetBootstrapUrl(string _platformStr,string _versionStr)
        {
            string bosBucketUrl = "https://zion-sdk-download.baidu-int.com/download/yuanbang/";
            string platFormfolder =GetPackageName() + _versionStr + "_{PLATFORM}/";
            return bosBucketUrl + platFormfolder;
        }

    }
}
