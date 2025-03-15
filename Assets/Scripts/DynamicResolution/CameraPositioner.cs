using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPositioner : MonoBehaviour
{
    private CubeContainerMaintainer cc;
    public float divideBy = 1.75f;
    private void Start()
    {
        cc = FindAnyObjectByType<CubeContainerMaintainer>();
    }

    void FixedUpdate()
    {
        // uses... MATH to find the correct POSITION? NO WAY!
        var gridSize = cc.dim;
        var viewAngle = GetComponent<Camera>().fieldOfView / divideBy;
        var distanceAwayFromGrid = cc.dim / Mathf.Tan(Mathf.Deg2Rad * viewAngle);
        this.transform.position = new Vector3(-gridSize/2f + 0.5f, -gridSize/2f +.5f, distanceAwayFromGrid);
    }
}
