using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


namespace GPUVerb
{
    public class FDTDTest : MonoBehaviour
    {
        [SerializeField]
        float m_cubeSize = 0.1f;
        [SerializeField]
        float m_baseHeight = 1;
        [SerializeField]
        float m_motionScale = 50;
        [SerializeField]
        float m_simTime = 4f;
        [SerializeField]
        Vector2 m_minCorner = new Vector2(0, 0);
        [SerializeField]
        Vector2 m_maxCorner = new Vector2(9, 9);
        [SerializeField]
        Transform m_listener;

        class Info
        {
            public GameObject ins;
            public Vector2 pos;
            public IEnumerator<Cell> cur;
        }

        List<Info> m_cubeInfos = new List<Info>();
        FDTDBase m_solver;


        // Start is called before the first frame update
        void Start()
        {
            m_solver = new FDTDRef(
                new Vector2Int(Mathf.CeilToInt(m_maxCorner.x), Mathf.CeilToInt(m_maxCorner.y)), 
                PlaneverbResolution.LowResolution);

            for (float x = m_minCorner.x; x <= m_maxCorner.x; x += m_cubeSize)
            {
                for (float y = m_minCorner.y; y <= m_maxCorner.y; y += m_cubeSize)
                {
                    GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.transform.position = new Vector3(x, m_baseHeight, y);
                    obj.transform.localScale = Vector3.one * m_cubeSize;
                    
                    Destroy(obj.GetComponent<Collider>());

                    Info info = new Info();
                    info.ins = obj;
                    info.pos = new Vector2(x, y);

                    m_cubeInfos.Add(info);
                }
            }
        }

        IEnumerator Simulate()
        {
            m_solver.GenerateResponse(m_listener.position);
            foreach (Info info in m_cubeInfos)
            {
                info.cur = m_solver.GetResponse(m_solver.ToGridPos(info.pos)).GetEnumerator();
            }

            int numSamples = m_solver.GetResponseLength();
            int lastSample = -1;
            for(float curTime = 0; curTime <= m_simTime; curTime += Time.deltaTime)
            {
                int sample = Mathf.FloorToInt(curTime / m_simTime);
                if(sample >= numSamples)
                {
                    break;
                }

                foreach (Info info in m_cubeInfos)
                {
                    if(lastSample != sample)
                    {
                        info.cur.MoveNext();
                    }

                    Cell data = info.cur.Current;
                    float h = m_baseHeight + m_motionScale * data.pressure;
                    info.ins.transform.position = new Vector3(info.pos.x, h, info.pos.y);

                    if (!Mathf.Approximately(h, m_baseHeight))
                        Debug.Break();
                }

                lastSample = sample;
                yield return new WaitForEndOfFrame();
            }
        }

        private void OnGUI()
        {
            if(GUILayout.Button("Play"))
            {
                StopAllCoroutines();
                StartCoroutine(Simulate());
            }
        }
    }
}
