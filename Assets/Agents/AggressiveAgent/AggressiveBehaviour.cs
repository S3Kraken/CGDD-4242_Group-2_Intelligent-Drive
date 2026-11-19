using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using static UnityEngine.GraphicsBuffer;

public class AggressiveBehaviour : MonoBehaviour
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
    bool targetLocked = false;

    bool hadRecentCollision = false;
    bool forceWaypointTravel = false;
    public float timeSinceLastWaypoint = 0.0f;
    public float timeSinceLastCollision = 0.0f;
    private int waypointsCrossed = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        carController = GetComponent<CarController>();
        waypointPath = FindAnyObjectByType<WaypointPath>();
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

        RamIntoOthers();

        //Basic Driver Waypoint following
        float forwardAmount, turnAmount;
        FollowWaypoints(out forwardAmount, out turnAmount);
        carController.Drive(turnAmount, forwardAmount);
    }

    private void FollowWaypoints(out float forwardAmount, out float turnAmount)
    {
        if (waypointPath.GetCurrentWaypoint(currentWaypointIndex) != null && !targetLocked)
        {
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
                foreach (var target in inRangeDrivers)
                {
                    // Finds who is closest to the agent, then picks that one to be rammed into.
                    distance = Vector3.Distance(this.gameObject.transform.position, target.transform.position);
                    if (distance < previousDistance)
                    {
                        lockedTarget = target;
                    }
                    previousDistance = distance;

                }

                targetLocked = true;
                SetWaypoint(lockedTarget.transform.position);

                carController.SetGrip(100);
                carController.SetAcceleration(50);
                carController.SetTurnDegrees(120);

                //Debug.Log("Locked on to " + target);
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
}

