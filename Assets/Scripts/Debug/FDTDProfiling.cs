using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;


namespace GPUVerb
{
    public class FDTDProfiling : MonoBehaviour
    {
        [SerializeField] Vector2 m_gridMaxCorner = new Vector2(10,10);
        FDTDBase m_solver = null;
        double m_time = 0;

        void Start()
        {
            m_solver = new FDTDRef(m_gridMaxCorner, PlaneverbResolution.MidResolution);
        }

        // Update is called once per frame
        void Update()
        {
            Stopwatch sw = Stopwatch.StartNew();

            GPUVerbContext.Instance.FDTDSolver.GenerateResponse(Vector3.zero);
            sw.Stop();
            m_time = sw.Elapsed.TotalMilliseconds;
        }

        void OnGUI()
        {
            GUILayout.Label($"time = {m_time} ms");
        }
    }
}
