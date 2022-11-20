using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;


// test the interop between C++ and C#
public class InteropTest : MonoBehaviour
{
    [DllImport("ProjectPlaneverbUnityPlugin.dll")]
    static extern void MDArrayTest(IntPtr arr, int x, int y, int z);
    [DllImport("ProjectPlaneverbUnityPlugin.dll")]
    static extern void StructArrayTest(IntPtr arr);
    [DllImport("ProjectPlaneverbUnityPlugin.dll")]
    static extern void CopyTest(IntPtr dst, IntPtr src, int n);

    void Print<T>(T[,,] arr)
    {
        StringBuilder sb = new StringBuilder();

        for(int i=0; i<arr.GetLength(0); ++i)
        {
            for(int j=0; j<arr.GetLength(1); ++j)
            {
                for(int k=0; k<arr.GetLength(2); ++k)
                {
                    sb.Append(arr[i, j, k].ToString());
                    sb.Append(" ");
                }
                sb.Append("\n");
            }
            sb.Append("\n");
        }

        Debug.Log(sb.ToString());
    }

    void MDArrayTest()
    {
        int[,,] data = new int[3, 2, 5];
        for (int i = 0; i < data.GetLength(0); ++i)
            for (int j = 0; j < data.GetLength(1); ++j)
                for (int k = 0; k < data.GetLength(2); ++k)
                    data[i, j, k] = (i + 1) * (j + 1) * (k + 1);

        Print(data);
        data = new int[3, 2, 5];
        Print(data);

        unsafe
        {
            fixed (int* ptr = data)
            {
                MDArrayTest((IntPtr)ptr, data.GetLength(0), data.GetLength(1), data.GetLength(2));
            }
        }

        Print(data);
    }

    void MemoryTest()
    {
        int[] flat = new int[30];
        for (int i = 0; i < 2; ++i)
            for (int j = 0; j < 5; ++j)
                for (int k = 0; k < 3; ++k)
                    flat[i * 15 + j * 3 + k] = (i + 1) * (j + 1) * (k + 1);

        int[,,] data = new int[2, 5, 3];
        int[,,] refdata = new int[2, 5, 3];
        for (int i = 0; i < 2; ++i)
            for (int j = 0; j < 5; ++j)
                for (int k = 0; k < 3; ++k)
                    refdata[i,j,k] = (i + 1) * (j + 1) * (k + 1);

        unsafe
        {
            fixed(int* dst = data)
            {
                fixed(int* src = flat)
                {
                    CopyTest((IntPtr)dst, (IntPtr)src, 30);
                }
            }
        }

        Print(refdata);
        Print(data);

        for (int i = 0; i < 2; ++i)
            for (int j = 0; j < 5; ++j)
                for (int k = 0; k < 3; ++k)
                    if (data[i, j, k] != refdata[i, j, k])
                        Debug.Break();
    }

    // Start is called before the first frame update
    void Start()
    {
        // MDArrayTest();
        MemoryTest();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
