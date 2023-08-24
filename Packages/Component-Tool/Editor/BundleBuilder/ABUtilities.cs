using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Baidu.Meta.ComponentsTool.Editor
{
    /// <summary>
    /// 可以运行时使用的工具类
    /// </summary>
    public class ABUtilities
    {
        public string GetLatestVersionString()
        {
            string currentVersion;
            if (!PlayerPrefs.HasKey("baseBundleVer"))
                currentVersion = "0.0.0";
            else
                currentVersion = PlayerPrefs.GetString("baseBundleVer");
            
            return currentVersion;
        }

        static public string GetFileMD5(string filepath)
        {
            var filestream = new FileStream(filepath, System.IO.FileMode.Open);
            if (filestream == null)
            {
                string V = "";
                return V;
            }
            MD5 md5 = MD5.Create();
            var fileMD5Bytes = md5.ComputeHash(filestream);
            filestream.Close();
            string filemd5 = System.BitConverter.ToString(fileMD5Bytes).Replace("-", "").ToLower();
            return filemd5;
        }

        //-------------------
        // This exists because of the horrible Byte-Order-Mark issue where the first three bytes of 
        // a file MAY contain indicators for little/big endian and UTF8/16 encodings.
        // We only try to handle UTF8 for now.
        static public string GetStringFromUTF8File(byte[] data)
        {
            // UTF8
            if (data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            {
                // BOM, skip them
                return Encoding.UTF8.GetString(data, 3, data.Length - 3);
            }
            // assume no BOM
            return Encoding.UTF8.GetString(data, 0, data.Length);
        }

        //-------------------
        /// <summary>
        /// 返回Application.persistentDataPath + "/Bundles/"+ versionString+"/"
        /// </summary>
        /// <param name="versionString"></param>
        /// <returns></returns>
        static public string GetRuntimeCacheFolder(string versionString,string appVersionStr="")
        {
            if (string.IsNullOrEmpty(appVersionStr))
            {
                appVersionStr = Application.version;
            }
            string cachePath = Application.persistentDataPath + "/Bundles/"+ appVersionStr+"/" + versionString + "/";
            if (!Directory.Exists(cachePath))
                Directory.CreateDirectory(cachePath);
            return cachePath;
        }

        /// <summary>
        /// 刪除其他版本的Bundles包文件夾
        /// </summary>
        /// <param name="_appVersionStr"></param>
        /// <returns></returns>
        static public void DeleteOtherVersionAppFolder(string _appVersionStr)
        {
            string cachePath = Application.persistentDataPath + "/Bundles/";
            if (!Directory.Exists(cachePath))
            {
                Debug.Log("dir not exist: " + cachePath);
                Directory.CreateDirectory(cachePath);
                return;
            }
            else
            {
                string[] dirs = Directory.GetDirectories(cachePath);
                for (int i = 0, cnt = dirs.Length; i < cnt; i++)
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(dirs[i]);
                    if (directoryInfo.Name != _appVersionStr)
                    {
                        Debug.Log("DeleteOtherVersionAppFolder:Delete folder: " + dirs[i]);
                        Directory.Delete(dirs[i], true);
                    }
                }
            }
        }

        /// <summary>
        /// 返回Application.persistentDataPath + "/Bundles/"文件夹下
        /// 最新的一个版本号文件夹的名称,0.1.2这三组数都不能大于9，否则这个方法无效
        /// </summary>
        /// <returns></returns>
        string GetLatestVersionFolder()
        {
            string cachePath = Application.persistentDataPath + "/Bundles/";
            if (!Directory.Exists(cachePath))
            {
                return "";
            }
            else
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(cachePath);
                DirectoryInfo[] dirs = directoryInfo.GetDirectories();
                if (dirs.Length == 0)
                {
                    return "";
                }
                else if (dirs.Length == 1)
                {
                    return dirs[0].Name;
                }
                else
                {
                    List<int> nums = new List<int>();
                    for (int i = 0, cnt = dirs.Length; i < cnt; i++)
                    {
                        int num = int.Parse(dirs[i].Name.Replace(".", ""));
                        nums.Add(num);
                    }
                    nums.Sort();
                    int lastNum = nums[nums.Count - 1];
                    if (lastNum < 10)
                        return string.Format("0.0.{0}", lastNum);
                    else if (lastNum < 100)
                    {
                        return string.Format("0.{0}.{1}", (int)(lastNum / 10.0), lastNum % 10);
                    }
                    else
                    {
                        return string.Format("{0}.{1}.{2}", (int)(lastNum / 100.0), (int)(lastNum / 10.0 % 10), (int)(lastNum % 100 % 10.0));
                    }
                }
            }
        }

        /// <summary>
        /// 根据平台返回win64,ios,android或webgl中一个
        /// </summary>
        /// <returns></returns>
        static public string GetRuntimePlatform()
        {
            string platfomStr = "";
            //开始下载最新版本号
            if (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.OSXEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                platfomStr = "win64";
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                platfomStr = "android";
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                platfomStr = "ios";
            }
            else if (Application.platform==RuntimePlatform.WebGLPlayer)
            {
                platfomStr = "webgl";
            }
            return platfomStr;
        }

        /// <summary>
        /// 远程服务器的子文件夹，也就是放版本号文件夹的资源文件夹:BaseBundlesBuild
        /// </summary>
        /// <returns></returns>
        static public string GetRuntimeSubFolder()
        {
            return "BaseBundlesBuild";
        }

        /// <summary>
        /// 根据运行平台返回对应的资源地址
        /// _isBos=true代表百度Bos平台
        /// </summary>
        /// <param name="_isBos">是否为Bos平台</param>
        /// <returns></returns>
        static public string GetResUrl(bool _isBos)
        {
            string tmpPlat = GetRuntimePlatform();
           return GetResUrl(tmpPlat, _isBos);
        }
        /// <summary>
        /// 获取资源下载URL
        /// _isBos=true 就是生产环境，cdn地址为bos
        /// </summary>
        /// <param name="_platform">win64,ios,android</param>
        /// <param name="_isBos">是否为线上生产环境</param>
        /// <returns></returns>
        static public string GetResUrl(string _platform,bool _isBos)
        {
            string url = "";
            if (_isBos)
            {
                //拼接后的地址: https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/ios/AssertBundle/baidu/unity/ios/config_win64.txt

                if (_platform == "android")
                {
                    url = "https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/android/";
                }
                else if (_platform == "ios")
                {
                    url = "https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/ios/";
                }
                else
                {
                    //win64
                    url = "https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/windows/";
                }
            }
            else
            {
                url = "https://zion-sdk-download.baidu-int.com/download/yuanbang/";
            }

            return url;
        }

        //-------------------

        /// <summary>
        ///On windows, this places the runtime asset bundle cache in the folder:
        /// C:\Users\[username]\AppData\LocalLow\[YourCompany]\[YourGame]\Bundles\Versions\
        ///You can make it whatever you want, so long as it's relative to Application.persistentDataPath.
        /// </summary>
        /// <param name="versionString"></param>
        static public void ConfigureCache(string versionString)
        {
            // Configure the cache so we can use it
            string cachePath = GetRuntimeCacheFolder(versionString);
            if (Directory.Exists(cachePath) == false)
                Directory.CreateDirectory(cachePath);

            Cache cache = Caching.GetCacheByPath(cachePath);
            if (!cache.valid)
            {
                cache = Caching.AddCache(cachePath);
                while (cache.ready == false)
                {
                }
            }
            if (!cache.valid) Debug.LogError("<color=#ff8080>Cache is NOT valid at " + cachePath + "</color>");
            if (!cache.ready) Debug.LogError("<color=#ff8080>Cache is NOT ready at " + cachePath + "</color>");
            if (cache.readOnly) Debug.LogError("<color=#ff8080>Cache is read-only at " + cachePath + "</color>");
            // we assume the currentCacheForWriting is the context for caching from now on.
            Caching.currentCacheForWriting = cache;
        }

        //-------------------
        // Android is a really, really picky environment when it comes to loading files from /StreamingAssets/
        static public string RemoveDoubleSlashes(string url)
        {
            int schemeIndex = url.IndexOf("://");
            if (schemeIndex != -1)
            {
                string scheme = url.Substring(0, schemeIndex + 3);
                string remainder = url.Substring(schemeIndex + 3);
                string result = scheme + remainder.Replace("//", "/");
                return result;
            }
            else  // very simple, no scheme to worry about, so no place there SHOULD be double slashes we need to preserve.
            {
                return url.Replace("//", "/");
            }
        }

        /// <summary>
        /// 把包名.换成_
        /// </summary>
        /// <param name="_packageName"></param>
        /// <returns></returns>
        static public string MakePackageNameEasy(string _packageName)
        {
            return (_packageName.Replace(".", "_") + "_").ToLowerInvariant();
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
                    Directory.CreateDirectory(destFolder);

                //Get all files in top directory.
                string[] files = Directory.GetFiles(sourceFolder);
                foreach (string file in files)
                {
                    //Do not copy meta files and manifest files.
                    //if (file.EndsWith(".meta") || file.EndsWith(".manifest")) continue;
                    string name = Path.GetFileName(file);
                    string dest = Path.Combine(destFolder, name);
                    File.Copy(file, dest, true); //Copy the file.
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
        /// Copy file.
        /// </summary>
        /// <param name="sourceFile">Source file path.</param>
        /// <param name="destFile">Destination file path.</param>
        public static void CopyFile(string sourceFile, string destFile)
        {
            if (string.IsNullOrEmpty(sourceFile) || string.IsNullOrEmpty(destFile))
            {
                Debug.LogError($"File name IsNullOrEmpty, sourceFile: {sourceFile}, destFile: {destFile}.");
                return;
            }

            try
            {
                if (!File.Exists(sourceFile))
                {
                    Debug.LogError($"Source file does not exists, sourceFile: {sourceFile}");
                    return;
                }

                string directoryName = Path.GetDirectoryName(destFile);
                if (string.IsNullOrEmpty(directoryName))
                {
                    Debug.LogError($"Destination directory name IsNullOrEmpty, destFile: {destFile}");
                    return;
                }

                if (!Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);
                File.Copy(sourceFile, destFile, true); //Copy the file.
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public static string GetFileHash(string filePath, MD5 md5 = null, StringBuilder sb = null)
        {
            byte[] hashCode;
            try
            {
                FileStream fileStream = new FileStream(filePath, FileMode.Open);
                if (md5 == null)
                    md5 = new MD5CryptoServiceProvider();
                hashCode = md5.ComputeHash(fileStream);
                fileStream.Close();
            }
            catch (Exception e)
            {
                Debug.LogError("ComputeHash failed! filePath: " + filePath);
                Debug.LogError(e);
                return null;
            }
            if (sb == null)
                sb = new StringBuilder();
            sb.Clear();
            foreach (var b in hashCode)
                sb.Append(b.ToString("x2"));
            string hash = sb.ToString();
            sb.Clear();
            return hash;
        }
    }
}
