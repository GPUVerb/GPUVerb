using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUVerb
{
    public class GPUVerbContext : SingletonBehavior<GPUVerbContext>
    {
        [SerializeField]
        private Vector2 m_minCorner = new Vector2(0, 0);
        [SerializeField]
        private Vector2 m_maxCorner = new Vector2(10, 10);
        [SerializeField]
        private bool m_useRefClass = true;

        private FDTDBase m_FDTDSolver = null;
        private AnalyzerBase m_AnalyzerSolver = null;


        public Vector2 MinCorner { get => m_minCorner; }
        public Vector2 MaxCorner { get => m_maxCorner; }
        public FDTDBase FDTDSolver { get => m_FDTDSolver; }
        public AnalyzerBase AnalyzerSolver { get => m_AnalyzerSolver; }

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
            }
            else
            {
                m_FDTDSolver = new FDTD(
                    new Vector2Int(Mathf.CeilToInt(m_maxCorner.x), Mathf.CeilToInt(m_maxCorner.y)),
                    PlaneverbResolution.LowResolution);
/*                m_AnalyzerSolver = new Analyzer(
                    new Vector2Int(Mathf.CeilToInt(m_maxCorner.x), Mathf.CeilToInt(m_maxCorner.y)),
                    m_FDTDSolver.GetGridSizeInCells(),
                    PlaneverbResolution.LowResolution,
                    m_FDTDSolver.ID);*/
            }
        }

        public Vector2Int ToGridPos(Vector2 worldPos)
        {
            return m_FDTDSolver.ToGridPos(worldPos);
        }

        public AnalyzerResult? GetOutput(Vector2Int pos)
        {
            if(m_FDTDSolver == null)
            {
                Debug.LogError("FDTD Solver not set");
                return null;
            }
            if (m_AnalyzerSolver == null)
            {
                Debug.LogError("Analyzer not set");
                return null;
            }
            m_FDTDSolver.GenerateResponse(Listener.Position);
            m_AnalyzerSolver.AnalyzeResponses(Listener.Position);
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
