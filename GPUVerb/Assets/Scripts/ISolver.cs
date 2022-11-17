using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GPUVerb
{
    public class Cell
    {
        float pressure = 0.0f; // air pressure
        float velX = 0.0f;     // x component of particle velocity
        float velY = 0.0f;     // y component of particle velocity
        short b = 1;        // B field packed into 2 2 byte fields
        short by = 1;       // B field packed into 2 2 byte fields

        public Cell(float pressure, float velX, float velY, short b, short by)
        {
            this.pressure = pressure;
            this.velX = velX;
            this.velY = velY;
            this.b = b;
            this.by = by;
        }
    }

    public interface ISolver
    {
        void GenerateResponse(Vector3 listener);
        Cell GetResponse(Vector2 gridPos);
    }
}
