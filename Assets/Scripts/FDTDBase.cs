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
        public double pressure; // air pressure
        [FieldOffset(8)]
        public double velX;     // x component of particle velocity
        [FieldOffset(16)]
        public double velY;     // y component of particle velocity
        [FieldOffset(24)][MarshalAs(UnmanagedType.I2)]
        public short b;        // B field packed into 2 2 byte fields
        [FieldOffset(32)][MarshalAs(UnmanagedType.I2)]
        public short by;       // B field packed into 2 2 byte fields

        public Cell(double pressure = 0, double velX = 0, double velY = 0, short b = 1, short by = 1)
        {
            this.pressure = pressure;
            this.velX = velX;
            this.velY = velY;
            this.b = b;
            this.by = by;
        }

        public bool Equals(Cell other)
        {
            return Math.Equals(pressure, other.pressure) &&
                Math.Equals(velX, other.velX) &&
                Math.Equals(velY, other.velY) &&
                b == other.b && by == other.by;
        }
        public bool Equals(Cell other, double tolerance)
        {
            return Math.Abs(pressure - other.pressure) <= tolerance &&
                   Math.Abs(velX - other.velX) <= tolerance &&
                   Math.Abs(velY - other.velY) <= tolerance &&
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
        public double absorption;
        public BoundaryInfo(double absorption = 0)
        {
            this.absorption = absorption;
        }
    }

    public abstract class FDTDBase : IDisposable
    {
        public class GeomtryUpdateInfo
        {
            public enum Type : byte
            {
                ADD, REMOVE, UPDATE
            }
            public GeomtryUpdateInfo(Type type, PlaneVerbAABB geom)
            {
                processed = false;
                this.type = type;
                this.bounds = geom;
            }
            private GeomtryUpdateInfo() { }
            public bool processed;
            public Type type;
            public PlaneVerbAABB bounds;
        }
        private class GeomInfo
        {
            public GeomInfo(PlaneVerbAABB bounds)
            {
                present = true;
                this.bounds = bounds;
            }
            public bool present;
            public PlaneVerbAABB bounds;
        }

        protected const double k_soundSpeed = 343.21f;
        protected const double k_pointsPerWaveLength = 3.5f;

        protected Vector2 m_gridSize;
        protected Vector2Int m_gridSizeInCells;
        protected double m_cellSize;
        protected double m_dt;
        protected uint m_samplingRate;
        protected double m_numSecsPerResponse;
        protected int m_responseLength;

        #region Geometry Data
        protected const int k_invalidGeomID = -1;
        private List<GeomInfo> m_geometries;
        private List<GeomtryUpdateInfo> m_pendingUpdates;
        #endregion
        
        protected Cell[,,] m_grid;

        // TODO: remove ID
        protected int m_id;
        public int ID { get => m_id; }

        public FDTDBase(Vector2 gridSize, PlaneverbResolution res) 
        {
            m_gridSize = gridSize;

            double minWavelength = k_soundSpeed / (double)res;
            m_cellSize = minWavelength / k_pointsPerWaveLength;
            m_dt = m_cellSize / (k_soundSpeed * 1.5f);
            m_samplingRate = (uint)(1.0f / m_dt);

            m_gridSizeInCells = new Vector2Int(
                    (int)Math.Ceiling(gridSize.x / m_cellSize),
                    (int)Math.Ceiling(gridSize.y / m_cellSize)
                );

            const double domainSize = 25; // TODO: 25m, may make this tweakable later
            m_numSecsPerResponse = domainSize / (Mathf.Sqrt(2) * k_soundSpeed) + 0.25f;
            m_responseLength = (int)(m_samplingRate * m_numSecsPerResponse);

            m_geometries = new List<GeomInfo>();
            m_pendingUpdates = new List<GeomtryUpdateInfo>();
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
        public int AddGeometry(in PlaneVerbAABB geom)
        {
            if (!IsInGrid(geom.min) && !IsInGrid(geom.max))
            {
                return k_invalidGeomID;
            }

            m_geometries.Add(new GeomInfo(geom));
            m_pendingUpdates.Add(new GeomtryUpdateInfo(GeomtryUpdateInfo.Type.UPDATE, geom));

            return m_geometries.Count - 1;
        }
        public void UpdateGeometry(int id, in PlaneVerbAABB geom)
        {
            if(!IsValid(id))
            {
                return;
            }

            if (m_pendingUpdates[id].type == GeomtryUpdateInfo.Type.REMOVE)
            {
                // can't override remove
                return;
            }

            m_pendingUpdates[id].processed = false;
            m_pendingUpdates[id].type = GeomtryUpdateInfo.Type.UPDATE;
            m_pendingUpdates[id].bounds = geom;
        }
        public void RemoveGeometry(int id)
        {
            if (!IsValid(id))
            {
                return;
            }

            m_pendingUpdates[id].processed = false;
            m_pendingUpdates[id].type = GeomtryUpdateInfo.Type.REMOVE;
        }

        protected void ProcessGeometryUpdates()
        {
            for(int id = 0; id < m_pendingUpdates.Count; ++id)
            {
                if(m_pendingUpdates[id].processed)
                {
                    continue;
                }

                if(m_pendingUpdates[id].type == GeomtryUpdateInfo.Type.ADD)
                    // same as update but makes it a case anyway
                    // because FDTDRef needs this distinction
                {
                    DoAddGeometry(id, m_pendingUpdates[id].bounds);
                    m_geometries[id].present = true;
                    m_geometries[id].bounds = m_pendingUpdates[id].bounds;
                }
                else if(m_pendingUpdates[id].type == GeomtryUpdateInfo.Type.UPDATE)
                {
                    DoUpdateGeometry(id, m_pendingUpdates[id].bounds);
                    m_geometries[id].present = true;
                    m_geometries[id].bounds = m_pendingUpdates[id].bounds;
                }
                else if(m_pendingUpdates[id].type == GeomtryUpdateInfo.Type.REMOVE)
                {
                    DoRemoveGeometry(id);
                    m_geometries[id].present = false;
                }

                // mark as processed
                m_pendingUpdates[id].processed = true;
            }
        }
        protected abstract void DoAddGeometry(int id, in PlaneVerbAABB geom);
        protected abstract void DoRemoveGeometry(int id);
        protected abstract void DoUpdateGeometry(int id, in PlaneVerbAABB geom);

        public PlaneVerbAABB GetBounds(int id)
        {
            return m_geometries[id].bounds;
        }
        public bool IsValid(int id)
        {
            return id >= 0 && id < m_geometries.Count && m_geometries[id].present;
        }

        public bool IsInGrid(in Vector2 pos)
        {
            return pos.x >= 0 && pos.x <= m_gridSize.x && pos.y >= 0 && pos.y <= m_gridSize.y;
        }
        public Vector2Int ToGridPos(Vector2 pos)
        {
            return new Vector2Int(
                (int)Math.Clamp(Math.Floor(pos.x / m_cellSize), 0, m_gridSizeInCells.x - 1),
                (int)Math.Clamp(Math.Floor(pos.y / m_cellSize), 0, m_gridSizeInCells.y - 1)
            );
        }

        public Vector2Int GetGridSizeInCells() => m_gridSizeInCells;
        public double GetCellSize() => m_cellSize;
        public abstract void Dispose();

        #region DEBUG
        
        #endregion
    }
}
