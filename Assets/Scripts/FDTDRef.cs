using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GPUVerb
{
    // TODO: call into the planeverb DLL
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

        Cell[,,] m_grid;
        int m_numSamples;
        int m_id;
        public FDTDRef(Vector2 gridSize, PlaneverbResolution res) : base(gridSize, res)
        {
            m_id = PlaneverbCreateGrid(gridSize.x, gridSize.y, (int)res);
            m_numSamples = PlaneverbGetGridResponseLength(m_id);
            m_grid = new Cell[m_gridSizeInCells.x, m_gridSizeInCells.y, m_numSamples];
        }
        ~FDTDRef()
        {
            PlaneverbDestroyGrid(m_id);
        }
        public override void GenerateResponse(Vector3 listener)
        {
            unsafe
            {
                fixed(Cell* ptr = m_grid)
                {
                    PlaneverbGetGridResponse(m_id, listener.x, listener.z, (IntPtr)ptr);
                }
            }
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

        public override void AddGeometry(Bounds bounds)
        {
            PlaneverbAddAABB(m_id, (PlaneVerbAABB)bounds);
        }
    }
}
