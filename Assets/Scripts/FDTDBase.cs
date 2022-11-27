using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


namespace GPUVerb
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Cell : IEquatable<Cell>
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

        public bool Equals(Cell other)
        {
            return Mathf.Approximately(pressure, other.pressure) &&
                Mathf.Approximately(velX, other.velX) &&
                Mathf.Approximately(velY, other.velY) &&
                b == other.b && by == other.by;
        }

        public override string ToString()
        {
            return $"[ pr = {pressure}, vel = [{velX},{velY}], b = {b}, by = {by} ]";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BoundaryInfo
    {
        public float absorption;
        public BoundaryInfo(float absorption = 0)
        {
            this.absorption = absorption;
        }
    }

    public abstract class FDTDBase : IDisposable
    {
        protected const float k_soundSpeed = 343.21f;
        protected const float k_pointsPerWaveLength = 3.5f;

        protected Vector2Int m_gridSizeInCells;
        protected float m_cellSize;
        protected float m_dt;
        protected uint m_samplingRate;
        protected float m_numSecsPerResponse;
        protected int m_responseLength;
        protected SortedDictionary<int, Bounds> m_geometries;
        protected int m_nextGeoID;

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

            m_geometries = new SortedDictionary<int, Bounds>();
            m_nextGeoID = 0;
        }
        private FDTDBase() { }
        public virtual int GetResponseLength() => m_responseLength;
        public abstract void GenerateResponse(Vector3 listener);
        public abstract IEnumerable<Cell> GetResponse(Vector2Int gridPos);
        public virtual int AddGeometry(Bounds geom)
        {
            m_geometries.Add(m_nextGeoID, geom);
            return m_nextGeoID++;
        }
        public virtual void UpdateGeometry(int id, Bounds geom)
        {
            if (!m_geometries.ContainsKey(id))
                return;
            m_geometries[id] = geom;
        }
        public virtual void RemoveGeometry(int id)
        {
            if (!m_geometries.ContainsKey(id))
                return;
            m_geometries.Remove(id);
        }

        public Vector2Int ToGridPos(Vector2 pos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(pos.x / m_cellSize),
                Mathf.FloorToInt(pos.y / m_cellSize)
            );
        }
        public Vector2Int GetGridSizeInCells() => m_gridSizeInCells;
        public abstract void Dispose();
    }
}
