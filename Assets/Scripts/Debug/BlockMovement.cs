using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockMovement : MonoBehaviour
{
    public bool startMovement = false;
    public float speed = 5;
    public float MovementRange = 3.0f;
    public Vector3 direction = Vector3.zero;

    private Vector3 startPosition;
    private float positive = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        startPosition = gameObject.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if(startMovement)
        {
            transform.position += direction * (speed * Time.deltaTime) * positive;
            if ((transform.position - startPosition).magnitude >= MovementRange)
            {
                positive = positive > 0 ? -1.0f : 1.0f;
            }
        }
    }
}
