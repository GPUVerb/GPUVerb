using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoateEmitter : MonoBehaviour
{
    public bool startRotate = false;
    public float angularSpeed = 20;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (startRotate)
        {
            transform.Rotate(0, angularSpeed * Time.deltaTime, 0);
        }
    }
}
