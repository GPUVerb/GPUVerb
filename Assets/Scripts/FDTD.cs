using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUVerb
{
    // TODO: our implementaiton of FDTD using compute shader
    public class FDTD : FDTDBase
    {
        public FDTD(Vector2Int gridSize, PlaneverbResolution res) : base(gridSize, res)
        {
            throw new System.NotImplementedException();
        }
        public override void GenerateResponse(Vector3 listener)
        {
            throw new System.NotImplementedException();
        }
        public override IEnumerable<Cell> GetResponse(Vector2Int gridPos)
        {
            throw new System.NotImplementedException();
        }
    }
}
