using System;
using System.Collections.Generic;
using UnityEngine;

namespace Baidu.Meta.ComponentsTool.Editor
{
    /// <summary>
    /// APP打包发布的时候，要把各个ABManifest合并为一个，
    /// 每个bundle名已经带package名区分了
    /// </summary>
    [Serializable]
    public class ABManifest
    {
        //public string packageName;
        public ABAssetBundleInfo[] assetBundles;
        public string[] knownTypes;

        /// <summary>
        ///  this array is generated on manifest load, based on knownTypes, so we don't have to do it per-asset.
        /// </summary>
        [NonSerialized]
        public Type[] runtimeTypes;

        [Serializable]
        public class ABAssetBundleInfo
        {
            public ABAssetBundleInfo() { }
            public ABAssetBundleInfo
                (string folderName, string filehash, long flength, int[] deps,
                string[] assets, int[] assetTypeIndices, int[] assetHash,string fileMd5)
            {
                bundleName = folderName;
                //Hash128 does not convert to Json directly, so it comes in already as a string
                hash128 = filehash;
                length = flength;
                filename = bundleName + "-" + filehash + ".assetbundle";
                dependencies = deps;
                assetNames = assets;
                assetTypeIdx = assetTypeIndices;
                assetHashes = assetHash;
                Bundle = null;  // runtime only
                md5 = fileMd5;
                BundleFullPath = "";
            }

            /// <summary>
            ///  this is what the game code will ask for
            /// </summary>
            public string bundleName;
            /// <summary>
            /// this is the on-disk and on-web filename, which is bundleName-HASH128.assetbundle
            /// </summary>
            public string filename;
            /// <summary>
            /// this is also part of the downloaded filename, so it's impossible to get this out of sync, 
            /// but stored here separately so we can access it without stupid parsing.
            /// </summary>
            public string hash128;
            //file Md5
            public string md5;
            /// <summary>
            /// number of bytes in the file, for verification purposes that we didn't download half the file and crash
            /// </summary>
            public long length;
            /// <summary>
            /// list of indices into assets array that this bundle is dependent on being loaded BEFORE loading this. 
            /// If they are all loaded in manifest-order, you don't need to worry about this.
            /// </summary>
            public int[] dependencies;
            /// <summary>
            /// list of all the assets inside this bundle
            /// </summary>
            public string[] assetNames;
            /// <summary>
            /// corresponding list of indices into the knownTypes array for each asset's type
            /// </summary>
            public int[] assetTypeIdx;
            /// <summary>
            /// corresponding list of hashes for each asset and all its subassets
            /// </summary>
            public int[] assetHashes;

            /// <summary>
            /// This is used during runtime after it's been loaded, 
            /// so it's non-serializable, so it doesn't get written to the manifest.
            /// </summary>
            [NonSerialized]
            public AssetBundle Bundle;

            /// <summary>
            /// 在本地存储的完整路径
            /// </summary>
            [NonSerialized]
            public string BundleFullPath;
        }

        public void GenerateRuntimeTypes()
        {
            // Generate type array
            runtimeTypes = new Type[knownTypes.Length];
            for (int i = 0; i < knownTypes.Length; i++)
            {
                // skip UnityEditor.SceneAsset types, 
                //since these cannot be loaded by object type anyway.
                //Use SceneManager.LoadScene()
                if (knownTypes[i] != "CannotLoad")
                {
                    runtimeTypes[i] = Type.GetType(knownTypes[i]);
                    if (runtimeTypes[i] == null) Debug.LogError("Unable to reproduce knownType: " + knownTypes[i]);
                }
            }
        }

        // This takes an override manifest and bundle and
        // merges it into the 'this' object, so that the rest of the code treats all loading uniformly.
        public void MergeManifest(ABManifest overrideManifest)
        {
            int originalBundleCount = assetBundles.Length;

            // Add to the assetbundles array.
            List<ABAssetBundleInfo> finalBundleList = new List<ABAssetBundleInfo>(assetBundles);
            finalBundleList.AddRange(overrideManifest.assetBundles);
            assetBundles = finalBundleList.ToArray();

            // Add to the known types array for any types that aren't already part of the list
            List<string> finalKnownTypes = new List<string>(knownTypes);
            foreach (string type in overrideManifest.knownTypes)
            {
                if (finalKnownTypes.IndexOf(type) == -1)
                    finalKnownTypes.Add(type);
            }
            knownTypes = finalKnownTypes.ToArray();

            // Reindex the assetTypeIdx array for the override bundles since they refers to a merged knownTypes array now.
            for (int iBundle = originalBundleCount; iBundle < assetBundles.Length; iBundle++)
            {
                // walk and re-index the known types for all the assets in this bundle
                for (int j = 0; j < assetBundles[iBundle].assetTypeIdx.Length; j++)
                {
                    int oldIndex = assetBundles[iBundle].assetTypeIdx[j];
                    int foundIndex = finalKnownTypes.IndexOf(overrideManifest.knownTypes[oldIndex]);
                    assetBundles[iBundle].assetTypeIdx[j] = foundIndex;
                }
            }

            // Finally, regenerate the types for the manifest, since they are different length now.
            GenerateRuntimeTypes();
        }
    }
}
