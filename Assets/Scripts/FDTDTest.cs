using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text;


namespace GPUVerb
{
    public class FDTDTest : MonoBehaviour
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
        
        Transform m_listener = null;

        bool m_simFinished = true;

        class Info
        {
            public GameObject ins;
            public Vector2 pos;
            public int curSample;
        }

        List<Info> m_cubeInfos = new List<Info>();


        private void Awake()
        {
            FDTDUnitTest();
        }


        // Start is called before the first frame update
        private void Start()
        {
            m_listener = Listener.Instance.transform;

            Vector2 minCorner = GPUVerbContext.Instance.MinCorner;
            Vector2 maxCorner = GPUVerbContext.Instance.MaxCorner;

            for (float x = minCorner.x; x <= maxCorner.x; x += m_cubeSize)
            {
                for (float y = minCorner.y; y <= maxCorner.y; y += m_cubeSize)
                {
                    GameObject obj;
                    if(m_cubePrefab == null)
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

        IFDTDResult GetData(FDTDBase solver)
        {
            solver.GenerateResponse(Listener.Position);
            IFDTDResult res = solver.GetGrid();

            foreach (Info info in m_cubeInfos)
            {
                info.curSample = 0;
            }

            int cnt = 0;
            Trav(res, solver, -1, (Cell c) =>
            {
                if (Mathf.Approximately(0, c.pressure))
                    ++cnt;
            });

            Vector2Int size = solver.GetGridSizeInCells();
            int totalCnt = size.x * size.y * solver.GetResponseLength();
            if (cnt == totalCnt)
            {
                Debug.LogError("FDTD response is all zero");
            }
            return res;
        }

        IEnumerator Simulate()
        {
            m_simFinished = false;
            
            Vector2 gridSize = GPUVerbContext.Instance.MaxCorner;
            using FDTDBase solver = new FDTDGPU(gridSize, GPUVerbContext.Instance.SimulationRes);
            
            var gameObjs = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach(var go in gameObjs)
            {
                var geoms = go.GetComponentsInChildren<FDTDGeometry>();
                foreach(var geom in geoms)
                {
                    solver.AddGeometry(geom.GetBounds());
                }
            }
            solver.ProcessGeometryUpdates();
            IFDTDResult res = GetData(solver);

            int numSamples = solver.GetResponseLength();
            int lastSample = -1;
            for(float curTime = 0; curTime <= m_simTime; curTime += Time.deltaTime)
            {
                int sample = Mathf.FloorToInt(curTime / m_simTime * numSamples);
                if(sample >= numSamples)
                {
                    break;
                }

                foreach (Info info in m_cubeInfos)
                {
                    if(lastSample != sample)
                    {
                        ++info.curSample;
                    }

                    Vector2Int posGrid = solver.ToGridPos(info.pos);

                    float pr = res[posGrid.x, posGrid.y, info.curSample].pressure;
                    float h = m_baseHeight + m_motionScale * pr;
                    info.ins.transform.position = new Vector3(info.pos.x, h, info.pos.y);
                }

                lastSample = sample;
                yield return new WaitForEndOfFrame();
            }

            m_simFinished = true;
        }

        private void OnGUI()
        {
            if(m_simFinished)
            {
                if (GUILayout.Button("Play"))
                {
                    StopAllCoroutines();
                    StartCoroutine(Simulate());
                }
            } else
            {
                GUILayout.Label("Simulating...");
            }
        }

        private void OnDrawGizmos()
        {
            if(m_listener != null)
            {
                Gizmos.DrawWireSphere(m_listener.position, 0.2f);
            }
        }

        void Trav(IFDTDResult res, FDTDBase solver, int iter, Action<Cell> func)
        {
            Vector2Int gridSizeInCells = solver.GetGridSizeInCells();

            for (int x = 0; x < gridSizeInCells.x; ++x)
            {
                for (int y = 0; y < gridSizeInCells.y; ++y)
                {
                    for (int z = 0; z < solver.GetResponseLength(); ++z)
                    {
                        if (iter == z || iter == -1)
                        {
                            func(res[x, y, z]);
                        }
                    }
                }
            }
        }

        private void FDTDUnitTest()
        {
            // checks the similarity of two grids
            // and return the time step when two grids have the most difference
            bool Check(Cell[,,] arr1, Cell[,,] arr2, float tolerance, out int idx, out int cnt)
            {
                int mostMismatchIdx = -1;
                int maxMismatch = 0;


                for (int k = 0; k < arr1.GetLength(2); ++k)
                {
                    int curMismatch = 0;

                    for (int i = 0; i < arr1.GetLength(0); ++i)
                    {
                        for (int j = 0; j < arr1.GetLength(1); ++j)
                        {
                            if (!arr1[i, j, k].Equals(arr2[i, j, k], tolerance))
                            {
                                ++curMismatch;
                            }
                        }
                    }

                    if (curMismatch > maxMismatch)
                    {
                        maxMismatch = curMismatch;
                        mostMismatchIdx = k;
                    }
                }

                cnt = maxMismatch;
                idx = mostMismatchIdx;
                if (mostMismatchIdx != -1)
                {
                    return false;
                }
                return true;
            }

            Vector2 gridSize = new Vector2(5, 5);
            using FDTDBase correct = new FDTDGPU(gridSize, PlaneverbResolution.LowResolution);
            using FDTDBase fdtd = new FDTDGPU2(gridSize, PlaneverbResolution.LowResolution);

            Vector2Int gridSizeInCells = correct.GetGridSizeInCells();
            int numSamples = correct.GetResponseLength();

            Debug.Assert(gridSizeInCells == fdtd.GetGridSizeInCells());
            Debug.Assert(numSamples == fdtd.GetResponseLength());

            Cell[,,] c1 = new Cell[gridSizeInCells.x, gridSizeInCells.y, numSamples];
            Cell[,,] c2 = new Cell[gridSizeInCells.x, gridSizeInCells.y, numSamples];

            correct.AddGeometry(new PlaneVerbAABB(new Vector2(2.5f, 2.5f), 1, 1, AbsorptionConstants.GetAbsorption(AbsorptionCoefficient.Default)));
            fdtd.AddGeometry(new PlaneVerbAABB(new Vector2(2.5f, 2.5f), 1, 1, AbsorptionConstants.GetAbsorption(AbsorptionCoefficient.Default)));
            Debug.Assert(correct.ProcessGeometryUpdates());
            Debug.Assert(fdtd.ProcessGeometryUpdates());

            void GetResponse(Cell[,,] input1, Cell[,,] input2)
            {
                correct.GenerateResponse(Vector3.one);
                fdtd.GenerateResponse(Vector3.one);

                IFDTDResult resCorrect = correct.GetGrid();
                IFDTDResult res = fdtd.GetGrid();

                for (int x = 0; x < gridSizeInCells.x; ++x)
                {
                    for (int y = 0; y < gridSizeInCells.y; ++y)
                    {
                        for (int z = 0; z < correct.GetResponseLength(); ++z)
                        {
                            input1[x, y, z] = resCorrect[x, y, z];
                            input2[x, y, z] = res[x, y, z];
                        }
                    }
                }
            }

            GetResponse(c1, c2);
            if (!Check(c1, c2, 0.1f, out int iter, out int cnt))
            {
                Debug.Log($"Mismatch at time {iter}, cnt = {cnt}:");

                StringBuilder sb1 = new StringBuilder();
                StringBuilder sb2 = new StringBuilder();
                for (int x = 0; x < gridSizeInCells.x; ++x)
                {
                    for (int y = 0; y < gridSizeInCells.y; ++y)
                    {
                        sb1.Append(c1[x, y, iter].ToString(true)); sb1.Append(", ");
                        sb2.Append(c2[x, y, iter].ToString(true)); sb2.Append(", ");
                    }
                    sb1.AppendLine(); sb2.AppendLine();
                }
                Debug.Log(sb1.ToString());
                Debug.Log(sb2.ToString());
            }

            Cell[,,] last1 = new Cell[gridSizeInCells.x, gridSizeInCells.y, numSamples];
            Cell[,,] last2 = new Cell[gridSizeInCells.x, gridSizeInCells.y, numSamples];
            int linearSize = gridSizeInCells.x * gridSizeInCells.y * numSamples;
            for (int i=0; i<10; ++i)
            {
                GetResponse(c1, c2);

                if(i > 0)
                {
                    if(!Check(c1, last1, 0.0001f, out int _, out int _))
                    {
                        Debug.Log("Data inconsistency detected in Planverb FDTD");   
                    }
                    if(!Check(c2, last2, 0.0001f, out int _, out int _))
                    {
                        Debug.Log("Data inconsistency detected in GPU FDTD");
                    }
                }

                Array.Copy(c1, last1, linearSize);
                Array.Copy(c2, last2, linearSize);
            }
        }
    }
}
