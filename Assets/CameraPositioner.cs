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

    // Update is called once per frame
    void Update()
    {
        var gridSize = cc.dim;
        var viewAngle = GetComponent<Camera>().fieldOfView;
        var distanceAwayFromGrid = cc.dim / Mathf.Tan(Mathf.Deg2Rad * viewAngle);
        this.transform.position = new Vector3(-gridSize/2, -gridSize/2, distanceAwayFromGrid);
    }
}
