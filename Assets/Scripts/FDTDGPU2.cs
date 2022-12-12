using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace GPUVerb
{
    // TODO: our implementaiton of FDTD using compute shader
    public class FDTDGPU2 : FDTDBase
    {
        public class Result : IFDTDResult
        {
            private ComputeBuffer m_buf;
            private Cell[] m_grid;
            private int m_xdim, m_ydim, m_zdim;
            private Cell[] Grid
            {
                get
                {
                    if (m_grid == null)
                    {
                        m_grid = new Cell[m_xdim * m_ydim * m_zdim];
                        m_buf.GetData(m_grid);
                    }
                    return m_grid;
                }
            }

            public Result(ComputeBuffer buf, int xdim, int ydim, int zdim)
            {
                m_buf = buf;
                m_grid = null;
                m_xdim = xdim;
                m_ydim = ydim;
                m_zdim = zdim;
            }
            private Result() { }
            public Cell this[int x, int y, int t]
            {
                get => Grid[t * m_xdim * m_ydim + y * m_xdim + x];
            }
            public ComputeBuffer GetComputeBuffer() => m_buf;
            public Array ToArray() => Grid;
        }

        const string k_gridShaderParam = "grid";
        const string k_boundariesShaderParam = "boundaries";
        const string k_gaussianPulseShaderParam = "gaussianPulse";
        const string k_listenerPosShaderParam = "listenerPos";
        const string k_courantShaderParam = "courant";
        const string k_curTimeShaderParam = "curTime";
        const string k_gridDimShaderParam = "gridDim";
        const string k_updateDimShaderParam = "updateDim";
        const string k_absorptionShaderParam = "updateAbsorption";

        const string k_shaderPath = "Shaders/FDTD";
        const string k_ZeroKernelName = "KernZero";
        const string k_FDTDKernelName = "KernFDTD";
        const string k_AddGeomKernelName = "KernAddBounds";
        const string k_RemoveGeomKernelName = "KernRemoveBounds";

        ComputeShader m_shader = null;
        ComputeBuffer m_gaussianBuffer = null;
        ComputeBuffer m_boundaryBuffer = null;
        ComputeBuffer m_gridBuf = null;

        int m_FDTDKernel = -1;
        int m_zeroKernel = -1;
        int m_addGeomKernel = -1;
        int m_removeGeomKernel = -1;

        Vector2Int m_threadGroupDim = Vector2Int.zero;

        Result m_curResult;

        #region Load Balancing
        // how many time steps should we process for each update
        int m_timeStepsPerFixedUpdate;
        int m_curTimeStep;
        bool m_listenerSet = false;
        #endregion

        public FDTDGPU2(Vector2 gridSize, PlaneverbResolution res) : base(gridSize, res)
        {
            m_shader = Resources.Load<ComputeShader>(k_shaderPath);
            m_FDTDKernel = m_shader.FindKernel(k_FDTDKernelName);
            m_zeroKernel = m_shader.FindKernel(k_ZeroKernelName);
            m_addGeomKernel = m_shader.FindKernel(k_AddGeomKernelName);
            m_removeGeomKernel = m_shader.FindKernel(k_RemoveGeomKernelName);

            m_shader.GetKernelThreadGroupSizes(m_FDTDKernel, out uint x, out uint y, out uint _);
            m_threadGroupDim = new Vector2Int((int)x, (int)y);


            int planeSize = m_gridSizeInCells.x * m_gridSizeInCells.y;
            int totalSize = planeSize * m_responseLength;

            m_boundaryBuffer = new ComputeBuffer(planeSize, Marshal.SizeOf(typeof(BoundaryInfo)));
            m_gaussianBuffer = new ComputeBuffer(m_responseLength, sizeof(float));
            m_gridBuf = new ComputeBuffer(totalSize, Marshal.SizeOf(typeof(Cell)));
            {
                float[] data = new float[m_responseLength];
                float sigma = 1.0f / (0.5f * Mathf.PI * (float)res);
                float delay = 2 * sigma;
                float dt = 1.0f / m_samplingRate;
                for (int i = 0; i < m_responseLength; ++i)
                {
                    float t = i * dt;
                    data[i] = Mathf.Exp(-(t - delay) * (t - delay) / (sigma * sigma));
                }
                m_gaussianBuffer.SetData(data);

            }
            {
                Cell[] data = new Cell[planeSize];
                for (int i = 0; i < planeSize; ++i)
                {
                    data[i].b = data[i].by = 1;
                }
                m_gridBuf.SetData(data, 0, 0, planeSize);
            }
            {
                BoundaryInfo[] data = new BoundaryInfo[planeSize];
                for (int i = 0; i < planeSize; ++i)
                {
                    data[i].absorption = AbsorptionConstants.GetAbsorption(AbsorptionCoefficient.Vacuum);
                }
                m_boundaryBuffer.SetData(data);
            }

            // bind gridDim
            m_shader.SetInts(k_gridDimShaderParam, new int[] { m_gridSizeInCells.x, m_gridSizeInCells.y, m_responseLength });

            // bind grid
            m_shader.SetBuffer(m_zeroKernel, k_gridShaderParam, m_gridBuf);
            m_shader.SetBuffer(m_FDTDKernel, k_gridShaderParam, m_gridBuf);

            // bind boundary
            m_shader.SetBuffer(m_FDTDKernel, k_boundariesShaderParam, m_boundaryBuffer);
            // bind courant
            m_shader.SetFloat(k_courantShaderParam, k_soundSpeed * m_dt / m_cellSize);
            // bind gaussian pulse
            m_shader.SetBuffer(m_FDTDKernel, k_gaussianPulseShaderParam, m_gaussianBuffer);

            // init update
            int freq = GPUVerbContext.Instance.SimulationFreq;
            int timeStepsPerSec = m_responseLength * freq;
            m_timeStepsPerFixedUpdate = Mathf.CeilToInt(timeStepsPerSec * Time.fixedDeltaTime);
            m_curTimeStep = 0;
            m_listenerSet = false;
        }

        public override IFDTDResult GetGrid()
        {
            if (m_curResult == null)
            {
                Debug.LogWarning("FDTD result not ready but the response is wanted");
                ContinueResponse(true);
            }

            return m_curResult;
        }


        private void ContinueResponse(bool forceAll)
        {
            if (!m_listenerSet)
            {
                return;
            }

            Vector2Int dim = GetDispatchDim(m_gridSizeInCells, m_threadGroupDim);
            int steps = forceAll ? m_responseLength : m_timeStepsPerFixedUpdate;

            for (int cnt = 0;
                cnt < steps && m_curTimeStep < m_responseLength;
                ++cnt, ++m_curTimeStep)
            {
                if (m_curTimeStep == 0)
                {
                    // take care of t == 0
                    m_shader.Dispatch(m_zeroKernel, dim.x, dim.y, 1);
                }
                else
                {
                    m_shader.SetInt(k_curTimeShaderParam, m_curTimeStep);
                    m_shader.Dispatch(m_FDTDKernel, dim.x, dim.y, 1);
                }
            }

            if (m_curTimeStep == m_responseLength)
            {
                m_curTimeStep = 0;
                m_curResult = new Result(m_gridBuf, m_gridSizeInCells.x, m_gridSizeInCells.y, m_responseLength);
            }
        }

        public override void Update()
        {
            ContinueResponse(false);
        }

        public override void GenerateResponse(Vector3 listener)
        {
            // continue current response if not finished
            if (m_curTimeStep != m_responseLength)
            {
                ContinueResponse(true);
            }

            ProcessGeometryUpdates();
            // set next listener pos
            Vector2Int listenerPosGrid = ToGridPos(new Vector2(listener.x, listener.z));
            // bind listener pos
            m_shader.SetFloats(k_listenerPosShaderParam, new float[] { listenerPosGrid.x, listenerPosGrid.y });
            m_listenerSet = true;
        }

        void AddGeometryHelper(in PlaneVerbAABB bounds)
        {
            if (bounds.Equals(PlaneVerbAABB.s_empty))
            {
                return;
            }

            GetGeometryUpdateParams(bounds, out Vector2Int boundsMin, out Vector2Int boundsMax);
            m_shader.SetInts(k_updateDimShaderParam, new int[] { boundsMin.x, boundsMin.y, boundsMax.x, boundsMax.y });
            m_shader.SetFloat(k_absorptionShaderParam, bounds.absorption);
            m_shader.SetBuffer(m_addGeomKernel, k_boundariesShaderParam, m_boundaryBuffer);
            m_shader.SetBuffer(m_addGeomKernel, k_gridShaderParam, m_gridBuf);

            Vector2Int dim = GetDispatchDim(boundsMax - boundsMin + Vector2Int.one, m_threadGroupDim);
            m_shader.Dispatch(m_addGeomKernel, dim.x, dim.y, 1);
        }
        void RemoveGeometryHelper(in PlaneVerbAABB bounds)
        {
            if (bounds.Equals(PlaneVerbAABB.s_empty))
            {
                return;
            }

            GetGeometryUpdateParams(bounds, out Vector2Int boundsMin, out Vector2Int boundsMax);
            m_shader.SetInts(k_updateDimShaderParam, new int[] { boundsMin.x, boundsMin.y, boundsMax.x, boundsMax.y });
            m_shader.SetBuffer(m_removeGeomKernel, k_boundariesShaderParam, m_boundaryBuffer);
            m_shader.SetBuffer(m_removeGeomKernel, k_gridShaderParam, m_gridBuf);

            Vector2Int dim = GetDispatchDim(boundsMax - boundsMin + Vector2Int.one, m_threadGroupDim);
            m_shader.Dispatch(m_removeGeomKernel, dim.x, dim.y, 1);
        }

        protected override void DoRemoveGeometry(int id)
        {
            RemoveGeometryHelper(GetBounds(id));
        }
        protected override void DoAddGeometry(int id, in PlaneVerbAABB geom)
        {
            DoUpdateGeometry(id, geom);
        }
        protected override void DoUpdateGeometry(int id, in PlaneVerbAABB geom)
        {
            RemoveGeometryHelper(GetBounds(id));
            AddGeometryHelper(geom);
        }
        public override void Dispose()
        {
            m_gaussianBuffer.Dispose();
            m_boundaryBuffer.Dispose();
            m_gridBuf.Dispose();
        }
    }
}
