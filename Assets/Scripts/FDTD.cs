using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GPUVerb
{
    // TODO: our implementaiton of FDTD using compute shader
    public class FDTD : FDTDBase
    {
        const string k_gridShaderParam = "grid";
        const string k_gridInShaderParam = "gridIn";
        const string k_gridOutShaderParam = "gridOut";
        const string k_boundariesShaderParam = "boundaries";
        const string k_gaussianPulseShaderParam = "gaussianPulse";
        const string k_listenerPosShaderParam = "listenerPos";
        const string k_courantShaderParam = "courant";
        const string k_curTimeShaderParam = "curTime";
        const string k_gridDimShaderParam = "gridDim";
        const string k_updateDimShaderParam = "updateDim";
        const string k_absorptionShaderParam = "updateAbsorption";

        const string k_shaderPath = "Shaders/FDTD";
        const string k_ZeroKernelName = "KernZero";
        const string k_FDTDKernelName = "KernFDTD";
        const string k_AddGeomKernelName = "KernAddBounds";
        const string k_RemoveGeomKernelName = "KernRemoveBounds";


        ComputeShader m_shader = null;
        ComputeBuffer m_gaussianBuffer = null;
        ComputeBuffer m_boundaryBuffer = null;
        ComputeBuffer m_gridOutputBuf = null;
        ComputeBuffer m_gridInputBuf = null;
        ComputeBuffer m_gridBuf = null;
        Cell[,,] m_grid = null;

        int m_FDTDKernel = -1;
        int m_ZeroKernel = -1;
        int m_addGeomKernel = -1;
        int m_removeGeomKernel = -1;

        Vector2Int m_threadGroupDim = Vector2Int.zero;
        float[] m_gaussianPulse;
        int m_gridSizeInCells1D = 0;


        public FDTD(Vector2 gridSize, PlaneverbResolution res) : base(gridSize, res)
        {
            m_shader = Resources.Load<ComputeShader>(k_shaderPath);
            m_FDTDKernel = m_shader.FindKernel(k_FDTDKernelName);
            m_ZeroKernel = m_shader.FindKernel(k_ZeroKernelName);
            m_addGeomKernel = m_shader.FindKernel(k_AddGeomKernelName);
            m_removeGeomKernel = m_shader.FindKernel(k_RemoveGeomKernelName);

            m_shader.GetKernelThreadGroupSizes(m_FDTDKernel, out uint x, out uint y, out uint _);
            m_threadGroupDim = new Vector2Int((int)x, (int)y);


            int planeSize = m_gridSizeInCells.x * m_gridSizeInCells.y;
            int numSamples = GetResponseLength();
            int totalSize = planeSize * numSamples;

            m_boundaryBuffer = new ComputeBuffer(planeSize, Marshal.SizeOf(typeof(BoundaryInfo)));
            m_gaussianBuffer = new ComputeBuffer(numSamples, sizeof(float));
            m_gridOutputBuf = new ComputeBuffer(planeSize, Marshal.SizeOf(typeof(Cell)));
            m_gridInputBuf = new ComputeBuffer(planeSize, Marshal.SizeOf(typeof(Cell)));
            m_gridBuf = new ComputeBuffer(totalSize, Marshal.SizeOf(typeof(Cell)));

            m_grid = new Cell[m_gridSizeInCells.x, m_gridSizeInCells.y, GetResponseLength()];

            m_gridSizeInCells1D = totalSize;


            m_gaussianPulse = new float[numSamples];
            float sigma = 1.0f / (0.5f * Mathf.PI * (float)res);
            float delay = 2 * sigma;
            float dt = 1.0f / m_samplingRate;
            for (int i = 0; i < numSamples; ++i)
            {
                float t = i * dt;
                m_gaussianPulse[i] = Mathf.Exp(-(t - delay) * (t - delay) / (sigma * sigma));
            }

            m_gaussianBuffer.SetData(m_gaussianPulse);

            // initialize the input grid by "removing" the bounds spanning the whole grid, i.e. no geometries
            RemoveGeometryHelper(
                new PlaneVerbAABB(
                    new Vector2(gridSize.x / 2, gridSize.y / 2),
                    gridSize.x, gridSize.y, 
                    AbsorptionConstants.GetAbsorption(AbsorptionCoefficient.Default)
                )
            );
        }

        Vector2Int GetDispatchDim(Vector2Int inputDim)
        {
            int gridDimX = (inputDim.x + m_threadGroupDim.x - 1) / m_threadGroupDim.x;
            int gridDimY = (inputDim.y + m_threadGroupDim.y - 1) / m_threadGroupDim.y;
            return new Vector2Int(gridDimX, gridDimY);
        }

        public override void GenerateResponse(Vector3 listener)
        {
            Vector2Int listenerPosGrid = ToGridPos(new Vector2(listener.x, listener.z));
            Vector2Int dim = GetDispatchDim(m_gridSizeInCells);

            // bind gridDim
            m_shader.SetInts(k_gridDimShaderParam, new int[] { m_gridSizeInCells.x, m_gridSizeInCells.y, GetResponseLength() });

            ComputeBuffer inBuf = m_gridInputBuf, outBuf = m_gridOutputBuf;
            Action swapBuf = () =>
            {
                var tmp = inBuf;
                inBuf = outBuf;
                outBuf = tmp;
            };
            m_shader.SetBuffer(m_ZeroKernel, k_gridOutShaderParam, inBuf);
            m_shader.Dispatch(m_ZeroKernel, dim.x, dim.y, 1);

            // required binding: 
            //     gridDim, curTime, grid, gridIn, gridOut, boundaries, courant, listenerPos, gaussianPulse

            // bind grid
            m_shader.SetBuffer(m_FDTDKernel, k_gridShaderParam, m_gridBuf);
            // bind boundary
            m_shader.SetBuffer(m_FDTDKernel, k_boundariesShaderParam, m_boundaryBuffer);
            // bind courant
            m_shader.SetFloat(k_courantShaderParam, k_soundSpeed * m_dt / m_cellSize);
            // bind listener pos
            m_shader.SetFloats(k_listenerPosShaderParam, new float[] { listenerPosGrid.x, listenerPosGrid.y });
            // bind gaussian pulse
            m_shader.SetBuffer(m_FDTDKernel, k_gaussianPulseShaderParam, m_gaussianBuffer);

            // kernel reads from inBuf and writes to outBuf
            // outBuf is copied to m_gridBuf[offset]
            for (int t = 0; t < m_responseLength; ++t)
            {
                // bind current iteration number
                m_shader.SetInt(k_curTimeShaderParam, t);
                // pingpong grid in & out
                m_shader.SetBuffer(m_FDTDKernel, k_gridInShaderParam, inBuf);
                m_shader.SetBuffer(m_FDTDKernel, k_gridOutShaderParam, outBuf);

                m_shader.Dispatch(m_FDTDKernel, dim.x, dim.y, 1);
                swapBuf();

                // result should be in the inBuf because it's swapped before loop exit
            }

            // copy m_gridBuf to grid
            m_gridBuf.GetData(m_grid);
        }
        public override IEnumerable<Cell> GetResponse(Vector2Int gridPos)
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

        void AddGeometryHelper(PlaneVerbAABB bounds)
        {
            Vector2Int boundsMin = ToGridPos(new Vector2(bounds.min.x, bounds.min.y));
            Vector2Int boundsMax = ToGridPos(new Vector2(bounds.max.x, bounds.max.y));
            Vector2Int boundsSize = boundsMax - boundsMin + new Vector2Int(1, 1);

            m_shader.SetInts(k_gridDimShaderParam, new int[] { m_gridSizeInCells.x, m_gridSizeInCells.y });
            m_shader.SetInts(k_updateDimShaderParam, new int[] { boundsMin.x, boundsMin.y, boundsMax.x, boundsMax.y });
            m_shader.SetFloat(k_absorptionShaderParam, bounds.absorption);
            m_shader.SetBuffer(m_addGeomKernel, k_boundariesShaderParam, m_boundaryBuffer);
            m_shader.SetBuffer(m_addGeomKernel, k_gridOutShaderParam, m_gridInputBuf);

            Vector2Int dim = GetDispatchDim(boundsSize);
            m_shader.Dispatch(m_addGeomKernel, dim.x, dim.y, 1);
        }
        void RemoveGeometryHelper(PlaneVerbAABB bounds)
        {
            Vector2Int boundsMin = ToGridPos(new Vector2(bounds.min.x, bounds.min.y));
            Vector2Int boundsMax = ToGridPos(new Vector2(bounds.max.x, bounds.max.y));
            Vector2Int boundsSize = boundsMax - boundsMin + new Vector2Int(1, 1);

            m_shader.SetInts(k_gridDimShaderParam, new int[] { m_gridSizeInCells.x, m_gridSizeInCells.y });
            m_shader.SetInts(k_updateDimShaderParam, new int[] { boundsMin.x, boundsMin.y, boundsMax.x, boundsMax.y });
            m_shader.SetBuffer(m_removeGeomKernel, k_boundariesShaderParam, m_boundaryBuffer);
            m_shader.SetBuffer(m_removeGeomKernel, k_gridOutShaderParam, m_gridInputBuf);

            Vector2Int dim = GetDispatchDim(boundsSize);
            m_shader.Dispatch(m_removeGeomKernel, dim.x, dim.y, 1);
        }
        public override int AddGeometry(PlaneVerbAABB bounds)
        {
            AddGeometryHelper(bounds);
            return base.AddGeometry(bounds);
        }

        public override void RemoveGeometry(int id)
        {
            RemoveGeometryHelper(m_geometries[id]);
            base.RemoveGeometry(id);
        }

        public override void UpdateGeometry(int id, PlaneVerbAABB geom)
        {
            RemoveGeometryHelper(m_geometries[id]);
            AddGeometryHelper(geom);
            base.UpdateGeometry(id, geom);
        }

        public override void Dispose()
        {
            m_gaussianBuffer.Dispose();
            m_boundaryBuffer.Dispose();
            m_gridOutputBuf.Dispose();
            m_gridInputBuf.Dispose();
            m_gridBuf.Dispose();
        }
    }
}
