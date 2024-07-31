#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    internal class LitBackSpecular : BaseShaderGUI
    {
        static readonly string[] workflowModeNames = Enum.GetNames(typeof(LitGUI.WorkflowMode));

        private LitGUI.LitProperties       litProperties;
        private LitDetailGUI.LitProperties litDetailProperties;

        private MaterialProperty m_BackSpecularIntension;
        private MaterialProperty m_BackSpecularRange;
        private MaterialProperty m_BackSpecularNormal;
        // private MaterialProperty m_FrontDistortion;
        // private MaterialProperty m_FrontInte;
        // private MaterialProperty m_TranslucentAreaMap;
        // // private MaterialProperty m_SSSAreaMap;
        //
        // private MaterialProperty m_ShiftMap;
        // private MaterialProperty m_Specular1Color;
        // private MaterialProperty m_Specular1;
        // private MaterialProperty m_SpecOffset1;
        // private MaterialProperty m_SpecNoise1;
        // private MaterialProperty m_SpecularExponent1;
        //
        // private MaterialProperty m_Specular2Color;
        // private MaterialProperty m_Specular2;
        // private MaterialProperty m_SpecOffset2;
        // private MaterialProperty m_SpecNoise2;
        // private MaterialProperty m_SpecularExponent2;
        //
        // private MaterialProperty m_BackIntansity;
        // private MaterialProperty m_AddHairAlpha;
        // private MaterialProperty m_AddHairskewing;
        // private MaterialProperty m_AddHairskewingNoise;
        
        // MaterialHeaderScopeList m_MaterialScopeList = new MaterialHeaderScopeList(uint.MaxValue & ~(uint)Expandable.Advanced);
        
        public static readonly GUIContent HairLabel = EditorGUIUtility.TrTextContent("Back Light",
            "");

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            litProperties           = new LitGUI.LitProperties(properties);
            m_BackSpecularIntension = FindProperty("_BackSpecularIntension", properties);
            m_BackSpecularRange     = FindProperty("_BackSpecularRange",     properties);
            m_BackSpecularNormal    = FindProperty("_BackSpecularNormal",     properties);
            // m_FrontInte          = FindProperty("_FrontInte",          properties);
            // m_TranslucentAreaMap = FindProperty("_TranslucentAreaMap", properties);
            //
            //
            //
            // m_ShiftMap          = FindProperty("_ShiftMap",         properties);
            // m_Specular1Color    = FindProperty("_Specular1Color",        properties);
            // m_Specular1         = FindProperty("_Specular1",        properties);
            // m_SpecOffset1       = FindProperty("_SpecOffset1",   properties);
            // m_SpecNoise1        = FindProperty("_SpecNoise1",   properties);
            // m_SpecularExponent1 = FindProperty("_SpecularExponent1", properties);
            //
            // m_Specular2Color    = FindProperty("_Specular2Color",        properties);
            // m_Specular2         = FindProperty("_Specular2",        properties);
            // m_SpecOffset2       = FindProperty("_SpecOffset2",   properties);
            // m_SpecNoise2        = FindProperty("_SpecNoise2",   properties);
            // m_SpecularExponent2 = FindProperty("_SpecularExponent2", properties);
            //
            // m_BackIntansity       = FindProperty("_BackIntansity",       properties);
            // m_AddHairAlpha        = FindProperty("_AddHairAlpha",        properties);
            // m_AddHairskewing      = FindProperty("_AddHairskewing",      properties);
            // m_AddHairskewingNoise = FindProperty("_AddHairskewingNoise", properties);
            
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
            materialEditor.ShaderProperty(m_BackSpecularIntension, "烘培高光强度");
            materialEditor.ShaderProperty(m_BackSpecularRange,     "烘培高光区域");
            materialEditor.ShaderProperty(m_BackSpecularNormal,   "烘培高光法线强度");
            // materialEditor.ShaderProperty(m_ShiftMap,          "高光扰动贴图");
            //
            // materialEditor.ShaderProperty(m_Specular1Color,    "高光1颜色");
            // materialEditor.ShaderProperty(m_Specular1,         "高光1强度");
            // materialEditor.ShaderProperty(m_SpecOffset1,       "高光1偏移");
            // materialEditor.ShaderProperty(m_SpecNoise1,        "高光1扰动强度");
            // materialEditor.ShaderProperty(m_SpecularExponent1, "高光1区域大小");
            //
            // materialEditor.ShaderProperty(m_Specular2Color,     "高光2颜色");
            // materialEditor.ShaderProperty(m_Specular2,          "高光2强度");
            // materialEditor.ShaderProperty(m_SpecOffset2,        "高光2偏移");
            // materialEditor.ShaderProperty(m_SpecNoise2,         "高光2扰动强度");
            // materialEditor.ShaderProperty(m_SpecularExponent2,  "高光2区域大小");
            //
            // // materialEditor.ShaderProperty(m_TranslucentAreaMap, "透射区域");
            // // materialEditor.ShaderProperty(m_ScattColor,         "透射颜色");
            // // materialEditor.ShaderProperty(m_BackDistortion,     "透射强度衰减");
            // // materialEditor.ShaderProperty(m_FrontInte,          "透射强度");
            //
            // materialEditor.ShaderProperty(m_BackIntansity,  "背面强度");
            //
            // materialEditor.ShaderProperty(m_AddHairskewingNoise,   "添加发丝偏移噪波");
            // materialEditor.ShaderProperty(m_AddHairskewing, "添加发丝偏移");
            // materialEditor.ShaderProperty(m_AddHairAlpha,   "添加发丝透明度");
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