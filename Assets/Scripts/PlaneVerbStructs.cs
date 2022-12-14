using UnityEngine;
using System.Runtime.InteropServices;
using System;

namespace GPUVerb
{
    public enum PlaneverbResolution
    {
        LowResolution = 275,
        MidResolution = 375,
        HighResolution = 500,
        ExtremeResolution = 750,
    };
    public enum SourceDirectivityPattern
    {
        Omni,
        Cardioid
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PlaneVerbVec2
    {
        public float x;
        public float y;

        public PlaneVerbVec2(float x, float y) 
        {
            this.x = x;
            this.y = y;
        }

        public static implicit operator Vector2(PlaneVerbVec2 vec2) => new Vector2(vec2.x, vec2.y);
        public static implicit operator PlaneVerbVec2(Vector2 vec2) => new PlaneVerbVec2(vec2.x, vec2.y);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PlaneVerbOutput
    {
        public float occlusion;
        public float wetGain;
        public float rt60;
        public float lowpass;
        public float directionX;
        public float directionY;
        public float sourceDirectivityX;
        public float sourceDirectivityY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PlaneVerbAABB : IEquatable<PlaneVerbAABB>
    {
        // an empty aabb
        public static PlaneVerbAABB s_empty = new PlaneVerbAABB(new PlaneVerbVec2(-1, -1), 0, 0, 0);
        public PlaneVerbVec2 position;
        public float width;
        public float height;
        public float absorption;
        public Vector2 min
        {
            get => new Vector2(position.x - width / 2, position.y - height / 2);
        }
        public Vector2 max
        {
            get => new Vector2(position.x + width / 2, position.y + height / 2);
        }
        public PlaneVerbAABB(PlaneVerbVec2 position, float width, float height, float absorption)
        {
            this.position = position;
            this.width = width;
            this.height = height;
            this.absorption = absorption;
        }
        public PlaneVerbAABB(Bounds bounds, float absorption)
        {
            this.position = new PlaneVerbVec2(bounds.center.x, bounds.center.z);
            this.width = bounds.size.x;
            this.height = bounds.size.z;
            this.absorption = absorption;
        }

        public bool Equals(PlaneVerbAABB other)
        {
            return Mathf.Approximately(other.position.x, position.x) &&
                Mathf.Approximately(other.position.y, position.y) &&
                Mathf.Approximately(other.width, width) &&
                Mathf.Approximately(other.height, height) &&
                Mathf.Approximately(other.absorption, absorption);
        }
    }
}