using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Networking;
using Application = UnityEngine.Application;

namespace Baidu.Meta.ComponentsTool.Editor
{
    public static class PackageTool
    {
        //[MenuItem("Components-Tool/生成Package")]
        public static void MakePackage()
        {
            Assembly2Dll.CompileAssembly();

            copySrc2Package();

            copyEditor2Package();

            copyProtocol2Package();

            copyRuntime2Package();

            copyRuntimeAnyPlatform2Package();

            //ABMenuItems.DoBuildAll();

            Debug.Log($"compiled package finish output {DirectoryUtil.PackagePath(false)}");
        }

        [MenuItem("Assets/Components-Tool/Publish Selected Package")]
        public static void PublishSelectedFolder()
        {
            string folder = Application.dataPath.Remove(Application.dataPath.LastIndexOf('/'));
            string fullPath = folder+"/" + AssetDatabase.GetAssetPath(Selection.activeInstanceID);
            if (!Directory.Exists(fullPath))
            {
                Debug.Log("Please select package folder");
                return;
            }
            PublishPackageInfolder(fullPath);
        }

        //Baidu.Meta.ComponentsTool.Editor.PackageTool.PublishPackage -pkgName=${PKGNAME}

        //[MenuItem("Components-Tool/发布Package")]
        public static void PublishPackage()
        {
            string packagePath = "";
            //string packagePath = DirectoryUtil.PackagePath(false);

            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("pkgName=", StringComparison.InvariantCulture))
                {
                    packagePath = arg.Remove(0, 8);
                }
            }
            Debug.Log(packagePath);
            if (packagePath=="none"||string.IsNullOrEmpty(packagePath))
            {
                Debug.Log($"Package path is none or empty, return");
                return;
            }

            string folder = Application.dataPath.Remove(Application.dataPath.LastIndexOf('/'))+ "/Packages/";

