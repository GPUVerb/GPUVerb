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
        AbsorptionCoefficient m_absorption = AbsorptionCoefficient.Default;
        AbsorptionCoefficient m_lastAbsorption = AbsorptionCoefficient.Default;

        TransformState m_lastTransformState = new TransformState();

        int m_geomID = -1;
        Collider m_collider = null;
        FDTDBase m_solver = null;

        // Start is called before the first frame update
        void Start()
        {
            m_collider = GetComponent<Collider>();
            m_solver = GPUVerbContext.Instance.FDTDSolver;
            m_geomID = m_solver.AddGeometry(new PlaneVerbAABB(m_collider.bounds, AbsorptionConstants.GetAbsorption(m_absorption)));

            m_lastTransformState = new TransformState(transform);
            m_lastAbsorption = m_absorption;
        }

        private void Update()
        {
            TransformState curState = new TransformState(transform);
            if (!curState.Equals(m_lastTransformState))
            {
                m_solver.UpdateGeometry(m_geomID, new PlaneVerbAABB(m_collider.bounds, AbsorptionConstants.GetAbsorption(m_absorption)));
                m_lastTransformState = curState;
            }

            if (m_absorption != m_lastAbsorption)
            {
                m_solver.UpdateGeometry(m_geomID, new PlaneVerbAABB(m_collider.bounds, AbsorptionConstants.GetAbsorption(m_absorption)));
                m_lastAbsorption = m_absorption;
            }
        }
        private void OnDestroy()
        {
            m_solver.RemoveGeometry(m_geomID);
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
