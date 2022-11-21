using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GPUVerb
{
    [RequireComponent(typeof(Collider))]
    public class FDTDGeometry : MonoBehaviour
    {
        Vector3 m_lastPos = Vector3.zero;
        int m_geomID = -1;
        Collider m_collider = null;
        FDTDBase m_solver = null;

        // Start is called before the first frame update
        void Start()
        {
            m_collider = GetComponent<Collider>();
            m_solver = GPUVerbContext.Instance.FDTDSolver;
            m_geomID = m_solver.AddGeometry(m_collider.bounds);

            m_lastPos = transform.position;
        }

        private void Update()
        {
            if(transform.position != m_lastPos)
            {
                m_solver.UpdateGeometry(m_geomID, m_collider.bounds);
            }
            m_lastPos = transform.position;
        }

        private void OnDrawGizmos()
        {
            if(m_collider == null)
            {
                m_collider = GetComponent<Collider>();
            }

            Gizmos.DrawWireCube(m_collider.bounds.center, m_collider.bounds.size);
        }
    }
}
