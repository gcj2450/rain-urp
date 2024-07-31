using UnityEngine;
using System.Collections;
using Kalagaan.HairDesignerExtension;
using UnityEngine.UI;

public class HairDesignerFurDemo : MonoBehaviour {

    public HairDesigner m_hairDesigner;
    //public string m_layerName = "Fur";


    public void Start()
    {
        m_hairDesigner.GetLayer(0).m_enable = false;
    }


    bool shell = false;
    public void SelectFur()
    {
        m_hairDesigner.GetLayer(0).m_enable = !shell;
        m_hairDesigner.GetLayer(2).m_enable = shell;
        shell = !shell;
    }



	public void SetDensity ( Slider slider )
    {
        HairDesignerShaderProcedural hdsp = m_hairDesigner.GetLayer("Fur Polygons").GetShaderParams() as HairDesignerShaderProcedural;
        if(hdsp!=null)
            hdsp.m_hairDensity = Mathf.Lerp(0, 50, slider.value);
        HairDesignerShaderProcedural_v2  hdsp2 = m_hairDesigner.GetLayer("Fur Polygons").GetShaderParams() as HairDesignerShaderProcedural_v2;
        if (hdsp2 != null)
            hdsp2.m_hairDensity = Mathf.Lerp(0, 50, slider.value);

        HairDesignerShaderFurShell fs = m_hairDesigner.GetLayer("Fur Shells").GetShaderParams() as HairDesignerShaderFurShell;
        fs.m_furLength = Mathf.Lerp(.1f, .2f, slider.value);
    }
}
