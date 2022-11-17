using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUVerb
{
    // TODO: call into the planeverb DLL
    // this is a wrapper for the planeverb FDTD to test our FDTD's correctness
    public class FDTDRef : ISolver
    {
        public void GenerateResponse(Vector3 listener)
        {
            throw new System.NotImplementedException();
        }

        public Cell GetResponse(Vector2 gridPos)
        {
            throw new System.NotImplementedException();
        }
    }
}
