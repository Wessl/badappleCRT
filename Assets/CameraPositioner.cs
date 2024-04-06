using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPositioner : MonoBehaviour
{
    private CubeContainer cc;
    private void Start()
    {
        cc = FindObjectOfType<CubeContainer>();
    }

    void FixedUpdate()
    {
        // uses... MATH to find the correct POSITION? NO WAY!
        var gridSize = cc.dim;
        var viewAngle = GetComponent<Camera>().fieldOfView;
        var distanceAwayFromGrid = cc.dim / Mathf.Tan(Mathf.Deg2Rad * viewAngle);
        this.transform.position = new Vector3(-gridSize/2f, -gridSize/2f, distanceAwayFromGrid);
    }
}
