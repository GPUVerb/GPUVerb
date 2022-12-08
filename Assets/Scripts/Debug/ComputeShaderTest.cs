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
            // test fdtd shader
            {
                var fdtdShader = Resources.Load<ComputeShader>("Shaders/FDTD");
                int cellKernel = fdtdShader.FindKernel("KernTest");
                int cellZeroKernel = fdtdShader.FindKernel("KernZero");

                fdtdShader.GetKernelThreadGroupSizes(cellKernel, out uint x, out uint y, out uint _);
                Debug.Log($"Cell Struct Size = {Marshal.SizeOf(typeof(Cell))}");

                using var inputBuf = new ComputeBuffer(m_gridDim.x * m_gridDim.y, Marshal.SizeOf(typeof(Cell)));
                using var outputBuf = new ComputeBuffer(m_gridDim.x * m_gridDim.y, Marshal.SizeOf(typeof(Cell)));

                m_result = new Cell[m_gridDim.x, m_gridDim.y];

                fdtdShader.SetInts("gridDim", new int[] { m_gridDim.x, m_gridDim.y });

                // test buffer ping-pong
                ComputeBuffer inbuf = inputBuf, outbuf = outputBuf;
                fdtdShader.SetBuffer(cellZeroKernel, "gridOut", inbuf);
                fdtdShader.Dispatch(cellZeroKernel, (int)((m_gridDim.x + x - 1) / x), (int)((m_gridDim.y + y - 1) / y), 1);
                for (int i = 0; i < 2; ++i)
                {
                    fdtdShader.SetBuffer(cellKernel, "gridIn", inbuf);
                    fdtdShader.SetBuffer(cellKernel, "gridOut", outbuf);

                    fdtdShader.Dispatch(cellKernel, (int)((m_gridDim.x + x - 1) / x), (int)((m_gridDim.y + y - 1) / y), 1);

                    var tmp = inbuf;
                    inbuf = outbuf;
                    outbuf = tmp;

                    inbuf.GetData(m_result);
                    PrintGrid();
                }
            }

            // test shared buffer between 2 shaders
            {
                var c1 = Resources.Load<ComputeShader>("Shaders/Test/CS1");
                var c2 = Resources.Load<ComputeShader>("Shaders/Test/CS2");

                using var buf = new ComputeBuffer(8, sizeof(int));
                var data = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
                var kern1 = c1.FindKernel("CSMain");
                var kern2 = c2.FindKernel("CSMain");

                buf.SetData(data);
                c1.SetBuffer(kern1, "res", buf);
                c1.Dispatch(kern1, 1, 1, 1);
                buf.GetData(data);

                c2.SetBuffer(kern2, "res", buf);
                c2.Dispatch(kern2, 1, 1, 1);
                buf.GetData(data);

                Debug.Log(string.Join(',', data));
            }
        }
    }
}

