using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;

namespace GPUVerb
{
    // this is a wrapper for the planeverb Analyzer to test our Analyzer's correctness
    public class AnalyzerRef : AnalyzerBase
    {
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern void PlaneverbCreateConfig(float sizeX, float sizeY, int gridResolution);

        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern int PlaneverbCreateEmissionManager();
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern void PlaneverbDestroyEmissionManager(int id);

        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern int PlaneverbCreateFreeGrid();
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern void PlaneverbDestroyFreeGrid(int id);

        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern int PlaneverbCreateAnalyzer(int id);
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern void PlaneverbDestroyAnalyzer(int id);

        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern void PlaneverbAnalyzeResponses(int id, float listenerX, float listenerZ);
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern PlaneVerbOutput PlaneverbGetOutputWithID(int id, int emissionID);
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern void PlaneverbGetOneAnalyzerResponse(int id, float emitterX, float emitterY, float emitterZ, IntPtr result);
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern void PlaneverbGetAnalyzerResponses(int gridId, IntPtr results);


        //Debug
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern float PlaneverbGetEdry(int gridId, uint serialIndex);
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern float PlaneverbGetEFree(int gridId, uint serialIndex);

        AnalyzerResult[,] m_AnalyzerGrid;
        Vector2Int gridSizeInCells;


        public AnalyzerRef(Vector2 gridSize, Vector2Int in_gridSizeInCells, PlaneverbResolution res, int gridId) : base()
        {
            this.gridSizeInCells = in_gridSizeInCells;
            PlaneverbCreateConfig(gridSize.x, gridSize.y, (int)res);

            m_id = PlaneverbCreateEmissionManager();
            Debug.Assert(m_id == gridId);

            m_id = PlaneverbCreateFreeGrid();
            Debug.Assert(m_id == gridId);

            m_id = PlaneverbCreateAnalyzer(gridId);
            Debug.Assert(m_id == gridId);

            m_AnalyzerGrid = new AnalyzerResult[gridSizeInCells.x, gridSizeInCells.y];
        }
        public override void AnalyzeResponses(Vector3 listener)
        {
            Profiler.BeginSample("AnalyzerRef.GenerateResponse");
            unsafe
            {
                fixed (AnalyzerResult* ptr = m_AnalyzerGrid)
                {
                    PlaneverbAnalyzeResponses(m_id, listener.x, listener.z);

                    Debug.Log("Analyzer done " );

                    PlaneverbGetAnalyzerResponses(m_id, (IntPtr)ptr);
                }
            }

            //Debug.Log(m_AnalyzerGrid[0,0].ToString());

            Profiler.EndSample();
        }

        public override AnalyzerResult GetAnalyzerResponse(Vector2Int gridPos)
        {
            if (gridPos.x >= gridSizeInCells.x || gridPos.x < 0 || gridPos.y >= gridSizeInCells.y || gridPos.y < 0)
            {
                return new AnalyzerResult();
                Debug.Log("Access outside of Analyzer Grid");
            }
            return m_AnalyzerGrid[gridPos.x, gridPos.y];
        }

        public override void Dispose()
        {
            PlaneverbDestroyEmissionManager(m_id);
            PlaneverbDestroyFreeGrid(m_id);
            PlaneverbDestroyAnalyzer(m_id);
        }
    }
}
