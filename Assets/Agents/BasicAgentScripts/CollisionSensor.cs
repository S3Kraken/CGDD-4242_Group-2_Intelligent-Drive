using UnityEngine;

public class CollisionSensor : MonoBehaviour
{
    [Tooltip("layers for ground and road, contacts with these are ignored")]
    [SerializeField] private LayerMask ignoredLayers;
    [Tooltip("repeat hits inside this many seconds count once")]
    [SerializeField] private float hitCooldown = 0.25f;

    private RaceManager raceManager;
    private RacingDrivers driver;
    private Vector3 lastPosition;
    private float lastCarHitTime = -10f;
    private float lastWallHitTime = -10f;

    //Speed over the last physics step, so it is the speed before an impact
    public Vector3 PreImpactVelocity { get; private set; }

    void Start()
    {
        raceManager = FindFirstObjectByType<RaceManager>();
        driver = GetComponentInParent<RacingDrivers>();
        lastPosition = transform.position;

        if (ignoredLayers.value == 0)
        {
            ignoredLayers = LayerMask.GetMask("Ground", "Road");
        }
    }

    void FixedUpdate()
    {
        PreImpactVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (raceManager == null || driver == null)
        {
            return;
        }

        int otherLayer = collision.collider.gameObject.layer;
        if (((1 << otherLayer) & ignoredLayers.value) != 0)
        {
            return;
        }

        RacingDrivers otherDriver = collision.collider.GetComponentInParent<RacingDrivers>();

        if (otherDriver == null)
        {
            if (Time.time - lastWallHitTime >= hitCooldown)
            {
                lastWallHitTime = Time.time;
                raceManager.ReportWallHit(driver);
            }
            return;
        }

        if (otherDriver == driver)
        {
            return;
        }

        //Both cars get this callback, so only the one with the lower id reports
        CollisionSensor other = collision.collider.GetComponentInParent<CollisionSensor>();
        if (other != null && GetInstanceID() > other.GetInstanceID())
        {
            return;
        }

        if (Time.time - lastCarHitTime < hitCooldown)
        {
            return;
        }
        lastCarHitTime = Time.time;

        //The car moving harder toward the other one is the attacker
        Vector3 toOther = (otherDriver.DriverTransform.position - transform.position).normalized;
        float myApproach = Vector3.Dot(PreImpactVelocity, toOther);
        float otherApproach = other != null ? Vector3.Dot(other.PreImpactVelocity, -toOther) : 0f;

        if (myApproach >= otherApproach)
        {
            raceManager.ReportCollision(driver, otherDriver);
        }
        else
        {
            raceManager.ReportCollision(otherDriver, driver);
        }
    }
}
