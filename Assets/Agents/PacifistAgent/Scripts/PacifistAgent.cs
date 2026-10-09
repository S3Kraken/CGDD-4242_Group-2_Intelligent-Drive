using UnityEngine;

public class PacifistAgent : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //private Transform targetWaypointTransform;
    
    private CarController carController;
    private WaypointPath waypointPath;

    private Vector3 currentWaypoint;
    private int currentWaypointIndex;
    private int nextWaypointIndex;

    private PacifistObstacleAvoidance pAvoidance;
    private float avoidanceTimer = 0f;
    private float avoidanceDuration = 0.5f;
    private float avoidanceTurn = 0f;
    private float yieldTimer = 0f;
    private float yieldDuration = 0.5f;
    private float yieldTurn = 0f;

    void Start()
    {
        carController = GetComponent<CarController>();
        waypointPath = FindAnyObjectByType<WaypointPath>();
        pAvoidance = GetComponent<PacifistObstacleAvoidance>();
        currentWaypointIndex = waypointPath.CurrentIndex;
        nextWaypointIndex = waypointPath.NextIndex;

}

    // Update is called once per frame
    void Update()
    {
        if (waypointPath.GetCurrentWaypoint(currentWaypointIndex) != null)
        {
            SetWaypoint(waypointPath.GetCurrentWaypoint(currentWaypointIndex).position);
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
                if (distanceToWaypoint > reverseDistance)
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
            //This is the PACIFIST behaviour. If copying the waypoint code, remove this region.
            #region pacifist behaviour

            bool carIsClose = pAvoidance.DistanceToCar < 10f;
            bool carIsCloseBehind = pAvoidance.DistanceToCarBehind < 10f;

            if (pAvoidance != null && pAvoidance.ObstacleAhead)
            {
                avoidanceTimer = avoidanceDuration;

                if (!pAvoidance.LeftBlocked)
                {
                    avoidanceTurn = -1f;
                }
                else if (!pAvoidance.RightBlocked)
                {
                    avoidanceTurn = 1f;
                }
                else
                {
                    avoidanceTurn = 0f;
                }
            }
            else
            {
                avoidanceTimer -= Time.deltaTime;
            }

            if (avoidanceTimer > 0f)
            {
                if (avoidanceTurn != 0f)
                {
                    turnAmount = avoidanceTurn;
                }
                else
                {
                    forwardAmount = -1f;
                }
            }
            if (pAvoidance != null && pAvoidance.CarAhead)
            {
                yieldTimer = yieldDuration;

                if (!pAvoidance.LeftBlocked)
                {
                    avoidanceTurn = -1f;
                }
                else if (!pAvoidance.RightBlocked)
                {
                    avoidanceTurn = 1f;
                }
                else
                { 
                    avoidanceTurn = 0f;
                }
                if (carIsClose)
                {
                    forwardAmount = -1;
                }
            }
            else
            {
                yieldTimer -= Time.deltaTime;
            }

            if (yieldTimer > 0f)
            {
                if (avoidanceTurn != 0f)
                {
                    turnAmount = avoidanceTurn;
                }
                else
                {
                    forwardAmount = -1f;
                }
            }
            if (pAvoidance != null && pAvoidance.CarBehind)
            {
                yieldTimer = yieldDuration;

                if (!pAvoidance.RearLeftBlocked)
                {
                    avoidanceTurn = -1f;
                }
                else if (!pAvoidance.RearRightBlocked)
                {
                    avoidanceTurn = 1f;
                }
                else
                {
                    avoidanceTurn = 0f;
                }
                if (carIsClose)
                {
                    forwardAmount = -1;
                }
            }
            else
            {
                yieldTimer -= Time.deltaTime;
            }

            if (yieldTimer > 0f)
            {
                if (avoidanceTurn != 0f)
                {
                    turnAmount = avoidanceTurn;
                }
                else
                {
                    forwardAmount = -1f;
                }
            }

            #endregion
        }
        else
        {
            waypointPath.WaypointReached(ref currentWaypointIndex, ref nextWaypointIndex);
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
