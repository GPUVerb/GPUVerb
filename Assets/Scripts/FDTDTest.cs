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

        // checks the similarity of two grids
        // and return the time step when two grids have the most difference
        bool Check(FDTDBase f1, FDTDBase f2, Vector3 listener, float tolerance)
        {
            Vector2Int gridSizeInCells = f1.GetGridSizeInCells();
            int numSamples = f1.GetResponseLength();
            f1.GenerateResponse(listener);
            f2.GenerateResponse(listener);

            IFDTDResult res1 = f1.GetGrid();
            IFDTDResult res2 = f2.GetGrid();

            int mostMismatchIdx = -1;
            int maxMismatch = 0;
            int totalMismatch = 0;

            for (int k = 0; k < numSamples; ++k)
            {
                int curMismatch = 0;

                for (int i = 0; i < gridSizeInCells.x; ++i)
                {
                    for (int j = 0; j < gridSizeInCells.y; ++j)
                    {
                        if (!res1[i, j, k].Equals(res2[i, j, k], tolerance))
                        {
                            ++curMismatch;
                            ++totalMismatch;
                        }
                    }
                }

                if (curMismatch > maxMismatch)
                {
                    maxMismatch = curMismatch;
                    mostMismatchIdx = k;
                }
            }
            if (mostMismatchIdx != -1)
            {
                Debug.Log($"Max Mismatch at time = {mostMismatchIdx}, cnt = {maxMismatch}, total = {totalMismatch}, " +
                    $"difference = {100.0 * totalMismatch / (gridSizeInCells.x * gridSizeInCells.y * numSamples)}%:");
                Debug.Log("==== content dump at max mismatch time ====");
                StringBuilder sb1 = new StringBuilder();
                StringBuilder sb2 = new StringBuilder();
                for (int x = 0; x < gridSizeInCells.x; ++x)
                {
                    for (int y = 0; y < gridSizeInCells.y; ++y)
                    {
                        sb1.Append(res1[x, y, mostMismatchIdx].ToString(true)); sb1.Append(", ");
                        sb2.Append(res2[x, y, mostMismatchIdx].ToString(true)); sb2.Append(", ");
                    }
                    sb1.AppendLine(); sb2.AppendLine();
                }
                Debug.Log(sb1.ToString());
                Debug.Log(sb2.ToString());
                Debug.Log("========================================");

                return totalMismatch <= 50;
            }
            return true;
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

            using FDTDBase correct = new FDTDGPU(gridSize, GPUVerbContext.Instance.SimulationRes);
            using FDTDBase solver = new FDTDGPU2(gridSize, GPUVerbContext.Instance.SimulationRes);
            Debug.Log($"Simulate using {solver.GetType().Name} and check agains {correct.GetType().Name}");


            var gameObjs = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach(var go in gameObjs)
            {
                var geoms = go.GetComponentsInChildren<FDTDGeometry>();
                foreach(var geom in geoms)
                {
                    correct.AddGeometry(geom.GetBounds());
                    solver.AddGeometry(geom.GetBounds());
                }
            }
            correct.ProcessGeometryUpdates();
            solver.ProcessGeometryUpdates();

            if(!Check(correct, solver, Listener.Position, 0.1f))
            {
                m_simFinished = true;
                yield break;
            }

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

        void PrintGrid(FDTDBase fdtd, int time)
        {
            StringBuilder sb = new StringBuilder();
            var grid = fdtd.GetGrid();
            Vector2Int gridSizeInCells = fdtd.GetGridSizeInCells();

            for (int i = 0; i < gridSizeInCells.x; ++i)
            {
                for (int j = 0; j < gridSizeInCells.y; ++j)
                {
                    sb.Append(grid[i, j, time].ToString(true));
                    sb.Append(' ');
                }
                sb.AppendLine();
            }
            Debug.Log(sb.ToString());
        }

        private void FDTDUnitTest()
        {
            Debug.Log("Begin FDTD unit test");

            Vector2 gridSize = new Vector2(10, 10);
            using FDTDBase correct = new FDTDGPU(gridSize, PlaneverbResolution.LowResolution);
            using FDTDBase fdtd = new FDTDGPU2(gridSize, PlaneverbResolution.LowResolution);

            Vector2Int gridSizeInCells = correct.GetGridSizeInCells();
            int numSamples = correct.GetResponseLength();

            Debug.Assert(gridSizeInCells == fdtd.GetGridSizeInCells());
            Debug.Assert(numSamples == fdtd.GetResponseLength());

            correct.AddGeometry(new PlaneVerbAABB(new Vector2(2.5f, 2.5f), 1, 1, AbsorptionConstants.GetAbsorption(AbsorptionCoefficient.Default)));
            fdtd.AddGeometry(new PlaneVerbAABB(new Vector2(2.5f, 2.5f), 1, 1, AbsorptionConstants.GetAbsorption(AbsorptionCoefficient.Default)));
            Debug.Assert(correct.ProcessGeometryUpdates());
            Debug.Assert(fdtd.ProcessGeometryUpdates());

            Check(correct, fdtd, Vector3.one, 0.1f);
            PrintGrid(correct, 20);
            PrintGrid(fdtd, 20);

            Debug.Log("end FDTD unit test");
        }
    }
}
