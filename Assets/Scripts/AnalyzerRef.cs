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
        static extern void PlaneverbCreateConfig(double sizeX, double sizeY, int gridResolution);

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
        static extern void PlaneverbAnalyzeResponses(int id, double listenerX, double listenerZ);
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern PlaneVerbOutput PlaneverbGetOutputWithID(int id, int emissionID);
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern void PlaneverbGetOneAnalyzerResponse(int id, double emitterX, double emitterY, double emitterZ, IntPtr result);
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern void PlaneverbGetAnalyzerResponses(int gridId, IntPtr results);


        //Debug
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern double PlaneverbGetEdry(int gridId, uint serialIndex);
        [DllImport("ProjectPlaneverbUnityPlugin.dll")]
        static extern double PlaneverbGetEFree(int gridId, uint serialIndex);

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

                    FDTDBase FDTDsolver = GPUVerbContext.Instance.FDTDSolver;
                    Vector2Int temp = FDTDsolver.ToGridPos(new Vector2(5, 0));

                    AnalyzerResult ptr_temp =  new AnalyzerResult();
                    AnalyzerResult* input = &ptr_temp;

                    PlaneverbGetOneAnalyzerResponse(m_id, 5.0f, 0.0f, 0.0f, (IntPtr)input);
                    Debug.Log("Analyzer: " + input[0].occlusion);

                    PlaneverbGetAnalyzerResponses(m_id, (IntPtr)ptr);
                    Debug.Log("Analyzer: " + m_AnalyzerGrid[temp.x, temp.y].occlusion);

                    //Debug.Log("In Game X: " + 19.0f/ FDTDsolver.GetCellSize());
                    //Debug.Log("In Game Y: " + 0.0f / FDTDsolver.GetCellSize());
                    Debug.Log("Pressue: " + PlaneverbGetEdry(m_id,(uint) (temp.x * gridSizeInCells.y + temp.y)));
                    Debug.Log("OnsetSample: " + PlaneverbGetEFree(m_id, (uint)(temp.x * gridSizeInCells.y + temp.y)));
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
