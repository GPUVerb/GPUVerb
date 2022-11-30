using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockMovement : MonoBehaviour
{
    public float speed = 5;
    public bool startMovement = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(startMovement)
        {
            this.transform.position += new Vector3(Time.deltaTime * speed, 0.0f, 0.0f);
        }
    }
}
