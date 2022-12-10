using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

[RequireComponent(typeof(AudioSource))]
public class ReverbWriter : MonoBehaviour
{
    [DllImport("GPUVerbDSPPlugin")]
    private static extern float getReverbBuf(ref IntPtr buf, int bufidx);

    [DllImport("GPUVerbDSPPlugin")]
    private static extern bool zeroReverb(int bufidx);

    private static int MAX_FRAME_LENGTH = 4096; // arbitrary
    float[] outputBuffer = new float[MAX_FRAME_LENGTH];

    public int index;

    // Start is called before the first frame update
    void Start()
    {
        // index should be 0 to 2
    }

    // Update is called once per frame
    void Update()
    {}

    private void OnAudioFilterRead(float[] data, int channels)
    {
        IntPtr result = IntPtr.Zero;
        float size = getReverbBuf(ref result, index);

        Marshal.Copy(result, outputBuffer, 0, MAX_FRAME_LENGTH);

        // choose the right length in case data buffer too big
        int dataBufferLength = (data.Length > outputBuffer.Length) ? outputBuffer.Length : data.Length;

        // memcpy the data over
        Array.Copy(outputBuffer, data, dataBufferLength);

        zeroReverb(index);
    }
}