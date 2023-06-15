/****************************************************
    文件：SceneModifyMgr.cs
    作者：#CREATEAUTHOR#
    邮箱:  gaocanjun@baidu.com
    日期：#CREATETIME#
    功能：Todo
*****************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 支持修改的类型
/// </summary>
public enum ModifyType
{
    None,
    UIText,
    UIImage,
    UIRawImage,
    MaterialColor,
    MaterialTexture,
    Model
}

/// <summary>
/// 可修改物体信息
/// 根据ModifyType类型，填充不同信息
/// GameObjectPath是必填项
/// </summary>
[Serializable]
public class ModifyInfo
{
    /// <summary>
    /// 被修改物体路径从根节点开始
    /// </summary>
    public string GameObjectPath = "";
    /// <summary>
    /// 修改的类型
    /// </summary>
    public ModifyType ModType = ModifyType.None;
    /// <summary>
    /// 修改的材质球索引,修改类型为材质颜色或者材质贴图时必须填
    /// </summary>
    public int MaterialIndex = 0;
    /// <summary>
    ///修改的属性名，修改类型为材质颜色或者材质贴图时必须填
    /// </summary>
    public string MaterialPropertyName = "_BaseColor";
}

public class ModifyInfoCollector : MonoBehaviour
{
    public List<ModifyInfo> ModifyObjList = new List<ModifyInfo>();
}
