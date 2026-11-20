using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour, RacingDrivers
{
    private CarController carController;
    private WaypointPath waypointPath;
    private PlayerInput player;
    private InputAction movement;
    private RaceManager raceManager;
    private Collider driverCollider;

    private Vector3 currentWaypoint;
    private int lastWaypointIndex;
    private int currentWaypointIndex;
    private int nextWaypointIndex;

    private float steerDirection;
    private float throttleDirection;

    private int lapNumber;
    private bool finalLap = false;
    private bool raceFinished = false;

    //test for lerp
    private bool isResetting = false;

    #region Interface Variables
    public Transform DriverTransform => transform;
    public int CurrentWaypoint => currentWaypointIndex;

    public int LapNumber => lapNumber;

    public float DistanceToWaypoint =>
        waypointPath != null && waypointPath.LastWaypoint+1 > 0
        ? Vector3.Distance(transform.position, waypointPath.GetCurrentWaypoint(currentWaypointIndex).position)
        : 0f;
    public float RaceProgress
    {
        get
        {
            return waypointPath != null && waypointPath.LastWaypoint+1 > 0
                ? (float)currentWaypointIndex / waypointPath.LastWaypoint+1
                : 0f;
        }
    }
    public void ReceiveHotspotInformation()
    {
        // Store or process collision hotspot information here.
    }
    public void ResetDriver()
    {
        //driverCollider.enabled = false;
        //transform.position = Vector3.Slerp(transform.position, waypointPath.GetCurrentWaypoint(lastWaypointIndex).position, 5f*Time.deltaTime);
        isResetting = true;
        //driverCollider.enabled = true;
    }
    #endregion
    #region collision recovery variables
    private int collisionsCount = 0;
    private bool recoveringFromCollision = false;

    public float recoveryReverseTime = 1.0f;
    public float recoveryTurnTime = 0.75f;
    #endregion


    public int CurrentWaypointIndex {  get { return currentWaypointIndex; } }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        carController = GetComponent<CarController>();
        waypointPath = FindAnyObjectByType<WaypointPath>();
        currentWaypointIndex = waypointPath.CurrentIndex;
        nextWaypointIndex = waypointPath.NextIndex;
        lastWaypointIndex = currentWaypointIndex;
        player = GetComponent<PlayerInput>();
        movement = player.actions["Move"];
        raceManager = FindAnyObjectByType<RaceManager>();
        driverCollider = GetComponent<Collider>();

        lapNumber = 0;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 inputVector = movement.ReadValue<Vector2>();

        //This if statements dictates if the player or the ai is driving.
        //it is supposed to be like when you finish a race in MK and the character continues to drive
        if (isResetting)
        {
            driverCollider.enabled = false;
            transform.position = Vector3.Lerp(transform.position, waypointPath.GetCurrentWaypoint(lastWaypointIndex).position, 1.5f * Time.deltaTime);
            
            if (Vector3.Distance(transform.position, waypointPath.GetCurrentWaypoint(lastWaypointIndex).position) < 5f)
            {
                isResetting = false;
                driverCollider.enabled = true;
            }
        }
        else if (!raceFinished)
        {
            steerDirection = inputVector.x;
            throttleDirection = inputVector.y;

            carController.Drive(steerDirection, throttleDirection);
        }
        else
        {
            float forwardAmount = 0f;
            float turnAmount = 0f;

            float reachedWaypointDistanceAI = 10f;
            float distanceToWaypointAI = Vector3.Distance(transform.position, currentWaypoint);

            if (distanceToWaypointAI >= reachedWaypointDistanceAI)
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
                    if (distanceToWaypointAI > reverseDistance)
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
            }
            carController.Drive(turnAmount, forwardAmount);
        }

        if (waypointPath.GetCurrentWaypoint(currentWaypointIndex) != null)
        {
            SetWaypoint(waypointPath.GetCurrentWaypoint(currentWaypointIndex).position);
        }

        float reachedWaypointDistance = 10f;
        float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint);

        if (distanceToWaypoint <= reachedWaypointDistance)
        {
            
            if (!finalLap && currentWaypointIndex == waypointPath.LastWaypoint)
            {
                lapNumber++;
                if(lapNumber == raceManager.RaceLaps)
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
        if (raceFinished)
        {
            StartCoroutine(RecoverFromCollision());
        }

        collisionsCount++;
    }
   
}
