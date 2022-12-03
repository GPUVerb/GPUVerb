using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUVerb
{
    public class Listener : SingletonBehavior<Listener>
    {
        public static Vector3 Position => Instance.transform.position;

        TransformState m_lastTransformState = new TransformState();

        private void Start()
        {
            m_lastTransformState = new TransformState(transform);
        }

        private void Update()
        {
            TransformState curState = new TransformState(transform);
            if (!curState.Equals(m_lastTransformState))
            {
                GPUVerbContext.Instance.UpdateListener(transform);

                m_lastTransformState = curState;
            }
        }
    }
}