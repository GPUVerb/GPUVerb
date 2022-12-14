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
            return Mathf.Abs(pressure - other.pressure) <= tolerance &&
                   Mathf.Abs(velX - other.velX) <= tolerance &&
                   Mathf.Abs(velY - other.velY) <= tolerance &&
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
                return $"{pressure.ToString("0.0")} {velX.ToString("0.0")} {velY.ToString("0.0")} {b} {by}";
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

    // this is needed because we want to make AnalyzerGPU/Ref class to work
    // with any of FDTDGPU/Ref
    public interface IFDTDResult
    {
        Cell this[int x, int y, int t] { get; }
        Array ToArray();
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

        protected const float k_soundSpeed = 343.21f;
        protected const float k_pointsPerWaveLength = 3.5f;

        protected Vector2 m_gridSize;
        protected Vector2Int m_gridSizeInCells;
        protected float m_cellSize;
        protected float m_dt;
        protected uint m_samplingRate;
        protected float m_numSecsPerResponse;
        protected int m_responseLength;
        protected PlaneverbResolution m_resolution;


        #region Geometry Data
        public const int k_invalidGeomID = -1;
        private List<GeomInfo> m_geometries;
        private List<GeomtryUpdateInfo> m_pendingUpdates;
        #endregion


        public virtual int GetResponseLength() => m_responseLength;
        public Vector2 GetGridSize() => m_gridSize;
        public Vector2Int GetGridSizeInCells() => m_gridSizeInCells;
        public float GetCellSize() => m_cellSize;
        public PlaneverbResolution GetResolution() => m_resolution;
        public uint GetSamplingRate() => m_samplingRate;

        // TODO: remove ID
        protected int m_id;
        public int ID { get => m_id; }

        public FDTDBase(Vector2 gridSize, PlaneverbResolution res) 
        {
            m_gridSize = gridSize;
            m_resolution = res;

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

            m_geometries = new List<GeomInfo>();
            m_pendingUpdates = new List<GeomtryUpdateInfo>();
        }
        private FDTDBase() { }


        public abstract void GenerateResponse(Vector3 listener);

        public abstract IFDTDResult GetGrid();

        public virtual void Update() { }

        public IEnumerable<Cell> GetResponse(Vector2Int gridPos)
        {
            IFDTDResult grid = GetGrid();
            if (gridPos.x >= m_gridSizeInCells.x || gridPos.x < 0 || gridPos.y >= m_gridSizeInCells.y || gridPos.y < 0)
            {
                yield break;
            }
            for (int i = 0; i < GetResponseLength(); ++i)
            {
                yield return grid[gridPos.x, gridPos.y, i];
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

        public bool ProcessGeometryUpdates()
        {
            bool ret = false;
            for(int id = 0; id < m_pendingUpdates.Count; ++id)
            {
                if(m_pendingUpdates[id].processed)
                {
                    continue;
                }

                ret = true;

                if (m_pendingUpdates[id].type == GeomtryUpdateInfo.Type.ADD)
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
            return ret;
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
                Mathf.Clamp(Mathf.FloorToInt(pos.x / m_cellSize), 0, m_gridSizeInCells.x - 1),
                Mathf.Clamp(Mathf.FloorToInt(pos.y / m_cellSize), 0, m_gridSizeInCells.y - 1)
            );
        }
        protected Vector2Int GetDispatchDim(Vector2Int inputDim, Vector2Int threadGroupDim)
        {
            int gridDimX = (inputDim.x + threadGroupDim.x - 1) / threadGroupDim.x;
            int gridDimY = (inputDim.y + threadGroupDim.y - 1) / threadGroupDim.y;
            return new Vector2Int(gridDimX, gridDimY);
        }

        protected void GetGeometryUpdateParams(in PlaneVerbAABB bounds, out Vector2Int gridMin, out Vector2Int gridMax)
        {
            gridMin = ToGridPos(bounds.min);
            gridMax = ToGridPos(bounds.max);
            if (gridMax.x <= gridMin.x)
            {
                gridMax.x = gridMin.x + 1;
            }
            if (gridMax.y <= gridMin.y)
            {
                gridMax.y = gridMin.y + 1;
            }
        }

        public abstract void Dispose();

        #region DEBUG
        
        #endregion
    }
}
