using UnityEngine;
using System.Collections;

namespace Kalagaan
{
    namespace HairDesignerExtension
    {
        public class HairDesignerFurDemo2 : MonoBehaviour {

            public HairDesigner m_hd;
            public float m_timer = 2f;
            public int m_id = 0;
            float m_lastTime = 0f;
            void Start() {

                for (int i = 0; i < m_hd.m_generators.Count; ++i)
                    m_hd.GetLayer(i).m_enable = i == m_id;
            }


            void Update()
            {
                if (Time.time > m_lastTime + m_timer)
                {
                    m_lastTime = Time.time;
                    m_hd.GetLayer(m_id).m_enable = false;
                    m_id++;
                    m_id %= m_hd.m_generators.Count;
                    m_hd.GetLayer(m_id).m_enable = true;
                }

            }
        }
    }
}
