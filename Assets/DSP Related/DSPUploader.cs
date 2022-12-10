using System;

using UnityEngine;
using System.Runtime.InteropServices;

namespace GPUVerb {
    //enum SourceDirectivityPattern {
    //    Omni,
    //    Cardioid
    //}

    [RequireComponent(typeof(AudioSource))]
	[RequireComponent(typeof(Emitter))]
	class DSPUploader : MonoBehaviour
	{
		private enum EffectData {
			SPATIALIZE,
			MUTE_DRY,

			SMOOTHING_FACTOR,
			WET_GAIN_RATIO,
			// TODO: something for toggling dry output (on/off)

			sourcePattern,
			dryGain,
			wetGain,
			rt60,
			lowPass, // TODO: implement here? or leave up to user effects?
			direcX,
			direcY,
			sDirectivityX,
			sDirectivityY,

			numParams
		};

		AudioSource source;
		Emitter emitter;

		// public interface
		public SourceDirectivityPattern sourcePattern;
		public float SMOOTHING = 2f;
		public float WET_GAIN_RATIO = 0.1f;

		public bool SPATIALIZE = true;
		public bool SUPPRESS_DRY_SOUND = false;

		void Start()
		{
			source = GetComponent<AudioSource>();
			emitter = GetComponent<Emitter>();

			if (!source.spatialize)
			{
				Debug.Log("Non-spatialized audio source! Enable spatialization");
			}
		}

		void Update()
		{
			AnalyzerResult data = emitter.AcousticData;

			source.SetSpatializerFloat((int)EffectData.SPATIALIZE, Convert.ToSingle(SPATIALIZE));
			source.SetSpatializerFloat((int)EffectData.MUTE_DRY, Convert.ToSingle(SUPPRESS_DRY_SOUND));
			source.SetSpatializerFloat((int)EffectData.SMOOTHING_FACTOR, SMOOTHING);
			source.SetSpatializerFloat((int)EffectData.WET_GAIN_RATIO, WET_GAIN_RATIO);
			source.SetSpatializerFloat((int)EffectData.sourcePattern, (float)sourcePattern);
			source.SetSpatializerFloat((int)EffectData.dryGain, data.occlusion);
			source.SetSpatializerFloat((int)EffectData.wetGain, data.wetGain);
			source.SetSpatializerFloat((int)EffectData.rt60, data.rt60);
			source.SetSpatializerFloat((int)EffectData.lowPass, data.lowpassIntensity);
			source.SetSpatializerFloat((int)EffectData.direcX, data.direction.x);
			source.SetSpatializerFloat((int)EffectData.direcY, data.direction.y);
			source.SetSpatializerFloat((int)EffectData.sDirectivityX, data.sourceDirectivity.x);
			source.SetSpatializerFloat((int)EffectData.sDirectivityY, data.sourceDirectivity.y);
		}
	}
}