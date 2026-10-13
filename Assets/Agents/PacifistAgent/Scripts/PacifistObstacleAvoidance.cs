using UnityEngine;

public class PacifistObstacleAvoidance : MonoBehaviour
{
    [Header("Sensor Settings")]
    public Transform sensorOrigin;
    public LayerMask obstacleMask, driverMask;



    private float forwardDistance = 20f;
    private float sideDistance = 14f;
    private float sensorRadius = 1.5f;
    private float sideAngle = 45f;

    private float vehicleDetectionDistance = 25f;
    private float rearDetectionDistance = 14f;
    private float rearSideDistance = 14f;
    private float rearSideAngle = 45f;


    public bool ObstacleAhead { get; private set; }
    public bool LeftBlocked { get; private set; }
    public bool RightBlocked { get; private set; }
    public bool CarAhead { get; private set; }
    public float DistanceToCar { get; private set; }
    public bool LeftBlockedCar { get; private set; }
    public bool RightBlockedCar { get; private set; }
    public bool CarBehind { get; private set; }
    public float DistanceToCarBehind { get; private set; }
    public bool RearLeftBlocked { get; private set; }
    public bool RearRightBlocked { get; private set; }

    public Vector3 AvoidanceDirection { get; private set; }

    // True when an obstacle is ahead and neither side is clear.
    public bool MustBrake => ObstacleAhead && LeftBlocked && RightBlocked;

    void FixedUpdate()
    {
        CheckForObstacles();
        CheckForCars();
        CheckForCarsBehind();
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

        ObstacleAhead = Physics.SphereCast(origin, sensorRadius, forward, out _, forwardDistance, ~0, QueryTriggerInteraction.Ignore);

        LeftBlocked = Physics.SphereCast(origin, sensorRadius, left, out _, sideDistance, ~0, QueryTriggerInteraction.Ignore);

        RightBlocked = Physics.SphereCast( origin, sensorRadius, right, out _, sideDistance, ~0, QueryTriggerInteraction.Ignore);

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
    void CheckForCars()
    {
        Vector3 origin = sensorOrigin.position;
        Vector3 forward = transform.forward;

        Vector3 left = Quaternion.AngleAxis(-sideAngle, transform.up) * forward;

        Vector3 right = Quaternion.AngleAxis(sideAngle, transform.up) * forward;

        LeftBlockedCar = Physics.SphereCast(origin, sensorRadius, left, out _, sideDistance, driverMask, QueryTriggerInteraction.Ignore);

        RightBlockedCar = Physics.SphereCast(origin, sensorRadius, right, out _, sideDistance, driverMask, QueryTriggerInteraction.Ignore);

        Debug.DrawRay(origin, left * sideDistance, LeftBlockedCar ? Color.yellow : Color.green);

        Debug.DrawRay(origin, right * sideDistance, RightBlockedCar ? Color.yellow : Color.green);

        if (Physics.SphereCast(origin, sensorRadius, forward, out RaycastHit hit, vehicleDetectionDistance, driverMask, QueryTriggerInteraction.Ignore) ||
            LeftBlockedCar || RightBlockedCar)
        {
            CarAhead = true;
            DistanceToCar = hit.distance;

            Debug.DrawRay( origin, forward * hit.distance, Color.yellow);
        }
        else
        {
            CarAhead = false;
            DistanceToCar = vehicleDetectionDistance;

            Debug.DrawRay(origin, forward * vehicleDetectionDistance, Color.green);
        }
    }
    void CheckForCarsBehind()
    {
        Vector3 origin = sensorOrigin.position;
        Vector3 backward = -transform.forward;

        Vector3 rearLeft = Quaternion.AngleAxis(-rearSideAngle, transform.up) * backward;

        Vector3 rearRight = Quaternion.AngleAxis(rearSideAngle, transform.up) * backward;

        RearLeftBlocked = Physics.SphereCast(origin, sensorRadius, rearLeft, out RaycastHit leftHit, rearSideDistance, driverMask, QueryTriggerInteraction.Ignore);

        RearRightBlocked = Physics.SphereCast(origin, sensorRadius, rearRight, out RaycastHit rightHit, rearSideDistance, driverMask, QueryTriggerInteraction.Ignore);

        Debug.DrawRay(origin, rearLeft * rearSideDistance, RearLeftBlocked ? Color.magenta : Color.green);

        Debug.DrawRay(origin, rearRight * rearSideDistance, RearRightBlocked ? Color.magenta : Color.green);

        if (Physics.SphereCast( origin, sensorRadius, backward, out RaycastHit hit, rearDetectionDistance, driverMask, QueryTriggerInteraction.Ignore))
        {
            CarBehind = true;
            DistanceToCarBehind = hit.distance;

            Debug.DrawRay(origin, backward * hit.distance, Color.magenta);
        }
        else
        {
            CarBehind = false;
            DistanceToCarBehind = rearDetectionDistance;

            Debug.DrawRay(origin, backward * rearDetectionDistance, Color.green);
        }
    }
    
}
