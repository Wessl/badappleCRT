using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPositioner : MonoBehaviour
{
    

    // Update is called once per frame
    void Update()
    {
        var gridSize = CubeContainer.dim;
        var viewAngle = GetComponent<Camera>().fieldOfView;
        var distanceAwayFromGrid = CubeContainer.dim / Mathf.Tan(Mathf.Deg2Rad * viewAngle);
        this.transform.position = new Vector3(-gridSize/2, -gridSize/2, distanceAwayFromGrid);
    }
}
