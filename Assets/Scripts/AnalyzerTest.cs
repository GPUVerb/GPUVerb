using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Text;


namespace GPUVerb
{
    public class AnalyzerTest : MonoBehaviour
    {
        [SerializeField]
        GameObject m_cubePrefab = null;
        [SerializeField]
        float m_cubeSize = 0.1f;
        [SerializeField]
        float m_baseHeight = 0;
        [SerializeField]
        float m_motionScale = 10;
        [SerializeField]
        float m_simTime = 4f;

        [SerializeField]
        Transform m_listener = null;

        bool m_simFinished = true;

        class Info
        {
            public GameObject ins;
            public Vector2 pos;
            public IEnumerator<AnalyzerResult> cur;
        }

        List<Info> m_cubeInfos = new List<Info>();


        /*private void Awake()
        {
            AnalyzerUnitTest();
        }*/


        // Start is called before the first frame update
        private void Start()
        {
            Vector2 minCorner = GPUVerbContext.Instance.MinCorner;
            Vector2 maxCorner = GPUVerbContext.Instance.MaxCorner;

            for (float x = minCorner.x; x <= maxCorner.x; x += m_cubeSize)
            {
                for (float y = minCorner.y; y <= maxCorner.y; y += m_cubeSize)
                {
                    GameObject obj;
                    if (m_cubePrefab == null)
                    {
                        obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Destroy(obj.GetComponent<Collider>());
                    }
                    else
                    {
                        obj = Instantiate(m_cubePrefab);
                    }
                    obj.transform.position = new Vector3(x, m_baseHeight, y);
                    obj.transform.localScale = Vector3.one * m_cubeSize;

                    Info info = new Info
                    {
                        ins = obj,
                        pos = new Vector2(x, y)
                    };

                    m_cubeInfos.Add(info);
                }
            }
        }

        void GetData()
        {
            AnalyzerBase solver = GPUVerbContext.Instance.AnalyzerSolver;
            FDTDBase FDTDsolver = GPUVerbContext.Instance.FDTDSolver;

            solver.AnalyzeResponses(m_listener.position);

            foreach (Info info in m_cubeInfos)
            {
                info.cur = solver.GetAnalyzerResponse(FDTDsolver.ToGridPos(info.pos)).GetEnumerator();
            }
        }

        IEnumerator Simulate()
        {
            m_simFinished = false;
            GetData();

            for (float curTime = 0; curTime <= m_simTime; curTime += Time.deltaTime)
            {
                foreach (Info info in m_cubeInfos)
                {
                    AnalyzerResult data = info.cur.Current;
                    float occlusion = data.occlusion;
                    float h = m_baseHeight + m_motionScale * occlusion;
                    info.ins.transform.position = new Vector3(info.pos.x, h, info.pos.y);
                }
                yield return new WaitForEndOfFrame();
            }

            m_simFinished = true;
        }

        private void OnGUI()
        {
            if (m_simFinished)
            {
                if (GUILayout.Button("Play"))
                {
                    StopAllCoroutines();
                    StartCoroutine(Simulate());
                }
            }
            else
            {
                GUILayout.Label("Simulating...");
            }
        }

        private void OnDrawGizmos()
        {
            if (m_listener != null)
            {
                Gizmos.DrawWireSphere(m_listener.position, 0.2f);
            }
        }


        /*private void AnalyzerUnitTest()
        {
            bool Check(AnalyzerResult[,] arr1, AnalyzerResult[,] arr2)
            {
                int mismatch = 0;
                for (int i = 0; i < arr1.GetLength(0); ++i)
                {
                    for (int j = 0; j < arr1.GetLength(1); ++j)
                    {
                        if (!arr1[i, j].Equals(arr2[i, j]))
                        {
                            ++mismatch;
                        }
                    }
                }
                if (mismatch > 0)
                {
                    Debug.LogError($"{mismatch} out of {arr1.GetLength(0) * arr1.GetLength(1) * arr1.GetLength(2)}");
                    return false;
                }
                return true;
            }

            Vector2 gridSize = new Vector2(5, 5);
            FDTDBase correct = new AnalyzerRef(gridSize, PlaneverbResolution.LowResolution);
            FDTDBase fdtd = new FDTD(gridSize, PlaneverbResolution.LowResolution);

            Vector2Int gridSizeInCells = correct.GetGridSizeInCells();
            int numSamples = correct.GetResponseLength();


            Debug.Assert(gridSizeInCells == fdtd.GetGridSizeInCells());
            Debug.Assert(numSamples == fdtd.GetResponseLength());

            Cell[,,] c1 = new Cell[gridSizeInCells.x, gridSizeInCells.y, numSamples];
            Cell[,,] c2 = new Cell[gridSizeInCells.x, gridSizeInCells.y, numSamples];

            correct.AddGeometry(new PlaneVerbAABB(new Vector2(2.5f, 2.5f), 1, 1, AbsorptionConstants.GetAbsorption(AbsorptionCoefficient.Default)));
            fdtd.AddGeometry(new PlaneVerbAABB(new Vector2(2.5f, 2.5f), 1, 1, AbsorptionConstants.GetAbsorption(AbsorptionCoefficient.Default)));

            correct.GenerateResponse(Vector3.zero);
            fdtd.GenerateResponse(Vector3.zero);

            for (int x = 0; x < gridSizeInCells.x; ++x)
            {
                for (int y = 0; y < gridSizeInCells.y; ++y)
                {
                    int z = 0;
                    foreach (Cell c in correct.GetResponse(new Vector2Int(x, y)))
                    {
                        c1[x, y, z] = c;
                        ++z;
                    }
                    z = 0;
                    foreach (Cell c in fdtd.GetResponse(new Vector2Int(x, y)))
                    {
                        c2[x, y, z] = c;
                        ++z;
                    }
                }
            }

            if (Check(c1, c2))
            {
                Debug.Log("FDTD unit test passed");
            }
            else
            {
                StringBuilder sb1 = new();
                StringBuilder sb2 = new();
                int iter = 2;
                for (int x = 0; x < gridSizeInCells.x; ++x)
                {
                    for (int y = 0; y < gridSizeInCells.y; ++y)
                    {
                        sb1.Append(c1[x, y, iter].ToString(true)); sb1.Append(',');
                        sb2.Append(c2[x, y, iter].ToString(true)); sb2.Append(',');
                    }
                    sb1.AppendLine(); sb2.AppendLine();
                }
                Debug.Log(sb1.ToString());
                Debug.Log(sb2.ToString());
                Debug.Log("FDTD unit test failed");
            }

            correct.Dispose();
            fdtd.Dispose();
        }*/
    }
}