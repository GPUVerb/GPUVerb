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
        Collider m_collider = null;

        // get rasterized bounds based on player head plane
        bool IsWithinPlayerHeadSlice()
        {
            Bounds b = m_collider.bounds;
            float headY = Listener.Position.y;
            float thisY = b.center.y;
            float halfHeight = b.extents.y; // extents are half sizes
            return (thisY - halfHeight) <= headY && (thisY + halfHeight) >= headY;
        }

        public PlaneVerbAABB GetBounds()
        {
            return new PlaneVerbAABB(m_collider.bounds, AbsorptionConstants.GetAbsorption(m_absorption));
        }

        // Start is called before the first frame update
        void Start()
        {
            m_collider = GetComponent<Collider>();

            m_lastTransformState = new TransformState(transform);
            m_lastAbsorption = m_absorption;

            if(IsWithinPlayerHeadSlice())
            {
                m_geomID = GPUVerbContext.Instance.AddGeometry(GetBounds());
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
                    m_geomID = GPUVerbContext.Instance.AddGeometry(GetBounds());
                }
                else
                {
                    GPUVerbContext.Instance.UpdateGeometry(m_geomID, GetBounds());
                }
            }
            m_lastWinthinHead = withinHead;

            if(withinHead)
            {
                TransformState curState = new TransformState(transform);
                if (!curState.Equals(m_lastTransformState))
                {
                    GPUVerbContext.Instance.UpdateGeometry(m_geomID, GetBounds());
                    m_lastTransformState = curState;
                }
                if (m_absorption != m_lastAbsorption)
                {
                    GPUVerbContext.Instance.UpdateGeometry(m_geomID, GetBounds());
                    m_lastAbsorption = m_absorption;
                }
            }
        }
        private void OnDestroy()
        {
            GPUVerbContext.Instance.RemoveGeometry(m_geomID);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if(m_collider == null)
            {
                m_collider = GetComponent<Collider>();
            }


            Color save = Gizmos.color;
            if(EditorApplication.isPlaying && IsWithinPlayerHeadSlice())
            {
                Gizmos.color = Color.green;
            }

            Gizmos.DrawWireCube(m_collider.bounds.center, m_collider.bounds.size);
            Handles.Label(m_collider.bounds.center, Enum.GetName(typeof(AbsorptionCoefficient), m_absorption));

            Gizmos.color = save;
        }
#endif
    }
}
