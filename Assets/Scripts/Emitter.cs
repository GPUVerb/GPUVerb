using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GPUVerb
{
	public class Emitter : MonoBehaviour
	{
		#region DLL Interface
		private const string DLLNAME = "ProjectPlaneverbUnityPlugin";

		[DllImport(DLLNAME)]
		private static extern int PlaneverbEmit(float x, float y, float z);

		[DllImport(DLLNAME)]
		private static extern void PlaneverbUpdateEmission(int id, float x, float y, float z);

		[DllImport(DLLNAME)]
		private static extern void PlaneverbEndEmission(int id);

		[DllImport(DLLNAME)]
		private static extern AnalyzerResult PlaneverbGetOutput(int emissionID);

		[DllImport(DLLNAME)]
		private static extern int PlaneverbAddGeometry(float posX, float posY,
		float width, float height,
		float absorption);

		[DllImport(DLLNAME)]
		private static extern void PlaneverbUpdateGeometry(int id,
		float posX, float posY,
		float width, float height,
		float absorption);

		[DllImport(DLLNAME)]
		private static extern void PlaneverbRemoveGeometry(int id);

		[DllImport(DLLNAME)]
		private static extern void PlaneverbSetListenerPosition(float x, float y, float z);
		#endregion

		// Start is called before the first frame update
		void Start()
		{

		}

		// Update is called once per frame
		void Update()
		{

		}
	}
}

