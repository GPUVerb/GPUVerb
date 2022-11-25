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

        const string k_gridDimShaderParam = "gridDim";
        const string k_shaderPath = "Shaders/FDTD";
        const string k_ZeroKernelName = "KernZero";
        const string k_FDTDKernelName = "KernFDTD";


        ComputeShader m_shader = null;
        ComputeBuffer m_gridOutputBuf = null;
        ComputeBuffer m_gridInputBuf = null;

        Cell[] m_grid = null;
        int m_FDTDKernel = -1;
        int m_ZeroKernel = -1;

        Vector2Int m_threadGroupDim = Vector2Int.zero;
        int m_gridSizeInCells1D = 0;


        public FDTD(Vector2 gridSize, PlaneverbResolution res) : base(gridSize, res)
        {
            void InitComputeShader()
            {
                m_shader = Resources.Load<ComputeShader>(k_shaderPath);
                m_FDTDKernel = m_shader.FindKernel(k_FDTDKernelName);
                m_ZeroKernel = m_shader.FindKernel(k_ZeroKernelName);

                m_shader.GetKernelThreadGroupSizes(m_FDTDKernel, out uint x, out uint y, out uint _);
                m_threadGroupDim = new Vector2Int((int)x, (int)y);

                int totalSize = m_gridSizeInCells.x * m_gridSizeInCells.y * GetResponseLength();
                m_gridOutputBuf = new ComputeBuffer(totalSize, Marshal.SizeOf(typeof(Cell)));
                m_gridInputBuf = new ComputeBuffer(totalSize, Marshal.SizeOf(typeof(Cell)));

                m_grid = new Cell[totalSize];
                m_gridSizeInCells1D = totalSize;
            }
            
            InitComputeShader();
        }

        public override void GenerateResponse(Vector3 listener)
        {
            int gridDimX = (m_gridSizeInCells.x + m_threadGroupDim.x - 1) / m_threadGroupDim.x;
            int gridDimY = (m_gridSizeInCells.y + m_threadGroupDim.y - 1) / m_threadGroupDim.y;

            m_shader.SetInts(k_gridDimShaderParam, new int[] { m_gridSizeInCells.x, m_gridSizeInCells.y });

            ComputeBuffer inBuf = m_gridInputBuf, outBuf = m_gridOutputBuf;
            Action swapBuf = () =>
            {
                var tmp = inBuf;
                inBuf = outBuf;
                outBuf = tmp;
            };

            m_shader.SetBuffer(m_ZeroKernel, k_gridOutShaderParam, inBuf);
            m_shader.Dispatch(m_ZeroKernel, gridDimX, gridDimY, 1);
                        
            for (int t = 0; t < m_responseLength; ++t)
            {
                m_shader.SetBuffer(m_FDTDKernel, k_gridInShaderParam, inBuf);
                m_shader.SetBuffer(m_FDTDKernel, k_gridOutShaderParam, outBuf);

                m_shader.Dispatch(m_FDTDKernel, gridDimX, gridDimY, 1);
                swapBuf();
            }
            // result should be in the inBuf because it's swapped before loop exit
            inBuf.GetData(m_grid);
        }
        public override IEnumerable<Cell> GetResponse(Vector2Int gridPos)
        {
            throw new System.NotImplementedException();
        }
        public override int AddGeometry(Bounds bounds)
        {
            throw new System.NotImplementedException();
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
