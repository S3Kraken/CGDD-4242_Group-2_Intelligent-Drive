using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class AssertiveScript : MonoBehaviour, RacingDrivers
{
    private CarController carController;
    private WaypointPath waypointPath;

    [SerializeField] private Vector3 currentWaypoint;
    private int currentWaypointIndex;
    private int nextWaypointIndex;
    private int lastWaypointIndex;

    //this is if you want the collision recovery
    #region collision recovery variables
    private bool recoveringFromCollision = false;

    public float recoveryReverseTime = 1.0f;
    public float recoveryTurnTime = 0.75f;
    #endregion

    #region Avoidance variables
    private BasicObstacleAvoidance avoidanceScript;
    private float avoidanceTimer = 0f;
    private float avoidanceDuration = 0.5f;
    private float avoidanceTurn = 0f;
    private float yieldTimer = 0f;
    #endregion

    float setAccel;
    float setGrip;
    float setTurnDegrees;

    //Targeting vars
    [SerializeField] List<GameObject> Targets = new List<GameObject>();
    [SerializeField] List<GameObject> inRangeDrivers = new List<GameObject>();
    [SerializeField] GameObject lockedTarget;
    private Rigidbody rb;
    private Collider driverCollider;
    private bool isResetting = false;
    bool targetLocked = false;

    //Truce vars
    [SerializeField] int aggressiveAgentCollisions = 0;
    float truceCollisonTimeLimit = 20f;
    [SerializeField] bool truceMade;


    // RacingDrivers Interface Implementation Start

    private int lapNumber = 1;

    public int CurrentWaypoint => currentWaypointIndex;
    public int LapNumber => lapNumber;
    public Transform DriverTransform => transform;

    public float DistanceToWaypoint
    {
        get
        {
            if (waypointPath == null)
                return 0f;

            Transform waypoint = waypointPath.GetCurrentWaypoint(currentWaypointIndex);

            if (waypoint == null)
                return 0f;

            return Vector3.Distance(transform.position, waypoint.position);
        }
    }

    public float RaceProgress => waypointPath == null || waypointPath.WaypointCount == 0
        ? 0f
        : (float)currentWaypointIndex / waypointPath.WaypointCount;
    


    public void ResetDriver()
    {
        isResetting = true;
        StopAllCoroutines();
        recoveringFromCollision = false;

        int count = waypointPath.WaypointCount;
        int previousIndex = (currentWaypointIndex - 1 + count) % count;
        Vector3 respawnPosition = waypointPath.GetCurrentWaypoint(previousIndex).position;
        Vector3 lookDirection = waypointPath.GetCurrentWaypoint(currentWaypointIndex).position - respawnPosition;
        lookDirection.y = 0f;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = respawnPosition;
        if (lookDirection != Vector3.zero)
        {
            rb.rotation = Quaternion.LookRotation(lookDirection);
        }

        carController.Drive(0f, 0f);
    }

    // RacingDrivers Interface Implementation End

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        carController = GetComponent<CarController>();
        waypointPath = FindAnyObjectByType<WaypointPath>();
        currentWaypointIndex = waypointPath.CurrentIndex;
        nextWaypointIndex = waypointPath.NextIndex;

        avoidanceScript = GetComponent<BasicObstacleAvoidance>();

        setAccel = carController.acceleration;
        setGrip = carController.grip;
        setTurnDegrees = carController.turnDegrees;
    }

    // Update is called once per frame
    void Update()
    {
        if (recoveringFromCollision)
        {
            return;
        }

        HuntTargets();

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
            //Basic Driver Waypoint following
            float forwardAmount, turnAmount;
            FollowWaypoints(out forwardAmount, out turnAmount);
            carController.Drive(turnAmount, forwardAmount);
        }
    }

    private void FollowWaypoints(out float forwardAmount, out float turnAmount)
    {
        if (waypointPath.GetCurrentWaypoint(currentWaypointIndex) != null && !targetLocked)
        {
            lastWaypointIndex = currentWaypointIndex;
            SetWaypoint(waypointPath.GetCurrentWaypoint(currentWaypointIndex).position);
        }
        else
        {
            SetWaypoint(lockedTarget.transform.position);
        }
        forwardAmount = 0f;
        turnAmount = 0f;
        float reachedWaypointDistance = targetLocked ? .2f : 10f;
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

            //Basic Avoidance
            #region Avoidance

            if (avoidanceScript != null && avoidanceScript.ObstacleAhead)
            {
                avoidanceTimer = avoidanceDuration;

                if (!avoidanceScript.LeftBlocked)
                {
                    avoidanceTurn = -1f;
                }
                else if (!avoidanceScript.RightBlocked)
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
            int previousWaypoint = currentWaypointIndex;
            waypointPath.WaypointReached(ref currentWaypointIndex, ref nextWaypointIndex);
            //forwardAmount = 0f;
            //turnAmount = 0f;
            if (currentWaypointIndex < previousWaypoint)
            {
                lapNumber++;
                Debug.Log(gameObject.name + " completed lap " + lapNumber);
            }
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
        //Ignore ground and road stuff
        if (collision.gameObject.name.Contains("Ground") || collision.gameObject.name.Contains("Road"))
            return;


        if (collision.gameObject.CompareTag("Driver"))
        {

            GameObject colGO = collision.gameObject;


            //If we hit the aggressive agent
            if (colGO.TryGetComponent<AggressiveBehaviour>(out AggressiveBehaviour aggDriver))
            {

                //increment the counter and call the coroutine to decrement it after 20s

                aggressiveAgentCollisions++;
                StartCoroutine(DecrementAggressiveCollisonCountAfterTimeLimit());

                //if there have been 4 or more collisions with aggressive in the past 20 seconds,
                //call a truce for 20s.
                if (aggressiveAgentCollisions >= 4 && !truceMade)
                {
                    TryTruce(aggDriver);
                }
            }

            //if we hit the locked on driver
            if (targetLocked && lockedTarget == colGO)
            {
                Targets.Remove(colGO);

                Vector3 direction = (colGO.transform.position - transform.position).normalized;
                //direction.y = 0f;
                colGO.GetComponent<Rigidbody>().AddForce(100 * direction, ForceMode.Impulse);
                return;
            }

            //if the collision agent isn't in the list, add it
            if (!Targets.Contains(colGO))
            {

                //if we have a truce and the agent is the aggressive agent, don't add it
                if (truceMade && colGO == aggDriver.gameObject)
                    return;

                Targets.Add(colGO);
            }
            return;
        }

        if (recoveringFromCollision)
        {
            return;
        }

        StartCoroutine(RecoverFromCollision());
    }

    public void HuntTargets()
    {
        if (!targetLocked && Targets.Count > 0 && inRangeDrivers.Count > 0)
        {
            foreach (var target in Targets)
            {
                if (inRangeDrivers.Contains(target))
                {
                    targetLocked = true;
                    lockedTarget = target;
                    SetWaypoint(target.transform.position);

                    carController.SetGrip(100);
                    carController.SetAcceleration(50);
                    carController.SetTurnDegrees(120);

                    //Debug.Log("Locked on to " + target);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Driver"))
        {

            inRangeDrivers.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Driver"))
        {
            inRangeDrivers.Remove(other.gameObject);

            if (other.gameObject == lockedTarget)
            {
                lockedTarget = null;
                targetLocked = false;

                carController.SetGrip(setGrip);
                carController.SetAcceleration(setAccel);
                carController.SetTurnDegrees(setTurnDegrees);
            }
        }
    }

    #region Truce Methods

    //50% chance to work. If it does we check the same method on teh aggressive driver
    //and see if it works. If it does, start the truce for 20s.
    public void TryTruce(AggressiveBehaviour aggDriver)
    {
        if (Random.Range(0, 2) == 1)
        {
            //Debug.Log("Assertive accepted");

            if (aggDriver.TryTruce(this.gameObject))
            {
                //Debug.Log("Truce Formed!");

                TruceMade(aggDriver.gameObject); //Start the truce

                StartCoroutine(EndTruce()); //End the truce in 20s
            }
        }
    }
    public void TruceMade(GameObject aggDriver)
    {
        truceMade = true; //set the var

        Targets.Remove(aggDriver); //remove aggressive from the target list

        if (aggDriver == lockedTarget) //make sure it's no longer the locked target.
        {
            lockedTarget = null;
            targetLocked = false;

            carController.SetGrip(setGrip);
            carController.SetAcceleration(setAccel);
            carController.SetTurnDegrees(setTurnDegrees);
        }
    }
    IEnumerator EndTruce()
    {
        yield return new WaitForSeconds(20f);
        truceMade = false;
    }


    IEnumerator DecrementAggressiveCollisonCountAfterTimeLimit()
    {
        yield return new WaitForSeconds(truceCollisonTimeLimit);
        aggressiveAgentCollisions--;
    }


    #endregion

    public void ReceiveHotspotInformation()
    {

    }
}
