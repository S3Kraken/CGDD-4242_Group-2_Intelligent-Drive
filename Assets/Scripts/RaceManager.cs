using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using static UnityEngine.GraphicsBuffer;

public class RaceManager : MonoBehaviour
{
    [SerializeField] private Leaderboard leaderboardUI;
    // For Rigidbody Collision and Speed Pad Monitoring.
    private Dictionary<RacingDrivers, int> collisionsCaused = new Dictionary<RacingDrivers, int>();
    private Dictionary<RacingDrivers, int> collisionsTaken = new Dictionary<RacingDrivers, int>();
    private Dictionary<RacingDrivers, int> wallHits = new Dictionary<RacingDrivers, int>();
    private Dictionary<RacingDrivers, int> padsCollected = new Dictionary<RacingDrivers, int>();
    private Dictionary<RacingDrivers, RacingDrivers> lastAttacker = new Dictionary<RacingDrivers, RacingDrivers>();

    public void ReportCollision(RacingDrivers attacker, RacingDrivers victim)
    {
        AddOne(collisionsCaused, attacker);
        AddOne(collisionsTaken, victim);
        lastAttacker[victim] = attacker;
        Debug.Log(attacker.DriverTransform.name + " hit " + victim.DriverTransform.name);
    }

    public void ReportWallHit(RacingDrivers driver)
    {
        AddOne(wallHits, driver);
        Debug.Log(driver.DriverTransform.name + " hit a wall");
    }

    public void ReportPadCollected(SpeedPad pad, RacingDrivers driver)
    {
        AddOne(padsCollected, driver);
        Debug.Log(driver.DriverTransform.name + " collected " + pad.name);
    }

    public RacingDrivers GetLastAttacker(RacingDrivers victim)
    {
        RacingDrivers attacker;
        lastAttacker.TryGetValue(victim, out attacker);
        return attacker;
    }

    public int GetCollisionsCaused(RacingDrivers driver) { return GetCount(collisionsCaused, driver); }
    public int GetCollisionsTaken(RacingDrivers driver) { return GetCount(collisionsTaken, driver); }
    public int GetWallHits(RacingDrivers driver) { return GetCount(wallHits, driver); }
    public int GetPadsCollected(RacingDrivers driver) { return GetCount(padsCollected, driver); }

    private void AddOne(Dictionary<RacingDrivers, int> counts, RacingDrivers driver)
    {
        int count;
        counts.TryGetValue(driver, out count);
        counts[driver] = count + 1;
    }

    private int GetCount(Dictionary<RacingDrivers, int> counts, RacingDrivers driver)
    {
        int count;
        counts.TryGetValue(driver, out count);
        return count;
    }

    // End of Rigidbody Collision and Speed Pad Monitoring changes.

    // Drag all AI driver objects into this array in the Inspector.
    public MonoBehaviour[] driverObjects;

    private List<RacingDrivers> drivers = new List<RacingDrivers>();

    private Dictionary<RacingDrivers, float> driverProgress =
        new Dictionary<RacingDrivers, float>();

    [SerializeField] private int raceLaps = 3;

    public int RaceLaps { get { return raceLaps; } }

    void Start()
    {
        InitializeDrivers();
    }
    void InitializeDrivers()
    {
        foreach (MonoBehaviour driverObject in driverObjects)
        {
            RacingDrivers driver = driverObject as RacingDrivers;

            if (driver != null)
            {
                drivers.Add(driver);
                driverProgress.Add(driver, 0f);
            }
            else
            {
                string targetName = driverObject != null ? driverObject.name : "None";
                Debug.LogWarning(targetName + " does not implement RaceDriver.");
            }
        }
    }

    // Continuously receives progress information from all drivers.
    void Update()
    {
        foreach (RacingDrivers driver in drivers)
        {
            ReceiveDriverProgress(driver);
        }
        
            UpdateLeaderboard();
        if (leaderboardUI != null)
        {
            leaderboardUI.UpdateLeaderboard(drivers);
        }
        
    }

    // Receives waypoint, distance, and lap information from a driver.
    void ReceiveDriverProgress(RacingDrivers driver)
    {
        int waypoint = driver.CurrentWaypoint;
        int lap = driver.LapNumber;
        float distance = driver.DistanceToWaypoint;

        // Calculates approximate progress through the race.
        float progress = (lap - 1) * 1000f + waypoint * 10f
                         - distance;

        driverProgress[driver] = progress;
    }

    void UpdateLeaderboard()
    {
        List<RacingDrivers> leaderboard = drivers
        .OrderByDescending(driver =>
            (driver.LapNumber - 1) + driver.RaceProgress)
        .ToList();

        for (int i = 0; i < leaderboard.Count; i++)
        {
            Debug.Log((i + 1) + ". " +
                      leaderboard[i].DriverTransform.name);
            Debug.Log("1st Place: " + leaderboard[0].DriverTransform.name);
        }
    }

    public void ShareHotspotInformation()
    {
        foreach (RacingDrivers driver in drivers)
        {
            driver.ReceiveHotspotInformation();
        }
    }
    public void ResetDriver(RacingDrivers driver)
    {
        if (driver != null)
        {
            driver.ResetDriver();
        }
    }
}