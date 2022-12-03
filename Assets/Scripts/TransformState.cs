using System;
using UnityEngine;

namespace GPUVerb
{
    struct TransformState : IEquatable<TransformState>
    {
        public Quaternion rot;
        public Vector3 pos;
        public Vector3 scale;
        public TransformState(Transform t)
        {
            rot = t.rotation;
            pos = t.position;
            scale = t.localScale;
        }
        public bool Equals(TransformState other)
        {
            return other.rot == rot && other.pos == pos && other.scale == scale;
        }
    }
}