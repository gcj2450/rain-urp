using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GridBrushBase;

namespace Baidu.Meta.ComponentsTool.Editor
{
    public static class DirectoryUtil
    {
        public static string OutputPath()
        {
            string path = Directory.GetParent(Application.dataPath).ToString();

            string outputDirectory = path + "/ComponentCompiled/";

            return outputDirectory;
        }

        public static string DllOutputAssemblyPath()
        {
            string dllPath = Path.Combine(OutputPath(), "Assembly2Dll");

            if (!Directory.Exists(dllPath))
            {
                Directory.CreateDirectory(dllPath);
            }

            return dllPath;
        }

        public static string DllOutputPath(BuildTarget target)
        {
            string dllPath = Path.Combine(OutputPath(), "Assembly2Dll", target.ToString());

            if (!Directory.Exists(dllPath))
            {
                Directory.CreateDirectory(dllPath);
            }

            return dllPath;
        }

        public static string PackagePath(bool needDeleteIfExist)
        {
            string packagePath = Path.Combine(OutputPath(), "CompiledPackage");

            if (Directory.Exists(packagePath) && needDeleteIfExist)
            {
                Directory.Delete(packagePath, true);
            }

            if (!Directory.Exists(packagePath))
            {
                Directory.CreateDirectory(packagePath);
            }

            return packagePath;
        }

        public static string PackageEditorPath()
        {
            string packagePath = PackagePath(false);

            string editorPath = Path.Combine(packagePath, "Editor");

            if (!Directory.Exists(editorPath))
            {
                Directory.CreateDirectory(editorPath);
            }

            return editorPath;
        }

        public static string PackageEditorPluginsPath()
        {
            string packagePath = PackagePath(false);

            string editorPluginsPath = Path.Combine(packagePath, "Editor", "Plugins");

            if (!Directory.Exists(editorPluginsPath))
            {
                Directory.CreateDirectory(editorPluginsPath);
            }

            return editorPluginsPath;
        }

        public static string PackageProtocolPath()
        {
            string packagePath = PackagePath(false);

            string protocolPath = Path.Combine(packagePath, "Protocol");

            if (!Directory.Exists(protocolPath))
            {
                Directory.CreateDirectory(protocolPath);
            }

            return protocolPath;
        }

        public static string PackageRuntimePath(BuildTarget target)
        {
            string packagePath = PackagePath(false);

            string subFolder = "Windows";

            switch (target)
            {
                case BuildTarget.Android:
                    subFolder = "Android";
                    break;
                case BuildTarget.iOS:
                    subFolder = "iOS";
                    break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    subFolder = "Windows";
                    break;
                case BuildTarget.WebGL:
                    subFolder = "WebGL";
                    break;
            }

            string runtimePath = Path.Combine(packagePath, "Plugins", subFolder);

            if (!Directory.Exists(runtimePath))
            {
                Directory.CreateDirectory(runtimePath);
            }

            return runtimePath;
        }

        public static string PackageRuntimeAnyPlatformPath()
        {
            string packagePath = PackagePath(false);

            string runtimeAnyPlatformPath = Path.Combine(packagePath, "Plugins");

            if (!Directory.Exists(runtimeAnyPlatformPath))
            {
                Directory.CreateDirectory(runtimeAnyPlatformPath);
            }

            return runtimeAnyPlatformPath;
        }

        public static string AssetSrcPath()
        {
            string path = Directory.GetParent(Application.dataPath).ToString();

            string srcDirectory = Path.Combine(path, "Assets", "Src");

            return srcDirectory;
        }

        public static string EditorDllPath()
        {
            string rootPath = Directory.GetParent(Application.dataPath).ToString();
            string folderPath = Path.Combine(rootPath, "Library", "ScriptAssemblies");
            string filePath = Path.Combine(folderPath, AssemblyNameUtil.EditorDllName());

            return filePath;
        }

        public static string ProtocolDllPath()
        {
            string rootPath = Directory.GetParent(Application.dataPath).ToString();
            string folderPath = Path.Combine(rootPath, "Library", "ScriptAssemblies");
            string filePath = Path.Combine(folderPath, AssemblyNameUtil.ProtocolDllName());

            return filePath;
        }

        public static string RuntimeDllPath(BuildTarget target)
        {
            string folderPath = DirectoryUtil.DllOutputPath(target);
            string filePath = Path.Combine(folderPath, AssemblyNameUtil.RuntimeDllName());

            return filePath;
        }

        public static string RuntimeAnyPlatformDllPath()
        {
            string rootPath = Directory.GetParent(Application.dataPath).ToString();
            string folderPath = Path.Combine(rootPath, "Library", "ScriptAssemblies");
            string filePath = Path.Combine(folderPath, AssemblyNameUtil.RuntimeAnyPlatformDllName());

            return filePath;
        }

    }
}
