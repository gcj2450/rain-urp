/****************************************************
    文件：ModifyInfoCollectorEditor.cs
    作者：#CREATEAUTHOR#
    邮箱:  gaocanjun@baidu.com
    日期：#CREATETIME#
    功能：Todo
*****************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Design;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(ModifyInfoCollector))]
public class ModifyInfoCollectorEditor : UnityEditor.Editor
{
    ModifyInfoCollector group;

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("提示：添加完收集到信息后，点击Verify按钮，验证设置是否正确", MessageType.Info);
        base.OnInspectorGUI();
        group = target as ModifyInfoCollector;
        if (group == null)
        {
            return;
        }

        if (GUILayout.Button("Verify"))
        {
            for (int i = 0, cnt = group.ModifyObjList.Count; i < cnt; i++)
            {
                ModifyInfo modInfo = group.ModifyObjList[i];
                GameObject go = GameObject.Find(modInfo.GameObjectPath);
                if (go == null)
                {
                    Debug.LogError($"{modInfo.GameObjectPath} Verify Failed");
                    break;
                }
                else
                {
                    switch (modInfo.ModType)
                    {
                        case ModifyType.UIText:
                            if (go.GetComponent<Text>() == null)
                            {
                                Debug.LogError($"{go.name} Verify Failed");
                            }
                            break;
                        case ModifyType.UIImage:
                            if (go.GetComponent<Image>() == null)
                            {
                                Debug.LogError($"{go.name} Verify Failed");
                            }
                            break;
                        case ModifyType.UIRawImage:
                            if (go.GetComponent<RawImage>() == null)
                            {
                                Debug.LogError($"{go.name} Verify Failed");
                            }
                            break;
                        case ModifyType.MaterialColor:
                        case ModifyType.MaterialTexture:
                            if (go.GetComponent<Renderer>() == null)
                            {
                                Debug.LogError($"{go.name} Verify Failed");
                            }
                            else
                            {
                                if (go.GetComponent<Renderer>() != null&& modInfo.MaterialIndex>go.GetComponent<Renderer>().sharedMaterials.Length-1)
                                {
                                    Debug.LogError($"{go.name} Verify Failed");
                                }
                                if (string.IsNullOrEmpty( modInfo.MaterialPropertyName))
                                {
                                    Debug.LogError($"{go.name} Verify Failed");
                                }
                                else
                                {
                                    if (go.GetComponent<Renderer>() != null && modInfo.MaterialIndex <= go.GetComponent<Renderer>().sharedMaterials.Length - 1)
                                    {
                                        if (!go.GetComponent<Renderer>().sharedMaterials[modInfo.MaterialIndex].HasProperty(modInfo.MaterialPropertyName))
                                        {
                                            Debug.LogError($"{go.name} Verify Failed");
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            Debug.Log("Verify End");
        }
    }
}
