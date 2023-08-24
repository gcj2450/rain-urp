using UnityEditor;

namespace Baidu.Meta.ComponentsTool.Editor
{
    /// <summary>
    ///  configuration data
    /// </summary>
    public struct ABBuildInfo
    {
        public ABBuildInfo(BuildTarget target, string targetString,  BuildAssetBundleOptions bundleOptions, string srcBundleFolder,
                            string destBundleFolder, string destConfigFolder, string configJson, bool selfContainedBuild, string embedBundleFolder, string buildVersion, bool doLogging,
                            string[] ignoreEndsWith, string[] ignoreContains, string[] ignoreExact)
        {
            Target = target;
            TargetString = targetString;
            BundleOptions = bundleOptions;
           
            SrcBundleFolder = srcBundleFolder;
            DestBundleFolder = destBundleFolder;
            DestConfigFolder = destConfigFolder;
            ConfigJson = configJson;
            SelfContainedBuild = selfContainedBuild;
            EmbedBundleFolder = embedBundleFolder;
            BuildVersion = buildVersion;

            Logging = doLogging;
            IgnoreEndsWith = ignoreEndsWith;
            IgnoreContains = ignoreContains;
            IgnoreExact = ignoreExact;
        }
        public BuildTarget Target { get; private set; }
        public string TargetString { get; private set; }
        public BuildAssetBundleOptions BundleOptions { get; private set; }

        public string SrcBundleFolder { get; private set; }
        public string DestBundleFolder { get; private set; }
        public string DestConfigFolder { get; private set; }
        public string ConfigJson { get; private set; }
        public bool SelfContainedBuild { get; private set; }
        public string EmbedBundleFolder { get; private set; }
        public string BuildVersion { get; private set; }

        public bool Logging { get; private set; }
        public string[] IgnoreEndsWith { get; private set; }
        public string[] IgnoreContains { get; private set; }
        public string[] IgnoreExact { get; private set; }
    }
}
