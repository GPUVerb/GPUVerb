namespace GPUVerb
{
	public static class InternalConstants
	{
		// Internal constants
		public const float GV_PI = 3.141593f;                       // PI
		public const float GV_RHO = 1.2041f;                        // air density
		public const float GV_C = 343.21f;                          // speed of sound
		public const float GV_Z_AIR = GV_RHO * GV_C;                       // natural impedance of air
		public const float GV_INV_Z_AIR = 1.0f / GV_Z_AIR;           // inverse impedance for absorbing boundaries
		public const float GV_INV_Z_REFLECT = 0.0f;                 // inverse impedance for reflecting boundaries
		public const float GV_AUDIBLE_THRESHOLD_GAIN = 0.00000316f; // precalculated -110 dB converted to linear gain
		public const float GV_DRY_DIRECTION_ANALYSIS_LENGTH = 0.005f;// length of time flux of first wavefront (source direction)
		public const float GV_DRY_GAIN_ANALYSIS_LENGTH = 0.01f;     // length of time to process the initial pulse for occlusion
		public const float GV_WET_GAIN_ANALYSIS_LENGTH = 0.080f;    // length of time to process early reflections
		public const float GV_SQRT_2 = 1.4142136f;                  // precalculated sqrt(2)
		public const float GV_SQRT_3 = 1.7320508f;                  // precalculated sqrt(3)
		public const float GV_MAX_AUDIBLE_FREQ = 20000.0f;           // maximum audible frequency for humans
		public const float GV_MIN_AUDIBLE_FREQ = 20.0f;              // minimum audible frequency for humans
		public const float GV_POINTS_PER_WAVELENGTH = 3.5f;         // number of cells per wavelength
		public const float GV_SCHROEDER_OFFSET_S = 0.01f;           // experimentally calculated amount to cut off schroeder tail
		public const float GV_DISTANCE_GAIN_THRESHOLD = 0.891251f;  // -1dB converted to linear gain
		public const int GV_DELAY_CLOSE_THRESHOLD = 5;          // "close enough" delay threshold when analyzing for direction
		public const float GV_IMPULSE_RESPONSE_S = GV_SQRT_2 * (12.50f) / GV_C + (0.25f);         // number of seconds to collect per impulse response
	}
}

