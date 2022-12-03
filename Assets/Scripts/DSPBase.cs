using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUVerb
{
    [System.Serializable]
    public struct DSPConfig
    {
        [Tooltip("Maximum length for the audio thread's callback, usually a power of 2.")]
        public int maxCallbackLength;

        [Tooltip("Factored into lerping dspParams over multiple audio callbacks.")]
        public int dspSmoothingFactor;

        [Tooltip("Unity's audio engine sampling rate. Usually 44100 or 48000.")]
        public int samplingRate;

        [Tooltip("Flag for whether PlaneverbDSP should process spatialization (true), or if the user will handle spatialization themselves (false).")]
        public bool useSpatialization;

        [Tooltip("Ratio for how much the reverberant sound affects the audio.")]
        public float wetGainRatio;
    }

    public abstract class DSPBase : IDisposable
    {
        public static int k_invalidID = -1;
        public static int k_maxFrameLen = 4096;

        public DSPBase(DSPConfig config) { }
        public abstract void SetListenerPos(Vector3 pos, Vector3 forward);
        public abstract int RegisterEmitter(Vector3 pos, Vector3 forward);
        public abstract void UpdateEmitter(int id, Vector3 pos, Vector3 forward);
        public abstract void RemoveEmitter(int id);
        public abstract void SetEmitterDirectivityPattern(int id, SourceDirectivityPattern pattern);
        public abstract void SendSource(int id, in AnalyzerResult param, float[] data, int numSamples, int channels);
        public abstract float[] GetOutput(ReverbIndex reverb);
        public abstract void Dispose();
    }
}