using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUVerb
{
    public class AudioManager : SingletonBehavior<AudioManager>
    {
        private HashSet<Emitter> m_emitters = new HashSet<Emitter>();
        public HashSet<Emitter> Emitters { get => m_emitters; }

        public void AddEmitter(Emitter emitter)
        {
            m_emitters.Add(emitter);
        }
        public void RemoveEmitter(Emitter emitter)
        {
            m_emitters.Remove(emitter);
        }
    }
}
