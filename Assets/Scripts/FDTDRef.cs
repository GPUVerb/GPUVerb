using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;

namespace GPUVerb
{
    // DONE: call into the planeverb DLL
    // this is a wrapper for the planeverb FDTD to test our FDTD's correctness
    public class FDTDRef : FDTDBase
    {
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern int PlaneverbCreateGrid(float sizeX, float sizeY, int gridResolution);
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern void PlaneverbDestroyGrid(int id);
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern int PlaneverbGetGridResponseLength(int id);
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern void PlaneverbGetGridResponse(int gridId, float listenerX, float listenerZ, IntPtr result);
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern void PlaneverbAddAABB(int gridId, PlaneVerbAABB aabb);
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern void PlaneverbUpdateAABB(int gridId, PlaneVerbAABB oldVal, PlaneVerbAABB newVal);
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern void PlaneverbRemoveAABB(int gridId, PlaneVerbAABB aabb);


        Cell[,,] m_grid;
        int m_numSamples;
        int m_id;


        public FDTDRef(Vector2 gridSize, PlaneverbResolution res) : base(gridSize, res)
        {
            m_id = PlaneverbCreateGrid(gridSize.x, gridSize.y, (int)res);
            m_numSamples = PlaneverbGetGridResponseLength(m_id);
            m_grid = new Cell[m_gridSizeInCells.x, m_gridSizeInCells.y, m_numSamples];
            m_geometries = new SortedDictionary<int, Bounds>();
        }
        public override void GenerateResponse(Vector3 listener)
        {
            Profiler.BeginSample("FDTDRef.GenerateResponse");

            unsafe
            {
                fixed(Cell* ptr = m_grid)
                {
                    PlaneverbGetGridResponse(m_id, listener.x, listener.z, (IntPtr)ptr);
                }
            }

            Profiler.EndSample();
        }

        public override IEnumerable<Cell> GetResponse(Vector2Int gridPos)
        {
            if(gridPos.x >= m_grid.GetLength(0) || gridPos.x < 0 || gridPos.y >= m_grid.GetLength(1) || gridPos.y < 0)
            {
                yield break;
            }
            for(int i=0; i<m_numSamples; ++i)
            {
                yield return m_grid[gridPos.x, gridPos.y, i];
            }
        }

        public override int GetResponseLength()
        {
            return PlaneverbGetGridResponseLength(m_id);
        }

        public override int AddGeometry(Bounds geom)
        {
            PlaneverbAddAABB(m_id, (PlaneVerbAABB)geom);
            return base.AddGeometry(geom);
        }

        public override void UpdateGeometry(int id, Bounds geom)
        {
            PlaneverbUpdateAABB(m_id, (PlaneVerbAABB)m_geometries[id], (PlaneVerbAABB)geom);
            base.UpdateGeometry(id, geom);
        }

        public override void RemoveGeometry(int id)
        {
            PlaneverbRemoveAABB(m_id, (PlaneVerbAABB)m_geometries[id]);
            base.RemoveGeometry(id);
        }

        public override void Dispose()
        {
            PlaneverbDestroyGrid(m_id);
        }
    }
}
