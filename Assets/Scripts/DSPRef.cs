using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GPUVerb
{
    // wrapper for the planeverb DSP.
    public class DSPRef : DSPBase
    {
		#region DLL Interface
		private const string DLLNAME = "PlaneverbDSPUnityPlugin";

		[DllImport(DLLNAME)]
		private static extern void PlaneverbDSPInit(int maxCallbackLength, int samplingRate,
		int dspSmoothingFactor, bool useSpatialization, float wetGainRatio);

		[DllImport(DLLNAME)]
		private static extern void PlaneverbDSPExit();

		[DllImport(DLLNAME)]
		private static extern void PlaneverbDSPSetListenerTransform(float posX, float posY, float posZ,
		float forwardX, float forwardY, float forwardZ);

		[DllImport(DLLNAME)]
		private static extern void PlaneverbDSPUpdateEmitter(int emissionID, float posX, float posY, float posZ,
			float forwardX, float forwardY, float forwardZ);

		[DllImport(DLLNAME)]
		private static extern void PlaneverbDSPSetEmitterDirectivityPattern(int emissionId, int pattern);

		[DllImport(DLLNAME)]
		private static extern void PlaneverbDSPSendSource(int emissionID, in GPUVerb.AnalyzerResult dspParams,
			float[] input, int numFrames);

		[DllImport(DLLNAME)]
		private static extern bool PlaneverbDSPProcessOutput();

		[DllImport(DLLNAME)]
		private static extern void PlaneverbDSPGetDryBuffer(ref IntPtr buf);

		[DllImport(DLLNAME)]
		private static extern void PlaneverbDSPGetBufferA(ref IntPtr ptrArray);

		[DllImport(DLLNAME)]
		private static extern void PlaneverbDSPGetBufferB(ref IntPtr ptrArray);

		[DllImport(DLLNAME)]
		private static extern void PlaneverbDSPGetBufferC(ref IntPtr ptrArray);

		private delegate void FetchOutputBuffer(ref IntPtr ptrArray);
		private static FetchOutputBuffer[] s_outputFetchers =
		{
			PlaneverbDSPGetDryBuffer,
			PlaneverbDSPGetBufferA,
			PlaneverbDSPGetBufferB,
			PlaneverbDSPGetBufferC
		};

		#endregion
		public DSPRef(DSPConfig config) : base(config)
        {
			PlaneverbDSPInit(config.maxCallbackLength, config.samplingRate, 
				config.dspSmoothingFactor, config.useSpatialization, config.wetGainRatio);
		}
		public override void Dispose()
		{
			PlaneverbDSPExit();
		}

		public override void SetListenerPos(Vector3 pos, Vector3 forward)
        {
			PlaneverbDSPSetListenerTransform(pos.x, pos.y, pos.z, forward.x, forward.y, forward.z);
		}


		int m_nextEmitterId = 0;
		public override int RegisterEmitter(Vector3 pos, Vector3 forward)
		{
			PlaneverbDSPUpdateEmitter(m_nextEmitterId, pos.x, pos.y, pos.z, forward.x, forward.y, forward.z);
			return m_nextEmitterId++;
		}
		public override void UpdateEmitter(int id, Vector3 pos, Vector3 forward)
        {
			PlaneverbDSPUpdateEmitter(id, pos.x, pos.y, pos.z, forward.x, forward.y, forward.z);
		}
		public override void RemoveEmitter(int id)
		{
			
		}
		public override void SetEmitterDirectivityPattern(int id, SourceDirectivityPattern pattern)
        {
			PlaneverbDSPSetEmitterDirectivityPattern(id, (int)pattern);
		}

        public override void SendSource(int id, in AnalyzerResult param, float[] data, int numSamples, int channels)
        {
			int frames = numSamples / channels;
			PlaneverbDSPSendSource(id, param, data, frames);
		}

        public override float[] GetOutput(ReverbIndex reverb)
        {
            if(!PlaneverbDSPProcessOutput())
            {
				return null;
            }

			if (reverb == ReverbIndex.COUNT)
            {
				return null;
			}

			// fetch the buffer
			IntPtr result = IntPtr.Zero;
			s_outputFetchers[(int)reverb](ref result);

			// copy the buffer as a float array
			float[] buf = new float[k_maxFrameLen];
			Marshal.Copy(result, buf, 0, k_maxFrameLen);
			return buf;
		}
    }

}
