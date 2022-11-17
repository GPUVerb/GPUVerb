using UnityEngine;

namespace GPUVerb
{
    public struct PlaneVerbOutput
    {
        public float occlution;
        public float wetGain;
        public float rt60;
        public float lowpass;
        public Vector2 direction;
        public Vector2 sourceDirectivity;
    }
}