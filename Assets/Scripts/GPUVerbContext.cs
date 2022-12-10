using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

namespace GPUVerb
{
    public class GPUVerbContext : SingletonBehavior<GPUVerbContext>
    {
        [SerializeField]
        private Vector2 m_minCorner = new Vector2(0, 0);
        [SerializeField]
        private Vector2 m_maxCorner = new Vector2(10, 10);
        [SerializeField]
        private PlaneverbResolution m_simulationRes = PlaneverbResolution.LowResolution;
        [SerializeField]
        private float m_simulationFreq = 10;
        private float m_timePerSimFrame = 1;
        private float m_simTimer = 0;

        [SerializeField]
        private DSPConfig m_dspConfig = new DSPConfig();
        
        [SerializeField]
        private bool m_useRefClass = true;

        private FDTDBase m_FDTDSolver = null;
        private AnalyzerBase m_AnalyzerSolver = null;
        private DSPBase m_DSP = null;
        private Vector2Int m_lastListenerPos = new Vector2Int(-1,-1);
        private Vector3 m_lastListenerForward = Vector3.zero;

        private Vector2Int m_gridSize = Vector2Int.zero;

        public Vector2 MinCorner { get => m_minCorner; }
        public Vector2 MaxCorner { get => m_maxCorner; }
        public PlaneverbResolution SimulationRes { get => m_simulationRes; }

        public FDTDBase FDTDSolver { get => m_FDTDSolver; }
        public AnalyzerBase AnalyzerSolver { get => m_AnalyzerSolver; }
        public DSPBase DSP { get => m_DSP; }

        protected override void Init()
        {
            m_gridSize = new Vector2Int(Mathf.RoundToInt(m_maxCorner.x), Mathf.RoundToInt(m_maxCorner.y));
            m_timePerSimFrame = 1 / m_simulationFreq;

            if (m_useRefClass)
            {
                m_FDTDSolver = new FDTDCPU(m_gridSize, m_simulationRes);
                m_AnalyzerSolver = new AnalyzerRef(m_FDTDSolver);
                m_DSP = new DSPRef(m_dspConfig);
            }
            else
            {
                m_FDTDSolver = new FDTDGPU2(m_gridSize, m_simulationRes);
                m_AnalyzerSolver = new AnalyzerGPU(m_FDTDSolver);
                m_DSP = new DSPRef(m_dspConfig);
            }
        }

        private void Update()
        {
            m_simTimer += Time.deltaTime;
            if(m_simTimer >= m_timePerSimFrame)
            {
                Simulate();
                m_simTimer = 0;
            }
        }

        private void Simulate()
        {
            bool shouldSimulate = m_FDTDSolver.ProcessGeometryUpdates();
            bool shouldProcess = false;

            Vector2Int listenerPos = ToGridPos(Listener.Position);
            if(m_lastListenerPos != listenerPos)
            {
                m_lastListenerPos = listenerPos;
                shouldSimulate = true;
            }
            if(m_lastListenerForward != Listener.Forward)
            {
                m_lastListenerForward = Listener.Forward;
                shouldProcess = true;
            }

            if (shouldSimulate)
            {
                if (m_FDTDSolver == null)
                {
                    Debug.LogError("FDTD Solver not set");
                    return;
                }
                if (m_AnalyzerSolver == null)
                {
                    Debug.LogError("Analyzer not set");
                    return;
                }

                Profiler.BeginSample("FDTD");
                // this call may not be synchronous, so we're not doing precise profiling
                m_FDTDSolver.GenerateResponse(Listener.Position);
                Profiler.EndSample();

                Profiler.BeginSample("Analyzer");
                m_AnalyzerSolver.AnalyzeResponses(m_FDTDSolver.GetGrid(), Listener.Position);
                Profiler.EndSample();

                m_DSP.SetListenerPos(Listener.Position, Listener.Forward);
            }
            else if(shouldProcess)
            {
                m_DSP.SetListenerPos(Listener.Position, Listener.Forward);
            }
        }

        public Vector2Int ToGridPos(Vector2 worldPos)
        {
            return m_FDTDSolver.ToGridPos(worldPos);
        }

        public int AddGeometry(in PlaneVerbAABB bounds)
        {
            return m_FDTDSolver.AddGeometry(bounds);
        }
        public void UpdateGeometry(int id, in PlaneVerbAABB bounds)
        {
            m_FDTDSolver.UpdateGeometry(id, bounds);
        }
        public void RemoveGeometry(int id)
        {
            m_FDTDSolver.RemoveGeometry(id);
        }
        public AnalyzerResult? GetOutput(Vector2Int pos)
        {
            return m_AnalyzerSolver.GetAnalyzerResponse(pos);
        }
        private void OnDestroy()
        {
            if(m_FDTDSolver != null)
            {
                m_FDTDSolver.Dispose();
            }
            if (m_AnalyzerSolver != null)
            {
                m_AnalyzerSolver.Dispose();
            }
            if(m_DSP != null)
            {
                m_DSP.Dispose();
            }
        }

        private void OnDrawGizmos()
        {
            Color save = Gizmos.color;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(m_minCorner.x, 0, m_minCorner.y), new Vector3(m_minCorner.x, 0, m_maxCorner.y));
            Gizmos.DrawLine(new Vector3(m_minCorner.x, 0, m_maxCorner.y), new Vector3(m_maxCorner.x, 0, m_maxCorner.y));
            Gizmos.DrawLine(new Vector3(m_maxCorner.x, 0, m_maxCorner.y), new Vector3(m_maxCorner.x, 0, m_minCorner.y));
            Gizmos.DrawLine(new Vector3(m_maxCorner.x, 0, m_minCorner.y), new Vector3(m_minCorner.x, 0, m_minCorner.y));
            Gizmos.color = save;
        }
    }
}
