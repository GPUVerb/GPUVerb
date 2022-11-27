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
        const string k_gridInShaderParam = "gridIn";
        const string k_gridOutShaderParam = "gridOut";
        const string k_boundariesShaderParam = "boundaries";

        const string k_courantShaderParam = "courant";


        const string k_gridDimShaderParam = "gridDim";
        const string k_updateDimShaderParam = "updateDim";

        const string k_shaderPath = "Shaders/FDTD";
        const string k_ZeroKernelName = "KernZero";
        const string k_FDTDKernelName = "KernFDTD";
        const string k_AddGeomKernelName = "KernAddBounds";


        ComputeShader m_shader = null;

        ComputeBuffer m_boundaryBuffer = null;
        ComputeBuffer m_gridOutputBuf = null;
        ComputeBuffer m_gridInputBuf = null;
        ComputeBuffer m_gridBuf = null;
        Cell[,,] m_grid = null;

        int m_FDTDKernel = -1;
        int m_ZeroKernel = -1;
        int m_AddGeomKernel = -1;

        Vector2Int m_threadGroupDim = Vector2Int.zero;
        int m_gridSizeInCells1D = 0;


        public FDTD(Vector2 gridSize, PlaneverbResolution res) : base(gridSize, res)
        {
            void InitComputeShader()
            {
                m_shader = Resources.Load<ComputeShader>(k_shaderPath);
                m_FDTDKernel = m_shader.FindKernel(k_FDTDKernelName);
                m_ZeroKernel = m_shader.FindKernel(k_ZeroKernelName);
                m_AddGeomKernel = m_shader.FindKernel(k_AddGeomKernelName);

                m_shader.GetKernelThreadGroupSizes(m_FDTDKernel, out uint x, out uint y, out uint _);
                m_threadGroupDim = new Vector2Int((int)x, (int)y);


                int planeSize = m_gridSizeInCells.x * m_gridSizeInCells.y;
                int totalSize = planeSize * GetResponseLength();

                m_boundaryBuffer = new ComputeBuffer(planeSize, Marshal.SizeOf(typeof(BoundaryInfo)));
                m_gridOutputBuf = new ComputeBuffer(planeSize, Marshal.SizeOf(typeof(Cell)));
                m_gridInputBuf = new ComputeBuffer(planeSize, Marshal.SizeOf(typeof(Cell)));
                m_gridBuf = new ComputeBuffer(totalSize, Marshal.SizeOf(typeof(Cell)));
                m_grid = new Cell[m_gridSizeInCells.x, m_gridSizeInCells.y, GetResponseLength()];

                m_gridSizeInCells1D = totalSize;
            }
            
            InitComputeShader();
        }
        ~FDTD()
        {
            m_boundaryBuffer.Dispose();
            m_gridOutputBuf.Dispose();
            m_gridInputBuf.Dispose();
            m_gridBuf.Dispose();
        }

        Vector2Int GetDispatchDim(Vector2Int inputDim)
        {
            int gridDimX = (inputDim.x + m_threadGroupDim.x - 1) / m_threadGroupDim.x;
            int gridDimY = (inputDim.y + m_threadGroupDim.y - 1) / m_threadGroupDim.y;
            return new Vector2Int(gridDimX, gridDimY);
        }

        public override void GenerateResponse(Vector3 listener)
        {
            Vector2Int dim = GetDispatchDim(m_gridSizeInCells);
            int gridDimX = dim.x;
            int gridDimY = dim.y;

            m_shader.SetInts(k_gridDimShaderParam, new int[] { m_gridSizeInCells.x, m_gridSizeInCells.y });
            m_shader.SetFloat(k_courantShaderParam, k_soundSpeed * m_dt / m_cellSize);

            ComputeBuffer inBuf = m_gridInputBuf, outBuf = m_gridOutputBuf;
            Action swapBuf = () =>
            {
                var tmp = inBuf;
                inBuf = outBuf;
                outBuf = tmp;
            };
            
            m_shader.SetBuffer(m_ZeroKernel, k_gridOutShaderParam, inBuf);
            m_shader.Dispatch(m_ZeroKernel, gridDimX, gridDimY, 1);

            // bind boundary
            m_shader.SetBuffer(m_FDTDKernel, k_boundariesShaderParam, m_boundaryBuffer);
            int planeSize = m_gridSizeInCells.x * m_gridSizeInCells.y;
            int offset = 0;
            for (int t = 0; t < m_responseLength; ++t, offset += planeSize)
            {
                // bind grid in & out
                m_shader.SetBuffer(m_FDTDKernel, k_gridInShaderParam, inBuf);
                m_shader.SetBuffer(m_FDTDKernel, k_gridOutShaderParam, outBuf);

                m_shader.Dispatch(m_FDTDKernel, gridDimX, gridDimY, 1);
                swapBuf();

                // result should be in the inBuf because it's swapped before loop exit
                ComputeBuffer.CopyCount(inBuf, m_gridBuf, offset);
            }
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
        public override int AddGeometry(Bounds bounds)
        {
            int ret = base.AddGeometry(bounds);
            Vector2Int boundsMin = ToGridPos(new Vector2(bounds.min.x, bounds.min.z));
            Vector2Int boundsMax = ToGridPos(new Vector2(bounds.max.x, bounds.max.z));
            Vector2Int boundsSize = boundsMin - boundsMax;

            m_shader.SetInts(k_gridDimShaderParam, new int[] { m_gridSizeInCells.x, m_gridSizeInCells.y });
            m_shader.SetInts(k_updateDimShaderParam, new int[] { boundsMin.x, boundsMin.y, boundsMax.x, boundsMax.y });
            m_shader.SetBuffer(m_AddGeomKernel, k_boundariesShaderParam, m_boundaryBuffer);
            m_shader.SetBuffer(m_AddGeomKernel, k_gridOutShaderParam, m_gridOutputBuf);

            Vector2Int dim = GetDispatchDim(boundsSize);
            m_shader.Dispatch(m_AddGeomKernel, dim.x, dim.y, 1);
            return ret;
        }

        public override void RemoveGeometry(int id)
        {
            throw new System.NotImplementedException();
        }

        public override void UpdateGeometry(int id, Bounds geom)
        {
            throw new System.NotImplementedException();
        }
    }
}
