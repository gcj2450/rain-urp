#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    internal class LitAndMatcap : BaseShaderGUI
    {
        static readonly string[] workflowModeNames = Enum.GetNames(typeof(LitGUI.WorkflowMode));

        private LitGUI.LitProperties       litProperties;
        private LitDetailGUI.LitProperties litDetailProperties;

        private MaterialProperty m_MainLightHeight;
        private MaterialProperty m_MatCapMap;
        private MaterialProperty m_MatCapMapStrength;
        private MaterialProperty m_MatCapMapColor;
        private MaterialProperty m_MatCapMapShadowColor;
        private MaterialProperty m_MatCapMapShadowStrength;
        private MaterialProperty m_MatCapMap2;
        private MaterialProperty m_MatCapMap2Strength;
        private MaterialProperty m_MatCapSpecularHeight;
        private MaterialProperty m_ALPHATEST_OPEN;
        private MaterialProperty m_NOLIGHT_ON;
        
        // MaterialHeaderScopeList m_MaterialScopeList = new MaterialHeaderScopeList(uint.MaxValue & ~(uint)Expandable.Advanced);
        
        public static readonly GUIContent HairLabel = EditorGUIUtility.TrTextContent("MatCap Light",
            "");

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            litProperties             = new LitGUI.LitProperties(properties);
            m_MainLightHeight         = FindProperty("_MainLightHeight",               properties);
            m_MatCapMap               = FindProperty("_MatCapMap",               properties);
            m_MatCapMapStrength       = FindProperty("_MatCapMapStrength",       properties);
            m_MatCapMapColor          = FindProperty("_MatCapMapColor",          properties);
            m_MatCapMapShadowColor    = FindProperty("_MatCapMapShadowColor",    properties);
            m_MatCapMapShadowStrength = FindProperty("_MatCapMapShadowStrength", properties);
            m_MatCapMap2              = FindProperty("_MatCapMap2",              properties);
            m_MatCapMap2Strength      = FindProperty("_MatCapMap2Strength",      properties);
            m_MatCapSpecularHeight    = FindProperty("_MatCapSpecularHeight",    properties);
            m_MatCapMap2              = FindProperty("_MatCapMap2",              properties);
            m_ALPHATEST_OPEN          = FindProperty("_ALPHATEST_OPEN",          properties);
            m_NOLIGHT_ON              = FindProperty("_NOLIGHT_ON",              properties);
            
        }

        // material changed check
        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, LitGUI.SetMaterialKeywords, LitDetailGUI.SetMaterialKeywords);
            //强制打开法线通道防止出现无法获取切线报错
            CoreUtils.SetKeyword(material, "_NORMALMAP", true);
        }
        
        
        //添加一个折叠
        public override void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList)
        {
            materialScopesList.RegisterHeaderScope(HairLabel, 16, DrawHairOptions);
        }
        
        public  void DrawHairOptions(Material material)
        {
            materialEditor.ShaderProperty(m_MainLightHeight,               "调整主光源高度");
            materialEditor.ShaderProperty(m_MatCapMap,               "间接照明MatCapMap贴图");
            materialEditor.ShaderProperty(m_MatCapMapStrength,       "间接照明MatCapMap强度");
            materialEditor.ShaderProperty(m_MatCapMapColor,          "间接照明MatCapMap颜色");
            materialEditor.ShaderProperty(m_MatCapMapShadowColor,    "间接照明MatCapMap阴影颜色");
            materialEditor.ShaderProperty(m_MatCapMapShadowStrength, "间接照明MatCapMap阴影强度");
            materialEditor.ShaderProperty(m_MatCapMap2,              "直接照明MatCapMap贴图");
            materialEditor.ShaderProperty(m_MatCapMap2Strength,      "直接照明MatCapMap贴图强度");
            materialEditor.ShaderProperty(m_MatCapSpecularHeight,    "假高光的高度");
            materialEditor.ShaderProperty(m_ALPHATEST_OPEN,          "间接光MatCapMap开关");
            materialEditor.ShaderProperty(m_NOLIGHT_ON,              "无光兼容开关");
        }
        
        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            LitGUI.Inputs(litProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        // material main advanced options
        public override void DrawAdvancedOptions(Material material)
        {
            base.DrawAdvancedOptions(material);
            if (litProperties.reflections != null && litProperties.highlights != null)
            {
                materialEditor.ShaderProperty(litProperties.highlights,  LitGUI.Styles.highlightsText);
                materialEditor.ShaderProperty(litProperties.reflections, LitGUI.Styles.reflectionsText);
            }
        }


    }
}
#endif