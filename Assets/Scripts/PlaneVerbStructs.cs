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
        public float occlution;
        public float wetGain;
        public float rt60;
        public float lowpass;
        public PlaneVerbVec2 direction;
        public PlaneVerbVec2 sourceDirectivity;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PlaneVerbAABB
    {
        public PlaneVerbVec2 position;
        public float width;
        public float height;
        public float absorption;

        public PlaneVerbAABB(PlaneVerbVec2 position, float width, float height, float absorption)
        {
            this.position = position;
            this.width = width;
            this.height = height;
            this.absorption = absorption;
        }

        public static explicit operator Bounds(PlaneVerbAABB aabb)
        {
            Vector3 pos = new Vector3(aabb.position.x, 0, aabb.position.y);
            return new Bounds(pos, new Vector3(aabb.width, 0, aabb.height));
        }
        public static explicit operator PlaneVerbAABB(Bounds bounds)
        {
            return new PlaneVerbAABB(
                new PlaneVerbVec2(bounds.center.x, bounds.center.z),
                bounds.size.x, bounds.size.z,
                AbsorptionConstants.GetAbsorption(AbsorptionCoefficient.Default));
        }
    }
}