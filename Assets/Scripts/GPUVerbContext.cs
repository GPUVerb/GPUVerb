using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GPUVerb
{
    public class BackgroundWorker
    {
        private GPUVerbContext m_context;

        // write-only by the game thread, read only by Background Worker
        public Vector3 ListenerPos { private get; set; }
        // current processed position
        public Vector3 m_Pos;

        private bool m_terminate;
        private AnalyzerResult[,] m_result;

        public BackgroundWorker(GPUVerbContext context)
        {
            m_result = null;
            m_context = context;
            m_terminate = false;
            m_Pos = ListenerPos = Vector3.zero;
        }
        public void Terminate()
        {
            m_terminate = true;
        }
        
        public void Execute()
        {
            while(!m_terminate)
            {
                if (m_context.FDTDSolver == null)
                {
                    Debug.LogError("FDTD Solver not set");
                    continue;
                }
                if (m_context.AnalyzerSolver == null)
                {
                    Debug.LogError("Analyzer not set");
                    continue;
                }
                if (ListenerPos != m_Pos)
                {
                    m_Pos = ListenerPos;

                    m_context.FDTDSolver.GenerateResponse(m_Pos);
                    m_context.AnalyzerSolver.AnalyzeResponses(m_Pos);

                    // cache result (to prevent game thread from reading a grid that is being written to)
                    AnalyzerResult[,] grid = m_context.AnalyzerSolver.GetGrid();

                    if (m_result == null)
                    {
                        m_result = new AnalyzerResult[grid.GetLength(0), grid.GetLength(1)];
                    }
                    Array.Copy(grid, m_result, grid.GetLength(0) * grid.GetLength(1));
                }
            }
        }

        public AnalyzerResult? GetResponse(Vector2Int pos)
        {
            if (m_result == null)
            {
                return null;
            }
            if (pos.x >= m_result.GetLength(0) || pos.x < 0 || pos.y >= m_result.GetLength(1) || pos.y < 0)
            {
                Debug.Log("Access outside of Analyzer Grid");
                return null;
            }

            return m_result[pos.x, pos.y];
        }
    }


    public class GPUVerbContext : SingletonBehavior<GPUVerbContext>
    {
        [SerializeField]
        private Vector2 m_minCorner = new Vector2(0, 0);
        [SerializeField]
        private Vector2 m_maxCorner = new Vector2(10, 10);
        [SerializeField]
        private DSPConfig m_dspConfig = new DSPConfig();
        
        [SerializeField]
        private bool m_useRefClass = true;

        private FDTDBase m_FDTDSolver = null;
        private AnalyzerBase m_AnalyzerSolver = null;
        private DSPBase m_DSP = null;


        private Thread m_backgroundWorkerThread = null;
        private BackgroundWorker m_backgroundWorker = null;

        public Vector2 MinCorner { get => m_minCorner; }
        public Vector2 MaxCorner { get => m_maxCorner; }
        public FDTDBase FDTDSolver { get => m_FDTDSolver; }
        public AnalyzerBase AnalyzerSolver { get => m_AnalyzerSolver; }
        public DSPBase DSP { get => m_DSP; }


        protected override void Init()
        {
            if(m_useRefClass)
            {
                m_FDTDSolver = new FDTDRef(
                    new Vector2Int(Mathf.RoundToInt(m_maxCorner.x), Mathf.RoundToInt(m_maxCorner.y)),
                    PlaneverbResolution.LowResolution);
                m_AnalyzerSolver = new AnalyzerRef(
                    new Vector2Int(Mathf.RoundToInt(m_maxCorner.x), Mathf.RoundToInt(m_maxCorner.y)),
                    m_FDTDSolver.GetGridSizeInCells(),
                    PlaneverbResolution.LowResolution,
                    m_FDTDSolver.ID);
                m_DSP = new DSPRef(m_dspConfig);
            }
            else
            {
                m_FDTDSolver = new FDTD(
                    new Vector2Int(Mathf.CeilToInt(m_maxCorner.x), Mathf.CeilToInt(m_maxCorner.y)),
                    PlaneverbResolution.LowResolution);
            }

            m_backgroundWorker = new BackgroundWorker(this);
            m_backgroundWorkerThread = new Thread(new ThreadStart(m_backgroundWorker.Execute));
            m_backgroundWorkerThread.Start();
        }

        public Vector2Int ToGridPos(Vector2 worldPos)
        {
            return m_FDTDSolver.ToGridPos(worldPos);
        }

        public void UpdateListener(Transform listenerTransform)
        {
            m_backgroundWorker.ListenerPos = listenerTransform.position;
            m_DSP.SetListenerPos(listenerTransform.position, listenerTransform.forward);
        }

        public AnalyzerResult? GetOutput(Vector2Int pos)
        {
            return m_backgroundWorker.GetResponse(pos);
        }
        private void OnDestroy()
        {
            m_backgroundWorker.Terminate();
            m_backgroundWorkerThread.Join();

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
            Gizmos.DrawLine(new Vector3(m_minCorner.x, 0, m_minCorner.y), new Vector3(m_minCorner.x, 0, m_maxCorner.y));
            Gizmos.DrawLine(new Vector3(m_minCorner.x, 0, m_maxCorner.y), new Vector3(m_maxCorner.x, 0, m_maxCorner.y));
            Gizmos.DrawLine(new Vector3(m_maxCorner.x, 0, m_maxCorner.y), new Vector3(m_maxCorner.x, 0, m_minCorner.y));
            Gizmos.DrawLine(new Vector3(m_maxCorner.x, 0, m_minCorner.y), new Vector3(m_minCorner.x, 0, m_minCorner.y));
        }
    }
}
