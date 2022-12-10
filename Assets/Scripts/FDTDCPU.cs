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
    public class FDTDCPU : FDTDBase
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

        public class Result : IFDTDResult
        {
            private Cell[,,] m_grid;
            public Result(Cell[,,] grid)
            {
                m_grid = grid;
            }
            private Result() { }
            public Cell this[int x, int y, int t] 
            { 
                get => m_grid[x, y, t];
            }

            public Array ToArray() => m_grid;
        }

        private int m_numSamples;
        private Cell[,,] m_grid;
        public override IFDTDResult GetGrid()
        {
            return new Result(m_grid);
        }

        public FDTDCPU(Vector2 gridSize, PlaneverbResolution res) : base(gridSize, res)
        {
            m_id = PlaneverbCreateGrid(gridSize.x, gridSize.y, (int)res);
            m_numSamples = PlaneverbGetGridResponseLength(m_id);
            m_grid = new Cell[m_gridSizeInCells.x, m_gridSizeInCells.y, m_numSamples];
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

        public override int GetResponseLength()
        {
            return PlaneverbGetGridResponseLength(m_id);
        }
        protected override void DoAddGeometry(int id, in PlaneVerbAABB geom)
        {
            PlaneverbAddAABB(m_id, geom);
        }
        protected override void DoRemoveGeometry(int id)
        {
            PlaneverbRemoveAABB(m_id, GetBounds(id));
        }
        protected override void DoUpdateGeometry(int id, in PlaneVerbAABB geom)
        {
            PlaneverbUpdateAABB(m_id, GetBounds(id), geom);
        }
        public override void Dispose()
        {
            PlaneverbDestroyGrid(m_id);
        }
    }
}
