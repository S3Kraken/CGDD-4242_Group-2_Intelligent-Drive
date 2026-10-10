using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilityAgent : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //private Transform targetWaypointTransform;

    private CarController carController;
    private WaypointPath waypointPath;
    private SpeedPad[] allPads;

    private Vector3 currentWaypoint;
    private int currentWaypointIndex;
    private int nextWaypointIndex;

    private SpeedPad currentTargetPad;
    private readonly Dictionary<SpeedPad, float> scores = new Dictionary<SpeedPad, float>();
    private int padsCollected;
    private float lapStartTime = -1f;

    [Header("Utility tuning")]
    [SerializeField] private bool usePads = true;           // untick for the waypoint-only baseline run
    [SerializeField] private float detectionRadius = 40f;   // DETECTION_RADIUS (m)
    [SerializeField] private float pObs = 5f;               // P_OBS (s)
    [SerializeField] private float hCommit = 0.5f;          // H_COMMIT (s)

    [Header("Movement tuning")]
    [SerializeField] private float reachedWaypointDistance = 10f;
    [SerializeField] private float maxSteerAngle = 45f;     // MAX_STEER_ANGLE (degrees)
    [SerializeField] private float slowdownFactor = 0.5f;   // SLOWDOWN_FACTOR
    [SerializeField] private float rayAngle = 30f;          // angle of the left/right rays
    [SerializeField] private float avoidDistance = 10f;     // AVOID_DISTANCE (m)
    [SerializeField] private float avoidStrength = 0.8f;    // AVOID_STRENGTH
    [SerializeField] private float carHalfWidth = 1.2f;

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
        allPads = FindObjectsByType<SpeedPad>(FindObjectsSortMode.None);
    }



    void Update()
    {
        if (recoveringFromCollision)
        {
            return;
        }

        Transform target = waypointPath.GetCurrentWaypoint(currentWaypointIndex);
        if (target != null)
        {
            SetWaypoint(target.position);
        }

        float reachedWaypointDistance = 10f;
        float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint);

        if (distanceToWaypoint >= reachedWaypointDistance)
        {
            currentTargetPad = usePads ? ChooseTarget() : null;
            Vector3 destination = currentTargetPad != null ? currentTargetPad.transform.position : currentWaypoint;
            Debug.DrawLine(transform.position, destination, currentTargetPad != null ? Color.green : Color.yellow);
            MoveToward(destination);
        }
        else
        {
            waypointPath.WaypointReached(ref currentWaypointIndex, ref nextWaypointIndex);
            LogLap();
            carController.Drive(0f, 0f);
        }
    }

    //private void MoveToward(Vector3 destination)
    //{
    //    Vector3 dirToDestination = (destination - transform.position).normalized;
    //    float distance = Vector3.Distance(transform.position, destination);

    //    // Drive forward, unless the destination is behind us and close: then reverse
    //    float forwardAmount = 1f;
    //    float dot = Vector3.Dot(transform.forward, dirToDestination);
    //    float reverseDistance = 25f;
    //    if (dot <= 0 && distance <= reverseDistance)
    //    {
    //        forwardAmount = -1f;
    //    }

    //    // Steer fully left or right toward the destination
    //    float angleToDir = Vector3.SignedAngle(transform.forward, dirToDestination, Vector3.up);
    //    float turnAmount = angleToDir > 0 ? 1f : -1f;

    //    carController.Drive(turnAmount, forwardAmount);
    //}

    private void MoveToward(Vector3 destination)
    {
        Vector3 toDest = destination - transform.position;
        toDest.y = 0f;

        float angle = Vector3.SignedAngle(transform.forward, toDest, Vector3.up);
        float steer = Mathf.Clamp(angle / maxSteerAngle, -1f, 1f);
        float throttle = 1f - slowdownFactor * Mathf.Abs(steer);   //calculated before the avoidance nudges

        Vector3 leftDir = Quaternion.Euler(0f, -rayAngle, 0f) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0f, rayAngle, 0f) * transform.forward;

        bool leftHit = Physics.Raycast(transform.position, leftDir, avoidDistance, ~0, QueryTriggerInteraction.Ignore);
        bool rightHit = Physics.Raycast(transform.position, rightDir, avoidDistance, ~0, QueryTriggerInteraction.Ignore);

        Debug.DrawRay(transform.position, leftDir * avoidDistance, leftHit ? Color.red : Color.green);
        Debug.DrawRay(transform.position, rightDir * avoidDistance, rightHit ? Color.red : Color.green);

        if (leftHit)
        {
            steer += avoidStrength;
        }
        if (rightHit)
        {
            steer -= avoidStrength;
        }

        steer = Mathf.Clamp(steer, -1f, 1f);
        carController.Drive(steer, throttle);
    }

    public void SetWaypoint(Vector3 waypoint)
    {
        currentWaypoint = waypoint;
    }

    private SpeedPad ChooseTarget()
    {
        scores.Clear();
        SpeedPad bestPad = null;
        float bestScore = -50f;

        foreach (SpeedPad pad in allPads)
        {
            if (Vector3.Distance(transform.position, pad.transform.position) > detectionRadius)
            {
                continue;
            }

            float score = ScorePad(pad);
            scores[pad] = score;

            if (score > bestScore)
            {
                bestPad = pad;
                bestScore = score;
            }
        }

        return bestScore > 0f ? bestPad : null;
    }

    private float ScorePad(SpeedPad pad)
    {
        if (!pad.IsAvailable)
        {
            return -100f;
        }

        float vCruise = carController.CruiseSpeed;
        Vector3 carPos = transform.position;
        Vector3 padPos = pad.transform.position;

        float benefit = pad.TBoost * (pad.VBoost / vCruise - 1f);

        float detour = (Vector3.Distance(carPos, padPos)
                      + Vector3.Distance(padPos, currentWaypoint)
                      - Vector3.Distance(carPos, currentWaypoint)) / vCruise;

        Vector3 toPad = padPos - carPos;
        toPad.y = 0f;
        float angle = Mathf.Abs(Vector3.SignedAngle(transform.forward, toPad, Vector3.up)) * Mathf.Deg2Rad;
        float turn = angle / carController.TurnRate;

        float obstacle = PathBlocked(pad) ? pObs : 0f;
        float bonus = pad == currentTargetPad ? hCommit : 0f;

        return benefit - detour - turn - obstacle + bonus;
    }

    private bool PathBlocked(SpeedPad pad)
    {
        Vector3 toPad = pad.transform.position - transform.position;
        toPad.y = 0f;
        Vector3 direction = toPad.normalized;
        float distance = toPad.magnitude;

        // Sideways direction, measured across the line to the pad
        Vector3 side = Vector3.Cross(Vector3.up, direction);

        float[] offsets = { -carHalfWidth, 0f, carHalfWidth };
        foreach (float offset in offsets)
        {
            Vector3 origin = transform.position + side * offset;
            if (Physics.Raycast(origin, direction, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                return true;
            }
        }
        return false;
    }

    public void OnPadCollected(SpeedPad pad)
    {
        padsCollected++;
        Debug.Log("Pad Collected: " + padsCollected);
        if (pad == currentTargetPad)
        {
            currentTargetPad = null;
        }
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

        StartCoroutine(RecoverFromCollision());

        collisionsCount++;
    }

    private void LogLap()
    {
        Debug.Log("LogLap called, current index = " + currentWaypointIndex);
        if (currentWaypointIndex != 1) return;
        if (lapStartTime >= 0f)
        {
            Debug.Log($"Lap time: {Time.time - lapStartTime:F2} s | pads collected so far: {padsCollected} | usePads: {usePads}");
        }
        lapStartTime = Time.time;
    }

    private void OnGUI()
    {
        int y = 10;
        GUI.Label(new Rect(10, y, 500, 20), "Target: " + (currentTargetPad != null ? currentTargetPad.name : "waypoint"));
        foreach (KeyValuePair<SpeedPad, float> entry in scores)
        {
            y += 20;
            GUI.Label(new Rect(10, y, 500, 20), $"{entry.Key.name}: {entry.Value:F2} s");
        }
    }
}

