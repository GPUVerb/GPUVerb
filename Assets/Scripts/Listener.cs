using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUVerb
{
    public class Listener : SingletonBehavior<Listener>
    {
        public static Vector3 Position => Instance.transform.position;
        public static Vector3 Forward => Instance.transform.forward;
    }
}