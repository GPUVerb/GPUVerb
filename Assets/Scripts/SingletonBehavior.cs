using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUVerb
{
    public abstract class SingletonBehavior<T> : MonoBehaviour where T : SingletonBehavior<T>, new()
    {
        public static T Instance { get; private set; } = null;

        private void Awake()
        {
            if (Instance != null && !ReferenceEquals(Instance, this))
            {
                Debug.LogWarning($"singleton instance destroyed because one already exists of type = {typeof(T).Name}");
                Destroy(this);
            }
            else
            {
                Instance = this as T;
                Init();
            }
        }
        protected virtual void Init() { }
    }
}