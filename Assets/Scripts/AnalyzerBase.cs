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
        public double occlusion; // Dry Gain
        [FieldOffset(8)]
        public double wetGain;     // Wet Gain
        [FieldOffset(16)]
        public double rt60;     // RT60, the time that the sound decay to 60dB
        [FieldOffset(24)]
        public double lowpassIntensity; // Low pass filter
        [FieldOffset(32)]
        public PlaneVerbVec2 direction; // direction
        [FieldOffset(48)]
        public PlaneVerbVec2 sourceDirectivity; // sound directivity

        public AnalyzerResult(double occlusion = 0, double wetGain = 0, double rt60 = 0, double lowpassIntensity = 1)
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
            return Math.Equals(occlusion, other.occlusion) &&
                Math.Equals(wetGain, other.wetGain) &&
                Math.Equals(rt60, other.rt60) &&
                Math.Equals(lowpassIntensity, other.lowpassIntensity) &&
                Math.Equals(direction.x, other.direction.x) &&
                Math.Equals(direction.y, other.direction.y) &&
                Math.Equals(sourceDirectivity.x, other.sourceDirectivity.x) &&
                Math.Equals(sourceDirectivity.y, other.sourceDirectivity.y);
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
        protected double m_dx;
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
