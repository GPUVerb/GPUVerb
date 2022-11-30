using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;


namespace GPUVerb
{
    public class FDTDProfiling : MonoBehaviour
    {
        [SerializeField] Vector2 m_gridMaxCorner = new Vector2(10,10);
        
        FDTDRef m_solver = null;
        FDTD m_solverGPU = null;

        double m_time = 0;
        double m_timeGPU = 0;

        void Start()
        {
            m_solver = new FDTDRef(m_gridMaxCorner, PlaneverbResolution.MidResolution);
            m_solverGPU = new FDTD(m_gridMaxCorner, PlaneverbResolution.MidResolution);
        }

        // Update is called once per frame
        void Test()
        {
            Stopwatch sw = Stopwatch.StartNew();

            m_solver.GenerateResponse(Vector3.zero);
            sw.Stop();
            m_time = sw.Elapsed.TotalMilliseconds;


            sw = Stopwatch.StartNew();

            m_solverGPU.GenerateResponse(Vector3.zero);
            sw.Stop();
            m_timeGPU = sw.Elapsed.TotalMilliseconds;
        }

        void OnDestroy()
        {
            m_solver.Dispose();
            m_solverGPU.Dispose();
        }

        void OnGUI()
        {
            if(GUILayout.Button("Test"))
            {
                Test();
            }
            GUILayout.Label($"time            = {m_time} ms");
            GUILayout.Label($"GPU solver time = {m_timeGPU} ms");
        }
    }
}
