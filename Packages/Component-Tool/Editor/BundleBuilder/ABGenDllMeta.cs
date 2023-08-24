using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Baidu.Meta.ComponentsTool.Editor
{
    public static class ABGenDllMeta
    {
        public static string editorDllMetaTemplate = "fileFormatVersion: 2\r\nguid: GUID_VALUE\r\nPluginImporter:\r\n  externalObjects: {}\r\n  serializedVersion: 2\r\n  iconMap: {}\r\n  executionOrder: {}\r\n  defineConstraints: []\r\n  isPreloaded: 0\r\n  isOverridable: 0\r\n  isExplicitlyReferenced: 0\r\n  validateReferences: 1\r\n  platformData:\r\n  - first:\r\n      : Any\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        Exclude Android: 1\r\n        Exclude Editor: 0\r\n        Exclude Linux64: 1\r\n        Exclude OSXUniversal: 1\r\n        Exclude WebGL: 1\r\n        Exclude Win: 1\r\n        Exclude Win64: 1\r\n        Exclude WindowsStoreApps: 1\r\n        Exclude iOS: 1\r\n  - first:\r\n      Android: Android\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: ARMv7\r\n  - first:\r\n      Any: \r\n    second:\r\n      enabled: 0\r\n      settings: {}\r\n  - first:\r\n      Editor: Editor\r\n    second:\r\n      enabled: 1\r\n      settings:\r\n        CPU: AnyCPU\r\n        DefaultValueInitialized: true\r\n        OS: AnyOS\r\n  - first:\r\n      Standalone: Linux64\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      Standalone: OSXUniversal\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      Standalone: Win\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      Standalone: Win64\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      Windows Store Apps: WindowsStoreApps\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: AnyCPU\r\n        DontProcess: false\r\n        PlaceholderPath: \r\n        SDK: AnySDK\r\n        ScriptingBackend: AnyScriptingBackend\r\n  - first:\r\n      iPhone: iOS\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        AddToEmbeddedBinaries: false\r\n        CPU: AnyCPU\r\n        CompileFlags: \r\n        FrameworkDependencies: \r\n  userData: \r\n  assetBundleName: \r\n  assetBundleVariant: \r\n";
        public static string androidDllMetaTemplate = "fileFormatVersion: 2\r\nguid: GUID_VALUE\r\nPluginImporter:\r\n  externalObjects: {}\r\n  serializedVersion: 2\r\n  iconMap: {}\r\n  executionOrder: {}\r\n  defineConstraints: []\r\n  isPreloaded: 0\r\n  isOverridable: 0\r\n  isExplicitlyReferenced: 0\r\n  validateReferences: 1\r\n  platformData:\r\n  - first:\r\n      : Any\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        Exclude Android: 0\r\n        Exclude Editor: 1\r\n        Exclude Linux64: 1\r\n        Exclude OSXUniversal: 1\r\n        Exclude WebGL: 1\r\n        Exclude Win: 1\r\n        Exclude Win64: 1\r\n        Exclude WindowsStoreApps: 1\r\n        Exclude iOS: 1\r\n  - first:\r\n      Android: Android\r\n    second:\r\n      enabled: 1\r\n      settings:\r\n        CPU: ARMv7\r\n  - first:\r\n      Any: \r\n    second:\r\n      enabled: 0\r\n      settings: {}\r\n  - first:\r\n      Editor: Editor\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: AnyCPU\r\n        DefaultValueInitialized: true\r\n        OS: AnyOS\r\n  - first:\r\n      Standalone: Linux64\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      Standalone: OSXUniversal\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      Standalone: Win\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      Standalone: Win64\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      Windows Store Apps: WindowsStoreApps\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: AnyCPU\r\n        DontProcess: false\r\n        PlaceholderPath: \r\n        SDK: AnySDK\r\n        ScriptingBackend: AnyScriptingBackend\r\n  - first:\r\n      iPhone: iOS\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        AddToEmbeddedBinaries: false\r\n        CPU: AnyCPU\r\n        CompileFlags: \r\n        FrameworkDependencies: \r\n  userData: \r\n  assetBundleName: \r\n  assetBundleVariant: \r\n";
        public static string iosDllMetaTemplate = "fileFormatVersion: 2\r\nguid: GUID_VALUE\r\nPluginImporter:\r\n  externalObjects: {}\r\n  serializedVersion: 2\r\n  iconMap: {}\r\n  executionOrder: {}\r\n  defineConstraints: []\r\n  isPreloaded: 0\r\n  isOverridable: 0\r\n  isExplicitlyReferenced: 0\r\n  validateReferences: 1\r\n  platformData:\r\n  - first:\r\n      : Any\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        Exclude Android: 1\r\n        Exclude Editor: 1\r\n        Exclude Linux64: 1\r\n        Exclude OSXUniversal: 1\r\n        Exclude WebGL: 1\r\n        Exclude Win: 1\r\n        Exclude Win64: 1\r\n        Exclude WindowsStoreApps: 1\r\n        Exclude iOS: 0\r\n  - first:\r\n      Android: Android\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: ARMv7\r\n  - first:\r\n      Any: \r\n    second:\r\n      enabled: 0\r\n      settings: {}\r\n  - first:\r\n      Editor: Editor\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: AnyCPU\r\n        DefaultValueInitialized: true\r\n        OS: AnyOS\r\n  - first:\r\n      Standalone: Linux64\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      Standalone: OSXUniversal\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      Standalone: Win\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      Standalone: Win64\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      Windows Store Apps: WindowsStoreApps\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: AnyCPU\r\n        DontProcess: false\r\n        PlaceholderPath: \r\n        SDK: AnySDK\r\n        ScriptingBackend: AnyScriptingBackend\r\n  - first:\r\n      iPhone: iOS\r\n    second:\r\n      enabled: 1\r\n      settings:\r\n        AddToEmbeddedBinaries: false\r\n        CPU: AnyCPU\r\n        CompileFlags: \r\n        FrameworkDependencies: \r\n  userData: \r\n  assetBundleName: \r\n  assetBundleVariant: \r\n";
        public static string win64DllMetaTemplate = "fileFormatVersion: 2\r\nguid: GUID_VALUE\r\nPluginImporter:\r\n  externalObjects: {}\r\n  serializedVersion: 2\r\n  iconMap: {}\r\n  executionOrder: {}\r\n  defineConstraints: []\r\n  isPreloaded: 0\r\n  isOverridable: 0\r\n  isExplicitlyReferenced: 0\r\n  validateReferences: 1\r\n  platformData:\r\n  - first:\r\n      : Any\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        Exclude Android: 1\r\n        Exclude Editor: 1\r\n        Exclude Linux64: 0\r\n        Exclude OSXUniversal: 0\r\n        Exclude WebGL: 1\r\n        Exclude Win: 0\r\n        Exclude Win64: 0\r\n        Exclude WindowsStoreApps: 1\r\n        Exclude iOS: 1\r\n  - first:\r\n      Android: Android\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: ARMv7\r\n  - first:\r\n      Any: \r\n    second:\r\n      enabled: 0\r\n      settings: {}\r\n  - first:\r\n      Editor: Editor\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: AnyCPU\r\n        DefaultValueInitialized: true\r\n        OS: AnyOS\r\n  - first:\r\n      Standalone: Linux64\r\n    second:\r\n      enabled: 1\r\n      settings:\r\n        CPU: AnyCPU\r\n  - first:\r\n      Standalone: OSXUniversal\r\n    second:\r\n      enabled: 1\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      Standalone: Win\r\n    second:\r\n      enabled: 1\r\n      settings:\r\n        CPU: x86\r\n  - first:\r\n      Standalone: Win64\r\n    second:\r\n      enabled: 1\r\n      settings:\r\n        CPU: x86_64\r\n  - first:\r\n      Windows Store Apps: WindowsStoreApps\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: AnyCPU\r\n        DontProcess: false\r\n        PlaceholderPath: \r\n        SDK: AnySDK\r\n        ScriptingBackend: AnyScriptingBackend\r\n  - first:\r\n      iPhone: iOS\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        AddToEmbeddedBinaries: false\r\n        CPU: AnyCPU\r\n        CompileFlags: \r\n        FrameworkDependencies: \r\n  userData: \r\n  assetBundleName: \r\n  assetBundleVariant: \r\n";
        public static string webglDllMetaTemplate = "fileFormatVersion: 2\r\nguid: GUID_VALUE\r\nPluginImporter:\r\n  externalObjects: {}\r\n  serializedVersion: 2\r\n  iconMap: {}\r\n  executionOrder: {}\r\n  defineConstraints: []\r\n  isPreloaded: 0\r\n  isOverridable: 0\r\n  isExplicitlyReferenced: 0\r\n  validateReferences: 1\r\n  platformData:\r\n  - first:\r\n      : Any\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        Exclude Android: 1\r\n        Exclude Editor: 1\r\n        Exclude Linux64: 1\r\n        Exclude OSXUniversal: 1\r\n        Exclude WebGL: 0\r\n        Exclude Win: 1\r\n        Exclude Win64: 1\r\n        Exclude WindowsStoreApps: 1\r\n        Exclude iOS: 1\r\n  - first:\r\n      Android: Android\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: ARMv7\r\n  - first:\r\n      Any: \r\n    second:\r\n      enabled: 0\r\n      settings: {}\r\n  - first:\r\n      Editor: Editor\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: AnyCPU\r\n        DefaultValueInitialized: true\r\n        OS: AnyOS\r\n  - first:\r\n      Standalone: Linux64\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      Standalone: OSXUniversal\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      Standalone: Win\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      Standalone: Win64\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: None\r\n  - first:\r\n      WebGL: WebGL\r\n    second:\r\n      enabled: 1\r\n      settings: {}\r\n  - first:\r\n      Windows Store Apps: WindowsStoreApps\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: AnyCPU\r\n        DontProcess: false\r\n        PlaceholderPath: \r\n        SDK: AnySDK\r\n        ScriptingBackend: AnyScriptingBackend\r\n  - first:\r\n      iPhone: iOS\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        AddToEmbeddedBinaries: false\r\n        CPU: AnyCPU\r\n        CompileFlags: \r\n        FrameworkDependencies: \r\n  userData: \r\n  assetBundleName: \r\n  assetBundleVariant: \r\n";
        public static string anyDllMetaTemplate = "fileFormatVersion: 2\r\nguid: GUID_VALUE\r\nPluginImporter:\r\n  externalObjects: {}\r\n  serializedVersion: 2\r\n  iconMap: {}\r\n  executionOrder: {}\r\n  defineConstraints: []\r\n  isPreloaded: 0\r\n  isOverridable: 0\r\n  isExplicitlyReferenced: 0\r\n  validateReferences: 1\r\n  platformData:\r\n  - first:\r\n      Any: \r\n    second:\r\n      enabled: 1\r\n      settings: {}\r\n  - first:\r\n      Editor: Editor\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        DefaultValueInitialized: true\r\n  - first:\r\n      Windows Store Apps: WindowsStoreApps\r\n    second:\r\n      enabled: 0\r\n      settings:\r\n        CPU: AnyCPU\r\n  userData: \r\n  assetBundleName: \r\n  assetBundleVariant: \r\n";
        public static string fileMetaTemplate = "fileFormatVersion: 2\r\nguid: GUID_VALUE\r\nDefaultImporter:\r\n  externalObjects: {}\r\n  userData: \r\n  assetBundleName: \r\n  assetBundleVariant: \r\n";
        public static string folderMetaTemplate = "fileFormatVersion: 2\r\nguid: GUID_VALUE\r\nfolderAsset: yes\r\nDefaultImporter:\r\n  externalObjects: {}\r\n  userData: \r\n  assetBundleName: \r\n  assetBundleVariant: \r\n";

        public static string MetaGuid()
        {
            string uuidN = System.Guid.NewGuid().ToString("N");

            //UnityEngine.Debug.Log($"MetaGuid {uuidN}");

            return uuidN;
        }

        public static string EditorDllMeta()
        {
            string uuidN = MetaGuid();

            string res = editorDllMetaTemplate.Replace("GUID_VALUE", uuidN);
            UnityEngine.Debug.Log($"EditorDllMeta {res}");

            return res;
        }

        public static string AndroidDllMeta()
        {
            string uuidN = MetaGuid();

            string res = androidDllMetaTemplate.Replace("GUID_VALUE", uuidN);
            UnityEngine.Debug.Log($"AndroidDllMeta {res}");

            return res;
        }

        public static string iOSDllMeta()
        {
            string uuidN = MetaGuid();

            string res = iosDllMetaTemplate.Replace("GUID_VALUE", uuidN);
            UnityEngine.Debug.Log($"iOSDllMeta {res}");

            return res;
        }

        public static string Win64DllMeta()
        {
            string uuidN = MetaGuid();

            string res = win64DllMetaTemplate.Replace("GUID_VALUE", uuidN);
            UnityEngine.Debug.Log($"Win64DllMeta {res}");

            return res;
        }

        public static string WebglDllMeta()
        {
            string uuidN = MetaGuid();

            string res = webglDllMetaTemplate.Replace("GUID_VALUE", uuidN);
            UnityEngine.Debug.Log($"WebglDllMeta {res}");

            return res;
        }

        public static string AnyDllMeta()
        {
            string uuidN = MetaGuid();

            string res = anyDllMetaTemplate.Replace("GUID_VALUE", uuidN);
            UnityEngine.Debug.Log($"AnyDllMeta {res}");

            return res;
        }

        public static string FileMeta()
        {
            string uuidN = MetaGuid();

            string res = fileMetaTemplate.Replace("GUID_VALUE", uuidN);
            //UnityEngine.Debug.Log($"FileMeta {res}");

            return res;
        }

        public static string FolderMeta()
        {
            string uuidN = MetaGuid();

            string res = folderMetaTemplate.Replace("GUID_VALUE", uuidN);
            UnityEngine.Debug.Log($"FolderMeta {res}");

            return res;
        }

        public static void MetaFileGen(string metaDesPath, string metaText)
        {
            using (FileStream fs = File.Create(metaDesPath))
            {
                byte[] info = new UTF8Encoding(true).GetBytes(metaText);
                fs.Write(info, 0, info.Length);
            }
        }

        public static void EditorMetaFileGen(string metaDesPath)
        {
            string metaText = ABGenDllMeta.EditorDllMeta();

            MetaFileGen(metaDesPath, metaText);
        }

        public static void AndroidMetaFileGen(string metaDesPath)
        {
            string metaText = ABGenDllMeta.AndroidDllMeta();

            MetaFileGen(metaDesPath, metaText);
        }

        public static void iOSMetaFileGen(string metaDesPath)
        {
            string metaText = ABGenDllMeta.iOSDllMeta();

            MetaFileGen(metaDesPath, metaText);
        }

        public static void Win64MetaFileGen(string metaDesPath)
        {
            string metaText = ABGenDllMeta.Win64DllMeta();

            MetaFileGen(metaDesPath, metaText);
        }

        public static void WebglMetaFileGen(string metaDesPath)
        {
            string metaText = ABGenDllMeta.WebglDllMeta();

            MetaFileGen(metaDesPath, metaText);
        }

        public static void AnyMetaFileGen(string metaDesPath)
        {
            string metaText = ABGenDllMeta.AnyDllMeta();

            MetaFileGen(metaDesPath, metaText);
        }

        public static void FileMetaFileGen(string metaDesPath)
        {
            string metaText = ABGenDllMeta.FileMeta();

            MetaFileGen(metaDesPath, metaText);
        }

        public static void FolderMetaFileGen(string metaDesPath)
        {
            string metaText = ABGenDllMeta.FolderMeta();

            MetaFileGen(metaDesPath, metaText);
        }

        public static void TargetMetaFileGen(string metaDesPath, BuildTarget target)
        {
            string metaText = ABGenDllMeta.AnyDllMeta();

            switch (target)
            {
                case BuildTarget.Android:
                    metaText = ABGenDllMeta.AndroidDllMeta();
                    break;
                case BuildTarget.iOS:
                    metaText = ABGenDllMeta.iOSDllMeta();
                    break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    metaText = ABGenDllMeta.Win64DllMeta();
                    break;
                case BuildTarget.WebGL:
                    metaText = ABGenDllMeta.WebglDllMeta();
                    break;
            }

            MetaFileGen(metaDesPath, metaText);
        }
    }
}
