using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using static UnityEngine.GraphicsBuffer;

public class AggressiveBehaviour : MonoBehaviour, RacingDrivers
{
    private CarController carController;
    private WaypointPath waypointPath;

    [SerializeField] private Vector3 currentWaypoint;
    private int currentWaypointIndex;
    private int nextWaypointIndex;

    //this is if you want the collision recovery
    #region collision recovery variables
    private bool recoveringFromCollision = false;

    public float recoveryReverseTime = 1.0f;
    public float recoveryTurnTime = 0.75f;
    #endregion


    [SerializeField] List<GameObject> Targets = new List<GameObject>();
    [SerializeField] List<GameObject> inRangeDrivers = new List<GameObject>();
    [SerializeField] GameObject lockedTarget;
    private Collider driverCollider;
    private Rigidbody rb;
    bool targetLocked = false;

    bool hadRecentCollision = false;
    bool forceWaypointTravel = false;
    private bool isResetting = false;
    public float timeSinceLastWaypoint = 0.0f;
    public float timeSinceLastCollision = 0.0f;
    private int waypointsCrossed = 0;
    private int lapNumber = 1;
    private int lastWaypointIndex = 0;


    bool truceMade;

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
    public void ResetDriver()
    {
        isResetting = true;
        //driverCollider.enabled = false;
        //transform.position = waypointPath.GetCurrentWaypoint(lastWaypointIndex).position;
        //driverCollider.enabled = true;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        carController = GetComponent<CarController>();
        waypointPath = FindAnyObjectByType<WaypointPath>();
        rb = GetComponent<Rigidbody>();
        driverCollider = GetComponent<Collider>();
        currentWaypointIndex = waypointPath.CurrentIndex;
        nextWaypointIndex = waypointPath.NextIndex;
    }

    // Update is called once per frame
    void Update()
    {
        if (hadRecentCollision)
        {
            timeSinceLastWaypoint += Time.deltaTime;
            timeSinceLastCollision += Time.deltaTime;

            // If too much time has passed since a waypoint was crossed, prioritize travelling to the waypoints
            if (timeSinceLastWaypoint >= 2.0f)
            {
                lockedTarget = null;
                targetLocked = false;

                forceWaypointTravel = true;
                hadRecentCollision = false;

                //Debug.Log("Forced Waypoint Enabled");
            }

            if (timeSinceLastCollision >= 5.0f)
            {
                hadRecentCollision = false;
                timeSinceLastCollision = 0.0f;
            }
        }

        if (recoveringFromCollision)
        {
            return;
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
                RamIntoOthers();

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
        }
        else
        {
            waypointPath.WaypointReached(ref currentWaypointIndex, ref nextWaypointIndex);
            forwardAmount = 0f;
            turnAmount = 0f;

            timeSinceLastWaypoint = 0.0f;

            // Track how many waypoints have been prioritized. Return to normal behavior after crossing two waypoints.
            if (forceWaypointTravel)
            {
                waypointsCrossed++;
                // Debug.Log("Forced Waypoint Crossed: " + waypointsCrossed);

                if (waypointsCrossed >= 2)
                {
                    waypointsCrossed = 0;
                    forceWaypointTravel = false;
                    hadRecentCollision = false;
                    //Debug.Log("Forced Waypoint Disabled");
                }
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
        if (collision.gameObject.name == "Ground" || collision.gameObject.name == "Road")
            return;

        if (collision.gameObject.CompareTag("Driver"))
        {
            GameObject colGO = collision.gameObject;

            Vector3 direction = (colGO.transform.position - transform.position).normalized;
            //direction.y = 0f;
            colGO.GetComponent<Rigidbody>().AddForce(100 * direction, ForceMode.Impulse);
            hadRecentCollision = true;
            timeSinceLastCollision = 0.0f;
            // Debug.Log("Recent Collision Detected");
            return;
        }

        if (recoveringFromCollision)
        {
            return;
        }

        StartCoroutine(RecoverFromCollision());
    }

    public void RamIntoOthers()
    {
        // If the agent is forcefully trying to reach the waypoints, then it ignores trying to ram into others.
        if (forceWaypointTravel)
        {
            return;
        }

        if (inRangeDrivers.Count > 0)
        {
            float distance = 0.0f;
            float previousDistance = float.MaxValue;

            if (!lockedTarget)
            {
                foreach (GameObject target in inRangeDrivers)
                {

                    //Don't check assertive agents if the truce is made.
                    if (truceMade && target.TryGetComponent<AssertiveScript>(out AssertiveScript assertive))
                        continue;


                    // Finds who is closest to the agent, then picks that one to be rammed into.
                    distance = Vector3.Distance(this.gameObject.transform.position, target.transform.position);
                    if (distance < previousDistance)
                    {
                        previousDistance = distance;
                        lockedTarget = target;
                    }

                }

                if (lockedTarget != null)
                {
                    targetLocked = true;
                    SetWaypoint(lockedTarget.transform.position);

                    carController.SetGrip(100);
                    carController.SetAcceleration(50);
                    carController.SetTurnDegrees(120);

                    //Debug.Log("Locked on to " + target);}
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

                carController.SetGrip(5);
                carController.SetAcceleration(40);
                carController.SetTurnDegrees(90);
            }
        }
    }


    #region Truce Methods


    //Called from the assertive driver and passes itself.
    //50% chance for the truce to work and returns true if it did and false otherwise.
    public bool TryTruce(GameObject assertiveDriver)
    {
        if (Random.Range(0, 2) == 1)
        {
            TruceMade(assertiveDriver);
            return true;
        }
        return false;
    }


    public void TruceMade(GameObject assertiveDriver)
    {

        //set the truce var to true
        truceMade = true;


        //remove the assertive driver from the inRange list and make sure it's not a 
        //locked target anymore

        inRangeDrivers.Remove(assertiveDriver);

        if (assertiveDriver == lockedTarget)
        {
            lockedTarget = null;
            targetLocked = false;

            carController.SetGrip(5);
            carController.SetAcceleration(40);
            carController.SetTurnDegrees(90);
        }

        StartCoroutine(EndTruce());
    }

    IEnumerator EndTruce() //Ends the truce after 20s
    {
        yield return new WaitForSeconds(20f);
        truceMade = false;
    }


    #endregion

    public void ReceiveHotspotInformation()
    {

    }
}

