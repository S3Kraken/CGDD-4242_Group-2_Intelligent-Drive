using System.Collections;
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

    //this is only for the pacifist agent
    #region Pacifist variables
    private PacifistObstacleAvoidance pAvoidance;
    private float avoidanceTimer = 0f;
    private float avoidanceDuration = 0.5f;
    private float avoidanceTurn = 0f;
    private float yieldTimer = 0f;
    private float yieldDuration = 0.5f;
    #endregion

    //this is if you want the collision recovery
    #region collision recovery variables
    private int collisionsCount = 0;
    private bool recoveringFromCollision = false;

    public float recoveryReverseTime = 1.0f;
    public float recoveryTurnTime = 0.75f;
    #endregion

    void Start()
    {
        carController = GetComponent<CarController>();
        waypointPath = FindAnyObjectByType<WaypointPath>();
        currentWaypointIndex = waypointPath.CurrentIndex;
        nextWaypointIndex = waypointPath.NextIndex;

        pAvoidance = GetComponent<PacifistObstacleAvoidance>();
    }

    // Update is called once per frame
    void Update()
    {
        if (recoveringFromCollision)
        {
            return;
        }


        //Basic Driver Waypoint following
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

                if (!pAvoidance.LeftBlockedCar)
                {
                    avoidanceTurn = -1f;
                }
                else if (!pAvoidance.RightBlockedCar)
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


    //This is completely optional, and it is if you want to have a recovery from collisions.
    private IEnumerator RecoverFromCollision()
    {
        recoveringFromCollision = true;

        // Step 1: Stop moving forward
        carController.Drive(0f, 0f);

        yield return new WaitForSeconds(0.3f);

        // Step 2: Reverse away
        carController.Drive(0f, -1f);

        yield return new WaitForSeconds(recoveryReverseTime);

        // Step 3: Steer away
        carController.Drive(1f, 0f);

        yield return new WaitForSeconds(recoveryTurnTime);

        // Step 4: Return control to agent
        carController.Drive(0f, 0f);

        recoveringFromCollision = false;
    }
    public void OnCollisionEnter(Collision collision)
    {
        if (recoveringFromCollision)
        {
            return;
        }

        if (collision.gameObject.name == "Ground" || collision.gameObject.name == "Road")
            return;

        StartCoroutine(RecoverFromCollision());
        
        collisionsCount++;
    }
}
