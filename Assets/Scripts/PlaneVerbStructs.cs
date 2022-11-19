using UnityEngine;

namespace GPUVerb
{
    public enum PlaneverbResolution
    {
        LowResolution = 275,
        MidResolution = 375,
        HighResolution = 500,
        ExtremeResolution = 750,
    };

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