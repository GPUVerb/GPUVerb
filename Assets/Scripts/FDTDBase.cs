using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


namespace GPUVerb
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Cell
    {
        public float pressure; // air pressure
        public float velX;     // x component of particle velocity
        public float velY;     // y component of particle velocity
        [MarshalAs(UnmanagedType.I2)]
        public short b;        // B field packed into 2 2 byte fields
        [MarshalAs(UnmanagedType.I2)]
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
        }
        private FDTDBase() { }
        public abstract int GetResponseLength();
        public abstract void GenerateResponse(Vector3 listener);
        public abstract IEnumerable<Cell> GetResponse(Vector2Int gridPos);
        public Vector2Int ToGridPos(Vector2 pos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(pos.x / m_cellSize),
                Mathf.FloorToInt(pos.y / m_cellSize)
            );
        }
    }
}