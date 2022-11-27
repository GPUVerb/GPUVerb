using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using UnityEngine;

namespace GPUVerb
{
    public class ComputeShaderTest : MonoBehaviour
    {
        [SerializeField]
        Vector2Int m_gridDim = new Vector2Int(10, 10);

        [SerializeField]
        ComputeShader m_shader = null;
        ComputeBuffer m_InputBuf = null;
        ComputeBuffer m_OutputBuf = null;

        Cell[,] m_result = null;

        void PrintGrid()
        {

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < m_result.GetLength(0); ++i)
            {
                for (int j = 0; j < m_result.GetLength(1); ++j)
                {
                    Cell cell = m_result[i, j];
                    sb.Append(cell);
                }
                sb.AppendLine();
            }
            Debug.Log(sb.ToString());
        }

        // Start is called before the first frame update
        void Start()
        {
            int cellKernel = m_shader.FindKernel("KernTest");
            int cellZeroKernel = m_shader.FindKernel("KernZero");

            m_shader.GetKernelThreadGroupSizes(cellKernel, out uint x, out uint y, out uint _);

            Debug.Log($"Cell Struct Size = {Marshal.SizeOf(typeof(Cell))}");
            
            m_InputBuf = new ComputeBuffer(m_gridDim.x * m_gridDim.y, Marshal.SizeOf(typeof(Cell)));
            m_OutputBuf = new ComputeBuffer(m_gridDim.x * m_gridDim.y, Marshal.SizeOf(typeof(Cell)));

            m_result = new Cell[m_gridDim.x, m_gridDim.y];

            m_shader.SetInts("gridDim", new int[] { m_gridDim.x, m_gridDim.y });


            ComputeBuffer inbuf = m_InputBuf, outbuf = m_OutputBuf;
            m_shader.SetBuffer(cellZeroKernel, "gridOut", inbuf);
            m_shader.Dispatch(cellZeroKernel, (int)((m_gridDim.x + x - 1) / x), (int)((m_gridDim.y + y - 1) / y), 1);
            for (int i = 0; i < 2; ++i)
            {
                m_shader.SetBuffer(cellKernel, "gridIn", inbuf);
                m_shader.SetBuffer(cellKernel, "gridOut", outbuf);

                m_shader.Dispatch(cellKernel, (int)((m_gridDim.x + x - 1) / x), (int)((m_gridDim.y + y - 1) / y), 1);

                var tmp = inbuf;
                inbuf = outbuf;
                outbuf = tmp;

                inbuf.GetData(m_result);
                PrintGrid();
            }
        }

        private void OnDestroy()
        {
            if(m_InputBuf != null)
                m_InputBuf.Dispose();
        }
    }
}

