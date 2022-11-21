using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GPUVerb
{
    public class Geometry
    {
        private GameObject m_owner;
        private Collider m_collider;

        public Geometry(GameObject owner)
        {
            m_owner = owner;
            m_collider = owner.GetComponent<Collider>();
        }
        public void UpdatePos(Vector3 pos)
        {
            
        }
    }

}

