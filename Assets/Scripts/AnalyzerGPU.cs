
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GPUVerb
{
    // our implementaiton of Analyzer using compute shader
    public class AnalyzerGPU : AnalyzerBase
    {
        const string k_FDTDgridShaderParam = "FDTDgrid";
        const string k_analyzerGridShaderParam = "analyzerGrid";
        const string k_delaySamplesShaderParam = "delaySamples";

        const string k_listenerPosShaderParam = "listenerPos";
        const string k_gridDimShaderParam = "gridDim";
        const string k_cellSizeShaderParam = "cellSize";
        const string k_samplingRateShaderParam = "samplingRate";
        const string k_responseLengthShaderParam = "responseLength";
        const string k_eFreeShaderParam = "EFree";
        const string k_resolutionShaderParam = "resolution";

        const string k_audibleThresholdGainShaderParam = "AUDIBLE_THRESHOLD_GAIN";
        const string k_dryGainAnalysisLengthShaderParam = "DRY_GAIN_ANALYSIS_LENGTH";
        const string k_dryDirectionAnalysisLengthShaderParam = "DRY_DIRECTION_ANALYSIS_LENGTH";
        const string k_wetGainAnalysisLengthShaderParam = "WET_GAIN_ANALYSIS_LENGTH";
        const string k_schroederOffsetSGainShaderParam = "SCHROEDER_OFFSET_S";
        const string k_cShaderParam = "C";
        const string k_delayCloseThresholdShaderParam = "DELAY_CLOSE_THRESHOLD";
        const string k_distanceGainThresholdShaderParam = "DISTANCE_GAIN_THRESHOLD";

        const string k_shaderPath = "Shaders/Analyzer";
        const string k_EncodeResponseKernelName = "KernEncodeResponse";
        const string k_EncodeListenerDirectionKernelName = "KernEncodeListenerDirection";


        ComputeShader m_shader = null;
        ComputeBuffer m_FDTDgridBuffer = null;
        ComputeBuffer m_analyzerGridBuffer = null;
        ComputeBuffer m_delaySamplesBuffer = null;

        int m_encodeResponseKernel = -1;
        int m_encodeListenerDirectionKernel = -1;

        Vector2Int m_threadGroupDim = Vector2Int.zero;
        int[] m_delaySamples;


        //FreeGrid precomputation
        float m_EFree;

        void SimulateFreeFieldEnergy()
        {
            //this fdtd grid should have the same grid data as our fdtd grid
            FDTDBase FDTDsolver = GPUVerbContext.Instance.FDTDSolver;

            int listenerX = gridSizeInCells.x / 2;
            int listenerY = gridSizeInCells.y / 2;
            int emitterX = listenerX + (int)(1.0f / m_cellSize);// still not sure why adding this
            int emitterY = listenerY;

            FDTDsolver.GenerateResponse(new Vector3(listenerX * m_cellSize, 0, listenerY * m_cellSize));

            //Combine CalculateEFree here

            // Dry duration, plus delay to get 1m away
            int numSamples = (int)(InternalConstants.GV_DRY_GAIN_ANALYSIS_LENGTH * m_samplingRate) +
                (int)(1.0f / InternalConstants.GV_C * m_samplingRate);
            Debug.Assert(numSamples < m_responseLength);

            float efree = 0.0f;

            int count = 0;
            foreach (Cell c in FDTDsolver.GetResponse(new Vector2Int(emitterX, emitterY)))
            {
                if(count>=numSamples)
                {
                    break;
                }
                efree += c.pressure * c.pressure;
                ++count;
            }

            float r = (float)(emitterX - emitterY) * m_cellSize;
            efree *= r;
            m_EFree = efree;
        }

        void AnalyerInitSetup()
        {
            FDTDBase FDTDsolver = GPUVerbContext.Instance.FDTDSolver;
            this.gridSizeInCells = FDTDsolver.GetGridSizeInCells();
            this.m_responseLength = FDTDsolver.GetResponseLength();
            this.m_samplingRate = FDTDsolver.GetSamplingRate();
            this.m_cellSize = FDTDsolver.GetCellSize();
            this.m_resolution = FDTDsolver.GetResolution();
            SimulateFreeFieldEnergy();
        }

        public AnalyzerGPU() : base()
        {
            AnalyerInitSetup();

            m_shader = Resources.Load<ComputeShader>(k_shaderPath);
            m_encodeResponseKernel = m_shader.FindKernel(k_EncodeResponseKernelName);
            m_encodeListenerDirectionKernel = m_shader.FindKernel(k_EncodeListenerDirectionKernelName);

            m_shader.GetKernelThreadGroupSizes(m_encodeResponseKernel, out uint x, out uint y, out uint _);
            m_threadGroupDim = new Vector2Int((int)x, (int)y);

            int planeSize = gridSizeInCells.x * gridSizeInCells.y;
            int totalSize = planeSize * m_responseLength;

            m_FDTDgridBuffer = new ComputeBuffer(totalSize, Marshal.SizeOf(typeof(Cell)));
            m_analyzerGridBuffer = new ComputeBuffer(planeSize, Marshal.SizeOf(typeof(AnalyzerResult)));
            m_delaySamplesBuffer = new ComputeBuffer(planeSize, sizeof(int));

            // set some constant parameters in shader
            m_shader.SetInts(k_gridDimShaderParam, new int[] { gridSizeInCells.x, gridSizeInCells.y });
            m_shader.SetFloat(k_cellSizeShaderParam, m_cellSize);
            m_shader.SetFloat(k_samplingRateShaderParam, m_samplingRate);
            m_shader.SetFloat(k_responseLengthShaderParam, m_responseLength);
            m_shader.SetFloat(k_eFreeShaderParam, m_EFree);
            m_shader.SetInt(k_resolutionShaderParam, (int)m_resolution);

            m_shader.SetFloat(k_audibleThresholdGainShaderParam, InternalConstants.GV_AUDIBLE_THRESHOLD_GAIN);
            m_shader.SetFloat(k_dryGainAnalysisLengthShaderParam, InternalConstants.GV_DRY_GAIN_ANALYSIS_LENGTH);
            m_shader.SetFloat(k_dryDirectionAnalysisLengthShaderParam, InternalConstants.GV_DRY_DIRECTION_ANALYSIS_LENGTH);
            m_shader.SetFloat(k_wetGainAnalysisLengthShaderParam, InternalConstants.GV_WET_GAIN_ANALYSIS_LENGTH);
            m_shader.SetFloat(k_schroederOffsetSGainShaderParam, InternalConstants.GV_SCHROEDER_OFFSET_S);
            m_shader.SetFloat(k_cShaderParam, InternalConstants.GV_C);
            m_shader.SetInt(k_delayCloseThresholdShaderParam, InternalConstants.GV_DELAY_CLOSE_THRESHOLD);
            m_shader.SetFloat(k_distanceGainThresholdShaderParam, InternalConstants.GV_DISTANCE_GAIN_THRESHOLD);

            m_AnalyzerGrid = new AnalyzerResult[gridSizeInCells.x, gridSizeInCells.y];
            m_delaySamples = new int[planeSize];
        }

        Vector2Int GetDispatchDim(Vector2Int inputDim)
        {
            int gridDimX = (inputDim.x + m_threadGroupDim.x - 1) / m_threadGroupDim.x;
            int gridDimY = (inputDim.y + m_threadGroupDim.y - 1) / m_threadGroupDim.y;
            return new Vector2Int(gridDimX, gridDimY);
        }

        public override void AnalyzeResponses(Vector3 listener)
        {
            /*Vector3 listenerPos = listener;
            Plus grid offset
            listenerPos.x += gridOffset.x;
            listenerPos.z += gridOffset.y;*/

            int maxVal = int.MaxValue;
            Array.Fill<int>(m_delaySamples, maxVal);

            Vector2Int dim = GetDispatchDim(gridSizeInCells);

            //Ecode Response Compute shader calls
            FDTDBase FDTDsolver = GPUVerbContext.Instance.FDTDSolver;
            // Copy FDTD grid data into compute shader
            m_FDTDgridBuffer.SetData(FDTDsolver.GetGrid());
            // bind FDTD grid
            m_shader.SetBuffer(m_encodeResponseKernel, k_FDTDgridShaderParam, m_FDTDgridBuffer);
            // bind analyzer grid
            m_shader.SetBuffer(m_encodeResponseKernel, k_analyzerGridShaderParam, m_analyzerGridBuffer);
            // Copu dalay array into buffer
            m_delaySamplesBuffer.SetData(m_delaySamples);
            // bind delay samples
            m_shader.SetBuffer(m_encodeResponseKernel, k_delaySamplesShaderParam, m_delaySamplesBuffer);
            // bind listener pos
            m_shader.SetFloats(k_listenerPosShaderParam, new float[] { listener.x, listener.z });
            // dispatch Encode Response kernel
            m_shader.Dispatch(m_encodeResponseKernel, dim.x, dim.y, 1);


            // Encode Listener Position
            // bind FDTD grid
            m_shader.SetBuffer(m_encodeListenerDirectionKernel, k_FDTDgridShaderParam, m_FDTDgridBuffer);
            // bind analyzer grid
            m_shader.SetBuffer(m_encodeListenerDirectionKernel, k_analyzerGridShaderParam, m_analyzerGridBuffer);
            // bind delay samples
            m_shader.SetBuffer(m_encodeListenerDirectionKernel, k_delaySamplesShaderParam, m_delaySamplesBuffer);
            // dispatch Encode Listener Direction kernel
            m_shader.Dispatch(m_encodeListenerDirectionKernel, dim.x, dim.y, 1);

            //copy analyzerGrid to AnalyzerGrid in c#
            m_analyzerGridBuffer.GetData(m_AnalyzerGrid);
        }
        public override AnalyzerResult GetAnalyzerResponse(Vector2Int gridPos)
        {
            if (gridPos.x >= gridSizeInCells.x || gridPos.x < 0 || gridPos.y >= gridSizeInCells.y || gridPos.y < 0)
            {
                Debug.Log("Access outside of Analyzer Grid");
                return new AnalyzerResult();
            }
            return m_AnalyzerGrid[gridPos.x, gridPos.y];
        }
        public override void Dispose()
        {
            m_FDTDgridBuffer.Dispose();
            m_analyzerGridBuffer.Dispose();
            m_delaySamplesBuffer.Dispose();
        }
    }
}
