using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;


// test the interop between C++ and C#
public class InteropTest : MonoBehaviour
{
    [DllImport("ProjectPlaneverbUnityPlugin.dll")]
    static extern void MDArrayTest(IntPtr arr);
    [DllImport("ProjectPlaneverbUnityPlugin.dll")]
    static extern void StructArrayTest(IntPtr arr);


    // Start is called before the first frame update
    void Start()
    {
        int[,] data;
        data = new int[10, 10];

        GPUVerb.Cell[,] cells;
        cells = new GPUVerb.Cell[3, 3];

        unsafe
        {
            fixed (int* ptr = data)
            {
                MDArrayTest((IntPtr)ptr);
            }
            fixed (GPUVerb.Cell* ptr = cells)
            {
                StructArrayTest((IntPtr)ptr);
            }
        }

        string str = "";
        for (int i = 0; i < 10; ++i)
        {
            for (int j = 0; j < 10; ++j)
            {
                str += data[i, j].ToString() + ',';
            }
            str += '\n';
        }
        Debug.Log(str);

        str = "";
        for (int i = 0; i < 3; ++i)
        {
            for (int j = 0; j < 3; ++j)
            {
                str += $"[ {cells[i, j].b} {cells[i,j].velX} ], ";
            }
            str += '\n';
        }
        Debug.Log(str);


        for (int i = 0; i < 3; ++i)
        {
            for (int j = 0; j < 3; ++j)
            {
                cells[i,j].velX = (i + 1) * (j + 1);
                cells[i,j].b = (short)((i + 1) + (j + 1));
            }
        }

        str = "";
        for (int i = 0; i < 3; ++i)
        {
            for (int j = 0; j < 3; ++j)
            {
                str += $"[ {cells[i, j].b} {cells[i, j].velX} ], ";
            }
            str += '\n';
        }
        Debug.Log(str);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
