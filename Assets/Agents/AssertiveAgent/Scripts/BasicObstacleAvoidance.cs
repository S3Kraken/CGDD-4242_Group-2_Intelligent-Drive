using UnityEngine;

public class BasicObstacleAvoidance : MonoBehaviour
{
    [Header("Sensor Settings")]
    Transform sensorOrigin;
    public LayerMask obstacleMask, driverMask;



    private float forwardDistance = 20f;
    private float sideDistance = 14f;
    private float sensorRadius = 1.5f;
    private float sideAngle = 45f;


    public bool ObstacleAhead { get; private set; }
    public bool LeftBlocked { get; private set; }
    public bool RightBlocked { get; private set; }

    public Vector3 AvoidanceDirection { get; private set; }

    // True when an obstacle is ahead and neither side is clear.
    public bool MustBrake => ObstacleAhead && LeftBlocked && RightBlocked;

    private void Awake()
    {
        sensorOrigin = transform.GetChild(0) as Transform;
    }

    void FixedUpdate()
    {
        CheckForObstacles();
    }

    void CheckForObstacles()
    {
        if (sensorOrigin == null)
        {
            return;

        }

        Vector3 origin = sensorOrigin.position;
        Vector3 forward = transform.forward;

        Vector3 left = Quaternion.AngleAxis(-sideAngle, transform.up) * forward;

        Vector3 right = Quaternion.AngleAxis(sideAngle, transform.up) * forward;

        ObstacleAhead = Physics.SphereCast(origin, sensorRadius, forward, out _, forwardDistance, obstacleMask, QueryTriggerInteraction.Ignore);

        LeftBlocked = Physics.SphereCast(origin, sensorRadius, left, out _, sideDistance, obstacleMask, QueryTriggerInteraction.Ignore);

        RightBlocked = Physics.SphereCast( origin, sensorRadius, right, out _, sideDistance, obstacleMask, QueryTriggerInteraction.Ignore);

        AvoidanceDirection = Vector3.zero;

        Debug.DrawRay(origin, forward * forwardDistance, ObstacleAhead ? Color.red : Color.green);

        Debug.DrawRay(origin, left * sideDistance, LeftBlocked ? Color.red : Color.green);

        Debug.DrawRay(origin, right * sideDistance, RightBlocked ? Color.red : Color.green);

        if (!ObstacleAhead)
        {
            return;
        }

        // Prefer a clear side. If both are clear,
        // choose the side with more room to check.
        if (!LeftBlocked)
        {
            AvoidanceDirection = left;
        }
        else if (!RightBlocked)
        {
            AvoidanceDirection = right;
        }
    }
}
