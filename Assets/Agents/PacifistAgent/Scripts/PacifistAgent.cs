using UnityEngine;

public class PacifistAgent : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //private Transform targetWaypointTransform;
    
    private CarController carController;
    private WaypointPath waypointPath;

    private Vector3 currentWaypoint;

    void Start()
    {
        carController = GetComponent<CarController>();
        waypointPath = FindAnyObjectByType<WaypointPath>();

    }

    // Update is called once per frame
    void Update()
    {
        if (waypointPath.GetCurrentWaypoint() != null)
        {
            SetWaypoint(waypointPath.GetCurrentWaypoint().position);
        }
        float forwardAmount = 0f;
        float turnAmount = 0f;

        float reachedWaypointDistance = 10f;
        float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint);
        if (distanceToWaypoint >= reachedWaypointDistance)
        {
            Vector3 dirToMovePosition = (currentWaypoint - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, dirToMovePosition);
            if (dot > 0)
            {
                forwardAmount = 1f;
            }
            else
            {
                forwardAmount = -1f;
                float reverseDistance = 25f;
                if(distanceToWaypoint > reverseDistance)
                {
                    forwardAmount = 1f;
                }
                else
                {
                    forwardAmount = -1f;
                }
            }

            float angleToDir = Vector3.SignedAngle(transform.forward, dirToMovePosition, Vector3.up);
            if (angleToDir > 0)
            {
                turnAmount = 1f;
            }
            else
            {
                turnAmount = -1f;
            }
        }
        else
        {
            waypointPath.WaypointReached();
            //forwardAmount = 0f;
            //turnAmount = 0f;
        }

            carController.Drive(turnAmount, forwardAmount);
    }

    public void SetWaypoint(Vector3 waypoint)
    {
        currentWaypoint = waypoint;
    }
}
