using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Baidu.Meta.ComponentsTool.Editor
{
    public static class AssemblyNameUtil
    {
        public static string PrefixName()
        {
            string path = Directory.GetParent(Application.dataPath).ToString();

            string scrDirectory = DirectoryUtil.AssetSrcPath();

            // 根据文档，Editor、Runtime、Protocol里面的的程序集名字前缀一样
            /*
              ├── Editor
              │   ├── [company-name].[package-name].Editor.asmdef
              │   └── EditorExample.cs
              ├── Runtime
              │   ├── [company-name].[package-name].Runtime.asmdef
              │   └── RuntimeExample.cs
              ├── Protocol
              │   ├── [company-name].[package-name].Protocol.asmdef
              │   └── InterfaceExample.cs
              │   └── EventExample.cs 
             */

            string name = SearchName(Path.Combine(scrDirectory, "Editor"), ".Editor.asmdef");

            if (name != null)
            {
                return name;
            }

            name = SearchName(Path.Combine(scrDirectory, "Runtime"), ".Runtime.asmdef");

            if (name != null)
            {
                return name;
            }

            name = SearchName(Path.Combine(scrDirectory, "Protocol"), ".Protocol.asmdef");

            if (name != null)
            {
                return name;
            }

            return null;
        }

        public static string EditorPrefixName()
        {
            string path = Directory.GetParent(Application.dataPath).ToString();

            string scrDirectory = DirectoryUtil.AssetSrcPath();

            /*
              ├── Editor
              │   ├── [company-name].[package-name].Editor.asmdef
              │   └── EditorExample.cs
             */

            string name = SearchName(Path.Combine(scrDirectory, "Editor"), ".Editor.asmdef");

            if (name != null)
            {
                return name;
            }

            return null;
        }

        public static string RuntimePrefixName()
        {
            string path = Directory.GetParent(Application.dataPath).ToString();

            string scrDirectory = DirectoryUtil.AssetSrcPath();

            /*
              ├── Runtime
              │   ├── [company-name].[package-name].Runtime.asmdef
              │   └── RuntimeExample.cs
             */

            string name = SearchName(Path.Combine(scrDirectory, "Runtime"), ".Runtime.asmdef");

            if (name != null)
            {
                return name;
            }

            return null;
        }

        public static string RuntimeAnyPlatformPrefixName()
        {
            string path = Directory.GetParent(Application.dataPath).ToString();

            string scrDirectory = DirectoryUtil.AssetSrcPath();

            /*
              ├── Runtime
              │   ├── [company-name].[package-name].Runtime.AnyPlatform.asmdef
              │   └── RuntimeExample.cs
             */

            string name = SearchName(Path.Combine(scrDirectory, "Runtime"), ".Runtime.AnyPlatform.asmdef");

            if (name != null)
            {
                return name;
            }

            return null;
        }

        public static string ProtocolPrefixName()
        {
            string path = Directory.GetParent(Application.dataPath).ToString();

            string scrDirectory = DirectoryUtil.AssetSrcPath();

            /*
              ├── Protocol
              │   ├── [company-name].[package-name].Protocol.asmdef
              │   └── InterfaceExample.cs
              │   └── EventExample.cs 
             */

            string name = SearchName(Path.Combine(scrDirectory, "Protocol"), ".Protocol.asmdef");

            if (name != null)
            {
                return name;
            }

            return null;
        }

        public static string SearchName(string path, string suffix)
        {

            if (!Directory.Exists(path))
            {
                Debug.Log($"Directory not exist {path} {suffix}");

                return null;
            }

            string[] files = Directory.GetFiles(path);

            foreach (string file in files)
            {
                if (file.EndsWith(suffix))
                {
                    String name = Path.GetFileName(file);
                    name = name.Replace(suffix, "");

                    return name;
                }
            }

            return null;
        }

        public static string RuntimeDllName()
        {
            string name = $"{RuntimePrefixName()}.Runtime.dll";

            return name;
        }

        public static string RuntimeAnyPlatformDllName()
        {
            string name = $"{RuntimeAnyPlatformPrefixName()}.Runtime.AnyPlatform.dll";

            return name;
        }

        public static string ProtocolDllName()
        {
            string name = $"{ProtocolPrefixName()}.Protocol.dll";

            return name;
        }

        public static string EditorDllName()
        {
            string name = $"{EditorPrefixName()}.Editor.dll";

            return name;
        }

    }
}
