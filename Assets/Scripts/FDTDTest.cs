using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


namespace GPUVerb
{
    public class FDTDTest : MonoBehaviour
    {
        FDTDBase solver;
        // Start is called before the first frame update
        void Start()
        {
            solver = new FDTDRef(new Vector2Int(10, 10), PlaneverbResolution.LowResolution);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
