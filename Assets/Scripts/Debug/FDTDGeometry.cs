using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System;
using UnityEditor;
#endif

namespace GPUVerb
{
    [RequireComponent(typeof(Collider))]
    public class FDTDGeometry : MonoBehaviour
    {
        [SerializeField]
        AbsorptionCoefficient m_absorption;

        AbsorptionCoefficient m_lastAbsorption;
        Vector3 m_lastPos = Vector3.zero;


        int m_geomID = -1;
        Collider m_collider = null;
        FDTDBase m_solver = null;

        // Start is called before the first frame update
        void Start()
        {
            m_collider = GetComponent<Collider>();
            m_solver = GPUVerbContext.Instance.FDTDSolver;
            m_geomID = m_solver.AddGeometry(new PlaneVerbAABB(m_collider.bounds, AbsorptionConstants.GetAbsorption(m_absorption)));

            m_lastPos = transform.position;
            m_lastAbsorption = m_absorption;
        }

        private void Update()
        {
            if(transform.position != m_lastPos)
            {
                m_solver.UpdateGeometry(m_geomID, new PlaneVerbAABB(m_collider.bounds, AbsorptionConstants.GetAbsorption(m_absorption)));
                m_lastPos = transform.position;
            }

            if (m_absorption != m_lastAbsorption)
            {
                m_solver.UpdateGeometry(m_geomID, new PlaneVerbAABB(m_collider.bounds, AbsorptionConstants.GetAbsorption(m_absorption)));
                m_lastAbsorption = m_absorption;
            }
        }
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if(m_collider == null)
            {
                m_collider = GetComponent<Collider>();
            }

            Gizmos.DrawWireCube(m_collider.bounds.center, m_collider.bounds.size);
            Handles.Label(m_collider.bounds.center, Enum.GetName(typeof(AbsorptionCoefficient), m_absorption));
        }
#endif
    }
}
