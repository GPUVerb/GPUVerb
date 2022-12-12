using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Text;


namespace GPUVerb
{
    public class AnalyzerTest : MonoBehaviour
    {
        [SerializeField]
        GameObject m_cubePrefab = null;
        [SerializeField]
        float m_cubeSize = 0.1f;
        [SerializeField]
        float m_baseHeight = 0;
        [SerializeField]
        float m_motionScale = 10;

        [SerializeField]
        Transform m_listener = null;


        class AnalyzerInfo
        {
            public GameObject ins;
            public Vector2 pos;
            public AnalyzerResult cur;
        }

        List<AnalyzerInfo> m_cubeInfos = new List<AnalyzerInfo>();

        // Start is called before the first frame update
        private void Start()
        {
            Vector2 minCorner = GPUVerbContext.Instance.MinCorner;
            Vector2 maxCorner = GPUVerbContext.Instance.MaxCorner;

            for (float x = minCorner.x; x <= maxCorner.x; x += m_cubeSize)
            {
                for (float y = minCorner.y; y <= maxCorner.y; y += m_cubeSize)
                {
                    GameObject obj;
                    if (m_cubePrefab == null)
                    {
                        obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Destroy(obj.GetComponent<Collider>());
                    }
                    else
                    {
                        obj = Instantiate(m_cubePrefab);
                    }
                    obj.transform.position = new Vector3(x, m_baseHeight, y);
                    obj.transform.localScale = Vector3.one * m_cubeSize;

                    AnalyzerInfo info = new AnalyzerInfo
                    {
                        ins = obj,
                        pos = new Vector2(x, y)
                    };

                    m_cubeInfos.Add(info);
                }
            }
        }

        void GetData()
        {
            AnalyzerBase solver = GPUVerbContext.Instance.AnalyzerSolver;
            FDTDBase FDTDsolver = GPUVerbContext.Instance.FDTDSolver;

            FDTDsolver.GenerateResponse(m_listener.position);


            solver.AnalyzeResponses(FDTDsolver.GetGrid(), m_listener.position);

            foreach (AnalyzerInfo info in m_cubeInfos)
            {
                info.cur = solver.GetAnalyzerResponse(FDTDsolver.ToGridPos(info.pos));
            }
        }

        void Simulate()
        {
            GetData();

            foreach (AnalyzerInfo info in m_cubeInfos)
            {
                AnalyzerResult data = info.cur;
                float occlusion = data.sourceDirectivity.x;
                float h = m_baseHeight + m_motionScale * occlusion;
                info.ins.transform.position = new Vector3(info.pos.x, h, info.pos.y);
            }

        }


        private void OnGUI()
        {
            if (GUILayout.Button("Play"))
            {
                Simulate();
            }
        }

        private void OnDrawGizmos()
        {
            if (m_listener != null)
            {
                Gizmos.DrawWireSphere(m_listener.position, 0.2f);
            }
        }
    }
}