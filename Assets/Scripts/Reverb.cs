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
		private static int m_runtimeIndex = 0;

		private void Awake()
		{
			Debug.Assert(m_index != ReverbIndex.COUNT, "PlaneverbReverb MyIndex not set properly!");
			// ensure must be attached to an object with an AudioSource component
			Debug.Assert(GetComponent<AudioSource>() != null,
				"PlaneverbReverb component attached to GameObject without an AudioSource!");
		}

		private void OnAudioFilterRead(float[] data, int channels)
		{
			int dataBufferLength = data.Length;
			HashSet<Emitter> emitters = AudioManager.Instance.Emitters;

			// case: first reverb component to run during this audio frame, and there are emitters playing
			if (m_runtimeIndex == 0 && emitters.Count > 0)
			{
				// get source buffer from each PVDSP Audio Source
				float[] buffer;
				foreach (Emitter emitter in emitters)
				{
					buffer = emitter.GetSource(dataBufferLength);
					GPUVerbContext.Instance.DSP.SendSource(
						emitter.ID,
						emitter.AcousticData,
						buffer, dataBufferLength,
						channels);
				}
			}

			// increment the runtime index looping back around to zero from 3
			m_runtimeIndex = (m_runtimeIndex + 1) % (int)ReverbIndex.COUNT;

			float[] dspOutput = GPUVerbContext.Instance.DSP.GetOutput(m_index);

			// fill the in/out data buffer IFF output was processed successfully
			if (dspOutput != null)
			{
				// choose the right length in case data buffer too big
				dataBufferLength = (dataBufferLength > dspOutput.Length) ? dspOutput.Length : dataBufferLength;

				// memcpy the data over
				Array.Copy(dspOutput, data, dataBufferLength);
			}
			// case that the PVDSP module couldn't generate valid output
			else
			{
				// fill output with 0
				for (int i = 0; i < dataBufferLength; ++i)
				{
					data[i] = 0f;
				}
			}
		}
	}

}
