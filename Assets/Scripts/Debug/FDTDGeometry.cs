using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GPUVerb
{
    [RequireComponent(typeof(Collider))]
    public class FDTDGeometry : MonoBehaviour
    {

        // Start is called before the first frame update
        void Start()
        {
            Collider collider = GetComponent<Collider>();
            GPUVerbContext.Instance.FDTDSolver.AddGeometry(collider.bounds);
        }
    }

}
