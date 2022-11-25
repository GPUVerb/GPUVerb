using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


namespace GPUVerb
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Cell
    {
        [FieldOffset(0)]
        public float pressure; // air pressure
        [FieldOffset(4)]
        public float velX;     // x component of particle velocity
        [FieldOffset(8)]
        public float velY;     // y component of particle velocity
        [FieldOffset(12)][MarshalAs(UnmanagedType.I2)]
        public short b;        // B field packed into 2 2 byte fields
        [FieldOffset(14)][MarshalAs(UnmanagedType.I2)]
        public short by;       // B field packed into 2 2 byte fields

        public Cell(float pressure = 0, float velX = 0, float velY = 0, short b = 1, short by = 1)
        {
            this.pressure = pressure;
            this.velX = velX;
            this.velY = velY;
            this.b = b;
            this.by = by;
        }
    }
    public abstract class FDTDBase
    {
        const float k_soundSpeed = 343.21f;
        const float k_pointsPerWaveLength = 3.5f;

        protected Vector2Int m_gridSizeInCells;
        protected float m_cellSize;
        protected float m_dt;
        protected uint m_samplingRate;
        protected float m_numSecsPerResponse;
        protected int m_responseLength;

        public FDTDBase(Vector2 gridSize, PlaneverbResolution res) 
        {
            float minWavelength = k_soundSpeed / (float)res;
            m_cellSize = minWavelength / k_pointsPerWaveLength;
            m_dt = m_cellSize / (k_soundSpeed * 1.5f);
            m_samplingRate = (uint)(1.0f / m_dt);

            m_gridSizeInCells = new Vector2Int(
                    Mathf.CeilToInt(gridSize.x / m_cellSize),
                    Mathf.CeilToInt(gridSize.y / m_cellSize)
                );

            const float domainSize = 25; // TODO: 25m, may make this tweakable later
            m_numSecsPerResponse = domainSize / (Mathf.Sqrt(2) * k_soundSpeed) + 0.25f;
            m_responseLength = (int)(m_samplingRate * m_numSecsPerResponse);
        }
        private FDTDBase() { }
        public virtual int GetResponseLength() => m_responseLength;
        public abstract void GenerateResponse(Vector3 listener);
        public abstract IEnumerable<Cell> GetResponse(Vector2Int gridPos);
        public abstract int AddGeometry(Bounds geom);
        public abstract void UpdateGeometry(int id, Bounds geom);
        public abstract void RemoveGeometry(int id);
        
        public Vector2Int ToGridPos(Vector2 pos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(pos.x / m_cellSize),
                Mathf.FloorToInt(pos.y / m_cellSize)
            );
        }
    }
}
