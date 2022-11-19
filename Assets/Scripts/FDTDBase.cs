using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


namespace GPUVerb
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Cell
    {
        public float pressure; // air pressure
        public float velX;     // x component of particle velocity
        public float velY;     // y component of particle velocity
        [MarshalAs(UnmanagedType.I2)]
        public short b;        // B field packed into 2 2 byte fields
        [MarshalAs(UnmanagedType.I2)]
        public short by;       // B field packed into 2 2 byte fields

        public Cell(float pressure = 0, float velX = 0, float velY = 0, short b = 1, short by = 1)
        {
            this.pressure = pressure;
            this.velX = velX;
            this.velY = velY;
            this.b = b;
            this.by = by;
        }
    }

    public abstract class FDTDBase
    {
        public FDTDBase(Vector2Int gridSize, PlaneverbResolution res) { }
        private FDTDBase() { }
        public abstract void GenerateResponse(Vector3 listener);
        public abstract IEnumerable<Cell> GetResponse(Vector2Int gridPos);
    }
}