            string[] pkgs = Directory.GetDirectories(folder);
            foreach (var item in pkgs)
            {
                string pkgFolderName = Path.GetFileName(item);
                if (pkgFolderName==packagePath)
                {
                    Debug.Log($"Publish Package: {pkgFolderName}");
                    PackagePublish.NpmPublish(item);
                }
            }
            
        }

        [MenuItem("Components-Tool/选择并发布Package")]
        public static void ChosePublishPackage()
        {
            string folder = Application.dataPath.Remove(Application.dataPath.LastIndexOf('/'));
            string folderName = Path.GetFileName(folder);
            string path = EditorUtility.OpenFolderPanel("窗口标题", folder, folderName);
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("no folder selected");
                return;
            }
            PublishPackageInfolder(path);
        }

        static void PublishPackageInfolder(string folderPath)
        { 
            string packageName = GetPackageName(folderPath);
            string packageVersion = GetPackageVersion(folderPath);
            if (packageVersion=="0.0.0")
            {
                EditorUtility.DisplayDialog("错误", "请选择package.json所在的文件夹", "OK", "Cancel");
                return;
            }
            Debug.Log(packageName);
            Debug.Log(packageVersion);
            string url = $"http://vrfe.baidu-int.com/npm/{packageName}/";
            var request = UnityWebRequest.Get(url);

            request.SendWebRequest();

            while (!request.isDone && !request.isHttpError && !request.isNetworkError)
            {
            }

            if (request.isHttpError || request.isNetworkError)
            {
                if (request.error.Contains("Not Found"))
                {
                    Debug.Log("Package remote not exist publish directly");
                    PackagePublish.NpmPublish(folderPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "无法获取远程版本号", "OK", "Cancel");
                    Debug.Log("Couldn't finish opening request!");
                    return;
                }
            }
            else
            {
                string result = request.downloadHandler.text;
                Debug.Log("Received: " + result);

                JObject jsonObj = JObject.Parse(result);
                if (jsonObj.ContainsKey("error"))
                {
                    Debug.Log(jsonObj["error"]);
                }
                else
                {
                    //正式包
                    List<Version> stdVersions = new List<Version>();
                    //预发布包
                    List<Version> preVersions = new List<Version>();
                    List<Version> previewVersions = new List<Version>();
                    //体验包
                    List<Version> expVersions = new List<Version>();

                    Dictionary<string, List<Version>> otherVersionDic = new Dictionary<string, List<Version>>();

                    IDictionary<string, JToken> rates = (JObject)jsonObj["versions"];
                    foreach (var item in rates)
                    {
                        if (item.Key.Contains("-pre"))
                        {
                            preVersions.Add(new Version(item.Key.Replace("-pre", "")));
                        }
                        else if (item.Key.Contains("-preview"))
                        {
                            previewVersions.Add(new Version(item.Key.Replace("-preview", "")));
                        }
                        else if (item.Key.Contains("-exp"))
                        {
                            expVersions.Add(new Version(item.Key.Replace("-exp", "")));
                        }
                        else if (item.Key.Contains("-"))
                        {
                            string regst = "\\-.*?(?=\\.)";
                            Regex rg = new Regex(regst, RegexOptions.IgnoreCase);
                            string dicKey=  rg.Match(item.Key).Value;       //"1.0.0-Pico.1" 截取出 -Pico;

                            if (otherVersionDic.ContainsKey(dicKey))
                            {
                                otherVersionDic[dicKey].Add(new Version(item.Key.Replace(dicKey, "")));
                            }
                            else
                            {
                                otherVersionDic[dicKey] = new List<Version>();
                                otherVersionDic[dicKey].Add(new Version(item.Key.Replace(dicKey, "")));
                            }
                        }
                        else
                        {
                            try
                            {
                                stdVersions.Add(new Version(item.Key));
                            }
                            catch (Exception ex)
                            {
                                UnityEngine.Debug.Log("gcj: parase packages caught error: "+ex.Message);
                            }
                        }
                    }
                    stdVersions.Sort();
                    preVersions.Sort();
                    previewVersions.Sort();
                    expVersions.Sort();
                    foreach (var item in otherVersionDic)
                    {
                        item.Value.Sort();
                    }
                    //使用正确的版本号发布
                    List<Version> versions = new List<Version>();
                    string localVersionStr = "";
                    if (packageVersion.Contains("-pre"))
                    {
                        localVersionStr = packageVersion.Replace("-pre", "");
                        versions=preVersions;
                    }
                    else if (packageVersion.Contains("-preview"))
                    {
                        localVersionStr = packageVersion.Replace("-preview", "");
                        versions = previewVersions;
                    }
                    else if (packageVersion.Contains("-exp"))
                    {
                        localVersionStr = packageVersion.Replace("-exp", "");
                        versions = expVersions;
                    }
                    else if (packageVersion.Contains("-"))
                    {
                        string regst = "\\-.*?(?=\\.)";
                        Regex rg = new Regex(regst, RegexOptions.IgnoreCase);
                        string dicKey = rg.Match(packageVersion).Value;       //"1.0.0-Pico.1" 截取出 -Pico;

                        localVersionStr = packageVersion.Replace(dicKey, "");
                        versions = otherVersionDic[dicKey];

                    }
                    else
                    {
                        localVersionStr = packageVersion;
                        versions = stdVersions;
                    }

                    if (versions.Count > 0)
                    {
                        Version newestVersion=versions[versions.Count - 1];
                        Version localVersion = new Version(localVersionStr);
                        if (localVersion<= newestVersion)
                        {
                            EditorUtility.DisplayDialog("错误", "相同版本号Package已发布, 请提升版本号再发布", "OK", "Cancel");
                        }
                        else
                        {
                            PackagePublish.NpmPublish(folderPath);
                        }
                    }
                    else
                    {
                        PackagePublish.NpmPublish(folderPath);
                        //EditorUtility.DisplayDialog("错误", "未找到合适版本号", "OK", "Cancel");
                    }
                }
            }
        }

        /// <summary>
        /// 获取package包名
        /// </summary>
        /// <returns></returns>
        static public string GetPackageName(string packagePath)
        {
            string pkgName = "";
            JObject jsonObj = GetPackageJson(packagePath);
            if (jsonObj != null)
            {
                pkgName = jsonObj["name"].ToString();
            }
            return pkgName;
        }

        /// <summary>
        /// 解析package.json获取版本号
        /// 使用了NewtonSoft.Json.dll
        /// </summary>
        /// <returns></returns>
        static public string GetPackageVersion(string packagePath)
        {
            string version = "0.0.0";

            JObject jsonObj = GetPackageJson(packagePath);
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
        static public JObject GetPackageJson(string packagePath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(packagePath);
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

        //static IEnumerator getDatas(string packageName)
        //{
        //    string url = $"http://vrfe.baidu-int.com/npm/{packageName}/";
        //    UnityEngine.Networking.UnityWebRequest uwr = UnityEngine.Networking.UnityWebRequest.Get(url);
        //    yield return uwr.SendWebRequest();
        //    if (uwr.isNetworkError)
        //    {
        //        Debug.Log("Error While Sending: " + uwr.error);
        //    }
        //    else
        //    {
        //        string result = uwr.downloadHandler.text;
        //        Debug.Log("Received: " + result);

        //        JObject jsonObj = JObject.Parse(result);
        //        if (jsonObj.ContainsKey("error"))
        //        {
        //            Debug.Log(jsonObj["error"]);
        //        }
        //        else
        //        {
        //            List<Version> versions = new List<Version>();
        //            IDictionary<string, JToken> rates = (JObject)jsonObj["versions"];
        //            foreach (var item in rates)
        //            {
        //                Debug.Log(item.Key);
        //                versions.Add(new Version(item.Key));
        //            }
        //            versions.Sort();
        //            if (versions.Count > 0)
        //                Debug.Log(versions[versions.Count - 1]);
        //        }
        //    }
        //}


        public static void copySrc2Package()
        {
            string srcDirectory = DirectoryUtil.AssetSrcPath();
            string desDirectory = DirectoryUtil.PackagePath(true);

            CopyDirectory(srcDirectory, desDirectory, true);

            copySrcEditorPlugins2Package();

            Debug.Log($"compiled package step : copySrc2Package finish");
        }

        public static void copySrcEditorPlugins2Package()
        {
            string srcDirectory = DirectoryUtil.AssetSrcPath();
            string EditorPluginsPath = Path.Combine(srcDirectory, "Editor", "Plugins");

            if (Directory.Exists(EditorPluginsPath))
            {
                string EditorPluginsMetaPath = Path.Combine(srcDirectory, "Editor", "Plugins.meta");
                if (File.Exists(EditorPluginsMetaPath))
                {
                    string packageEditorPath = DirectoryUtil.PackageEditorPath();
                    File.Copy(EditorPluginsMetaPath, Path.Combine(packageEditorPath, "Plugins.meta"), true);
                }

                string desDirectory = DirectoryUtil.PackageEditorPluginsPath();

                CopyDirectoryCommon(EditorPluginsPath, desDirectory, true);
            }
        }

        private static void CopyDirectory(string sourcePath, string destPath, bool isRoot)
        {
            string floderName = Path.GetFileName(sourcePath);
            DirectoryInfo di = isRoot ? new DirectoryInfo(destPath) : Directory.CreateDirectory(Path.Combine(destPath, floderName));
            string[] files = Directory.GetFileSystemEntries(sourcePath);

            foreach (string file in files)
            {
                if (Directory.Exists(file))
                {
                    string subFloderName = Path.GetFileName(file);
                    bool ignoreCopy = isRoot && (subFloderName == "Runtime" || subFloderName == "Protocol" || subFloderName == "Editor" || subFloderName == "Assets");
                    if (!ignoreCopy)
                    {
                        CopyDirectory(file, di.FullName, false);
                    }
                }
                else
                {
                    string fileName = Path.GetFileName(file);
                    bool ignoreCopy = isRoot && (fileName == "Runtime.meta" || fileName == "Assets.meta");
                    if (!ignoreCopy)
                    {
                        File.Copy(file, Path.Combine(di.FullName, Path.GetFileName(file)), true);
                        Debug.Log($"copy file {file} -> {di.FullName}");
                    }
                }
            }
        }

        private static void CopyDirectoryCommon(string sourcePath, string destPath, bool isRoot)
        {
            string floderName = Path.GetFileName(sourcePath);
            DirectoryInfo di = isRoot ? new DirectoryInfo(destPath) : Directory.CreateDirectory(Path.Combine(destPath, floderName));
            string[] files = Directory.GetFileSystemEntries(sourcePath);

            foreach (string file in files)
            {
                if (Directory.Exists(file))
                {
                    string subFloderName = Path.GetFileName(file);
                    CopyDirectory(file, di.FullName, false);
                }
                else
                {
                    string fileName = Path.GetFileName(file);
                    File.Copy(file, Path.Combine(di.FullName, Path.GetFileName(file)), true);
                    Debug.Log($"copy file {file} -> {di.FullName}");
                }
            }
        }

        public static void copyEditor2Package()
        {
            // 1 copy dll
            string srcDllPath = DirectoryUtil.EditorDllPath();

            if (!File.Exists(srcDllPath))
            {
                Debug.Log($"copyEditor2Package file not exist: {srcDllPath}");
                return;
            }

            string packageEditorPath = DirectoryUtil.PackageEditorPath();
            string dllName = AssemblyNameUtil.EditorDllName();

            string desDllPath = Path.Combine(packageEditorPath, dllName);
            File.Copy(srcDllPath, desDllPath, true);

            // 2 生成对应的meta

            string metaDesPath = Path.Combine(packageEditorPath, $"{dllName}.meta");
            GenDllMeta.EditorMetaFileGen(metaDesPath);

            // 3 copy pdb

            String srcPdbPath = System.IO.Path.ChangeExtension(srcDllPath, "pdb");
            if (File.Exists(srcPdbPath))
            {
                String desPdbPath = System.IO.Path.ChangeExtension(desDllPath, "pdb");
                File.Copy(srcPdbPath, desPdbPath, true);

                string PdbMetaDesPath = $"{desPdbPath}.meta";
                GenDllMeta.FileMetaFileGen(PdbMetaDesPath);
            }

            Debug.Log($"compiled package step : copyEditor2Package finish");
        }

        public static void copyProtocol2Package()
        {
            // 1 copy dll
            string srcDllPath = DirectoryUtil.ProtocolDllPath();

            if (!File.Exists(srcDllPath))
            {
                Debug.Log($"copyProtocol2Package file not exist: {srcDllPath}");
                return;
            }

            string packageProtocolPath = DirectoryUtil.PackageProtocolPath();
            string dllName = AssemblyNameUtil.ProtocolDllName();

            string desDllPath = Path.Combine(packageProtocolPath, dllName);

            File.Copy(srcDllPath, desDllPath, true);

            // 2 生成对应的meta

            string metaDesPath = Path.Combine(packageProtocolPath, $"{dllName}.meta");
            GenDllMeta.AnyMetaFileGen(metaDesPath);

            // 3 copy pdb

            String srcPdbPath = System.IO.Path.ChangeExtension(srcDllPath, "pdb");
            if (File.Exists(srcPdbPath))
            {
                String desPdbPath = System.IO.Path.ChangeExtension(desDllPath, "pdb");
                File.Copy(srcPdbPath, desPdbPath, true);

                string PdbMetaDesPath = $"{desPdbPath}.meta";
                GenDllMeta.FileMetaFileGen(PdbMetaDesPath);
            }

            Debug.Log($"compiled package step : copyProtocol2Package finish");
        }

        public static void copyRuntime2Package()
        {
            List<BuildTarget> targetList = new List<BuildTarget>
        {
            BuildTarget.Android,
            BuildTarget.iOS,
            BuildTarget.StandaloneWindows64,
            BuildTarget.WebGL,
        };

            bool hasPlugins = false;

            for (int i = 0; i < targetList.Count; i++)
            {
                BuildTarget target = targetList[i];

                // 1 copy dll
                string srcDllPath = DirectoryUtil.RuntimeDllPath(target);

                if (!File.Exists(srcDllPath))
                {
                    Debug.Log($"copyRuntime2Package file not exist: {srcDllPath} {target}");
                    continue;
                }

                hasPlugins = true;

                string packageRuntimePath = DirectoryUtil.PackageRuntimePath(target);
                string dllName = AssemblyNameUtil.RuntimeDllName();

                string desDllPath = Path.Combine(packageRuntimePath, dllName);

                File.Copy(srcDllPath, desDllPath, true);

                // 2 生成对应的meta

                string metaDesPath = Path.Combine(packageRuntimePath, $"{dllName}.meta");
                GenDllMeta.TargetMetaFileGen(metaDesPath, target);

                // 3 copy pdb
                String srcPdbPath = System.IO.Path.ChangeExtension(srcDllPath, "pdb");
                if (File.Exists(srcPdbPath))
                {
                    String desPdbPath = System.IO.Path.ChangeExtension(desDllPath, "pdb");
                    File.Copy(srcPdbPath, desPdbPath, true);

                    string PdbMetaDesPath = $"{desPdbPath}.meta";
                    GenDllMeta.FileMetaFileGen(PdbMetaDesPath);
                }

                string FolderMetaDesPath = $"{packageRuntimePath}.meta";
                if (!File.Exists(FolderMetaDesPath))
                {
                    GenDllMeta.FolderMetaFileGen(FolderMetaDesPath);
                }
            }

            if (hasPlugins)
            {
                string packagePluginsPath = DirectoryUtil.PackageRuntimeAnyPlatformPath();
                string FolderMetaDesPath = $"{packagePluginsPath}.meta";
                if (!File.Exists(FolderMetaDesPath))
                {
                    GenDllMeta.FolderMetaFileGen(FolderMetaDesPath);
                }
            }

            Debug.Log($"compiled package step : copyRuntime2Package finish");
        }

        public static void copyRuntimeAnyPlatform2Package()
        {
            // 1 copy dll
            string srcDllPath = DirectoryUtil.RuntimeAnyPlatformDllPath();

            if (!File.Exists(srcDllPath))
            {
                Debug.Log($"copyRuntimeAnyPlatform2Package file not exist: {srcDllPath}");
                return;
            }

            string packageRuntimeAnyPath = DirectoryUtil.PackageRuntimeAnyPlatformPath();
            string dllName = AssemblyNameUtil.RuntimeAnyPlatformDllName();

            string desDllPath = Path.Combine(packageRuntimeAnyPath, dllName);

            File.Copy(srcDllPath, desDllPath, true);

            // 2 生成对应的meta

            string metaDesPath = Path.Combine(packageRuntimeAnyPath, $"{dllName}.meta");
            GenDllMeta.AnyMetaFileGen(metaDesPath);

            // 3 copy pdb
            String srcPdbPath = System.IO.Path.ChangeExtension(srcDllPath, "pdb");
            if (File.Exists(srcPdbPath))
            {
                String desPdbPath = System.IO.Path.ChangeExtension(desDllPath, "pdb");
                File.Copy(srcPdbPath, desPdbPath, true);

                string PdbMetaDesPath = $"{desPdbPath}.meta";
                GenDllMeta.FileMetaFileGen(PdbMetaDesPath);
            }

            string FolderMetaDesPath = $"{packageRuntimeAnyPath}.meta";
            if (!File.Exists(FolderMetaDesPath))
            {
                GenDllMeta.FolderMetaFileGen(FolderMetaDesPath);
            }

            Debug.Log($"compiled package step : copyRuntimeAnyPlatform2Package finish");
        }

    }
}

