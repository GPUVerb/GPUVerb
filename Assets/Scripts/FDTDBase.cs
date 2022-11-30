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
        public bool Equals(Cell other, float tolerance)
        {
            return Mathf.Approximately(tolerance, Mathf.Abs(pressure - other.pressure)) &&
                   Mathf.Approximately(tolerance, Mathf.Abs(velX - other.velX)) &&
                   Mathf.Approximately(tolerance, Mathf.Abs(velY - other.velY)) &&
                   b == other.b && by == other.by;
        }
        public override string ToString()
        {
            return $"[ pr = {pressure}, vel = [{velX},{velY}], b = {b}, by = {by} ]";
        }
        public string ToString(bool concise)
        {
            if(concise)
            {
                return $"{pressure} {velX} {velY} {b} {by}";
            }
            else
            {
                return ToString();
            }
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
        public struct GeomtryUpdateInfo
        {
            public enum Type
            {
                ADD, REMOVE, UPDATE
            }
            public Type type;
            public int id;
            public PlaneVerbAABB bounds;
        }

        protected const float k_soundSpeed = 343.21f;
        protected const float k_pointsPerWaveLength = 3.5f;

        protected Vector2 m_gridSize;
        protected Vector2Int m_gridSizeInCells;
        protected float m_cellSize;
        protected float m_dt;
        protected uint m_samplingRate;
        protected float m_numSecsPerResponse;
        protected int m_responseLength;

        #region Geometry Data
        protected const int k_invalidGeomID = -1;
        protected List<(bool, PlaneVerbAABB)> m_geometries;
        protected int m_nextGeoID;
        protected SortedDictionary<int, GeomtryUpdateInfo> m_pendingUpdates;
        #endregion
        
        protected Cell[,,] m_grid;

        // TODO: remove ID
        protected int m_id;
        public int ID { get => m_id; }

        public FDTDBase(Vector2 gridSize, PlaneverbResolution res) 
        {
            m_gridSize = gridSize;

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

            m_geometries = new List<(bool, PlaneVerbAABB)>();
            m_nextGeoID = 0;
        }
        private FDTDBase() { }
        public virtual int GetResponseLength() => m_responseLength;
        public abstract void GenerateResponse(Vector3 listener);
        public IEnumerable<Cell> GetResponse(Vector2Int gridPos)
        {
            if (gridPos.x >= m_grid.GetLength(0) || gridPos.x < 0 || gridPos.y >= m_grid.GetLength(1) || gridPos.y < 0)
            {
                yield break;
            }
            for (int i = 0; i < GetResponseLength(); ++i)
            {
                yield return m_grid[gridPos.x, gridPos.y, i];
            }
        }
        public virtual int AddGeometry(in PlaneVerbAABB geom)
        {
            m_geometries.Add((true, geom));
            return m_geometries.Count - 1;
        }
        public virtual void UpdateGeometry(int id, in PlaneVerbAABB geom)
        {
            if (!IsValid(id))
                return;
            m_geometries[id] = (true, geom);
        }
        public virtual void RemoveGeometry(int id)
        {
            if (!IsValid(id))
                return;
            m_geometries[id] = (false, new PlaneVerbAABB());
        }
        public PlaneVerbAABB GetBounds(int id)
        {
            return m_geometries[id].Item2;
        }
        public bool IsValid(int id)
        {
            return id >= 0 && id < m_geometries.Count && m_geometries[id].Item1;
        }

        public bool IsInGrid(in Vector2 pos)
        {
            return pos.x >= 0 && pos.x <= m_gridSize.x && pos.y >= 0 && pos.y <= m_gridSize.y;
        }
        public Vector2Int ToGridPos(Vector2 pos)
        {
            return new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(pos.x / m_cellSize), 0, m_gridSizeInCells.x - 1),
                Mathf.Clamp(Mathf.FloorToInt(pos.y / m_cellSize), 0, m_gridSizeInCells.y - 1)
            );
        }

        public Vector2Int GetGridSizeInCells() => m_gridSizeInCells;
        public float GetCellSize() => m_cellSize;
        public abstract void Dispose();

        #region DEBUG
        
        #endregion
    }
}
