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

        void PrintGrid(Cell[,,] result, int t)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < result.GetLength(0); ++i)
            {
                for (int j = 0; j < result.GetLength(1); ++j)
                {
                    Cell cell = result[i, j, t];
                    sb.Append(cell.ToString(true));
                    sb.Append(' ');
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
                Debug.Log("---FDTD SANITY TEST---");
                var fdtdShader = Resources.Load<ComputeShader>("Shaders/FDTD2");
                int cellKernel = fdtdShader.FindKernel("KernTest");

                fdtdShader.GetKernelThreadGroupSizes(cellKernel, out uint x, out uint y, out uint _);
                Debug.Log($"Cell Struct Size = {Marshal.SizeOf(typeof(Cell))}");

                using var inputBuf = new ComputeBuffer(m_gridDim.x * m_gridDim.y * 3, Marshal.SizeOf(typeof(Cell)));
                Cell[,,] result = new Cell[m_gridDim.x, m_gridDim.y, 3];

                fdtdShader.SetInts("gridDim", new int[] { m_gridDim.x, m_gridDim.y, 3 });
                fdtdShader.SetBuffer(cellKernel, "grid", inputBuf);
                for (int i = 0; i < 3; ++i)
                {
                    fdtdShader.SetInt("curTime", i);
                    fdtdShader.Dispatch(cellKernel, (int)((m_gridDim.x + x - 1) / x), (int)((m_gridDim.y + y - 1) / y), 1);
                    inputBuf.GetData(result);
                    PrintGrid(result, i);
                }
            }

            // test shared buffer between 2 shaders
            {
                Debug.Log("---SHARING COMPUTE BUFFER TEST---");
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

            // test loop dispatch vs single dispatch
            {
                Debug.Log("---DISPATCH TEST---");
                var shader = Resources.Load<ComputeShader>("Shaders/Test/CS1");
                using var buf = new ComputeBuffer(8, sizeof(int));
                var data = new int[8];

                var bigKern = shader.FindKernel("bigKernel");
                var smallKern = shader.FindKernel("smallKernel");
                shader.SetBuffer(bigKern, "res", buf);
                shader.Dispatch(bigKern, 1, 1, 1);
                buf.GetData(data);
                Debug.Log(string.Join(',', data));

                Array.Fill(data, 0);
                buf.SetData(data);

                shader.SetBuffer(smallKern, "res", buf);
                for (int i=0; i<8; ++i)
                {
                    shader.SetInt("iter", i);
                    shader.Dispatch(smallKern, 1, 1, 1);
                }
                buf.GetData(data);
                Debug.Log(string.Join(',', data));
            }
        }
    }
}

