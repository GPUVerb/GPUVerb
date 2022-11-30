using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GPUVerb
{
    [StructLayout(LayoutKind.Explicit)]
    public struct AnalyzerResult : IEquatable<AnalyzerResult>
    {
        [FieldOffset(0)]
        public float occlusion; // Dry Gain
        [FieldOffset(4)]
        public float wetGain;     // Wet Gain
        [FieldOffset(8)]
        public float rt60;     // RT60, the time that the sound decay to 60dB
        [FieldOffset(12)]
        public float lowpassIntensity; // Low pass filter
        [FieldOffset(16)]
        public PlaneVerbVec2 direction; // direction
        [FieldOffset(24)]
        public PlaneVerbVec2 sourceDirectivity; // sound directivity

        public AnalyzerResult(float occlusion = 0, float wetGain = 0, float rt60 = 0, float lowpassIntensity = 1)
        {
            this.occlusion = occlusion;
            this.wetGain = wetGain;
            this.rt60 = rt60;
            this.lowpassIntensity = lowpassIntensity;
            this.direction = new PlaneVerbVec2(0.0f,0.0f);
            this.sourceDirectivity = new PlaneVerbVec2(0.0f, 0.0f);
        }

        public bool Equals(AnalyzerResult other)
        {
            return Mathf.Approximately(occlusion, other.occlusion) &&
                Mathf.Approximately(wetGain, other.wetGain) &&
                Mathf.Approximately(rt60, other.rt60) &&
                Mathf.Approximately(lowpassIntensity, other.lowpassIntensity) &&
                Mathf.Approximately(direction.x, other.direction.x) &&
                Mathf.Approximately(direction.y, other.direction.y) &&
                Mathf.Approximately(sourceDirectivity.x, other.sourceDirectivity.x) &&
                Mathf.Approximately(sourceDirectivity.y, other.sourceDirectivity.y);
        }

        public override string ToString()
        {
            return $"[ occlusion = {occlusion}, wetGain = {wetGain}, rt60 = {rt60}, lowpassIntensity = {lowpassIntensity}, direction = [{direction.x},{direction.y}], sourceDirectivity = [{sourceDirectivity.x},{sourceDirectivity.y}]";
        }
        public string ToString(bool concise)
        {
            if (concise)
            {
                return $"{occlusion} {wetGain} {rt60} {lowpassIntensity} {direction.x} {direction.y} {sourceDirectivity.x} {sourceDirectivity.y}";
            }
            else
            {
                return ToString();
            }
        }
    }

    public abstract class AnalyzerBase : IDisposable
    {
        protected uint m_gridX;
        protected uint m_gridY;
        protected int m_responseLength;
        protected uint m_samplingRate;
        protected float m_dx;
        protected int m_numThreads;
        protected int m_resolution;
        protected int m_id;
        public int ID { get => m_id; }
        public AnalyzerBase()
        {
        }
        //private AnalyzerBase() { }
        public virtual int GetResponseLength() => m_responseLength;
        public abstract void AnalyzeResponses(Vector3 listener);
        public abstract AnalyzerResult GetAnalyzerResponse(Vector2Int gridPos);

        /*public Vector2Int ToGridPos(Vector2 pos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(pos.x / m_cellSize),
                Mathf.FloorToInt(pos.y / m_cellSize)
            );
        }*/
        //public Vector2Int GetGridSizeInCells() => m_gridSizeInCells;
        public abstract void Dispose();

        #region DEBUG

        #endregion
    }
}
