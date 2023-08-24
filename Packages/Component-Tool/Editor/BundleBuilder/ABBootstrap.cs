using System;
using UnityEngine;

namespace Baidu.Meta.ComponentsTool.Editor
{
    /// <summary>
    /// 用于指向Bundle包真实位置的配置文件
    /// 这个会生成一个 "config_{PLATFORM}.json" 文件, 文件定义了Bundle包所在地址
    /// </summary>
    [Serializable]
    public class ABBootstrap
    {
        public ABBootstrap() { }
        public ABBootstrap(string _cdnBundleUrl) 
        {
            cdnBundleUrl = _cdnBundleUrl;
        }
        public ABBootstrap(ABBootstrap copyFrom)
        {
            cdnBundleUrl = copyFrom.cdnBundleUrl;
        }

        [Tooltip("All this data will be stored in a config_{platform}.json file which you will need to host on a website.  " +
            "It gets fetched first as a means to finding the rest of the game assets. " +
            " Specifically, this URL should point to where you host asset bundles, " +
            "something like https://somecdn.cloudfront.net/game/{PLATFORM}/Bundles/")]
        public string cdnBundleUrl;
        public long totalFileSize = 0;
        /// <summary>
        /// the package of these bundles belong to
        /// </summary>
        //public string packageName;
        //	public string statsUrl;
        //	public string matchmakingUrl;
        //	public string anythingElseYouMightWant;
    }
}