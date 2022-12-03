using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GPUVerb
{
	public class Emitter : MonoBehaviour
	{
		[SerializeField]
		AudioClip m_clip = null;

		[SerializeField]
		bool m_playOnAwake = true;

		[SerializeField]
		bool m_loop = true;

		[Range(-48f, 12f)]
		[SerializeField]
		float m_volume = 4;

		private float m_volumeGain = 0;

		[SerializeField]
		SourceDirectivityPattern m_pattern = SourceDirectivityPattern.Cardioid;

		DSPBase m_dsp = null;
		int m_id = DSPBase.k_invalidID;

		#region ClipData
		// read index into the clip data
		private int m_readIndex = 0;

		AnalyzerResult m_acousticData = new AnalyzerResult();

		// playing flag
		private bool m_isPlaying = false;

		// full buffer from the clip
		private float[] m_clipData = null;

		// buffer that's used during the audio callback
		private float[] m_runtimeArray = new float[DSPBase.k_maxFrameLen];

		// total number of samples in the clip
		private int m_samples = 0;

		#endregion

		public int ID { get => m_id; }
        public AnalyzerResult AcousticData { get => m_acousticData; }


        // Start is called before the first frame update
        void Start()
		{
			m_dsp = GPUVerbContext.Instance.DSP;
			m_volumeGain = Mathf.Pow(10f, m_volume / 20f);

			if (m_playOnAwake)
			{
				OnStartEmission();
				UpdateEmitter();
			}
		}

		// Update is called once per frame
		void Update()
		{
			if(m_id != DSPBase.k_invalidID && m_isPlaying)
            {
				UpdateEmitter();

				Vector2Int pos = GPUVerbContext.Instance.ToGridPos(new Vector2(transform.position.x, transform.position.z));
				AnalyzerResult? data = GPUVerbContext.Instance.GetOutput(pos);
				if (data != null)
				{
					m_acousticData = data.Value;
				}
			}
		}

        void OnDestroy()
        {
			OnEndEmission();
		}

        public float[] GetSource(int numSamples)
        {
			if(!m_isPlaying)
            {
				return null;
            }

			// find the end index for the clipdata buffer
			int realSamplesEnd = Mathf.Min(m_readIndex + numSamples, m_samples);

			// find the real number of samples to use (in case this is the end of the clip)
			int realSamplesToUse = realSamplesEnd - m_readIndex;

			// apply volume
			for (int i = 0, j = m_readIndex; i < realSamplesToUse; ++i, ++j)
			{
				m_runtimeArray[i] = m_clipData[j] * m_volumeGain;
			}

			// increment the readindex into the clipdata
			m_readIndex += realSamplesToUse;

			// case the clip is finished playing
			if (realSamplesToUse < numSamples)
			{
				if (!m_loop)
				{
					OnEndEmission();
				}
				else
				{
					// reset readindex
					m_readIndex = 0;

					// figure out the number of samples left to fill the data buffer
					int numSamplesLeft = numSamples - realSamplesToUse;

					// memcpy data over
					Array.Copy(m_clipData, m_readIndex, m_runtimeArray, realSamplesToUse, numSamplesLeft);

					// increment readIndex again
					m_readIndex += numSamplesLeft;
				}
			}
			return m_runtimeArray;
		}

		private void SetClip(AudioClip clip)
        {
			m_clip = clip;

			if (clip == null)
			{
				m_samples = 0;
				m_clipData = null;
				m_isPlaying = false;
			}
			else
			{
				m_samples = clip.samples * clip.channels;
				m_clipData = new float[m_samples];
				if (!clip.GetData(m_clipData, 0))
				{
					Debug.LogError($"emitter: failed to get data from clip, gameobject = {gameObject.name}");
				}
				m_isPlaying = true;
			}
		}

		private void OnStartEmission()
        {
			m_id = m_dsp.RegisterEmitter(transform.position, transform.forward);
			SetClip(m_clip);
			AudioManager.Instance.AddEmitter(this);
		}

		private void OnEndEmission()
		{
			m_dsp.RemoveEmitter(m_id);
			m_id = DSPBase.k_invalidID;
			SetClip(null);
			AudioManager.Instance.RemoveEmitter(this);
		}

		private void UpdateEmitter()
        {
			Debug.Assert(m_id != DSPBase.k_invalidID);

			m_dsp.UpdateEmitter(m_id, transform.position, transform.forward);
			m_dsp.SetEmitterDirectivityPattern(m_id, m_pattern);
		}
	}
}

