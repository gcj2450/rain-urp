using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using static UnityEditor.ShaderData;
using Object = UnityEngine.Object;

namespace Baidu.Meta.ComponentsTool.Editor
{
    /// <summary>
    ///     资源的打包分组。
    /// </summary>
    [CreateAssetMenu(menuName = "ABBundleCollector/Create", fileName = "ABBundleCollector", order = 0)]
    public class ABBundleCollector : ScriptableObject
    {
        [Tooltip("该配置对应的平台")]
        public BundlePlatform platform;
        [Tooltip("每个物体打成一个bundle")]
        public Object[] bundles;

        private string GetDirectoryName(string path)
        {
            var dir = Path.GetDirectoryName(path);
            return !string.IsNullOrEmpty(dir) ? dir.Replace("\\", "/") : string.Empty;
        }


        public void CollectAssets(ABBundleCollector bundleCollector, Action<string,Dictionary<string, List<string>>> onCollect)
        {
            if (bundleCollector == null || bundleCollector.bundles == null) return;

            Dictionary<string, List<string>> assetsPerBundle = new Dictionary<string, List<string>>();
            List<string> allassets = new List<string>();

            foreach (var asset in bundleCollector.bundles)
            {
                if (asset == null) continue;

                var path = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(path)) continue;

                if (!Directory.Exists(path))
                {
                    //Debug.Log($"path is not folder: {path} ");
                    string cleanS = path.Replace('\\', '/').ToLowerInvariant();
                    string bundleName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

                    if (!IsValidAsset(cleanS))
                        continue;

                    if (!allassets.Contains(cleanS))
                    {
                        if (assetsPerBundle.ContainsKey(bundleName) == false)
                            assetsPerBundle[bundleName] = new List<string>();
                        assetsPerBundle[bundleName].Add(cleanS);
                        allassets.Add(cleanS);
                        //Debug.Log($"bundleName: {bundleName}, cleanS: {cleanS}");
                    }
                    continue;
                }
                var guidsInfolder = AssetDatabase.FindAssets("",new[] { path });
                //Debug.Log($"guids: {guidsInfolder.Length}");
                foreach (var guidItem in guidsInfolder)
                {
                    var child = AssetDatabase.GUIDToAssetPath(guidItem);
                    if (string.IsNullOrEmpty(child)|| Directory.Exists(child))
                        continue;


                    string bundleName = Path.GetFileNameWithoutExtension(path).Replace('\\', '/').ToLowerInvariant();
                    string cleanS = child.Replace('\\', '/').ToLowerInvariant();

                    if (!IsValidAsset(cleanS))
                        continue;

                    if (!allassets.Contains(cleanS))
                    {
                        if (assetsPerBundle.ContainsKey(bundleName) == false)
                            assetsPerBundle[bundleName] = new List<string>();
                        assetsPerBundle[bundleName].Add(cleanS);
                        allassets.Add(cleanS);
                        //Debug.Log($"bundleName: {bundleName}, cleanS: {cleanS}");
                    }
                }
            }
            Debug.Log($"All assets count: {allassets.Count}");
            onCollect?.Invoke(platform.ToString(),assetsPerBundle);
        }

        private bool IsValidAsset(string assetPath)
        {
            Type asset = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (asset == null)
            {
                Debug.Log("<color=#ff8080>Skipping problem asset or is missing a dependency (often a mono script): " + assetPath + "</color>");
                return false;
            }
            else if (asset.AssemblyQualifiedName.StartsWith("UnityEditor.DefaultAsset"))  // ignore folders
            {
                return false;
            }
            else if (asset.AssemblyQualifiedName.StartsWith("UnityEditor.SceneAsset"))  // allow scenes
            {
                return true;
            }
            else if (asset.AssemblyQualifiedName.StartsWith("UnityEditor."))
            {
                Debug.Log("<color=#ff8080>Skipping Editor-only asset type [" + asset.AssemblyQualifiedName + "]: " + assetPath + "</color>");
                return false;
            }
            return true;
        }

    }

    public enum BundlePlatform
    {
        win64,
        android,
        ios,
        webgl,
        vr
    }
}