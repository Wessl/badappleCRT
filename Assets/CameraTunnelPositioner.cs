using UnityEngine;

public class CameraTunnelPositioner : MonoBehaviour
{
    private CubeContainerMaintainer ccm;
    public float divideBy = 1.75f;
    private int fixedTimeSteps;
    void Start()
    {
        ccm = FindAnyObjectByType<CubeContainerMaintainer>();
        fixedTimeSteps = 0;
    }

    void FixedUpdate()
    {
        // uses... MATH to find the correct POSITION? NO WAY!
        var gridSize = ccm.dim;
        var viewAngle = GetComponent<Camera>().fieldOfView / divideBy;
        var distanceAwayFromGrid = ccm.dim / Mathf.Tan(Mathf.Deg2Rad * viewAngle);
        var midPosition = new Vector3(-gridSize/2f + 0.5f, -gridSize/2f +.5f, distanceAwayFromGrid - fixedTimeSteps++);
        
        // OK now like, rotate around it on the XY plane
        transform.position = midPosition + new Vector3(Mathf.Sin(Time.time) * 30 + ccm.dim, Mathf.Cos(Time.time) * 30 + ccm.dim, -60);
        transform.LookAt(midPosition + new Vector3(ccm.dim, ccm.dim, 0));
    }
}
