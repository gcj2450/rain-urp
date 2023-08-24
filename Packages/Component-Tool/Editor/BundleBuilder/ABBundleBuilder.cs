using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace Baidu.Meta.ComponentsTool.Editor
{
    // This runs through a folder and generates one asset bundle per folder containing everything in each folder.
    public class ABBundleBuilder
    {
        static public bool DoBuildBundles(Dictionary<string, List<string>> assetsPerBundle, string buildVersion, string outputPath, BuildAssetBundleOptions opt, BuildTarget platform, string platformString, bool doLogging, string[] ignoreEndsWith, string[] ignoreContains, string[] ignoreExact)
        {
            if (assetsPerBundle==null|| assetsPerBundle.Count == 0)
            {
                Debug.Log($"assetsPerBundle ==null or count==0");
                return false;
            }
            
            AssetBundleManifest manifest = null;
            AssetBundleBuild[] buildMap = null;
            Dictionary<string, HashSet<string>> dependencyMatrix = null;
            List<string> bundleOrder = null;
            if (assetsPerBundle.Count > 0)
            {
                // Create an asset bundle for each folder containing all the child assets inside it.
                buildMap = new AssetBundleBuild[assetsPerBundle.Count];
                List<string> bundleNames = new List<string>(assetsPerBundle.Keys);
                for (int i = 0; i < bundleNames.Count; i++)
                {
                    buildMap[i].assetBundleName = bundleNames[i] + ".assetbundle";
                    buildMap[i].assetNames = assetsPerBundle[bundleNames[i]].ToArray();
                }

                // Build all the asset bundles in one go.
                manifest = BuildPipeline.BuildAssetBundles(outputPath, buildMap, opt, platform);
                if (manifest == null)
                {
                    Debug.LogError("<color=#ff8080>[" + platformString + "] Asset bundle build failed </color>");
                    return false;
                }

                // Convert the dependencies per bundle to a hash set of their DIRECT dependencies.
                dependencyMatrix = GenerateDependencyMatrix(manifest);

                // Figure out the correct ordering for bundles.  This may fail, which is ok, it just means there are cyclical dependencies.
                bundleOrder = DetermineBundleOrder(dependencyMatrix, buildMap);
            }
            else
            {
                Debug.Log("<color=#ff8080>[" + platformString + "] No asset bundles to build</color>");
                dependencyMatrix = new Dictionary<string, HashSet<string>>();
                bundleOrder = new List<string>();
                //没有可打包的数资，返回失败
                return false;

            }

            // Clean the folder of junk we don't want or need.  Always remove override files, since they can't be valid now that we just built bundles.
            RemoveUnnecessaryFiles(outputPath, true);

            // Generate the manifest we want to load, with dependencies and hashes all the hashes calculated for the asset bundles and a list of all the assets in each bundle.
            // Note, this also renamed the bundles on disk to contain their hash128 in the name.
            ABManifest manifestData = GenerateBundleManifest(outputPath, dependencyMatrix, bundleOrder, buildMap, manifest);

            string manifestJson = JsonUtility.ToJson(manifestData);  // convert structure over to json
            string manifestPath = outputPath + "/manifest-" + buildVersion + ".json";
            File.WriteAllText(manifestPath, manifestJson, System.Text.Encoding.UTF8);
            return true;  // everything is ok
        }

        // Build all the bundles under the specified folder, generate a manifest.json that is put along side them, and return the path to it.
        static public bool DoBuildBundles(string srcBundleFolder, string buildVersion, string outputPath, BuildAssetBundleOptions opt, BuildTarget platform, string platformString, bool doLogging, string[] ignoreEndsWith, string[] ignoreContains, string[] ignoreExact)
        {
            if (Directory.Exists(srcBundleFolder) == false)
            {
                Debug.LogError("<color=#ff8080>Root of bundles folders does not exist (" + srcBundleFolder + ")</color>");
                return false;
            }
            Debug.Log("srcBundleFolder: " + srcBundleFolder);

            // Get all asset paths first, then collect them into separate bundles in a single pass.
            string[] allAssets = AssetDatabase.GetAllAssetPaths();
            Dictionary<string, List<string>> assetsPerBundle = new Dictionary<string, List<string>>();
            string assetsBundlesFolder = ("Assets" + srcBundleFolder.Remove(0, Application.dataPath.Length)).ToLowerInvariant();
            Debug.Log("assetsBundlesFolder: " + assetsBundlesFolder);   // assets/src/assets/

            foreach (string s in allAssets)
            {
                string cleanS = s.Replace('\\', '/').ToLowerInvariant();  // always work in lower case and with forward slashes
                if (cleanS.StartsWith(assetsBundlesFolder))
                {
                    string bundleName = cleanS.Remove(0, assetsBundlesFolder.Length);
                    int nextSlash = bundleName.IndexOf('/');
                    if (nextSlash != -1)  // ignore files laying in the bundle folder
                    {
                        //bundle名字就是每个文件夹的名字
                        bundleName =bundleName.Remove(nextSlash);

                        // Handle ignore logic
                        if (ShouldIgnore(cleanS, doLogging, ignoreEndsWith, ignoreContains, ignoreExact))
                            continue;

                        // Skip any asset that fails to produce a main asset type
                        if (!IsValidAsset(cleanS))
                            continue;

                        // Logs all assets added to each bundle.
                        //if (doLogging)
                        //    Debug.Log("Bundle [" + bundleName + "] Asset [" + cleanS + "]");

                        if (assetsPerBundle.ContainsKey(bundleName) == false)
                            assetsPerBundle[bundleName] = new List<string>();
                        assetsPerBundle[bundleName].Add(cleanS);  // all asset names are fully lowercase in the manifest
                    }
                }
            }
            AssetBundleManifest manifest = null;
            AssetBundleBuild[] buildMap = null;
            Dictionary<string, HashSet<string>> dependencyMatrix = null;
            List<string> bundleOrder = null;
            if (assetsPerBundle.Count > 0)
            {
                // Create an asset bundle for each folder containing all the child assets inside it.
                buildMap = new AssetBundleBuild[assetsPerBundle.Count];
                List<string> bundleNames = new List<string>(assetsPerBundle.Keys);
                for (int i = 0; i < bundleNames.Count; i++)
                {
                    buildMap[i].assetBundleName = bundleNames[i] + ".assetbundle";
                    buildMap[i].assetNames = assetsPerBundle[bundleNames[i]].ToArray();
                }

                // Build all the asset bundles in one go.
                manifest = BuildPipeline.BuildAssetBundles(outputPath, buildMap, opt, platform);
                if (manifest == null)
                {
                    Debug.LogError("<color=#ff8080>[" + platformString + "] Asset bundle build failed for " + srcBundleFolder + "</color>");
                    return false;
                }

                // Convert the dependencies per bundle to a hash set of their DIRECT dependencies.
                dependencyMatrix = GenerateDependencyMatrix(manifest);

                // Figure out the correct ordering for bundles.  This may fail, which is ok, it just means there are cyclical dependencies.
                bundleOrder = DetermineBundleOrder(dependencyMatrix, buildMap, assetsBundlesFolder);
            }
            else
            {
                Debug.Log("<color=#ff8080>[" + platformString + "] No asset bundles to build: No subfolders in " + srcBundleFolder + "</color>");
                dependencyMatrix = new Dictionary<string, HashSet<string>>();
                bundleOrder = new List<string>();
                //没有可打包的数资，返回失败
                return false;

            }

            // Clean the folder of junk we don't want or need.  Always remove override files, since they can't be valid now that we just built bundles.
            RemoveUnnecessaryFiles(outputPath, true);

            // Generate the manifest we want to load, with dependencies and hashes all the hashes calculated for the asset bundles and a list of all the assets in each bundle.
            // Note, this also renamed the bundles on disk to contain their hash128 in the name.
            ABManifest manifestData = GenerateBundleManifest(outputPath, dependencyMatrix, bundleOrder, buildMap, manifest);

            string manifestJson = JsonUtility.ToJson(manifestData);  // convert structure over to json
            string manifestPath = outputPath + "/manifest-" + buildVersion + ".json";
            File.WriteAllText(manifestPath, manifestJson, System.Text.Encoding.UTF8);
            return true;  // everything is ok
        }

        // This creates the set of dependencies as a data structure we can easily use to output the .json file.
        static private Dictionary<string, HashSet<string>> GenerateDependencyMatrix(AssetBundleManifest manifest)
        {
            string[] bundleNames = manifest.GetAllAssetBundles();

            // create an adjacency matrix where dependency[x][y] being true implies X depends on Y.
            Dictionary<string, HashSet<string>> dependencyMatrix = new Dictionary<string, HashSet<string>>();
            foreach (string name in bundleNames)
            {
                string[] dependencies = manifest.GetDirectDependencies(name);
                dependencyMatrix.Add(name, new HashSet<string>(dependencies));
            }
            return dependencyMatrix;
        }

        // This function looks at the dependency list and discovers (correctly) if there are loops in the dependency list, and 
        // if not, what order everything must be loaded in to resolve correctly.
        static private List<string> DetermineBundleOrder(Dictionary<string, HashSet<string>> dependencyMatrix, AssetBundleBuild[] buildMap, string assetBundlesFolder="")
        {
            // Now, if the bundles are a directed-acyclical-graph, we should be able to always add another bundle that 
            // has NO dependencies, or ONLY dependencies previously added to the list.
            List<string> bundlesInMinimumDependencyOrder = new List<string>();
            List<string> bundlesRemaining = new List<string>(dependencyMatrix.Keys);
            while (bundlesRemaining.Count > 0)
            {
                bool placedABundle = false;
                for (int j = 0; j < bundlesRemaining.Count; j++)
                {
                    string bundleToCheck = bundlesRemaining[j];
                    if (AreAllDependenciesInTheList(bundlesInMinimumDependencyOrder, dependencyMatrix[bundleToCheck]))
                    {
                        placedABundle = true;
                        bundlesInMinimumDependencyOrder.Add(bundlesRemaining[j]);
                        bundlesRemaining.RemoveAt(j);
                        break;
                    }
                }
                if (placedABundle == false)
                {
                    // Do exhaustive analysis of each asset and determine which ones are cyclically dependent.
                    Dictionary<string, int> assetToBundle = new Dictionary<string, int>();
                    foreach (string bundleName in bundlesRemaining)
                    {
                        for (int bIndex = 0; bIndex < buildMap.Length; bIndex++)  // find the bundle and all its assets in the buildMap
                        {
                            if (buildMap[bIndex].assetBundleName == bundleName)
                            {
                                foreach (string assetName in buildMap[bIndex].assetNames)
                                {
                                    assetToBundle.Add(assetName, bIndex);  // remember what bundle this came from
                                }
                            }
                        }
                    }

                    // Now, walk the list of assets and remove any that only depend on nothing or  only items in their own bundles.
                    Dictionary<string, List<string>> crossBundleDeps = new Dictionary<string, List<string>>();
                    foreach (var assetAndBundle in assetToBundle)
                    {
                        // build a list of direct dependencies per asset.  Note, this returns the mixed case filenames of things,
                        // and returns ALL dependencies including script files and built-in objects, etc.
                        string[] deps = AssetDatabase.GetDependencies(assetAndBundle.Key, false);

                        int myBundleIndex = assetAndBundle.Value;
                        int hisBundleIndex = 0;
                        foreach (string dependency in deps)
                        {
                            string lowerDependency = dependency.ToLowerInvariant();

                            // this should never fail because any asset that is listed in a remaining bundle
                            // SHOULD only be dependent on nothing or assets in unplaced bundles.
                            if (assetToBundle.TryGetValue(lowerDependency, out hisBundleIndex))
                            {
                                if (hisBundleIndex != myBundleIndex)
                                {
                                    // Worthy of display, so record any of the things this asset is dependent on that lives in another bundle
                                    if (crossBundleDeps.ContainsKey(assetAndBundle.Key) == false)
                                        crossBundleDeps.Add(assetAndBundle.Key, new List<string>());
                                    crossBundleDeps[assetAndBundle.Key].Add(lowerDependency);
                                }
                            }
                        }
                    }

                    // Invert the lookup so it's easier to display by bundle
                    Dictionary<int, List<string>> bundleToAsset = new Dictionary<int, List<string>>();
                    foreach (var kvp in assetToBundle)
                    {
                        if (bundleToAsset.ContainsKey(kvp.Value) == false)
                            bundleToAsset.Add(kvp.Value, new List<string>());
                        bundleToAsset[kvp.Value].Add(kvp.Key);
                    }

                    // Now, let's show What depends on What.
                    string message = "";
                    List<string> problemDeps = null;
                    foreach (var kvp in bundleToAsset)
                    {
                        // This loop actually tracks through the dependency matrix until it arrives at itself,
                        // or gives up.  If it doesn't cycle, it's not an error, just a bundle that depends on some that cycle.
                        HashSet<string> currentBundle = new HashSet<string>();
                        foreach (string depBundle in dependencyMatrix[buildMap[kvp.Key].assetBundleName])
                        {
                            currentBundle.Add(depBundle);  // initialize the set with all this bundle's dependencies.
                        }

                        for (int i = 0; i < dependencyMatrix.Count; i++)  // only check N steps, because if you don't find it by then, we're trapped in a cycle somewhere else.
                        {
                            // For each step, we expand one dependency link farther by adding their deps to the set.
                            List<string> currentSet = new List<string>(currentBundle);  // take a copy so we can modify currentBundle
                            foreach (string bundleName in currentSet)
                            {
                                foreach (string depBundle in dependencyMatrix[bundleName])
                                {
                                    currentBundle.Add(depBundle);
                                }
                            }
                        }
                        // At this point, if our OWN bundle is in the set, we have a cycle.  If not, this bundle is fine.
                        if (currentBundle.Contains(buildMap[kvp.Key].assetBundleName) == false)
                            continue;

                        bool hasDisplayedThisBundle = false;
                        foreach (string asset in kvp.Value)
                        {
                            if (crossBundleDeps.TryGetValue(asset, out problemDeps))  // only display if there might be an issue
                            {
                                string srcBundleName = buildMap[kvp.Key].assetBundleName.Substring(0, buildMap[kvp.Key].assetBundleName.LastIndexOf('.'));
                                string shortAssetName = asset.Remove(0, asset.IndexOf(srcBundleName) + srcBundleName.Length + 1);
                                if (!hasDisplayedThisBundle)
                                {
                                    message += "<color=#ff8080>" + srcBundleName + "</color>\n";
                                    hasDisplayedThisBundle = true;
                                }

                                message += "    + " + shortAssetName + "\n";
                                if (!string.IsNullOrEmpty(assetBundlesFolder))
                                {
                                    foreach (string dependency in crossBundleDeps[asset])  // display everything that is a potential problem
                                    {
                                        string shortDep = dependency.Remove(0, assetBundlesFolder.Length);
                                        message += "        -> " + shortDep + "\n";
                                    }
                                }
                            }
                        }
                        if (hasDisplayedThisBundle)
                            message += "\n";
                    }

                    ABErrorDialog.InitWindow(message);  // shows a GUI popup or logs when in a headless build

                    // Fail out
                    string err = "<color=#ff8080>Asset bundles have circular dependencies: ";
                    foreach (string ab in bundlesRemaining)
                        err += ab + " ";
                    err += "</color>";
                    throw new Exception(err);
                }
            }
            // this returns the exact order that asset bundles need to be loaded and mounted for assets to resolve.
            return bundlesInMinimumDependencyOrder;
        }

        // If every string in dependencies is contained in bundles, return true.
        static private bool AreAllDependenciesInTheList(List<string> bundles, HashSet<string> dependencies)
        {
            foreach (string s in dependencies)
            {
                if (!bundles.Contains(s))
                    return false;
            }
            return true;
        }

        // Create the structure we want to store off in the manifest.json file for our system to load at runtime.
        static private ABManifest GenerateBundleManifest(string outputPath, Dictionary<string, HashSet<string>> dependencyMatrix,
                                                        List<string> bundleOrder, AssetBundleBuild[] buildMap, AssetBundleManifest unityManifest)
        {
            List<ABManifest.ABAssetBundleInfo> assetManifest = new List<ABManifest.ABAssetBundleInfo>();
            List<string> knownTypes = new List<string>();  // asset types get recorded here

            foreach (string bundle in bundleOrder)  // bundle is a short name (foo.assetbundle)
            {
                // Get the hash for each asset bundle, which was already computed during build process.
                string hash128 = unityManifest != null ? unityManifest.GetAssetBundleHash(bundle).ToString() : "00000000000000000000000000000000";

                // Get length of file on disk
                string fullPathToBundle = outputPath + "/" + bundle;
                FileInfo finfo = new FileInfo(fullPathToBundle);
                long flength = finfo.Length;
                string fileMd5 = ABUtilities.GetFileMD5(fullPathToBundle);
                // Walk the set of dependencies and generate a list of integer indices into the bundles that specify what 
                // other direct dependencies need to be loaded (recursively) before loading this bundle.
                List<int> bundleIndices = new List<int>();
                foreach (string dep in dependencyMatrix[bundle])
                {
                    bundleIndices.Add(bundleOrder.IndexOf(dep));
                }

                // Find the set of assets in the manifest's build map
                int buildMapIndex = -1;
                for (int i = 0; i < buildMap.Length; i++)
                {
                    if (buildMap[i].assetBundleName.Equals(bundle))
                    {
                        buildMapIndex = i;
                        break;
                    }
                }
                if (buildMapIndex == -1)
                    Debug.LogError("<color=#ff8080>Asset bundle " + bundle + " was not found in the buildMap, so the asset list would be empty!</color>");

                string bundleWithoutSuffix = bundle.Replace(".assetbundle", "");

                // Copy the type names into the map too (this may be overkill, but I'd like to be able to search by 
                // type and override old bundles with new bundles, without having to search each bundle one by one, back to front)
                int[] assetIndices = new int[buildMap[buildMapIndex].assetNames.Length];
                int[] assetHashes = new int[buildMap[buildMapIndex].assetNames.Length];
                for (int i = 0; i < assetIndices.Length; i++)
                {
                    string assetPath = buildMap[buildMapIndex].assetNames[i];
                    Type asset = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                    string typeString = asset.AssemblyQualifiedName;

                    // skip any assets that are not going to be available at runtime, specifically UnityEditor.DefaultAsset (folders).  
                    // Except Scenes.  We need those, but we remap them to Object, because the UnityEditor assembly is not available.  This 
                    // is why scenes have special handling in AssetBundles in the first place.
                    if (typeString.StartsWith("UnityEditor.SceneAsset"))
                        typeString = "CannotLoad";
                    else if (typeString.StartsWith("UnityEditor."))
                        typeString = "EditorOnly";

                    // Find the asset type in the set of known types, or add it.
                    int index = knownTypes.IndexOf(typeString);
                    if (index == -1)
                    {
                        knownTypes.Add(typeString);
                        assetIndices[i] = knownTypes.Count - 1;
                    }
                    else
                    {
                        assetIndices[i] = index;
                    }

                    // This gives us a hash of the asset, its dependencies, its name, platform. importer Version, etc.  If anything changes, we will 
                    // know to include the whole asset and its subassets in an override bundle.
                    Hash128 hash = AssetDatabase.GetAssetDependencyHash(assetPath);
                    int quickHash = ABEditorUtilities.HashOfHash128(hash);
                    assetHashes[i] = quickHash;
                }

                // Add this bundle to the manifest
                ABManifest.ABAssetBundleInfo bundleInfo = new ABManifest.ABAssetBundleInfo(bundleWithoutSuffix, hash128, flength, bundleIndices.ToArray(),
                                                                                            buildMap[buildMapIndex].assetNames, assetIndices, assetHashes, fileMd5);
                assetManifest.Add(bundleInfo);

                // Rename the bundle filename so it includes the hash128.
                string fullPathToBundleFinal = outputPath + "/" + bundleInfo.filename;
                if (File.Exists(fullPathToBundleFinal))
                    File.Delete(fullPathToBundleFinal);
                File.Move(fullPathToBundle, fullPathToBundleFinal);
            }

            // convert to final manifest object
            ABManifest manifestData = new ABManifest();
            //manifestData.packageName = ABEditorUtilities.GetPackageName();
            manifestData.assetBundles = assetManifest.ToArray();
            manifestData.knownTypes = knownTypes.ToArray();
            return manifestData;
        }

        // Get rid of the *.manifest and Bundles files, so we are left with only stuff we actually want to put on the CDN
        static private void RemoveUnnecessaryFiles(string outputPath, bool removeOverrides)
        {
            File.Delete(outputPath + "/Bundles");
            foreach (var filename in Directory.GetFiles(outputPath, "*.manifest", SearchOption.TopDirectoryOnly))
            {
                File.Delete(filename);
            }
            if (removeOverrides)
            {
                foreach (var filename in Directory.GetFiles(outputPath, "override-*", SearchOption.TopDirectoryOnly))
                {
                    File.Delete(filename);
                }
            }
        }

        //-------------------
        // Check all the hashes in the asset database against those in the manifest and take anything that doesn't match and add it to an override bundle.
        static public void DoOverrideBundleBuild(ABBuildInfo buildInfo, string _buildVersion,string _platformStr)
        {
            // We want to pop the override bundle into the Cache folder, which ends up being Application.persistentDataPath+"/Bundles" typically
            ABUtilities.ConfigureCache(ABEditorUtilities.GetBundleVersionFromPackageJson());

            // Load up the build configuration early, so if it fails, we didn't waste a lot of time.
            ABBuildConfig buildConfig = ABEditorUtilities.GetABBuildConfig();
            if (buildConfig == null) Debug.LogError("<color=#ff8080>ABBuildConfig not found.</color>");

            // Load up the bundle manifest of the current build
            string manifestFullPath = ABUtilities.GetRuntimeCacheFolder(ABEditorUtilities.GetBundleVersionFromPackageJson()) + "/manifest-" + _buildVersion + ".json";
            if (File.Exists(manifestFullPath) == false) Debug.LogError("<color=#ff8080>Bundle manifest does not exist in runtime cache: " + manifestFullPath + "</color>");
            byte[] fileContents = File.ReadAllBytes(manifestFullPath);
            string jsonString = ABUtilities.GetStringFromUTF8File(fileContents);
            ABManifest manifest = JsonUtility.FromJson<ABManifest>(jsonString);  // convert json to our data structure
            if (manifest == null) Debug.LogError("<color=#ff8080>Bundle manifest did not parse: " + manifestFullPath + "</color>");

            // Make the runtime type array from known types
            manifest.GenerateRuntimeTypes();

            // We only care about the dependencies of assets INSIDE the srcBundles folder, not EVERYTHING.  So we collect them as we go.
            HashSet<string> allBundleableAssets = new HashSet<string>();

            // Collect all the things that need to be put into a special bundle here.
            HashSet<string> overrideAssets = new HashSet<string>();
            Dictionary<string, HashSet<string>> bundleToAssetsInBundle = new Dictionary<string, HashSet<string>>();  // we build a quick lookup we will use below

            // Walk all the assets in the manifest and check if their dependency hash has changed.
            for (int bundleIndex = 0; bundleIndex < manifest.assetBundles.Length; bundleIndex++)
            {
                ABManifest.ABAssetBundleInfo abi = manifest.assetBundles[bundleIndex];
                HashSet<string> assetsInThisBundle = new HashSet<string>();

                for (int i = 0; i < abi.assetNames.Length; i++)
                {
                    allBundleableAssets.Add(abi.assetNames[i]);  // collect all asset names in one place for dependency checking
                    assetsInThisBundle.Add(abi.assetNames[i]);  // all assets get put into this hash set, to speed up searching below

                    Hash128 h = AssetDatabase.GetAssetDependencyHash(abi.assetNames[i]);
                    int quickHash = ABEditorUtilities.HashOfHash128(h);
                    if (quickHash != abi.assetHashes[i])
                    {
                        // Logs all override assets
                        if (buildInfo.Logging)
                            Debug.Log("Override Asset Changed [" + abi.assetNames[i] + "]");

                        overrideAssets.Add(abi.assetNames[i]);  // save the asset to be added later
                    }
                }
                bundleToAssetsInBundle[abi.bundleName] = assetsInThisBundle;
            }

            // Ok, now we do the difficult work of figuring out what assets are NOT in the manifest but ARE in one of the /Assets/Bundles/... folders.
            string[] allAssets = AssetDatabase.GetAllAssetPaths();
            string assetsBundlesFolder = ("Assets" + buildInfo.SrcBundleFolder.Remove(0, Application.dataPath.Length)).ToLowerInvariant();
            foreach (string s in allAssets)
            {
                string cleanS = s.Replace('\\', '/').ToLowerInvariant();  // always work in lower case and with forward slashes
                if (cleanS.StartsWith(assetsBundlesFolder))
                {
                    string bundleName = cleanS.Remove(0, assetsBundlesFolder.Length);
                    int nextSlash = bundleName.IndexOf('/');
                    if (nextSlash != -1)  // ignore files laying in the bundle folder
                    {
                        bundleName = bundleName.Remove(nextSlash);

                        // Handle ignore logic
                        if (ShouldIgnore(cleanS, buildInfo.Logging, buildInfo.IgnoreEndsWith, buildInfo.IgnoreContains, buildInfo.IgnoreExact))
                            continue;

                        // Skip any asset that fails to produce a main asset type
                        if (!IsValidAsset(cleanS))
                            continue;

                        // If this bundle exists, ask it if the asset is there too.  If it wasn't previously bundled, add it to the override list.
                        if (!bundleToAssetsInBundle.ContainsKey(bundleName) || !bundleToAssetsInBundle[bundleName].Contains(cleanS))
                        {
                            // Logs all override assets
                            if (buildInfo.Logging)
                                Debug.Log("Override Asset New [" + cleanS + "]");

                            overrideAssets.Add(cleanS);
                        }

                        // Record anything that could be bundled here too, for dependency walking.
                        allBundleableAssets.Add(cleanS);
                    }
                }
            }

            // Okay, now we have exactly the changes, but we need to collect all things that are DEPENDENT upon these changes, too.
            // So what we do is grab ALL dependencies once, then walk upward from the assets in the override bundle and collect all the
            // higher-level dependent assets, recursively, until there is nothing left to collect.  Kinda complicated, but works 100%.
            // The built-in asset database functions are kind of useless for this because I don't want to do all the heavy lifting to recursively
            // ask for all the things each asset DEPENDS on, instead, I want to find out anything that DEPENDS ON XYZ asset.  They don't suppor that.
            if (overrideAssets.Count > 0)
            {
                // Also, this captures all dependencies as an inverted dependency chain, so X -> A,B,C instead is A->X, B->X, C->X.
                // This lets us trivially pull in the parent-dependencies of X.
                Dictionary<string, List<string>> dependencyChain = new Dictionary<string, List<string>>();
                List<string> depList;
                foreach (string assetName in allBundleableAssets)
                {
                    string[] deps = AssetDatabase.GetDependencies(assetName);
                    foreach (string d in deps)
                    {
                        string cleanS = d.Replace('\\', '/').ToLowerInvariant();  // always work in lower case and with forward slashes
                        if (dependencyChain.TryGetValue(cleanS, out depList) == false)
                        {
                            depList = new List<string>();
                            dependencyChain.Add(cleanS, depList);
                        }
                        depList.Add(assetName);  // put the parent asset in the child's list of dependencies
                    }
                }

                // Now, we create a work-list of assets that we will iteratively pull from and add their parents to.
                Queue<string> workAssets = new Queue<string>(overrideAssets);
                while (workAssets.Count > 0)
                {
                    string childAsset = workAssets.Dequeue();
                    if (dependencyChain.TryGetValue(childAsset, out depList))  // this should ALWAYS find something.
                    {
                        foreach (string parentAsset in depList)  // walk all parent assets that depend on this
                        {
                            if (overrideAssets.Contains(parentAsset) == false)  // don't allow infinite loops
                            {
                                overrideAssets.Add(parentAsset);  // put this in the bundle too
                                workAssets.Enqueue(parentAsset);  // track upward from the parent too
                            }
                        }
                    }
                }
            }

            // At this point, we collected ALL the files that needs to be in the override bundle.  Let's make it.
            string outputPath = ABUtilities.GetRuntimeCacheFolder(ABEditorUtilities.GetBundleVersionFromPackageJson()) + "/";

            // Produce the override bundle, but only if something is in it.
            List<string> bundleOrder = new List<string>();
            Dictionary<string, HashSet<string>> dependencyMatrix = new Dictionary<string, HashSet<string>>();
            AssetBundleManifest unityManifest = null;
            AssetBundleBuild[] buildMap = new AssetBundleBuild[overrideAssets.Count];
            if (overrideAssets.Count > 0)
            {
                // Create an asset bundle for each folder containing all the child assets inside it.
                string bundleName = "override-" + _buildVersion + ".assetbundle";
                buildMap[0].assetBundleName = bundleName;
                buildMap[0].assetNames = new List<string>(overrideAssets).ToArray();
                bundleOrder.Add(bundleName);
                dependencyMatrix.Add(bundleName, new HashSet<string>());

                // Build the override bundle
                unityManifest = BuildPipeline.BuildAssetBundles(outputPath, buildMap, buildInfo.BundleOptions, buildInfo.Target);
                if (unityManifest == null)
                    Debug.LogError("<color=#ff8080>[" + _platformStr + "] Asset bundle build failed for " + buildMap[0].assetBundleName + "</color>");

                // Clean the folder of junk we don't want or need.
                RemoveUnnecessaryFiles(outputPath, false);
            }

            ABManifest overrideManifest = GenerateBundleManifest(outputPath, dependencyMatrix, bundleOrder, buildMap, unityManifest);

            // Write the override manifest
            string manifestJson = JsonUtility.ToJson(overrideManifest);  // convert structure over to json
            string manifestPath = ABUtilities.GetRuntimeCacheFolder(ABEditorUtilities.GetBundleVersionFromPackageJson()) + "/override-" + _buildVersion + ".json";
            File.WriteAllText(manifestPath, manifestJson, System.Text.Encoding.UTF8);
        }

        // If this returns true, ignore this file.
        static private bool ShouldIgnore(string assetPath, bool doLogging, string[] ignoreEndsWith, string[] ignoreContains, string[] ignoreExact)
        {
            foreach (string s in ignoreEndsWith)
            {
                if (assetPath.EndsWith(s))
                {
                    if (doLogging) Debug.Log("<color=#ffff00>Ignoring (EndsWith) [" + assetPath + "] Pattern: " + s + "</color>");
                    return true;
                }
            }
            foreach (string s in ignoreContains)
            {
                if (assetPath.Contains(s))
                {
                    if (doLogging) Debug.Log("<color=#ffff00>Ignoring (Contains) [" + assetPath + "] Pattern: " + s + "</color>");
                    return true;
                }
            }
            foreach (string s in ignoreExact)
            {
                if (assetPath == s)
                {
                    if (doLogging) Debug.Log("<color=#ffff00>Ignoring (Exact) [" + assetPath + "]</color>");
                    return true;
                }
            }
            return false;
        }

        // Check that the main asset type is not null.
        static private bool IsValidAsset(string assetPath)
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
}
