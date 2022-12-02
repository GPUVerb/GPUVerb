using UnityEngine;
using System.Runtime.InteropServices;

namespace GPUVerb
{
    public enum PlaneverbResolution
    {
        LowResolution = 275,
        MidResolution = 375,
        HighResolution = 500,
        ExtremeResolution = 750,
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct PlaneVerbVec2
    {
        public double x;
        public double y;

        public PlaneVerbVec2(double x, double y) 
        {
            this.x = x;
            this.y = y;
        }

        public static implicit operator Vector2(PlaneVerbVec2 vec2) => new Vector2((float)(vec2.x), (float)(vec2.y));
        public static implicit operator PlaneVerbVec2(Vector2 vec2) => new PlaneVerbVec2(vec2.x, vec2.y);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PlaneVerbOutput
    {
        public double occlusion;
        public double wetGain;
        public double rt60;
        public double lowpass;
        public PlaneVerbVec2 direction;
        public PlaneVerbVec2 sourceDirectivity;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PlaneVerbAABB
    {
        public PlaneVerbVec2 position;
        public double width;
        public double height;
        public double absorption;
        public Vector2 min
        {
            get => new Vector2((float)(position.x - width / 2), (float)(position.y - height / 2));
        }
        public Vector2 max
        {
            get => new Vector2((float)(position.x + width / 2), (float)(position.y + height / 2));
        }

        public PlaneVerbAABB(PlaneVerbVec2 position, double width, double height, double absorption)
        {
            this.position = position;
            this.width = width;
            this.height = height;
            this.absorption = absorption;
        }
        public PlaneVerbAABB(Bounds bounds, double absorption)
        {
            this.position = new PlaneVerbVec2(bounds.center.x, bounds.center.z);
            this.width = bounds.size.x;
            this.height = bounds.size.z;
            this.absorption = absorption;
        }
    }
}