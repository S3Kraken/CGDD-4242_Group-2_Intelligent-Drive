using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private CarController carController;
    private WaypointPath waypointPath;
    private PlayerInput player;
    private InputAction movement;
    private InputActionAsset inputActions;
    private RaceManager raceManager;

    private Vector3 currentWaypoint;
    private int currentWaypointIndex;
    private int nextWaypointIndex;

    private float steerDirection;
    private float throttleDirection;

    private int lapsLeft;
    private bool finalLap = false;
    private bool racedFinished = false;

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
        player = GetComponent<PlayerInput>();
        movement = player.actions["Move"];
        raceManager = FindAnyObjectByType<RaceManager>();

        lapsLeft = raceManager.RaceLaps;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 inputVector = movement.ReadValue<Vector2>();

        if (!racedFinished)
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
                if (angleToDir > 0)
                {
                    turnAmount = 1f;
                }
                else
                {
                    turnAmount = -1f;
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
                lapsLeft--;
                if(lapsLeft == 0)
                {
                    finalLap = true;
                }
            }

            if (finalLap && currentWaypointIndex == waypointPath.LastWaypoint)
            {
                //win or loss state
                racedFinished = true;
            }

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
        if (racedFinished)
        {
            StartCoroutine(RecoverFromCollision());
        }

        collisionsCount++;
    }
}
