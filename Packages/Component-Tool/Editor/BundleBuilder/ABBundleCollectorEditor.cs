using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Baidu.Meta.ComponentsTool.Editor
{
    [CustomEditor(typeof(ABBundleCollector))]
    public class ABBundleCollectorEditor : UnityEditor.Editor
    {
        ABBundleCollector group;
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("提示：" +
                                     "\n1.将需要打包bundle的文件夹拖到下面的Bundles内，" +
                                     "\n   文件夹内及子文件夹内所有文件会被打包成一个bundle；" +
                                     "\n2.文件夹建议按类型分类存放资产，避免循环引用；" +
                                     "\n3.各个平台共用的数资数资放到一个文件夹，平台独有的放到单独文件夹" +
                                     "\n4.场景文件需要单独放到一个文件夹，不允许一个文件夹内有场景又有其他资源；" +
                                     "\n5.父文件夹已经被添加后，如果再添加子文件夹或者文件，打包将失败!" +
                                     "\n6.Bundle的版本号读取的是package包内package.json的版本号；", MessageType.Info);

            base.OnInspectorGUI();

            group = target as ABBundleCollector;
            if (group == null)
            {
                return;
            }

            if (GUILayout.Button("BuildBundles"))
            {
                CheckAssets(group);
            }
            
        }


        private void CheckAssets(ABBundleCollector group)
        {
            //EditorUtility.DisplayDialog("Error", "find duplicate bundle assets", "OK");

            //ABErrorDialog.InitWindow("这是一个警告");
            group.CollectAssets(group, (platform, assetsPerBundle) =>
            {
                Debug.Log($"platform: {platform}, bundles count: {assetsPerBundle.Count}");

                ABMenuItems.OnCollect(platform, assetsPerBundle);
            });
        }

    }
}