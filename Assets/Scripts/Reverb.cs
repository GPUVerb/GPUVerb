using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUVerb
{
	public enum ReverbIndex
	{
		DRY, A, B, C, COUNT
	};

	[RequireComponent(typeof(AudioSource))]
	public class Reverb : MonoBehaviour
	{
		// a value on the range [0, 3), represents the index into the output fetcher array
		[SerializeField]
		ReverbIndex m_index = ReverbIndex.COUNT;

		private DSPBase m_dsp;
		private float[] m_dspOutput = new float[DSPBase.k_maxFrameLen];
		private static int s_runtimeIndex = 0;
		private static bool s_processFlag = false;


		private void Awake()
		{
			Debug.Assert(m_index != ReverbIndex.COUNT, "PlaneverbReverb MyIndex not set properly!");
			// ensure must be attached to an object with an AudioSource component
			Debug.Assert(GetComponent<AudioSource>() != null,
				"PlaneverbReverb component attached to GameObject without an AudioSource!");
		}

        private void Start()
        {
			m_dsp = GPUVerbContext.Instance.DSP;
		}

        private void OnAudioFilterRead(float[] data, int channels)
		{
			int dataBufferLength = data.Length;
			HashSet<Emitter> emitters = AudioManager.Instance.Emitters;

			// case: first reverb component to run during this audio frame, and there are emitters playing
			if (s_runtimeIndex == 0 && emitters.Count > 0)
			{
				// get source buffer from each PVDSP Audio Source
				float[] buffer;
				foreach (Emitter emitter in emitters)
				{
					buffer = emitter.GetSource(dataBufferLength);
					m_dsp.SendSource(
						emitter.ID,
						emitter.AcousticData,
						buffer, dataBufferLength,
						channels);
				}

				s_processFlag = m_dsp.GenerateOutput();
			}

			// increment the runtime index looping back around to zero from 3
			s_runtimeIndex = (s_runtimeIndex + 1) % (int)ReverbIndex.COUNT;

			// fill the in/out data buffer IFF output was processed successfully
			if (s_processFlag)
			{
				m_dsp.GetOutput(m_index, m_dspOutput);

				// choose the right length in case data buffer too big
				dataBufferLength = dataBufferLength > m_dspOutput.Length ? m_dspOutput.Length : dataBufferLength;

				// memcpy the data over
				Array.Copy(m_dspOutput, data, dataBufferLength);
			}
			// case that the PVDSP module couldn't generate valid output
			else
			{
				// fill output with 0
				Array.Fill(m_dspOutput, 0f);
			}
		}
	}

}
