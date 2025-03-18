using System;
using UnityEngine;

public class CameraSwayer : MonoBehaviour
{
    private CubeContainerMaintainer ccm;
    private int fixedTimeSteps;

    void Start()
    {
        ccm = FindAnyObjectByType<CubeContainerMaintainer>();
        fixedTimeSteps = 0;
    }
    private void FixedUpdate()
    {
        // OK now like, rotate around it on the XY plane.
        // multiply sin(time) by dim to scale (linearly) out correctly. add with ccm.dim to adjust for offset (square starts to be drawn at origin out dim, dim in x,y)
        var gridSize = ccm.dim;
        var viewAngle = 45 / 1.75f;
        var distanceAwayFromGrid = ccm.dim / Mathf.Tan(Mathf.Deg2Rad * viewAngle);
        var midPosition = new Vector3(-gridSize/2f + 0.5f, -gridSize/2f +.5f, distanceAwayFromGrid - fixedTimeSteps++);
        transform.localPosition = new Vector3(Mathf.Sin(Time.time) * ccm.dim + ccm.dim, Mathf.Cos(Time.time) * ccm.dim + ccm.dim, -ccm.dim * 4);
        transform.LookAt(midPosition + new Vector3(ccm.dim, ccm.dim, 0));
    }
}
