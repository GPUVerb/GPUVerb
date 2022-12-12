using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoateEmitter : MonoBehaviour
{
    public bool startRotate = false;
    public float angularSpeed = 20;

    private bool singing = true;
    private float initialVolume = 0.0f;
    // Start is called before the first frame update
    void Start()
    {
        initialVolume = this.gameObject.GetComponent<AudioSource>().volume;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown("2"))
        {
            startRotate = !startRotate;
        }
        if (Input.GetKeyDown("3"))
        {
            if(singing)
            {
                this.gameObject.GetComponent<AudioSource>().volume = 0.0f;
                singing = false;
            }
            else
            {
                this.gameObject.GetComponent<AudioSource>().volume = initialVolume;
                singing = true;
            }
        }
        if (startRotate)
        {
            transform.Rotate(0, angularSpeed * Time.deltaTime, 0);
        }
    }
}
