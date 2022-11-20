using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


namespace GPUVerb
{
    public class FDTDTest : MonoBehaviour
    {
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern float PlaneverbGetResponsePressure(int gridId, float x, float z, IntPtr result);


        [SerializeField]
        float m_cubeSize = 0.1f;
        [SerializeField]
        float m_baseHeight = 1;
        [SerializeField]
        float m_motionScale = 50;
        [SerializeField]
        float m_simTime = 4f;
        
        [SerializeField]
        Transform m_listener;

        bool m_simFinished = true;

        class Info
        {
            public GameObject ins;
            public Vector2 pos;
            public IEnumerator<Cell> cur;
        }

        List<Info> m_cubeInfos = new List<Info>();


        // Start is called before the first frame update
        void Start()
        {
            Vector2 minCorner = GPUVerbContext.Instance.MinCorner;
            Vector2 maxCorner = GPUVerbContext.Instance.MaxCorner;

            for (float x = minCorner.x; x <= maxCorner.x; x += m_cubeSize)
            {
                for (float y = minCorner.y; y <= maxCorner.y; y += m_cubeSize)
                {
                    GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.transform.position = new Vector3(x, m_baseHeight, y);
                    obj.transform.localScale = Vector3.one * m_cubeSize;
                    
                    Destroy(obj.GetComponent<Collider>());

                    Info info = new Info
                    {
                        ins = obj,
                        pos = new Vector2(x, y)
                    };

                    m_cubeInfos.Add(info);
                }
            }
        }


        float[][] pressures;
        void GetData()
        {
            FDTDBase solver = GPUVerbContext.Instance.FDTDSolver;

            solver.GenerateResponse(m_listener.position);

            foreach (Info info in m_cubeInfos)
            {
                info.cur = solver.GetResponse(solver.ToGridPos(info.pos)).GetEnumerator();
            }
            
            /*
            pressures = new float[m_cubeInfos.Count][];
            for(int i=0; i<m_cubeInfos.Count; ++i)
            {
                pressures[i] = new float[m_solver.GetResponseLength()];
                unsafe
                {
                    fixed(float* ptr = pressures[i])
                    {
                        PlaneverbGetResponsePressure(0, m_cubeInfos[i].pos.x, m_cubeInfos[i].pos.y, (IntPtr)ptr);
                    }
                }
            }
            */
        }

        IEnumerator Simulate()
        {
            m_simFinished = false;
            GetData();

            int numSamples = GPUVerbContext.Instance.FDTDSolver.GetResponseLength();
            int lastSample = -1;
            for(float curTime = 0; curTime <= m_simTime; curTime += Time.deltaTime)
            {
                int sample = Mathf.FloorToInt(curTime / m_simTime * numSamples);
                if(sample >= numSamples)
                {
                    break;
                }

                foreach (Info info in m_cubeInfos)
                {
                    if(lastSample != sample)
                    {
                        info.cur.MoveNext();
                    }

                    Cell data = info.cur.Current;
                    float pr = data.pressure;
                    float h = m_baseHeight + m_motionScale * pr;
                    info.ins.transform.position = new Vector3(info.pos.x, h, info.pos.y);
                }

                lastSample = sample;
                yield return new WaitForEndOfFrame();
            }

            m_simFinished = true;
        }

        private void OnGUI()
        {
            if(m_simFinished)
            {
                if (GUILayout.Button("Play"))
                {
                    StopAllCoroutines();
                    StartCoroutine(Simulate());
                }
            } else
            {
                GUILayout.Label("Simulating...");
            }
        }
    }
}
