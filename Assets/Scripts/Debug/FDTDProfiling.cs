using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUVerb
{
    public class FDTDProfiling : MonoBehaviour
    {
        public class ThreadWrapper
        {
            public Vector3 pos;
            public FDTDBase solver;
            public double result;

            public ThreadWrapper(FDTDBase solver, Vector3 pos)
            {
                this.solver = solver;
                this.pos = pos;
            }
            public void Execute()
            {
                Stopwatch sw = Stopwatch.StartNew();
                solver.GenerateResponse(pos);
                sw.Stop();

                result = sw.Elapsed.TotalMilliseconds;
            }
        }

#if UNITY_EDITOR
        [SerializeField] Vector2 m_gridMaxCorner = new Vector2(100,100);

        // Update is called once per frame
        void Test(string path, bool cpu)
        {
            using(var writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write)))
            {
                bool tooslow = false;
                for (Vector2 gridSize = new Vector2(5, 5);
                    gridSize.x <= m_gridMaxCorner.x;
                    gridSize += Vector2.one)
                {
                    double timeCPU = -1, timeGPU, timeGPU2;

                    if(cpu)
                    {
                        if (!tooslow)
                        {
                            using (var fdtd = new FDTDCPU(gridSize, PlaneverbResolution.MidResolution))
                            {
                                // make it run in a thread because CPU version is very inefficient
                                ThreadWrapper wrapper = new ThreadWrapper(fdtd, gridSize / 2);
                                Thread thread = new Thread(new ThreadStart(wrapper.Execute));
                                thread.Start();
                                thread.Join(new System.TimeSpan(0, 1, 0));
                                if (thread.IsAlive)
                                {
                                    thread.Abort();
                                    tooslow = true;
                                    timeCPU = -1;
                                }
                                else
                                {
                                    timeCPU = wrapper.result;
                                }
                            }
                        }
                    }

                    using (var fdtd = new FDTDGPU(gridSize, PlaneverbResolution.MidResolution))
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        fdtd.GenerateResponse(gridSize / 2);

                        // force sync to cpu
                        var _ = fdtd.GetGrid()[0, 0, 0];

                        sw.Stop();
                        timeGPU = sw.Elapsed.TotalMilliseconds;
                    }
                    using (var fdtd = new FDTDGPU2(gridSize, PlaneverbResolution.MidResolution))
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        fdtd.GenerateResponse(gridSize / 2);

                        // force sync to cpu
                        var _ = fdtd.GetGrid()[0, 0, 0];

                        sw.Stop();
                        timeGPU2 = sw.Elapsed.TotalMilliseconds;
                    }

                    if(cpu)
                    {
                        writer.WriteLine($"{gridSize.x}x{gridSize.y},{timeCPU},{timeGPU},{timeGPU2}");
                    }
                    else
                    {
                        writer.WriteLine($"{gridSize.x}x{gridSize.y},{timeGPU},{timeGPU2}");
                    }
                }
            }

        }

        void OnGUI()
        {
            if (GUILayout.Button("Test GPU and CPU"))
            {
                string path = EditorUtility.SaveFilePanel("write csv", "", "fdtd_profile", "csv");

                if (path.Length != 0)
                {
                    if (new FileInfo(path).Exists)
                    {
                        File.WriteAllText(path, string.Empty);
                    }

                    using (var writer = new StreamWriter(new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
                    {
                        writer.WriteLine("grid size, CPU time, GPU time, GPU2 time");
                    }
                }
                Test(path, true);
            }

            if (GUILayout.Button("Test GPU"))
            {
                string path = EditorUtility.SaveFilePanel("write csv", "", "fdtd_profile_gpu", "csv");

                if (path.Length != 0)
                {
                    if (new FileInfo(path).Exists)
                    {
                        File.WriteAllText(path, string.Empty);
                    }

                    using (var writer = new StreamWriter(new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
                    {
                        writer.WriteLine("grid size, GPU time, GPU2 time");
                    }
                }
                Test(path, false);
            }
        }
#endif

    }
}
