using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grapher : MonoBehaviour
{
    float xVal = 0;
    float xValOffset = 0;
    float[] spectrum = new float[256];

    public float ampMultiplier; // = 200
    public float xValIncrement = 0.0001f;

    Queue<Vector3> verts = new Queue<Vector3>();

    void Update()
    {
        float val = 0;
        for (int channel = 0; channel < 2; ++channel) {
            AudioListener.GetSpectrumData(spectrum, channel, FFTWindow.BlackmanHarris);
            for (int i = 0; i < spectrum.Length; ++i)
            {
                val += spectrum[i];
            }
        }
        val /= (2 * spectrum.Length);

        xVal += xValIncrement;
        verts.Enqueue(new Vector3(xVal, Mathf.Min(1f, ampMultiplier * val), 0));
        if (xVal >= 0.95) { // close to end of screen, we should start scrolling the list
            verts.Dequeue();
            xValOffset -= xValIncrement;
        }
    }

    void OnPostRender()
    {
        GL.PushMatrix();
        GL.LoadOrtho();


        GL.Begin(GL.LINES);
        GL.Color(Color.red);
        Vector3 prevVertex = Vector3.zero;
        foreach (var vert in verts) {
            GL.Vertex(new Vector3(prevVertex.x + xValOffset, prevVertex.y, prevVertex.z));
            GL.Vertex(new Vector3(vert.x + xValOffset, vert.y, vert.z));
            prevVertex = vert;
        }
        GL.End();
        GL.PopMatrix();
    }
}
