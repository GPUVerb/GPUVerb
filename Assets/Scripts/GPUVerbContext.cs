using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUVerb
{
    public class GPUVerbContext : MonoBehaviour
    {
        public static GPUVerbContext Instance { get; private set; }
        private static GPUVerbContext s_instance;
        private void Awake()
        {
            if(Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
                Init();
            }
        }

        [SerializeField]
        private Vector2 m_minCorner = new Vector2(0, 0);
        [SerializeField]
        private Vector2 m_maxCorner = new Vector2(9, 9);
        private FDTDBase m_FDTDSolver;


        public Vector2 MinCorner { get => m_minCorner; }
        public Vector2 MaxCorner { get => m_maxCorner; }
        public FDTDBase FDTDSolver { get => m_FDTDSolver; }

        private void Init()
        {
            m_FDTDSolver = new FDTDRef(
                new Vector2Int(Mathf.CeilToInt(m_maxCorner.x), Mathf.CeilToInt(m_maxCorner.y)),
                PlaneverbResolution.LowResolution);
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
