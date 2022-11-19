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

        Cell[,,] m_grid;
        int m_NumSamples;
        int m_id;
        public FDTDRef(Vector2Int gridSize, PlaneverbResolution res) : base(gridSize, res)
        {
            m_id = PlaneverbCreateGrid(gridSize.x, gridSize.y, (int)res);
            m_NumSamples = PlaneverbGetGridResponseLength(m_id);
            m_grid = new Cell[gridSize.x, gridSize.y, m_NumSamples];
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
            for(int i=0; i<m_NumSamples; ++i)
            {
                yield return m_grid[gridPos.x, gridPos.y, i];
            }
        }
    }
}
