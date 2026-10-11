using UnityEngine;

public class DefensiveDriver : MonoBehaviour
{

    public Transform[] waypoints;
    public float waypointRadius = 3f;

    public float speed = 8f;
    public float turnSpeed = 5f;
    public float acceleration = 10f;
    [Range(0.1f, 1f)] public float blockedSpeedMultiplier = 0.5f;
    public string opponentTag = "Opponent";
    public float frontDistance = 8f;
    public float sideDistance = 4f;
    public float frontAngle = 25f;
    public float sensorRadius = 0.75f;
    public float sensorHeight = 0.5f;

    public float maxAvoidAngle = 40f;
    public float avoidanceSmoothing = 3f;
    public float clearDelay = 0.4f;

    private int currentWaypoint = 0;
    private float currentSpeed;
    private int avoidSide = 0;
    private float avoidance = 0f;
    private float holdTimer = 0f;
    private bool bothSidesBlocked = false;

    void Start()
    {
        currentSpeed = speed;
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        UpdateAvoidance();

        Vector3 waypointDir = GetWaypointDirection();
        Vector3 direction = Quaternion.AngleAxis(avoidance * maxAvoidAngle, Vector3.up) * waypointDir;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }

        float targetSpeed = bothSidesBlocked ? speed * blockedSpeedMultiplier : speed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        CheckWaypoint();
    }

    Vector3 GetWaypointDirection()
    {
        Vector3 direction = waypoints[currentWaypoint].position - transform.position;
        direction.y = 0;
        return direction.normalized;
    }

    void UpdateAvoidance()
    {
        // Sensors
        bool front = Sense(transform.forward, frontDistance);
        bool frontL = Sense(Quaternion.Euler(0, -frontAngle, 0) * transform.forward, frontDistance);
        bool frontR = Sense(Quaternion.Euler(0, frontAngle, 0) * transform.forward, frontDistance);
        bool left = Sense(-transform.right, sideDistance);
        bool right = Sense(transform.right, sideDistance);

        bool leftBlocked = left || frontL;
        bool rightBlocked = right || frontR;
        bool threat = front || leftBlocked || rightBlocked;

        bothSidesBlocked = leftBlocked && rightBlocked;

        if (threat)
        {
            holdTimer = clearDelay;

            if (avoidSide == -1 && leftBlocked) avoidSide = 0;
            if (avoidSide == 1 && rightBlocked) avoidSide = 0;

            if (avoidSide == 0)
            {
                if (!leftBlocked && !rightBlocked)
                    avoidSide = PreferredSide();
                else if (!leftBlocked)
                    avoidSide = -1;
                else if (!rightBlocked)
                    avoidSide = 1;
            }
        }
        else
        {
            holdTimer -= Time.deltaTime;
            if (holdTimer <= 0f)
                avoidSide = 0;
        }

        avoidance = Mathf.MoveTowards(avoidance, avoidSide, avoidanceSmoothing * Time.deltaTime);
    }

    int PreferredSide()
    {
        float dot = Vector3.Dot(transform.right, GetWaypointDirection());
        return dot >= 0f ? 1 : -1;
    }

    bool Sense(Vector3 dir, float distance)
    {
        Vector3 origin = transform.position + Vector3.up * sensorHeight;

        bool hitSomething = Physics.SphereCast(origin, sensorRadius, dir, out RaycastHit hit, distance);

        bool isOpponent = hitSomething
            && hit.collider.transform.root != transform.root
            && (hit.collider.CompareTag(opponentTag) || hit.collider.transform.root.CompareTag(opponentTag));

        Debug.DrawRay(origin, dir * distance, isOpponent ? Color.red : Color.green);

        return isOpponent;
    }

    void CheckWaypoint()
    {
        if (Vector3.Distance(transform.position, waypoints[currentWaypoint].position) < waypointRadius)
        {
            currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
        }
    }
}