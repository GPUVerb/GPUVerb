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
        bool m_lastWinthinHead = false;

        int m_geomID = FDTDBase.k_invalidGeomID;
        Collider[] m_colliders = null;
        Bounds m_bounds = new Bounds(Vector3.zero, Vector3.zero);

        public AbsorptionCoefficient Absorption { get => m_absorption; set => m_absorption = value; }

        // get rasterized bounds based on player head plane
        bool IsWithinPlayerHeadSlice()
        {
            float headY = Listener.Position.y;
            float thisY = m_bounds.center.y;
            float halfHeight = m_bounds.extents.y; // extents are half sizes
            return (thisY - halfHeight) <= headY && (thisY + halfHeight) >= headY;
        }
        public Bounds GetBounds() => m_bounds;
        public PlaneVerbAABB GetPlaneverbBounds()
        {
            return new PlaneVerbAABB(m_bounds, AbsorptionConstants.GetAbsorption(m_absorption));
        }

        // Start is called before the first frame update
        void Start()
        {
            RecalculateBounds();

            m_lastTransformState = new TransformState(transform);
            m_lastAbsorption = m_absorption;

            if(IsWithinPlayerHeadSlice())
            {
                m_geomID = GPUVerbContext.Instance.AddGeometry(GetPlaneverbBounds());
                m_lastWinthinHead = true;
            }
            else
            {
                m_lastWinthinHead = false;
            }
        }

        private void Update()
        {
            bool withinHead = IsWithinPlayerHeadSlice();
            if(m_lastWinthinHead && !withinHead)
            {
                GPUVerbContext.Instance.UpdateGeometry(m_geomID, PlaneVerbAABB.s_empty);
            }
            else if(!m_lastWinthinHead && withinHead)
            {
                if(m_geomID == FDTDBase.k_invalidGeomID)
                {
                    m_geomID = GPUVerbContext.Instance.AddGeometry(GetPlaneverbBounds());
                }
                else
                {
                    GPUVerbContext.Instance.UpdateGeometry(m_geomID, GetPlaneverbBounds());
                }
            }
            m_lastWinthinHead = withinHead;

            if(withinHead)
            {
                TransformState curState = new TransformState(transform);
                if (!curState.Equals(m_lastTransformState))
                {
                    RecalculateBounds();
                    GPUVerbContext.Instance.UpdateGeometry(m_geomID, GetPlaneverbBounds());
                    m_lastTransformState = curState;
                }
                if (m_absorption != m_lastAbsorption)
                {
                    GPUVerbContext.Instance.UpdateGeometry(m_geomID, GetPlaneverbBounds());
                    m_lastAbsorption = m_absorption;
                }
            }
        }
        private void OnDestroy()
        {
            GPUVerbContext.Instance.RemoveGeometry(m_geomID);
        }

        private void RecalculateBounds()
        {
            if(m_colliders == null)
            {
                m_colliders = GetComponentsInChildren<Collider>();
            }
            m_bounds = new Bounds(transform.position, Vector3.zero);
            foreach (Collider collider in m_colliders)
            {
                m_bounds.Encapsulate(collider.bounds);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Color save = Gizmos.color;
            if(EditorApplication.isPlaying && IsWithinPlayerHeadSlice())
            {
                Gizmos.color = Color.green;
            }

            RecalculateBounds();
            Gizmos.DrawWireCube(m_bounds.center, m_bounds.size);
            Handles.Label(m_bounds.center, Enum.GetName(typeof(AbsorptionCoefficient), m_absorption));

            Gizmos.color = save;
        }
#endif
    }
}