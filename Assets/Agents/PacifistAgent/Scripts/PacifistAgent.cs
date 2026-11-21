using System.Collections;
using UnityEngine;

public class PacifistAgent : MonoBehaviour, RacingDrivers
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //private Transform targetWaypointTransform;
    
    private CarController carController;
    private WaypointPath waypointPath;
    private RaceManager raceManager;
    private Collider driverCollider;
    private Rigidbody rb;

    private Vector3 currentWaypoint;
    private int lastWaypointIndex;
    private int currentWaypointIndex;
    private int nextWaypointIndex;

    private int lapNumber;
    private bool finalLap;
    private bool raceFinished;

    private bool isResetting = false;


    #region Interface Variables
    public Transform DriverTransform => transform;
    public int CurrentWaypoint => currentWaypointIndex;

    public int LapNumber => lapNumber;

    public float DistanceToWaypoint =>
        waypointPath != null && waypointPath.LastWaypoint + 1 > 0
        ? Vector3.Distance(transform.position, waypointPath.GetCurrentWaypoint(currentWaypointIndex).position)
        : 0f;
    public float RaceProgress
    {
        get
        {
            return waypointPath != null && waypointPath.LastWaypoint + 1 > 0
                ? (float)currentWaypointIndex / waypointPath.LastWaypoint + 1
                : 0f;
        }
    }
    public void ReceiveHotspotInformation()
    {
        // Store or process collision hotspot information here.
    }
    public void ResetDriver()
    {
        isResetting = true;
        //driverCollider.enabled = false;
        //transform.position = waypointPath.GetCurrentWaypoint(lastWaypointIndex).position;
        //driverCollider.enabled = true;
    }
    #endregion

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

    private int lapNumber = 1;

    public int CurrentWaypoint => currentWaypointIndex;

    public int LapNumber => lapNumber;

    public float DistanceToWaypoint =>
        Vector3.Distance(transform.position, currentWaypoint);

    public Transform DriverTransform => transform;

    public float RaceProgress
    {
        get
        {
            if (waypointPath == null || waypointPath.WaypointCount == 0)
                return 0f;

            return (float)currentWaypointIndex /
                   waypointPath.WaypointCount;
        }
    }


    void Start()
    {
        carController = GetComponent<CarController>();
        waypointPath = FindAnyObjectByType<WaypointPath>();
        currentWaypointIndex = waypointPath.CurrentIndex;
        nextWaypointIndex = waypointPath.NextIndex;
        driverCollider = GetComponent<Collider>();
        raceManager = FindFirstObjectByType<RaceManager>();
        rb = GetComponent<Rigidbody>();

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
        if (isResetting)
        {
            rb.isKinematic = true;
            driverCollider.enabled = false;
            transform.position = Vector3.Lerp(transform.position, waypointPath.GetCurrentWaypoint(lastWaypointIndex).position, 1.5f * Time.deltaTime);

            if (Vector3.Distance(transform.position, waypointPath.GetCurrentWaypoint(lastWaypointIndex).position) < 5f)
            {
                isResetting = false;
                driverCollider.enabled = true;
                rb.isKinematic = false;
            }

        }
        else
        {


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
                turnAmount = Mathf.Clamp(angleToDir / 45f, -1f, 1f);

                if (Mathf.Abs(angleToDir) < 2f)
                {
                    turnAmount = 0f;
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
                    forwardAmount = 0.5f;
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
                        if (forwardAmount > 0)
                        {
                            forwardAmount = -0.5f;

                        }
                        else
                        {
                            forwardAmount = 0f;

                        }
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
                        forwardAmount = -0.5f;
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
                        if (forwardAmount <= 0)
                        {
                            forwardAmount = 0.5f;

                        }
                        else
                        {
                            forwardAmount = 0f;

                        }
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
                        forwardAmount = -0.5f;
                    }
                }

                #endregion
            }
            else
            {
                if (!finalLap && currentWaypointIndex == waypointPath.LastWaypoint)
                {
                    lapNumber++;
                    if (lapNumber >= raceManager.RaceLaps)
                    {
                        finalLap = true;
                    }
                }

                if (finalLap && currentWaypointIndex == waypointPath.LastWaypoint)
                {
                    //win or loss state
                    raceFinished = true;
                }
                lastWaypointIndex = currentWaypointIndex;
                waypointPath.WaypointReached(ref currentWaypointIndex, ref nextWaypointIndex);
                //forwardAmount = 0f;
                //turnAmount = 0f;
            }

            
        
        else
        {
            int previousWaypoint = currentWaypointIndex;

            waypointPath.WaypointReached(ref currentWaypointIndex, ref nextWaypointIndex
            );

            // Check if the driver completed a lap.
            if (currentWaypointIndex < previousWaypoint)
            {
                lapNumber++;

                Debug.Log(gameObject.name +
                          " completed lap " + lapNumber);
            }
        }
    

            carController.Drive(turnAmount, forwardAmount);
        }
            
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

    public void ReceiveHotspotInformation()
    {
      
    }
}
